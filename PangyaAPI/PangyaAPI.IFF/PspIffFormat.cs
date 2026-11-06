using System.Buffers.Binary;

namespace PangyaAPI.IFF;

internal static class PspIffFormat
{
    // EBOOT.BIN.dec readers 001c9c2c..001cade8 use these fixed strides.
    private static readonly IReadOnlyDictionary<string, int> RecordSizes =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Character"] = 0x160, ["Parts"] = 0x1cc, ["Club"] = 0xac,
            ["ClubSet"] = 0xb0, ["Ball"] = 0x2e4, ["Item"] = 0xac,
            ["Caddie"] = 0xb0, ["CaddieItem"] = 0xd0, ["SetItem"] = 0xac,
            ["Course"] = 0x11c, ["Title"] = 0x58, ["Skin"] = 0xac,
            ["HairStyle"] = 0x78, ["AuxPart"] = 0xc4, ["QuestDrop"] = 0xd8,
            ["Quest"] = 0xd8, ["Desc"] = 0x204
        };

    internal static void ValidateRecordSize(string fileName, int recordSize)
    {
        if (RecordSizes.TryGetValue(Path.GetFileNameWithoutExtension(fileName), out int expected) &&
            recordSize != expected)
            throw new InvalidDataException($"PSP {fileName} requires {expected}-byte records, not {recordSize}.");
    }

    internal static bool TryReadHeaderlessTitle(Stream stream, string fileName, int maximumCount,
        out IffHeader header)
    {
        header = default;
        const int size = 0x58;
        if (!Path.GetFileName(fileName).Equals("Title.iff", StringComparison.OrdinalIgnoreCase) ||
            stream.Length == 0 || stream.Length % size != 0)
            return false;

        // The shipped archive contains a bare Title array. Check every record's
        // enabled word and category-12 ID instead of interpreting its ID as a version.
        Span<byte> prefix = stackalloc byte[8];
        stream.Position = 0;
        stream.ReadExactly(prefix);
        if (BinaryPrimitives.ReadUInt32LittleEndian(prefix) > 1 ||
            BinaryPrimitives.ReadUInt32LittleEndian(prefix[4..]) >> 26 != 12)
            return false;
        if (stream.Length / size > Math.Min(maximumCount, int.MaxValue))
            throw new InvalidDataException("The PSP Title record count exceeds the configured limit.");
        int count = checked((int)(stream.Length / size));
        for (int index = 0; index < count; index++)
        {
            stream.Position = (long)index * size;
            stream.ReadExactly(prefix);
            if (BinaryPrimitives.ReadUInt32LittleEndian(prefix) > 1 ||
                BinaryPrimitives.ReadUInt32LittleEndian(prefix[4..]) >> 26 != 12)
                return false;
        }
        header = new IffHeader(count, 0, 1, [0, 0, 0]) { IsHeaderless = true };
        return true;
    }
}
