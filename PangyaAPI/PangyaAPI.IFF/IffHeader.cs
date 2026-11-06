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

public readonly record struct IffHeader(int RecordCount, ushort Revision, byte Magic, byte[] Reserved)
{
    public const int BinarySize = 8;
    public const string PspRegion = "PSP";
    public bool IsPsp => Magic == 1 && Reserved is [0, 0, 0];
    // Some shipped PSP Title tables consist only of 0x58-byte records.
    public bool IsHeaderless { get; init; }
    public int DataOffset => IsHeaderless ? 0 : BinarySize;
    internal static IffFormatProfile PspProfile => new(PspRegion, [PspRegion], 40);
    public const string Japan983Region = "Japan_983";
    internal static IffFormatProfile Japan983Profile => Japan(Japan983Region);

    public IffFormatProfile? FormatProfile => ResolveFormatProfile();
    public string Region => FormatProfile?.Variant ?? "Unknown";

    public IffFormatProfile? ResolveFormatProfile(string? regionHint = null)
    {
        if (IsPsp) return PspProfile;
        IffFormatProfile? profile = DetectFormatProfile(Revision, Magic);
        // JP 983 stores a 32-bit version. Its linkage can also be 57, shared with Global.
        bool japanHint = string.Equals(regionHint, "JP", StringComparison.OrdinalIgnoreCase);
        if (Magic == 13 && Reserved is [0, 0, 0] &&
            ((Revision == 0 && regionHint is null) ||
                (japanHint && (profile is null || Revision == 57))))
            return Japan983Profile;
        return profile;
    }

    public static IReadOnlyList<string> GetSchemaRegions(string region) =>
        region.Equals(Japan983Region, StringComparison.OrdinalIgnoreCase) ? [Japan983Region, "JP"] : [region];

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

        if (BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..]) == 1)
        {
            uint count = BinaryPrimitives.ReadUInt32LittleEndian(bytes);
            if (count > int.MaxValue) throw new InvalidDataException("The PSP IFF record count is too large.");
            return new IffHeader((int)count, 0, 1, [0, 0, 0]);
        }
        return new IffHeader(
            BinaryPrimitives.ReadUInt16LittleEndian(bytes),
            BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..]),
            bytes[4],
            bytes[5..8].ToArray());
    }

    internal void Write(Span<byte> destination, int? count = null)
    {
        if (destination.Length < BinarySize)
            throw new ArgumentException("The destination is too small.", nameof(destination));
        if (Reserved is not { Length: 3 })
            throw new InvalidDataException("The IFF header must contain exactly three reserved bytes.");

        int recordCount = count ?? RecordCount;
        if (recordCount < 0) throw new InvalidDataException("The IFF record count cannot be negative.");
        if (IsPsp)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)recordCount);
            BinaryPrimitives.WriteUInt32LittleEndian(destination[4..], 1);
            return;
        }
        if (recordCount > ushort.MaxValue) throw new InvalidDataException("PC IFF files cannot contain more than 65535 records.");
        BinaryPrimitives.WriteUInt16LittleEndian(destination, (ushort)recordCount);
        BinaryPrimitives.WriteUInt16LittleEndian(destination[2..], Revision);
        destination[4] = Magic;
        Reserved.CopyTo(destination[5..]);
    }
}
