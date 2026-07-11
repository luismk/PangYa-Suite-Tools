namespace PangyaAPI.IFF;

public sealed class IffReader : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private bool _enumerated;
    public IffDocumentInfo Info { get; }

    private IffReader(Stream stream, string fileName, IffReaderOptions options)
    {
        if (!stream.CanRead || !stream.CanSeek) throw new ArgumentException("IFF input must be readable and seekable.", nameof(stream));
        if (stream.Length < IffHeader.BinarySize) throw new InvalidDataException("The IFF file is shorter than its header.");
        Span<byte> headerBytes = stackalloc byte[IffHeader.BinarySize];
        stream.Position = 0;
        stream.ReadExactly(headerBytes);
        IffHeader header = IffHeader.Parse(headerBytes);
        if (header.RecordCount > options.MaximumRecordCount) throw new InvalidDataException("The IFF record count exceeds the configured limit.");
        long payload = stream.Length - IffHeader.BinarySize;
        int size;
        if (header.RecordCount == 0)
        {
            if (payload != 0) throw new InvalidDataException("An IFF with zero records contains trailing data.");
            size = 0;
        }
        else
        {
            if (payload % header.RecordCount != 0) throw new InvalidDataException("The IFF payload is not divisible by its record count.");
            long computed = payload / header.RecordCount;
            if (computed <= 0 || computed > options.MaximumRecordSize) throw new InvalidDataException("The IFF record size is invalid or exceeds the configured limit.");
            size = checked((int)computed);
        }
        IffFormatProfile? profile = header.FormatProfile;
        string? fileNameRegion = IffRegionDetector.FromFileName(fileName) ?? options.FallbackSchemaRegion;
        IReadOnlyList<string> schemaRegions = options.SchemaRegion is { } explicitRegion
            ? [explicitRegion]
            : profile?.SchemaRegions ?? (fileNameRegion is null ? ["Unknown"] : [fileNameRegion]);
        string schemaRegion = options.SchemaRegion ?? profile?.Variant ?? fileNameRegion ?? "Unknown";
        IffSchemaResolution resolution = size == 0
            ? new IffSchemaResolution(null)
            : (options.SchemaProvider ?? new EmbeddedIffSchemaProvider()).Resolve(fileName, schemaRegions, size);
        IffSchema? schema = resolution.Schema;
        if (schema is not null && profile is not null)
            schema = schema with
            {
                DefaultStringSize = profile.DefaultStringSize,
                DefaultLongStringSize = profile.DefaultLongStringSize
            };
        if (size > 0 && schema is null)
        {
            IffField rawRecord = new("Raw record", 0, size, IffFieldType.Raw, false, IsVisible: false);
            schema = new IffSchema(Path.GetFileNameWithoutExtension(fileName), size, [rawRecord], false,
                profile?.DefaultStringSize ?? Math.Min(32, size), DefaultLongStringSize: profile?.DefaultLongStringSize ?? 512);
        }
        Info = new IffDocumentInfo(fileName, schemaRegion, size, schema, header, resolution.Warning);
        _stream = stream;
        _leaveOpen = options.LeaveOpen;
    }

    public static IffReader Open(Stream stream, string fileName, IffReaderOptions? options = null) => new(stream, fileName, options ?? new());

    public async IAsyncEnumerable<IffRecord> ReadRecordsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_enumerated) throw new InvalidOperationException("IFF records can only be streamed once by a reader.");
        _enumerated = true;
        _stream.Position = IffHeader.BinarySize;
        for (int index = 0; index < Info.Header.RecordCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            byte[] bytes = GC.AllocateUninitializedArray<byte>(Info.RecordSize);
            await _stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
            yield return new IffRecord(index, bytes, Info.Schema);
        }
        if (_stream.Position != _stream.Length) throw new InvalidDataException("Unexpected trailing IFF data.");
    }

    public void Dispose() { if (!_leaveOpen) _stream.Dispose(); }
    public ValueTask DisposeAsync() => _leaveOpen ? ValueTask.CompletedTask : _stream.DisposeAsync();
}
