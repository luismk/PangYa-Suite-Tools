using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;
using PangyaAPI.UI;
using System.ComponentModel;
using System.Xml;

namespace PangYa_Suite_Tools.Shop;

internal sealed class FrmPangyaUiEditor : Form
{
    private const string LogSource = "UI Editor";
    private readonly string _dataRoot;
    private readonly IReadOnlyList<string> _uiFiles;
    private readonly PangyaFileImageProvider _assets;
    private readonly ToolStrip _toolbar = new();
    private readonly ToolStripComboBox _fileSelector = new();
    private readonly ToolStripButton _openButton = new();
    private readonly ToolStripButton _wallpaperButton = new();
    private readonly ToolStripButton _saveButton = new();
    private readonly ToolStripButton _reloadButton = new();
    private readonly ToolStripLabel _stateLabel = new();
    private readonly ToolStripComboBox _stateSelector = new();
    private readonly ToolStripButton _debugButton = new();
    private readonly ToolStripButton _zoomOutButton = new();
    private readonly ToolStripLabel _zoomLabel = new();
    private readonly ToolStripButton _zoomInButton = new();
    private readonly ToolStripSeparator _ifdefSeparator = new() { Visible = false };
    private readonly ToolStripLabel _ifdefLabel = new() { Visible = false };
    private readonly List<ToolStripControlHost> _ifdefControls = [];
    private readonly HashSet<string> _enabledSymbols = new(StringComparer.OrdinalIgnoreCase);
    private readonly TreeView _elementTree = new();
    private readonly PangyaUiCanvas _canvas;
    private readonly PangyaUiPropertyPanel _properties;
    private readonly ToolStripStatusLabel _statusLabel = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly StatusStrip _status = new();
    private PangyaUiDocument? _document;
    private string? _currentPath;
    private bool _dirty;
    private bool _changingFile;

    private FrmPangyaUiEditor(string dataRoot, IReadOnlyList<string> uiFiles,
        PangyaFileImageProvider assets)
        : this(dataRoot, uiFiles, assets, PangyaUiResourceCatalog.Empty)
    {
    }

    private FrmPangyaUiEditor(string dataRoot, IReadOnlyList<string> uiFiles,
        PangyaFileImageProvider assets,
        PangyaUiResourceCatalog resourceCatalog)
    {
        _dataRoot = dataRoot;
        _uiFiles = uiFiles;
        _assets = assets;
        _canvas = new PangyaUiCanvas(assets, resourceCatalog) { Dock = DockStyle.None };
        Activated += (_, _) => _canvas.SetViewFocused(true);
        Deactivate += (_, _) => _canvas.SetViewFocused(false);
        _properties = new PangyaUiPropertyPanel(assets);

        Name = "frmPangyaUiEditor";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(1280, 800);
        MinimumSize = new Size(960, 640);
        KeyPreview = true;

        ConfigureToolbar();
        ConfigureWorkspace();
        _status.Items.Add(_statusLabel);
        Controls.Add(_status);
        Controls.Add(_toolbar);

        _canvas.SelectionChanged += (_, node) => SelectNode(node, fromCanvas: true);
        _canvas.ElementChanged += (_, e) =>
        {
            MarkDirty();
            _properties.SelectedNode = e.Node;
            RefreshSelectedTreeNode();
        };
        _properties.ElementChanged += (_, _) =>
        {
            MarkDirty();
            RefreshSelectedTreeNode();
            _canvas.ResetViewerScroll(_properties.SelectedNode);
            _canvas.Invalidate();
        };
        _elementTree.AfterSelect += (_, e) =>
        {
            if (e.Node?.Tag is PangyaUiNode node) SelectNode(node, fromCanvas: false);
        };
        FormClosing += FrmPangyaUiEditor_FormClosing;
        LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
        Disposed += (_, _) =>
        {
            LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
            _assets.Dispose();
        };
        ApplyLocalization();
        PopulateFileSelector();
    }

