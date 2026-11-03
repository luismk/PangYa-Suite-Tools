namespace PangYa_Suite_Tools.Configuration;

internal enum PathTextBoxKind
{
    PakArchive,
    IffArchiveOrFolder,
    IffDataRoot,
    UpdateListViewerFile,
    UpdateListVerificationRoot,
    UpdateListSourceFolder,
    UpdateListDestinationFolder,
    UpdateListExistingFile,
    PakDiffSnapshotA,
    PakDiffSnapshotB,
    PakDiffSourceClient,
    PakDiffCompareClient
}

internal static class PathTextBoxPreferences
{
    private static readonly object PreferenceLock = new();

    internal static string? PreferencePathOverride { get; set; }

    private static string PreferencePath => PreferencePathOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PangYa-Suite-Tools", "path-textboxes.txt");

    internal static string LoadPath(PathTextBoxKind kind)
    {
        lock (PreferenceLock)
        {
            return LoadPaths().GetValueOrDefault(kind, string.Empty);
        }
    }

    internal static void SavePath(PathTextBoxKind kind, string? path) =>
        SavePaths(new Dictionary<PathTextBoxKind, string?> { [kind] = path });

    internal static void SavePaths(IReadOnlyDictionary<PathTextBoxKind, string?> paths)
    {
        lock (PreferenceLock)
        {
            Dictionary<PathTextBoxKind, string> savedPaths = LoadPaths();
            foreach ((PathTextBoxKind kind, string? path) in paths)
            {
                if (!string.IsNullOrWhiteSpace(path)) savedPaths[kind] = path.Trim();
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath)!);
                IEnumerable<string> lines = savedPaths
                    .OrderBy(item => item.Key)
                    .Select(item => $"{item.Key}\t{item.Value}");
                File.WriteAllLines(PreferencePath, lines);
            }
            catch
            {
                // A display preference must never prevent a file operation.
            }
        }
    }

    private static Dictionary<PathTextBoxKind, string> LoadPaths()
    {
        var paths = new Dictionary<PathTextBoxKind, string>();
        try
        {
            if (!File.Exists(PreferencePath)) return paths;

            foreach (string line in File.ReadLines(PreferencePath))
            {
                int separator = line.IndexOf('\t');
                if (separator <= 0 || separator == line.Length - 1) continue;

                if (Enum.TryParse(line[..separator], out PathTextBoxKind kind))
                    paths[kind] = line[(separator + 1)..];
            }
        }
        catch
        {
            paths.Clear();
        }

        return paths;
    }
}
