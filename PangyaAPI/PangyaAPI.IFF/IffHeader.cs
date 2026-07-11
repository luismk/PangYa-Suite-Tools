using System.Buffers.Binary;

namespace PangyaAPI.IFF;

public enum IffRegionRevision : ushort
{
    Default = 0, Global_30447 = 30447, Global_30087 = 30087, Global_30425 = 30425,
    Global_57 = 57, Japan = 548, Japan_52428 = 52428, Japan_8960 = 8960,
    Japan_30312 = 30312, Korea_30395 = 30395
}

public enum IffMagicNumber : byte { Season4 = 11, Season5 = 12, Season5_6_JP_GB = 13 }

public sealed record IffFormatProfile(string Variant, IReadOnlyList<string> SchemaRegions,
    int DefaultStringSize, int DefaultLongStringSize = 512);

public readonly record struct IffHeader(ushort RecordCount, ushort Revision, byte Magic, byte[] Reserved)
{
    public const int BinarySize = 8;

    public IffFormatProfile? FormatProfile => DetectFormatProfile(Revision, Magic);
    public string Region => FormatProfile?.Variant ?? "Unknown";

    public static IffFormatProfile? DetectFormatProfile(ushort revision, byte magic)
    {
        if (magic is not (11 or 12 or 13)) return null;
        if (magic == 11 && revision is 0 or 32322) return new("TH", ["TH"], 40);
        if (magic == 12 && revision is 30319 or 18 or 26998 or 0) return new("JP", ["JP"], 64);
        return revision switch
        {
            30447 => Global("Global_30447"), 30087 => Global("Global_30087"),
            30425 => Global("Global_30425"), 57 => Global("Global_57"),
            548 => Japan("Japan"), 52428 => Japan("Japan_52428"),
            8960 => Japan("Japan_8960"), 30312 => Japan("Japan_30312"),
            30395 => new("Korea_30395", ["Korea_30395", "KR"], 40), _ => null
        };
    }

    private static IffFormatProfile Global(string variant) => new(variant, [variant, "Global"], 40);
    private static IffFormatProfile Japan(string variant) => new(variant, [variant, "JP"], 64);

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
