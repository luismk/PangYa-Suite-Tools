using System.Text;
using PangyaAPI.PAK.Flags;
using PangyaAPI.Utilities.Cryptography;
using PangyaAPI.Utilities.Logging;

namespace PangyaAPI.PAK.Models;

public class PakWriter
{
    public ILogSink? LogSink { get; set; }
    public byte PakVersion = 0x12;
    public uint kXorKey = 0x71u;
    public string Author { get; set; } = "PangyaAPI.PAK";
    public PakFileEntryVersion EntryVersion { get; set; } = PakFileEntryVersion.V3;
    public PakFileEntryType EntryType { get; set; } = PakFileEntryType.LZ772;
    public byte CompressLevel { get; set; } = 5;
    public uint[] LocationKeys { get; set; } = PakKeys.JP;
    public Encoding FileNameEncoding { get; set; } = PakFileNameEncoding.CreateDefault();

    /// <summary>Zero selects an adaptive value capped at four workers.</summary>
    public int MaxDegreeOfParallelism { get; set; }

    /// <summary>Zero selects one eighth of available managed memory, clamped to 64-512 MiB.</summary>
    public long MaxBufferedBytes { get; set; }
    internal bool PreserveExistingPayloadTypes { get; set; }

    internal readonly record struct BuildItem(
        bool IsDirectory,
        string ArchivePath,
        long EstimatedBytes,
        Func<CancellationToken, byte[]>? ReadData,
        PakFileEntryType? ExistingType = null,
        uint ExistingSize = 0,
        Func<CancellationToken, byte[]>? ReadCompressedData = null);

    private readonly record struct PreparedItem(
        bool IsDirectory, string ArchivePath, byte[] NameField,
        PakFileEntryType Type, byte[] Data, uint Size);

    public void CreateFromDirectory(string sourceDir, string outputPath, Action<string>? log = null) =>
        CreateFromDirectory(sourceDir, outputPath, log, CancellationToken.None);

    public void CreateFromDirectory(string sourceDir, string outputPath, Action<string>? log,
                                    CancellationToken cancellationToken)
    {
        string dir = sourceDir.TrimEnd('/', '\\');
        string baseDir = Path.GetDirectoryName(dir) ?? dir;
        var items = new List<BuildItem> { DirectoryItem(Path.GetFileName(dir)) };
        items.AddRange(EnumerateDirectory(dir, baseDir));
        CreateAtomically(items, outputPath, log, cancellationToken);
    }

    public void CreateFromDirectoryContents(string sourceDir, string outputPath, Action<string>? log = null) =>
        CreateFromDirectoryContents(sourceDir, outputPath, log, CancellationToken.None);

    public void CreateFromDirectoryContents(string sourceDir, string outputPath, Action<string>? log,
                                            CancellationToken cancellationToken)
    {
        string dir = sourceDir.TrimEnd('/', '\\');
        CreateAtomically(EnumerateDirectory(dir, dir).ToList(), outputPath, log, cancellationToken);
    }

    public void CreateFromFile(string filePath, string outputPath, Action<string>? log = null) =>
        CreateFromFile(filePath, outputPath, log, CancellationToken.None);

    public void CreateFromFile(string filePath, string outputPath, Action<string>? log,
                               CancellationToken cancellationToken)
    {
        var info = new FileInfo(filePath);
        CreateAtomically([FileItem(info, info.Name)], outputPath, log, cancellationToken);
    }

    private IEnumerable<BuildItem> EnumerateDirectory(string sourceDir, string baseDir)
    {
        foreach (string path in Directory.EnumerateFileSystemEntries(sourceDir, "*", SearchOption.AllDirectories)
                                         .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            string relative = Path.GetRelativePath(baseDir, path).Replace('\\', '/');
            if (Directory.Exists(path)) yield return DirectoryItem(relative);
            else yield return FileItem(new FileInfo(path), relative);
        }
    }

    private static BuildItem DirectoryItem(string archivePath) =>
        new(true, archivePath.Replace('\\', '/'), 0, null);

