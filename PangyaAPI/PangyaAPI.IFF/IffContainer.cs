using System.Buffers.Binary;
using System.IO.Compression;
using PangyaAPI.PAK.Models;
using PangyaAPI.Utilities.Cryptography;

namespace PangyaAPI.IFF;

public enum IffContainerKind { LooseFile, ZipArchive, XorZipArchive, EncryptedZipArchive }

public sealed record IffContainerOptions(bool LeaveOpen = false, string? TemporaryDirectory = null);

public sealed record IffContainerSaveOptions(IffContainerKind Kind, string? EncryptionRegion = null);

public sealed record IffContainerEntry(string Name, long Length, Func<CancellationToken, ValueTask<Stream>> OpenAsync);

public sealed class IffContainer : IDisposable, IAsyncDisposable
{
    private readonly string _sourcePath;
    private readonly FileStream? _archiveStream;
    private readonly ZipArchive? _archive;
    private readonly string? _temporaryPath;
    private bool _disposed;

    public IffContainerKind Kind { get; }
    public string? EncryptionRegion { get; }
    public string? FileNameRegion { get; }
    public IReadOnlyList<IffContainerEntry> Entries { get; }

    private IffContainer(string path)
    {
        _sourcePath = path;
        Kind = IffContainerKind.LooseFile;
        FileNameRegion = IffRegionDetector.FromFileName(path);
        var info = new FileInfo(path);
        Entries = [new IffContainerEntry(info.Name, info.Length, _ =>
            ValueTask.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan)))];
    }

    private IffContainer(string path, string archivePath, IffContainerKind kind, string? region, string? temporaryPath)
    {
        _sourcePath = path;
        Kind = kind;
        EncryptionRegion = region;
        FileNameRegion = IffRegionDetector.FromFileName(path);
        _temporaryPath = temporaryPath;
        _archiveStream = new FileStream(archivePath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.RandomAccess);
        try
        {
            _archive = new ZipArchive(_archiveStream, ZipArchiveMode.Read, leaveOpen: true);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var entries = new List<IffContainerEntry>();
            foreach (ZipArchiveEntry entry in _archive.Entries)
            {
                ValidateEntryName(entry.FullName);
                if (!seen.Add(entry.FullName)) throw new InvalidDataException($"Duplicate archive entry '{entry.FullName}'.");
                if (!entry.FullName.EndsWith(".iff", StringComparison.OrdinalIgnoreCase)) continue;
                entries.Add(new IffContainerEntry(entry.FullName, entry.Length, token => OpenSeekableEntryAsync(entry, token)));
            }
            Entries = entries;
        }
        catch
        {
            _archive?.Dispose();
            _archiveStream.Dispose();
            if (_temporaryPath is not null) TryDelete(_temporaryPath);
            throw;
        }
    }

    public static async Task<IffContainer> OpenAsync(string path, IffContainerOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        options ??= new();
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("IFF input was not found.", fullPath);
        byte[] signature = new byte[8];
        await using (var input = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
            await input.ReadExactlyAsync(signature, cancellationToken).ConfigureAwait(false);

        if (IsZip(signature)) return new IffContainer(fullPath, fullPath, IffContainerKind.ZipArchive, null, null);
        if (LooksLikeLooseIff(fullPath, signature, new FileInfo(fullPath).Length)) return new IffContainer(fullPath);

        string tempDirectory = options.TemporaryDirectory ?? Path.GetTempPath();
        Directory.CreateDirectory(tempDirectory);
        string xorTemp = Path.Combine(tempDirectory, $"pangya-iff-{Guid.NewGuid():N}.zip");
        await TransformXorAsync(fullPath, xorTemp, cancellationToken).ConfigureAwait(false);
        if (await HasZipSignatureAsync(xorTemp, cancellationToken).ConfigureAwait(false))
            return new IffContainer(fullPath, xorTemp, IffContainerKind.XorZipArchive, "XOR 0x71", xorTemp);
        TryDelete(xorTemp);

        foreach ((string label, uint[] keys) in PakKeys.All)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string temp = Path.Combine(tempDirectory, $"pangya-iff-{Guid.NewGuid():N}.zip");
            try
            {
                await TransformXteaAsync(fullPath, temp, keys, decrypt: true, cancellationToken).ConfigureAwait(false);
                if (!await HasZipSignatureAsync(temp, cancellationToken).ConfigureAwait(false)) { File.Delete(temp); continue; }
                return new IffContainer(fullPath, temp, IffContainerKind.EncryptedZipArchive, label, temp);
            }
            catch (InvalidDataException) { TryDelete(temp); }
            catch { TryDelete(temp); throw; }
        }
        throw new InvalidDataException("The file is neither a loose IFF nor a supported ZIP/XTEA IFF container.");
    }

    public async Task SaveEntryAsync(string entryName, IffHeader header, IReadOnlyList<IffRecord> records,
        CancellationToken cancellationToken = default, IffContainerSaveOptions? saveOptions = null)
    {
        ThrowIfDisposed();
        IffContainerKind outputKind = saveOptions?.Kind ?? Kind;
        string? outputRegion = saveOptions?.EncryptionRegion ?? EncryptionRegion;
        if (Kind == IffContainerKind.LooseFile && outputKind != IffContainerKind.LooseFile)
            throw new InvalidOperationException("Loose IFF files cannot be converted to archive containers.");
        if (Kind != IffContainerKind.LooseFile && outputKind == IffContainerKind.LooseFile)
            throw new InvalidOperationException("Archive containers cannot be saved as loose IFF files.");
        if (outputKind == IffContainerKind.EncryptedZipArchive &&
            !PakKeys.All.Any(item => item.Label == outputRegion))
            throw new ArgumentException("A supported XTEA key is required for encrypted IFF output.", nameof(saveOptions));
        IffContainerEntry entry = Entries.Single(item => item.Name.Equals(entryName, StringComparison.OrdinalIgnoreCase));
        string directory = Path.GetDirectoryName(_sourcePath)!;
        string temporaryOutput = Path.Combine(directory, $".{Path.GetFileName(_sourcePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            if (Kind == IffContainerKind.LooseFile)
            {
                await WriteIffFileAsync(temporaryOutput, header, records, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                string replacementIff = Path.Combine(Path.GetTempPath(), $"pangya-iff-entry-{Guid.NewGuid():N}.iff");
                bool encoded = outputKind is IffContainerKind.EncryptedZipArchive or IffContainerKind.XorZipArchive;
                string plainZip = encoded ? Path.Combine(Path.GetTempPath(), $"pangya-iff-save-{Guid.NewGuid():N}.zip") : temporaryOutput;
                try
                {
                    await WriteIffFileAsync(replacementIff, header, records, cancellationToken).ConfigureAwait(false);
                    await ValidateLooseIffAsync(replacementIff, entry.Name, cancellationToken).ConfigureAwait(false);
                    await RebuildZipAsync(plainZip, entry.Name, replacementIff, cancellationToken).ConfigureAwait(false);
                    if (outputKind == IffContainerKind.EncryptedZipArchive)
                    {
                        uint[] keys = PakKeys.All.Single(item => item.Label == outputRegion).Keys;
                        await TransformXteaAsync(plainZip, temporaryOutput, keys, decrypt: false, cancellationToken).ConfigureAwait(false);
                    }
                    else if (outputKind == IffContainerKind.XorZipArchive)
                        await TransformXorAsync(plainZip, temporaryOutput, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    TryDelete(replacementIff);
                    if (plainZip != temporaryOutput) TryDelete(plainZip);
                }
            }
            await ValidateOutputAsync(temporaryOutput, entry.Name, outputKind, cancellationToken).ConfigureAwait(false);
            if (Kind != IffContainerKind.LooseFile)
            {
                _archive?.Dispose();
                _archiveStream?.Dispose();
                if (_temporaryPath is not null) TryDelete(_temporaryPath);
                _disposed = true;
            }
            AtomicReplace(temporaryOutput, _sourcePath);
        }
        catch { TryDelete(temporaryOutput); throw; }
    }

    private async Task RebuildZipAsync(string outputPath, string replacementName, string replacementIffPath, CancellationToken token)
    {
        await using var output = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 64 * 1024, FileOptions.Asynchronous);
        using var target = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        foreach (ZipArchiveEntry sourceEntry in _archive!.Entries)
        {
            token.ThrowIfCancellationRequested();
            ZipArchiveEntry destinationEntry = target.CreateEntry(sourceEntry.FullName, CompressionLevel.Optimal);
            await using Stream destination = destinationEntry.Open();
            if (sourceEntry.FullName.Equals(replacementName, StringComparison.OrdinalIgnoreCase))
                await using (var replacement = new FileStream(replacementIffPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan))
                    await replacement.CopyToAsync(destination, token).ConfigureAwait(false);
            else
                await using (Stream source = sourceEntry.Open()) await source.CopyToAsync(destination, token).ConfigureAwait(false);
        }
    }

    private static async Task WriteIffFileAsync(string path, IffHeader header, IReadOnlyList<IffRecord> records, CancellationToken token)
    {
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None,
            64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await IffWriter.WriteAsync(output, header, Enumerate(records, token), token).ConfigureAwait(false);
    }

    private static async Task ValidateOutputAsync(string path, string entryName, IffContainerKind outputKind,
        CancellationToken token)
    {
        if (outputKind == IffContainerKind.LooseFile)
        {
            await ValidateLooseIffAsync(path, entryName, token).ConfigureAwait(false);
            return;
        }
        if (outputKind == IffContainerKind.ZipArchive)
        {
            using ZipArchive zip = ZipFile.OpenRead(path);
            ZipArchiveEntry? entry = zip.Entries.SingleOrDefault(item => item.FullName.Equals(entryName, StringComparison.OrdinalIgnoreCase));
            if (entry is null)
                throw new InvalidDataException("The saved archive does not contain the edited IFF entry.");
            await using Stream staged = await OpenSeekableEntryAsync(entry, token).ConfigureAwait(false);
            await ValidateIffStreamAsync(staged, entryName, token).ConfigureAwait(false);
            return;
        }
        await using IffContainer container = await OpenAsync(path, cancellationToken: token).ConfigureAwait(false);
        IffContainerEntry? encodedEntry = container.Entries.SingleOrDefault(item => item.Name.Equals(entryName, StringComparison.OrdinalIgnoreCase));
        if (encodedEntry is null) throw new InvalidDataException("The saved archive does not contain the edited IFF entry.");
        await using Stream encodedStream = await encodedEntry.OpenAsync(token).ConfigureAwait(false);
        await ValidateIffStreamAsync(encodedStream, entryName, token).ConfigureAwait(false);
    }

    private static async Task ValidateLooseIffAsync(string path, string entryName, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await ValidateIffStreamAsync(stream, entryName, token).ConfigureAwait(false);
    }

    private static async Task ValidateIffStreamAsync(Stream stream, string entryName, CancellationToken token)
    {
        await using IffReader reader = IffReader.Open(stream, Path.GetFileName(entryName), new(LeaveOpen: true));
        await foreach (IffRecord _ in reader.ReadRecordsAsync(token).ConfigureAwait(false)) { }
    }

    private static async ValueTask<Stream> OpenSeekableEntryAsync(ZipArchiveEntry entry, CancellationToken token)
    {
        string path = Path.Combine(Path.GetTempPath(), $"pangya-iff-entry-{Guid.NewGuid():N}.tmp");
        try
        {
            await using (Stream source = entry.Open())
            await using (var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
                await source.CopyToAsync(output, token).ConfigureAwait(false);
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
        }
        catch { TryDelete(path); throw; }
    }

    private static async IAsyncEnumerable<IffRecord> Enumerate(IReadOnlyList<IffRecord> records, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        foreach (IffRecord record in records) { token.ThrowIfCancellationRequested(); yield return record; await Task.Yield(); }
    }

    private static async Task TransformXteaAsync(string inputPath, string outputPath, uint[] keys, bool decrypt, CancellationToken token)
    {
        await using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (decrypt && input.Length % 8 != 0) throw new InvalidDataException("Encrypted IFF containers must be aligned to an eight-byte XTEA block.");
        byte[] buffer = new byte[64 * 1024];
        int read;
        while ((read = await input.ReadAsync(buffer, token).ConfigureAwait(false)) != 0)
        {
            int transformedLength = read;
            if (read % 8 != 0)
            {
                if (decrypt) throw new InvalidDataException("The encrypted IFF stream ended in a partial XTEA block.");
                transformedLength = (read + 7) & ~7;
                Array.Clear(buffer, read, transformedLength - read);
            }
            for (int offset = 0; offset < transformedLength; offset += 8)
            {
                ulong source = BinaryPrimitives.ReadUInt64LittleEndian(buffer.AsSpan(offset, 8));
                ulong transformed = decrypt ? Xtea.Decrypt(keys, source) : Xtea.Encrypt(keys, source);
                BinaryPrimitives.WriteUInt64LittleEndian(buffer.AsSpan(offset, 8), transformed);
            }
            await output.WriteAsync(buffer.AsMemory(0, transformedLength), token).ConfigureAwait(false);
        }
    }

    private static async Task TransformXorAsync(string inputPath, string outputPath, CancellationToken token)
    {
        await using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        byte[] buffer = new byte[64 * 1024];
        int read;
        while ((read = await input.ReadAsync(buffer, token).ConfigureAwait(false)) != 0)
        {
            for (int i = 0; i < read; i++) buffer[i] ^= 0x71;
            await output.WriteAsync(buffer.AsMemory(0, read), token).ConfigureAwait(false);
        }
    }

    private static async Task<bool> HasZipSignatureAsync(string path, CancellationToken token)
    {
        byte[] signature = new byte[4];
        await using var probe = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (await probe.ReadAsync(signature, token).ConfigureAwait(false) != signature.Length) return false;
        return IsZip(signature);
    }

    private static bool LooksLikeLooseIff(string path, byte[] bytes, long length)
    {
        if (!Path.GetExtension(path).Equals(".iff", StringComparison.OrdinalIgnoreCase) || bytes.Length < IffHeader.BinarySize)
            return false;
        IffHeader header = IffHeader.Parse(bytes);
        long payload = length - IffHeader.BinarySize;
        return header.Region != "Unknown" &&
            (header.RecordCount == 0 ? payload == 0 : payload > 0 && payload % header.RecordCount == 0);
    }
    private static bool IsZip(ReadOnlySpan<byte> bytes) => bytes is [0x50, 0x4B, 0x03 or 0x05 or 0x07, 0x04 or 0x06 or 0x08, ..];
    private static void ValidateEntryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || Path.IsPathRooted(name) || name.Split('/', '\\').Any(part => part == ".."))
            throw new InvalidDataException($"Unsafe archive entry name '{name}'.");
    }
    private static void AtomicReplace(string temporary, string target)
    {
        string backup = target + ".bak";
        if (File.Exists(target)) File.Replace(temporary, target, backup, ignoreMetadataErrors: true);
        else File.Move(temporary, target);
    }
    private static void TryDelete(string path) { try { File.Delete(path); } catch { } }
    private void ThrowIfDisposed() { ObjectDisposedException.ThrowIf(_disposed, this); }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _archive?.Dispose(); _archiveStream?.Dispose();
        if (_temporaryPath is not null) TryDelete(_temporaryPath);
    }
    public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
}
