namespace PangYa_Suite_Tools.Logging;

public enum AppLogLevel
{
    Information,
    Warning,
    Error
}

public sealed record AppLogEntry(DateTime Timestamp, string Source, AppLogLevel Level, string Message);

public interface IAppLogger
{
    event EventHandler<AppLogEntry>? EntryLogged;

    IReadOnlyList<AppLogEntry> GetEntries();

    void Log(string source, string message, AppLogLevel level = AppLogLevel.Information);
}
