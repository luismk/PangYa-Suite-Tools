using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Shop;
using PangyaAPI.IFF;
using PangyaAPI.PAK.Models;
using System.Globalization;
using System.Text;

namespace PangYa_Suite_Tools;

internal sealed record IffReferenceCatalogItem(
    uint Key,
    string Name,
    string IconId,
    string? IconPath,
    string TargetFile,
    IffItemIdTableRow? ItemIdInfo = null);

internal sealed record IffReferenceDisplay(
    IffField Field,
    uint Key,
    string TargetFile,
    string Name,
    string IconId,
    string? IconPath,
    bool MissingRecord,
    bool MissingIcon,
    IffItemIdTableRow? ItemIdInfo = null);

internal sealed record IffItemIdTableRow(
    string SourceFile,
    uint ItemId,
    uint IffType,
    uint CharacterSerial,
    uint Position,
    uint Group,
    uint Type,
    uint Serial,
    string DisplayName,
    string IconId,
    string? IconPath,
    string? CharacterName)
{
    public string Summary
    {
        get
        {
            string character = string.IsNullOrWhiteSpace(CharacterName)
                ? CharacterSerial.ToString(CultureInfo.CurrentCulture)
                : $"{CharacterName} ({CharacterSerial.ToString(CultureInfo.CurrentCulture)})";
            return string.Format(CultureInfo.CurrentCulture,
                "{0} | Type {1} | Character {2} | Serial {3}",
                SourceFile, Type, character, Serial);
        }
    }
}

internal interface IIffReferenceResolver
{
    string? DataRoot { get; }
    IReadOnlyList<IffReferenceCatalogItem> GetCatalog(IffField field);
    IffReferenceDisplay Resolve(IffField field, object? value);
    string? TryResolveIconPath(IffField? field, string iconId);
}

internal sealed class IffReferenceResolver : IIffReferenceResolver
{
    private static readonly string[] ImageExtensions = [".tga", ".png", ".jpg", ".bmp"];

    private sealed record ReferenceIndex(
        IffFieldReference Reference,
        IReadOnlyDictionary<uint, IffReferenceCatalogItem> Items,
        IReadOnlyList<IffReferenceCatalogItem> Catalog);

    private readonly Dictionary<string, ReferenceIndex> _indexes;
    private readonly ShopAssetResolver? _assets;

    private IffReferenceResolver(Dictionary<string, ReferenceIndex> indexes, string? dataRoot, ShopAssetResolver? assets) =>
        (_indexes, DataRoot, _assets) = (indexes, dataRoot, assets);

    public string? DataRoot { get; }

    public static bool Supports(IffDocumentInfo? document) =>
        document?.Schema?.Fields.Any(field => field.Reference is not null ||
            field.Type == IffFieldType.Icon ||
            field.Type == IffFieldType.Sound ||
            field.Name.Equals("Icon", StringComparison.OrdinalIgnoreCase)) == true;

    public static async Task<IIffReferenceResolver?> CreateAsync(
        IffDocumentInfo? document,
        IffContainer? currentContainer,
        string? iffDirectoryPath,
        string sourcePath,
        string region,
        Encoding encoding,
        IIffSchemaProvider schemaProvider,
        CancellationToken cancellationToken,
        string? dataRootOverride = null)
    {
        if (document?.Schema is null) return null;
        IffFieldReference[] references = document.Schema.Fields
            .Select(field => field.Reference)
            .OfType<IffFieldReference>()
            .GroupBy(reference => IndexKey(reference), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        string? manualDataRoot = NormalizeDataRoot(dataRootOverride);
        string? dataRoot = manualDataRoot ?? DetectDataRoot(sourcePath);
        ShopAssetResolver? assets = TryCreateAssetResolver(dataRoot);
        var indexes = new Dictionary<string, ReferenceIndex>(StringComparer.OrdinalIgnoreCase);
        foreach (IffFieldReference reference in references)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReferenceIndex? index = await LoadIndexAsync(reference, currentContainer, iffDirectoryPath, sourcePath,
                region, encoding, schemaProvider, dataRoot, manualDataRoot is not null, assets, cancellationToken);
            if (index is not null) indexes[IndexKey(reference)] = index;
            else indexes[IndexKey(reference)] = new ReferenceIndex(reference,
                new Dictionary<uint, IffReferenceCatalogItem>(), []);
        }

        return new IffReferenceResolver(indexes, dataRoot, assets);
    }