    public static async Task<FrmPangyaUiEditor> CreateAsync(string dataRoot,
        CancellationToken cancellationToken = default, string? startupWallpaper = null)
    {
        string fullRoot = Path.GetFullPath(dataRoot);
        (IReadOnlyList<string> files, PangyaFileImageProvider assets,
            PangyaUiResourceCatalog catalog) =
            await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<string> discovered = PangyaUiDocument.FindUiFiles(fullRoot);
            if (discovered.Count == 0) throw new InvalidDataException(Strings.UiEditor_NoXmlFiles);
            var resolver = new PangyaFileImageProvider(fullRoot);
            PangyaUiResourceCatalog catalog = PangyaUiResourceCatalog.Load(discovered);
            return (discovered, resolver, catalog);
        }, cancellationToken);
        var editor = new FrmPangyaUiEditor(fullRoot, files, assets, catalog);
        editor._canvas.StartupWallpaper = startupWallpaper;
        editor._wallpaperButton.Checked = !string.IsNullOrEmpty(startupWallpaper);
        try
        {
            if (!string.IsNullOrWhiteSpace(startupWallpaper))
            {
                string wallpaper = startupWallpaper;
                Image? image = await Task.Run(() =>
                {
                    string? path = Path.IsPathFullyQualified(wallpaper)
                        ? wallpaper : assets.TryResolvePath(wallpaper);
                    return PangyaImageLoader.Load(path);
                }, cancellationToken);
                editor._canvas.SetStartupWallpaperImage(image);
                cancellationToken.ThrowIfCancellationRequested();
            }
            string initial = files.FirstOrDefault(path =>
                Path.GetFileName(path).Equals("shop.xml",
                    StringComparison.OrdinalIgnoreCase)) ?? files[0];
            await editor.LoadDocumentAsync(initial, cancellationToken);
            return editor;
        }
        catch
        {
            editor.Dispose();
            throw;
        }
    }

    private void ConfigureToolbar()
    {
        _wallpaperButton.Click += async (_, _) => await SelectStartupWallpaperAsync();
        _toolbar.Name = "uiEditorToolbar";
        _toolbar.Dock = DockStyle.Top;
        _toolbar.GripStyle = ToolStripGripStyle.Hidden;
        _toolbar.Padding = new Padding(4, 2, 4, 2);
        _toolbar.Height = 32;

        _fileSelector.Name = "cboUiFiles";
        _fileSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _fileSelector.AutoSize = false;
        _fileSelector.Width = 300;
        _fileSelector.SelectedIndexChanged += async (_, _) =>
        {
            if (_changingFile || _fileSelector.SelectedItem is not UiFileOption option) return;
            if (!ConfirmDiscard())
            {
                SelectCurrentFile();
                return;
            }
            await LoadDocumentAsync(option.Path, CancellationToken.None);
        };

        _openButton.Name = "btnOpenUiXml";
        _openButton.Click += async (_, _) => await BrowseForXmlAsync();
        _saveButton.Name = "btnSaveUiXml";
        _saveButton.Click += async (_, _) => await SaveAsync();
        _reloadButton.Name = "btnReloadUiXml";
        _reloadButton.Click += async (_, _) =>
        {
            if (_currentPath is null || !ConfirmDiscard()) return;
            await LoadDocumentAsync(_currentPath, CancellationToken.None);
        };
        _debugButton.Name = "btnDebugUiBounds";
        _debugButton.CheckOnClick = true;
        _debugButton.CheckedChanged += (_, _) =>
        {
            _canvas.ShowDebugBounds = _debugButton.Checked;
            _canvas.Invalidate();
        };
        _stateSelector.Name = "cboUiButtonState";
        _stateSelector.DropDownStyle = ComboBoxStyle.DropDownList;
        _stateSelector.AutoSize = false;
        _stateSelector.Width = 110;
        _stateSelector.SelectedIndexChanged += (_, _) =>
        {
            _canvas.ButtonState = (PangyaUiButtonState)Math.Max(0, _stateSelector.SelectedIndex);
            _canvas.Invalidate();
        };
        _zoomOutButton.Text = "−";
        _zoomOutButton.Click += (_, _) => ChangeZoom(-0.1f);
        _zoomInButton.Text = "+";
        _zoomInButton.Click += (_, _) => ChangeZoom(0.1f);

        _toolbar.Items.AddRange([
            _fileSelector, new ToolStripSeparator(),
            _openButton, _saveButton, _reloadButton, _wallpaperButton, new ToolStripSeparator(),
            _stateLabel, _stateSelector, _debugButton, new ToolStripSeparator(),
            _zoomOutButton, _zoomLabel, _zoomInButton,
            _ifdefSeparator, _ifdefLabel
        ]);
    }

    private void ConfigureWorkspace()
    {
        var canvasHost = new Panel
        {
            Name = "pnlUiCanvasHost",
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(30, 32, 36)
        };
        canvasHost.Controls.Add(_canvas);
        canvasHost.ClientSizeChanged += (_, _) => _canvas.ViewportSize = canvasHost.ClientSize;
        _canvas.ViewportSize = canvasHost.ClientSize;

        var rightSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            Size = new Size(900, 700),
            SplitterDistance = 650,
            Panel2MinSize = 260
        };
        rightSplit.Panel1.Controls.Add(canvasHost);
        rightSplit.Panel2.Controls.Add(_properties);

        _elementTree.Name = "treeUiElements";
        _elementTree.Dock = DockStyle.Fill;
        _elementTree.HideSelection = false;

        var mainSplit = new SplitContainer
        {
            Name = "splitUiEditor",
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            Size = new Size(1200, 700),
            SplitterDistance = 280,
            Panel1MinSize = 220
        };
        mainSplit.Panel1.Controls.Add(_elementTree);
        mainSplit.Panel2.Controls.Add(rightSplit);
        mainSplit.Location = new Point(0, _toolbar.Height);

        Controls.Add(mainSplit);
        mainSplit.BringToFront();
    }

    private void PopulateFileSelector()
    {
        _changingFile = true;
        try
        {
            _fileSelector.Items.Clear();
            foreach (string path in _uiFiles)
                _fileSelector.Items.Add(new UiFileOption(
                    Path.GetRelativePath(Path.Combine(_dataRoot, "ui"), path), path));
            SelectCurrentFile();
        }
        finally { _changingFile = false; }
    }

    private async Task BrowseForXmlAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Title = Strings.UiEditor_OpenXml,
            Filter = Strings.UiEditor_XmlFilter,
            InitialDirectory = Path.Combine(_dataRoot, "ui"),
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK || !ConfirmDiscard()) return;
        await LoadDocumentAsync(dialog.FileName, CancellationToken.None);
    }

    private async Task LoadDocumentAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            UseWaitCursor = true;
            _toolbar.Enabled = false;
            PangyaUiDocument document = await Task.Run(() => PangyaUiDocument.Load(path), cancellationToken);
            _document = document;
            _currentPath = document.Path;
            _dirty = false;
            ConfigureIfdefControls(document);
            _canvas.LoadDocument(document);
            _canvas.EnabledSymbols = _enabledSymbols;
            BuildElementTree();
            SelectCurrentFile();
            UpdateTitle();
            _statusLabel.Text = string.Format(LocalizationManager.CurrentCulture,
                Strings.UiEditor_LoadedFormat, Path.GetFileName(path), document.Nodes.Count);
            AppLogger.Instance.Log(LogSource, $"Loaded PangYa UI XML '{path}' with {document.Nodes.Count} elements.");
            foreach (string warning in document.Warnings)
                AppLogger.Instance.Log(LogSource, warning, AppLogLevel.Warning);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException
                                   or InvalidDataException or ArgumentException)
        {
            AppLogger.Instance.Log(LogSource, $"Could not load PangYa UI XML: {ex.Message}", AppLogLevel.Error);
            MessageBox.Show(this, ex.Message, Strings.UiEditor_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
            _toolbar.Enabled = true;
            UpdateActionState();
        }
    }

    private async Task SaveAsync()
    {
        if (_document is null || !_dirty) return;
        try
        {
            UseWaitCursor = true;
            _saveButton.Enabled = false;
            await _document.SaveAtomicAsync();
            _dirty = false;
            UpdateTitle();
            UpdateActionState();
            _statusLabel.Text = Strings.UiEditor_Saved;
            AppLogger.Instance.Log(LogSource, $"Saved PangYa UI XML '{_document.Path}'.");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            AppLogger.Instance.Log(LogSource, $"Could not save PangYa UI XML: {ex.Message}", AppLogLevel.Error);
            MessageBox.Show(this, ex.Message, Strings.UiEditor_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
            UpdateActionState();
        }
    }

    private void BuildElementTree(PangyaUiNode? preferredSelection = null)
    {
        _elementTree.BeginUpdate();
        try
        {
            _elementTree.Nodes.Clear();
            if (_document is null) return;
            foreach (PangyaUiNode node in _document.GetVisibleRoots(_enabledSymbols))
                _elementTree.Nodes.Add(CreateTreeNode(node));
            _elementTree.ExpandAll();
            TreeNode? preferredTreeNode = preferredSelection is null
                ? null
                : FindTreeNode(_elementTree.Nodes, preferredSelection);
            if (preferredTreeNode is not null)
                _elementTree.SelectedNode = preferredTreeNode;
            else if (_elementTree.Nodes.Count > 0)
                _elementTree.SelectedNode = _elementTree.Nodes[0];
            else
                SelectNode(null, fromCanvas: false);
        }
        finally { _elementTree.EndUpdate(); }
    }

    private TreeNode CreateTreeNode(PangyaUiNode node)
    {
        var treeNode = new TreeNode(node.DisplayName) { Tag = node };
        foreach (PangyaUiNode child in node.Children.Where(child => child.IsVisible(_enabledSymbols)))
            treeNode.Nodes.Add(CreateTreeNode(child));
        return treeNode;
    }

    private void ConfigureIfdefControls(PangyaUiDocument document)
    {
        foreach (ToolStripControlHost host in _ifdefControls)
        {
            _toolbar.Items.Remove(host);
            host.Dispose();
        }
        _ifdefControls.Clear();
        _enabledSymbols.Clear();

        foreach (string symbol in document.ConditionalSymbols.OrderBy(value => value,
                     StringComparer.OrdinalIgnoreCase))
        {
            var checkBox = new CheckBox
            {
                Name = "chkUiIfdef_" + symbol,
                Text = symbol,
                AutoSize = true,
                Margin = Padding.Empty,
                Checked = false
            };
            checkBox.CheckedChanged += (_, _) =>
            {
                if (checkBox.Checked) _enabledSymbols.Add(symbol);
                else _enabledSymbols.Remove(symbol);
                RefreshConditionalView();
            };
            var host = new ToolStripControlHost(checkBox)
            {
                Name = "hostUiIfdef_" + symbol,
                AutoSize = true
            };
            _ifdefControls.Add(host);
            _toolbar.Items.Add(host);
        }

        bool hasConditions = _ifdefControls.Count > 0;
        _ifdefSeparator.Visible = hasConditions;
        _ifdefLabel.Visible = hasConditions;
    }

    private void RefreshConditionalView()
    {
        if (_document is null) return;
        PangyaUiNode? previous = _canvas.SelectedNode;
        PangyaUiNode? target = previous is not null && _document.IsVisible(previous, _enabledSymbols)
            ? previous
            : previous?.FindContainingForm();
        if (target is not null && !_document.IsVisible(target, _enabledSymbols)) target = null;

        _canvas.EnabledSymbols = _enabledSymbols;
        BuildElementTree(target);
        _canvas.Invalidate();
    }

    private void SelectNode(PangyaUiNode? node, bool fromCanvas)
    {
        _canvas.SelectedForm = node?.FindContainingForm();
        _canvas.SelectedNode = node;
        _properties.SelectedNode = node;
        if (fromCanvas && node is not null)
        {
            TreeNode? treeNode = FindTreeNode(_elementTree.Nodes, node);
            if (treeNode is not null) _elementTree.SelectedNode = treeNode;
        }
    }

    private static TreeNode? FindTreeNode(TreeNodeCollection nodes, PangyaUiNode target)
    {
        foreach (TreeNode node in nodes)
        {
            if (ReferenceEquals(node.Tag, target)) return node;
            TreeNode? nested = FindTreeNode(node.Nodes, target);
            if (nested is not null) return nested;
        }
        return null;
    }

    private void RefreshSelectedTreeNode()
    {
        if (_elementTree.SelectedNode?.Tag is PangyaUiNode node)
            _elementTree.SelectedNode.Text = node.DisplayName;
    }

    private void MarkDirty()
    {
        if (_document is null) return;
        _dirty = true;
        UpdateTitle();
        UpdateActionState();
    }

    private bool ConfirmDiscard() => !_dirty ||
        MessageBox.Show(this, Strings.IFFManager_DiscardChanges, Strings.UiEditor_Title,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    private void FrmPangyaUiEditor_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!ConfirmDiscard()) e.Cancel = true;
    }

    private void SelectCurrentFile()
    {
        _changingFile = true;
        try
        {
            _fileSelector.SelectedItem = _fileSelector.Items.Cast<UiFileOption>()
                .FirstOrDefault(option => option.Path.Equals(_currentPath, StringComparison.OrdinalIgnoreCase));
        }
        finally { _changingFile = false; }
    }

    private void ChangeZoom(float delta)
    {
        _canvas.Zoom = Math.Clamp(_canvas.Zoom + delta, 0.25f, 3f);
        _zoomLabel.Text = $"{_canvas.Zoom:P0}";
    }

    private void LocalizationManager_CultureChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        ApplyLocalization();
    }

    private async Task SelectStartupWallpaperAsync()
    {
        if (!string.IsNullOrEmpty(_canvas.StartupWallpaper))
        {
            _canvas.StartupWallpaper = null;
            _canvas.SetStartupWallpaperImage(null);
            _wallpaperButton.Checked = false;
            _canvas.Invalidate();
            return;
        }
        using OpenFileDialog dialog = FileDialogFactory.CreateUiResourceOpenDialog(_dataRoot);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        string relative = Path.GetRelativePath(_dataRoot, dialog.FileName);
        if (Path.IsPathRooted(relative) || relative == ".." ||
            relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            MessageBox.Show(this, Strings.UiEditor_ResourceMustBeInData, Strings.UiEditor_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string filename = dialog.FileName;
        _wallpaperButton.Enabled = false;
        try
        {
            Image? image = await Task.Run(() => PangyaImageLoader.Load(filename));
            if (IsDisposed || Disposing)
            {
                image?.Dispose();
                return;
            }
            if (image is null)
            {
                MessageBox.Show(this, Strings.UiEditor_LoadFailed, Strings.UiEditor_Title,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _canvas.StartupWallpaper = filename;
            _canvas.SetStartupWallpaperImage(image);
            _wallpaperButton.Checked = true;
        }
        finally
        {
            if (!IsDisposed && !Disposing) _wallpaperButton.Enabled = true;
        }
    }

    private void ApplyLocalization()
    {
        _openButton.Text = Strings.UiEditor_OpenXml;
        _wallpaperButton.Text = Strings.UiEditor_StartupWallpaper;
        _saveButton.Text = Strings.IFFManager_Save;
        _reloadButton.Text = Strings.UiEditor_Reload;
        _debugButton.Text = Strings.UiEditor_DebugBounds;
        _ifdefLabel.Text = Strings.UiEditor_Defines;
        _stateLabel.Text = Strings.UiEditor_ButtonState;
        int state = Math.Max(0, _stateSelector.SelectedIndex);
        _stateSelector.Items.Clear();
        _stateSelector.Items.AddRange([
            Strings.UiEditor_StateNormal,
            Strings.UiEditor_StateHover,
            Strings.UiEditor_StateSelected
        ]);
        _stateSelector.SelectedIndex = Math.Min(state, _stateSelector.Items.Count - 1);
        _zoomLabel.Text = $"{_canvas.Zoom:P0}";
        _properties.ApplyLocalization();
        if (_document is null) _statusLabel.Text = Strings.UiEditor_Ready;
        UpdateTitle();
    }

    private void UpdateTitle() =>
        Text = Strings.UiEditor_Title + (_currentPath is null ? string.Empty : $" — {Path.GetFileName(_currentPath)}") +
               (_dirty ? " *" : string.Empty);

    private void UpdateActionState()
    {
        _saveButton.Enabled = _document is not null && _dirty && !UseWaitCursor;
        _reloadButton.Enabled = _document is not null && !UseWaitCursor;
    }

    private sealed record UiFileOption(string Label, string Path)
    {
        public override string ToString() => Label;
    }
}

internal sealed class PangyaUiCanvas : Control
{
    private bool _viewFocused = true;

    internal void SetViewFocused(bool focused)
    {
        if (_viewFocused == focused) return;
        _viewFocused = focused;
        Invalidate();
    }
    private const int FreshFormTitleHeight = 20;
    [System.ComponentModel.DesignerSerializationVisibility(
        System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    internal string? StartupWallpaper { get; set; }
    private Image? _startupWallpaperImage;

    internal void SetStartupWallpaperImage(Image? image)
    {
        _startupWallpaperImage?.Dispose();
        _startupWallpaperImage = image;
        Invalidate();
    }
    private readonly Dictionary<PangyaUiNode, int> _viewerTopRows = [];
    private readonly Dictionary<PangyaUiNode, float> _viewerScrollRows = [];
    private readonly Dictionary<PangyaUiNode, int> _extendedGaugeSteps = [];
    private readonly Dictionary<PangyaUiNode, PangyaUiComboPreview> _combos = [];
    private readonly Dictionary<PangyaUiNode, IReadOnlyList<string>> _editLines = [];
    private readonly Dictionary<PangyaUiNode, int> _editTopRows = [];
    private readonly System.Windows.Forms.Timer _editCaretTimer = new() { Interval = 50 };
    private PangyaUiNode? _focusedEdit;
    private long _editCaretStart;
    private int _editCaretIndex;
    private PangyaUiNode? _openCombo;

    internal void SetComboPreview(PangyaUiNode node, PangyaUiComboPreview preview)
    {
        if (preview.IsOpen && _openCombo is not null && !ReferenceEquals(_openCombo, node))
            _combos[_openCombo] = _combos[_openCombo] with { IsOpen = false, HoveredIndex = -1 };
        _combos[node] = preview;
        if (preview.IsOpen) _openCombo = node;
        else if (ReferenceEquals(_openCombo, node)) _openCombo = null;
        Invalidate();
    }
    private PangyaUiNode? _draggedViewer;
    private int _viewerThumbGrabOffset;
    private readonly Dictionary<PangyaUiNode, PangyaUiMenuState> _menuStates = [];
    private readonly System.Windows.Forms.Timer _menuPulseTimer = new() { Interval = 33 };
    private readonly System.Windows.Forms.Timer _layoutAnimationTimer = new() { Interval = 33 };
    private readonly System.Windows.Forms.Timer _captionTimer = new() { Interval = 33 };
    private readonly System.Windows.Forms.Timer _tooltipTimer = new() { Interval = 500 };
    private bool _showTooltip;
    private PangyaUiNode? _fadingTooltip;
    private Point? _fadingTooltipPointer;
    private long _tooltipFadeStart;
    private readonly System.Windows.Forms.Timer _buttonTimer = new() { Interval = 33 };
    private readonly System.Windows.Forms.Timer _frameBlinkTimer = new() { Interval = 800 };
    private readonly Dictionary<PangyaUiNode, TimeSpan> _frameBlinkElapsed = [];
    private readonly Dictionary<PangyaUiNode, long> _buttonPressTicks = [];
    private PangyaUiNode? _pressedButton;
    private bool _pressedButtonInside;
    private readonly HashSet<PangyaUiNode> _latchedButtons = [];
    private readonly Dictionary<PangyaUiNode, float> _captionOffsets = [];
    private PangyaUiNode? _hoveredCaption;
    private float _captionLimit;
    private long _captionTick;
    private long _menuPulseStart;
    private long _layoutAnimationStart;
    private PangyaUiNode? _hoveredTextControl;
    private PangyaUiCursorPreview? _cursorPreview;
    private Point? _textControlPointer;
    internal const int FormPadding = 16;
    private readonly PangyaUiRenderer _renderer;
    private HashSet<string> _enabledSymbols = new(StringComparer.OrdinalIgnoreCase);
    private PangyaUiDocument? _document;
    private PangyaUiNode? _selectedForm;
    private PangyaUiNode? _selectedNode;
    private PangyaUiNode? _draggedNode;
    private Point? _dragStart;
    private Rectangle _dragStartBounds;
    private Size _viewportSize;
    private float _zoom = 1f;

    public PangyaUiCanvas(IPangyaImageProvider assets,
        PangyaUiResourceCatalog? resourceCatalog = null)
    {
        _renderer = new PangyaUiRenderer(assets, resourceCatalog);
        DoubleBuffered = true;
        BackColor = Color.FromArgb(36, 39, 44);
        Cursor = Cursors.Default;
        MouseDown += CanvasMouseDown;
        MouseMove += CanvasMouseMove;
        Disposed += (_, _) => _startupWallpaperImage?.Dispose();
        _editCaretTimer.Tick += (_, _) => Invalidate();
        Disposed += (_, _) => _editCaretTimer.Dispose();
        LostFocus += (_, _) => FocusEdit(null);
        MouseLeave += (_, _) => UpdateTextControlHover(null, null);
        MouseLeave += (_, _) => UpdateCaptionHover(null);
        MouseLeave += (_, _) => UpdateCursorPreview(null);
        Disposed += (_, _) => UpdateCursorPreview(null);
        _tooltipTimer.Tick += (_, _) =>
        {
            if (_fadingTooltip is null)
            {
                _tooltipTimer.Stop();
                _showTooltip = true;
            }
            else if (GetTooltipAlpha() == 0)
            {
                _tooltipTimer.Stop();
                _fadingTooltip = null;
                _fadingTooltipPointer = null;
            }
            Invalidate();
        };
        Disposed += (_, _) => _tooltipTimer.Dispose();
        _captionTimer.Tick += (_, _) => AdvanceCaptions();
        Disposed += (_, _) => _captionTimer.Dispose();
        _buttonTimer.Tick += (_, _) => ExpireButtonPresses();
        Disposed += (_, _) => _buttonTimer.Dispose();
        _frameBlinkTimer.Tick += (_, _) =>
        {
            if (_selectedNode is not PangyaUiNode frame || !_frameBlinkElapsed.TryGetValue(frame, out TimeSpan elapsed))
                return;
            _frameBlinkElapsed[frame] = elapsed == TimeSpan.Zero ? TimeSpan.FromMilliseconds(800) : TimeSpan.Zero;
            Invalidate();
        };
        Disposed += (_, _) => _frameBlinkTimer.Dispose();
        _menuPulseTimer.Tick += (_, _) => UpdateMenuPulse();
        Disposed += (_, _) => _menuPulseTimer.Dispose();
        _layoutAnimationTimer.Tick += (_, _) => Invalidate();
        Disposed += (_, _) => _layoutAnimationTimer.Dispose();
        MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Left && _pressedButton is PangyaUiNode pressed &&
                _combos.TryGetValue(pressed, out PangyaUiComboPreview? combo) &&
                _renderer.HitTestComboUnfold(pressed, LogicalPoint(e.Location), _enabledSymbols))
                SetComboPreview(pressed, combo with { IsOpen = !combo.IsOpen, HoveredIndex = -1 });
            ClearPressedButton();
            EndViewerDrag();
            _draggedNode = null;
            _dragStart = null;
            Capture = false;
        };
        MouseCaptureChanged += (_, _) =>
        {
            if (Capture) return;
            ClearPressedButton();
            EndViewerDrag();
        };
    }

    internal void ResetViewerScroll(PangyaUiNode? node)
    {
        if (node is null) return;
        if (ReferenceEquals(_draggedViewer, node))
        {
            EndViewerDrag();
            Capture = false;
        }
        _viewerTopRows.Remove(node);
        _viewerScrollRows.Remove(node);
    }

    private static bool ViewerAcceptsMouse(PangyaUiNode node)
    {
        if (!node.Parameters.TryGetValue("mouse_event", out string? setting)) return true;
        ReadOnlySpan<char> value = setting.AsSpan().TrimStart();
        if (!value.IsEmpty && value[0] is ('+' or '-')) value = value[1..];
        foreach (char digit in value)
        {
            if (!char.IsAsciiDigit(digit)) break;
            if (digit != '0') return true;
        }
        return false;
    }

    private void EndViewerDrag()
    {
        if (_draggedViewer is PangyaUiNode viewer)
        {
            _viewerScrollRows.Remove(viewer);
            Invalidate();
        }
        _draggedViewer = null;
    }

    public event EventHandler<PangyaUiNode?>? SelectionChanged;
    public event EventHandler<PangyaUiNodeEventArgs>? ElementChanged;
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowDebugBounds { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PangyaUiButtonState ButtonState { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = value;
            UpdateCanvasSize();
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PangyaUiNode? SelectedForm
    {
        get => _selectedForm;
        set
        {
            if (!ReferenceEquals(_selectedForm, value))
            {
                _layoutAnimationStart = Environment.TickCount64;
                _layoutAnimationTimer.Enabled = value is not null &&
                    _renderer.HasAnimatedControls(value, _enabledSymbols);
            }
            _selectedForm = value;
            if (_selectedNode is not null && value is not null && !_selectedNode.IsWithin(value))
                SelectedNode = null;
            UpdateCanvasSize();
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PangyaUiNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (!ReferenceEquals(_selectedNode, value))
            {
                _frameBlinkTimer.Stop();
                _frameBlinkElapsed.Clear();
                if (value is not null && _renderer.CanPreviewFrameBlink(value, _enabledSymbols))
                {
                    _frameBlinkElapsed[value] = TimeSpan.Zero;
                    _frameBlinkTimer.Start();
                }
            }
            _selectedNode = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal IReadOnlySet<string> EnabledSymbols
    {
        get => _enabledSymbols;
        set
        {
            _enabledSymbols = new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
            if (_selectedNode is not null && _document is not null &&
                !_document.IsVisible(_selectedNode, _enabledSymbols))
                SelectedNode = null;
            bool animate = _selectedForm is not null &&
                _renderer.HasAnimatedControls(_selectedForm, _enabledSymbols);
            if (animate && !_layoutAnimationTimer.Enabled)
                _layoutAnimationStart = Environment.TickCount64;
            _layoutAnimationTimer.Enabled = animate;
            Invalidate();
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Size ViewportSize
    {
        get => _viewportSize;
        set
        {
            _viewportSize = new Size(Math.Max(0, value.Width), Math.Max(0, value.Height));
            UpdateCanvasSize();
        }
    }

    public void LoadDocument(PangyaUiDocument document)
    {
        _document = document;
        _viewerTopRows.Clear();
        _viewerScrollRows.Clear();
        _extendedGaugeSteps.Clear();
        _combos.Clear();
        _editLines.Clear();
        _editTopRows.Clear();
        FocusEdit(null);
        _openCombo = null;
        _draggedViewer = null;
        _menuStates.Clear();
        _menuPulseTimer.Stop();
        _layoutAnimationTimer.Stop();
        _captionTimer.Stop();
        _buttonTimer.Stop();
        _frameBlinkTimer.Stop();
        _frameBlinkElapsed.Clear();
        _buttonPressTicks.Clear();
        _latchedButtons.Clear();
        ClearPressedButton();
        _captionOffsets.Clear();
        _hoveredCaption = null;
        UpdateTextControlHover(null, null);
        _tooltipTimer.Stop();
        _fadingTooltip = null;
        _fadingTooltipPointer = null;
        _selectedForm = null;
        _selectedNode = null;
        UpdateCanvasSize();
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Color.FromArgb(24, 26, 30));
        if (_document is null) return;
        using var transform = new System.Drawing.Drawing2D.Matrix(
            Zoom, 0f, 0f, Zoom, FormPadding * Zoom, FormPadding * Zoom);
        e.Graphics.Transform = transform;
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        _renderer.Render(e.Graphics, _document, SelectedForm, RenderOptions() with { CursorPreview = _cursorPreview });
    }

    private void UpdateCursorPreview(PangyaUiCursorPreview? cursor)
    {
        if (_cursorPreview == cursor) return;
        if (_cursorPreview is null) Cursor.Hide();
        else if (cursor is null) Cursor.Show();
        _cursorPreview = cursor;
        if (!IsDisposed) Invalidate();
    }

    private void FocusEdit(PangyaUiNode? node)
    {
        _focusedEdit = node;
        _editCaretIndex = node is null ? 0 : GetEditText(node).Length;
        _editCaretStart = Environment.TickCount64;
        _editCaretTimer.Enabled = node is not null;
        Invalidate();
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (_focusedEdit is not PangyaUiNode node || e.Handled) return;
        string value = GetEditText(node);
        if (e.KeyChar == '\b')
        {
            if (_editCaretIndex > 0)
            {
                int previous = PreviousEditIndex(value, _editCaretIndex);
                value = value.Remove(previous, _editCaretIndex - previous);
                _editCaretIndex = previous;
            }
        }
        else
        {
            bool newline = e.KeyChar == '\r';
            if (newline && node.Parameters.GetValueOrDefault("multiline") == "0") return;
            if (!newline && char.IsControl(e.KeyChar)) return;
            if (node.Parameters.GetValueOrDefault("numberonly") == "true" &&
                e.KeyChar is not (>= '0' and <= '9')) return;
            int limit = int.TryParse(node.Parameters.GetValueOrDefault("charlimit"), out int parsed)
                ? Math.Clamp(parsed, 0, 16384) : 255;
            if (value.Length >= limit) return;
            if (!newline)
            {
                int start = _editCaretIndex == 0 ? 0 : value.LastIndexOf('\n', _editCaretIndex - 1) + 1;
                int end = value.IndexOf('\n', _editCaretIndex);
                string line = value[start..(end < 0 ? value.Length : end)];
                float widthLimit = float.TryParse(node.Parameters.GetValueOrDefault("widthlimit"),
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                    out float width) && float.IsFinite(width) ? width : 1000;
                using Graphics graphics = CreateGraphics();
                using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
                format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                if (graphics.MeasureString(line, SystemFonts.DefaultFont, PointF.Empty, format).Width > widthLimit)
                    return;
            }
            value = value.Insert(_editCaretIndex++, (newline ? '\n' : e.KeyChar).ToString());
        }
        _editLines[node] = value.Split('\n');
        _editTopRows.Remove(node);
        _editCaretStart = Environment.TickCount64;
        e.Handled = true;
        Invalidate();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (_focusedEdit is PangyaUiNode edit &&
            keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or Keys.Delete)
        {
            if (keyData != Keys.Delete &&
                !string.Equals(edit.Parameters.GetValueOrDefault("caretmove", "true"), "true",
                    StringComparison.OrdinalIgnoreCase))
                return true;
            string text = GetEditText(edit);
            int end = text.IndexOf('\n', _editCaretIndex);
            switch (keyData)
            {
                case Keys.Up:
                case Keys.Down:
                    _editCaretIndex = MoveEditCaretVertically(text, _editCaretIndex, keyData == Keys.Up);
                    break;
                case Keys.Left:
                    _editCaretIndex = PreviousEditIndex(text, _editCaretIndex);
                    break;
                case Keys.Right:
                    _editCaretIndex = NextEditIndex(text, _editCaretIndex);
                    break;
                case Keys.Home:
                    _editCaretIndex = _editCaretIndex == 0 ? 0 : text.LastIndexOf('\n', _editCaretIndex - 1) + 1;
                    break;
                case Keys.End:
                    _editCaretIndex = end < 0 ? text.Length : end;
                    break;
                case Keys.Delete:
                    int next = NextEditIndex(text, _editCaretIndex);
                    _editLines[edit] = text.Remove(_editCaretIndex, next - _editCaretIndex).Split('\n');
                    break;
            }
            _editCaretStart = Environment.TickCount64;
            _editTopRows.Remove(edit);
            Invalidate();
            return true;
        }
        PangyaUiNode? node = _openCombo ?? SelectedNode;
        if (node is not null && _combos.TryGetValue(node, out PangyaUiComboPreview? combo) &&
            combo.Items.Count > 0)
        {
            if (combo.IsOpen && keyData is Keys.Escape or Keys.Enter)
            {
                SetComboPreview(node, combo with { IsOpen = false, HoveredIndex = -1 });
                return true;
            }
            if (keyData is Keys.Up or Keys.Down)
            {
                int count = combo.Items.Count;
                int current = combo.SelectedIndex;
                int next = keyData == Keys.Up
                    ? (current <= 0 || current >= count ? count - 1 : current - 1)
                    : (current < 0 || current >= count - 1 ? 0 : current + 1);
                int maximum = PangyaUiRenderer.GetComboMaximumTopRow(node, combo);
                int capacity = count - maximum;
                int topRow = Math.Clamp(combo.TopRow, 0, maximum);
                if (next < topRow) topRow = next;
                else if (next - topRow >= capacity) topRow = next - capacity + 1;
                SetComboPreview(node, combo with { SelectedIndex = next,
                    HoveredIndex = combo.IsOpen ? next : -1, TopRow = topRow });
                return true;
            }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if (e.Delta != 0 && _openCombo is PangyaUiNode open &&
            _combos.TryGetValue(open, out PangyaUiComboPreview? combo))
        {
            Point pointer = LogicalPoint(e.Location);
            if (_renderer.HitTestComboRow(open, pointer, RenderOptions()) >= 0)
            {
                int steps = Math.Max(1, Math.Abs(e.Delta / SystemInformation.MouseWheelScrollDelta));
                int maximum = PangyaUiRenderer.GetComboMaximumTopRow(open, combo);
                int next = (int)Math.Clamp((long)Math.Clamp(combo.TopRow, 0, maximum) -
                    Math.Sign(e.Delta) * (long)steps, 0, maximum);
                SetComboPreview(open, combo with { TopRow = next });
                int hovered = _renderer.HitTestComboRow(open, pointer, RenderOptions());
                SetComboPreview(open, _combos[open] with { HoveredIndex = hovered });
                if (e is HandledMouseEventArgs handled) handled.Handled = true;
                return;
            }
        }
        if (_document is not null && e.Delta != 0)
        {
            PangyaUiNode? viewer = _renderer.HitTest(_document, SelectedForm,
                LogicalPoint(e.Location), RenderOptions());
            if (viewer is not null && _editLines.TryGetValue(viewer, out IReadOnlyList<string>? lines))
            {
                int maxEditRow = PangyaUiRenderer.GetEditMaximumTopRow(viewer, lines.Count);
                if (maxEditRow > 0)
                {
                    int initial = GetEditCarets()?.GetValueOrDefault(viewer) is { } caret
                        ? Math.Max(0, caret.Line - (lines.Count - maxEditRow) + 1) : 0;
                    int current = _editTopRows.GetValueOrDefault(viewer, initial);
                    int steps = Math.Max(1, Math.Abs(e.Delta / SystemInformation.MouseWheelScrollDelta));
                    _editTopRows[viewer] = Math.Clamp(current - Math.Sign(e.Delta) * steps, 0, maxEditRow);
                    if (e is HandledMouseEventArgs handledEdit) handledEdit.Handled = true;
                    Invalidate();
                    return;
                }
            }
            if (viewer is not null && ViewerAcceptsMouse(viewer))
            {
                int maximum = _renderer.GetViewerMaximumTopRow(viewer, _enabledSymbols);
                if (maximum > 0)
                {
                    int row = _viewerTopRows.GetValueOrDefault(viewer);
                    int steps = e.Delta / 119;
                    _viewerScrollRows.Remove(viewer);
                    _viewerTopRows[viewer] = Math.Clamp(row - steps, 0, maximum);
                    if (e is HandledMouseEventArgs handled) handled.Handled = true;
                    Invalidate();
                    return;
                }
            }
        }
        base.OnMouseWheel(e);
    }

    internal Rectangle GetRenderedBounds(PangyaUiNode node)
        => _renderer.GetRenderedBounds(node, RenderOptions());

    internal void CanvasMouseDown(object? sender, MouseEventArgs e)
    {
        if (_document is null || e.Button != MouseButtons.Left) return;
        Point logical = LogicalPoint(e.Location);
        if (_openCombo is PangyaUiNode open && _combos.TryGetValue(open, out PangyaUiComboPreview? combo))
        {
            int item = _renderer.HitTestComboRow(open, logical, RenderOptions());
            bool unfold = _renderer.HitTestComboUnfold(open, logical, _enabledSymbols);
            if (unfold)
            {
                BeginComboPress(open);
                return;
            }
            SetComboPreview(open, combo with
            {
                IsOpen = false, HoveredIndex = -1,
                SelectedIndex = item >= 0 ? item : combo.SelectedIndex
            });
            if (item >= 0) return;
        }
        if (SelectedForm is not null)
        {
            foreach (PangyaUiNode viewer in RenderOrderedNodes().Reverse())
            {
                if (!viewer.IsRenderVisible ||
                    !ViewerAcceptsMouse(viewer))
                    continue;
                Rectangle thumb = _renderer.GetViewerScrollThumb(viewer, RenderOptions());
                if (thumb.IsEmpty) continue;
                Rectangle viewport = GetRenderedBounds(viewer);
                var track = new Rectangle(thumb.X - 3, viewport.Y, thumb.Width, viewport.Height);
                if (!track.Contains(logical)) continue;
                if (logical.Y < thumb.Top || logical.Y > thumb.Bottom)
                {
                    // Fresh moves by half the visible row capacity for a track click.
                    int step = viewport.Height / 20 / 2;
                    int maximum = _renderer.GetViewerMaximumTopRow(viewer, _enabledSymbols);
                    int topRow = _viewerTopRows.GetValueOrDefault(viewer);
                    _viewerScrollRows.Remove(viewer);
                    _viewerTopRows[viewer] = Math.Clamp(topRow + (logical.Y < thumb.Y ? -step : step), 0, maximum);
                    Invalidate();
                    return;
                }
                _draggedViewer = viewer;
                _viewerThumbGrabOffset = logical.Y - thumb.Y;
                Capture = true;
                Invalidate();
                return;
            }
        }
        PangyaUiNode? hit = _renderer.HitTest(_document, SelectedForm, logical,
            RenderOptions());
        FocusEdit(null);
        _draggedNode = null;
        _dragStart = null;
        Capture = false;
        if (hit is null)
            return;
        if (_renderer.GetViewerMaximumTopRow(hit, _enabledSymbols) > 0)
            Focus();
        SelectedNode = hit;
        SelectionChanged?.Invoke(this, hit);
        if (hit.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase) &&
            hit.Parameters.GetValueOrDefault("readonly") != "true" &&
            hit.Parameters.GetValueOrDefault("focusoff") != "true")
        {
            Focus();
            FocusEdit(hit);
            return;
        }
        if (_combos.TryGetValue(hit, out PangyaUiComboPreview? closed) && closed.Items.Count > 0 &&
            _renderer.HitTestComboUnfold(hit, logical, _enabledSymbols))
        {
            BeginComboPress(hit);
            return;
        }
        int pushStyle = int.TryParse(hit.Parameters.GetValueOrDefault("pushstyle"), out int configuredPushStyle)
            ? configuredPushStyle : 1;
        if (PangyaUiRenderer.HasButtonSkin(hit) && !_buttonPressTicks.ContainsKey(hit))
        {
            if (pushStyle == 2)
            {
                if (!_latchedButtons.Add(hit)) _latchedButtons.Remove(hit);
            }
            else if (pushStyle == 3)
                _latchedButtons.Add(hit);
            else
            {
                _pressedButton = hit;
                _pressedButtonInside = true;
                Capture = true;
            }
            Invalidate();
        }
        int gaugeDirection = _renderer.HitTestExtendedGaugeButton(hit, logical, _enabledSymbols);
        if (gaugeDirection != 0)
        {
            int steps = PangyaUiRenderer.GetExtendedGaugeStepCount(hit);
            _extendedGaugeSteps[hit] = (int)Math.Clamp(
                (long)_extendedGaugeSteps.GetValueOrDefault(hit) + gaugeDirection, 0, steps);
            Invalidate();
            return;
        }
        if (pushStyle == 1 && PangyaUiRenderer.GetButtonPushDelay(hit) > 0.1 &&
            !_buttonPressTicks.ContainsKey(hit))
        {
            // Fresh seeds the elapsed counter at 0.1 seconds on push-button down.
            _buttonPressTicks[hit] = Environment.TickCount64 - 100;
            _buttonTimer.Start();
            Invalidate();
        }
        int row = hit.Type.ToUpperInvariant() switch
        {
            "CONTEXTMENU" => _renderer.HitTestMenuRow(hit, logical, RenderOptions()),
            "TEXTBUTTON" => _renderer.HitTestTextButtonSegment(hit, logical, RenderOptions()),
            _ => -1
        };
        if (row >= 0)
        {
            _menuStates[hit] = new PangyaUiMenuState(row, SelectionAlpha: 0);
            if (!_menuPulseTimer.Enabled)
            {
                _menuPulseStart = Environment.TickCount64;
                _menuPulseTimer.Start();
            }
            Invalidate();
            return;
        }
        if (hit.IsEditable && CanStartDrag(hit, logical))
        {
            _draggedNode = hit;
            _dragStart = logical;
            _dragStartBounds = hit.Bounds;
            Capture = true;
        }
    }

    private bool CanStartDrag(PangyaUiNode node, Point logicalPoint)
    {
        if (node.Type.Equals("MACROITEM", StringComparison.OrdinalIgnoreCase))
            return false;
        if (!node.IsForm || node.Type.Equals("LAYOUT", StringComparison.OrdinalIgnoreCase))
            return true;

        // FrForm starts with dragging enabled; FrForm::Init can disable it.
        if (node.Parameters.TryGetValue("drag", out string? value) &&
            int.TryParse(value, out int drag) && drag == 0)
            return false;

        Rectangle bounds = GetRenderedBounds(node);
        bounds.Height = Math.Min(bounds.Height, FreshFormTitleHeight);
        return bounds.Contains(logicalPoint);
    }

    internal void CanvasMouseMove(object? sender, MouseEventArgs e)
    {
        Point logical = LogicalPoint(e.Location);
        if (_openCombo is PangyaUiNode open && _combos.TryGetValue(open, out PangyaUiComboPreview? combo))
        {
            int item = _renderer.HitTestComboRow(open, logical, RenderOptions());
            if (item != combo.HoveredIndex)
                SetComboPreview(open, combo with { HoveredIndex = item });
        }
        if (_pressedButton is PangyaUiNode pressed)
        {
            bool inside = _combos.ContainsKey(pressed)
                ? _renderer.HitTestComboUnfold(pressed, logical, _enabledSymbols)
                : GetRenderedBounds(pressed).Contains(logical);
            if (_pressedButtonInside != inside)
            {
                _pressedButtonInside = inside;
                Invalidate();
            }
        }
        if (_draggedViewer is PangyaUiNode viewer && Capture && e.Button == MouseButtons.Left)
        {
            Rectangle thumb = _renderer.GetViewerScrollThumb(viewer, RenderOptions());
            Rectangle viewport = GetRenderedBounds(viewer);
            int travel = viewport.Height - thumb.Height;
            int maximum = _renderer.GetViewerMaximumTopRow(viewer, _enabledSymbols);
            if (!thumb.IsEmpty && travel > 0 && maximum > 0)
            {
                long distance = Math.Clamp((long)logical.Y - _viewerThumbGrabOffset - viewport.Y, 0, travel);
                float row = distance * (float)maximum / travel;
                _viewerScrollRows[viewer] = row;
                int wholeRow = (int)row;
                _viewerTopRows[viewer] = row - wholeRow > 0.5f ? wholeRow + 1 : wholeRow;
                Invalidate();
            }
            return;
        }
        if (_dragStart is not Point start || _draggedNode is null || e.Button != MouseButtons.Left)
        {
            PangyaUiNode? hit = _document is null ? null :
                _renderer.HitTest(_document, SelectedForm, logical, RenderOptions());
            UpdateCaptionHover(hit is not null && PangyaUiRenderer.IsScrollingCaption(hit) ? hit : null);
            UpdateTextControlHover(hit is not null &&
                (hit.Type.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("TEXTBUTTON", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("LISTBOX", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("GAUGEBAREX", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("COMBOBOX", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("COMBOCTLEX", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase) ||
                 (hit.Type.Equals("VIEWER", StringComparison.OrdinalIgnoreCase) &&
                  hit.Parameters.GetValueOrDefault("use_wheel") == "true" &&
                  ViewerAcceptsMouse(hit))) ? hit : null, logical);
            Cursor = hit is null ? Cursors.Default : hit.Type.ToUpperInvariant() switch
            {
                "FORM" when hit.IsEditable && CanStartDrag(hit, logical) => Cursors.SizeAll,
                "BUTTON" or "TEXTBUTTON" or "TABBUTTON" => Cursors.Hand,
                "VIEWER" when ViewerAcceptsMouse(hit) => Cursors.Hand,
                "EDIT" => Cursors.IBeam,
                _ => Cursors.Default
            };
            int cursorIndex = Cursor == Cursors.Hand ? 1 : Cursor == Cursors.IBeam ? 12 : 0;
            UpdateCursorPreview(_document is not null && Cursor != Cursors.SizeAll &&
                _renderer.CanDrawCursor(cursorIndex, _enabledSymbols) ? new(logical, cursorIndex) : null);
            return;
        }
        int dx = logical.X - start.X;
        int dy = logical.Y - start.Y;
        _draggedNode.SetBounds(new Rectangle(
            _dragStartBounds.X + dx, _dragStartBounds.Y + dy,
            _dragStartBounds.Width, _dragStartBounds.Height));
        ElementChanged?.Invoke(this, new PangyaUiNodeEventArgs(_draggedNode));
        Invalidate();
    }

    private void UpdateCaptionHover(PangyaUiNode? caption)
    {
        if (ReferenceEquals(_hoveredCaption, caption)) return;
        _hoveredCaption = caption;
        _captionLimit = caption is null ? 0 : _renderer.GetCaptionScrollLimit(caption, _enabledSymbols);
        if (_captionTimer.Enabled || (_captionLimit <= 0 && _captionOffsets.Count == 0)) return;
        _captionTick = Environment.TickCount64;
        _captionTimer.Start();
    }

    private void AdvanceCaptions()
    {
        long now = Environment.TickCount64;
        float distance = (now - _captionTick) * 0.1f;
        _captionTick = now;
        bool changed = false;
        foreach (PangyaUiNode caption in _captionOffsets.Keys.ToArray())
        {
            if (ReferenceEquals(caption, _hoveredCaption)) continue;
            float offset = _captionOffsets[caption] - distance;
            if (offset <= 0) _captionOffsets.Remove(caption);
            else _captionOffsets[caption] = offset;
            changed = true;
        }
        if (_hoveredCaption is PangyaUiNode hovered)
        {
            float current = _captionOffsets.GetValueOrDefault(hovered);
            float next = Math.Min(_captionLimit, current + distance);
            if (next != current)
            {
                _captionOffsets[hovered] = next;
                changed = true;
            }
        }
        if (changed) Invalidate();
        else _captionTimer.Stop();
    }

    private void UpdateTextControlHover(PangyaUiNode? menu, Point? pointer)
    {
        if (menu is null) pointer = null;
        if (ReferenceEquals(menu, _hoveredTextControl) && pointer == _textControlPointer) return;
        if (!ReferenceEquals(menu, _hoveredTextControl))
        {
            _tooltipTimer.Stop();
            if (menu?.Parameters.GetValueOrDefault("tooltip") is { Length: > 0 })
            {
                _fadingTooltip = null;
                _fadingTooltipPointer = null;
                _tooltipTimer.Interval = 500;
                _tooltipTimer.Start();
            }
            else if (_showTooltip && _hoveredTextControl is not null)
            {
                _fadingTooltip = _hoveredTextControl;
                _fadingTooltipPointer = pointer ?? _textControlPointer;
                _tooltipFadeStart = Environment.TickCount64;
                _tooltipTimer.Interval = 33;
                _tooltipTimer.Start();
            }
            else if (_fadingTooltip is not null)
                _tooltipTimer.Start();
            _showTooltip = false;
        }
        if (_fadingTooltip is not null && pointer.HasValue) _fadingTooltipPointer = pointer;
        _hoveredTextControl = menu;
        _textControlPointer = pointer;
        Invalidate();
    }

    private void UpdateMenuPulse()
    {
        double phase = ((Environment.TickCount64 - _menuPulseStart) % 3000) / 500.0;
        byte alpha = (byte)(255 * Math.Min(1.0, Math.Min(phase, 6.0 - phase)));
        bool changed = false;
        foreach (PangyaUiNode node in _menuStates.Keys.ToArray())
        {
            PangyaUiMenuState state = _menuStates[node];
            if (state.SelectionAlpha == alpha) continue;
            _menuStates[node] = state with { SelectionAlpha = alpha };
            changed = true;
        }
        if (changed) Invalidate();
    }

    internal Point LogicalPoint(Point point) =>
        new((int)Math.Floor(point.X / Zoom - FormPadding),
            (int)Math.Floor(point.Y / Zoom - FormPadding));

    private void UpdateCanvasSize()
    {
        if (_document is null) return;
        Size logicalSize = _selectedForm?.Bounds.Size is { Width: > 0, Height: > 0 } formSize
            ? formSize
            : _document.CanvasSize;
        int contentWidth = (int)Math.Ceiling((logicalSize.Width + FormPadding * 2) * Zoom);
        int contentHeight = (int)Math.Ceiling((logicalSize.Height + FormPadding * 2) * Zoom);
        Size = new Size(
            Math.Max(Math.Max(1, contentWidth), _viewportSize.Width),
            Math.Max(Math.Max(1, contentHeight), _viewportSize.Height));
    }

    internal IReadOnlyList<PangyaUiNode> RenderOrderedNodes()
        => _document is null ? [] : _renderer.GetRenderOrder(_document, SelectedForm,
            SelectedNode, _enabledSymbols);

    private void BeginComboPress(PangyaUiNode node)
    {
        Focus();
        _pressedButton = node;
        _pressedButtonInside = true;
        Capture = true;
        Invalidate();
    }

    private void ClearPressedButton()
    {
        if (_pressedButton is null) return;
        _pressedButton = null;
        _pressedButtonInside = false;
        Invalidate();
    }

    private void ExpireButtonPresses()
    {
        long now = Environment.TickCount64;
        bool changed = false;
        foreach (PangyaUiNode node in _buttonPressTicks.Keys.ToArray())
            if ((now - _buttonPressTicks[node]) / 1000.0 > PangyaUiRenderer.GetButtonPushDelay(node))
                changed |= _buttonPressTicks.Remove(node);
        if (_buttonPressTicks.Count == 0) _buttonTimer.Stop();
        if (changed) Invalidate();
    }

    private byte GetTooltipAlpha() => _fadingTooltip is null ? (byte)220 :
        (byte)Math.Clamp(220 - (Environment.TickCount64 - _tooltipFadeStart) * 0.66, 0, 220);

    private IReadOnlyDictionary<PangyaUiNode, PangyaUiEditCaret>? GetEditCarets()
    {
        if (_focusedEdit is not PangyaUiNode node) return null;
        string text = GetEditText(node);
        int line = 0;
        int column = 0;
        for (int index = 0; index < _editCaretIndex; index++)
        {
            if (text[index] == '\n')
            {
                line++;
                column = 0;
            }
            else column++;
        }
        return new Dictionary<PangyaUiNode, PangyaUiEditCaret>
        {
            [node] = new(line, column,
                TimeSpan.FromMilliseconds(Environment.TickCount64 - _editCaretStart))
        };
    }

    private string GetEditText(PangyaUiNode node) => string.Join("\n", _editLines.GetValueOrDefault(node) ?? []);

    private static int PreviousEditIndex(string text, int index) =>
        index <= 0 ? 0 : index - (index > 1 && char.IsSurrogatePair(text[index - 2], text[index - 1]) ? 2 : 1);

    private static int MoveEditCaretVertically(string text, int index, bool up)
    {
        int start = index == 0 ? 0 : text.LastIndexOf('\n', index - 1) + 1;
        int end = text.IndexOf('\n', index);
        if ((up && start == 0) || (!up && end < 0)) return index;

        int targetStart = up ? (start == 1 ? 0 : text.LastIndexOf('\n', start - 2) + 1) : end + 1;
        int targetEnd = up ? start - 1 : text.IndexOf('\n', targetStart);
        if (targetEnd < 0) targetEnd = text.Length;
        int target = targetStart;
        for (int source = start; source < index && target < targetEnd; source = NextEditIndex(text, source))
            target = NextEditIndex(text, target);
        return target;
    }

    private static int NextEditIndex(string text, int index) =>
        index >= text.Length ? text.Length :
            index + (index + 1 < text.Length && char.IsSurrogatePair(text[index], text[index + 1]) ? 2 : 1);

    private PangyaUiRenderOptions RenderOptions() => new(
        ButtonState, _enabledSymbols, ShowDebugBounds, SelectedNode, 1f / Zoom)
        {
            ViewerTopRows = _viewerTopRows, ViewerScrollRows = _viewerScrollRows, StartupWallpaper = StartupWallpaper,
            StartupWallpaperImage = _startupWallpaperImage,
            ExtendedGaugeSteps = _extendedGaugeSteps,
            Combos = _combos,
            EditLines = _editLines, EditCarets = GetEditCarets(), EditTopRows = _editTopRows,
            ComboViewportHeight = SelectedForm?.Bounds.Height ?? _document?.CanvasSize.Height ?? 0,
            IsViewFocused = _viewFocused,
            FrameBlinkElapsed = _frameBlinkElapsed,
            ButtonPressTicks = _buttonPressTicks, CurrentTick = Environment.TickCount64,
            PressedButton = _pressedButtonInside ? _pressedButton : null,
            LatchedButtons = _latchedButtons,
            HoveredNode = _draggedViewer ?? _hoveredTextControl, Pointer = _textControlPointer, MenuStates = _menuStates,
            ShowTooltip = (_showTooltip || _fadingTooltip is not null) && _viewFocused && !Capture,
            TooltipAlpha = GetTooltipAlpha(),
            TooltipNode = _fadingTooltip, TooltipPointer = _fadingTooltipPointer,
            CaptionScrollOffsets = _captionOffsets,
            EditEmoticonAnimationTime = _layoutAnimationTimer.Enabled
                ? TimeSpan.FromMilliseconds(Environment.TickCount64 - _layoutAnimationStart) : null,
            LayoutAnimationTime = _layoutAnimationTimer.Enabled
                ? TimeSpan.FromMilliseconds(Environment.TickCount64 - _layoutAnimationStart) : TimeSpan.Zero
        };
}

internal sealed class PangyaUiNodeEventArgs(PangyaUiNode node) : EventArgs
{
    public PangyaUiNode Node { get; } = node;
}

internal sealed class PangyaUiPropertyPanel : UserControl
{
    private readonly PangyaFileImageProvider _assets;
    private readonly GroupBox _group = new() { Dock = DockStyle.Fill };
    private readonly TableLayoutPanel _layout = new()
    {
        Dock = DockStyle.Top,
        AutoSize = true,
        ColumnCount = 2,
        Padding = new Padding(8)
    };
    private readonly Label _nameLabel = new();
    private readonly Label _typeLabel = new();
    private readonly Label _xLabel = new();
    private readonly Label _yLabel = new();
    private readonly Label _widthLabel = new();
    private readonly Label _heightLabel = new();
    private readonly Label _resourceLabel = new();
    private readonly TextBox _name = new();
    private readonly TextBox _type = new();
    private readonly NumericUpDown _x = Numeric();
    private readonly NumericUpDown _y = Numeric();
    private readonly NumericUpDown _width = Numeric(nonNegative: true);
    private readonly NumericUpDown _height = Numeric(nonNegative: true);
    private readonly TextBox _resource = new();
    private readonly Button _resourceBrowse = new() { Text = "…", Dock = DockStyle.Fill };
    private readonly TableLayoutPanel _resourceEditor = new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        ColumnCount = 2,
        Margin = Padding.Empty
    };
    private PangyaUiNode? _selectedNode;
    private bool _loading;

    public PangyaUiPropertyPanel(PangyaFileImageProvider assets)
    {
        _assets = assets;
        Name = "pnlUiProperties";
        Dock = DockStyle.Fill;
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        _layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _resourceEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _resourceEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34));
        _resourceEditor.Controls.Add(_resource, 0, 0);
        _resourceEditor.Controls.Add(_resourceBrowse, 1, 0);
        AddRow(_nameLabel, _name, 0);
        AddRow(_typeLabel, _type, 1);
        AddRow(_xLabel, _x, 2);
        AddRow(_yLabel, _y, 3);
        AddRow(_widthLabel, _width, 4);
        AddRow(_heightLabel, _height, 5);
        AddRow(_resourceLabel, _resourceEditor, 6);
        _group.Controls.Add(_layout);
        Controls.Add(_group);

        _name.TextChanged += (_, _) => ApplyName();
        _type.TextChanged += (_, _) => ApplyType();
        _resource.TextChanged += (_, _) => ApplyResource();
        _x.ValueChanged += (_, _) => ApplyBounds();
        _y.ValueChanged += (_, _) => ApplyBounds();
        _width.ValueChanged += (_, _) => ApplyBounds();
        _height.ValueChanged += (_, _) => ApplyBounds();
        _resourceBrowse.Click += (_, _) => BrowseForResource();
    }

    public event EventHandler? ElementChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PangyaUiNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            _selectedNode = value;
            LoadSelectedNode();
        }
    }

    public void ApplyLocalization()
    {
        UpdateGroupText();
        _nameLabel.Text = Strings.UiEditor_Name;
        _typeLabel.Text = Strings.UiEditor_Type;
        _xLabel.Text = "X";
        _yLabel.Text = "Y";
        _widthLabel.Text = Strings.UiEditor_Width;
        _heightLabel.Text = Strings.UiEditor_Height;
        _resourceLabel.Text = Strings.UiEditor_Resource;
        _resourceBrowse.AccessibleName = Strings.UiEditor_SelectResource;
    }

    internal Point DisplayedLocation =>
        new(decimal.ToInt32(_x.Value), decimal.ToInt32(_y.Value));

    internal string GetResourceInitialDirectory()
    {
        string? resolvedPath = _assets.TryResolvePath(_resource.Text);
        string? resolvedDirectory = Path.GetDirectoryName(resolvedPath);
        return !string.IsNullOrWhiteSpace(resolvedDirectory) && Directory.Exists(resolvedDirectory)
            ? resolvedDirectory
            : _assets.DataRoot;
    }

    internal bool TrySetResourceFromPath(string selectedPath)
    {
        string fullPath = Path.GetFullPath(selectedPath);
        string relativePath = Path.GetRelativePath(_assets.DataRoot, fullPath);
        if (Path.IsPathRooted(relativePath) ||
            relativePath.Equals("..", StringComparison.Ordinal) ||
            relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            return false;

        _resource.Text = Path.GetFileName(fullPath);
        return true;
    }

    private void LoadSelectedNode()
    {
        _loading = true;
        try
        {
            bool enabled = _selectedNode?.IsEditable == true;
            _layout.Enabled = enabled;
            _name.Text = _selectedNode?.Name ?? string.Empty;
            _type.Text = _selectedNode?.Type ?? string.Empty;
            _resource.Text = _selectedNode?.GetResource(PangyaUiButtonState.Normal) ?? string.Empty;
            Rectangle bounds = _selectedNode?.Bounds ?? Rectangle.Empty;
            _x.Value = Clamp(_x, bounds.X);
            _y.Value = Clamp(_y, bounds.Y);
            _width.Value = Clamp(_width, bounds.Width);
            _height.Value = Clamp(_height, bounds.Height);
            UpdateGroupText();
        }
        finally { _loading = false; }
    }

    private void UpdateGroupText()
    {
        _group.Text = _selectedNode?.IsPreviewOnly == true
            ? $"{Strings.UiEditor_Properties} — {Strings.UiEditor_ConditionalPreview}"
            : Strings.UiEditor_Properties;
    }

    private void ApplyName()
    {
        if (_loading || _selectedNode is null) return;
        _selectedNode.SetName(_name.Text);
        ElementChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyType()
    {
        if (_loading || _selectedNode is null) return;
        _selectedNode.SetType(_type.Text);
        ElementChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyResource()
    {
        if (_loading || _selectedNode is null) return;
        _selectedNode.SetResource(_resource.Text);
        ElementChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BrowseForResource()
    {
        using OpenFileDialog dialog = FileDialogFactory.CreateUiResourceOpenDialog(GetResourceInitialDirectory());
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        FileDialogFactory.RememberDirectory(FileDialogKind.UiResource, dialog.FileName);
        if (!TrySetResourceFromPath(dialog.FileName))
            MessageBox.Show(FindForm(), Strings.UiEditor_ResourceMustBeInData, Strings.UiEditor_Title,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    private void ApplyBounds()
    {
        if (_loading || _selectedNode is null) return;
        _selectedNode.SetBounds(new Rectangle(
            decimal.ToInt32(_x.Value), decimal.ToInt32(_y.Value),
            decimal.ToInt32(_width.Value), decimal.ToInt32(_height.Value)));
        ElementChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddRow(Label label, Control editor, int row)
    {
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        editor.Dock = DockStyle.Fill;
        _layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _layout.Controls.Add(label, 0, row);
        _layout.Controls.Add(editor, 1, row);
    }

    private static NumericUpDown Numeric(bool nonNegative = false) => new()
    {
        Minimum = nonNegative ? 0 : -8192,
        Maximum = 8192
    };

    private static decimal Clamp(NumericUpDown control, int value) =>
        Math.Clamp((decimal)value, control.Minimum, control.Maximum);
}
