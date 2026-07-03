using System.Diagnostics;
using PangyaAPI.PAK.Models;
using Xunit.Abstractions;

namespace PangyaAPI.Tests;

public sealed class PakPerformanceProbeTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(4, 1_048_576, "large-files")]
    [InlineData(128, 1024, "many-small-files")]
    public async Task StreamingWriter_RecordsSyntheticMetrics(int fileCount, int fileSize, string scenario)
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine(scenario);
        Directory.CreateDirectory(source);
        byte[] contents = Enumerable.Range(0, fileSize).Select(index => (byte)(index * 17)).ToArray();
        for (int index = 0; index < fileCount; index++)
            File.WriteAllBytes(Path.Combine(source, $"file-{index:D4}.bin"), contents);

        long baseline = GC.GetTotalMemory(forceFullCollection: true);
        long peak = baseline;
        using var finished = new CancellationTokenSource();
        Task sampler = Task.Run(async () =>
        {
            while (!finished.IsCancellationRequested)
            {
                InterlockedExtensions.Max(ref peak, GC.GetTotalMemory(forceFullCollection: false));
                await Task.Delay(2);
            }
        });

        var stopwatch = Stopwatch.StartNew();
        new PakWriter { MaxBufferedBytes = 64L * 1024 * 1024, MaxDegreeOfParallelism = 4 }
            .CreateFromDirectoryContents(source, temp.Combine(scenario + ".pak"));
        stopwatch.Stop();
        finished.Cancel();
        await sampler;

        output.WriteLine("scenario={0}; files={1}; sourceBytes={2}; elapsedMs={3}; sampledManagedDelta={4}",
            scenario, fileCount, (long)fileCount * fileSize, stopwatch.ElapsedMilliseconds, Math.Max(0, peak - baseline));
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref long target, long value)
        {
            long current;
            while (value > (current = Volatile.Read(ref target)) &&
                   Interlocked.CompareExchange(ref target, value, current) != current) { }
        }
    }
}
