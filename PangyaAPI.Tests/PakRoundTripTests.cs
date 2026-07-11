using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using System.Text;

namespace PangyaAPI.Tests;

public sealed class PakRoundTripTests
{
    public static TheoryData<PakFileEntryVersion, PakFileEntryType> Formats => new()
    {
        { PakFileEntryVersion.Raw, PakFileEntryType.Raw },
        { PakFileEntryVersion.V1, PakFileEntryType.Raw },
        { PakFileEntryVersion.V2, PakFileEntryType.LZ77 },
        { PakFileEntryVersion.V3, PakFileEntryType.Raw },
        { PakFileEntryVersion.V3, PakFileEntryType.LZ77 },
        { PakFileEntryVersion.V3, PakFileEntryType.LZ772 },
    };

    [Theory]
    [MemberData(nameof(Formats))]
    public void CreateParseExtract_RoundTripsFixture(
        PakFileEntryVersion version, PakFileEntryType type)
    {
        using var temp = new TemporaryDirectory();
        string source = CreateFixture(temp);
        string pak = temp.Combine("fixture.pak");
        string output = temp.Combine("output");

        new PakWriter
        {
            EntryVersion = version,
            EntryType = type,
            LocationKeys = PakKeys.JP,
            Author = "PangyaAPI.Tests"
        }.CreateFromDirectoryContents(source, pak);

        using var reader = new PakReader(pak);
        reader.Parse(version == PakFileEntryVersion.V3 ? PakKeys.JP : null);
        reader.Extract("*", output);

        Assert.Equal("PangyaAPI.Tests", reader.Header.Author);
        AssertFixture(source, output);
    }

    [Theory]
    [MemberData(nameof(AllPakKeys))]
    public void Version3_RoundTripsWithEveryRegionKey(string label, uint[] keys)
    {
        using var temp = new TemporaryDirectory();
        string source = CreateFixture(temp);
        string pak = temp.Combine($"{label}.pak");

        new PakWriter
        {
            EntryVersion = PakFileEntryVersion.V3,
            EntryType = PakFileEntryType.LZ772,
            LocationKeys = keys
        }.CreateFromDirectoryContents(source, pak);

        using var reader = new PakReader(pak);
        reader.Parse(keys);

        foreach (var entry in reader.Entries.Where(e => e.Type != PakFileEntryType.Directory))
        {
            string sourcePath = System.IO.Path.Combine(source, entry.Name.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Assert.Equal(File.ReadAllBytes(sourcePath), reader.ExtractEntryToBytes(entry));
        }
    }

    [Fact]
    public void Version3_RoundTripsEucKrEntryNames()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("source");
        string koreanDirectory = System.IO.Path.Combine(source, "한글");
        string koreanFile = System.IO.Path.Combine(koreanDirectory, "파일.txt");
        string pak = temp.Combine("korean-names.pak");
        Directory.CreateDirectory(koreanDirectory);
        File.WriteAllText(koreanFile, "PangYa");

        new PakWriter
        {
            EntryVersion = PakFileEntryVersion.V3,
            EntryType = PakFileEntryType.LZ772,
            LocationKeys = PakKeys.KR
        }.CreateFromDirectoryContents(source, pak);

        using var reader = new PakReader(pak);
        reader.Parse(PakKeys.KR);
        PakFileEntry entry = Assert.Single(reader.Entries,
            item => item.Type != PakFileEntryType.Directory);

        Assert.Equal(@"한글\파일.txt", entry.Name);
        Assert.Equal(File.ReadAllBytes(koreanFile), reader.ExtractEntryToBytes(entry));
    }

    [Fact]
    public void Extract_WritesPangyaIffPathSidecar()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("source");
        string dataDirectory = System.IO.Path.Combine(source, "data");
        string pak = temp.Combine("pangya.pak");
        string output = temp.Combine("output");
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllBytes(System.IO.Path.Combine(dataDirectory, "pangya_th.iff"), [1, 2, 3, 4]);
        File.WriteAllText(System.IO.Path.Combine(dataDirectory, "Item.iff"), "item-data");

        new PakWriter { EntryType = PakFileEntryType.Raw }
            .CreateFromDirectoryContents(source, pak);

        using var reader = new PakReader(pak);
        reader.Parse();
        reader.Extract("*", output);

