using System.Globalization;
using System.Text;

namespace PangyaAPI.IFF;

public sealed record IffPatchFieldResult(
    string FieldName,
    bool Changed,
    string? SkipReason,
    bool Truncated)
{
    internal IffField TargetField { get; init; } = null!;
    internal object? SourceValue { get; init; }
}

public sealed record IffPatchCandidate(
    uint ItemId,
    int TargetRecordIndex,
    string TargetLabel,
    string SourceLabel,
    ReadOnlyMemory<byte> TargetBytes,
    IReadOnlyList<IffPatchFieldResult> FieldResults)
{
    internal Encoding TargetEncoding { get; init; } = Encoding.Latin1;

    public int ChangedFieldCount => FieldResults.Count(result => result.Changed);
    public IReadOnlyList<string> SkippedFields => FieldResults
        .Where(result => result.SkipReason is not null)
        .Select(result => $"{result.FieldName}: {result.SkipReason}")
        .ToArray();
    public IReadOnlyList<string> TruncatedFields => FieldResults
        .Where(result => result.Truncated)
        .Select(result => result.FieldName)
        .ToArray();
}

public sealed record IffPatchSelectionSummary(
    int SelectedRecordCount,
    int ChangedRecordCount,
    int ChangedFieldCount,
    int SkippedFieldCount,
    int TruncatedFieldCount);

public sealed record IffPatchAnalysis(
    IReadOnlyList<IffPatchCandidate> Candidates,
    IReadOnlyList<string> SelectableFields,
    int SourceOnlyRecordCount,
    int TargetOnlyRecordCount)
{
    public int ChangedFieldCount => Candidates.Sum(candidate => candidate.ChangedFieldCount);
    public int SkippedFieldCount => Candidates.Sum(candidate => candidate.SkippedFields.Count);
    public int TruncatedFieldCount => Candidates.Sum(candidate => candidate.TruncatedFields.Count);

    public IffPatchSelectionSummary Summarize(
        IEnumerable<uint> selectedItemIds,
        IEnumerable<string> selectedFieldNames)
    {
        ArgumentNullException.ThrowIfNull(selectedItemIds);
        ArgumentNullException.ThrowIfNull(selectedFieldNames);
        HashSet<uint> selectedItems = selectedItemIds.ToHashSet();
        HashSet<string> selectedFields = selectedFieldNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        IffPatchCandidate[] candidates = Candidates
            .Where(candidate => selectedItems.Contains(candidate.ItemId))
            .ToArray();

        int changedRecords = 0;
        int changedFields = 0;
        int skippedFields = 0;
        int truncatedFields = 0;
        foreach (IffPatchCandidate candidate in candidates)
        {
            PatchResult result = BuildResult(candidate, candidate.TargetBytes.Span, selectedFields);
            if (result.BytesChanged) changedRecords++;
            changedFields += result.ChangedFieldCount;
            skippedFields += result.SkippedFieldCount;
            truncatedFields += result.TruncatedFieldCount;
        }

        return new IffPatchSelectionSummary(
            candidates.Length, changedRecords, changedFields, skippedFields, truncatedFields);
    }

    public int Apply(
        IList<IffRecord> targetRecords,
        IEnumerable<uint> selectedItemIds,
        IEnumerable<string> selectedFieldNames)
    {
        ArgumentNullException.ThrowIfNull(targetRecords);
        ArgumentNullException.ThrowIfNull(selectedItemIds);
        ArgumentNullException.ThrowIfNull(selectedFieldNames);
        HashSet<uint> selectedItems = selectedItemIds.ToHashSet();
        HashSet<string> selectedFields = selectedFieldNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        IffPatchCandidate[] candidates = Candidates
            .Where(candidate => selectedItems.Contains(candidate.ItemId))
            .ToArray();

        foreach (IffPatchCandidate candidate in candidates)
        {
            if (candidate.TargetRecordIndex < 0 || candidate.TargetRecordIndex >= targetRecords.Count)
                throw new InvalidOperationException("The target IFF records changed after the patch was analyzed.");
            if (targetRecords[candidate.TargetRecordIndex].Bytes.Length != candidate.TargetBytes.Length)
                throw new InvalidOperationException("The target IFF record size changed after the patch was analyzed.");
        }

        int changedRecords = 0;
        foreach (IffPatchCandidate candidate in candidates)
        {
            IffRecord target = targetRecords[candidate.TargetRecordIndex];
            PatchResult result = BuildResult(candidate, target.Bytes.Span, selectedFields);
            if (!result.BytesChanged) continue;
            target.ReplaceBytes(result.Bytes);
            changedRecords++;
        }
        return changedRecords;
    }

    private static PatchResult BuildResult(
        IffPatchCandidate candidate,
        ReadOnlySpan<byte> targetBytes,
        HashSet<string> selectedFields)
    {
        byte[] result = targetBytes.ToArray();
        int changedFields = 0;
        int skippedFields = 0;
        int truncatedFields = 0;

        foreach (IffPatchFieldResult fieldResult in candidate.FieldResults)
        {
            if (!selectedFields.Contains(fieldResult.FieldName)) continue;
            if (fieldResult.SkipReason is not null)
            {
                skippedFields++;
                continue;
            }

            IffField field = fieldResult.TargetField;
            byte[] before = result.AsSpan(field.Offset, field.Width).ToArray();
            field.SetValue(result, fieldResult.SourceValue, candidate.TargetEncoding);
            if (!before.AsSpan().SequenceEqual(result.AsSpan(field.Offset, field.Width))) changedFields++;
            if (fieldResult.Truncated) truncatedFields++;
        }

        return new PatchResult(
            result,
            !targetBytes.SequenceEqual(result),
            changedFields,
            skippedFields,
            truncatedFields);
    }

    private sealed record PatchResult(
        byte[] Bytes,
        bool BytesChanged,
        int ChangedFieldCount,
        int SkippedFieldCount,
        int TruncatedFieldCount);
}