    private static BuildItem FileItem(FileInfo file, string archivePath) =>
        new(false, archivePath.Replace('\\', '/'), file.Length,
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return File.ReadAllBytes(file.FullName);
            });

    private void CreateAtomically(IReadOnlyList<BuildItem> items, string outputPath,
                                  Action<string>? log, CancellationToken cancellationToken)
    {
        string fullOutputPath = Path.GetFullPath(outputPath);
        string? directory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string candidate = fullOutputPath + $".{Guid.NewGuid():N}.tmp";

        try
        {
            Report(log, $"Creating PAK '{fullOutputPath}' from {items.Count} items using {FileNameEncoding.EncodingName} filenames, {EntryVersion}/{EntryType}.");
            WriteCandidate(items, candidate, log, cancellationToken);
            Report(log, $"Wrote temporary PAK candidate '{candidate}'.");
            ValidateCandidate(candidate);
            Report(log, "Validated the PAK candidate successfully.");
            PromoteCandidate(candidate, fullOutputPath);
            Report(log, $"Created PAK successfully: {fullOutputPath} ({new FileInfo(fullOutputPath).Length} bytes).");
        }
        catch (Exception ex)
        {
            Report(log, $"PAK creation failed: {ex.Message}", LogSeverity.Error);
            throw;
        }
        finally
        {
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
                Report(log, $"Removed temporary PAK candidate '{candidate}'.");
            }
        }
    }

    private void Report(Action<string>? callback, string message, LogSeverity severity = LogSeverity.Information)
    {
        callback?.Invoke(message);
        LogSink?.Log("PAK Writer", message, severity);
    }

    internal void WriteCandidate(IReadOnlyList<BuildItem> items, string candidatePath,
                                 Action<string>? log = null,
                                 CancellationToken cancellationToken = default,
                                 Action<string, int, int>? onProgress = null)
    {
        if (MaxDegreeOfParallelism < 0) throw new ArgumentOutOfRangeException(nameof(MaxDegreeOfParallelism));
        if (MaxBufferedBytes < 0) throw new ArgumentOutOfRangeException(nameof(MaxBufferedBytes));
        int workers = MaxDegreeOfParallelism > 0
            ? MaxDegreeOfParallelism
            : Math.Max(1, Math.Min(Environment.ProcessorCount, 4));
        long budget = MaxBufferedBytes > 0 ? MaxBufferedBytes : GetAdaptiveBufferBudget();

        var metadata = new List<PakFileEntry>(items.Count);
        using var output = new FileStream(candidatePath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                                          1024 * 1024, FileOptions.SequentialScan);
        using var writer = new BinaryWriter(output, Encoding.ASCII, leaveOpen: true);

        int completed = 0;
        for (int start = 0; start < items.Count;)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int count = SelectBatchSize(items, start, workers, budget);
            var prepared = new PreparedItem[count];

            Parallel.For(0, count, new ParallelOptions
            {
                MaxDegreeOfParallelism = workers,
                CancellationToken = cancellationToken
            }, index => prepared[index] = Prepare(items[start + index], cancellationToken));
            if ((start + count) % 50 == 0 || start + count == items.Count)
                onProgress?.Invoke("compression", start + count, items.Count);

            foreach (PreparedItem item in prepared)
            {
                cancellationToken.ThrowIfCancellationRequested();
                WritePayloadAndMetadata(writer, item, metadata);
                completed++;
                if (completed % 50 == 0 || completed == items.Count)
                    onProgress?.Invoke("writing", completed, items.Count);
            }
            start += count;
        }

        cancellationToken.ThrowIfCancellationRequested();
        WriteTableAndFooter(writer, metadata);
        writer.Flush();
        output.Flush(flushToDisk: true);
    }

    private PreparedItem Prepare(BuildItem item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] nameField = EncodeName(item.ArchivePath);
        if (item.IsDirectory)
            return new(true, item.ArchivePath, nameField, PakFileEntryType.Directory, [], 0);

        PakFileEntryType type = SelectType(item.ArchivePath);
        if (PreserveExistingPayloadTypes && item.ExistingType.HasValue)
            type = item.ExistingType.Value;
        else if (item.ExistingSize == 0 && item.ExistingType == PakFileEntryType.Raw)
            type = PakFileEntryType.Raw;
        if (item.ReadCompressedData != null && item.ExistingType == type)
        {
            byte[] compressed = item.ReadCompressedData(cancellationToken);
            return new(false, item.ArchivePath, nameField, type, compressed, item.ExistingSize);
        }

        byte[] source = item.ReadData?.Invoke(cancellationToken)
            ?? throw new InvalidDataException($"No data source exists for {item.ArchivePath}.");
        if (source.LongLength > uint.MaxValue)
            throw new InvalidDataException($"Entry exceeds the PAK size limit: {item.ArchivePath}");

        if (source.Length == 0) type = PakFileEntryType.Raw;
        byte[] data = type switch
        {
            PakFileEntryType.Raw => source,
            PakFileEntryType.LZ77 => Lz77.Compress(source, CompressLevel, null)
                ?? throw new InvalidDataException($"Failed to compress {item.ArchivePath}."),
            PakFileEntryType.LZ772 => Lz772.Compress(source, CompressLevel, null)
                ?? throw new InvalidDataException($"Failed to compress {item.ArchivePath}."),
            _ => throw new InvalidDataException($"Unsupported entry type: {type}.")
        };
        return new(false, item.ArchivePath, nameField, type, data, checked((uint)source.Length));
    }

    private PakFileEntryType SelectType(string archivePath)
    {
        string extension = Path.GetExtension(archivePath);
        return extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            ? PakFileEntryType.Raw : EntryType;
    }

    private byte[] EncodeName(string archivePath)
    {
        byte[] bytes = PakFileEntry.EncodeName(archivePath.Replace('\\', '/'), FileNameEncoding);
        int length = EntryVersion == PakFileEntryVersion.V3 ? ((bytes.Length + 7) / 8) * 8 : bytes.Length + 1;
        if (length > byte.MaxValue)
            throw new InvalidDataException($"Entry name is too long for the PAK format: {archivePath}");
        byte[] field = new byte[length];
        bytes.CopyTo(field, 0);
        return field;
    }

    private void WritePayloadAndMetadata(BinaryWriter writer, PreparedItem item, List<PakFileEntry> metadata)
    {
        if (writer.BaseStream.Position > uint.MaxValue)
            throw new InvalidDataException("PAK payload offsets exceed the format limit.");
        uint offset = checked((uint)writer.BaseStream.Position);
        if (!item.IsDirectory) writer.Write(item.Data);
        if (item.Data.LongLength > uint.MaxValue)
            throw new InvalidDataException($"Compressed entry exceeds the PAK limit: {item.ArchivePath}");

        var entry = new PakFileEntry
        {
            NameLength = checked((byte)(EntryVersion == PakFileEntryVersion.V3
                ? item.NameField.Length : item.NameField.Length - 1)),
            Type = item.Type,
            Version = EntryVersion,
            Offset = offset,
            CompressSize = checked((uint)item.Data.Length),
            Size = item.Size
        };
        entry.SetRawNameForWrite(item.NameField);
        metadata.Add(entry);
    }

    private void WriteTableAndFooter(BinaryWriter writer, IReadOnlyList<PakFileEntry> entries)
    {
        byte[] authorBytes = Encoding.ASCII.GetBytes(Author);
        if (authorBytes.Length > ushort.MaxValue) throw new InvalidDataException("PAK author is too long.");
        writer.Write(authorBytes);
        writer.Write((ushort)((authorBytes.Length >> 8) | (authorBytes.Length << 8)));
        if (writer.BaseStream.Position > uint.MaxValue) throw new InvalidDataException("PAK entry table offset exceeds the format limit.");
        uint tableOffset = checked((uint)writer.BaseStream.Position);

        foreach (PakFileEntry entry in entries)
        {
            writer.Write(entry.NameLength);
            writer.Write((byte)(((byte)entry.Version << 4) | (byte)entry.Type));
            uint offset = entry.Offset;
            uint size = entry.Size;
            byte[] name = (byte[])entry.NameRaw.Clone();

            if (EntryVersion != PakFileEntryVersion.Raw && EntryVersion < PakFileEntryVersion.V3)
            {
                size ^= kXorKey;
                for (int index = 0; index < entry.NameLength; index++) name[index] ^= (byte)kXorKey;
            }
            else if (EntryVersion == PakFileEntryVersion.V3)
            {
                ulong packed = Xtea.Encrypt(LocationKeys, ((ulong)size << 32) | offset);
                size = (uint)(packed >> 32);
                offset = (uint)packed;
                for (int index = 0; index < name.Length; index += 8)
                {
                    byte[] block = BitConverter.GetBytes(Xtea.Encrypt(LocationKeys, BitConverter.ToUInt64(name, index)));
                    Buffer.BlockCopy(block, 0, name, index, 8);
                }
            }

            writer.Write(offset);
            writer.Write(entry.CompressSize);
            writer.Write(size);
            writer.Write(name);
        }

        writer.Write(tableOffset);
        writer.Write(checked((uint)entries.Count));
        writer.Write(PakVersion);
    }

    private void ValidateCandidate(string path)
    {
        using var reader = new PakReader(path, FileNameEncoding);
        reader.Parse(EntryVersion == PakFileEntryVersion.V3 ? LocationKeys : null);
    }

    internal static void PromoteCandidate(string candidate, string destination)
    {
        if (File.Exists(destination)) File.Replace(candidate, destination, null, ignoreMetadataErrors: true);
        else File.Move(candidate, destination);
    }

    private static int SelectBatchSize(IReadOnlyList<BuildItem> items, int start, int workers, long budget)
    {
        int count = 0;
        long bytes = 0;
        while (start + count < items.Count && count < workers)
        {
            long sourceBytes = Math.Max(1, items[start + count].EstimatedBytes);
            long estimate = sourceBytes > long.MaxValue / 3 ? long.MaxValue : sourceBytes * 3;
            if (count > 0 && bytes + estimate > budget) break;
            bytes = checked(bytes + estimate);
            count++;
        }
        return Math.Max(1, count);
    }

    private static long GetAdaptiveBufferBudget()
    {
        long available = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        if (available <= 0) available = 512L * 1024 * 1024;
        return Math.Clamp(available / 8, 64L * 1024 * 1024, 512L * 1024 * 1024);
    }
}
