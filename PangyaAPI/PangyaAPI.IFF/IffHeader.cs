using System.Buffers.Binary;

namespace PangyaAPI.IFF;

public readonly record struct IffHeader(ushort RecordCount, ushort Revision, byte Magic, byte[] Reserved)
{
    public const int BinarySize = 8;

    public string Region => (Revision, Magic) switch
    {
        (0 or 32322, 11) => "TH",
        (30319 or 18 or 26998 or 0, 12) => "JP",
        _ => "Unknown"
    };

    internal static IffHeader Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length < BinarySize)
            throw new InvalidDataException("The IFF header is truncated.");

        return new IffHeader(
            BinaryPrimitives.ReadUInt16LittleEndian(bytes),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]),
            bytes[4],
            bytes[5..8].ToArray());
    }

    internal void Write(Span<byte> destination, ushort? count = null)
    {
        if (destination.Length < BinarySize)
            throw new ArgumentException("The destination is too small.", nameof(destination));
        if (Reserved is not { Length: 3 })
            throw new InvalidDataException("The IFF header must contain exactly three reserved bytes.");

        BinaryPrimitives.WriteUInt16LittleEndian(destination, count ?? RecordCount);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], Revision);
        destination[4] = Magic;
        Reserved.CopyTo(destination[5..]);
    }
}
