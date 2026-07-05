namespace PangyaAPI.Utilities.Logging;

public enum LogSeverity
{
    Information,
    Warning,
    Error
}

public interface ILogSink
{
    void Log(string source, string message, LogSeverity severity = LogSeverity.Information);
}
