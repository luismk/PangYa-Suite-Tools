using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using PangyaAPI.UpdateList.Models;

namespace PangyaAPI.Tests;

public sealed class InvalidInputTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    public void PakReader_RejectsTruncatedFiles(int length)
    {
        using var temp = new TemporaryDirectory();
        string pak = temp.Combine("truncated.pak");
        File.WriteAllBytes(pak, new byte[length]);

        using var reader = new PakReader(pak);
        Assert.Throws<InvalidDataException>(() => reader.Parse());
    }

    [Fact]
    public void PakReader_RejectsEntryTableOutsideFile()
    {
        using var temp = new TemporaryDirectory();
        string pak = temp.Combine("invalid-offset.pak");
        using (var stream = File.Create(pak))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(uint.MaxValue);
            writer.Write(1u);
            writer.Write((byte)0x12);
        }

        using var reader = new PakReader(pak);
        Assert.Throws<InvalidDataException>(() => reader.Parse());
    }

    [Fact]
    public void Extract_DoesNotWriteOutsideDestination()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("source.bin");
        string destination = temp.Combine("output");
        string escaped = temp.Combine("escaped.bin");
        File.WriteAllBytes(source, [1, 2, 3]);

        using var reader = new PakReader(source);
        reader.Entries.Add(new PakFileEntry
        {
            NameRaw = System.Text.Encoding.ASCII.GetBytes("../escaped.bin"),
            NameLength = 14,
            Type = PakFileEntryType.Raw,
            Size = 3,
            CompressSize = 3,
            Offset = 0
        });

        Assert.Throws<InvalidDataException>(() => reader.Extract("*", destination));
        Assert.False(File.Exists(escaped));
    }

    [Fact]
    public void UpdateReader_RejectsTruncatedCiphertext()
    {
        using var temp = new TemporaryDirectory();
        string update = temp.Combine("truncated.dat");
        File.WriteAllBytes(update, [1, 2, 3]);

        Assert.ThrowsAny<Exception>(() => new UpdateReader(UpdateKeys.JP).ReadUpdateList(update));
        Assert.Single(Directory.GetFiles(temp.Path));
    }
}
