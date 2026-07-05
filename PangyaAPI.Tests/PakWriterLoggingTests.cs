using PangyaAPI.PAK.Flags;
using PangyaAPI.PAK.Models;
using PangyaAPI.Utilities.Logging;

namespace PangyaAPI.Tests;

public sealed class PakWriterLoggingTests
{
    [Fact]
    public void Writer_ReportsCreationAndValidationToLogSink()
    {
        using var temp = new TemporaryDirectory();
        string source = temp.Combine("source");
        string pak = temp.Combine("logged.pak");
        Directory.CreateDirectory(source);
        File.WriteAllText(Path.Combine(source, "sample.txt"), "sample");
        var sink = new RecordingLogSink();
        var writer = new PakWriter
        {
            EntryVersion = PakFileEntryVersion.V2,
            EntryType = PakFileEntryType.Raw,
            LogSink = sink
        };

        writer.CreateFromDirectoryContents(source, pak);

        Assert.Contains(sink.Messages, message => message.Contains("Creating PAK", StringComparison.Ordinal));
        Assert.Contains(sink.Messages, message => message.Contains("Validated", StringComparison.Ordinal));
        Assert.Contains(sink.Messages, message => message.Contains("Created PAK successfully", StringComparison.Ordinal));
    }

    private sealed class RecordingLogSink : ILogSink
    {
        public List<string> Messages { get; } = [];

        public void Log(string source, string message, LogSeverity severity = LogSeverity.Information)
        {
            Assert.Equal("PAK Writer", source);
            Messages.Add(message);
        }
    }
}
