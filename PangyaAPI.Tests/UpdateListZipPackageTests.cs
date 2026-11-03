using PangyaAPI.UpdateList.Models;
using PangyaAPI.Utilities.Cryptography;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace PangyaAPI.Tests;

public sealed class UpdateListZipPackageTests
{
    [Fact]
    public void GenerateFromDirectory_ReportsEveryProcessedRelativePath()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(Path.Combine(source, "data"));
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(source, "root.dll"), "root");
        File.WriteAllText(Path.Combine(source, "data", "nested.dll"), "nested");
        var processedFiles = new ConcurrentBag<string>();

        new UpdateMaker().GenerateFromDirectory(
            source,
            Path.Combine(outputDirectory, "updatelist"),
            UpdateKeys.JP,
            "patch",
            onFileProcessing: processedFiles.Add);

        Assert.Equal(
            [Path.Combine("data", "nested.dll"), "root.dll"],
            processedFiles.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public void GenerateFromDirectory_ZipDisabledPreservesLegacyPackageMetadata()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(source, "client.dll"), "client");
        string output = Path.Combine(outputDirectory, "updatelist");

        new UpdateMaker().GenerateFromDirectory(source, output, UpdateKeys.JP, "patch");

        UpdateEntry entry = Assert.Single(
            new UpdateReader(UpdateKeys.JP).ReadUpdateList(output).Entries);
        Assert.Equal(717469, entry.psize);
        Assert.False(File.Exists(Path.Combine(outputDirectory, "client.dll.zip")));
    }

    [Fact]
    public void GenerateFromDirectory_ZipEnabledCreatesSingleEntryAndRecordsActualSize()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outputDirectory);
        byte[] contents = Enumerable.Range(0, 4096).Select(index => (byte)(index % 251)).ToArray();
        File.WriteAllBytes(Path.Combine(source, "client.dll"), contents);
        string output = Path.Combine(outputDirectory, "updatelist");
        var progress = new List<(int Done, int Total)>();

        new UpdateMaker().GenerateFromDirectory(
            source, output, UpdateKeys.JP, "patch",
            onProgress: (done, total) => progress.Add((done, total)),
            createZipPackages: true);

        string package = Path.Combine(outputDirectory, "client.dll_1.zip");
        using var archive = ZipFile.OpenRead(package);
        ZipArchiveEntry archiveEntry = Assert.Single(archive.Entries);
        Assert.Equal("client.dll", archiveEntry.FullName);
        using Stream entryStream = archiveEntry.Open();
        using var extracted = new MemoryStream();
        entryStream.CopyTo(extracted);
        Assert.Equal(contents, extracted.ToArray());

        UpdateEntry entry = Assert.Single(
            new UpdateReader(UpdateKeys.JP).ReadUpdateList(output).Entries);
        Assert.Equal("client.dll_1.zip", entry.pname);
        Assert.Equal(new FileInfo(package).Length, entry.psize);
        Assert.Contains((2, 2), progress);
    }

    [Fact]
    public void GenerateFromDirectory_ReleasesGeneratedUpdateListAndZipHandles()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(source, "client.dll"), "client");
        string output = Path.Combine(outputDirectory, "updatelist");

        new UpdateMaker().GenerateFromDirectory(
            source,
            output,
            UpdateKeys.JP,
            "patch",
            createZipPackages: true);

        AssertCanOpenExclusively(output);
        AssertCanOpenExclusively(Path.Combine(outputDirectory, "client.dll_1.zip"));
    }

    [Fact]
    public void GenerateFromDirectory_IdenticalPackageIsRetained()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(source, "client.dll"), "unchanged");
        string output = Path.Combine(outputDirectory, "updatelist");
        var maker = new UpdateMaker();
        maker.GenerateFromDirectory(
            source, output, UpdateKeys.JP, "patch", createZipPackages: true);
        string package = Path.Combine(outputDirectory, "client.dll_1.zip");
        string originalHash = Sha256.ComputeFileHex(package);
        DateTime retainedTimestamp = new(2001, 2, 3, 4, 5, 6, DateTimeKind.Local);
        File.SetLastWriteTime(package, retainedTimestamp);

        maker.GenerateFromDirectory(
            source, output, UpdateKeys.JP, "patch", createZipPackages: true);

        Assert.Equal(originalHash, Sha256.ComputeFileHex(package));
        Assert.Equal(retainedTimestamp, File.GetLastWriteTime(package));
    }

    [Fact]
    public void GenerateFromDirectory_ChangedSameLengthSourceReplacesPackage()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outputDirectory);
        string sourceFile = Path.Combine(source, "client.dll");
        File.WriteAllText(sourceFile, "AAAA");
        DateTime timestamp = File.GetLastWriteTime(sourceFile);
        string output = Path.Combine(outputDirectory, "updatelist");
        var maker = new UpdateMaker();
        maker.GenerateFromDirectory(
            source, output, UpdateKeys.JP, "patch", createZipPackages: true);
        string package = Path.Combine(outputDirectory, "client.dll_1.zip");
        string firstHash = Sha256.ComputeFileHex(package);

        File.WriteAllText(sourceFile, "BBBB");
        File.SetLastWriteTime(sourceFile, timestamp);
        maker.GenerateFromDirectory(
            source, output, UpdateKeys.JP, "patch", createZipPackages: true);

        Assert.NotEqual(firstHash, Sha256.ComputeFileHex(package));
        using var archive = ZipFile.OpenRead(package);
        using var reader = new StreamReader(Assert.Single(archive.Entries).Open());
        Assert.Equal("BBBB", reader.ReadToEnd());
    }

    [Fact]
    public void GenerateFromDirectory_CorruptPackageIsReplacedAndStalePackagesRemain()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(source, "client.dll"), "client");
        string package = Path.Combine(outputDirectory, "client.dll_1.zip");
        string stalePackage = Path.Combine(outputDirectory, "stale.zip");
        File.WriteAllText(package, "not a zip");
        File.WriteAllText(stalePackage, "retain me");

        new UpdateMaker().GenerateFromDirectory(
            source,
            Path.Combine(outputDirectory, "updatelist"),
            UpdateKeys.JP,
            "patch",
            createZipPackages: true);

        using var archive = ZipFile.OpenRead(package);
        Assert.Single(archive.Entries);
        Assert.Equal("retain me", File.ReadAllText(stalePackage));
    }

    [Fact]
    public void GenerateFromDirectory_DirectoryNamesAreFlattenedIntoPackageNames()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(Path.Combine(source, "GameGuard"));
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(source, "GameGuard.des"), "root");
        File.WriteAllText(Path.Combine(source, "GameGuard", "GameGuard.des"), "nested");
        string output = Path.Combine(outputDirectory, "updatelist");

        new UpdateMaker().GenerateFromDirectory(
            source, output, UpdateKeys.JP, "patch", createZipPackages: true);

        string[] packages = Directory.EnumerateFiles(outputDirectory, "*.zip")
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.Equal(["GameGuard.des_1.zip", "GameGuard_GameGuard.des_2.zip"], packages);
        List<UpdateEntry> entries =
            new UpdateReader(UpdateKeys.JP).ReadUpdateList(output).Entries;
        Assert.Equal(2, entries.Count);
        Assert.Equal(
            ["GameGuard.des_1.zip", "GameGuard_GameGuard.des_2.zip"],
            entries.Select(entry => entry.pname)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
        Assert.All(entries, entry =>
            Assert.Equal(
                new FileInfo(Path.Combine(outputDirectory, entry.pname)).Length,
                entry.psize));
        Assert.Empty(Directory.EnumerateFiles(outputDirectory, "*.tmp"));
    }

    [Fact]
    public void GenerateFromDirectory_OrderSuffixesDisambiguateFlattenedNames()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(Path.Combine(source, "one_two"));
        Directory.CreateDirectory(Path.Combine(source, "one"));
        Directory.CreateDirectory(outputDirectory);
        File.WriteAllText(Path.Combine(source, "one_two", "client.dll"), "one");
        File.WriteAllText(Path.Combine(source, "one", "two_client.dll"), "two");
        string output = Path.Combine(outputDirectory, "updatelist");

        new UpdateMaker().GenerateFromDirectory(
            source, output, UpdateKeys.JP, "patch", createZipPackages: true);

        string[] packages = Directory.EnumerateFiles(outputDirectory, "*.zip")
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        Assert.Equal(
            ["one_two_client.dll_1.zip", "one_two_client.dll_2.zip"],
            packages);
        List<UpdateEntry> entries =
            new UpdateReader(UpdateKeys.JP).ReadUpdateList(output).Entries;
        Assert.Equal(2, entries.Count);
        Assert.Equal(
            ["one_two_client.dll_1.zip", "one_two_client.dll_2.zip"],
            entries.Select(entry => entry.pname)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray());
        Assert.All(entries, entry =>
            Assert.Equal(
                new FileInfo(Path.Combine(outputDirectory, entry.pname)).Length,
                entry.psize));
        Assert.Empty(Directory.EnumerateFiles(outputDirectory, "*.tmp"));
    }

    [Fact]
    public void GenerateFromDirectory_PackagingErrorCleansCandidateAndDoesNotWriteUpdateList()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("client");
        string outputDirectory = temp.Combine("published");
        Directory.CreateDirectory(source);
        Directory.CreateDirectory(outputDirectory);
        string sourceFile = Path.Combine(source, "client.dll");
        File.WriteAllText(sourceFile, "client");
        string output = Path.Combine(outputDirectory, "updatelist");
        FileStream? lockedSource = null;

        try
        {
            Assert.Throws<IOException>(() => new UpdateMaker().GenerateFromDirectory(
                source,
                output,
                UpdateKeys.JP,
                "patch",
                onProgress: (done, _) =>
                {
                    if (done == 1)
                    {
                        lockedSource = new FileStream(
                            sourceFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    }
                },
                createZipPackages: true));
        }
        finally
        {
            lockedSource?.Dispose();
        }

        Assert.False(File.Exists(output));
        Assert.Empty(Directory.EnumerateFiles(outputDirectory, "*.zip"));
        Assert.Empty(Directory.EnumerateFiles(outputDirectory, "*.tmp"));
    }

    private static void AssertCanOpenExclusively(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(stream.CanRead);
    }
}