public static class IffPatchAnalyzer
{
    private static readonly string[] KeyNames = ["ItemId", "TypeID", "ID"];

    public static IffPatchAnalysis Analyze(
        IffDocumentInfo targetDocument,
        IReadOnlyList<IffRecord> targetRecords,
        Encoding targetEncoding,
        IffDocumentInfo sourceDocument,
        IReadOnlyList<IffRecord> sourceRecords,
        Encoding sourceEncoding)
    {
        ArgumentNullException.ThrowIfNull(targetDocument);
        ArgumentNullException.ThrowIfNull(targetRecords);
        ArgumentNullException.ThrowIfNull(targetEncoding);
        ArgumentNullException.ThrowIfNull(sourceDocument);
        ArgumentNullException.ThrowIfNull(sourceRecords);
        ArgumentNullException.ThrowIfNull(sourceEncoding);

        IffSchema targetSchema = targetDocument.Schema ??
            throw new InvalidDataException("The target IFF does not have a schema.");
        IffSchema sourceSchema = sourceDocument.Schema ??
            throw new InvalidDataException("The source IFF does not have a schema.");
        IffField targetKey = ResolveKeyField(targetSchema, "target");
        IffField sourceKey = ResolveKeyField(sourceSchema, "source");
        Dictionary<uint, (int Index, IffRecord Record)> targets =
            IndexRecords(targetRecords, targetKey, targetEncoding, "target");
        Dictionary<uint, (int Index, IffRecord Record)> sources =
            IndexRecords(sourceRecords, sourceKey, sourceEncoding, "source");

        Dictionary<string, IffField> sourceFields = sourceSchema.Fields
            .Where(field => field.Type != IffFieldType.Raw)
            .ToDictionary(field => field.Name, StringComparer.OrdinalIgnoreCase);
        IffField[] selectableFields = targetSchema.Fields
            .Where(field => IsSelectable(field, targetKey, sourceFields, sourceKey))
            .ToArray();

        var candidates = new List<IffPatchCandidate>();
        foreach ((uint itemId, (int targetIndex, IffRecord targetRecord)) in targets.OrderBy(pair => pair.Key))
        {
            if (!sources.TryGetValue(itemId, out (int Index, IffRecord Record) source)) continue;
            candidates.Add(AnalyzeRecord(itemId, targetIndex, targetRecord, source.Record, selectableFields,
                sourceFields, targetEncoding, sourceEncoding));
        }

        return new IffPatchAnalysis(
            candidates,
            selectableFields.Select(field => field.Name).ToArray(),
            sources.Keys.Except(targets.Keys).Count(),
            targets.Keys.Except(sources.Keys).Count());
    }

    private static bool IsSelectable(
        IffField targetField,
        IffField targetKey,
        IReadOnlyDictionary<string, IffField> sourceFields,
        IffField sourceKey)
    {
        if (targetField.Type == IffFieldType.Raw || !targetField.IsEditable ||
            ReferenceEquals(targetField, targetKey) ||
            KeyNames.Contains(targetField.Name, StringComparer.OrdinalIgnoreCase))
            return false;
        return sourceFields.TryGetValue(targetField.Name, out IffField? sourceField) &&
            !ReferenceEquals(sourceField, sourceKey) &&
            AreCompatible(sourceField.Type, targetField.Type);
    }

