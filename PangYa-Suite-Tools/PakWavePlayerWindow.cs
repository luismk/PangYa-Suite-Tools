using PangYa_Suite_Tools.Localization;
using System.Windows.Media;

namespace PangYa_Suite_Tools;

internal sealed class PakWavePlayerWindow : Form
{
    private readonly MediaPlayer _player = new();
    private readonly Button _play = new() { AutoSize = true };
    private readonly Button _pause = new() { AutoSize = true, Enabled = false };
    private readonly Button _stop = new() { AutoSize = true, Enabled = false };
    private readonly Label _error = new() { AutoSize = true, MaximumSize = new Size(450, 0), Visible = false };
    private readonly string _entryName;
    private readonly string _path;
    private string? _errorDetail;

    internal PakWavePlayerWindow(string entryName, string path)
    {
        _entryName = entryName;
        _path = path;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        AutoScaleMode = AutoScaleMode.Font;
        ShowInTaskbar = false;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var layout = new FlowLayoutPanel
        {
            AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, Padding = new Padding(12)
        };
        var controls = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        controls.Controls.AddRange([_play, _pause, _stop]);
        layout.Controls.Add(controls);
        layout.Controls.Add(_error);
        Controls.Add(layout);

        _play.Enabled = false;
        _play.Click += (_, _) => Play();
        _pause.Click += (_, _) =>
        {
            _player.Pause();
            SetPlaybackControls(playing: false, canStop: true);
        };
        _stop.Click += (_, _) => ResetPlayback();
        _player.MediaOpened += (_, _) => Play();
        _player.MediaEnded += (_, _) => ResetPlayback();
        _player.MediaFailed += (_, e) => ShowFailure(e.ErrorException);

        ApplyLocalization();
        LocalizationManager.CultureChanged += CultureChanged;
        Disposed += (_, _) =>
        {
            LocalizationManager.CultureChanged -= CultureChanged;
            _player.Close();
        };
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try { _player.Open(new Uri(_path)); }
        catch (Exception ex) when (ex is IOException or InvalidOperationException or NotSupportedException or UriFormatException)
        {
            ShowFailure(ex);
        }
    }

    private void Play()
    {
        _player.Play();
        SetPlaybackControls(playing: true, canStop: true);
    }

    private void ResetPlayback()
    {
        _player.Stop();
        SetPlaybackControls(playing: false, canStop: false);
    }

    private void SetPlaybackControls(bool playing, bool canStop)
    {
        _play.Enabled = !playing;
        _pause.Enabled = playing;
        _stop.Enabled = canStop;
    }

    private void ShowFailure(Exception exception)
    {
        _errorDetail = exception.Message;
        ApplyLocalization();
        _error.Visible = true;
        _play.Enabled = _pause.Enabled = _stop.Enabled = false;
    }

    private void CultureChanged(object? sender, EventArgs e) => ApplyLocalization();

    private void ApplyLocalization()
    {
        Text = $"{Strings.PakFile_Preview} — {_entryName}";
        _play.Text = Strings.PakFile_AudioPlay;
        _pause.Text = Strings.PakFile_AudioPause;
        _stop.Text = Strings.PakFile_AudioStop;
        if (_errorDetail != null)
            _error.Text = string.Format(Strings.PakFile_PreviewUnavailable, _errorDetail);
    }
}
