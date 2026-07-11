using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;

namespace PangyaAPI.Tests;

public sealed class PakStreamingTests
{
    [Fact]
    public void Create_IsDeterministicAndCancellationPreservesDestination()
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string first = temp.Combine("first.pak");
        string second = temp.Combine("second.pak");
        var writer = NewWriter(PakKeys.JP);

        writer.CreateFromDirectoryContents(source, first);
        writer.CreateFromDirectoryContents(source, second);
        Assert.Equal(File.ReadAllBytes(first), File.ReadAllBytes(second));

        byte[] original = File.ReadAllBytes(first);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() =>
            writer.CreateFromDirectoryContents(source, first, null, cancelled.Token));
        Assert.Equal(original, File.ReadAllBytes(first));
        Assert.Empty(Directory.GetFiles(temp.Path, "*.tmp"));
    }

    [Fact]
    public void ByteBudget_BoundsConcurrentDataProviders()
    {
        using var temp = new TemporaryDirectory();
        int active = 0;
        int maximum = 0;
        var items = Enumerable.Range(0, 8).Select(index => new PakWriter.BuildItem(
            false, $"file{index}.bin", 40, cancellationToken =>
            {
                int current = Interlocked.Increment(ref active);
                int observed;
                do
                {
                    observed = maximum;
                    if (current <= observed) break;
                } while (Interlocked.CompareExchange(ref maximum, current, observed) != observed);
                Thread.Sleep(15);
                Interlocked.Decrement(ref active);
                return Enumerable.Repeat((byte)index, 40).ToArray();
            })).ToList();

        var writer = NewWriter(PakKeys.JP);
        writer.MaxDegreeOfParallelism = 4;
        writer.MaxBufferedBytes = 240;
        writer.WriteCandidate(items, temp.Combine("bounded.pak"));

        Assert.InRange(maximum, 1, 2);
    }

    [Fact]
    public void InjectAndRemove_RebuildWithoutChangingUnrelatedContents()
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string pak = temp.Combine("edit.pak");
        NewWriter(PakKeys.JP).CreateFromDirectoryContents(source, pak);
        byte[] originalPak = File.ReadAllBytes(pak);

        string replacement = temp.Combine("data.bin");
        byte[] replacementBytes = [9, 8, 7, 6, 5];
        File.WriteAllBytes(replacement, replacementBytes);
        string added = temp.Combine("added.bin");
        File.WriteAllBytes(added, [3, 1, 4]);
        using (var reader = Open(pak, PakKeys.JP))
            PakManager.InjectFiles(pak, reader,
                [new PakInjectItem(replacement, null), new PakInjectItem(added, "mods/sub")],
                Options(PakKeys.JP), SaveBck: true);
        Assert.Equal(originalPak, File.ReadAllBytes(pak + ".bak"));

        using (var reader = Open(pak, PakKeys.JP))
        {
            PakFileEntry changed = reader.Entries.Single(entry => entry.Name.EndsWith("data.bin"));
            Assert.Equal(replacementBytes, reader.ExtractEntryToBytes(changed));
            Assert.Contains(reader.Entries, entry => entry.Name.EndsWith("keep.txt"));
            Assert.Contains(reader.Entries, entry => entry.Type == PakFileEntryType.Directory && entry.Name == "mods");
            Assert.Contains(reader.Entries, entry => entry.Type == PakFileEntryType.Directory && entry.Name == "mods\\sub");
            Assert.Contains(reader.Entries, entry => entry.Name == "mods\\sub\\added.bin");
        }

        using (var reader = Open(pak, PakKeys.JP))
        {
            string removeName = reader.Entries.Single(entry => entry.Name.EndsWith("data.bin")).Name;
            PakManager.RemoveFiles(pak, reader, [removeName], Options(PakKeys.JP));
        }
        using var finalReader = Open(pak, PakKeys.JP);
        Assert.DoesNotContain(finalReader.Entries, entry => entry.Name.EndsWith("data.bin"));
        Assert.Contains(finalReader.Entries, entry => entry.Name.EndsWith("keep.txt"));
    }

    [Fact]
    public void InjectWithExplicitFolder_DoesNotReplaceSameNamedEntryElsewhere()
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string pak = temp.Combine("explicit-folder.pak");
        NewWriter(PakKeys.JP).CreateFromDirectoryContents(source, pak);

        string replacement = temp.Combine("data.bin");
        byte[] replacementBytes = [9, 8, 7];
        File.WriteAllBytes(replacement, replacementBytes);

        byte[] originalBytes;
        using (var reader = Open(pak, PakKeys.JP))
        {
            originalBytes = Assert.IsType<byte[]>(
                reader.ExtractEntryToBytes(FindEntry(reader, "nested/data.bin")));
            PakManager.InjectFiles(pak, reader,
                [new PakInjectItem(replacement, "selected/folder")], Options(PakKeys.JP));
        }

        using var updated = Open(pak, PakKeys.JP);
        Assert.Equal(originalBytes, updated.ExtractEntryToBytes(FindEntry(updated, "nested/data.bin")));
        Assert.Equal(replacementBytes,
            updated.ExtractEntryToBytes(FindEntry(updated, "selected/folder/data.bin")));
    }

    [Theory]
    [InlineData(PakFileEntryType.Raw)]
    [InlineData(PakFileEntryType.LZ77)]
    [InlineData(PakFileEntryType.LZ772)]
    public void RenameFile_RebuildsArchiveWithoutChangingPayloadType(PakFileEntryType type)
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string pak = temp.Combine($"{type}-rename.pak");
        NewWriter(PakKeys.JP, type).CreateFromDirectoryContents(source, pak);

        byte[] sourceBytes = File.ReadAllBytes(Path.Combine(source, "nested", "data.bin"));
        byte[] unchangedBytes = File.ReadAllBytes(Path.Combine(source, "keep.txt"));
        byte[] originalCompressed;
        using (var reader = Open(pak, PakKeys.JP))
        {
            PakFileEntry original = FindEntry(reader, "nested/data.bin");
            originalCompressed = reader.ReadCompressedEntryBytes(original);
            Assert.True(PakManager.Rename(pak, reader, original.Name, "renamed.bin",
                Options(PakKeys.JP, type), null, null, SaveBck: false, CancellationToken.None));
        }

        using var renamed = Open(pak, PakKeys.JP);
        Assert.DoesNotContain(renamed.Entries, entry => Normalize(entry.Name) == "nested/data.bin");
        PakFileEntry renamedEntry = FindEntry(renamed, "nested/renamed.bin");
        Assert.Equal(type, renamedEntry.Type);
        Assert.Equal(sourceBytes, renamed.ExtractEntryToBytes(renamedEntry));
        Assert.Equal(originalCompressed, renamed.ReadCompressedEntryBytes(renamedEntry));

        PakFileEntry unchanged = FindEntry(renamed, "keep.txt");
        Assert.Equal(unchangedBytes, renamed.ExtractEntryToBytes(unchanged));
    }

    [Fact]
    public void RenameMissingSource_ReturnsFalseAndLeavesArchiveUnchanged()
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string pak = temp.Combine("missing-rename.pak");
        NewWriter(PakKeys.JP).CreateFromDirectoryContents(source, pak);
        byte[] original = File.ReadAllBytes(pak);

        using var reader = Open(pak, PakKeys.JP);
        Assert.False(PakManager.Rename(pak, reader, "missing.bin", "renamed.bin",
            Options(PakKeys.JP), null, null, SaveBck: false, CancellationToken.None));

        Assert.Equal(original, File.ReadAllBytes(pak));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("bad/name.bin")]
    [InlineData("bad\\name.bin")]
    [InlineData(".")]
    [InlineData(" name.bin")]
    public void RenameInvalidLeafName_ThrowsAndLeavesArchiveUnchanged(string newName)
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string pak = temp.Combine("invalid-rename.pak");
        NewWriter(PakKeys.JP).CreateFromDirectoryContents(source, pak);
        byte[] original = File.ReadAllBytes(pak);

        using var reader = Open(pak, PakKeys.JP);
        Assert.Throws<ArgumentException>(() => PakManager.Rename(pak, reader, "nested/data.bin", newName,
            Options(PakKeys.JP), null, null, SaveBck: false, CancellationToken.None));

        Assert.Equal(original, File.ReadAllBytes(pak));
    }

    [Fact]
    public void RenameConflict_ThrowsAndLeavesArchiveUnchanged()
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        File.WriteAllBytes(Path.Combine(source, "nested", "collision.bin"), [4, 5, 6]);
        string pak = temp.Combine("collision-rename.pak");
        NewWriter(PakKeys.JP).CreateFromDirectoryContents(source, pak);
        byte[] original = File.ReadAllBytes(pak);

        using var reader = Open(pak, PakKeys.JP);
        Assert.Throws<InvalidDataException>(() => PakManager.Rename(pak, reader, "nested/data.bin", "COLLISION.bin",
            Options(PakKeys.JP), null, null, SaveBck: false, CancellationToken.None));

        Assert.Equal(original, File.ReadAllBytes(pak));
    }

    [Fact]
    public void RenameOverlongName_ThrowsAndRemovesCandidate()
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string pak = temp.Combine("overlong-rename.pak");
        NewWriter(PakKeys.JP).CreateFromDirectoryContents(source, pak);
        byte[] original = File.ReadAllBytes(pak);
        string overlongName = new string('a', 300) + ".bin";

        using var reader = Open(pak, PakKeys.JP);
        Assert.Throws<InvalidDataException>(() => PakManager.Rename(pak, reader, "nested/data.bin", overlongName,
            Options(PakKeys.JP), null, null, SaveBck: false, CancellationToken.None));

        Assert.Equal(original, File.ReadAllBytes(pak));
        Assert.Empty(Directory.GetFiles(temp.Path, "*.tmp"));
    }

    [Fact]
    public void RenameCancellation_PreservesOriginalAndRemovesCandidate()
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string pak = temp.Combine("cancel-rename.pak");
        NewWriter(PakKeys.JP).CreateFromDirectoryContents(source, pak);
        byte[] original = File.ReadAllBytes(pak);
        using var reader = Open(pak, PakKeys.JP);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => PakManager.Rename(
            pak, reader, "nested/data.bin", "renamed.bin", Options(PakKeys.JP),
            null, null, SaveBck: false, cancellation.Token));

        Assert.Equal(original, File.ReadAllBytes(pak));
        Assert.Empty(Directory.GetFiles(temp.Path, "*.tmp"));
    }

    [Fact]
    public void KeyChange_ReusesCompressedPayloads()
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string pak = temp.Combine("key-change.pak");
        NewWriter(PakKeys.JP).CreateFromDirectoryContents(source, pak);

        Dictionary<string, byte[]> before;
        using (var reader = Open(pak, PakKeys.JP))
        {
            before = reader.Entries.Where(entry => entry.Type != PakFileEntryType.Directory)
                .ToDictionary(entry => entry.Name, reader.ReadCompressedEntryBytes);
            PakManager.ChangeEncryptionKey(pak, reader, Options(PakKeys.EU));
        }

        using var changed = Open(pak, PakKeys.EU);
        foreach (PakFileEntry entry in changed.Entries.Where(entry => entry.Type != PakFileEntryType.Directory))
            Assert.Equal(before[entry.Name], changed.ReadCompressedEntryBytes(entry));
    }

    [Fact]
    public void CancelledRebuild_PreservesOriginalAndRemovesCandidate()
    {
        using var temp = new TemporaryDirectory();
        string source = CreateSource(temp);
        string pak = temp.Combine("cancel-rebuild.pak");
        NewWriter(PakKeys.JP).CreateFromDirectoryContents(source, pak);
        byte[] original = File.ReadAllBytes(pak);
        using var reader = Open(pak, PakKeys.JP);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => PakManager.ChangeEncryptionKey(
            pak, reader, Options(PakKeys.EU), null, null, false, cancellation.Token));

        Assert.Equal(original, File.ReadAllBytes(pak));
        Assert.Empty(Directory.GetFiles(temp.Path, "*.tmp"));
    }

    private static PakWriter NewWriter(uint[] keys, PakFileEntryType type = PakFileEntryType.LZ772) => new()
    {
        EntryVersion = PakFileEntryVersion.V3,
        EntryType = type,
        LocationKeys = keys,
        Author = "StreamingTests"
    };

    private static PakRebuildOptions Options(uint[] keys, PakFileEntryType type = PakFileEntryType.LZ772) =>
        new(PakFileEntryVersion.V3, type, 5, keys, "StreamingTests");

    private static PakReader Open(string path, uint[] keys)
    {
        var reader = new PakReader(path);
        reader.Parse(keys);
        return reader;
    }

    private static string CreateSource(TemporaryDirectory temp)
    {
        string source = temp.Combine("stream-source");
        Directory.CreateDirectory(Path.Combine(source, "nested"));
        File.WriteAllText(Path.Combine(source, "keep.txt"), new string('A', 2048));
        File.WriteAllBytes(Path.Combine(source, "nested", "data.bin"),
            Enumerable.Range(0, 4096).Select(index => (byte)(index * 31)).ToArray());
        File.WriteAllBytes(Path.Combine(source, "empty.bin"), []);
        File.WriteAllBytes(Path.Combine(source, "sound.wav"), Enumerable.Range(0, 257).Select(index => (byte)index).ToArray());
        return source;
    }

    private static PakFileEntry FindEntry(PakReader reader, string normalizedPath) =>
        reader.Entries.Single(entry => Normalize(entry.Name) == normalizedPath);

    private static string Normalize(string path) => path.Replace('\\', '/');
}