    private static IffPatchCandidate AnalyzeRecord(
        uint itemId,
        int targetIndex,
        IffRecord target,
        IffRecord source,
        IReadOnlyList<IffField> selectableFields,
        IReadOnlyDictionary<string, IffField> sourceFields,
        Encoding targetEncoding,
        Encoding sourceEncoding)
    {
        byte[] result = target.Bytes.ToArray();
        var fieldResults = new List<IffPatchFieldResult>(selectableFields.Count);

        foreach (IffField targetField in selectableFields)
        {
            IffField sourceField = sourceFields[targetField.Name];
            object? value = null;
            string? skipReason = null;
            bool truncated = false;
            bool changed = false;
            try
            {
                value = sourceField.GetValue(source.Bytes.Span, sourceEncoding);
                if (IsString(targetField.Type))
                {
                    string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
                    string fitted = FitString(text, targetField.Width, targetField.Encoding ?? targetEncoding);
                    if (!string.Equals(text, fitted, StringComparison.Ordinal))
                    {
                        value = fitted;
                        truncated = true;
                    }
                }

                byte[] before = result.AsSpan(targetField.Offset, targetField.Width).ToArray();
                targetField.SetValue(result, value, targetEncoding);
                changed = !before.AsSpan().SequenceEqual(result.AsSpan(targetField.Offset, targetField.Width));
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidDataException or InvalidOperationException
                or FormatException or OverflowException)
            {
                skipReason = ex.Message;
            }

            fieldResults.Add(new IffPatchFieldResult(
                targetField.Name, changed, skipReason, truncated)
            {
                TargetField = targetField,
                SourceValue = value
            });
        }

        return new IffPatchCandidate(
            itemId,
            targetIndex,
            DisplayLabel(target, targetEncoding),
            DisplayLabel(source, sourceEncoding),
            target.Bytes.ToArray(),
            fieldResults)
        {
            TargetEncoding = targetEncoding
        };
    }

    private static Dictionary<uint, (int Index, IffRecord Record)> IndexRecords(
        IReadOnlyList<IffRecord> records, IffField key, Encoding encoding, string side)
    {
        var indexed = new Dictionary<uint, (int, IffRecord)>();
        for (int index = 0; index < records.Count; index++)
        {
            uint itemId;
            try
            {
                itemId = Convert.ToUInt32(key.GetValue(records[index].Bytes.Span, encoding), CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
            {
                throw new InvalidDataException($"The {side} IFF record {index} has an invalid item ID.", ex);
            }
            if (!indexed.TryAdd(itemId, (index, records[index])))
                throw new InvalidDataException($"The {side} IFF contains duplicate item ID {itemId}.");
        }
        return indexed;
    }

    private static IffField ResolveKeyField(IffSchema schema, string side)
    {
        foreach (string keyName in KeyNames)
        {
            IffField[] matches = schema.Fields
                .Where(field => field.Name.Equals(keyName, StringComparison.OrdinalIgnoreCase) &&
                    field.Type is IffFieldType.UInt32 or IffFieldType.ItemIdReference or IffFieldType.Int32)
                .ToArray();
            if (matches.Length > 1)
                throw new InvalidDataException($"The {side} IFF schema has an ambiguous '{keyName}' item ID field.");
            if (matches.Length == 1) return matches[0];
        }
        throw new InvalidDataException($"The {side} IFF schema has no ItemId, TypeID, or ID field.");
    }

    private static string DisplayLabel(IffRecord record, Encoding encoding)
    {
        foreach (string name in new[] { "Name", "Description" })
        {
            if (!record.TryGetValue(name, out object? value, encoding)) continue;
            string text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
                return text.Length <= 80 ? text : text[..77] + "...";
        }
        return string.Empty;
    }

    private static bool AreCompatible(IffFieldType source, IffFieldType target)
    {
        if (IsString(source) && IsString(target)) return true;
        if (source == IffFieldType.DateTime || target == IffFieldType.DateTime)
            return source == target;
        return IsScalar(source) && IsScalar(target);
    }

    private static bool IsString(IffFieldType type) =>
        type is IffFieldType.FixedString or IffFieldType.LongString or IffFieldType.Icon or IffFieldType.ImageResource or IffFieldType.Sound;

    private static bool IsScalar(IffFieldType type) =>
        type is IffFieldType.Boolean or IffFieldType.Byte or IffFieldType.UInt16 or IffFieldType.Int16 or
            IffFieldType.UInt32 or IffFieldType.Int32 or IffFieldType.Int64 or IffFieldType.Single or
            IffFieldType.BitField or IffFieldType.BooleanBitField or IffFieldType.ByteRangeBoolean or
            IffFieldType.ItemIdReference;

    private static string FitString(string value, int width, Encoding encoding)
    {
        if (width <= 1) return string.Empty;
        int maximumBytes = width - 1;
        if (encoding.GetByteCount(value) <= maximumBytes) return value;

        var builder = new StringBuilder();
        TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(value);
        while (elements.MoveNext())
        {
            string element = elements.GetTextElement();
            if (encoding.GetByteCount(builder.ToString()) + encoding.GetByteCount(element) > maximumBytes) break;
            builder.Append(element);
        }
        return builder.ToString();
    }
}
