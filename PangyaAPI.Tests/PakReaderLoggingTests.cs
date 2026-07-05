using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using PangyaAPI.Utilities.Logging;

namespace PangyaAPI.Tests;

public sealed class PakReaderLoggingTests
{
    [Fact]
    public void Reader_ReportsLifecycleParsingAndExtractionToLogSink()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("source");
        string pak = temp.Combine("logged.pak");
        string output = temp.Combine("output");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "sample.txt"), "sample");
        new PakWriter
        {
            EntryVersion = PakFileEntryVersion.V2,
            EntryType = PakFileEntryType.Raw
        }.CreateFromDirectoryContents(source, pak);
        var sink = new RecordingLogSink();

        using (var reader = new PakReader(pak, logSink: sink))
        {
            reader.Parse();
            reader.Extract("*", output);
        }

        Assert.Contains(sink.Messages, message => message.Contains("Opened", StringComparison.Ordinal));
        Assert.Contains(sink.Messages, message => message.Contains("Parsed", StringComparison.Ordinal));
        Assert.Contains(sink.Messages, message => message.Contains("Extracted", StringComparison.Ordinal));
        Assert.Contains(sink.Messages, message => message.Contains("Closed", StringComparison.Ordinal));
    }

    private sealed class RecordingLogSink : ILogSink
    {
        public List<string> Messages { get; } = [];

        public void Log(string source, string message, LogSeverity severity = LogSeverity.Information)
        {
            Assert.Equal("PAK Reader", source);
            Messages.Add(message);
        }
    }
}
