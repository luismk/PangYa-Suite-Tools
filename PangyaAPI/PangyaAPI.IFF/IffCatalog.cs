using System.Collections.ObjectModel;

namespace PangyaAPI.IFF;

public sealed record IffCatalogOptions(
    string? Region = null,
    IReadOnlyCollection<string>? RequiredTables = null,
    IIffSchemaProvider? SchemaProvider = null,
    int MaximumRecordSize = 1024 * 1024,
    int MaximumRecordCount = ushort.MaxValue);

public sealed class IffTable
{
    private readonly IReadOnlyDictionary<ulong, IffRecord> _recordsByKey;

    internal IffTable(string fileName, IffDocumentInfo info, IReadOnlyList<IffRecord> records)
    {
        FileName = fileName;
        Info = info;
        Records = records;

        if (info.Schema?.KeyField is not { } keyField)
        {
            _recordsByKey = new ReadOnlyDictionary<ulong, IffRecord>(new Dictionary<ulong, IffRecord>());
            return;
        }

        var index = new Dictionary<ulong, IffRecord>();
        foreach (IffRecord record in records)
        {
            ulong key = ReadKey(record, keyField);
            if (!index.TryAdd(key, record))
                throw new InvalidDataException($"IFF table '{fileName}' contains duplicate key {key} in field '{keyField}'.");
        }
        _recordsByKey = new ReadOnlyDictionary<ulong, IffRecord>(index);
    }

    public string FileName { get; }
    public IffDocumentInfo Info { get; }
    public IReadOnlyList<IffRecord> Records { get; }
    public string? KeyField => Info.Schema?.KeyField;

    public bool TryGetRecord(uint key, out IffRecord? record) => _recordsByKey.TryGetValue(key, out record);

    public IffRecord? Find(uint key) => TryGetRecord(key, out IffRecord? record) ? record : null;

    public IffRecord GetRequired(uint key) => Find(key)
        ?? throw new KeyNotFoundException($"IFF table '{FileName}' does not contain key {key}.");

    private static ulong ReadKey(IffRecord record, string keyField)
    {
        object? value = record.GetValue(keyField);
        return value switch
        {
            byte number => number,
            sbyte number when number >= 0 => (ulong)number,
            ushort number => number,
            short number when number >= 0 => (ulong)number,
            uint number => number,
            int number when number >= 0 => (ulong)number,
            ulong number => number,
            long number when number >= 0 => (ulong)number,
            _ => throw new InvalidDataException($"IFF key field '{keyField}' must contain a non-negative integer.")
        };
    }
}

public sealed class IffCatalog
{
    private readonly IReadOnlyDictionary<string, IffTable> _tables;

    private IffCatalog(string sourcePath, string region, IReadOnlyDictionary<string, IffTable> tables)
    {
        SourcePath = sourcePath;
        Region = region;
        _tables = tables;
    }

    public string SourcePath { get; }
    public string Region { get; }
    public IReadOnlyCollection<IffTable> Tables => _tables.Values.Distinct().ToArray();

    public static async Task<IffCatalog> LoadAsync(string path, IffCatalogOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        options ??= new();
        string fullPath = Path.GetFullPath(path);
        await using IffContainer container = await IffContainer.OpenAsync(fullPath, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        string? configuredRegion = string.IsNullOrWhiteSpace(options.Region) ? null : options.Region.Trim();
        string? catalogRegion = configuredRegion;
        var tables = new Dictionary<string, IffTable>(StringComparer.OrdinalIgnoreCase);

        foreach (IffContainerEntry entry in container.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using Stream stream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using IffReader reader = IffReader.Open(stream, Path.GetFileName(entry.Name), new IffReaderOptions(
                options.MaximumRecordSize, options.MaximumRecordCount, LeaveOpen: true, options.SchemaProvider,
                configuredRegion, container.FileNameRegion));
            if (reader.Info.Header.RecordCount > 0 &&
                (reader.Info.Schema is null || reader.Info.SchemaWarning is not null &&
                    reader.Info.Schema.Fields.Any(field => IffSchemaCoverage.IsCatchAllRawRecord(field, reader.Info.RecordSize))))
                throw new InvalidDataException(reader.Info.SchemaWarning ?? $"No schema is defined for '{entry.Name}'.");

            var records = new List<IffRecord>(reader.Info.Header.RecordCount);
            await foreach (IffRecord record in reader.ReadRecordsAsync(cancellationToken).ConfigureAwait(false))
                records.Add(record);

            var table = new IffTable(Path.GetFileName(entry.Name), reader.Info, records.AsReadOnly());
            AddTableAlias(tables, table.FileName, table);
            AddTableAlias(tables, Path.GetFileNameWithoutExtension(table.FileName), table);
            string? detectedRegion = reader.Info.Region == "Unknown" ? null : reader.Info.Region;
            if (catalogRegion is null)
                catalogRegion = detectedRegion;
            else if (configuredRegion is null && detectedRegion is not null &&
                !catalogRegion.Equals(detectedRegion, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"IFF container mixes region profiles '{catalogRegion}' and '{detectedRegion}'.");
        }

        foreach (string required in options.RequiredTables ?? [])
        {
            if (!tables.ContainsKey(NormalizeTableName(required)))
                throw new InvalidDataException($"Required IFF table '{required}' is missing from '{fullPath}'.");
        }

        return new IffCatalog(fullPath, catalogRegion ?? "Unknown",
            new ReadOnlyDictionary<string, IffTable>(tables));
    }

    public bool TryGetTable(string name, out IffTable? table)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _tables.TryGetValue(NormalizeTableName(name), out table);
    }

    public IffTable GetTable(string name) => TryGetTable(name, out IffTable? table)
        ? table!
        : throw new KeyNotFoundException($"IFF catalog does not contain table '{name}'.");

    public IffRecord? Find(string tableName, uint key) => GetTable(tableName).Find(key);

    private static void AddTableAlias(IDictionary<string, IffTable> tables, string name, IffTable table)
    {
        string key = NormalizeTableName(name);
        if (!tables.TryAdd(key, table) && !ReferenceEquals(tables[key], table))
            throw new InvalidDataException($"IFF container contains duplicate table '{name}'.");
    }

    private static string NormalizeTableName(string name) =>
        Path.GetFileNameWithoutExtension(name.Trim());
}
