using PangYa_Suite_Tools.Logging;
using Xunit;

namespace PangYa_Suite_Tools.Tests;

public sealed class AppLoggerTests
{
    [Fact]
    public void Log_RetainsEntryAndPublishesIt()
    {
        var logger = new AppLogger();
        AppLogEntry? published = null;
        logger.EntryLogged += (_, entry) => published = entry;

        logger.Log("Test tool", "Something happened", AppLogLevel.Warning);

        AppLogEntry entry = Assert.Single(logger.GetEntries());
        Assert.Same(entry, published);
        Assert.Equal("Test tool", entry.Source);
        Assert.Equal("Something happened", entry.Message);
        Assert.Equal(AppLogLevel.Warning, entry.Level);
    }

    [Fact]
    public void GetEntries_ReturnsAnIndependentSnapshot()
    {
        var logger = new AppLogger();
        logger.Log("Test", "First");
        IReadOnlyList<AppLogEntry> snapshot = logger.GetEntries();

        logger.Log("Test", "Second");

        Assert.Single(snapshot);
        Assert.Equal(2, logger.GetEntries().Count);
    }

    [Fact]
    public void EnabledFileLogging_WritesAlongsideInMemoryLogging()
    {
        string directory = Path.Combine(Path.GetTempPath(), "PangYaAppLoggerTests", Guid.NewGuid().ToString("N"));
        string logPath = Path.Combine(directory, "application.log");
        string preferencePath = Path.Combine(directory, "preference.txt");
        try
        {
            var logger = new AppLogger(logPath, preferencePath) { FileLoggingEnabled = true };

            logger.Log("Test source", "Persist this message", AppLogLevel.Warning);

            Assert.Single(logger.GetEntries());
            string fileContents = File.ReadAllText(logPath);
            Assert.Contains("[Warning] [Test source] Persist this message", fileContents);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void FileLoggingPreference_IsLoadedWithoutOpeningLogWindow()
    {
        string directory = Path.Combine(Path.GetTempPath(), "PangYaAppLoggerTests", Guid.NewGuid().ToString("N"));
        string logPath = Path.Combine(directory, "application.log");
        string preferencePath = Path.Combine(directory, "preference.txt");
        try
        {
            var firstLogger = new AppLogger(logPath, preferencePath) { FileLoggingEnabled = true };
            var restartedLogger = new AppLogger(logPath, preferencePath);

            restartedLogger.Log("Background", "Viewer is closed");

            Assert.True(restartedLogger.FileLoggingEnabled);
            Assert.Contains("Viewer is closed", File.ReadAllText(logPath));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
