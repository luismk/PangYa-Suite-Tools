using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using PangyaAPI.IFF;

namespace PangyaAPI.Tests;

public sealed class PspIffTests
{
    public static TheoryData<string, int> Tables => new()
    {
        { "Character", 0x160 }, { "Parts", 0x1cc }, { "Club", 0xac },
        { "ClubSet", 0xb0 }, { "Ball", 0x2e4 }, { "Item", 0xac },
        { "Caddie", 0xb0 }, { "CaddieItem", 0xd0 }, { "SetItem", 0xac },
        { "Course", 0x11c }, { "Title", 0x58 }, { "Skin", 0xac },
        { "HairStyle", 0x78 }, { "AuxPart", 0xc4 }, { "QuestDrop", 0xd8 },
        { "Quest", 0xd8 }, { "Desc", 0x204 }
    };

    [Theory]
    [MemberData(nameof(Tables))]
    public async Task Tables_DecodeEditAndRoundTripWithoutChangingUnknownBytes(string name, int size)
    {
        byte[] original = Build(2, size);
        using var stream = new MemoryStream(original);
        await using IffReader reader = IffReader.Open(stream, name + ".iff");
        Assert.Equal(IffHeader.PspRegion, reader.Info.Region);
        Assert.Equal(size, reader.Info.RecordSize);
        Assert.Equal(2, reader.Info.Header.RecordCount);
        Assert.Null(reader.Info.SchemaWarning);
        Assert.True(reader.Info.Schema!.IsEditable);
        Assert.Equal("ItemId", reader.Info.Schema.KeyField);
        Assert.Equal(0, IffSchemaCoverage.Calculate(reader.Info.Schema, size).UnrepresentedBytes);
        List<IffRecord> records = await ReadAll(reader);
        using var output = new MemoryStream();
        await IffWriter.WriteAsync(output, reader.Info.Header, Enumerate(records));
        Assert.Equal(original, output.ToArray());

        records[0].SetValue("ItemId", 0x12345678u);
        byte[] expected = original.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(8 + (name == "Desc" ? 0 : 4)), 0x12345678);
        await IffWriter.WriteAsync(output, reader.Info.Header, Enumerate(records));
        Assert.Equal(expected, output.ToArray());
        output.Position = 0;
        await using IffReader rereader = IffReader.Open(output, name + ".iff", new(LeaveOpen: true));
        Assert.Equal(0x12345678u, (await ReadAll(rereader))[0].GetUInt32("ItemId"));
    }

    [Fact]
    public async Task KnownFields_UsePspOffsetsSignedStatsAndSelectedEncoding()
    {
        byte[] bytes = Build(1, 0x160);
        using var input = new MemoryStream(bytes);
        await using IffReader reader = IffReader.Open(input, "Character.iff");
        IffRecord record = Assert.Single(await ReadAll(reader));
        record.SetValue("Name", "누리");
        record.SetValue("Model", "nuri");
        record.SetValue("Stat1", -12);
        record.SetValue("PrimarySlotCount", 20);
        record.SetValue("AdditionalSlotCount", 4);
        Assert.Equal("누리", record.GetString("Name"));
        Assert.Equal("nuri", Encoding.ASCII.GetString(record.Bytes.Span.Slice(0x74, 4)));
        Assert.Equal(-12, BinaryPrimitives.ReadInt32LittleEndian(record.Bytes.Span[0x114..]));
        Assert.Equal(20, record.Bytes.Span[0x128]);
        Assert.Equal(4, record.Bytes.Span[0x129]);

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding shiftJis = Encoding.GetEncoding(932);
        record.SetValue("Name", "日本", shiftJis);
        Assert.Equal("日本", record.GetString("Name", shiftJis));
        Assert.Equal(shiftJis.GetBytes("日本"), record.Bytes.Span.Slice(8, 4).ToArray());

        using var ballInput = new MemoryStream(Build(1, 0x2e4));
        await using IffReader ballReader = IffReader.Open(ballInput, "Ball.iff");
        IffRecord ball = Assert.Single(await ReadAll(ballReader));
        ball.SetValue("Stat5", -32768);
        Assert.Equal(short.MinValue, BinaryPrimitives.ReadInt16LittleEndian(ball.Bytes.Span[0x2dc..]));
    }

    [Fact]
    public async Task Writer_UpdatesFull32BitCountAndReaderAppliesConfiguredLimit()
    {
        using var output = new MemoryStream();
        IffRecord record = IffRecord.CreateCopy(0, new byte[] { 7 }, null);
        await IffWriter.WriteAsync(output, new IffHeader(0, 0, 1, [0, 0, 0]),
            Enumerate(Enumerable.Repeat(record, 65536)));
        Assert.Equal(65536u, BinaryPrimitives.ReadUInt32LittleEndian(output.ToArray()));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(output.ToArray().AsSpan(4)));
        Assert.Throws<InvalidDataException>(() => IffReader.Open(output, "Unknown.iff", new(LeaveOpen: true)));
        await using IffReader reader = IffReader.Open(output, "Unknown.iff",
            new(MaximumRecordCount: 70000, LeaveOpen: true));
        Assert.Equal(65536, reader.Info.Header.RecordCount);
        Assert.Equal(65536, (await ReadAll(reader)).Count);
    }

    [Fact]
    public async Task Writer_PreservesPcCountLimit()
    {
        using var output = new MemoryStream();
        IffRecord record = IffRecord.CreateCopy(0, new byte[] { 7 }, null);
        await Assert.ThrowsAsync<InvalidDataException>(() => IffWriter.WriteAsync(output,
            new IffHeader(0, 548, 11, [0, 0, 0]), Enumerate(Enumerable.Repeat(record, 65536))));
    }

    [Theory]
    [InlineData(0u, 1)]
    [InlineData(2u, 0xac + 1)]
    [InlineData(1u, 0xad)]
    [InlineData(0x10000u, 0)]
    [InlineData(uint.MaxValue, 0)]
    public void Reader_RejectsInconsistentOrExcessiveInput(uint count, int payload)
    {
        byte[] bytes = new byte[8 + payload];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, count);
        bytes[4] = 1;
        using var stream = new MemoryStream(bytes);
        Assert.Throws<InvalidDataException>(() => IffReader.Open(stream, "Item.iff"));
    }

    [Fact]
    public async Task EmptyPspTable_RoundTripsVersionHeader()
    {
        byte[] bytes = Build(0, 0xac);
        using var input = new MemoryStream(bytes);
        await using IffReader reader = IffReader.Open(input, "Item.iff");
        Assert.Equal(IffHeader.PspRegion, reader.Info.Region);
        Assert.Empty(await ReadAll(reader));
        using var output = new MemoryStream();
        await IffWriter.WriteAsync(output, reader.Info.Header, Enumerate([]));
        Assert.Equal(bytes, output.ToArray());
    }

    [Fact]
    public async Task HeaderlessTitle_PreservesLayoutAndAppliesCountLimit()
    {
        byte[] bytes = TitleRecords();
        using var input = new MemoryStream(bytes);
        await using IffReader reader = IffReader.Open(input, "Title.iff");
        Assert.True(reader.Info.Header.IsHeaderless);
        Assert.Equal(0, reader.Info.Header.DataOffset);
        Assert.Equal(3, reader.Info.Header.RecordCount);
        List<IffRecord> records = await ReadAll(reader);
        Assert.Equal(0x30000002u, records[2].GetUInt32("ItemId"));
        using var output = new MemoryStream();
        await IffWriter.WriteAsync(output, reader.Info.Header, Enumerate(records));
        Assert.Equal(bytes, output.ToArray());
        Assert.Throws<InvalidDataException>(() => IffReader.Open(input, "Title.iff", new(MaximumRecordCount: 2)));
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            IffWriter.WriteAsync(output, reader.Info.Header, Enumerate([])));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Container_SavesPspEditsAtomicallyAndPreservesOtherEntries(bool archive)
    {
        string directory = Path.Combine(Path.GetTempPath(), "PspIffTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, archive ? "PangYaPSP.iff" : "Parts.iff");
        byte[] original = Build(1, 0x1cc);
        byte[] title = TitleRecords();
        try
        {
            if (archive)
            {
                using ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create);
                await WriteEntry(zip, "Parts.iff", original);
                await WriteEntry(zip, "Title.iff", title);
                await WriteEntry(zip, "notes.bin", [9, 8, 7]);
            }
            else await File.WriteAllBytesAsync(path, original);

            await using (IffContainer container = await IffContainer.OpenAsync(path))
            {
                IffContainerEntry entry = container.Entries.Single(item => item.Name == "Parts.iff");
                await using Stream stream = await entry.OpenAsync(default);
                await using IffReader reader = IffReader.Open(stream, entry.Name);
                IffRecord record = Assert.Single(await ReadAll(reader));
                record.SetValue("SlotMask", 0x42u);
                stream.Dispose();
                await container.SaveEntryAsync(entry.Name, reader.Info.Header, [record]);
            }
            Assert.True(File.Exists(path + ".bak"));
            byte[] expected = original.ToArray();
            BinaryPrimitives.WriteUInt32LittleEndian(expected.AsSpan(8 + 0xa0), 0x42);
            if (archive)
            {
                using ZipArchive zip = ZipFile.OpenRead(path);
                Assert.Equal(expected, await ReadEntry(zip, "Parts.iff"));
                Assert.Equal(title, await ReadEntry(zip, "Title.iff"));
                Assert.Equal(new byte[] { 9, 8, 7 }, await ReadEntry(zip, "notes.bin"));
            }
            else Assert.Equal(expected, await File.ReadAllBytesAsync(path));
            IffCatalog catalog = await IffCatalog.LoadAsync(path);
            Assert.Equal(IffHeader.PspRegion, catalog.Region);
            Assert.All(catalog.Tables, table => Assert.Equal(IffHeader.PspRegion, table.Info.Region));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public async Task HeaderlessTitle_RejectsEditsThatDestroyItsFormatIdentification()
    {
        using var input = new MemoryStream(TitleRecords());
        await using IffReader reader = IffReader.Open(input, "Title.iff");
        List<IffRecord> records = await ReadAll(reader);
        records[0].SetValue("ItemId", 1u);
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<InvalidDataException>(() =>
            IffWriter.WriteAsync(output, reader.Info.Header, Enumerate(records)));
    }

    [Fact]
    public async Task LooseHeaderlessTitle_IsRecognizedAndSavedWithoutAddingHeader()
    {
        string directory = Path.Combine(Path.GetTempPath(), "PspIffTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "Title.iff");
        byte[] bytes = TitleRecords();
        try
        {
            await File.WriteAllBytesAsync(path, bytes);
            await using (IffContainer container = await IffContainer.OpenAsync(path))
            {
                IffContainerEntry entry = Assert.Single(container.Entries);
                await using Stream stream = await entry.OpenAsync(default);
                await using IffReader reader = IffReader.Open(stream, entry.Name);
                List<IffRecord> records = await ReadAll(reader);
                records[0].SetValue("Name", "Title");
                stream.Dispose();
                await container.SaveEntryAsync(entry.Name, reader.Info.Header, records);
            }
            byte[] saved = await File.ReadAllBytesAsync(path);
            Assert.Equal(bytes.Length, saved.Length);
            Assert.Equal(0x30000000u, BinaryPrimitives.ReadUInt32LittleEndian(saved.AsSpan(4)));
            Assert.Equal(bytes, await File.ReadAllBytesAsync(path + ".bak"));
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    private static byte[] Build(int count, int size)
    {
        byte[] bytes = new byte[8 + count * size];
        new Random(817).NextBytes(bytes);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)count);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), 1);
        return bytes;
    }

    private static byte[] TitleRecords()
    {
        byte[] bytes = new byte[3 * 0x58];
        for (int i = 0; i < 3; i++)
        {
            bytes[i * 0x58] = 1;
            BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 0x58 + 4), 0x30000000u + (uint)i);
        }
        return bytes;
    }

    private static async Task<List<IffRecord>> ReadAll(IffReader reader)
    {
        var records = new List<IffRecord>();
        await foreach (IffRecord record in reader.ReadRecordsAsync()) records.Add(record);
        return records;
    }

    private static async IAsyncEnumerable<IffRecord> Enumerate(IEnumerable<IffRecord> records)
    {
        foreach (IffRecord record in records) yield return record;
        await Task.CompletedTask;
    }

    private static async Task WriteEntry(ZipArchive zip, string name, byte[] bytes)
    {
        await using Stream stream = zip.CreateEntry(name).Open();
        await stream.WriteAsync(bytes);
    }

    private static async Task<byte[]> ReadEntry(ZipArchive zip, string name)
    {
        await using Stream stream = zip.GetEntry(name)!.Open();
        using var output = new MemoryStream();
        await stream.CopyToAsync(output);
        return output.ToArray();
    }
}
