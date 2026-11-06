using System.Text;
using PangyaAPI.PAK.Models;

namespace PangyaAPI.Tests;

public sealed class PspPakArchiveTests
{
    [Theory]
    [InlineData(PspPakArchive.DataFileName)]
    [InlineData(PspPakArchive.IndexFileName)]
    public void ReadsBothMembersAndExtractsDuplicateNames(string member)
    {
        using var temp = new TemporaryDirectory();
        CreatePair(temp);

        using var reader = new PakReader(temp.Combine(member), Encoding.ASCII);
        reader.Parse();
        Assert.True(reader.IsPspArchive);
        Assert.Equal(2u, reader.Header.NumFileEntry);
        Assert.All(reader.Entries, entry => Assert.Equal("same_texture.gim", entry.Name));
        Assert.Equal(new byte[] { 1, 2 }, reader.ExtractEntryToBytes(reader.Entries[0]));
        Assert.Equal(new byte[] { 3, 4 }, reader.ExtractEntryToBytes(reader.Entries[1]));

        string output = temp.Combine("extracted");
        reader.Extract("*", output, writeManifest: false);
        Assert.Equal(new byte[] { 1, 2 }, File.ReadAllBytes(Path.Combine(output, "same_texture.gim")));
        Assert.Equal(new byte[] { 3, 4 }, File.ReadAllBytes(Path.Combine(output, "same_texture~1.gim")));
    }

    [Fact]
    public void ReplacesOnlyRequestedBytesAndPreservesBothIndexes()
    {
        using var temp = new TemporaryDirectory();
        var (index, data) = CreatePair(temp);
        byte[] originalIndex = File.ReadAllBytes(index);
        byte[] originalData = File.ReadAllBytes(data);
        string replacement = temp.Combine("replacement.gim");
        File.WriteAllBytes(replacement, [8, 9]);

        PspPakArchive.ReplaceEntries(index, new Dictionary<int, string> { [1] = replacement }, saveBackup: true);

        Assert.Equal(originalIndex, File.ReadAllBytes(index));
        Assert.Equal(originalData, File.ReadAllBytes(data + ".bak"));
        originalData[32768] = 8;
        originalData[32769] = 9;
        Assert.Equal(originalData, File.ReadAllBytes(data));
    }

    [Fact]
    public void ManagerInjectsUniqueEntryWhenOpeningIndexMember()
    {
        using var temp = new TemporaryDirectory();
        var (index, data) = CreatePair(temp, duplicateNames: false);
        string replacement = temp.Combine("next_texture.gim");
        File.WriteAllBytes(replacement, [8, 9]);
        using var reader = new PakReader(index);
        reader.Parse();

        PakManager.InjectFiles(index, reader, [new PakInjectItem(replacement, null)], default);

        using var updated = new PakReader(data);
        updated.Parse();
        Assert.Equal(new byte[] { 1, 2 }, updated.ExtractEntryToBytes(updated.Entries[0]));
        Assert.Equal(new byte[] { 8, 9 }, updated.ExtractEntryToBytes(updated.Entries[1]));
    }

    [Fact]
    public void RejectsResizingAndOutOfBoundsPayloads()
    {
        using var temp = new TemporaryDirectory();
        var (index, data) = CreatePair(temp);
        byte[] originalData = File.ReadAllBytes(data);
        string replacement = temp.Combine("replacement.gim");
        File.WriteAllBytes(replacement, [8]);
        Assert.Throws<InvalidDataException>(() => PspPakArchive.ReplaceEntries(
            index, new Dictionary<int, string> { [0] = replacement }));
        Assert.Equal(originalData, File.ReadAllBytes(data));

        File.WriteAllBytes(data, [1, 2]);
        using var reader = new PakReader(index);
        Assert.Throws<InvalidDataException>(() => reader.Parse());
        Assert.Empty(reader.Entries);
    }

    private static (string Index, string Data) CreatePair(TemporaryDirectory temp, bool duplicateNames = true)
    {
        string index = temp.Combine(PspPakArchive.IndexFileName);
        string data = temp.Combine(PspPakArchive.DataFileName);
        using (var writer = new BinaryWriter(File.Create(index)))
        {
            foreach (uint offset in new uint[] { 0, 32768 })
            {
                writer.Write((ushort)3);
                writer.Write(ushort.MaxValue);
                writer.Write(offset);
                writer.Write(2u);
                writer.Write(2u);
                writer.Write(offset == 0 || duplicateNames ? "same_texture.gim"u8 : "next_texture.gim"u8);
                writer.Write(new byte[12]);
            }
            writer.Write(new byte[64]);
        }

        using (var stream = File.Create(data))
        {
            stream.Write(new byte[] { 1, 2 });
            stream.Position = 32768;
            stream.Write(new byte[] { 3, 4 });
            stream.Position = 65536;
            stream.Write("PSPSHARE"u8);
        }

        return (index, data);
    }
}