        string extracted = System.IO.Path.Combine(output, "data", "pangya_th.iff");
        string manifestPath = PakExtractionSidecar.GetManifestPath(extracted);
        Assert.Equal(System.IO.Path.Combine(output, "data", "pakpath.json"), manifestPath);
        Assert.True(File.Exists(manifestPath));
        Assert.Equal(output, PakExtractionSidecar.TryResolveExtractionRoot(extracted));
        string json = File.ReadAllText(manifestPath);
        Assert.Contains("\"Files\":", json, StringComparison.Ordinal);
        Assert.Contains("\"FileName\": \"pangya_th.iff\"", json, StringComparison.Ordinal);
        Assert.Contains("\"FileName\": \"Item.iff\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Path\": \"data\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void WriteManifest_IncludesFilesFromAllExtractedPaks()
    {
        using var temp = new TemporaryDirectory();
        string output = temp.Combine("output");
        string dataDirectory = System.IO.Path.Combine(output, "data");
        Directory.CreateDirectory(dataDirectory);

        string pangyaPath = System.IO.Path.Combine(dataDirectory, "pangya_th.iff");
        string itemPath = System.IO.Path.Combine(dataDirectory, "Item.iff");
        string cardPath = System.IO.Path.Combine(dataDirectory, "Card.iff");
        File.WriteAllBytes(pangyaPath, [1]);
        File.WriteAllBytes(itemPath, [2]);
        File.WriteAllBytes(cardPath, [3]);

        PakExtractionSidecar.WriteManifest([
            (new PakFileEntry { Name = @"data\pangya_th.iff" }, pangyaPath),
            (new PakFileEntry { Name = @"data\Item.iff" }, itemPath),
            (new PakFileEntry { Name = @"data\Card.iff" }, cardPath),
        ]);

        string json = File.ReadAllText(System.IO.Path.Combine(dataDirectory, "pakpath.json"));
        Assert.Contains("\"FileName\": \"pangya_th.iff\"", json, StringComparison.Ordinal);
        Assert.Contains("\"FileName\": \"Item.iff\"", json, StringComparison.Ordinal);
        Assert.Contains("\"FileName\": \"Card.iff\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ReadersKeepIndependentFilenameEncodings()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding shiftJis = Encoding.GetEncoding(932);
        using var temp = new TemporaryDirectory();

        string koreanSource = temp.Combine("korean");
        string koreanFile = System.IO.Path.Combine(koreanSource, "한글.txt");
        Directory.CreateDirectory(koreanSource);
        File.WriteAllText(koreanFile, "KR");
        string koreanPak = temp.Combine("korean.pak");
        new PakWriter { LocationKeys = PakKeys.KR }
            .CreateFromDirectoryContents(koreanSource, koreanPak);

        string japaneseSource = temp.Combine("japanese");
        string japaneseFile = System.IO.Path.Combine(japaneseSource, "日本.txt");
        Directory.CreateDirectory(japaneseSource);
        File.WriteAllText(japaneseFile, "JP");
        string japanesePak = temp.Combine("japanese.pak");
        new PakWriter { LocationKeys = PakKeys.JP, FileNameEncoding = shiftJis }
            .CreateFromDirectoryContents(japaneseSource, japanesePak);

        using var koreanReader = new PakReader(koreanPak);
        using var japaneseReader = new PakReader(japanesePak, shiftJis);
        koreanReader.Parse(PakKeys.KR);
        japaneseReader.Parse(PakKeys.JP);

        Assert.Equal(51949, koreanReader.FileNameEncoding.CodePage);
        Assert.Equal(932, japaneseReader.FileNameEncoding.CodePage);
        Assert.Contains(koreanReader.Entries, entry => entry.Name == "한글.txt");
        Assert.Contains(japaneseReader.Entries, entry => entry.Name == "日本.txt");
    }

    public static IEnumerable<object[]> AllPakKeys() =>
        PakKeys.All.Select(item => new object[] { item.Label, item.Keys });

    private static string CreateFixture(TemporaryDirectory temp)
    {
        string source = temp.Combine("source");
        Directory.CreateDirectory(System.IO.Path.Combine(source, "nested"));
        File.WriteAllBytes(System.IO.Path.Combine(source, "empty.bin"), []);
        File.WriteAllText(System.IO.Path.Combine(source, "readme.txt"), "PangYa fixture\nLine two.");
        File.WriteAllBytes(System.IO.Path.Combine(source, "nested", "data.bin"),
            Enumerable.Range(0, 4096).Select(i => (byte)((i * 37) % 256)).ToArray());
        File.WriteAllBytes(System.IO.Path.Combine(source, "sound.wav"),
            Enumerable.Range(0, 257).Select(i => (byte)i).ToArray());
        return source;
    }

    private static void AssertFixture(string source, string output)
    {
        string[] sourceFiles = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
        Assert.Equal(sourceFiles.Length, Directory.GetFiles(output, "*", SearchOption.AllDirectories).Length);

        foreach (string sourceFile in sourceFiles)
        {
            string relative = System.IO.Path.GetRelativePath(source, sourceFile);
            Assert.Equal(File.ReadAllBytes(sourceFile), File.ReadAllBytes(System.IO.Path.Combine(output, relative)));
        }
    }
}
