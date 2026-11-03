using PangyaAPI.UpdateList.Models;
using PangyaAPI.Utilities.Cryptography;

namespace PangyaAPI.Tests;

public sealed class UpdateListRoundTripTests
{
    [Fact]
    public void WriteAndRead_ReleaseUpdateListFileHandleAfterEachOperation()
    {
        using var temp = new TemporaryDirectory();
        string output = temp.Combine("released-updatelist.dat");
        var header = new UpdateHeader
        {
            ClientPatchVersion = "1",
            ClientPatchNum = "1",
            UpdateVersion = "20260813"
        };
        var entries = new List<UpdateEntry>
        {
            new() { fname = "client.dll", fdir = "\\", pname = "client.zip" }
        };

        new UpdateWriter(UpdateKeys.JP).WriteUpdateList(output, header, entries);
        AssertCanOpenExclusively(output);

        _ = new UpdateReader(UpdateKeys.JP).ReadUpdateList(output);
        AssertCanOpenExclusively(output);
    }

    [Fact]
    public void UpdateKeys_IncludesSuperSsDevForGeneratorSelection()
    {
        var ssDev = Assert.Single(UpdateKeys.All, item => item.Label == "Super SS Dev");

        Assert.Same(UpdateKeys.SS, ssDev.Keys);
        Assert.Equal([0x087A0F82u, 0x1880DD08u, 0x85FA69CBu, 0xFF5808EAu], ssDev.Keys);
    }

    [Fact]
    public void GenerateFromDirectory_EnumeratesDeterministicallyAndSkipsMetadataFiles()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("update-source");
        Directory.CreateDirectory(Path.Combine(source, "data"));
        File.WriteAllText(Path.Combine(source, "z.txt"), "z");
        File.WriteAllText(Path.Combine(source, "data", "a.txt"), "a");
        File.WriteAllText(Path.Combine(source, "ignored.json"), "{}");
        string output = Path.Combine(source, "updatelist.dat");

        new UpdateMaker().GenerateFromDirectory(source, output, UpdateKeys.JP, "patch");
        var result = new UpdateReader(UpdateKeys.JP).ReadUpdateList(output);

        Assert.Equal(["a.txt", "z.txt"], result.Entries.Select(entry => entry.fname).ToArray());
        Assert.Equal("\\data", result.Entries[0].fdir);
        Assert.DoesNotContain(result.Entries, entry => entry.fname.EndsWith(".json"));
    }

    [Theory]
    [MemberData(nameof(AllUpdateKeys))]
    public void WriteRead_RoundTripsEveryPersistedField(string label, uint[] keys)
    {
        using var temp = new TemporaryDirectory();
        string output = temp.Combine($"update-{label}.dat");
        var header = new UpdateHeader
        {
            ClientPatchVersion = "42",
            ClientPatchNum = "7",
            UpdateVersion = "20260702"
        };
        var entries = new List<UpdateEntry>
        {
            new()
            {
                fname = "한글 & <item> \"special\".iff",
                fdir = "\\data\\item",
                fsize = 9_876_543_210,
                fcrc = -123456789,
                fdate = "2026-07-02",
                ftime = "21:30:45",
                pname = "패치 & file.zip",
                psize = 7654321
            }
        };

        new UpdateWriter(keys).WriteUpdateList(output, header, entries);
        var actual = new UpdateReader(keys).ReadUpdateList(output);

        Assert.Equal(header.ClientPatchVersion, actual.Header.ClientPatchVersion);
        Assert.Equal(header.ClientPatchNum, actual.Header.ClientPatchNum);
        Assert.Equal(header.UpdateVersion, actual.Header.UpdateVersion);
        Assert.Single(actual.Entries);
        AssertPersistedFields(entries[0], actual.Entries[0]);
        Assert.False(File.Exists(temp.Combine("updatelist_temp.xml")));
    }

    public static IEnumerable<object[]> AllUpdateKeys() =>
        UpdateKeys.All.Select(item => new object[] { item.Label, item.Keys });

    private static void AssertPersistedFields(UpdateEntry expected, UpdateEntry actual)
    {
        foreach (var field in UpdateEntryFieldMap.Fields)
            Assert.Equal(field.Get(expected), field.Get(actual));
    }

    private static void AssertCanOpenExclusively(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(stream.CanRead);
    }
}
