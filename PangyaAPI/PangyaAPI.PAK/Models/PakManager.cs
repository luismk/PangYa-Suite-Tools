using PangyaAPI.PAK.Flags;
using System.Text;

namespace PangyaAPI.PAK.Models;

public readonly record struct PakRebuildOptions(
    PakFileEntryVersion EntryVersion,
    PakFileEntryType EntryType,
    byte CompressLevel,
    uint[] LocationKeys,
    string Author)
{
    public Encoding FileNameEncoding { get; init; } = PakFileNameEncoding.CreateDefault();
}

public readonly record struct PakInjectItem(string SourcePath, string? RelativeFolder);

public static class PakManager
{
    public static string FindExistingRelativeFolder(PakReader reader, string fileName)
    {
        PakFileEntry? match = reader.Entries.FirstOrDefault(entry =>
            entry.Type != PakFileEntryType.Directory &&
            string.Equals(Path.GetFileName(entry.Name.Replace('/', '\\')), fileName,
                          StringComparison.OrdinalIgnoreCase));
        return match == null ? string.Empty : Path.GetDirectoryName(match.Name.Replace('/', '\\')) ?? string.Empty;
    }

    public static void InjectFiles(string pakPath, PakReader reader, IEnumerable<PakInjectItem> items,
                                   PakRebuildOptions options, string defaultRelativeFolder = "",
                                   Action<string>? log = null, Action<int, int>? onProgress = null,
                                   bool SaveBck = false) =>
        InjectFiles(pakPath, reader, items, options, defaultRelativeFolder, log, onProgress,
                    SaveBck, CancellationToken.None);

    public static void InjectFiles(string pakPath, PakReader reader, IEnumerable<PakInjectItem> items,
                                   PakRebuildOptions options, string defaultRelativeFolder,
                                   Action<string>? log, Action<int, int>? onProgress, bool SaveBck,
                                   CancellationToken cancellationToken)
    {
        var replacements = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (PakInjectItem item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = new FileInfo(item.SourcePath);
            if (!source.Exists) throw new FileNotFoundException("Injection source does not exist.", source.FullName);
            string folder = item.RelativeFolder ?? FindExistingRelativeFolder(reader, source.Name);
            if (string.IsNullOrEmpty(folder)) folder = defaultRelativeFolder;
            string archivePath = CombineArchivePath(folder, source.Name);
            replacements[archivePath] = source;
        }

        var buildItems = ExistingBuildItems(reader, entry =>
        {
            if (!replacements.Remove(Normalize(entry.Name), out FileInfo? replacement)) return null;
            log?.Invoke($"Atualizado: {entry.Name}");
            return SourceItem(replacement, Normalize(entry.Name));
        }).ToList();

        foreach ((string archivePath, FileInfo source) in replacements.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            buildItems.Add(SourceItem(source, archivePath));
            log?.Invoke($"Novo arquivo adicionado: {archivePath}");
        }

        buildItems = AddMissingDirectoryEntries(buildItems);
        Rebuild(pakPath, reader, buildItems, options, log, onProgress, SaveBck, cancellationToken);
    }

    public static void InjectFiles(string pakPath, PakReader reader, IEnumerable<string> sourceFiles,
                                   PakRebuildOptions options, string defaultRelativeFolder = "",
                                   Action<string>? log = null, Action<int, int>? onProgress = null)
    {
        InjectFiles(pakPath, reader, sourceFiles.Select(file => new PakInjectItem(file, null)),
                    options, defaultRelativeFolder, log, onProgress);
    }

    public static void ChangeEncryptionKey(string pakPath, PakReader reader, PakRebuildOptions newOptions,
                                           Action<string>? log = null, Action<int, int>? onProgress = null,
                                           bool SaveBck = false) =>
        ChangeEncryptionKey(pakPath, reader, newOptions, log, onProgress, SaveBck, CancellationToken.None);

    public static void ChangeEncryptionKey(string pakPath, PakReader reader, PakRebuildOptions newOptions,
                                           Action<string>? log, Action<int, int>? onProgress,
                                           bool SaveBck, CancellationToken cancellationToken)
    {
        log?.Invoke("Reescrevendo metadados e reutilizando os payloads comprimidos...");
        Rebuild(pakPath, reader, ExistingBuildItems(reader).ToList(), newOptions,
                log, onProgress, SaveBck, cancellationToken, preserveExistingPayloadTypes: true);
    }

