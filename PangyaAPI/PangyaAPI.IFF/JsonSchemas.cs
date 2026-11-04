using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PangyaAPI.IFF;

public sealed record IffSchemaResolution(IffSchema? Schema, string? Warning = null);

public interface IIffSchemaProvider
{
    IffSchemaResolution Resolve(string fileName, string region, int recordSize);
    IffSchemaResolution Resolve(string fileName, IReadOnlyList<string> regions, int recordSize) =>
        Resolve(fileName, regions.Count == 0 ? "Unknown" : regions[0], recordSize);
    IffSchemaResolution ResolveBase(IffSchemaBaseReference reference, IReadOnlyList<string> regions, int recordSize) =>
        Resolve(reference.Name + ".iff", reference.Region is { Length: > 0 } fixedRegion ? [fixedRegion] : regions,
            recordSize);
}

public static class IffSchemaRegistry
{
    private static readonly IIffSchemaProvider EmbeddedProvider = new EmbeddedIffSchemaProvider();

    public static IffSchema? Resolve(string fileName, IffHeader header, int recordSize)
    {
        IffSchemaResolution resolution = ResolveDetailed(fileName, header, recordSize);
        if (resolution.Schema is not null) return resolution.Schema;
        IffFormatProfile? profile = header.FormatProfile;
        return new IffSchema(Path.GetFileNameWithoutExtension(fileName), recordSize,
            [new IffField("Raw record", 0, recordSize, IffFieldType.Raw, false, IsVisible: false)], false,
            profile?.DefaultStringSize ?? Math.Min(32, recordSize),
            DefaultLongStringSize: profile?.DefaultLongStringSize ?? 512);
    }

    public static IffSchemaResolution ResolveDetailed(string fileName, IffHeader header, int recordSize,
        IIffSchemaProvider? provider = null) =>
        (provider ?? EmbeddedProvider).Resolve(fileName,
            header.FormatProfile?.SchemaRegions ?? [header.Region], recordSize);
}

public sealed record IffSchemaBaseReference(string Name, string? Region = null);

public sealed record IffSchemaDefinition(
    int SchemaVersion,
    string FileName,
    string Region,
    int MinimumRecordSize,
    bool IsEditable,
    IReadOnlyList<IffFieldDefinition> Fields,
    int DefaultStringSize = 1,
    IffSchemaUiDefinition? Ui = null,
    int DefaultLongStringSize = 512,
    IffSchemaBaseReference? Base = null,
    int DefaultRevision = 0,
    string? KeyField = null,
    bool IsOpaque = false,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] bool IsKeyless = false);

public sealed record IffFieldDefinition(
    string Name,
    int Offset,
    int Width,
    IffFieldType Type,
    bool IsEditable = true,
    int? EncodingCodePage = null,
    long? Minimum = null,
    long? Maximum = null,
    uint? BitMask = null,
    int BitShift = 0,
    bool? IsVisible = null,
    IffFieldReferenceDefinition? Reference = null,
    string? IconPath = null,
    string? SoundPath = null);

public sealed record IffFieldReferenceDefinition(
    string TargetFile,
    string TargetKeyField = "ItemId",
    string DisplayField = "Name",
    string IconField = "Icon",
    bool? PickerEnabled = null);

public sealed record IffSchemaUiDefinition(IReadOnlyList<IffFormTabDefinition> Tabs);

public sealed record IffFormTabDefinition(string Name, IReadOnlyList<IffFormFieldDefinition> Fields);

public sealed record IffFormFieldDefinition(
    string Field,
    string? Label = null,
    string? Editor = null,
    bool? IsVisible = null,
    int Order = 0);

public static class IffSchemaJson
{
    public const int CurrentVersion = 2;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    static IffSchemaJson() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static string Serialize(IffSchemaDefinition definition) =>
        JsonSerializer.Serialize(definition, Options);