    public IReadOnlyList<IffReferenceCatalogItem> GetCatalog(IffField field) =>
        TryGetIndex(field.Reference, out ReferenceIndex? index) && index is not null ? index.Catalog : [];

    public IffReferenceDisplay Resolve(IffField field, object? value)
    {
        uint key = ConvertReferenceKey(value);
        string targetFile = field.Reference?.TargetFile ?? string.Empty;
        if (key != 0 && TryGetIndex(field.Reference, out ReferenceIndex? index) && index is not null &&
            index.Items.TryGetValue(key, out IffReferenceCatalogItem? item))
        {
            return new IffReferenceDisplay(field, key, item.TargetFile, item.Name, item.IconId, item.IconPath,
                MissingRecord: false, MissingIcon: item.IconId.Length > 0 && item.IconPath is null,
                item.ItemIdInfo);
        }

        string name = key == 0
            ? string.Empty
            : string.Format(CultureInfo.CurrentCulture, Strings.IFFManager_ReferenceMissingRecordFormat, key);
        return new IffReferenceDisplay(field, key, targetFile, name, string.Empty, null,
            MissingRecord: key != 0, MissingIcon: key != 0);
    }

    public string? TryResolveIconPath(IffField? field, string iconId) =>
        ResolveIconPath(field, iconId, DataRoot, _assets);

    private bool TryGetIndex(IffFieldReference? reference, out ReferenceIndex? index)
    {
        index = null;
        return reference is not null && _indexes.TryGetValue(IndexKey(reference), out index);
    }

    private static async Task<ReferenceIndex?> LoadIndexAsync(
        IffFieldReference reference,
        IffContainer? currentContainer,
        string? iffDirectoryPath,
        string sourcePath,
        string region,
        Encoding encoding,
        IIffSchemaProvider schemaProvider,
        string? dataRoot,
        bool dataRootIsManual,
        ShopAssetResolver? assets,
        CancellationToken cancellationToken)
    {
        if (TryFindEntry(currentContainer, reference.TargetFile, out IffContainerEntry? archiveEntry) &&
            archiveEntry is not null)
        {
            return await CreateFromEntryAsync(reference, archiveEntry, region, encoding, schemaProvider, dataRoot, assets,
                await LoadCharacterSerialNamesAsync(currentContainer, iffDirectoryPath, sourcePath, dataRoot, dataRootIsManual,
                    region, encoding, schemaProvider, assets, cancellationToken), cancellationToken);
        }

        string? loosePath = FindLooseIffPath(reference.TargetFile, iffDirectoryPath, sourcePath, dataRoot);
        if (loosePath is null) return null;

        string looseDataRoot = (dataRootIsManual ? dataRoot : DetectDataRoot(loosePath) ?? dataRoot) ?? string.Empty;
        ShopAssetResolver? looseAssets = looseDataRoot.Length == 0 ? assets : TryCreateAssetResolver(looseDataRoot) ?? assets;
        await using IffContainer container = await IffContainer.OpenAsync(loosePath, cancellationToken: cancellationToken);
        IffContainerEntry? entry = container.Entries.FirstOrDefault(candidate =>
            candidate.Name.Equals(reference.TargetFile, StringComparison.OrdinalIgnoreCase));
        return entry is null
            ? null
            : await CreateFromEntryAsync(reference, entry, region, encoding, schemaProvider, looseDataRoot, looseAssets,
                await LoadCharacterSerialNamesAsync(currentContainer, iffDirectoryPath, sourcePath, dataRoot, dataRootIsManual,
                    region, encoding, schemaProvider, assets, cancellationToken),
                cancellationToken);
    }

