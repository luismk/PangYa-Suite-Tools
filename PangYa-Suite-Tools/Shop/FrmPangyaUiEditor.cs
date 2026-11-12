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
    private readonly ToolStripButton _openReferenceButton = new();
    private PangyaUiResourceCatalog _resourceCatalog;
    private string? _resourceEntryPath;
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
        _resourceCatalog = resourceCatalog;
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

    private static PangyaPreviewBitmapFont? ReadPreviewFont(string root, string filename, int height, CancellationToken cancellationToken)
    {
        string path = Path.Combine(root, "2d", "font", filename);
        if (!File.Exists(path)) path = Path.Combine(root, "data", "2d", "font", filename);
        try
        {
            using var font = PangyaAPI.WFT.WftFontReader.Open(path);
            if (font.CellSize != height) return null;
            using var korean = ReadKoreanPreviewFont(Path.GetDirectoryName(path)!, height);
            return new PangyaPreviewBitmapFont(font, cancellationToken, korean);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static PangyaAPI.WFT.WftFont? ReadKoreanPreviewFont(string directory, int height)
    {
        // Some Japanese archives store the original Korean filename as bytes
        // interpreted by the extractor in the Japanese code page.
        byte[] name = [0xb5, 0xb8, 0xbf, 0xf2, 0xc3, 0xbc];
        foreach (int codePage in new[] { 949, 932 })
        {
            string stem = System.Text.CodePagesEncodingProvider.Instance.GetEncoding(codePage)!.GetString(name);
            string path = Path.Combine(directory, $"fnt_{stem}_{height}.wft");
            try
            {
                var font = PangyaAPI.WFT.WftFontReader.Open(path);
                // The extracted JP archive labels the small Korean font 11
                // even though its WFNT header declares a 12-pixel cell.
                if (font.CellSize == height || height == 11 && font.CellSize == 12) return font;
                font.Dispose();
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                // The Japanese font is still usable without this optional fallback.
            }
        }
        return null;
    }

    private static PangyaUiLanguageTable? ReadPreviewLanguageTable(string root)
    {
        string? parent = Path.GetFileName(Path.TrimEndingDirectorySeparator(root)).Equals("data", StringComparison.OrdinalIgnoreCase)
            ? Directory.GetParent(root)?.FullName : null;
        foreach (string directory in new[] { root, Path.Combine(root, "data"), parent ?? root }.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string korean = Path.Combine(directory, "korea.dat");
            string japanese = Path.Combine(directory, "japan.dat");
            if (!File.Exists(korean) || !File.Exists(japanese)) continue;
            try { return PangyaUiLanguageTable.Load(korean, japanese); }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                // Keep looking for a complete pair.
            }
        }
        return null;
    }

    public static async Task<FrmPangyaUiEditor> CreateAsync(string dataRoot,
        CancellationToken cancellationToken = default, string? startupWallpaper = null, string? initialPath = null,
        string? resourceEntryPath = null)
    {
        string fullRoot = Path.GetFullPath(dataRoot);
        (IReadOnlyList<string> files, PangyaFileImageProvider assets,
            PangyaUiResourceCatalog catalog, PangyaPreviewBitmapFont? tooltipFont, PangyaPreviewBitmapFont? captionFont,
            PangyaUiLanguageTable? languageTable, string entryPath) =
            await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<string> discovered = PangyaUiDocument.FindUiFiles(fullRoot);
            if (discovered.Count == 0) throw new InvalidDataException(Strings.UiEditor_NoXmlFiles);
            var resolver = new PangyaFileImageProvider(fullRoot);
            string entry = resourceEntryPath ?? discovered.FirstOrDefault(path =>
                Path.GetFileName(path).Equals("all.xml", StringComparison.OrdinalIgnoreCase)) ??
                initialPath ?? discovered.FirstOrDefault(path =>
                    Path.GetFileName(path).Equals("shop.xml", StringComparison.OrdinalIgnoreCase)) ?? discovered[0];
            PangyaUiResourceCatalog catalog = PangyaUiResourceCatalog.LoadEntry(entry, discovered, fullRoot);
            return (discovered, resolver, catalog,
                ReadPreviewFont(fullRoot, "ms_ui_gothic_11_jp.wft", 11, cancellationToken),
                ReadPreviewFont(fullRoot, "fnt_japan_12.wft", 12, cancellationToken),
                ReadPreviewLanguageTable(fullRoot), Path.GetFullPath(entry));
        }, cancellationToken);
        var editor = new FrmPangyaUiEditor(fullRoot, files, assets, catalog);
        editor._resourceEntryPath = entryPath;
        editor._canvas.TooltipFont = tooltipFont;
        editor._canvas.LanguageTable = languageTable;
        editor._canvas.CaptionFont = captionFont;
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
            string initial = initialPath ?? files.FirstOrDefault(path =>
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
        _openReferenceButton.Name = "btnOpenUiReference";
        _openReferenceButton.Click += async (_, _) => await OpenReferenceAsync();
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
            _openButton, _openReferenceButton, _saveButton, _reloadButton, _wallpaperButton, new ToolStripSeparator(),
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

    private string? GetSelectedReferencePath()
    {
        PangyaUiNode? node = _properties.SelectedNode;
        if (node is null || _currentPath is null) return null;
        if (node.Type.Equals("INCLUDE", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                if (Path.IsPathRooted(node.Name)) return null;
                string path = Path.GetFullPath(node.Name, Path.GetDirectoryName(_currentPath)!);
                string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(_dataRoot)) + Path.DirectorySeparatorChar;
                return path.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? path : null;
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return null;
            }
        }
        PangyaUiNode? definition = _resourceCatalog.TryResolve(node.GetResource(_canvas.ButtonState),
            node.Type, _enabledSymbols);
        return definition is null ? null : _resourceCatalog.GetSourcePath(definition);
    }

    private async Task OpenReferenceAsync()
    {
        string? path = GetSelectedReferencePath();
        if (path is null) return;
        string[] symbols = _enabledSymbols.ToArray();
        PangyaUiNode? selected = _properties.SelectedNode;
        PangyaUiNode? definition = selected is null ? null : _resourceCatalog.TryResolve(
            selected.GetResource(_canvas.ButtonState), selected.Type, _enabledSymbols);
        _openReferenceButton.Enabled = false;
        try
        {
            FrmPangyaUiEditor editor = await CreateAsync(_dataRoot, initialPath: path,
                resourceEntryPath: _resourceEntryPath);
            editor._enabledSymbols.UnionWith(symbols);
            foreach (ToolStripControlHost host in editor._ifdefControls)
                if (host.Control is CheckBox checkBox)
                    checkBox.Checked = symbols.Contains(checkBox.Text, StringComparer.OrdinalIgnoreCase);
            editor.RefreshConditionalView();
            if (definition is not null && editor._document is not null)
            {
                PangyaUiNode? target = editor._document.Roots.FirstOrDefault(root =>
                    root.Name == definition.Name && root.Type == definition.Type && root.IsVisible(editor._enabledSymbols));
                if (target is not null) editor.BuildElementTree(target);
            }
            editor.Show();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException
                                   or InvalidDataException or ArgumentException)
        {
            AppLogger.Instance.Log(LogSource, $"Could not open referenced UI XML '{path}': {ex.Message}", AppLogLevel.Error);
            MessageBox.Show(this, ex.Message, Strings.UiEditor_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { if (!IsDisposed) UpdateActionState(); }
    }

    private async Task LoadDocumentAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            UseWaitCursor = true;
            _toolbar.Enabled = false;
            string entry = _resourceEntryPath ?? path;
            (PangyaUiDocument document, PangyaUiResourceCatalog catalog) = await Task.Run(() =>
            {
                PangyaUiDocument loaded = PangyaUiDocument.Load(path);
                cancellationToken.ThrowIfCancellationRequested();
                PangyaUiResourceCatalog resources = PangyaUiResourceCatalog.LoadEntry(entry, _uiFiles, _dataRoot);
                return (loaded, resources);
            }, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _resourceCatalog = catalog;
            _canvas.SetResourceCatalog(catalog);
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
        var previouslyEnabled = _enabledSymbols.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (ToolStripControlHost host in _ifdefControls)
        {
            _toolbar.Items.Remove(host);
            host.Dispose();
        }
        _ifdefControls.Clear();
        _enabledSymbols.Clear();

        foreach (string symbol in document.ConditionalSymbols.Concat(_resourceCatalog.ConditionalSymbols)
                     .Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(value => value,
                     StringComparer.OrdinalIgnoreCase))
        {
            var checkBox = new CheckBox
            {
                Name = "chkUiIfdef_" + symbol,
                Text = symbol,
                AutoSize = true,
                Margin = Padding.Empty,
                Checked = previouslyEnabled.Contains(symbol)
            };
            if (checkBox.Checked) _enabledSymbols.Add(symbol);
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
        UpdateActionState();
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
        _openReferenceButton.Text = Strings.UiEditor_OpenReference;
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
        _openReferenceButton.Enabled = !UseWaitCursor && GetSelectedReferencePath() is not null;
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
        if (!focused) UpdateCursorPreview(null);
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
    private readonly Dictionary<PangyaUiNode, bool> _viewerFollowBottom = [];
    private readonly Dictionary<PangyaUiNode, PangyaUiGaugeState> _gaugeInputs = [];
    private readonly Dictionary<PangyaUiNode, bool> _wheelEvents = [];

    internal void SetWindowWheelEvent(PangyaUiNode node, bool enabled)
    {
        _wheelEvents[node] = enabled;
        Invalidate();
    }
    private readonly Dictionary<PangyaUiNode, PangyaUiComboPreview> _combos = [];
    private readonly Dictionary<(PangyaUiNode Node, Point Origin), int> _selectedTabs = [];
    private readonly HashSet<PangyaUiNode> _makeRoomMatchForms = [];
    private (PangyaUiNode Node, int Index, Rectangle Bounds, Point Origin)? _pressedTab, _hoveredTab;
    private readonly List<(PangyaUiNode Node, int Index, Rectangle Bounds, Point Origin)> _renderedTabs = [];
    private readonly Dictionary<PangyaUiNode, IReadOnlyList<string>> _editLines = [];
    private readonly Dictionary<PangyaUiNode, int> _editTopRows = [];
    private readonly Dictionary<PangyaUiNode, bool> _editFollowBottom = [];
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
    private (PangyaUiNode Node, float Travel, int PreviousY, float Offset, Point TrackPoint)? _comboThumbDrag;
    private PangyaUiNode? _draggedViewer;
    private Point? _wheelPointer;
    private int _viewerDragPreviousY;
    private float _viewerThumbOffset;
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
    private readonly Dictionary<PangyaUiNode, PangyaUiButtonInputState> _buttonInputs = [];
    private long _buttonInputTick = Environment.TickCount64;
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
    private readonly System.Windows.Forms.Timer _cursorTimer = new() { Interval = 16 };
    private long _cursorAnimationStart;
    private Point? _textControlPointer;
    internal const int FormPadding = 16;
    private PangyaUiRenderer _renderer;
    private readonly IPangyaImageProvider _imageProvider;
    private HashSet<string> _enabledSymbols = new(StringComparer.OrdinalIgnoreCase);
    private PangyaUiDocument? _document;
    private PangyaUiNode? _selectedForm;
    private PangyaUiNode? _selectedNode;
    private PangyaUiNode? _draggedNode;
    private Point? _dragStart;
    private Rectangle _dragStartBounds;
    private Size _viewportSize;
    private float _zoom = 1f;

    internal void SetResourceCatalog(PangyaUiResourceCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _renderer = new PangyaUiRenderer(_imageProvider, catalog);
        Invalidate();
    }

    public PangyaUiCanvas(IPangyaImageProvider assets,
        PangyaUiResourceCatalog? resourceCatalog = null)
    {
        _imageProvider = assets;
        _renderer = new PangyaUiRenderer(assets, resourceCatalog);
        DoubleBuffered = true;
        BackColor = Color.FromArgb(36, 39, 44);
        Cursor = Cursors.Default;
        MouseDown += CanvasMouseDown;
        MouseDown += (_, e) => RefreshCursorPreview(e.Location);
        MouseMove += CanvasMouseMove;
        Disposed += (_, _) => _startupWallpaperImage?.Dispose();
        _editCaretTimer.Tick += (_, _) => Invalidate();
        Disposed += (_, _) => _editCaretTimer.Dispose();
        LostFocus += (_, _) => FocusEdit(null);
        MouseLeave += (_, _) =>
        {
            UpdateTextControlHover(null, null);
            if (_hoveredTab is null) return;
            _hoveredTab = null;
            Invalidate();
        };
        MouseLeave += (_, _) => UpdateCaptionHover(null);
        MouseLeave += (_, _) => UpdateCursorPreview(null);
        MouseLeave += (_, _) => UpdateWheelPointer(null);
        MouseLeave += (_, _) =>
        {
            foreach (PangyaUiButtonInputState input in _buttonInputs.Values) input.SetCursor(false);
        };
        _cursorTimer.Tick += (_, _) =>
        {
            if (_cursorPreview is not null)
                RefreshCursorPreview(PointToClient(System.Windows.Forms.Cursor.Position));
        };
        Disposed += (_, _) =>
        {
            UpdateCursorPreview(null);
            _cursorTimer.Dispose();
        };
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
            if (e.Button != MouseButtons.Left) return;
            if (e.Button == MouseButtons.Left && _pressedTab is { } tab &&
                tab.Bounds.Contains(LogicalPoint(e.Location)) && _renderedTabs.Contains(tab))
            {
                if (tab.Node.Name == "gametype" && tab.Node.Parent is { } room &&
                    string.Equals(room.Name, "makeroom", StringComparison.OrdinalIgnoreCase))
                {
                    bool changed = tab.Index == 1 ? _makeRoomMatchForms.Add(room) : _makeRoomMatchForms.Remove(room);
                    if (changed)
                    {
                        foreach (PangyaUiNode child in room.Children)
                        {
                            foreach (var key in _selectedTabs.Keys.Where(key => ReferenceEquals(key.Node, child)).ToArray())
                                _selectedTabs.Remove(key);
                            if (child.Name == "picture") ResetViewerScroll(child);
                        }
                    }
                }
                _selectedTabs[(tab.Node, tab.Origin)] = tab.Index;
                Invalidate();
            }
            _pressedTab = null;
            if (e.Button == MouseButtons.Left && _pressedButton is PangyaUiNode inputButton &&
                _buttonInputs.TryGetValue(inputButton, out PangyaUiButtonInputState? input))
            {
                bool inside = GetRenderedBounds(inputButton).Contains(LogicalPoint(e.Location));
                input.ButtonUp(inside);
                input.SetCursor(inside);
            }
            if (e.Button == MouseButtons.Left && _pressedButton is PangyaUiNode pressed &&
                _combos.TryGetValue(pressed, out PangyaUiComboPreview? combo) &&
                _renderer.HitTestComboUnfold(pressed, LogicalPoint(e.Location), _enabledSymbols, RenderOptions()))
                SetComboPreview(pressed, combo with { IsOpen = !combo.IsOpen, HoveredIndex = -1 });
            ClearPressedButton();
            EndViewerDrag();
            _draggedNode = null;
            _dragStart = null;
            Capture = false;
        };
        MouseUp += (_, e) => RefreshCursorPreview(e.Location);
        MouseUp += CanvasTextButtonMouseUp;
        MouseCaptureChanged += (_, _) =>
        {
            if (Capture) return;
            if (_comboThumbDrag is { } drag && _combos.TryGetValue(drag.Node, out PangyaUiComboPreview? combo) &&
                combo.ScrollThumbRow is float row)
                SetComboPreview(drag.Node, combo with
                {
                    TopRow = Math.Clamp((int)MathF.Ceiling(row - 0.5f), 0,
                        PangyaUiRenderer.GetComboMaximumTopRow(drag.Node, combo)),
                    ScrollThumbRow = null, ScrollThumbOffset = null
                });
            _comboThumbDrag = null;
            _pressedTab = null;
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
        _viewerFollowBottom.Remove(node);
    }

    private bool ViewerAcceptsMouse(PangyaUiNode node) =>
        PangyaUiRenderer.ViewerAcceptsMouse(node, RenderOptions());

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
                ClearTabHitState();
                _layoutAnimationStart = Environment.TickCount64;
                _layoutAnimationTimer.Enabled = value is not null &&
                    _renderer.HasAnimatedControls(value, _enabledSymbols);
            }
            if (!ReferenceEquals(_selectedForm, value)) UpdateCursorPreview(null);
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
            ClearTabHitState();
            _enabledSymbols = new HashSet<string>(value, StringComparer.OrdinalIgnoreCase);
            if (_cursorPreview is not null)
                RefreshCursorPreview(PointToClient(System.Windows.Forms.Cursor.Position));
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
        UpdateCursorPreview(null);
        _menuPulseStart = Environment.TickCount64;
        _document = document;
        _viewerTopRows.Clear();
        _viewerScrollRows.Clear();
        _viewerFollowBottom.Clear();
        _gaugeInputs.Clear();
        _wheelEvents.Clear();
        _wheelPointer = null;
        _combos.Clear();
        _renderedTabs.Clear();
        _hoveredTab = null;
        _selectedTabs.Clear();
        _makeRoomMatchForms.Clear();
        _pressedTab = null;
        _editLines.Clear();
        _editTopRows.Clear();
        _editFollowBottom.Clear();
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
        _buttonInputs.Clear();
        _buttonInputTick = Environment.TickCount64;
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

    private void ClearTabHitState()
    {
        _renderedTabs.Clear();
        _hoveredTab = null;
        if (_pressedTab is null) return;
        _pressedTab = null;
        Capture = false;
    }

    private (PangyaUiNode Node, int Index, Rectangle Bounds, Point Origin)? HitRenderedTab(PangyaUiNode hit, Point point)
    {
        for (int index = _renderedTabs.Count - 1; index >= 0; index--)
        {
            var tab = _renderedTabs[index];
            if ((ReferenceEquals(tab.Node, hit) || hit.IsForm ||
                 hit.Type.Equals("MACROITEM", StringComparison.OrdinalIgnoreCase)) && tab.Bounds.Contains(point))
                return tab;
        }
        return null;
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
        _renderedTabs.Clear();
        PangyaUiRenderOptions options = RenderOptions();
        PangyaUiNode? wheelFocusedNode = _wheelPointer is Point pointer
            ? _renderer.HitTestWheelTarget(_document, SelectedForm, pointer, options) : null;
        _renderer.Render(e.Graphics, _document, SelectedForm, options with
        {
            WheelFocusedNode = wheelFocusedNode,
            CursorPreview = _cursorPreview,
            HoveredTab = _hoveredTab is { } hover ? (hover.Node, hover.Index, hover.Bounds) : null,
            OnTabInstanceRendered = (node, index, bounds, origin) => _renderedTabs.Add((node, index, bounds, origin))
        });
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal PangyaPreviewBitmapFont? TooltipFont { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal PangyaPreviewBitmapFont? CaptionFont { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal PangyaUiLanguageTable? LanguageTable { get; set; }

    private int FreshCursorIndex
    {
        get
        {
            if (Cursor == Cursors.IBeam) return 12;
            if (Cursor != Cursors.Hand) return 0;
            PangyaUiNode? button = _hoveredTextControl;
            if (button is not null && _buttonInputs.TryGetValue(button, out PangyaUiButtonInputState? input))
                return input.Status == PangyaUiButtonState.Selected ? 2 : 1;
            bool selected = button is not null && (_latchedButtons.Contains(button) ||
                (_pressedButtonInside && ReferenceEquals(_pressedButton, button)));
            return selected ? 2 : 1;
        }
    }

    private void RefreshCursorPreview(Point location)
    {
        int index = FreshCursorIndex;
        if (index == 2 && !_renderer.CanDrawCursor(index, _enabledSymbols)) index = 1;
        int frames = _viewFocused && _document is not null && ClientRectangle.Contains(location) && Cursor != Cursors.SizeAll
            ? _renderer.GetCursorFrameCount(index, _enabledSymbols) : 0;
        if (frames == 0)
        {
            UpdateCursorPreview(null);
            return;
        }
        long now = Environment.TickCount64;
        if (_cursorPreview?.Index != index) _cursorAnimationStart = now;
        int frame = frames > 1 ? (int)(((now - _cursorAnimationStart) % 350) * frames / 350) : 0;
        _cursorTimer.Enabled = frames > 1;
        UpdateCursorPreview(new PangyaUiCursorPreview(LogicalPoint(location), index, frame));
    }

    private void UpdateCursorPreview(PangyaUiCursorPreview? cursor)
    {
        if (cursor is null) _cursorTimer.Stop();
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

    private static readonly System.Text.Encoding EditTextEncoding =
        System.Text.CodePagesEncodingProvider.Instance.GetEncoding(932)
        ?? throw new InvalidOperationException("Shift-JIS encoding is unavailable.");

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
            if (newline && !PangyaUiRenderer.IsMultilineEdit(node)) return;
            if (!newline && char.IsControl(e.KeyChar)) return;
            if (node.Parameters.GetValueOrDefault("numberonly") == "true" &&
                e.KeyChar is not (>= '0' and <= '9')) return;
            int limit = int.TryParse(node.Parameters.GetValueOrDefault("charlimit"), out int parsed)
                ? Math.Clamp(parsed, 0, 16384) : 255;
            string input = (newline ? '\n' : e.KeyChar).ToString();
            if ((long)EditTextEncoding.GetByteCount(value) + EditTextEncoding.GetByteCount(input) > limit) return;
            if (!newline)
            {
                int start = _editCaretIndex == 0 ? 0 : value.LastIndexOf('\n', _editCaretIndex - 1) + 1;
                int end = value.IndexOf('\n', _editCaretIndex);
                string line = value[start..(end < 0 ? value.Length : end)];
                float widthLimit = float.TryParse(node.Parameters.GetValueOrDefault("widthlimit"),
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture,
                    out float width) && float.IsFinite(width) ? width : 1000;
                string candidate = line.Insert(_editCaretIndex - start, input);
                float candidateWidth;
                if (CaptionFont is { } bitmapFont)
                    candidateWidth = bitmapFont.Measure(candidate);
                else
                {
                    using Graphics graphics = CreateGraphics();
                    using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
                    format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                    candidateWidth = graphics.MeasureString(candidate, SystemFonts.DefaultFont, PointF.Empty, format).Width;
                }
                if (candidateWidth > widthLimit) return;
            }
            value = value.Insert(_editCaretIndex++, input);
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

    private void UpdateWheelPointer(Point? pointer)
    {
        if (_wheelPointer == pointer) return;
        _wheelPointer = pointer;
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        Point pointer = LogicalPoint(e.Location);
        UpdateWheelPointer(pointer);
        PangyaUiRenderOptions options = RenderOptions();
        PangyaUiNode? target = _document is not null && e.Delta != 0
            ? _renderer.HitTestWheelTarget(_document, SelectedForm, pointer, options) : null;
        if (target is PangyaUiNode open && _combos.TryGetValue(open, out PangyaUiComboPreview? combo))
        {
            if (combo.IsOpen)
            {
                int steps = e.Delta / 119;
                int maximum = PangyaUiRenderer.GetComboMaximumTopRow(open, combo);
                int current = combo.ScrollThumbRow is float row ? (int)MathF.Ceiling(row - 0.5f) : combo.TopRow;
                int next = (int)Math.Clamp((long)Math.Clamp(current, 0, maximum) -
                    steps, 0, maximum);
                SetComboPreview(open, combo with { TopRow = next, ScrollThumbRow = null, ScrollThumbOffset = null });
                if (_comboThumbDrag is { } drag && ReferenceEquals(drag.Node, open) &&
                    _renderer.TryGetComboScrollbarGeometry(open, drag.TrackPoint, RenderOptions(), out _,
                        out RectangleF thumb, out Rectangle track))
                {
                    drag.Offset = thumb.Y - track.Y;
                    drag.Travel = track.Height - thumb.Height;
                    _comboThumbDrag = drag;
                }
                int hovered = _renderer.HitTestComboRow(open, pointer, RenderOptions());
                SetComboPreview(open, _combos[open] with { HoveredIndex = hovered });
                if (e is HandledMouseEventArgs handled) handled.Handled = true;
                return;
            }
        }
        if (target is PangyaUiNode viewer)
        {
            if (_editLines.TryGetValue(viewer, out IReadOnlyList<string>? lines) &&
                PangyaUiRenderer.TryScrollEditWheel(viewer, lines.Count, e.Delta, options, out var editScrolled))
            {
                _editTopRows[viewer] = editScrolled.EditTopRows![viewer];
                _editFollowBottom[viewer] = editScrolled.EditFollowBottom![viewer];
                if (e is HandledMouseEventArgs handledEdit) handledEdit.Handled = true;
                Invalidate();
                return;
            }
            if (_renderer.TryScrollViewerWheel(viewer, e.Delta, options, out var scrolled))
            {
                _viewerScrollRows.Remove(viewer);
                _viewerTopRows[viewer] = scrolled.ViewerTopRows![viewer];
                _viewerFollowBottom[viewer] = scrolled.ViewerFollowBottom![viewer];
                if (ReferenceEquals(viewer, _draggedViewer))
                {
                    RectangleF thumb = _renderer.GetViewerScrollThumbGeometry(viewer,
                        scrolled with { DraggedViewerThumb = null });
                    _viewerThumbOffset = thumb.Y - _renderer.GetViewerViewport(viewer, options).Y;
                }
                if (e is HandledMouseEventArgs handled) handled.Handled = true;
                Invalidate();
                return;
            }
        }
        base.OnMouseWheel(e);
    }

    internal Rectangle GetRenderedBounds(PangyaUiNode node)
        => _renderer.GetRenderedBounds(node, RenderOptions());

    internal void CanvasMouseDown(object? sender, MouseEventArgs e)
    {
        UpdateWheelPointer(LogicalPoint(e.Location));
        if (_document is null || e.Button != MouseButtons.Left) return;
        Point logical = LogicalPoint(e.Location);
        if (_openCombo is PangyaUiNode open && _combos.TryGetValue(open, out PangyaUiComboPreview? combo))
        {
            if (_renderer.TryGetComboScrollbarGeometry(open, logical, RenderOptions(), out int rowDelta,
                    out RectangleF thumb, out Rectangle track))
            {
                if (logical.Y >= thumb.Top && logical.Y <= thumb.Bottom && track.Height > thumb.Height)
                {
                    _comboThumbDrag = (open, track.Height - thumb.Height, logical.Y, thumb.Y - track.Y,
                        new Point(track.Right - 1, track.Top));
                    Capture = true;
                }
                int maximum = PangyaUiRenderer.GetComboMaximumTopRow(open, combo);
                int topRow = Math.Clamp(Math.Clamp(combo.TopRow, 0, maximum) + rowDelta, 0, maximum);
                SetComboPreview(open, combo with { TopRow = topRow, HoveredIndex = -1 });
                return;
            }
            int item = _renderer.HitTestComboRow(open, logical, RenderOptions());
            bool unfold = _renderer.HitTestComboUnfold(open, logical, _enabledSymbols, RenderOptions());
            if (unfold)
            {
                BeginComboPress(open);
                return;
            }
            if (item < 0 && _renderer.GetComboDropdownBounds(open, RenderOptions()).Contains(logical)) return;
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
                if (!viewer.IsRenderVisible)
                    continue;
                RectangleF thumb = _renderer.GetViewerScrollThumbGeometry(viewer, RenderOptions());
                if (thumb.IsEmpty) continue;
                Rectangle viewport = _renderer.GetViewerViewport(viewer, RenderOptions());
                var track = new RectangleF(thumb.X - 3, viewport.Y, thumb.Width, viewport.Height);
                if (!track.Contains(logical)) continue;
                if (logical.Y < thumb.Top || logical.Y > thumb.Bottom)
                {
                    // Fresh moves by half the visible row capacity for a track click.
                    int step = viewport.Height / 20 / 2;
                    int maximum = _renderer.GetViewerMaximumTopRow(viewer, _enabledSymbols, RenderOptions());
                    int topRow = _viewerTopRows.GetValueOrDefault(viewer);
                    _viewerScrollRows.Remove(viewer);
                    _viewerTopRows[viewer] = Math.Clamp(topRow + (logical.Y < thumb.Y ? -step : step), 0, maximum);
                    _viewerFollowBottom[viewer] = logical.Y >= thumb.Y && _viewerTopRows[viewer] == maximum;
                    Invalidate();
                    return;
                }
                _draggedViewer = viewer;
                _viewerDragPreviousY = logical.Y;
                _viewerThumbOffset = thumb.Y - viewport.Y;
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
        if (_renderer.GetViewerMaximumTopRow(hit, _enabledSymbols, RenderOptions()) > 0)
            Focus();
        SelectedNode = hit;
        SelectionChanged?.Invoke(this, hit);
        if (e.Button == MouseButtons.Left && HitRenderedTab(hit, logical) is { } tab)
        {
            _pressedTab = tab;
            Capture = true;
            return;
        }
        if (hit.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase) &&
            hit.Parameters.GetValueOrDefault("readonly") != "true" &&
            hit.Parameters.GetValueOrDefault("focusoff") != "true")
        {
            Focus();
            FocusEdit(hit);
            return;
        }
        if (_combos.TryGetValue(hit, out PangyaUiComboPreview? closed) && closed.Items.Count > 0 &&
            _renderer.HitTestComboUnfold(hit, logical, _enabledSymbols, RenderOptions()))
        {
            BeginComboPress(hit);
            return;
        }
        PangyaUiRenderOptions buttonOptions = RenderOptions();
        if (e.Button == MouseButtons.Left && hit.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase) &&
            PangyaUiRenderer.HasButtonSkin(hit, buttonOptions))
        {
            if (!_buttonInputs.TryGetValue(hit, out PangyaUiButtonInputState? input))
            {
                input = new() { PushStyle = PangyaUiRenderer.GetButtonPushStyle(hit, buttonOptions) };
                input.SetPushDelay(PangyaUiRenderer.GetButtonPushDelay(hit, buttonOptions));
                _buttonInputs[hit] = input;
            }
            input.SetCursor(true);
            // Native readiness and timing changes precede the capture request.
            input.ButtonDown(true, () =>
            {
                Capture = true;
                return Capture;
            });
            input.SetCursor(true);
            if (input.Captured)
            {
                _pressedButton = hit;
                _pressedButtonInside = true;
            }
            else Capture = false;
            if (input.PushStyle == 1 && input.SecondsSincePress <= input.PushDelay &&
                !_buttonPressTicks.ContainsKey(hit))
            {
                _buttonPressTicks[hit] = Environment.TickCount64 - 100;
                _buttonTimer.Start();
            }
            Invalidate();
            return;
        }
        int gaugeDirection = _renderer.HitTestExtendedGaugeButton(hit, logical, _enabledSymbols, RenderOptions());
        if (gaugeDirection != 0)
        {
            if (!_gaugeInputs.TryGetValue(hit, out PangyaUiGaugeState? gauge))
            {
                gauge = new();
                gauge.SetSteps(PangyaUiRenderer.GetExtendedGaugeStepCount(hit, RenderOptions()));
                _gaugeInputs[hit] = gauge;
            }
            if (gaugeDirection < 0) gauge.StepLeft(); else gauge.StepRight();
            Invalidate();
            return;
        }
        // FrTextButton selects on release and does not capture on press.
        if (hit.Type.Equals("TEXTBUTTON", StringComparison.OrdinalIgnoreCase)) return;
        int row = hit.Type.ToUpperInvariant() switch
        {
            "CONTEXTMENU" => _renderer.HitTestMenuRow(hit, logical, RenderOptions()),
            _ => -1
        };
        if (row >= 0)
        {
            PangyaUiMenuState state = _menuStates.GetValueOrDefault(hit) ?? new PangyaUiMenuState();
            if (hit.Type.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase) && state.SelectedRow == row) return;
            _menuStates[hit] = state with { SelectedRow = row, SelectionAlpha = GetMenuPulseAlpha() };
            if (!_menuPulseTimer.Enabled)
            {
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

    internal void CanvasTextButtonMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _document is null) return;
        Point logical = LogicalPoint(e.Location);
        PangyaUiRenderOptions options = RenderOptions();
        PangyaUiNode? hit = _renderer.HitTest(_document, _selectedForm, logical, options);
        if (hit is null || !hit.Type.Equals("TEXTBUTTON", StringComparison.OrdinalIgnoreCase)) return;
        // FrWnd::Process gates uncaptured events to the window's client rect.
        if (!_renderer.GetRenderedBounds(hit, options).Contains(logical)) return;
        int row = _renderer.HitTestTextButtonSegment(hit, logical, options);
        if (row < 0) return;
        PangyaUiMenuState state = _menuStates.GetValueOrDefault(hit) ?? new();
        if (state.SelectedRow == row) return;
        _menuStates[hit] = state with { SelectedRow = row, SelectionAlpha = GetMenuPulseAlpha() };
        if (!_menuPulseTimer.Enabled) _menuPulseTimer.Start();
        Invalidate();
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
        UpdateWheelPointer(logical);
        if (_comboThumbDrag is { } drag && Capture && e.Button == MouseButtons.Left &&
            _combos.TryGetValue(drag.Node, out PangyaUiComboPreview? draggingCombo) && draggingCombo.IsOpen)
        {
            int maximum = PangyaUiRenderer.GetComboMaximumTopRow(drag.Node, draggingCombo);
            float row = PangyaUiScrollState.MoveThumb(logical.Y, drag.Travel, maximum, ref drag.PreviousY, ref drag.Offset);
            _comboThumbDrag = drag;
            int topRow = Math.Min((int)(row + 0.5f), maximum);
            SetComboPreview(drag.Node, draggingCombo with
            {
                TopRow = topRow, HoveredIndex = -1, ScrollThumbRow = row, ScrollThumbOffset = drag.Offset
            });
            RefreshCursorPreview(e.Location);
            return;
        }
        if (_openCombo is PangyaUiNode open && _combos.TryGetValue(open, out PangyaUiComboPreview? combo))
        {
            int item = _renderer.HitTestComboRow(open, logical, RenderOptions());
            if (item != combo.HoveredIndex)
                SetComboPreview(open, combo with { HoveredIndex = item });
            if (_renderer.GetComboDropdownBounds(open, RenderOptions()).Contains(logical))
            {
                UpdateCaptionHover(null);
                UpdateTextControlHover(null, logical);
                Cursor = item >= 0 ? Cursors.Hand : Cursors.Default;
                RefreshCursorPreview(e.Location);
                return;
            }
        }
        if (_pressedButton is PangyaUiNode pressed)
        {
            bool inside = _combos.ContainsKey(pressed)
                ? _renderer.HitTestComboUnfold(pressed, logical, _enabledSymbols, RenderOptions())
                : GetRenderedBounds(pressed).Contains(logical);
            if (_buttonInputs.TryGetValue(pressed, out PangyaUiButtonInputState? input)) input.SetCursor(inside);
            if (_pressedButtonInside != inside)
            {
                _pressedButtonInside = inside;
                Invalidate();
            }
        }
        if (_draggedViewer is PangyaUiNode viewer && Capture && e.Button == MouseButtons.Left)
        {
            PangyaUiRenderOptions options = RenderOptions();
            RectangleF thumb = _renderer.GetViewerScrollThumbGeometry(viewer, options);
            Rectangle viewport = _renderer.GetViewerViewport(viewer, options);
            float travel = viewport.Height - thumb.Height;
            int maximum = _renderer.GetViewerMaximumTopRow(viewer, _enabledSymbols, options);
            if (!thumb.IsEmpty && travel > 0 && maximum > 0)
            {
                float row = PangyaUiScrollState.MoveThumb(logical.Y, travel, maximum,
                    ref _viewerDragPreviousY, ref _viewerThumbOffset);
                _viewerScrollRows[viewer] = row;
                _viewerFollowBottom[viewer] = row >= maximum;
                float boundedRow = Math.Min(row, maximum);
                int wholeRow = (int)boundedRow;
                _viewerTopRows[viewer] = boundedRow - wholeRow > 0.5f ? wholeRow + 1 : wholeRow;
                Invalidate();
            }
            RefreshCursorPreview(e.Location);
            return;
        }
        if (_dragStart is not Point start || _draggedNode is null || e.Button != MouseButtons.Left)
        {
            PangyaUiNode? hit = _document is null ? null :
                _renderer.HitTest(_document, SelectedForm, logical, RenderOptions());
            foreach (var pair in _buttonInputs)
                pair.Value.SetCursor(ReferenceEquals(pair.Key, hit));
            UpdateCaptionHover(hit is not null && PangyaUiRenderer.IsScrollingCaption(hit, RenderOptions()) ? hit : null);
            var hoveredTab = hit is null ? null : HitRenderedTab(hit, logical);
            if (_hoveredTab != hoveredTab)
            {
                _hoveredTab = hoveredTab;
                Invalidate();
            }
            UpdateTextControlHover(hit is not null &&
                (hit.Type.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("TEXTBUTTON", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("TABBUTTON", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("LISTBOX", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("GAUGEBAREX", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("COMBOBOX", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("COMBOCTLEX", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase) ||
                 hit.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase) ||
                 (hit.Type.Equals("VIEWER", StringComparison.OrdinalIgnoreCase) &&
                  hit.Parameters.GetValueOrDefault("use_wheel") == "true")) ? hit : null, logical);
            Cursor = hit is null ? Cursors.Default : hit.Type.ToUpperInvariant() switch
            {
                "FORM" when hit.IsEditable && CanStartDrag(hit, logical) => Cursors.SizeAll,
                "BUTTON" or "TEXTBUTTON" => Cursors.Hand,
                _ when HitRenderedTab(hit, logical) is not null => Cursors.Hand,
                "VIEWER" when ViewerAcceptsMouse(hit) => Cursors.Hand,
                "AREA" when PangyaUiRenderer.AreaAcceptsMouse(hit, RenderOptions()) => Cursors.Hand,
                "EDIT" => Cursors.IBeam,
                _ => Cursors.Default
            };
            RefreshCursorPreview(e.Location);
            return;
        }
        int dx = logical.X - start.X;
        int dy = logical.Y - start.Y;
        _draggedNode.SetBounds(new Rectangle(
            _dragStartBounds.X + dx, _dragStartBounds.Y + dy,
            _dragStartBounds.Width, _dragStartBounds.Height));
        ElementChanged?.Invoke(this, new PangyaUiNodeEventArgs(_draggedNode));
        RefreshCursorPreview(e.Location);
        Invalidate();
    }

    private void UpdateCaptionHover(PangyaUiNode? caption)
    {
        if (ReferenceEquals(_hoveredCaption, caption)) return;
        _hoveredCaption = caption;
        _captionLimit = caption is null ? 0 : _renderer.GetCaptionScrollLimit(caption, _enabledSymbols, RenderOptions());
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
            float next = current;
            if (current < _captionLimit)
            {
                next += distance;
                if (next >= _captionLimit + hovered.Bounds.Width) next = _captionLimit;
            }
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

    private byte GetMenuPulseAlpha()
    {
        double phase = ((Environment.TickCount64 - _menuPulseStart) % 3000) / 500.0;
        return (byte)(255 * Math.Min(1.0, Math.Min(phase, 6.0 - phase)));
    }

    private void UpdateMenuPulse()
    {
        byte alpha = GetMenuPulseAlpha();
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
    {
        if (_document is null) return [];
        PangyaUiRenderOptions options = RenderOptions();
        return _renderer.GetRenderOrder(_document, SelectedForm, options.SelectedNode,
            options.EnabledSymbols, options.TopmostNodes, options);
    }

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
        if (_buttonInputs.TryGetValue(_pressedButton, out PangyaUiButtonInputState? input) && input.Captured)
        {
            input.ButtonUp(false);
            input.SetCursor(false);
        }
        _pressedButton = null;
        _pressedButtonInside = false;
        Invalidate();
    }

    private void ExpireButtonPresses()
    {
        long now = Environment.TickCount64;
        bool changed = false;
        foreach (PangyaUiNode node in _buttonPressTicks.Keys.ToArray())
            if ((now - _buttonPressTicks[node]) / 1000.0 > PangyaUiRenderer.GetButtonPushDelay(node, RenderOptions()))
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

    private IReadOnlyDictionary<PangyaUiNode, PangyaUiImageButtonState> GetButtonInputSnapshots()
    {
        long now = Environment.TickCount64;
        double elapsed = Math.Max(0, now - _buttonInputTick) / 1000.0;
        _buttonInputTick = now;
        var snapshots = new Dictionary<PangyaUiNode, PangyaUiImageButtonState>();
        foreach (var pair in _buttonInputs)
        {
            PangyaUiButtonInputState input = pair.Value;
            input.AdvanceTime(elapsed);
            snapshots[pair.Key] = new(input.Enabled, input.SecondsSincePress)
            {
                Status = input.Status, PushStyle = input.PushStyle, PushDelay = input.PushDelay
            };
        }
        return snapshots;
    }

    private PangyaUiRenderOptions RenderOptions()
    {
        var options = new PangyaUiRenderOptions(
        ButtonState, _enabledSymbols, ShowDebugBounds, SelectedNode, 1f / Zoom)
        {
            ViewerTopRows = _viewerTopRows, ViewerScrollRows = _viewerScrollRows, StartupWallpaper = StartupWallpaper,
            ViewerFollowBottom = _viewerFollowBottom,
            DraggedViewerThumb = _draggedViewer is { } viewer ? (viewer, _viewerThumbOffset) : null,
            StartupWallpaperImage = _startupWallpaperImage,
            WheelEvents = _wheelEvents,
            Combos = _combos, SelectedTabInstances = _selectedTabs, MakeRoomMatchForms = _makeRoomMatchForms,
            EditLines = _editLines, EditCarets = GetEditCarets(), EditTopRows = _editTopRows,
            EditFollowBottom = _editFollowBottom,
            ComboViewportHeight = SelectedForm?.Bounds.Height ?? _document?.CanvasSize.Height ?? 0,
            IsViewFocused = _viewFocused,
            FrameBlinkElapsed = _frameBlinkElapsed,
            ButtonPressTicks = _buttonPressTicks.Where(pair => !_buttonInputs.ContainsKey(pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value), CurrentTick = Environment.TickCount64,
            ImageButtonStates = GetButtonInputSnapshots(),
            PressedButton = _pressedButtonInside ? _pressedButton : null,
            LatchedButtons = _latchedButtons,
            HoveredNode = _draggedViewer ?? _hoveredTextControl, Pointer = _textControlPointer, MenuStates = _menuStates,
            ShowTooltip = (_showTooltip || _fadingTooltip is not null) && _viewFocused && !Capture,
            CursorIndex = FreshCursorIndex,
            TooltipAlpha = GetTooltipAlpha(),
            CaptionFontHeight = CaptionFont?.Height,
            MeasureCaptionText = CaptionFont is { } captionFont ? captionFont.Measure : null,
            DrawCaptionText = CaptionFont is { } captionBitmapFont ? captionBitmapFont.Draw : null,
            LanguageTable = LanguageTable,
            TooltipFontHeight = TooltipFont?.Height,
            MeasureTooltipText = TooltipFont is { } font ? font.Measure : null,
            DrawTooltipText = TooltipFont is { } bitmapFont ? bitmapFont.Draw : null,
            TooltipNode = _fadingTooltip, TooltipPointer = _fadingTooltipPointer,
            CaptionScrollOffsets = _captionOffsets,
            EditEmoticonAnimationTime = _layoutAnimationTimer.Enabled
                ? TimeSpan.FromMilliseconds(Environment.TickCount64 - _layoutAnimationStart) : null,
            LayoutAnimationTime = _layoutAnimationTimer.Enabled
                ? TimeSpan.FromMilliseconds(Environment.TickCount64 - _layoutAnimationStart) : TimeSpan.Zero
        };
        foreach (var pair in _gaugeInputs) options = pair.Value.ApplyTo(pair.Key, options);
        return options;
    }
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
            bool signedRectangle = _selectedNode?.Element.Attributes.OfType<XmlAttribute>().Any(attribute =>
                attribute.LocalName.Equals("rect", StringComparison.OrdinalIgnoreCase) ||
                attribute.LocalName.Equals("bounds", StringComparison.OrdinalIgnoreCase)) == true;
            _width.Minimum = signedRectangle ? Math.Min(-8192, bounds.Width) : 0;
            _height.Minimum = signedRectangle ? Math.Min(-8192, bounds.Height) : 0;
            _width.Maximum = Math.Max(8192, bounds.Width);
            _height.Maximum = Math.Max(8192, bounds.Height);
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
