using PangyaAPI.WFT;
using System.Buffers.Binary;

namespace PangyaAPI.Tests;

public sealed class WftTrueTypeExporterTests : IDisposable
{
    private readonly TemporaryDirectory _directory = new();

    [Fact]
    public void Export_WritesDeterministicChecksummedTrueTypeTables()
    {
        string path = CreateFont(WftCoverageMode.Monochrome, 2,
            (0x0020, [0, 0], 1), (0x0041, [0x80, 0x40], 2));
        using WftFont font = WftFontReader.Open(path);
        var options = new WftTrueTypeExportOptions("Test Pixels");

        byte[] first = Export(font, options);
        byte[] second = Export(font, options);

        Assert.Equal(first, second);
        Assert.Equal(0x00010000u, ReadUInt32(first));
        Assert.Equal(0xB1B0AFBAu, Checksum(first));
        Dictionary<string, (int Offset, int Length)> tables = ReadTables(first);
        Assert.Equal(new[] { "OS/2", "cmap", "glyf", "head", "hhea", "hmtx", "loca", "maxp", "name", "post" },
            tables.Keys.Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(54, tables["head"].Length);
        Assert.Equal(36, tables["hhea"].Length);
        Assert.Equal(32, tables["maxp"].Length);
        Assert.Equal(96, tables["OS/2"].Length);
        Assert.Equal((ushort)3, ReadUInt16(first, tables["maxp"].Offset + 4));
        Assert.Equal((ushort)1024, ReadUInt16(first, tables["head"].Offset + 18));
        Assert.Equal((ushort)3, ReadUInt16(first, tables["hhea"].Offset + 34));
        AssertCmapMaps(first, tables["cmap"], 0x0020, 1);
        AssertCmapMaps(first, tables["cmap"], 0x0041, 2);
        Assert.Equal(12, ReadUInt16(first, FindCmap(first, tables["cmap"], 0, 4)));
        Assert.Equal(4, ReadUInt16(first, FindCmap(first, tables["cmap"], 3, 1)));
        Assert.Equal(12, ReadUInt16(first, FindCmap(first, tables["cmap"], 3, 10)));
        Assert.Equal(3850070400u, ReadUInt32(first, tables["head"].Offset + 24));
    }

    [Fact]
    public void Export_AppliesCoverageThresholdAndOmitsUnusedAndSurrogateRecords()
    {
        string path = CreateFont(WftCoverageMode.Antialiased, 2,
            (0x0041, [0x87, 0], 2),
            (0x0042, [0, 0], 0),
            (0xD800, [0xF0, 0], 2));
        using WftFont font = WftFontReader.Open(path);

        byte[] bytes = Export(font, new WftTrueTypeExportOptions("Threshold Test"));
        Dictionary<string, (int Offset, int Length)> tables = ReadTables(bytes);

        Assert.Equal((ushort)2, ReadUInt16(bytes, tables["maxp"].Offset + 4));
        AssertCmapMaps(bytes, tables["cmap"], 0x0041, 1);
        AssertCmapDoesNotMap(bytes, tables["cmap"], 0x0042);
        AssertCmapDoesNotMap(bytes, tables["cmap"], 0xD800);

        int loca = tables["loca"].Offset;
        uint glyphStart = ReadUInt32(bytes, loca + 4);
        uint glyphEnd = ReadUInt32(bytes, loca + 8);
        Assert.True(glyphEnd > glyphStart);
        int glyf = tables["glyf"].Offset + (int)glyphStart;
        Assert.Equal((short)1, ReadInt16(bytes, glyf));
    }

    [Fact]
    public void Export_ReportsProgressHonorsCancellationAndLeavesStreamOpen()
    {
        string path = CreateFont(WftCoverageMode.Monochrome, 1);
        using WftFont font = WftFontReader.Open(path);
        using var destination = new MemoryStream();
        var reports = new List<WftTrueTypeExportProgress>();
        var progress = new InlineProgress<WftTrueTypeExportProgress>(reports.Add);

        WftTrueTypeExporter.Export(font, destination,
            new WftTrueTypeExportOptions("Empty Test"), progress: progress);

        Assert.True(destination.CanWrite);
        Assert.Equal(font.GlyphCount, reports[^1].ProcessedGlyphRecords);
        Assert.Equal(font.GlyphCount, reports[^1].TotalGlyphRecords);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => WftTrueTypeExporter.Export(font,
            new MemoryStream(), new WftTrueTypeExportOptions("Cancel Test"), cancellation.Token));
    }

    [Fact]
    public void Export_PreservesHolesMaximumCodePointAndStyleFlags()
    {
        string path = CreateFont(WftCoverageMode.Monochrome, 3,
            (0x004F, [0xE0, 0xA0, 0xE0], 3),
            (WftFont.MaximumCodePoint, [0x80, 0, 0], 1));
        using WftFont font = WftFontReader.Open(path);

        byte[] bytes = Export(font,
            new WftTrueTypeExportOptions("Outlined Pixels", WftFontStyle.BoldItalic));
        Dictionary<string, (int Offset, int Length)> tables = ReadTables(bytes);

        uint ringGlyph = FindGlyph(bytes, tables["cmap"], 0x004F);
        Assert.NotEqual(0u, ringGlyph);
        AssertCmapMaps(bytes, tables["cmap"], WftFont.MaximumCodePoint, 2);
        uint ringOffset = ReadUInt32(bytes, tables["loca"].Offset + (int)ringGlyph * 4);
        Assert.Equal((short)2, ReadInt16(bytes, tables["glyf"].Offset + (int)ringOffset));
        Assert.Equal((ushort)3, ReadUInt16(bytes, tables["head"].Offset + 44));
        ushort selection = ReadUInt16(bytes, tables["OS/2"].Offset + 62);
        Assert.Equal((ushort)0x21, selection);
    }

