using System.IO;
using System.Security.Cryptography;
using PangyaAPI.PAK.Models;

namespace PangYa_Suite_Tools;

internal sealed class PakFileEditSession : IDisposable
{
    internal readonly record struct ArchiveStamp(string Path, long Size, DateTime Created, DateTime Modified);
    internal sealed record Revision(string Path, long Length, byte[] Hash, int Generation, ArchiveStamp WorkingStamp);

    private readonly FileSystemWatcher _watcher;
    private int _generation;
    private long _lastChange;

    public PakFileEditSession(PakReader reader, int ordinal, string workingPath, ArchiveStamp[] archive, byte[] hash)
    {
        Reader = reader;
        Ordinal = ordinal;
        WorkingPath = workingPath;
        DirectoryPath = Path.GetDirectoryName(workingPath) ?? throw new ArgumentException(nameof(workingPath));
        Archive = archive;
        AppliedHash = ObservedHash = hash;
        WorkingStamp = Stamp(workingPath);
        _watcher = new FileSystemWatcher(DirectoryPath, Path.GetFileName(workingPath))
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += (_, _) => SignalChange();
        _watcher.Created += (_, _) => SignalChange();
        _watcher.Deleted += (_, _) => SignalChange();
        _watcher.Renamed += (_, _) => SignalChange();
        _watcher.Error += (_, _) => SignalChange();
        try
        {
            _watcher.EnableRaisingEvents = true;
        }
        catch
        {
            _watcher.Dispose();
            throw;
        }
    }

    public PakReader Reader { get; set; }
    public int Ordinal { get; }
    public string WorkingPath { get; }
    public string DirectoryPath { get; }
    public ArchiveStamp[] Archive { get; set; }
    public byte[] AppliedHash { get; set; }
    public byte[] ObservedHash { get; private set; }
    public int ObservedGeneration { get; private set; }
    public ArchiveStamp WorkingStamp { get; private set; }
    public int Generation => Volatile.Read(ref _generation);
    public bool IsReady => Generation != ObservedGeneration &&
        Environment.TickCount64 - Interlocked.Read(ref _lastChange) >= 500;

    private void SignalChange()
    {
        Interlocked.Exchange(ref _lastChange, Environment.TickCount64);
        Interlocked.Increment(ref _generation);
    }

    public static ArchiveStamp[] CaptureArchive(PakReader reader)
    {
        string path = reader.ArchivePath;
        if (!reader.IsPspArchive)
            return [Stamp(path)];

        string folder = Path.GetDirectoryName(Path.GetFullPath(path)) ?? throw new InvalidDataException();
        return [Stamp(Path.Combine(folder, PspPakArchive.IndexFileName)),
                Stamp(Path.Combine(folder, PspPakArchive.DataFileName))];
    }

    private static ArchiveStamp Stamp(string path)
    {
        var file = new FileInfo(path);
        return new ArchiveStamp(file.FullName, file.Length, file.CreationTimeUtc, file.LastWriteTimeUtc);
    }

    public bool MatchesArchive()
    {
        try { return Archive.SequenceEqual(CaptureArchive(Reader)); }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public bool CanCleanWorkingCopy()
    {
        try
        {
            return Generation == ObservedGeneration && Stamp(WorkingPath) == WorkingStamp &&
                AppliedHash.AsSpan().SequenceEqual(ObservedHash);
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public bool Observe(Revision revision)
    {
        ObservedGeneration = revision.Generation;
        WorkingStamp = revision.WorkingStamp;
        bool newSave = !revision.Hash.AsSpan().SequenceEqual(ObservedHash);
        ObservedHash = revision.Hash;
        return newSave && !revision.Hash.AsSpan().SequenceEqual(AppliedHash);
    }

    public void SkipGeneration(int generation) => ObservedGeneration = generation;

    public Revision CaptureRevision()
    {
        int generation = Generation;
        string snapshot = Path.Combine(DirectoryPath, Guid.NewGuid().ToString("N") + ".snapshot");
        try
        {
            using var source = new FileStream(WorkingPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (source.Length > Array.MaxLength)
                throw new InvalidDataException();
            using var output = new FileStream(snapshot, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            source.CopyTo(output);
            output.Position = 0;
            return new Revision(snapshot, output.Length, SHA256.HashData(output), generation, Stamp(WorkingPath));
        }
        catch
        {
            TryDelete(snapshot);
            throw;
        }
    }

    public static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public void Dispose() => _watcher.Dispose();
}
