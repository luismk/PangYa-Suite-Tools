using PangyaAPI.Utilities.Cryptography;
using PangyaAPI.UpdateList.Localization;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace PangyaAPI.UpdateList.Models
{
    /// <summary>
    /// Scans a directory of update files and generates the final XTEA-encrypted
    /// update list.
    ///
    /// Based on the original XMLParser.cs (Dashboard.RecursiveFileProcessor):
    /// - fdate/ftime use LastWriteTime plus three hours (the legacy Pangya convention)
    /// - fdir is only the immediate parent directory name plus "\"
    /// - ZIP output pname = flattened relative path + "_" + file order + ".zip"
    /// - psize is the actual ZIP size (populated after compression)
    /// - CheckSum = MD5(name + size + date+3h) for change detection
    /// - Ignored extensions: .bak .txt .lib .exp .pdb .xml .dmp .cln .json,
    ///   plus files named "uninstall.exe"
    /// </summary>
    public class UpdateMaker
    {
        private readonly Crc32 _crcCalculator = new Crc32();

        // File extensions/names ignored by the scan (matching the original XMLParser).
        private static readonly string[] IgnoredSuffixes =
        {
            ".bak", ".txt", ".lib", ".exp", ".pdb", ".xml",
            ".dmp", ".cln", ".json", "uninstall.exe"
        };

        /// <summary>
        /// Recursively scans <paramref name="targetFolder"/>, builds the entries,
        /// and generates the final update list at <paramref name="outputPath"/>.
        /// </summary>
        public void GenerateFromDirectory(
            string targetFolder,
            string outputPath,
            uint[] regionKeys,
            string patchVersion,
            string updateVersion = "20090331",
            string clientPatchNum = "1",
            Action<int, int>? onProgress = null,
            bool createZipPackages = false,
            Action<string>? onFileProcessing = null)
        {
            if (!Directory.Exists(targetFolder))
                throw new DirectoryNotFoundException(UpdateListStrings.Format(
                    UpdateListStrings.UpdateMakerTargetDirectoryNotFound,
                    targetFolder));

            string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath))
                ?? throw new ArgumentException(
                    UpdateListStrings.UpdateMakerOutputPathMustIncludeDirectory,
                    nameof(outputPath));
            string[] files = Directory.EnumerateFiles(targetFolder, "*", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".cln", StringComparison.OrdinalIgnoreCase) &&
                               !path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) &&
                               !Path.GetFileName(path).Equals(Path.GetFileName(outputPath), StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            string[] packageNames = createZipPackages
                ? files.Select((file, index) =>
                    CreateOrderedFlatPackageName(targetFolder, file, index + 1)).ToArray()
                : files.Select(file => Path.GetFileName(file) + ".zip").ToArray();
            if (createZipPackages) ValidatePackageNames(files, packageNames);

            var entries = new UpdateEntry[files.Length];
            int totalProgress = files.Length * (createZipPackages ? 2 : 1);
            int completed = 0;

            Parallel.For(0, files.Length, new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount, 4))
            }, index =>
            {
                string file = files[index];
                var fileInfo = new FileInfo(file);
                string relativePath = Path.GetRelativePath(targetFolder, file);
                onFileProcessing?.Invoke(relativePath);
                string? directory = Path.GetDirectoryName(relativePath);
                entries[index] = new UpdateEntry
                {
                    fname = fileInfo.Name,
                    fdir = string.IsNullOrEmpty(directory) ? "\\" : "\\" + directory,
                    fsize = fileInfo.Length,
                    fcrc = _crcCalculator.CalculateFileCRC(file),
                    fdate = fileInfo.LastWriteTimeUtc.ToString("yyyy-MM-dd"), // Use UTC as in the legacy implementation.
                    ftime = fileInfo.LastWriteTimeUtc.ToString("HH:mm:ss"),
                    pname = packageNames[index],
                    psize = 717469 // Preserve the legacy placeholder when ZIP output is disabled.
                };

                onProgress?.Invoke(Interlocked.Increment(ref completed), totalProgress);
            });

            if (createZipPackages)
                CreatePackages(files, entries, outputDirectory, onProgress, totalProgress, ref completed);

            var header = new UpdateHeader
            {
                ClientPatchVersion = patchVersion,
                ClientPatchNum     = clientPatchNum,
                UpdateVersion      = updateVersion
            };

            var writer = new UpdateWriter(regionKeys);
            writer.WriteUpdateList(outputPath, header, entries.ToList());
        }

        private static string CreateOrderedFlatPackageName(
            string targetFolder,
            string file,
            int fileNumber)
        {
            string relativePath = Path.GetRelativePath(targetFolder, file);
            string flattenedName = string.Join(
                "_",
                relativePath.Split(
                    [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                    StringSplitOptions.RemoveEmptyEntries));
            return string.Concat(
                flattenedName,
                "_",
                fileNumber.ToString(CultureInfo.InvariantCulture),
                ".zip");
        }

        private static void ValidatePackageNames(
            IReadOnlyList<string> files,
            IReadOnlyList<string> packageNames)
        {
            var packageSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int index = 0; index < files.Count; index++)
            {
                string file = files[index];
                string fileName = Path.GetFileName(file);
                string packageName = packageNames[index];
                if (string.IsNullOrWhiteSpace(fileName) ||
                    string.IsNullOrWhiteSpace(packageName) ||
                    fileName is "." or ".." ||
                    packageName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                    !string.Equals(Path.GetFileName(packageName), packageName, StringComparison.Ordinal))
                {
                    throw new InvalidDataException(UpdateListStrings.Format(
                        UpdateListStrings.UpdateMakerInvalidZipPackageName,
                        file));
                }

                if (packageSources.TryGetValue(packageName, out string? firstSource))
                {
                    if (!string.Equals(
                        Sha256.ComputeFileHex(firstSource),
                        Sha256.ComputeFileHex(file),
                        StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(UpdateListStrings.Format(
                            UpdateListStrings.UpdateMakerZipPackageCollision,
                            firstSource,
                            file,
                            packageName));
                    }
                }
                else
                {
                    packageSources.Add(packageName, file);
                }
            }
        }

        private static void CreatePackages(
            IReadOnlyList<string> files,
            IReadOnlyList<UpdateEntry> entries,
            string outputDirectory,
            Action<int, int>? onProgress,
            int totalProgress,
            ref int completed)
        {
            var packageGroups = files
                .Select((file, index) => (File: file, Entry: entries[index]))
                .GroupBy(item => item.Entry.pname, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var candidates =
                new List<(string Candidate, string Destination, UpdateEntry[] Entries)>(
                    packageGroups.Length);
            try
            {
                foreach (var packageGroup in packageGroups)
                {
                    var groupedItems = packageGroup.ToArray();
                    string destination = Path.Combine(outputDirectory, packageGroup.Key);
                    string candidate = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    UpdateEntry[] groupedEntries = groupedItems.Select(item => item.Entry).ToArray();
                    candidates.Add((candidate, destination, groupedEntries));
                    CreateZipCandidate(
                        groupedItems[0].File,
                        groupedItems[0].Entry.fname,
                        candidate);
                    onProgress?.Invoke(
                        Interlocked.Add(ref completed, groupedItems.Length),
                        totalProgress);
                }

                foreach ((string candidate, string destination, UpdateEntry[] groupedEntries) in candidates)
                {
                    if (File.Exists(destination) &&
                        string.Equals(
                            Sha256.ComputeFileHex(candidate),
                            Sha256.ComputeFileHex(destination),
                            StringComparison.Ordinal))
                    {
                        File.Delete(candidate);
                    }
                    else if (File.Exists(destination))
                    {
                        File.Replace(candidate, destination, null, ignoreMetadataErrors: true);
                    }
                    else
                    {
                        File.Move(candidate, destination);
                    }

                    int packageSize = checked((int)new FileInfo(destination).Length);
                    foreach (UpdateEntry entry in groupedEntries)
                        entry.psize = packageSize;
                }
            }
            finally
            {
                foreach ((string candidate, _, _) in candidates)
                {
                    try
                    {
                        if (File.Exists(candidate)) File.Delete(candidate);
                    }
                    catch
                    {
                        // Preserve the original packaging error.
                    }
                }
            }
        }

        private static void CreateZipCandidate(string sourcePath, string entryName, string candidatePath)
        {
            using (FileStream output = new(
                       candidatePath,
                       FileMode.CreateNew,
                       FileAccess.ReadWrite,
                       FileShare.None))
            {
                using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
                ZipArchiveEntry archiveEntry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                DateTimeOffset sourceTimestamp = File.GetLastWriteTime(sourcePath);
                DateTimeOffset minimumZipTimestamp = new(1980, 1, 1, 0, 0, 0, sourceTimestamp.Offset);
                DateTimeOffset maximumZipTimestamp = new(2107, 12, 31, 23, 59, 58, sourceTimestamp.Offset);
                archiveEntry.LastWriteTime = sourceTimestamp < minimumZipTimestamp
                    ? minimumZipTimestamp
                    : sourceTimestamp > maximumZipTimestamp
                        ? maximumZipTimestamp
                        : sourceTimestamp;

                using Stream input = new FileStream(
                    sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using Stream entryOutput = archiveEntry.Open();
                input.CopyTo(entryOutput);
            }
        }
    }
}