    private static async Task<ReferenceIndex?> CreateFromEntryAsync(
        IffFieldReference reference,
        IffContainerEntry entry,
        string region,
        Encoding encoding,
        IIffSchemaProvider schemaProvider,
        string? dataRoot,
        ShopAssetResolver? assets,
        IReadOnlyDictionary<uint, string> characterNames,
        CancellationToken cancellationToken)
    {
        var items = new Dictionary<uint, IffReferenceCatalogItem>();
        await using Stream stream = await entry.OpenAsync(cancellationToken);
        await using IffReader reader = IffReader.Open(stream, Path.GetFileName(entry.Name),
            new(LeaveOpen: true, SchemaProvider: schemaProvider, SchemaRegion: region));
        IffSchema? schema = reader.Info.Schema;
        IffField? keyField = schema is null ? null : FindField(schema, reference.TargetKeyField);
        if (schema is null || keyField is null) return null;

        IffField? displayField = FindField(schema, reference.DisplayField);
        IffField? iconField = FindField(schema, reference.IconField);
        await foreach (IffRecord record in reader.ReadRecordsAsync(cancellationToken))
        {
            uint key = ConvertReferenceKey(keyField.GetValue(record.Bytes.Span, encoding));
            if (key == 0 || items.ContainsKey(key)) continue;
            string name = displayField is not null
                ? Convert.ToString(displayField.GetValue(record.Bytes.Span, encoding), CultureInfo.CurrentCulture)?.Trim() ?? string.Empty
                : string.Empty;
            string icon = iconField is not null
                ? Convert.ToString(iconField.GetValue(record.Bytes.Span, encoding), CultureInfo.CurrentCulture)?.Trim() ?? string.Empty
                : string.Empty;
            string? iconPath = icon.Length == 0 ? null : ResolveIconPath(iconField, icon, dataRoot, assets);
            IffItemIdTableRow? itemIdInfo = TryCreateItemIdRow(schema, record, encoding, reference.TargetFile,
                name, icon, iconPath, characterNames);
            items.Add(key, new IffReferenceCatalogItem(key,
                name.Length == 0 ? key.ToString(CultureInfo.CurrentCulture) : name,
                icon, iconPath, reference.TargetFile, itemIdInfo));
        }

        IffReferenceCatalogItem[] catalog = items.Values
            .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.Key)
            .ToArray();
        return new ReferenceIndex(reference, items, catalog);
    }

    private static IffField? FindField(IffSchema schema, string name) =>
        schema.Fields.FirstOrDefault(field => field.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    internal static IffItemIdTableRow? TryCreateItemIdRow(IffSchema schema, IffRecord record, Encoding encoding,
        string sourceFile, string displayName, string iconId, string? iconPath,
        IReadOnlyDictionary<uint, string>? characterNames = null)
    {
        IffField? itemIdField = FindField(schema, "ItemId");
        IffField? iffTypeField = FindField(schema, "IFF Type");
        IffField? characterSerialField = FindField(schema, "Character Serial");
        IffField? positionField = FindField(schema, "Position");
        IffField? groupField = FindField(schema, "Group");
        IffField? typeField = FindField(schema, "Type");
        IffField? serialField = FindField(schema, "Serial");
        if (itemIdField is null || iffTypeField is null || characterSerialField is null ||
            positionField is null || groupField is null || typeField is null || serialField is null)
            return null;

        uint itemId = ConvertReferenceKey(itemIdField.GetValue(record.Bytes.Span, encoding));
        uint characterSerial = ConvertReferenceKey(characterSerialField.GetValue(record.Bytes.Span, encoding));
        string? characterName = null;
        characterNames?.TryGetValue(characterSerial, out characterName);
        return new IffItemIdTableRow(sourceFile, itemId,
            ConvertReferenceKey(iffTypeField.GetValue(record.Bytes.Span, encoding)),
            characterSerial,
            ConvertReferenceKey(positionField.GetValue(record.Bytes.Span, encoding)),
            ConvertReferenceKey(groupField.GetValue(record.Bytes.Span, encoding)),
            ConvertReferenceKey(typeField.GetValue(record.Bytes.Span, encoding)),
            ConvertReferenceKey(serialField.GetValue(record.Bytes.Span, encoding)),
            displayName, iconId, iconPath, characterName);
    }

    private static async Task<IReadOnlyDictionary<uint, string>> LoadCharacterSerialNamesAsync(
        IffContainer? currentContainer,
        string? iffDirectoryPath,
        string sourcePath,
        string? dataRoot,
        bool dataRootIsManual,
        string region,
        Encoding encoding,
        IIffSchemaProvider schemaProvider,
        ShopAssetResolver? assets,
        CancellationToken cancellationToken)
    {
        const string characterFile = "Character.iff";
        if (TryFindEntry(currentContainer, characterFile, out IffContainerEntry? archiveEntry) && archiveEntry is not null)
            return await ReadCharacterSerialNamesAsync(archiveEntry, region, encoding, schemaProvider, cancellationToken);

        string? loosePath = FindLooseIffPath(characterFile, iffDirectoryPath, sourcePath, dataRoot);
        if (loosePath is null) return new Dictionary<uint, string>();
        string looseDataRoot = (dataRootIsManual ? dataRoot : DetectDataRoot(loosePath) ?? dataRoot) ?? string.Empty;
        ShopAssetResolver? looseAssets = looseDataRoot.Length == 0 ? assets : TryCreateAssetResolver(looseDataRoot) ?? assets;
        _ = looseAssets;
        await using IffContainer container = await IffContainer.OpenAsync(loosePath, cancellationToken: cancellationToken);
        IffContainerEntry? entry = container.Entries.FirstOrDefault(candidate =>
            candidate.Name.Equals(characterFile, StringComparison.OrdinalIgnoreCase));
        return entry is null
            ? new Dictionary<uint, string>()
            : await ReadCharacterSerialNamesAsync(entry, region, encoding, schemaProvider, cancellationToken);
    }

    private static async Task<IReadOnlyDictionary<uint, string>> ReadCharacterSerialNamesAsync(
        IffContainerEntry entry,
        string region,
        Encoding encoding,
        IIffSchemaProvider schemaProvider,
        CancellationToken cancellationToken)
    {
        var names = new Dictionary<uint, string>();
        await using Stream stream = await entry.OpenAsync(cancellationToken);
        await using IffReader reader = IffReader.Open(stream, Path.GetFileName(entry.Name),
            new(LeaveOpen: true, SchemaProvider: schemaProvider, SchemaRegion: region));
        IffSchema? schema = reader.Info.Schema;
        IffField? serialField = schema is null ? null : FindField(schema, "Serial");
        IffField? nameField = schema is null ? null : FindField(schema, "Name");
        if (schema is null || serialField is null || nameField is null) return names;
        await foreach (IffRecord record in reader.ReadRecordsAsync(cancellationToken))
        {
            uint serial = ConvertReferenceKey(serialField.GetValue(record.Bytes.Span, encoding));
            if (serial == 0 || names.ContainsKey(serial)) continue;
            string name = Convert.ToString(nameField.GetValue(record.Bytes.Span, encoding), CultureInfo.CurrentCulture)?.Trim() ?? string.Empty;
            if (name.Length > 0) names.Add(serial, name);
        }
        return names;
    }

    internal static uint ConvertReferenceKey(object? value)
    {
        try { return Convert.ToUInt32(value, CultureInfo.InvariantCulture); }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { return 0; }
    }

    private static bool TryFindEntry(IffContainer? container, string targetFile, out IffContainerEntry? entry)
    {
        entry = container?.Entries.FirstOrDefault(candidate =>
            candidate.Name.Equals(targetFile, StringComparison.OrdinalIgnoreCase));
        return entry is not null;
    }

    private static string? FindLooseIffPath(string targetFile, string? iffDirectoryPath, string sourcePath, string? dataRoot)
    {
        string? sidecarDataRoot = TryResolveDataRootFromPakSidecar(sourcePath);
        string[] candidates =
        [
            iffDirectoryPath is null ? string.Empty : Path.Combine(iffDirectoryPath, targetFile),
            dataRoot is null ? string.Empty : Path.Combine(dataRoot, targetFile),
            sidecarDataRoot is null ? string.Empty : Path.Combine(sidecarDataRoot, targetFile),
            Directory.Exists(sourcePath) ? Path.Combine(sourcePath, targetFile) : string.Empty,
            File.Exists(sourcePath) ? Path.Combine(Path.GetDirectoryName(sourcePath) ?? string.Empty, targetFile) : string.Empty
        ];
        return candidates.FirstOrDefault(path => path.Length > 0 && File.Exists(path));
    }

    private static string IndexKey(IffFieldReference reference) =>
        string.Join('\u001f', reference.TargetFile, reference.TargetKeyField, reference.DisplayField,
            reference.IconField);

    private static string? ResolveIconPath(IffField? field, string iconId, string? dataRoot, ShopAssetResolver? assets)
    {
        if (string.IsNullOrWhiteSpace(iconId)) return null;
        if (field?.Type == IffFieldType.Icon && !string.IsNullOrWhiteSpace(field.IconPath) && dataRoot is not null)
        {
            string? relativeMatch = TryResolveRelativeIconPath(dataRoot, field.IconPath, iconId);
            if (relativeMatch is not null) return relativeMatch;
        }

        return assets?.TryResolve(Path.GetFileNameWithoutExtension(iconId));
    }

    private static string? TryResolveRelativeIconPath(string dataRoot, string relativeIconPath, string iconId)
    {
        string root = Path.GetFullPath(dataRoot);
        string folder = Path.GetFullPath(Path.Combine(root, relativeIconPath));
        if (!IsInsideRoot(root, folder) || !Directory.Exists(folder)) return null;

        string fileName = Path.GetFileName(iconId);
        string stem = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        string[] candidates = extension.Length > 0
            ? [Path.Combine(folder, fileName)]
            : ImageExtensions.Select(candidateExtension => Path.Combine(folder, stem + candidateExtension)).ToArray();
        return candidates.FirstOrDefault(File.Exists);
    }

    private static bool IsInsideRoot(string root, string path)
    {
        string normalizedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        string normalizedPath = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
            Path.DirectorySeparatorChar;
        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static ShopAssetResolver? TryCreateAssetResolver(string? dataRoot)
    {
        if (dataRoot is null) return null;
        try { return new ShopAssetResolver(dataRoot); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { return null; }
    }

    private static string? NormalizeDataRoot(string? dataRoot)
    {
        if (string.IsNullOrWhiteSpace(dataRoot) || !Directory.Exists(dataRoot)) return null;
        return Path.GetFullPath(dataRoot);
    }

    private static string? DetectDataRoot(string sourcePath)
    {
        string? sidecarDataRoot = TryResolveDataRootFromPakSidecar(sourcePath);
        if (sidecarDataRoot is not null) return sidecarDataRoot;

        string? start = Directory.Exists(sourcePath) ? sourcePath : Path.GetDirectoryName(sourcePath);
        for (DirectoryInfo? current = start is null ? null : new DirectoryInfo(start);
             current is not null;
             current = current.Parent)
        {
            if (ContainsKnownAssets(current.FullName)) return current.FullName;
        }
        return null;
    }

    private static bool ContainsKnownAssets(string directory)
    {
        try
        {
            string[] directPatterns = ["*.tga", "*.png", "*.jpg", "*.bmp", "*.wav"];
            if (directPatterns.Any(pattern => Directory.EnumerateFiles(directory, pattern, SearchOption.TopDirectoryOnly).Any()))
                return true;
            string[] likelyDirectories = ["ui", "sound", "data\\ui", "data\\sound", "pangya\\ui", "pangya\\sound"];
            return likelyDirectories.Any(candidate => Directory.Exists(Path.Combine(directory, candidate)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            return false;
        }
    }

    internal static string? TryResolveDataRootFromPakSidecar(string sourcePath)
    {
        if (!File.Exists(sourcePath)) return null;
        string? root = PakExtractionSidecar.TryResolveExtractionRoot(sourcePath);
        return root is not null && Directory.Exists(root) ? root : null;
    }
}

internal static class IffPreviewImageLoader
{
    public static Image? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        try
        {
            if (path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)) return TgaDecoder.Load(path);
            using Image source = Image.FromFile(path);
            return new Bitmap(source);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
        {
            return null;
        }
    }
}
