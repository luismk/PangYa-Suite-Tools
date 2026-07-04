namespace PangyaAPI.IFF;

public static class IffWriter
{
    public static async Task WriteAsync(Stream destination, IffHeader header, IAsyncEnumerable<IffRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite || !destination.CanSeek) throw new ArgumentException("IFF output must be writable and seekable.", nameof(destination));
        destination.SetLength(0);
        byte[] headerBytes = new byte[IffHeader.BinarySize];
        header.Write(headerBytes, 0);
        await destination.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        int count = 0;
        int? recordSize = null;
        await foreach (IffRecord record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (count == ushort.MaxValue) throw new InvalidDataException("IFF files cannot contain more than 65535 records.");
            recordSize ??= record.Bytes.Length;
            if (record.Bytes.Length != recordSize) throw new InvalidDataException("All IFF records must have the same size.");
            await destination.WriteAsync(record.Bytes, cancellationToken).ConfigureAwait(false);
            count++;
        }
        destination.Position = 0;
        header.Write(headerBytes, checked((ushort)count));
        await destination.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
