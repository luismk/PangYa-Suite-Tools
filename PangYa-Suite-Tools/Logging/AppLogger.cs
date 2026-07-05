using PangyaAPI.Utilities.Logging;

namespace PangYa_Suite_Tools.Logging;

public sealed class AppLogger : IAppLogger, ILogSink
{
    private const int MaximumEntryCount = 10_000;
    private readonly Lock _syncRoot = new();
    private readonly List<AppLogEntry> _entries = [];
    private readonly string _logFilePath;
    private readonly string _preferencePath;
    private bool _fileLoggingEnabled;

    public AppLogger(string? logFilePath = null, string? preferencePath = null)
    {
        string applicationData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PangYa-Suite-Tools");
        _logFilePath = logFilePath ?? Path.Combine(applicationData, "application.log");
        _preferencePath = preferencePath ?? Path.Combine(applicationData, "file-logging.txt");
        _fileLoggingEnabled = LoadFileLoggingPreference();
    }

    public static AppLogger Instance { get; } = new();

    public event EventHandler<AppLogEntry>? EntryLogged;

    public string LogFilePath => _logFilePath;

    public bool FileLoggingEnabled
    {
        get
        {
            lock (_syncRoot) return _fileLoggingEnabled;
        }
        set
        {
            lock (_syncRoot)
            {
                _fileLoggingEnabled = value;
                SaveFileLoggingPreference(value);
            }
        }
    }

    public IReadOnlyList<AppLogEntry> GetEntries()
    {
        lock (_syncRoot)
        {
            return _entries.ToArray();
        }
    }

    public void Log(string source, string message, AppLogLevel level = AppLogLevel.Information)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(message);

        var entry = new AppLogEntry(DateTime.Now, source, level, message);
        lock (_syncRoot)
        {
            _entries.Add(entry);
            if (_entries.Count > MaximumEntryCount)
            {
                _entries.RemoveRange(0, _entries.Count - MaximumEntryCount);
            }
            if (_fileLoggingEnabled) AppendToFile(entry);
        }

        EntryLogged?.Invoke(this, entry);
    }

    void ILogSink.Log(string source, string message, LogSeverity severity) =>
        Log(source, message, severity switch
        {
            LogSeverity.Warning => AppLogLevel.Warning,
            LogSeverity.Error => AppLogLevel.Error,
            _ => AppLogLevel.Information
        });

    private void AppendToFile(AppLogEntry entry)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
            File.AppendAllText(LogFilePath,
                $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] [{entry.Source}] {entry.Message}{Environment.NewLine}");
        }
        catch
        {
            // Logging failures must never interrupt the operation being logged.
        }
    }

    private bool LoadFileLoggingPreference()
    {
        try
        {
            return File.Exists(_preferencePath) && bool.TryParse(File.ReadAllText(_preferencePath), out bool enabled) && enabled;
        }
        catch
        {
            return false;
        }
    }

    private void SaveFileLoggingPreference(bool enabled)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_preferencePath)!);
            File.WriteAllText(_preferencePath, enabled.ToString());
        }
        catch
        {
            // A preference failure must not disable in-memory logging.
        }
    }
}
