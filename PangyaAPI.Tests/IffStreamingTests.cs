using System.IO.Compression;
using System.Text;
using PangyaAPI.IFF;

namespace PangyaAPI.Tests;

public sealed class IffStreamingTests
{
    [Fact]
    public void FixedString_UsesSelectedEncodingForReadingAndWriting()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding shiftJis = Encoding.GetEncoding(932);
        var field = new IffField("Name", 0, 16, IffFieldType.FixedString, Encoding: Encoding.Latin1);
        byte[] record = new byte[16];
        shiftJis.GetBytes("日本", record);

        Assert.Equal("日本", field.GetValue(record, shiftJis));

        field.SetValue(record, "パンヤ", shiftJis);
        int terminator = Array.IndexOf(record, (byte)0);
        Assert.Equal("パンヤ", shiftJis.GetString(record, 0, terminator));
    }

    [Fact]
    public void CreateBlankRecord_UsesRequestedSizeAndStartsDirty()
    {
        IffRecord record = IffRecord.CreateBlank(4, 128, schema: null);

        Assert.Equal(4, record.Index);
        Assert.Equal(128, record.Bytes.Length);
        Assert.True(record.IsDirty);
        Assert.All(record.Bytes.ToArray(), value => Assert.Equal(0, value));
    }

    public static TheoryData<string, int> ThailandSchemas => new()
    {
        { "AuxPart.iff", 176 }, { "Ball.iff", 764 }, { "Caddie.iff", 200 }, { "CaddieItem.iff", 236 },
        { "CadieMagicBox.iff", 104 }, { "CadieMagicBoxRandom.iff", 16 }, { "Card.iff", 328 },
        { "Character.iff", 372 }, { "Club.iff", 196 }, { "ClubSet.iff", 179 }, { "Course.iff", 312 },
        { "CutinInfomation.iff", 208 }, { "Desc.iff", 516 }, { "Enchant.iff", 16 }, { "Furniture.iff", 464 },
        { "FurnitureAbility.iff", 60 }, { "HairStyle.iff", 148 }, { "Item.iff", 196 }, { "Mascot.iff", 256 },
        { "Match.iff", 332 }, { "OfflineShop.iff", 200 }, { "Part.iff", 512 }, { "QuestDrop.iff", 244 },
        { "SetItem.iff", 220 }, { "Skin.iff", 208 }, { "TikiPointTable.iff", 48 }, { "TikiRecipe.iff", 52 },
        { "TikiSpecialTable.iff", 60 }
    };

    [Theory]
    [MemberData(nameof(ThailandSchemas))]
    public void EveryJavaSchema_HasFieldsWithinItsRecord(string fileName, int recordSize)
    {
        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema schema = Assert.IsType<IffSchema>(IffSchemaRegistry.Resolve(fileName, header, recordSize));

        Assert.NotEmpty(schema.Fields);
        Assert.Equal(schema.Fields.Count, schema.Fields.Select(field => field.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(schema.Fields, field =>
        {
            Assert.True(field.Offset >= 0, field.Name);
            Assert.True(field.Width > 0, field.Name);
            Assert.True(field.Offset + field.Width <= recordSize,
                $"{fileName}: {field.Name} ends at {field.Offset + field.Width}, record size is {recordSize}.");
        });
    }

    [Theory]
    [MemberData(nameof(ThailandSchemas))]
    public void EveryEditableSchemaField_OnlyWritesInsideItsRange(string fileName, int recordSize)
    {
        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema schema = IffSchemaRegistry.Resolve(fileName, header, recordSize)!;
        foreach (IffField field in schema.Fields.Where(item => item.IsEditable))
        {
            byte[] record = new byte[recordSize];
            object value = field.Type switch
            {
                IffFieldType.Boolean => true,
                IffFieldType.FixedString => "A",
                IffFieldType.DateTime => new DateTime(2026, 7, 4, 12, 34, 56, DateTimeKind.Unspecified),
                _ => 1
            };

            field.SetValue(record, value);

            Assert.All(record.Take(field.Offset), item => Assert.Equal(0, item));
            Assert.All(record.Skip(field.Offset + field.Width), item => Assert.Equal(0, item));
            Assert.NotNull(field.GetValue(record));
        }
    }

    [Fact]
    public void ItemIdBitFields_FollowDocumentedLayoutAndPreserveNeighbors()
    {
        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema schema = IffSchemaRegistry.Resolve("Item.iff", header, 196)!;
        byte[] record = new byte[196];

        Set("IFF Type", 0x2A);
        Set("Character Serial", 0xAB);
        Set("Position", 0x1D);
        Set("Group", 0x3);
        Set("Type", 0x2);
        Set("Serial", 0x1AA);

        uint expected = (0x2Au << 26) | (0xABu << 18) | (0x1Du << 13) |
                        (0x3u << 11) | (0x2u << 9) | 0x1AAu;
        Assert.Equal(expected, schema.Fields.Single(field => field.Name == "ItemId").GetValue(record));

        uint before = BitConverter.ToUInt32(record, 4);
        Set("Position", 2);
        uint after = BitConverter.ToUInt32(record, 4);
        Assert.Equal(before & ~0x0003E000u, after & ~0x0003E000u);
        Assert.Equal(2u, schema.Fields.Single(field => field.Name == "Position").GetValue(record));
        Assert.Throws<ArgumentOutOfRangeException>(() => Set("Character Serial", 256));

        void Set(string name, object value) => schema.Fields.Single(field => field.Name == name).SetValue(record, value);
    }

    [Fact]
    public void MoneyAndShopFlags_ExposeBooleansAndPreserveUnknownBits()
    {
        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema schema = IffSchemaRegistry.Resolve("Item.iff", header, 196)!;
        byte[] record = new byte[196];
        IffField cookies = Field("Cookies");
        IffField pang = Field("Pang");
        IffField free = Field("Free");
        IffField inStock = Field("InStock");
        IffField showHot = Field("ShowHot");

        Assert.True((bool)free.GetValue(record));
        cookies.SetValue(record, true);
        pang.SetValue(record, true);
        Assert.Equal(0x03, record[104]);
        Assert.False((bool)free.GetValue(record));
        cookies.SetValue(record, false);
        Assert.Equal(0x02, record[104]);

        record[105] = 0xE0;
        inStock.SetValue(record, true);
        showHot.SetValue(record, true);
        Assert.Equal(0xF1, record[105]);
        inStock.SetValue(record, false);
        Assert.Equal(0xF0, record[105]);

        IffField Field(string name) => schema.Fields.Single(field => field.Name == name);
    }

    [Fact]
    public async Task Reader_StreamsAndWriterPreservesUnknownBytes()
    {
        byte[] source = BuildIff("Item.iff", count: 3, recordSize: 196);
        await using var input = new MemoryStream(source, writable: false);
        await using IffReader reader = IffReader.Open(input, "Item.iff", new(LeaveOpen: true));

        Assert.Equal("TH", reader.Info.Region);
        Assert.Equal(196, reader.Info.RecordSize);
        Assert.Equal("Item", reader.Info.Schema!.Name);
        var records = new List<IffRecord>();
        await foreach (IffRecord record in reader.ReadRecordsAsync()) records.Add(record);

        await using var output = new MemoryStream();
        await IffWriter.WriteAsync(output, reader.Info.Header, AsAsync(records));
        Assert.Equal(source, output.ToArray());
    }

    [Fact]
    public async Task EditingKnownField_ChangesOnlyItsBytes()
    {
        byte[] source = BuildIff("Item.iff", 1, 196);
        await using var input = new MemoryStream(source, writable: false);
        await using IffReader reader = IffReader.Open(input, "Item.iff");
        IffRecord record = await SingleAsync(reader.ReadRecordsAsync());
        byte[] before = record.Bytes.ToArray();

        record.SetValue("Price", 0x12345678u);

        byte[] after = record.Bytes.ToArray();
        Assert.Equal(before[..92], after[..92]);
        Assert.Equal([0x78, 0x56, 0x34, 0x12], after[92..96]);
        Assert.Equal(before[96..], after[96..]);
        Assert.True(record.IsDirty);
        record.AcceptChanges();
        Assert.False(record.IsDirty);
    }

    [Theory]
    [InlineData(7, 0)]
    [InlineData(10, 3)]
    [InlineData(205, 2)]
    public void Reader_RejectsMalformedLengths(int length, ushort count)
    {
        byte[] data = new byte[length];
        if (length >= 8)
        {
            BitConverter.GetBytes(count).CopyTo(data, 0);
            data[4] = 11;
        }
        using var stream = new MemoryStream(data);
        Assert.Throws<InvalidDataException>(() => IffReader.Open(stream, "Item.iff"));
    }

    [Fact]
    public async Task Container_OpensLooseAndZipEntries()
    {
        using var temp = new TemporaryDirectory();
        string loose = temp.Combine("Item.iff");
        await File.WriteAllBytesAsync(loose, BuildIff("Item.iff", 1, 196));
        await using (IffContainer container = await IffContainer.OpenAsync(loose))
        {
            Assert.Equal(IffContainerKind.LooseFile, container.Kind);
            Assert.Single(container.Entries);
        }

        string zipPath = temp.Combine("data.iff");
        using (ZipArchive zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            ZipArchiveEntry entry = zip.CreateEntry("Item.iff");
            await using Stream output = entry.Open();
            await output.WriteAsync(BuildIff("Item.iff", 1, 196));
        }
        await using (IffContainer container = await IffContainer.OpenAsync(zipPath))
        {
            Assert.Equal(IffContainerKind.ZipArchive, container.Kind);
            Assert.Equal("Item.iff", Assert.Single(container.Entries).Name);
        }
    }

    [Fact]
    public async Task Container_RejectsPathTraversal()
    {
        using var temp = new TemporaryDirectory();
        string path = temp.Combine("bad.zip");
        using (ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            ZipArchiveEntry entry = zip.CreateEntry("../Item.iff");
            await using Stream output = entry.Open();
            await output.WriteAsync(BuildIff("Item.iff", 1, 196));
        }
        await Assert.ThrowsAsync<InvalidDataException>(() => IffContainer.OpenAsync(path));
    }

    [Fact]
    public async Task XorContainer_AutoDetectsAndRoundTrips()
    {
        using var temp = new TemporaryDirectory();
        string plain = temp.Combine("plain.zip");
        string encoded = temp.Combine("encoded.iff");
        using (ZipArchive zip = ZipFile.Open(plain, ZipArchiveMode.Create))
        {
            ZipArchiveEntry zipEntry = zip.CreateEntry("Item.iff");
            await using Stream output = zipEntry.Open();
            await output.WriteAsync(BuildIff("Item.iff", 1, 196));
        }
        byte[] bytes = await File.ReadAllBytesAsync(plain);
        for (int i = 0; i < bytes.Length; i++) bytes[i] ^= 0x71;
        await File.WriteAllBytesAsync(encoded, bytes);

        await using IffContainer container = await IffContainer.OpenAsync(encoded);
        Assert.Equal(IffContainerKind.XorZipArchive, container.Kind);
        IffContainerEntry entry = Assert.Single(container.Entries);
        IffRecord record;
        IffHeader header;
        await using (Stream stream = await entry.OpenAsync(default))
        await using (IffReader reader = IffReader.Open(stream, entry.Name))
        {
            header = reader.Info.Header;
            record = await SingleAsync(reader.ReadRecordsAsync());
            record.SetValue("Price", 71u);
        }
        await container.SaveEntryAsync(entry.Name, header, [record]);

        Assert.True(File.Exists(encoded + ".bak"));
        await using IffContainer reopened = await IffContainer.OpenAsync(encoded);
        Assert.Equal(IffContainerKind.XorZipArchive, reopened.Kind);
        IffContainerEntry reopenedEntry = Assert.Single(reopened.Entries);
        await using Stream reopenedStream = await reopenedEntry.OpenAsync(default);
        await using IffReader reopenedReader = IffReader.Open(reopenedStream, reopenedEntry.Name);
        Assert.Equal(71u, Convert.ToUInt32((await SingleAsync(reopenedReader.ReadRecordsAsync())).GetValue("Price")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveEntry_ReplacesAtomicallyAndCreatesBackup(bool zipped)
    {
        using var temp = new TemporaryDirectory();
        string path = temp.Combine(zipped ? "data.zip" : "Item.iff");
        byte[] original = BuildIff("Item.iff", 1, 196);
        if (zipped)
        {
            using ZipArchive zip = ZipFile.Open(path, ZipArchiveMode.Create);
            ZipArchiveEntry zipEntry = zip.CreateEntry("Item.iff");
            await using (Stream output = zipEntry.Open())
                await output.WriteAsync(original);
            ZipArchiveEntry untouched = zip.CreateEntry("notes.bin");
            await using (Stream untouchedOutput = untouched.Open())
                await untouchedOutput.WriteAsync(new byte[] { 9, 8, 7, 6 });
        }
        else await File.WriteAllBytesAsync(path, original);

        await using IffContainer container = await IffContainer.OpenAsync(path);
        IffContainerEntry entry = Assert.Single(container.Entries);
        IffRecord record;
        IffHeader header;
        await using (Stream entryStream = await entry.OpenAsync(default))
        await using (IffReader reader = IffReader.Open(entryStream, entry.Name))
        {
            header = reader.Info.Header;
            record = await SingleAsync(reader.ReadRecordsAsync());
            record.SetValue("Price", 42u);
        }
        await container.SaveEntryAsync(entry.Name, header, [record]);

        Assert.True(File.Exists(path + ".bak"));
        if (zipped)
        {
            using ZipArchive savedZip = ZipFile.OpenRead(path);
            await using Stream untouched = savedZip.GetEntry("notes.bin")!.Open();
            using var bytes = new MemoryStream();
            await untouched.CopyToAsync(bytes);
            Assert.Equal([9, 8, 7, 6], bytes.ToArray());

            using ZipArchive backupZip = ZipFile.OpenRead(path + ".bak");
            Assert.NotNull(backupZip.GetEntry("Item.iff"));
            Assert.NotNull(backupZip.GetEntry("notes.bin"));
        }
        await using IffContainer reopened = await IffContainer.OpenAsync(path);
        IffContainerEntry savedEntry = Assert.Single(reopened.Entries);
        await using Stream savedStream = await savedEntry.OpenAsync(default);
        await using IffReader savedReader = IffReader.Open(savedStream, savedEntry.Name);
        Assert.Equal(42u, Convert.ToUInt32((await SingleAsync(savedReader.ReadRecordsAsync())).GetValue("Price")));
    }

    private static byte[] BuildIff(string _, ushort count, int recordSize)
    {
        byte[] data = new byte[8 + count * recordSize];
        BitConverter.GetBytes(count).CopyTo(data, 0);
        data[4] = 11;
        for (int i = 8; i < data.Length; i++) data[i] = (byte)(i * 37);
        return data;
    }

    private static async IAsyncEnumerable<IffRecord> AsAsync(IEnumerable<IffRecord> records)
    {
        foreach (IffRecord record in records) { yield return record; await Task.Yield(); }
    }

    private static async Task<IffRecord> SingleAsync(IAsyncEnumerable<IffRecord> records)
    {
        IffRecord? result = null;
        await foreach (IffRecord record in records)
        {
            Assert.Null(result);
            result = record;
        }
        return Assert.IsType<IffRecord>(result);
    }
}
