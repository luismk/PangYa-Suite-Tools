using System.Text;
using PangyaAPI.PAK.Flags;

namespace PangyaAPI.PAK.Models;

public static class PspPakArchive
{
    public const string IndexFileName = "psppackfilelist.pak";
    public const string DataFileName = "pspdata.pak";
    private const int PayloadAlignment = 32768;
    private static readonly SemaphoreSlim WriteGate = new(1, 1);

    internal static bool IsMemberPath(string path) =>
        Path.GetFileName(path).Equals(IndexFileName, StringComparison.OrdinalIgnoreCase) ||
        Path.GetFileName(path).Equals(DataFileName, StringComparison.OrdinalIgnoreCase);

    internal static string DataPath(string path) => SiblingPath(path, DataFileName);

    private static string SiblingPath(string path, string name) =>
        Path.GetFileName(path).Equals(name, StringComparison.OrdinalIgnoreCase)
            ? path
            : Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, name);

    internal static List<PakFileEntry> ReadIndex(string path, long dataLength, Encoding encoding)
    {
        using var index = new BinaryReader(File.OpenRead(SiblingPath(path, IndexFileName)));
        var entries = new List<PakFileEntry>();
        long previousEnd = 0;
        Span<byte> name = stackalloc byte[byte.MaxValue];
        while (index.BaseStream.Position < index.BaseStream.Length)
        {
            int type = index.BaseStream.ReadByte();
            if (type == 0)
            {
                while (index.BaseStream.Position < index.BaseStream.Length)
                    if (index.ReadByte() != 0)
                        throw new InvalidDataException("Invalid PSP PAK index padding.");
                break;
            }

            if (type != 3 || index.ReadByte() != 0)
                throw new InvalidDataException("Unsupported PSP PAK entry type.");

            index.ReadUInt16(); // Game entry ID. Multiple entries may use 0xFFFF.
            uint offset = index.ReadUInt32();
            uint storedSize = index.ReadUInt32();
            uint size = index.ReadUInt32();
            long end = (long)offset + storedSize;
            if (size != storedSize || size > Array.MaxLength || offset % PayloadAlignment != 0 ||
                offset < previousEnd || end > dataLength)
                throw new InvalidDataException("Invalid PSP PAK payload bounds or size.");

            int nameLength = 0;
            while (true)
            {
                byte value = index.ReadByte();
                if (value == 0) break;
                if (nameLength == name.Length)
                    throw new InvalidDataException("PSP PAK entry name is too long.");
                name[nameLength++] = value;
            }

            if (nameLength == 0)
                throw new InvalidDataException("Empty PSP PAK entry name.");

            // A minimum 16-byte name field, rounded up in 8-byte blocks, plus four reserved bytes.
            int nameFieldSize = Math.Max(16, (nameLength + 8) & ~7) + 4;
            for (int i = nameLength + 1; i < nameFieldSize; i++)
                if (index.ReadByte() != 0)
                    throw new InvalidDataException("Invalid PSP PAK entry name padding.");

            entries.Add(new PakFileEntry
            {
                Type = PakFileEntryType.Raw,
                Version = PakFileEntryVersion.Raw,
                Offset = offset,
                Size = size,
                CompressSize = size,
                NameLength = (byte)nameLength,
                NameRaw = name[..nameLength].ToArray(),
                FileNameEncoding = encoding
            });
            previousEnd = end;
        }

        if (entries.Count == 0)
            throw new InvalidDataException("The PSP PAK index contains no entries.");
        return entries;
    }

    internal static Dictionary<PakFileEntry, string> GetDuplicateExtractionNames(List<PakFileEntry> entries)
    {
        var reserved = entries.Select(entry => entry.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var names = new Dictionary<PakFileEntry, string>();
        for (int ordinal = 0; ordinal < entries.Count; ordinal++)
        {
            PakFileEntry entry = entries[ordinal];
            if (seen.Add(entry.Name)) continue;

            string extension = Path.GetExtension(entry.Name);
            string stem = entry.Name[..(entry.Name.Length - extension.Length)];
            string name = $"{stem}~{ordinal}{extension}";
            while (!reserved.Add(name))
                name = $"~{name}";
            names.Add(entry, name);
        }
        return names;
    }

    /// <summary>Replaces existing payloads by zero-based index position, preserving both PSP indexes.</summary>
    public static void ReplaceEntries(string memberPath, IReadOnlyDictionary<int, string> replacements,
        Action<int, int>? onProgress = null, bool saveBackup = false,
        CancellationToken cancellationToken = default) =>
        ReplaceEntries(memberPath, replacements, onProgress, saveBackup, cancellationToken, null);

    internal static void ReplaceEntries(string memberPath, IReadOnlyDictionary<int, string> replacements,
        Action<int, int>? onProgress, bool saveBackup, CancellationToken cancellationToken,
        Action? validateDestination)
    {
        ArgumentNullException.ThrowIfNull(replacements);
        if (!IsMemberPath(memberPath))
            throw new ArgumentException("Select either member of the PSP PAK pair.", nameof(memberPath));
        if (replacements.Count == 0) return;

        string fullPath = Path.GetFullPath(memberPath);
        string dataPath = DataPath(fullPath);
        string candidate = dataPath + $".{Guid.NewGuid():N}.tmp";
        WriteGate.Wait(cancellationToken);
        try
        {
            validateDestination?.Invoke();
            using (var data = File.OpenRead(dataPath))
            {
                var entries = ReadIndex(fullPath, data.Length, PakFileNameEncoding.CreateDefault());
                var patches = new List<(PakFileEntry Entry, string Path)>();
                foreach (var (ordinal, sourcePath) in replacements)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if ((uint)ordinal >= entries.Count)
                        throw new ArgumentOutOfRangeException(nameof(replacements), "Invalid PSP PAK entry position.");
                    PakFileEntry entry = entries[ordinal];
                    if (new FileInfo(sourcePath).Length != entry.Size)
                        throw new InvalidDataException($"PSP PAK replacement for '{entry.Name}' must be exactly {entry.Size} bytes.");
                    patches.Add((entry, sourcePath));
                }

                using var output = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write);
                data.CopyToAsync(output, cancellationToken).GetAwaiter().GetResult();
                int done = 0;
                foreach (var (entry, sourcePath) in patches)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    using var source = File.OpenRead(sourcePath);
                    if (source.Length != entry.Size)
                        throw new InvalidDataException($"PSP PAK replacement for '{entry.Name}' changed size.");
                    output.Position = entry.Offset;
                    source.CopyToAsync(output, cancellationToken).GetAwaiter().GetResult();
                    onProgress?.Invoke(++done, patches.Count);
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            validateDestination?.Invoke();
            File.Replace(candidate, dataPath, saveBackup ? dataPath + ".bak" : null, ignoreMetadataErrors: true);
        }
        finally
        {
            try { File.Delete(candidate); }
            finally { WriteGate.Release(); }
        }
    }
}