    [Fact]
    public void Export_RejectsInvalidNamesAndUnrepresentableMetrics()
    {
        string path = CreateFont(WftCoverageMode.Monochrome, 1,
            (0x0041, [0x80], ushort.MaxValue));
        using WftFont font = WftFontReader.Open(path);

        Assert.Throws<ArgumentException>(() => Export(font,
            new WftTrueTypeExportOptions(" ")));
        Assert.Throws<ArgumentOutOfRangeException>(() => Export(font,
            new WftTrueTypeExportOptions("Bad Threshold", coverageThreshold: 0)));
        Assert.Throws<InvalidDataException>(() => Export(font,
            new WftTrueTypeExportOptions("Impossible Metrics")));
    }

    private static byte[] Export(WftFont font, WftTrueTypeExportOptions options)
    {
        using var stream = new MemoryStream();
        WftTrueTypeExporter.Export(font, stream, options);
        return stream.ToArray();
    }

    private string CreateFont(WftCoverageMode mode, int cellSize,
        params (ushort CodePoint, byte[] Bitmap, ushort Advance)[] glyphs)
    {
        int rowStride = (cellSize * (mode == WftCoverageMode.Antialiased ? 4 : 1) + 7) / 8;
        int bitmapBytes = rowStride * cellSize;
        int recordSize = bitmapBytes + sizeof(ushort);
        string path = Path.Combine(_directory.Path, Guid.NewGuid() + ".wft");
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        stream.SetLength(16L + (long)recordSize * WftFont.MaximumGlyphCount);
        Span<byte> header = stackalloc byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(header, 0x544E4657);
        BinaryPrimitives.WriteUInt32LittleEndian(header[4..], (uint)cellSize);
        BinaryPrimitives.WriteUInt32LittleEndian(header[8..], (uint)mode);
        stream.Write(header);
        Span<byte> advanceBytes = stackalloc byte[2];
        foreach (var glyph in glyphs)
        {
            Assert.Equal(bitmapBytes, glyph.Bitmap.Length);
            stream.Position = 16L + (long)(glyph.CodePoint - WftFont.FirstCodePoint) * recordSize;
            stream.Write(glyph.Bitmap);
            BinaryPrimitives.WriteUInt16LittleEndian(advanceBytes, glyph.Advance);
            stream.Write(advanceBytes);
        }
        return path;
    }

    private static Dictionary<string, (int Offset, int Length)> ReadTables(byte[] bytes)
    {
        int count = ReadUInt16(bytes, 4);
        var result = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        for (int i = 0; i < count; i++)
        {
            int record = 12 + i * 16;
            string tag = System.Text.Encoding.ASCII.GetString(bytes, record, 4);
            int offset = checked((int)ReadUInt32(bytes, record + 8));
            int length = checked((int)ReadUInt32(bytes, record + 12));
            Assert.InRange(offset, 0, bytes.Length - length);
            result.Add(tag, (offset, length));
        }
        return result;
    }

    private static void AssertCmapMaps(byte[] bytes, (int Offset, int Length) cmap,
        uint codePoint, uint expectedGlyph)
        => Assert.Equal(expectedGlyph, FindGlyph(bytes, cmap, codePoint));

    private static void AssertCmapDoesNotMap(byte[] bytes, (int Offset, int Length) cmap,
        uint codePoint) => Assert.Equal(0u, FindGlyph(bytes, cmap, codePoint));

    private static uint FindGlyph(byte[] bytes, (int Offset, int Length) cmap, uint codePoint)
    {
        int subtable = cmap.Offset + checked((int)ReadUInt32(bytes, cmap.Offset + 8));
        Assert.Equal((ushort)12, ReadUInt16(bytes, subtable));
        uint groups = ReadUInt32(bytes, subtable + 12);
        for (int i = 0; i < groups; i++)
        {
            int group = subtable + 16 + i * 12;
            uint start = ReadUInt32(bytes, group);
            uint end = ReadUInt32(bytes, group + 4);
            if (codePoint >= start && codePoint <= end)
                return ReadUInt32(bytes, group + 8) + codePoint - start;
        }
        return 0;
    }

    private static int FindCmap(byte[] bytes, (int Offset, int Length) cmap, ushort platform,
        ushort encoding)
    {
        int count = ReadUInt16(bytes, cmap.Offset + 2);
        for (int i = 0; i < count; i++)
        {
            int record = cmap.Offset + 4 + i * 8;
            if (ReadUInt16(bytes, record) == platform &&
                ReadUInt16(bytes, record + 2) == encoding)
                return cmap.Offset + checked((int)ReadUInt32(bytes, record + 4));
        }
        throw new Xunit.Sdk.XunitException($"Missing cmap {platform}/{encoding}.");
    }

    private static uint Checksum(byte[] bytes)
    {
        uint sum = 0;
        for (int i = 0; i < bytes.Length; i += 4)
            sum = unchecked(sum + ReadUInt32(bytes, i));
        return sum;
    }

    private static short ReadInt16(byte[] bytes, int offset = 0) =>
        BinaryPrimitives.ReadInt16BigEndian(bytes.AsSpan(offset, 2));
    private static ushort ReadUInt16(byte[] bytes, int offset = 0) =>
        BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(offset, 2));
    private static uint ReadUInt32(byte[] bytes, int offset = 0)
    {
        Span<byte> value = stackalloc byte[4];
        bytes.AsSpan(offset, Math.Min(4, bytes.Length - offset)).CopyTo(value);
        return BinaryPrimitives.ReadUInt32BigEndian(value);
    }

    public void Dispose() => _directory.Dispose();

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