    public static void RemoveFiles(string pakPath, PakReader reader, IEnumerable<string> namesToRemove,
                                   PakRebuildOptions options, Action<string>? log = null,
                                   Action<int, int>? onProgress = null, bool SaveBck = false) =>
        RemoveFiles(pakPath, reader, namesToRemove, options, log, onProgress, SaveBck,
                    CancellationToken.None);

    public static void RemoveFiles(string pakPath, PakReader reader, IEnumerable<string> namesToRemove,
                                   PakRebuildOptions options, Action<string>? log,
                                   Action<int, int>? onProgress, bool SaveBck,
                                   CancellationToken cancellationToken)
    {
        var remove = new HashSet<string>(namesToRemove.Select(Normalize), StringComparer.OrdinalIgnoreCase);
        List<PakWriter.BuildItem> buildItems = ExistingBuildItems(reader)
            .Where(item => item.IsDirectory || !remove.Contains(item.ArchivePath))
            .ToList();
        foreach (string name in remove) log?.Invoke($"Removido: {name}");
        Rebuild(pakPath, reader, buildItems, options, log, onProgress, SaveBck, cancellationToken);
    }

    private static IEnumerable<PakWriter.BuildItem> ExistingBuildItems(
        PakReader reader, Func<PakFileEntry, PakWriter.BuildItem?>? replacement = null)
    {
        foreach (PakFileEntry entry in reader.Entries)
        {
            string archivePath = Normalize(entry.Name);
            if (entry.Type == PakFileEntryType.Directory)
            {
                yield return new PakWriter.BuildItem(true, archivePath, 0, null);
                continue;
            }

            PakWriter.BuildItem? substituted = replacement?.Invoke(entry);
            if (substituted.HasValue)
            {
                yield return substituted.Value;
                continue;
            }

            yield return new PakWriter.BuildItem(
                false,
                archivePath,
                Math.Max(entry.Size, entry.CompressSize),
                cancellationToken =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return reader.ExtractEntryToBytes(entry)
                        ?? throw new InvalidDataException($"Unable to decompress {entry.Name}.");
                },
                entry.Type,
                entry.Size,
                cancellationToken =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return reader.ReadCompressedEntryBytes(entry);
                });
        }
    }

    private static PakWriter.BuildItem SourceItem(FileInfo source, string archivePath) =>
        new(false, Normalize(archivePath), source.Length,
            cancellationToken =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                return File.ReadAllBytes(source.FullName);
            });

    private static List<PakWriter.BuildItem> AddMissingDirectoryEntries(
        IReadOnlyList<PakWriter.BuildItem> items)
    {
        var knownDirectories = new HashSet<string>(
            items.Where(item => item.IsDirectory).Select(item => item.ArchivePath),
            StringComparer.OrdinalIgnoreCase);
        var result = new List<PakWriter.BuildItem>(items.Count);

        foreach (PakWriter.BuildItem item in items)
        {
            if (!item.IsDirectory)
            {
                string? directory = Path.GetDirectoryName(item.ArchivePath.Replace('/', '\\'));
                var missing = new Stack<string>();
                while (!string.IsNullOrEmpty(directory))
                {
                    string normalized = Normalize(directory);
                    if (knownDirectories.Add(normalized)) missing.Push(normalized);
                    directory = Path.GetDirectoryName(directory);
                }
                while (missing.TryPop(out string? path))
                    result.Add(new PakWriter.BuildItem(true, path, 0, null));
            }
            result.Add(item);
        }
        return result;
    }

    private static void Rebuild(string pakPath, PakReader reader,
                                IReadOnlyList<PakWriter.BuildItem> items,
                                PakRebuildOptions options, Action<string>? log,
                                Action<int, int>? onProgress, bool saveBackup,
                                CancellationToken cancellationToken,
                                bool preserveExistingPayloadTypes = false)
    {
        string destination = Path.GetFullPath(pakPath);
        string candidate = destination + $".{Guid.NewGuid():N}.tmp";
        string backup = destination + ".bak";
        var writer = new PakWriter
        {
            EntryVersion = options.EntryVersion,
            EntryType = options.EntryType,
            CompressLevel = options.CompressLevel,
            LocationKeys = options.LocationKeys,
            Author = options.Author,
            FileNameEncoding = options.FileNameEncoding,
            PreserveExistingPayloadTypes = preserveExistingPayloadTypes
        };

        try
        {
            writer.WriteCandidate(items, candidate, log, cancellationToken,
                (_, done, total) => onProgress?.Invoke(done, total));
            using (var validationReader = new PakReader(candidate, options.FileNameEncoding))
                validationReader.Parse(options.EntryVersion == PakFileEntryVersion.V3 ? options.LocationKeys : null);

            cancellationToken.ThrowIfCancellationRequested();
            reader.Dispose();
            if (saveBackup)
            {
                if (File.Exists(backup)) File.Delete(backup);
                File.Copy(destination, backup);
            }
            PakWriter.PromoteCandidate(candidate, destination);
            log?.Invoke("PAK reconstruído com sucesso.");
        }
        finally
        {
            if (File.Exists(candidate)) File.Delete(candidate);
        }

    }

    /// <summary>
    /// Renomeia um arquivo ou pasta na memória do Reader e reconstrói PAK.
    /// </summary>
    public static bool Rename(string pakPath, PakReader reader, string oldPath, string newName, PakRebuildOptions options, Action<int, int>? onProgress = null)
    {
        oldPath = oldPath.Replace('/', '\\').Trim('\\');
        string oldPrefix = oldPath + "\\";

        // Extrai o diretório pai mantendo o padrão correto de barras invertidas
        string? parentDir = Path.GetDirectoryName(oldPath)?.Replace('/', '\\');
        string newPath = string.IsNullOrEmpty(parentDir) ? newName : Path.Combine(parentDir, newName);
        newPath = newPath.Replace('/', '\\');
        string newPrefix = newPath + "\\";

        bool modified = false;

        foreach (var entry in reader.Entries)
        {
            // Caso 1: É o arquivo exato ou a pasta exata mapeada (Comparando em formato Windows '\\')
            if (entry.Name.Equals(oldPath, StringComparison.OrdinalIgnoreCase))
            {
                entry.Name = newPath; // O setter do entry.Name vai cuidar de converter para '/' internamente no NameRaw!
                modified = true;
            }
            // Caso 2: É um arquivo contido dentro da pasta que está sendo renomeada
            else if (entry.Name.StartsWith(oldPrefix, StringComparison.OrdinalIgnoreCase))
            {
                entry.Name = newPrefix + entry.Name.Substring(oldPrefix.Length);
                modified = true;
            }
        }
        // Se nada foi alterado, abortamos o processo antes de mexer no disco
        if (!modified) return false;

        // ── 2. RECONSTRUÇÃO ────────────────────────────────────
        string directory = Path.GetDirectoryName(pakPath) ?? "";
        string candidate = Path.Combine(directory, Path.GetFileName(pakPath) + ".rename_candidate");
        string backup = Path.Combine(directory, Path.GetFileName(pakPath) + ".bak");

        // criar a struct
        var items = ExistingBuildItems(reader).ToList();

        var writer = new PakWriter
        {
            PakVersion = (byte)(reader.Header.Version),//estava fazendo errado, eu usei outra coisa
            EntryVersion = options.EntryVersion,
            EntryType = options.EntryType,
            CompressLevel = options.CompressLevel,
            LocationKeys = options.LocationKeys,
            Author = options.Author,
            FileNameEncoding = options.FileNameEncoding,
            PreserveExistingPayloadTypes = true
        };

        try
        {
            // Escreve o novo arquivo PAK temporário
            writer.WriteCandidate(items, candidate, null, CancellationToken.None,
                (_, done, total) => onProgress?.Invoke(done, total));

            // Fecha o leitor antigo para liberar o arquivo original do sistema operacional
            reader.Dispose();

            // Substituição segura com backup
            if (File.Exists(backup)) File.Delete(backup);
            File.Copy(pakPath, backup);

            PakWriter.PromoteCandidate(candidate, pakPath);
        }
        finally
        {
            if (File.Exists(candidate)) File.Delete(candidate);
        }

        return true;
    } 

    private static string CombineArchivePath(string folder, string fileName) =>
        string.IsNullOrWhiteSpace(folder) ? Normalize(fileName) : Normalize(Path.Combine(folder, fileName));

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');
}
