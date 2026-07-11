using System.Text.Json;

namespace PangyaAPI.PAK.Models;

public sealed record PakExtractionSidecar(string FileName, string Path)
{
    public const string ManifestFileName = "pakpath.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed record PakExtractionManifest(IReadOnlyList<PakExtractionSidecar> Files);

    public static bool IsPangyaIff(string entryName)
    {
        string fileName = System.IO.Path.GetFileName(NormalizeSeparators(entryName));
        return fileName.StartsWith("pangya_", StringComparison.OrdinalIgnoreCase) &&
            fileName.EndsWith(".iff", StringComparison.OrdinalIgnoreCase);
    }

    public static void WriteForEntry(PakFileEntry entry, string outputPath)
    {
        WriteManifest([(entry, outputPath)]);
    }

    public static void WriteManifest(IEnumerable<(PakFileEntry Entry, string OutputPath)> extractedFiles)
    {
        ArgumentNullException.ThrowIfNull(extractedFiles);

        (PakFileEntry Entry, string OutputPath)[] files = extractedFiles.ToArray();
        if (files.Length == 0) return;

        PakExtractionSidecar[] manifestFiles = files
            .Select(file => CreateItem(file.Entry))
            .DistinctBy(item => $"{item.Path}\u001f{item.FileName}", StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (manifestFiles.Length == 0) return;

        foreach (string manifestPath in files
            .Where(file => IsPangyaIff(file.Entry.Name))
            .Select(file => GetManifestPath(file.OutputPath))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string? manifestDirectory = System.IO.Path.GetDirectoryName(manifestPath);
            if (!string.IsNullOrEmpty(manifestDirectory)) Directory.CreateDirectory(manifestDirectory);
            string json = JsonSerializer.Serialize(new PakExtractionManifest(
                MergeManifestFiles(ReadManifestFiles(manifestPath), manifestFiles)), JsonOptions);
            File.WriteAllText(manifestPath, json);
        }
    }

    public static string GetManifestPath(string extractedFilePath) =>
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(extractedFilePath) ?? string.Empty, ManifestFileName);

    public static string? TryResolveExtractionRoot(string extractedFilePath)
    {
        string manifestPath = GetManifestPath(extractedFilePath);
        if (!File.Exists(manifestPath)) return null;

        try
        {
            string json = File.ReadAllText(manifestPath);
            PakExtractionSidecar? item = FindMatchingItem(json, extractedFilePath);
            if (item is null) return null;

            string normalizedPath = NormalizeSeparators(item.Path).Trim('/');
            if (normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries).Any(part => part == ".."))
                return null;

            string? root = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(extractedFilePath));
            foreach (string _ in normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                if (root is null) return null;
                root = Directory.GetParent(root)?.FullName;
            }

            if (root is null) return null;
            string expectedPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(
                root,
                normalizedPath.Replace('/', System.IO.Path.DirectorySeparatorChar),
                item.FileName));
            string actualPath = System.IO.Path.GetFullPath(extractedFilePath);
            return expectedPath.Equals(actualPath, StringComparison.OrdinalIgnoreCase) ? root : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return null;
        }
    }

    private static PakExtractionSidecar CreateItem(PakFileEntry entry)
    {
        string normalizedName = NormalizeSeparators(entry.Name).Trim('/');
        string fileName = System.IO.Path.GetFileName(normalizedName);
        string? directory = System.IO.Path.GetDirectoryName(normalizedName);
        return new PakExtractionSidecar(fileName, NormalizeSeparators(directory ?? string.Empty).Trim('/'));
    }

    private static PakExtractionSidecar? FindMatchingItem(string json, string extractedFilePath)
    {
        PakExtractionManifest? manifest = JsonSerializer.Deserialize<PakExtractionManifest>(json);
        string fileName = System.IO.Path.GetFileName(extractedFilePath);
        return manifest?.Files.FirstOrDefault(item =>
            item.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) &&
            !System.IO.Path.IsPathRooted(item.Path));
    }

    private static IReadOnlyList<PakExtractionSidecar> ReadManifestFiles(string manifestPath)
    {
        if (!File.Exists(manifestPath)) return [];

        try
        {
            string json = File.ReadAllText(manifestPath);
            return JsonSerializer.Deserialize<PakExtractionManifest>(json)?.Files ?? [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return [];
        }
    }

    private static PakExtractionSidecar[] MergeManifestFiles(
        IEnumerable<PakExtractionSidecar> existingFiles,
        IEnumerable<PakExtractionSidecar> newFiles) =>
        existingFiles.Concat(newFiles)
            .Where(item => !System.IO.Path.IsPathRooted(item.Path))
            .DistinctBy(item => $"{NormalizeSeparators(item.Path).Trim('/')}\u001f{item.FileName}",
                StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string NormalizeSeparators(string path) => path.Replace('\\', '/');
}
