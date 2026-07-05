using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;

namespace PangYa_Suite_Tools;

public sealed class FrmLog : Form
{
    private readonly IAppLogger _logger;
    private readonly TextBox _logText = new();
    private readonly Button _clearButton = new();
    private readonly CheckBox _fileLoggingCheckBox = new();
    private bool _initializingFileLogging;

    public FrmLog() : this(AppLogger.Instance)
    {
    }

    public FrmLog(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        InitializeComponent();
        _initializingFileLogging = true;
        _fileLoggingCheckBox.Checked = _logger is AppLogger appLogger && appLogger.FileLoggingEnabled;
        _initializingFileLogging = false;
        ApplyLocalization();

        foreach (AppLogEntry entry in _logger.GetEntries())
        {
            AppendEntry(entry);
        }

        _logger.EntryLogged += Logger_EntryLogged;
        LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.EntryLogged -= Logger_EntryLogged;
            LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        _logText.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _logText.BackColor = Color.FromArgb(24, 24, 24);
        _logText.ForeColor = Color.Gainsboro;
        _logText.Font = new Font("Consolas", 9F);
        _logText.Location = new Point(12, 12);
        _logText.Multiline = true;
        _logText.Name = "txtLog";
        _logText.ReadOnly = true;
        _logText.ScrollBars = ScrollBars.Both;
        _logText.Size = new Size(760, 400);
        _logText.WordWrap = false;

        _clearButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _clearButton.Location = new Point(672, 423);
        _clearButton.Name = "btnClear";
        _clearButton.Size = new Size(100, 30);
        _clearButton.Click += (_, _) => _logText.Clear();

        _fileLoggingCheckBox.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _fileLoggingCheckBox.AutoSize = true;
        _fileLoggingCheckBox.Location = new Point(12, 430);
        _fileLoggingCheckBox.Name = "chkLogToFile";
        _fileLoggingCheckBox.CheckedChanged += FileLoggingCheckBox_CheckedChanged;

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(784, 465);
        Controls.Add(_logText);
        Controls.Add(_clearButton);
        Controls.Add(_fileLoggingCheckBox);
        MinimumSize = new Size(500, 300);
        Name = nameof(FrmLog);
        StartPosition = FormStartPosition.CenterParent;
    }

    private void ApplyLocalization()
    {
        Text = Strings.Log_Title;
        _clearButton.Text = Strings.Log_Clear;
        _fileLoggingCheckBox.Text = Strings.Log_ToFile;
    }

    private void LocalizationManager_CultureChanged(object? sender, EventArgs e) => ApplyLocalization();

    private void FileLoggingCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        if (_initializingFileLogging || _logger is not AppLogger appLogger) return;
        appLogger.FileLoggingEnabled = _fileLoggingCheckBox.Checked;
        appLogger.Log("Application Log",
            _fileLoggingCheckBox.Checked
                ? $"File logging enabled: {appLogger.LogFilePath}"
                : "File logging disabled.");
    }

    private void Logger_EntryLogged(object? sender, AppLogEntry entry)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            BeginInvoke(() => AppendEntry(entry));
            return;
        }

        AppendEntry(entry);
    }

    private void AppendEntry(AppLogEntry entry)
    {
        _logText.AppendText($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss}] [{entry.Level}] [{entry.Source}] {entry.Message}{Environment.NewLine}");
        _logText.SelectionStart = _logText.TextLength;
        _logText.ScrollToCaret();
    }
}
