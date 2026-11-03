using System.Security;
using PangyaAPI.UpdateList.Localization;

namespace PangyaAPI.UpdateList.Models;

public sealed class UpdateListVerifier
{
    public IReadOnlyList<UpdateFileVerificationResult> VerifyDirectory(
        string rootDirectory,
        IReadOnlyList<UpdateEntry> entries,
        Action<int, int, UpdateFileVerificationResult>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        ArgumentNullException.ThrowIfNull(entries);

        string rootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootDirectory));
        if (!Directory.Exists(rootPath))
            throw new DirectoryNotFoundException(rootPath);

        var results = new List<UpdateFileVerificationResult>(entries.Count);
        for (int index = 0; index < entries.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UpdateEntry entry = entries[index]
                ?? throw new ArgumentException("Update-list entries must not contain null values.", nameof(entries));
            UpdateFileVerificationResult result = VerifyEntry(rootPath, entry, cancellationToken);
            results.Add(result);
            onProgress?.Invoke(index + 1, entries.Count, result);
        }

        return results;
    }

    private static UpdateFileVerificationResult VerifyEntry(
        string rootPath,
        UpdateEntry entry,
        CancellationToken cancellationToken)
    {
        if (!TryResolvePath(rootPath, entry, out string? fullPath, out string? pathError))
        {
            return new UpdateFileVerificationResult(
                entry,
                null,
                UpdateFileVerificationStatus.InvalidPath,
                null,
                pathError);
        }

        try
        {
            if (!File.Exists(fullPath))
            {
                return new UpdateFileVerificationResult(
                    entry,
                    fullPath,
                    UpdateFileVerificationStatus.Missing,
                    null,
                    null);
            }

            if (ContainsReparsePoint(rootPath, fullPath))
            {
                return new UpdateFileVerificationResult(
                    entry,
                    fullPath,
                    UpdateFileVerificationStatus.InvalidPath,
                    null,
                    UpdateListStrings.UpdateListVerifierReparsePoint);
            }

            int actualCrc = new Crc32().CalculateFileCRC(fullPath, cancellationToken);
            UpdateFileVerificationStatus status = actualCrc == entry.fcrc
                ? UpdateFileVerificationStatus.Match
                : UpdateFileVerificationStatus.ChecksumMismatch;
            return new UpdateFileVerificationResult(entry, fullPath, status, actualCrc, null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException
                                   or UnauthorizedAccessException
                                   or SecurityException
                                   or NotSupportedException)
        {
            return new UpdateFileVerificationResult(
                entry,
                fullPath,
                UpdateFileVerificationStatus.ReadError,
                null,
                ex.Message);
        }
    }

    private static bool TryResolvePath(
        string rootPath,
        UpdateEntry entry,
        out string? fullPath,
        out string? error)
    {
        fullPath = null;
        error = null;
        string fileName = entry.fname?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(fileName)
            || Path.IsPathRooted(fileName)
            || fileName.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '\\', '/']) >= 0
            || fileName is "." or "..")
        {
            error = UpdateListStrings.UpdateListVerifierEntryLeafFilenameUnsafe;
            return false;
        }

        try
        {
            string relativeDirectory = (entry.fdir ?? string.Empty)
                .Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);
            if (Path.IsPathRooted(relativeDirectory))
            {
                error = UpdateListStrings.UpdateListVerifierDirectoryIsRooted;
                return false;
            }

            string candidate = Path.GetFullPath(Path.Combine(rootPath, relativeDirectory, fileName));
            string rootPrefix = rootPath + Path.DirectorySeparatorChar;
            if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                error = UpdateListStrings.UpdateListVerifierPathEscapesRoot;
                return false;
            }

            fullPath = candidate;
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException
                                   or IOException
                                   or NotSupportedException
                                   or SecurityException)
        {
            error = ex.Message;
            return false;
        }
    }

    private static bool ContainsReparsePoint(string rootPath, string fullPath)
    {
        string relativePath = Path.GetRelativePath(rootPath, fullPath);
        string currentPath = rootPath;
        foreach (string part in relativePath.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            currentPath = Path.Combine(currentPath, part);
            if ((File.GetAttributes(currentPath) & FileAttributes.ReparsePoint) != 0)
                return true;
        }

        return false;
    }
}
