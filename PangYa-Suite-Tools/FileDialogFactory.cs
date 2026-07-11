using PangYa_Suite_Tools.Localization;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace PangYa_Suite_Tools;

internal enum FileDialogKind
{
    Pak,
    Iff,
    Icon,
    PakInject,
    UpdateList,
    ExistingUpdateList,
    Snapshot
}

internal static class FileDialogFactory
{
    private static readonly object DirectoryLock = new();
    private static readonly Dictionary<FileDialogKind, string> InitialDirectories = new();
    private static bool _directoriesLoaded;

    internal static string? PreferencePathOverride { get; set; }

    private static string PreferencePath => PreferencePathOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PangYa-Suite-Tools", "file-dialog-directories.txt");

    public static OpenFileDialog CreatePakOpenDialog() => CreateOpenDialog(
        kind: FileDialogKind.Pak,
        title: Strings.Pak_OpenFileTitle,
        filter: Strings.Pak_OpenFileFilter,
        defaultExt: "pak",
        fallbackInitialDirectory: null);

    public static OpenFileDialog CreateIffOpenDialog() => CreateOpenDialog(
        kind: FileDialogKind.Iff,
        title: Strings.IFFManager_OpenArchive,
        filter: Strings.IFFManager_OpenArchiveFilter,
        defaultExt: "iff",
        fallbackInitialDirectory: null);

    public static OpenFileDialog CreateIconOpenDialog(string? initialDirectory) => CreateOpenDialog(
        kind: FileDialogKind.Icon,
        title: Strings.Shop_SelectIcon,
        filter: Strings.Shop_IconFilter,
        defaultExt: "tga",
        fallbackInitialDirectory: initialDirectory);

    public static OpenFileDialog CreatePakInjectFilesDialog() => CreateOpenDialog(
        kind: FileDialogKind.PakInject,
        title: Strings.PakMaker_SelectTheFilesToUpdateInject,
        filter: Strings.PakMaker_InjectFilesFilter,
        defaultExt: string.Empty,
        multiselect: true,
        fallbackInitialDirectory: null);

    public static OpenFileDialog CreateUpdateListOpenDialog() => CreateOpenDialog(
        kind: FileDialogKind.UpdateList,
        title: Strings.UpdateList_SelectUpdateListFile,
        filter: Strings.UpdateList_OpenFileFilter,
        defaultExt: string.Empty,
        fallbackInitialDirectory: null);

    public static OpenFileDialog CreateExistingUpdateListOpenDialog() => CreateOpenDialog(
        kind: FileDialogKind.ExistingUpdateList,
        title: Strings.UpdateList_SelectExistingUpdateListFile,
        filter: Strings.UpdateList_OpenFileFilter,
        defaultExt: string.Empty,
        fallbackInitialDirectory: null);

    public static OpenFileDialog CreateSnapshotOpenDialog() => CreateOpenDialog(
        kind: FileDialogKind.Snapshot,
        title: Strings.PakDiff_LoadSnapshot,
        filter: Strings.PakDiff_SnapshotFilter,
        defaultExt: "paksnap",
        fallbackInitialDirectory: null);

    public static void RememberDirectory(FileDialogKind kind, string? selectedFilePath)
    {
        if (string.IsNullOrWhiteSpace(selectedFilePath)) return;

        string? directory = Path.GetDirectoryName(selectedFilePath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return;

        lock (DirectoryLock)
        {
            EnsureDirectoriesLoaded();
            InitialDirectories[kind] = directory;
            SaveDirectories();
        }
    }

    internal static void ClearRememberedDirectories()
    {
        lock (DirectoryLock)
        {
            InitialDirectories.Clear();
            _directoriesLoaded = true;
        }
    }

    internal static void ResetRememberedDirectoriesForReload()
    {
        lock (DirectoryLock)
        {
            InitialDirectories.Clear();
            _directoriesLoaded = false;
        }
    }

    private static OpenFileDialog CreateOpenDialog(
        FileDialogKind kind,
        string title,
        string filter,
        string defaultExt,
        bool multiselect = false,
        string? fallbackInitialDirectory = null)
    {
        var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            DefaultExt = defaultExt,
            Multiselect = multiselect,
            CheckFileExists = true,
            CheckPathExists = true
        };

        string? initialDirectory = GetInitialDirectory(kind, fallbackInitialDirectory);
        dialog.InitialDirectory = initialDirectory;

        return dialog;
    }

    private static string GetInitialDirectory(FileDialogKind kind, string? fallbackInitialDirectory)
    {
        if (!string.IsNullOrWhiteSpace(fallbackInitialDirectory) &&
            Directory.Exists(fallbackInitialDirectory))
            return fallbackInitialDirectory;

        lock (DirectoryLock)
        {
            EnsureDirectoriesLoaded();
            if (InitialDirectories.TryGetValue(kind, out string? rememberedDirectory))
            {
                if (Directory.Exists(rememberedDirectory)) return rememberedDirectory;
                InitialDirectories.Remove(kind);
                SaveDirectories();
            }
        }

        return GetDefaultInitialDirectory();
    }

    private static string GetDefaultInitialDirectory()
    {
        string[] candidates =
        [
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            AppContext.BaseDirectory
        ];

        return candidates.First(Directory.Exists);
    }

    private static void EnsureDirectoriesLoaded()
    {
        if (_directoriesLoaded) return;
        InitialDirectories.Clear();

        try
        {
            if (File.Exists(PreferencePath))
            {
                foreach (string line in File.ReadLines(PreferencePath))
                {
                    int separator = line.IndexOf('\t');
                    if (separator <= 0 || separator == line.Length - 1) continue;

                    string kindName = line[..separator];
                    string directory = line[(separator + 1)..];
                    if (Enum.TryParse(kindName, out FileDialogKind kind) && Directory.Exists(directory))
                        InitialDirectories[kind] = directory;
                }
            }
        }
        catch
        {
            InitialDirectories.Clear();
        }
        finally
        {
            _directoriesLoaded = true;
        }
    }

    private static void SaveDirectories()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath)!);
            IEnumerable<string> lines = InitialDirectories
                .Where(item => Directory.Exists(item.Value))
                .OrderBy(item => item.Key)
                .Select(item => $"{item.Key}\t{item.Value}");
            File.WriteAllLines(PreferencePath, lines);
        }
        catch
        {
            // Dialog preferences must never prevent the application from opening files.
        }
    }
}
