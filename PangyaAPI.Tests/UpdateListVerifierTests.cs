using PangyaAPI.UpdateList.Models;

namespace PangyaAPI.Tests;

public sealed class UpdateListVerifierTests
{
    [Fact]
    public void VerifyDirectory_ReportsMatchesMismatchesMissingFilesAndDuplicatesInOrder()
    {
        using var temp = new TemporaryDirectory();
        string nested = temp.Combine("data", "item");
        Directory.CreateDirectory(nested);
        string matchingPath = Path.Combine(nested, "match.bin");
        string mismatchPath = temp.Combine("mismatch.bin");
        File.WriteAllBytes(matchingPath, [1, 2, 3, 4]);
        File.WriteAllBytes(mismatchPath, [5, 6, 7, 8]);
        int matchingCrc = new Crc32().CalculateFileCRC(matchingPath);

        UpdateEntry matching = Entry("match.bin", @"\data\item", matchingCrc);
        UpdateEntry mismatch = Entry("mismatch.bin", @"\", 123);
        UpdateEntry missing = Entry("missing.bin", @"\", 456);
        IReadOnlyList<UpdateFileVerificationResult> results = new UpdateListVerifier().VerifyDirectory(
            temp.Path,
            [matching, mismatch, missing, matching]);

        Assert.Equal(
            [
                UpdateFileVerificationStatus.Match,
                UpdateFileVerificationStatus.ChecksumMismatch,
                UpdateFileVerificationStatus.Missing,
                UpdateFileVerificationStatus.Match
            ],
            results.Select(result => result.Status));
        Assert.Equal(matchingCrc, results[0].ActualCrc);
        Assert.NotEqual(mismatch.fcrc, results[1].ActualCrc);
        Assert.Null(results[2].ActualCrc);
        Assert.Same(matching, results[3].Entry);
    }

    [Theory]
    [InlineData("../outside.bin", "")]
    [InlineData("outside.bin", "../")]
    [InlineData("sub/file.bin", "")]
    [InlineData("C:\\outside.bin", "")]
    public void VerifyDirectory_RejectsUnsafePaths(string fileName, string directory)
    {
        using var temp = new TemporaryDirectory();

        UpdateFileVerificationResult result = Assert.Single(new UpdateListVerifier().VerifyDirectory(
            temp.Path,
            [Entry(fileName, directory, 0)]));

        Assert.Equal(UpdateFileVerificationStatus.InvalidPath, result.Status);
        Assert.Null(result.FullPath);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Fact]
    public void VerifyDirectory_HonorsCancellationBeforeReading()
    {
        using var temp = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => new UpdateListVerifier().VerifyDirectory(
            temp.Path,
            [Entry("file.bin", @"\", 0)],
            cancellationToken: cancellation.Token));
    }

    [Fact]
    public void VerifyDirectory_ReportsProgressForEveryEntry()
    {
        using var temp = new TemporaryDirectory();
        var progress = new List<(int Completed, int Total, UpdateFileVerificationStatus Status)>();

        new UpdateListVerifier().VerifyDirectory(
            temp.Path,
            [Entry("one.bin", @"\", 0), Entry("two.bin", @"\", 0)],
            (completed, total, result) => progress.Add((completed, total, result.Status)));

        Assert.Equal(
            [(1, 2, UpdateFileVerificationStatus.Missing), (2, 2, UpdateFileVerificationStatus.Missing)],
            progress);
    }

    [Fact]
    public void VerifyDirectory_ReportsReadErrorsWithoutStoppingRemainingEntries()
    {
        using var temp = new TemporaryDirectory();
        string lockedPath = temp.Combine("locked.bin");
        File.WriteAllBytes(lockedPath, [1, 2, 3]);
        using var locked = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        IReadOnlyList<UpdateFileVerificationResult> results = new UpdateListVerifier().VerifyDirectory(
            temp.Path,
            [Entry("locked.bin", @"\", 0), Entry("missing.bin", @"\", 0)]);

        Assert.Equal(UpdateFileVerificationStatus.ReadError, results[0].Status);
        Assert.False(string.IsNullOrWhiteSpace(results[0].ErrorMessage));
        Assert.Equal(UpdateFileVerificationStatus.Missing, results[1].Status);
    }

    private static UpdateEntry Entry(string fileName, string directory, int crc) => new()
    {
        fname = fileName,
        fdir = directory,
        fcrc = crc
    };
}
