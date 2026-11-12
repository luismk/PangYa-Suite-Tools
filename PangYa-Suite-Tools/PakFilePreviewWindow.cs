using System.Windows.Forms.Integration;
using System.Windows.Media.Media3D;
using PangYa_Suite_Tools.Localization;
using Wpf = System.Windows;
using Media = System.Windows.Media;
using Input = System.Windows.Input;

namespace PangYa_Suite_Tools;

internal sealed class PakFilePreviewWindow : Form
{
    private readonly string _entryName;
    private readonly PakFilePreviewData _preview;
    private Label? _notice;
    private Button? _reset;
    private ToolStripLabel? _encodingLabel;
    private ToolStripComboBox? _encodings;
    private Label? _axisLockStatus;
    private PakModelOrbit? _orbit;

    public PakFilePreviewWindow(string entryName, PakFilePreviewData preview)
    {
        _entryName = entryName;
        _preview = preview;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(820, 580);
        MinimumSize = new Size(380, 260);

        if (preview.TextBytes is { } bytes)
            ShowText(bytes);
        else if (preview.Image is { } image)
            Controls.Add(new PictureBox { Image = image, SizeMode = PictureBoxSizeMode.Zoom, Dock = DockStyle.Fill });
        else if (preview.Model is { } model)
            ShowModel(model);
        else
        {
            _notice = new Label { Dock = DockStyle.Fill, Padding = new Padding(12) };
            Controls.Add(_notice);
        }

        ApplyLocalization();
        LocalizationManager.CultureChanged += CultureChanged;
        Disposed += (_, _) => LocalizationManager.CultureChanged -= CultureChanged;
    }

    private void CultureChanged(object? sender, EventArgs args) => ApplyLocalization();

    private void ApplyLocalization()
    {
        Text = $"{Strings.PakFile_Preview} — {_entryName}";
        if (_encodingLabel != null)
            _encodingLabel.Text = Strings.PakFile_TextEncoding;
        if (_encodings != null)
            _encodings.Items[0] = Strings.PakFile_PreviewAuto;
        if (_reset != null)
            _reset.Text = Strings.PakFile_PreviewReset;
        UpdateAxisLockStatus();
        if (_notice != null)
        {
            _notice.Text = _preview.Sections is { } sections
                ? string.Format(Strings.PakFile_PreviewNoMesh, string.Join(", ", sections))
                : Strings.PakFile_PreviewMissingTextures;
        }
    }