    public static IffSchemaDefinition Deserialize(string json)
    {
        IffSchemaDefinition definition = JsonSerializer.Deserialize<IffSchemaDefinition>(json, Options)
            ?? throw new InvalidDataException("The IFF schema JSON is empty.");
        using JsonDocument document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("defaultStringSize", out _))
        {
            int inferred = definition.Fields?.FirstOrDefault(field =>
                    field.Type is IffFieldType.FixedString or IffFieldType.Icon or IffFieldType.ImageResource or IffFieldType.Sound)?.Width
                ?? Math.Min(32, definition.MinimumRecordSize);
            definition = definition with { DefaultStringSize = Math.Max(1, inferred) };
        }
        return definition;
    }

    public static IffSchemaDefinition FromSchema(string fileName, string region, IffSchema schema)
    {
        IReadOnlyList<IffField> localFields = schema.LocalFields ?? schema.Fields.Where(field => !field.IsInherited).ToArray();
        return new(CurrentVersion, Path.GetFileName(fileName), region, schema.MinimumRecordSize, schema.IsEditable,
            localFields.Select(FromField).ToArray(), schema.DefaultStringSize,
            schema.Ui, schema.DefaultLongStringSize, schema.BaseReference, schema.DefaultRevision, schema.KeyField,
            schema.IsOpaque, schema.IsKeyless);
    }

    public static IffFieldDefinition FromField(IffField field) => new(
        field.Name, field.Offset, field.Width, field.Type, field.IsEditable,
        field.Encoding?.CodePage, field.Minimum, field.Maximum, field.BitMask, field.BitShift,
        field.IsVisible, field.Reference is null ? null : new IffFieldReferenceDefinition(
            field.Reference.TargetFile, field.Reference.TargetKeyField, field.Reference.DisplayField,
            field.Reference.IconField, field.Reference.PickerEnabled), field.IconPath, field.SoundPath);

    public static IffSchema ToSchema(IffSchemaDefinition definition, int recordSize, IffSchema? baseSchema = null)
    {
        ValidateDefinition(definition, recordSize);
        if (definition.Base is not null && baseSchema is null)
            throw new InvalidDataException($"Schema '{definition.FileName}' requires base '{definition.Base.Name}'.");
        IEnumerable<IffFieldDefinition> materializedDefinitions = definition.IsOpaque
            ? CreateOpaqueFields(recordSize)
            : definition.Fields;
        IffField[] localFields = materializedDefinitions.Select(field => new IffField(
            field.Name, field.Offset, field.Width, field.Type, field.IsEditable,
            field.EncodingCodePage is int codePage ? Encoding.GetEncoding(codePage) : null,
            field.Minimum, field.Maximum, field.BitMask, field.BitShift,
            field.IsVisible ?? !IsCatchAllRaw(field, recordSize),
            field.Reference is null ? null : new IffFieldReference(
                field.Reference.TargetFile, field.Reference.TargetKeyField, field.Reference.DisplayField,
                field.Reference.IconField, field.Reference.PickerEnabled),
            field.IconPath,
            field.SoundPath)).ToArray();
        IffField[] inheritedFields = baseSchema?.Fields
            .Where(field => !IsCatchAllRawField(field, baseSchema.MinimumRecordSize))
            .Select(field => field with { IsInherited = true })
            .ToArray() ?? [];
        ValidateComposition(definition, inheritedFields, localFields, recordSize);
        IffField[] fields = [.. inheritedFields, .. localFields];
        string? keyField = definition.IsKeyless ? null :
            definition.KeyField ?? baseSchema?.KeyField ?? InferKeyField(fields);
        if (keyField is not null && !fields.Any(field => field.Name.Equals(keyField, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException($"Schema '{definition.FileName}' identifies missing key field '{keyField}'.");
        return new IffSchema(Path.GetFileNameWithoutExtension(definition.FileName),
            definition.MinimumRecordSize, fields, definition.IsEditable, definition.DefaultStringSize, definition.Ui,
            definition.DefaultLongStringSize, definition.Base, localFields, definition.DefaultRevision, keyField,
            definition.IsOpaque, definition.IsKeyless);
    }

    private static IReadOnlyList<IffFieldDefinition> CreateOpaqueFields(int recordSize)
    {
        int firstWidth = Math.Max(1, recordSize / 2);
        if (firstWidth == recordSize)
            return [new IffFieldDefinition("Opaque data", 0, recordSize, IffFieldType.Raw, false)];
        return
        [
            new IffFieldDefinition("Opaque data 1", 0, firstWidth, IffFieldType.Raw, false),
            new IffFieldDefinition("Opaque data 2", firstWidth, recordSize - firstWidth, IffFieldType.Raw, false)
        ];
    }

    internal static IffSchema CompleteCoverage(IffSchema schema, int recordSize)
    {
        IffField[] fields = schema.Fields
            .Where(field => !IffSchemaCoverage.IsCatchAllRawRecord(field, recordSize))
            .ToArray();
        var represented = new bool[recordSize];
        foreach (IffField field in fields)
        {
            int end = Math.Min(recordSize, field.Offset + field.Width);
            for (int offset = Math.Max(0, field.Offset); offset < end; offset++)
                represented[offset] = true;
        }

        var complete = new List<IffField>(fields);
        int start = 0;
        while (start < represented.Length)
        {
            while (start < represented.Length && represented[start]) start++;
            if (start == represented.Length) break;
            int end = start + 1;
            while (end < represented.Length && !represented[end]) end++;
            complete.Add(new IffField($"Reserved 0x{start:X}", start, end - start, IffFieldType.Raw,
                IsEditable: false, IsVisible: false));
            start = end;
        }
        IffField[] completed = complete.OrderBy(field => field.Offset)
            .ThenBy(field => field.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        return schema with { Fields = completed };
    }

    private static string? InferKeyField(IReadOnlyList<IffField> fields)
    {
        string[] conventionalNames = ["ItemId", "TypeId", "Id", "Index", "_typeid"];
        return conventionalNames.FirstOrDefault(name => fields.Any(field =>
            field.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && field.Type is
                IffFieldType.Byte or IffFieldType.SByte or IffFieldType.UInt16 or IffFieldType.Int16 or
                IffFieldType.UInt32 or IffFieldType.Int32 or IffFieldType.UInt64 or IffFieldType.Int64 or
                IffFieldType.ItemIdReference));
    }

    public static void ValidateDefinition(IffSchemaDefinition definition, int recordSize)
    {
        if (definition.SchemaVersion is not (1 or CurrentVersion))
            throw new InvalidDataException($"Unsupported IFF schema version {definition.SchemaVersion}.");
        if (definition.DefaultRevision < 0)
            throw new InvalidDataException("An IFF default schema revision cannot be negative.");
        if (definition.IsKeyless && definition.KeyField is not null)
            throw new InvalidDataException("A keyless IFF schema cannot specify a key field.");
        if (definition.KeyField is { Length: 0 })
            throw new InvalidDataException("An IFF schema key field cannot be empty.");
        if (definition.SchemaVersion == 1 && definition.Base is not null)
            throw new InvalidDataException("IFF schema version 1 does not support a base schema.");
        if (definition.Base is { } baseReference &&
            (string.IsNullOrWhiteSpace(baseReference.Name) || Path.GetFileName(baseReference.Name) != baseReference.Name ||
             baseReference.Name.EndsWith(".iff", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("An IFF base schema name must be a simple logical name without an extension.");
        if (string.IsNullOrWhiteSpace(definition.FileName) || Path.GetFileName(definition.FileName) != definition.FileName)
            throw new InvalidDataException("An IFF schema must contain a filename without a directory path.");
        if (string.IsNullOrWhiteSpace(definition.Region))
            throw new InvalidDataException("An IFF schema must contain a region or '*'.");
        if (definition.MinimumRecordSize <= 0 || recordSize < definition.MinimumRecordSize)
            throw new InvalidDataException($"The schema requires records of at least {definition.MinimumRecordSize} bytes.");
        if (definition.DefaultStringSize <= 0 || definition.DefaultStringSize > recordSize)
            throw new InvalidDataException("The default string size must fit within the record.");
        if (definition.DefaultLongStringSize <= 0)
            throw new InvalidDataException("The default long string size must be positive.");
        if (definition.Fields is null || (definition.Fields.Count == 0 && definition.Base is null && !definition.IsOpaque))
            throw new InvalidDataException("An IFF schema must contain at least one local field or a base schema.");
        if (definition.IsOpaque && (definition.Fields.Count != 0 || definition.Base is not null || definition.KeyField is not null))
            throw new InvalidDataException("An opaque IFF schema cannot declare fields, a base schema, or a key field.");
        if (definition.Ui?.Tabs is { } tabs)
        {
            foreach (IffFormTabDefinition tab in tabs)
            {
                if (string.IsNullOrWhiteSpace(tab.Name))
                    throw new InvalidDataException("IFF form tabs must have a name.");
                foreach (IffFormFieldDefinition formField in tab.Fields)
                {
                    if (string.IsNullOrWhiteSpace(formField.Field))
                        throw new InvalidDataException("IFF form fields must reference a schema field.");
                }
            }
        }

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (IffFieldDefinition field in definition.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name) || !names.Add(field.Name))
                throw new InvalidDataException($"IFF schema field names must be non-empty and unique: '{field.Name}'.");
            if (field.Offset < 0 || field.Width <= 0 || field.Offset > recordSize - field.Width)
                throw new InvalidDataException($"Field '{field.Name}' exceeds the {recordSize}-byte record.");
            ValidateFieldShape(field);
            ValidatePathFields(field);
            ValidateReference(field);
            if (field.EncodingCodePage is int codePage)
                _ = Encoding.GetEncoding(codePage);
        }
    }

    private static void ValidateReference(IffFieldDefinition field)
    {
        if (field.Reference is not { } reference)
        {
            if (field.Type == IffFieldType.ItemIdReference)
                throw new InvalidDataException($"Field '{field.Name}' of type ItemIdReference must declare reference metadata.");
            return;
        }
        if (string.IsNullOrWhiteSpace(reference.TargetFile) ||
            Path.GetFileName(reference.TargetFile) != reference.TargetFile)
            throw new InvalidDataException($"Field '{field.Name}' reference target must be an IFF filename without a path.");
        if (string.IsNullOrWhiteSpace(reference.TargetKeyField))
            throw new InvalidDataException($"Field '{field.Name}' reference target key field is required.");
        if (string.IsNullOrWhiteSpace(reference.DisplayField))
            throw new InvalidDataException($"Field '{field.Name}' reference display field is required.");
        if (string.IsNullOrWhiteSpace(reference.IconField))
            throw new InvalidDataException($"Field '{field.Name}' reference icon field is required.");
    }

    private static void ValidatePathFields(IffFieldDefinition field)
    {
        if (!string.IsNullOrWhiteSpace(field.IconPath))
        {
            if (field.Type is not (IffFieldType.Icon or IffFieldType.ImageResource))
                throw new InvalidDataException($"Field '{field.Name}' icon path can only be set on image resource fields.");
            ValidateRelativeAssetPath(field.Name, field.IconPath, "icon");
        }
        if (!string.IsNullOrWhiteSpace(field.SoundPath))
        {
            if (field.Type != IffFieldType.Sound)
                throw new InvalidDataException($"Field '{field.Name}' sound path can only be set on Sound fields.");
            ValidateRelativeAssetPath(field.Name, field.SoundPath, "sound");
        }
    }

    private static void ValidateRelativeAssetPath(string fieldName, string relativePath, string assetKind)
    {
        if (Path.IsPathRooted(relativePath))
            throw new InvalidDataException($"Field '{fieldName}' {assetKind} path must be relative.");
        string normalized = relativePath.Replace('\\', '/');
        if (normalized.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(segment => segment == ".."))
            throw new InvalidDataException($"Field '{fieldName}' {assetKind} path must not traverse parent directories.");
        if (normalized.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new InvalidDataException($"Field '{fieldName}' {assetKind} path contains invalid characters.");
    }

    private static bool IsCatchAllRaw(IffFieldDefinition field, int recordSize) =>
        field.Type == IffFieldType.Raw && field.Offset == 0 && field.Width == recordSize &&
        field.Name.Equals("Raw record", StringComparison.OrdinalIgnoreCase);

    private static void ValidateFieldShape(IffFieldDefinition field)
    {
        int expectedWidth = field.Type switch
        {
            IffFieldType.Boolean or IffFieldType.Byte or IffFieldType.SByte => 1,
            IffFieldType.UInt16 or IffFieldType.Int16 => 2,
            IffFieldType.UInt32 or IffFieldType.ItemIdReference or IffFieldType.Int32 or IffFieldType.Single => 4,
            IffFieldType.Int64 or IffFieldType.UInt64 => 8,
            IffFieldType.DateTime => 16,
            _ => 0
        };
        if (expectedWidth != 0 && field.Width != expectedWidth)
            throw new InvalidDataException($"Field '{field.Name}' must occupy {expectedWidth} bytes.");
        if (field.Minimum is long minimum && field.Maximum is long maximum && minimum > maximum)
            throw new InvalidDataException($"Field '{field.Name}' has an invalid numeric range.");
        if (field.Type is IffFieldType.BitField or IffFieldType.BooleanBitField)
        {
            uint widthMask = field.Width switch
            {
                1 => byte.MaxValue,
                2 => ushort.MaxValue,
                3 => 0x00FF_FFFFu,
                4 => uint.MaxValue,
                _ => 0
            };
            if (widthMask == 0 || field.BitMask is not uint mask || mask == 0 || (mask & ~widthMask) != 0 ||
                field.BitShift is < 0 or > 31 || (mask >> field.BitShift) == 0 ||
                field.Type == IffFieldType.BooleanBitField && (mask & (mask - 1)) != 0)
                throw new InvalidDataException($"Field '{field.Name}' has an invalid bit mask.");
        }
        if (field.Type == IffFieldType.ZeroBoolean && field.Width is not (1 or 2 or 4))
            throw new InvalidDataException($"Zero-boolean field '{field.Name}' must occupy one, two, or four bytes.");
    }

    private static bool IsCatchAllRawField(IffField field, int recordSize) =>
        field.Type == IffFieldType.Raw && !field.IsEditable && field.Offset == 0 && field.Width == recordSize &&
        field.Name.Equals("Raw record", StringComparison.OrdinalIgnoreCase);

    private static void ValidateComposition(IffSchemaDefinition definition, IReadOnlyList<IffField> inherited,
        IReadOnlyList<IffField> local, int recordSize)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (IffField field in inherited.Concat(local))
            if (!names.Add(field.Name))
                throw new InvalidDataException($"Schema '{definition.FileName}' contains duplicate inherited/local field '{field.Name}'.");

        foreach (IffField localField in local)
        {
            if (IsCatchAllRawField(localField, definition.MinimumRecordSize) || localField.Type is IffFieldType.BitField or IffFieldType.BooleanBitField)
                continue;
            int localEnd = checked(localField.Offset + localField.Width);
            foreach (IffField baseField in inherited)
            {
                if (baseField.Type is IffFieldType.BitField or IffFieldType.BooleanBitField) continue;
                int baseEnd = checked(baseField.Offset + baseField.Width);
                if (localField.Offset < baseEnd && baseField.Offset < localEnd)
                    throw new InvalidDataException($"Local field '{localField.Name}' overlaps inherited field '{baseField.Name}'.");
            }
        }
    }
}

public sealed record IffSavedSchemaSource(
    string FileName,
    string SourcePath,
    string DestinationPath,
    string CandidateRegion,
    string Json,
    IffSchemaDefinition? Definition,
    string? Error,
    bool IsEmbedded = false);

public sealed class DirectoryIffSchemaProvider(string directoryPath, IIffSchemaProvider? fallbackProvider = null) : IIffSchemaProvider
{
    public string DirectoryPath { get; } = Path.GetFullPath(directoryPath);

    public IffSchemaResolution Resolve(string fileName, string region, int recordSize) =>
        Resolve(fileName, [region], recordSize);

    public IffSchemaResolution Resolve(string fileName, IReadOnlyList<string> regions, int recordSize)
        => ResolveCore(fileName, regions, recordSize, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    private IffSchemaResolution ResolveCore(string fileName, IReadOnlyList<string> regions, int recordSize,
        HashSet<string> resolving)
    {
        fileName = Path.GetFileName(fileName);
        foreach (string region in regions)
        {
            string candidate = GetSchemaPath(fileName, region);
            if (!File.Exists(candidate)) continue;
            try
            {
                IffSchemaDefinition definition = IffSchemaJson.Deserialize(File.ReadAllText(candidate));
                if (!definition.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                    !(definition.Region.Equals(region, StringComparison.OrdinalIgnoreCase) || definition.Region == "*"))
                    throw new InvalidDataException("The schema filename or region does not match its JSON content.");
                return Materialize(definition, regions, recordSize, resolving);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException
                or ArgumentException or NotSupportedException)
            {
                return new IffSchemaResolution(null, $"Could not load IFF schema '{candidate}': {ex.Message}");
            }
        }
        string defaultCandidate = GetSchemaPath(fileName, "*");
        if (File.Exists(defaultCandidate))
        {
            try
            {
                IffSchemaDefinition definition = IffSchemaJson.Deserialize(File.ReadAllText(defaultCandidate));
                if (!definition.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) || definition.Region != "*")
                    throw new InvalidDataException("The schema filename or region does not match its JSON content.");
                return Materialize(definition, regions, recordSize, resolving);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException
                or ArgumentException or NotSupportedException)
            {
                return new IffSchemaResolution(null, $"Could not load IFF schema '{defaultCandidate}': {ex.Message}");
            }
        }
        return fallbackProvider?.Resolve(fileName, regions, recordSize)
            ?? new IffSchemaResolution(null, $"No JSON schema is defined for {fileName} ({string.Join(", ", regions)}).");
    }

    private IffSchemaResolution Materialize(IffSchemaDefinition definition, IReadOnlyList<string> regions,
        int recordSize, HashSet<string> resolving)
    {
        if (definition.Base is null)
            return new IffSchemaResolution(IffSchemaJson.ToSchema(definition, recordSize));

        string key = $"{definition.FileName}|{definition.Region}";
        if (!resolving.Add(key))
            throw new InvalidDataException($"Circular IFF base schema reference detected at '{key}'.");
        try
        {
            IReadOnlyList<string> baseRegions = definition.Base.Region is { Length: > 0 } fixedRegion
                ? [fixedRegion]
                : regions;
            string baseFileName = definition.Base.Name + ".iff";
            IffSchemaResolution baseResolution = ResolveCore(baseFileName, baseRegions, recordSize, resolving);
            if (baseResolution.Schema is null)
                return new IffSchemaResolution(null, baseResolution.Warning ??
                    $"Could not resolve base schema '{definition.Base.Name}' for '{definition.FileName}'.");
            return new IffSchemaResolution(IffSchemaJson.ToSchema(definition, recordSize, baseResolution.Schema),
                baseResolution.Warning);
        }
        finally
        {
            resolving.Remove(key);
        }
    }

    public string GetSchemaPath(string fileName, string region) =>
        Path.Combine(DirectoryPath, $"{Path.GetFileNameWithoutExtension(fileName)}.{NormalizeRegion(region)}.json");

    public IffSavedSchemaSource? ReadSavedSource(string fileName, IReadOnlyList<string> regions,
        int? recordSize = null)
    {
        fileName = Path.GetFileName(fileName);
        foreach ((string region, string path) in CandidateSources(fileName, regions))
        {
            if (!File.Exists(path)) continue;
            IffSavedSchemaSource localFileSource = ReadSource(fileName, path, path, region, isEmbedded: false);
            return recordSize is int size ? AttachMaterializationError(localFileSource, regions, size) : localFileSource;
        }

        if (fallbackProvider is EmbeddedIffSchemaProvider embedded &&
            embedded.ReadSavedSource(fileName, regions) is { } source)
        {
            IffSavedSchemaSource localSource = source with
            {
                DestinationPath = GetSchemaPath(fileName, source.CandidateRegion),
                IsEmbedded = true
            };
            return recordSize is int size ? AttachMaterializationError(localSource, regions, size) : localSource;
        }
        return null;
    }

    public IffSchemaDefinition SaveJson(IffSavedSchemaSource source, string json,
        IReadOnlyList<string> regions, int recordSize)
    {
        ArgumentNullException.ThrowIfNull(source);
        IffSchemaDefinition definition = IffSchemaJson.Deserialize(json);
        ValidateSourceIdentity(definition, source.FileName, source.CandidateRegion);
        EnsureMaterializes(definition, regions, recordSize);
        WriteAtomically(source.DestinationPath, json);
        return definition;
    }

    public void SaveValidated(IffSchemaDefinition definition, IReadOnlyList<string> regions, int recordSize)
    {
        EnsureMaterializes(definition, regions, recordSize);
        Save(definition);
    }

    public void Save(IffSchemaDefinition definition)
    {
        IffSchemaJson.ValidateDefinition(definition, definition.MinimumRecordSize);
        string destination = GetSchemaPath(definition.FileName, definition.Region);
        WriteAtomically(destination, IffSchemaJson.Serialize(definition));
    }

    public IReadOnlyList<IffSchemaDefinition> LoadDefinitions()
    {
        if (!Directory.Exists(DirectoryPath)) return [];
        var definitions = new List<IffSchemaDefinition>();
        foreach (string path in Directory.EnumerateFiles(DirectoryPath, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
        {
            try { definitions.Add(IffSchemaJson.Deserialize(File.ReadAllText(path))); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException) { }
        }
        return definitions;
    }

    private IEnumerable<(string Region, string Path)> CandidateSources(string fileName,
        IReadOnlyList<string> regions)
    {
        foreach (string region in regions.Distinct(StringComparer.OrdinalIgnoreCase))
            yield return (region, GetSchemaPath(fileName, region));
        yield return ("*", GetSchemaPath(fileName, "*"));
    }

    private static IffSavedSchemaSource ReadSource(string fileName, string sourcePath, string destinationPath,
        string candidateRegion, bool isEmbedded, string? json = null)
    {
        try
        {
            json ??= File.ReadAllText(sourcePath);
            try
            {
                IffSchemaDefinition definition = IffSchemaJson.Deserialize(json);
                return new(fileName, sourcePath, destinationPath, candidateRegion, json, definition, null, isEmbedded);
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException or NotSupportedException)
            {
                return new(fileName, sourcePath, destinationPath, candidateRegion, json, null, ex.Message, isEmbedded);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new(fileName, sourcePath, destinationPath, candidateRegion, json ?? string.Empty, null, ex.Message, isEmbedded);
        }
    }

    private void EnsureMaterializes(IffSchemaDefinition definition, IReadOnlyList<string> regions, int recordSize)
    {
        IffSchemaResolution resolution = Materialize(definition, regions, recordSize,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        if (resolution.Schema is null)
            throw new InvalidDataException(resolution.Warning ?? $"Could not materialize schema '{definition.FileName}'.");
    }

    private IffSavedSchemaSource AttachMaterializationError(IffSavedSchemaSource source,
        IReadOnlyList<string> regions, int recordSize)
    {
        if (source.Definition is null) return source;
        try
        {
            ValidateSourceIdentity(source.Definition, source.FileName, source.CandidateRegion);
            EnsureMaterializes(source.Definition, regions, recordSize);
            return source;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or NotSupportedException)
        {
            return source with { Error = ex.Message };
        }
    }

    private static void ValidateSourceIdentity(IffSchemaDefinition definition, string expectedFileName,
        string candidateRegion)
    {
        if (!definition.FileName.Equals(expectedFileName, StringComparison.OrdinalIgnoreCase) ||
            !(definition.Region.Equals(candidateRegion, StringComparison.OrdinalIgnoreCase) || definition.Region == "*"))
            throw new InvalidDataException("The schema filename or region does not match the saved schema source.");
    }

    private static void WriteAtomically(string destination, string contents)
    {
        string? directory = Path.GetDirectoryName(destination);
        if (string.IsNullOrEmpty(directory)) throw new InvalidDataException("The schema destination is invalid.");
        Directory.CreateDirectory(directory);
        string temporary = destination + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, contents);
            File.Move(temporary, destination, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private static string NormalizeRegion(string region) => region == "*" ? "default" : region.ToUpperInvariant();
}

public sealed class EmbeddedIffSchemaProvider(Assembly? assembly = null, string resourcePrefix = "PangyaAPI.IFF.Schemas.Defaults") : IIffSchemaProvider
{
    private readonly Assembly _assembly = assembly ?? typeof(EmbeddedIffSchemaProvider).Assembly;

    public IffSchemaResolution Resolve(string fileName, string region, int recordSize) =>
        Resolve(fileName, [region], recordSize);

    public IffSchemaResolution Resolve(string fileName, IReadOnlyList<string> regions, int recordSize)
        => ResolveCore(fileName, regions, recordSize, new HashSet<string>(StringComparer.OrdinalIgnoreCase), true);

    private IffSchemaResolution ResolveCore(string fileName, IReadOnlyList<string> regions, int recordSize,
        HashSet<string> resolving, bool completeCoverage)
    {
        fileName = Path.GetFileName(fileName);
        string stem = Path.GetFileNameWithoutExtension(fileName);
        foreach (string suffix in regions.Select(region => $".{stem}.{region.ToUpperInvariant()}.json")
            .Append($".{stem}.default.json"))
        {
            string? resource = _assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (resource is null) continue;
            try
            {
                using Stream stream = _assembly.GetManifestResourceStream(resource)!;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                IffSchemaDefinition definition = IffSchemaJson.Deserialize(reader.ReadToEnd());
                if (definition.Base is null)
                {
                    IffSchema schema = IffSchemaJson.ToSchema(definition, recordSize);
                    return new IffSchemaResolution(completeCoverage
                        ? IffSchemaJson.CompleteCoverage(schema, recordSize)
                        : schema);
                }

                string key = $"{definition.FileName}|{definition.Region}";
                if (!resolving.Add(key))
                    throw new InvalidDataException($"Circular IFF base schema reference detected at '{key}'.");
                try
                {
                    IReadOnlyList<string> baseRegions = definition.Base.Region is { Length: > 0 } fixedRegion
                        ? [fixedRegion]
                        : regions;
                    IffSchemaResolution baseResolution = ResolveCore(definition.Base.Name + ".iff", baseRegions,
                        recordSize, resolving, false);
                    if (baseResolution.Schema is null)
                        return new IffSchemaResolution(null, baseResolution.Warning ??
                            $"Could not resolve base schema '{definition.Base.Name}' for '{definition.FileName}'.");
                    IffSchema schema = IffSchemaJson.ToSchema(definition, recordSize, baseResolution.Schema);
                    return new IffSchemaResolution(completeCoverage
                        ? IffSchemaJson.CompleteCoverage(schema, recordSize)
                        : schema, baseResolution.Warning);
                }
                finally
                {
                    resolving.Remove(key);
                }
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException or NotSupportedException)
            {
                return new IffSchemaResolution(null, $"Could not load embedded IFF schema '{resource}': {ex.Message}");
            }
        }
        return new IffSchemaResolution(null, $"No JSON schema is defined for {fileName} ({string.Join(", ", regions)}).");
    }

    public void SeedDirectory(string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (string resource in _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal)))
        {
            string marker = resourcePrefix + ".";
            string fileName = resource[(resource.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..];
            string destination = Path.Combine(destinationDirectory, fileName);
            if (File.Exists(destination)) continue;
            using Stream source = _assembly.GetManifestResourceStream(resource)!;
            using FileStream target = new(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            source.CopyTo(target);
        }
    }

    public IReadOnlyList<IffSchemaDefinition> LoadDefinitions()
    {
        var definitions = new List<IffSchemaDefinition>();
        foreach (string resource in _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix + ".", StringComparison.Ordinal))
            .Order(StringComparer.OrdinalIgnoreCase))
        {
            using Stream stream = _assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream, Encoding.UTF8);
            definitions.Add(IffSchemaJson.Deserialize(reader.ReadToEnd()));
        }
        return definitions;
    }

    public IffSavedSchemaSource? ReadSavedSource(string fileName, IReadOnlyList<string> regions)
    {
        fileName = Path.GetFileName(fileName);
        string stem = Path.GetFileNameWithoutExtension(fileName);
        IEnumerable<(string Region, string Suffix)> candidates = regions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(region => (region, $".{stem}.{region.ToUpperInvariant()}.json"))
            .Append(("*", $".{stem}.default.json"));
        foreach ((string region, string suffix) in candidates)
        {
            string? resource = _assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (resource is null) continue;
            try
            {
                using Stream stream = _assembly.GetManifestResourceStream(resource)!;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                string json = reader.ReadToEnd();
                try
                {
                    IffSchemaDefinition definition = IffSchemaJson.Deserialize(json);
                    return new(fileName, resource, string.Empty, region, json, definition, null, true);
                }
                catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException or NotSupportedException)
                {
                    return new(fileName, resource, string.Empty, region, json, null, ex.Message, true);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return new(fileName, resource, string.Empty, region, string.Empty, null, ex.Message, true);
            }
        }
        return null;
    }

    private static IEnumerable<string> CandidateSuffixes(string fileName, string region)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName);
        yield return $".{stem}.{region.ToUpperInvariant()}.json";
        yield return $".{stem}.default.json";
    }
}
