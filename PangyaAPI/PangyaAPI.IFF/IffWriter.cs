using System.Buffers.Binary;

namespace PangyaAPI.IFF;

public static class IffWriter
{
    public static async Task WriteAsync(Stream destination, IffHeader header, IAsyncEnumerable<IffRecord> records, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite || !destination.CanSeek) throw new ArgumentException("IFF output must be writable and seekable.", nameof(destination));
        destination.SetLength(0);
        byte[] headerBytes = new byte[IffHeader.BinarySize];
        if (header.IsHeaderless && !header.IsPsp) throw new InvalidDataException("Only PSP tables support headerless output.");
        if (!header.IsHeaderless)
        {
            header.Write(headerBytes, 0);
            await destination.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        }
        int count = 0;
        int? recordSize = null;
        await foreach (IffRecord record in records.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (count == (header.IsPsp ? int.MaxValue : ushort.MaxValue)) throw new InvalidDataException("The IFF record count exceeds the format limit.");
            recordSize ??= record.Bytes.Length;
            if (header.IsHeaderless && record.Bytes.Length != 0x58)
                throw new InvalidDataException("Headerless PSP Title records must occupy 88 bytes.");
            if (header.IsHeaderless &&
                (BinaryPrimitives.ReadUInt32LittleEndian(record.Bytes.Span) > 1 ||
                 BinaryPrimitives.ReadUInt32LittleEndian(record.Bytes.Span[4..]) >> 26 != 12))
                throw new InvalidDataException("Headerless PSP Title records require an enabled word of 0 or 1 and a category-12 ID.");
            if (record.Bytes.Length != recordSize) throw new InvalidDataException("All IFF records must have the same size.");
            await destination.WriteAsync(record.Bytes, cancellationToken).ConfigureAwait(false);
            count++;
        }
        destination.Position = 0;
        if (header.IsHeaderless && count == 0)
            throw new InvalidDataException("A headerless PSP Title table must contain at least one record.");
        if (!header.IsHeaderless)
        {
            header.Write(headerBytes, count);
            await destination.WriteAsync(headerBytes, cancellationToken).ConfigureAwait(false);
        }
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