    private void ShowText(byte[] bytes)
    {
        var text = new TextBox
        {
            Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, WordWrap = false,
            ScrollBars = ScrollBars.Both, MaxLength = int.MaxValue, Text = ToWindowsLines(_preview.Text ?? string.Empty)
        };
        var strip = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden };
        var encodings = new ToolStripComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
        _encodingLabel = new ToolStripLabel();
        _encodings = encodings;
        encodings.Items.AddRange([Strings.PakFile_PreviewAuto, "UTF-8", "Shift-JIS (932)", "Korean (949)", "Windows-1252"]);
        int?[] codePages = [null, 65001, 932, 949, 1252];
        encodings.SelectedIndex = 0;
        encodings.SelectedIndexChanged += (_, _) =>
        {
            if (encodings.SelectedIndex >= 0)
                text.Text = ToWindowsLines(PakFilePreviewData.DecodeText(bytes, codePages[encodings.SelectedIndex], _preview.LegacyCodePage));
        };
        strip.Items.AddRange([_encodingLabel, encodings]);
        Controls.Add(text);
        Controls.Add(strip);
        Shown += (_, _) => text.Select(0, 0);
    }

    private static string ToWindowsLines(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            .Replace("\n", "\r\n", StringComparison.Ordinal);

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_orbit != null && keyData is Keys.X or Keys.Y or Keys.Z)
        {
            if ((msg.LParam.ToInt64() & (1L << 30)) == 0)
                ToggleAxisLock(keyData);
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void ToggleAxisLock(Keys key)
    {
        if (_orbit?.ToggleAxis(key) == true)
            UpdateAxisLockStatus();
    }

    private void UpdateAxisLockStatus()
    {
        if (_axisLockStatus == null || _orbit == null)
            return;
        _axisLockStatus.Text = _orbit.LockedAxis switch
        {
            Keys.None => Strings.PakFile_AxisLockHint,
            Keys.X => Strings.PakFile_AxisDragVertical,
            _ => string.Format(Strings.PakFile_AxisDragHorizontal, _orbit.LockedAxis)
        };
        _axisLockStatus.AccessibleDescription = _orbit.LockedAxis == Keys.None
            ? null
            : string.Format(Strings.PakFile_AxisLocked, _orbit.LockedAxis);
    }

    private void ShowModel(Model3DGroup model)
    {
        var camera = new PerspectiveCamera { FieldOfView = 45 };
        var viewport = new Wpf.Controls.Viewport3D { Camera = camera, ClipToBounds = true };
        var scene = new Model3DGroup();
        scene.Children.Add(model);
        scene.Children.Add(new AmbientLight(Media.Color.FromRgb(160, 160, 160)));
        scene.Children.Add(new DirectionalLight(Media.Color.FromRgb(120, 120, 120), new Vector3D(-1, -2, -3)));
        viewport.Children.Add(new ModelVisual3D { Content = scene });
        var axes = new PakModelAxisIndicator
        {
            HorizontalAlignment = Wpf.HorizontalAlignment.Left,
            VerticalAlignment = Wpf.VerticalAlignment.Bottom,
            Margin = new Wpf.Thickness(10)
        };
        var layers = new Wpf.Controls.Grid();
        layers.Children.Add(viewport);
        layers.Children.Add(axes);
        var background = new Wpf.Controls.Border { Background = Media.Brushes.DimGray, Child = layers, Focusable = true };
        var host = new ElementHost { Dock = DockStyle.Fill, Child = background };
        Controls.Add(host);

        Rect3D bounds = model.Bounds;
        var center = new Point3D(bounds.X + bounds.SizeX / 2, bounds.Y + bounds.SizeY / 2, bounds.Z + bounds.SizeZ / 2);
        double radius = Math.Max(new Vector3D(bounds.SizeX, bounds.SizeY, bounds.SizeZ).Length / 2, 0.01);
        var orbit = new PakModelOrbit();
        _orbit = orbit;
        double zoom = 1;
        Wpf.Point? drag = null;

        void PositionCamera()
        {
            double aspect = viewport.ActualWidth / Math.Max(1, viewport.ActualHeight);
            double tangent = Math.Tan(Math.PI / 8) / Math.Max(1, aspect);
            double distance = radius * 1.12 / Math.Sin(Math.Atan(tangent)) * zoom;
            Vector3D offset = orbit.Direction * distance;
            camera.Position = center + offset;
            camera.LookDirection = -offset;
            camera.UpDirection = orbit.Up;
            camera.NearPlaneDistance = radius * 0.001;
            camera.FarPlaneDistance = distance + radius * 2;
            axes.SetCameraDirection(orbit.Direction, orbit.Up);
        }

        background.PreviewKeyDown += (_, e) =>
        {
            if (Input.Keyboard.Modifiers == Input.ModifierKeys.None &&
                e.Key is Input.Key.X or Input.Key.Y or Input.Key.Z)
            {
                if (!e.IsRepeat)
                    ToggleAxisLock((Keys)Input.KeyInterop.VirtualKeyFromKey(e.Key));
                e.Handled = true;
            }
        };
        background.MouseLeftButtonDown += (_, e) =>
        {
            background.Focus();
            drag = e.GetPosition(background);
            background.CaptureMouse();
        };
        background.MouseMove += (_, e) =>
        {
            if (drag is not { } start || e.LeftButton != Input.MouseButtonState.Pressed)
                return;
            Wpf.Point current = e.GetPosition(background);
            orbit.Drag(current.X - start.X, current.Y - start.Y);
            drag = current;
            PositionCamera();
        };
        background.MouseLeftButtonUp += (_, _) => background.ReleaseMouseCapture();
        background.LostMouseCapture += (_, _) => drag = null;
        background.MouseWheel += (_, e) =>
        {
            zoom = Math.Clamp(zoom * Math.Pow(0.85, e.Delta / 120.0), 0.3, 8);
            PositionCamera();
            e.Handled = true;
        };
        viewport.SizeChanged += (_, _) => PositionCamera();
        PositionCamera();

        var tools = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        _reset = new Button { AutoSize = true };
        _reset.Click += (_, _) =>
        {
            orbit.Reset();
            zoom = 1;
            PositionCamera();
        };
        tools.Controls.Add(_reset);
        _axisLockStatus = new Label { AutoSize = true, Margin = new Padding(8, 8, 0, 0) };
        tools.Controls.Add(_axisLockStatus);
        if (_preview.MissingTextures)
        {
            _notice = new Label { AutoSize = true, Margin = new Padding(8, 8, 0, 0) };
            tools.Controls.Add(_notice);
        }
        Controls.Add(tools);
    }
}
