using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;
using PangYa_Suite_Tools.Configuration;
using PangyaAPI.IFF;
using PangyaAPI.PAK.Models;
using PangyaAPI.Utilities.Cryptography;
using System.Text;

namespace PangYa_Suite_Tools;

public partial class FrmIFFManager : Form
{
    private sealed record RegionOption(string Label, string? Region, string? DetectedRegion = null);
    private sealed record ContainerKeyOption(string Label, IffContainerSaveOptions SaveOptions);
    private const string LogSource = "IFF Editor";
    private bool _rebuildingEntryList;
    private string? _directoryPath;
    private IffContainer? _container;
    private IffContainerEntry? _entry;
    private IffDocumentInfo? _document;
    private readonly List<IffRecord> _records = [];
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _extractCancellation;
    private bool _isSaving;
    private bool _isExtracting;
    private bool _initializingEncodings = true;
    private PakEncodingOption? _selectedStringEncodingOption;
    private bool _initializingRegions = true;
    private Encoding _documentStringEncoding = IffStringEncodingPreferences.GetEncoding(
        IffStringEncodingPreferences.DefaultCodePage);
    private bool _structureDirty;
    private readonly List<IffField> _visibleFields = [];
    private readonly DirectoryIffSchemaProvider _schemaProvider;
    private readonly IffSchemaDefaultManager _schemaDefaultManager;
    private bool _schemasSeeded;
    private bool _schemaUpdatesChecked;
    private bool _schemaUpdatesAvailable = true;
    private bool _initializingContainerKeys = true;
    private bool _containerEncodingDirty;
    private IffContainerSaveOptions? _selectedSaveOptions;
    private ContainerKeyOption? _selectedContainerKeyOption;
    private RegionOption? _selectedRegionOption;
    private IffFormRecordEditor? _formEditor;
    private string? _dataRootOverride;
    private bool _showFormView = true;
    private ToolStrip? _editorToolbar;
    private ToolStripButton? _toolbarOpenArchive;
    private ToolStripButton? _toolbarExtract;
    private ToolStripButton? _toolbarExtractAll;
    private ToolStripButton? _toolbarSave;
    private ToolStripButton? _toolbarPatch;
    private ToolStripButton? _toolbarAddRow;
    private ToolStripButton? _toolbarDeleteRows;
    private ToolStripButton? _toolbarManageSchema;
    private ToolStripButton? _toolbarSchemaUpdates;
    private ToolStripButton? _toolbarRawRecord;
    private ToolStripButton? _toolbarFormView;
    private ToolStripButton? _toolbarGridView;
    private ToolStripDropDownButton? _toolbarContainerKey;
    private ToolStripDropDownButton? _toolbarRegion;
    private ToolStripDropDownButton? _toolbarStringEncoding;

    private IReadOnlyList<IffField> VisibleFields => _visibleFields;

    private bool CanEditDocument => _document?.Schema?.IsEditable == true;
    private bool CanSaveDocument => CanEditDocument || _containerEncodingDirty;

    private Encoding SelectedStringEncoding => _selectedStringEncodingOption is { } option
        ? IffStringEncodingPreferences.GetEncoding(option.CodePage)
        : IffStringEncodingPreferences.GetEncoding(IffStringEncodingPreferences.DefaultCodePage);

    private string? SelectedSchemaRegion => _selectedRegionOption?.Region;
    private string? SelectedDetectedRegion => _selectedRegionOption?.DetectedRegion;

    public FrmIFFManager()
    {
        InitializeComponent();
        txtIffDirectory.Text = PathTextBoxPreferences.LoadPath(PathTextBoxKind.IffArchiveOrFolder);
        _dataRootOverride = PathTextBoxPreferences.LoadPath(PathTextBoxKind.IffDataRoot);
        _schemaProvider = IffSchemaPreferences.CreateProvider();
        _schemaDefaultManager = new IffSchemaDefaultManager(_schemaProvider);
        ConfigureGrid();
        ConfigureEditorToolbar();
        RefreshContainerKeyComboBox();
        InitializeEncodingComboBox();
        InitializeRegionComboBox();
        ApplyLocalization();
        LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
        FormClosing += FrmIFFManager_FormClosing;
        AppLogger.Instance.Log(LogSource, "IFF editor opened.");
        Disposed += (_, _) =>
        {
            PathTextBoxPreferences.SavePaths(new Dictionary<PathTextBoxKind, string?>
            {
                [PathTextBoxKind.IffArchiveOrFolder] = txtIffDirectory.Text,
                [PathTextBoxKind.IffDataRoot] = _dataRootOverride
            });
            AppLogger.Instance.Log(LogSource, "IFF editor closed.");
            LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
            CancelActiveOperation(ref _loadCancellation);
            CancelActiveOperation(ref _extractCancellation);
            _container?.Dispose();
        };
    }

    public FrmIFFManager(string idiomaAtual) : this() => LocalizationManager.SetCulture(idiomaAtual);

    private static void CancelActiveOperation(ref CancellationTokenSource? activeCancellation)
    {
        CancellationTokenSource? cancellation = Interlocked.Exchange(ref activeCancellation, null);
        cancellation?.Cancel();
    }

    private void ConfigureGrid()
    {
        gridRecords.VirtualMode = true;
        gridRecords.CellValueNeeded += GridRecords_CellValueNeeded;
        gridRecords.CellValuePushed += GridRecords_CellValuePushed;
        gridRecords.CellPainting += GridRecords_CellPainting;
        gridRecords.MouseWheel += GridRecords_MouseWheel;
        gridRecords.DataError += (_, e) =>
        {
            e.ThrowException = false;
            string message = e.Exception?.Message ?? Strings.IFFManager_InvalidValue;
            AppLogger.Instance.Log(LogSource, $"Invalid grid value: {message}", AppLogLevel.Warning);
            MessageBox.Show(message, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        gridRecords.SelectionChanged += (_, _) =>
        {
            btnDeleteRows.Enabled = CanEditDocument && gridRecords.SelectedRows.Count > 0;
            UpdateToolbarState();
        };
    }

    private void RefreshContainerKeyComboBox(bool preserveSelection = false)
    {
        IffContainerSaveOptions? priorSelection = _selectedSaveOptions;
        bool priorDirty = _containerEncodingDirty;
        _initializingContainerKeys = true;
        var options = new List<ContainerKeyOption>();
        if (_container?.Kind == IffContainerKind.LooseFile || _container is null)
        {
            options.Add(new ContainerKeyOption(Strings.IFFManager_KeyNone,
                new IffContainerSaveOptions(IffContainerKind.LooseFile)));
        }
        else
        {
            options.Add(new ContainerKeyOption(Strings.IFFManager_KeyPlainZip,
                new IffContainerSaveOptions(IffContainerKind.ZipArchive)));
            foreach ((string label, _) in PakKeys.All)
                options.Add(new ContainerKeyOption(label,
                    new IffContainerSaveOptions(IffContainerKind.EncryptedZipArchive, label)));
        }
        ContainerKeyOption? selected = preserveSelection
            ? options.FirstOrDefault(option => option.SaveOptions == priorSelection)
            : options.FirstOrDefault(option =>
                _container?.Kind == option.SaveOptions.Kind &&
                (_container.Kind != IffContainerKind.EncryptedZipArchive ||
                 _container.EncryptionRegion == option.SaveOptions.EncryptionRegion));
        _selectedContainerKeyOption = selected ?? options[0];
        _selectedSaveOptions = _selectedContainerKeyOption.SaveOptions;
        _containerEncodingDirty = preserveSelection && priorDirty;
        RebuildContainerKeyMenu(options);
        _initializingContainerKeys = false;
    }

    private void RebuildContainerKeyMenu(IReadOnlyList<ContainerKeyOption> options)
    {
        if (_toolbarContainerKey is null) return;
        _toolbarContainerKey.DropDownItems.Clear();
        foreach (ContainerKeyOption option in options)
        {
            var item = new ToolStripMenuItem(option.Label)
            {
                Checked = option == _selectedContainerKeyOption,
                Tag = option
            };
            item.Click += (_, _) => SelectContainerKey(option);
            _toolbarContainerKey.DropDownItems.Add(item);
        }
        _toolbarContainerKey.Enabled = _container?.Kind != IffContainerKind.LooseFile && _container is not null;
        UpdateSelectorToolbarText();
    }

    private void SelectContainerKey(ContainerKeyOption option)
    {
        if (_initializingContainerKeys || _container is null) return;
        _selectedContainerKeyOption = option;
        _selectedSaveOptions = option.SaveOptions;
        _containerEncodingDirty = _container.Kind != option.SaveOptions.Kind ||
            _container.Kind == IffContainerKind.EncryptedZipArchive &&
            _container.EncryptionRegion != option.SaveOptions.EncryptionRegion;
        foreach (ToolStripMenuItem item in _toolbarContainerKey!.DropDownItems.OfType<ToolStripMenuItem>())
            item.Checked = ReferenceEquals(item.Tag, option);
        UpdateSelectorToolbarText();
        UpdateDirtyState();
    }

    private void InitializeEncodingComboBox()
    {
        int savedCodePage = IffStringEncodingPreferences.LoadCodePage();
        RefreshStringEncodingMenu(savedCodePage);
        _initializingEncodings = false;
    }

    private void RefreshStringEncodingMenu(int? selectedCodePage = null)
    {
        if (_toolbarStringEncoding is null) return;
        int codePage = selectedCodePage ?? _selectedStringEncodingOption?.CodePage ??
            IffStringEncodingPreferences.LoadCodePage();
        IReadOnlyList<PakEncodingOption> encodings = IffStringEncodingPreferences.GetAvailableEncodings();
        _selectedStringEncodingOption = encodings.FirstOrDefault(option => option.CodePage == codePage) ??
            encodings.First(option => option.CodePage == IffStringEncodingPreferences.DefaultCodePage);
        _toolbarStringEncoding.DropDownItems.Clear();
        foreach (PakEncodingOption option in encodings)
        {
            var item = new ToolStripMenuItem(option.Label)
            {
                Checked = option.CodePage == _selectedStringEncodingOption.CodePage,
                Tag = option
            };
            item.Click += (_, _) => SelectStringEncoding(option);
            _toolbarStringEncoding.DropDownItems.Add(item);
        }
        UpdateSelectorToolbarText();
    }

    private void InitializeRegionComboBox() => RefreshRegionComboBox(null, null);

    private void RefreshRegionComboBox(string? selectedRegion, string? detectedRegion = null)
    {
        if (string.IsNullOrWhiteSpace(detectedRegion) ||
            string.Equals(detectedRegion, "Unknown", StringComparison.OrdinalIgnoreCase))
            detectedRegion = null;
        _initializingRegions = true;
        var options = new List<RegionOption>
        {
            new(Strings.IFFManager_RegionAuto, null),
            new(Strings.IFFManager_RegionThailand, "TH"),
            new(Strings.IFFManager_RegionJapan, "JP"),
            new(Strings.IFFManager_RegionJapan983, IffHeader.Japan983Region),
            new(Strings.IFFManager_RegionPsp, IffHeader.PspRegion),
            new(Strings.IFFManager_RegionGlobal, "Global")
        };
        if (detectedRegion is not null)
            options.Add(new RegionOption(detectedRegion, null, detectedRegion));
        _selectedRegionOption = options
            .First(option => detectedRegion is not null
                ? string.Equals(option.DetectedRegion, detectedRegion, StringComparison.OrdinalIgnoreCase)
                : string.Equals(option.Region, selectedRegion, StringComparison.OrdinalIgnoreCase));
        RebuildRegionMenu(options);
        _initializingRegions = false;
    }

    private void RebuildRegionMenu(IReadOnlyList<RegionOption> options)
    {
        if (_toolbarRegion is null) return;
        _toolbarRegion.DropDownItems.Clear();
        foreach (RegionOption option in options)
        {
            var item = new ToolStripMenuItem(option.Label)
            {
                Checked = option == _selectedRegionOption,
                Tag = option
            };
            item.Click += (_, _) => SelectRegion(option);
            _toolbarRegion.DropDownItems.Add(item);
        }
        UpdateSelectorToolbarText();
    }

    private void SelectRegion(RegionOption option)
    {
        if (_initializingRegions) return;
        _selectedRegionOption = option;
        foreach (ToolStripMenuItem item in _toolbarRegion!.DropDownItems.OfType<ToolStripMenuItem>())
            item.Checked = ReferenceEquals(item.Tag, option);
        UpdateSelectorToolbarText();
        if (_document is not null) lblStatus.Text = Strings.IFFManager_RegionAppliesNextLoad;
    }

    private void SelectStringEncoding(PakEncodingOption option)
    {
        if (_initializingEncodings) return;
        _selectedStringEncodingOption = option;
        IffStringEncodingPreferences.SaveCodePage(option.CodePage);
        foreach (ToolStripMenuItem item in _toolbarStringEncoding!.DropDownItems.OfType<ToolStripMenuItem>())
            item.Checked = item.Tag is PakEncodingOption candidate && candidate.CodePage == option.CodePage;
        UpdateSelectorToolbarText();
        AppLogger.Instance.Log(LogSource, $"String encoding changed to '{option.Label}'.");
        if (_document is not null) lblStatus.Text = Strings.IFFManager_EncodingAppliesNextLoad;
    }

    private void LocalizationManager_CultureChanged(object? sender, EventArgs e)
    {
        if (IsDisposed || Disposing) return;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        string? selectedRegion = SelectedSchemaRegion;
        string? detectedRegion = SelectedDetectedRegion;
        Text = Strings.Iff_Title;
        lblIffDir.Text = Strings.Iff_Directory;
        btnBrowseIffDir.Text = Strings.Iff_Browse;
        btnOpenArchive.Text = Strings.IFFManager_OpenArchive;
        btnSave.Text = Strings.IFFManager_Save;
        btnAddRow.Text = Strings.IFFManager_AddRow;
        btnDeleteRows.Text = Strings.IFFManager_DeleteRows;
        btnAddColumn.Text = Strings.IFFManager_ManageColumns;
        UpdateToolbarText();
        grpIffFiles.Text = Strings.Iff_Files;
        RefreshRegionComboBox(selectedRegion, detectedRegion);
        RefreshContainerKeyComboBox(preserveSelection: true);
        RefreshStringEncodingMenu();
        UpdateSchemaCoverageLabel();
        if (_document is null) lblStatus.Text = Strings.IFFManager_ReadySelectTheIFFFilesDirectory;
        UpdateDirtyState();
    }

    private void ConfigureEditorToolbar()
    {
        btnOpenArchive.Visible = false;
        btnSave.Visible = false;
        btnAddRow.Visible = false;
        btnDeleteRows.Visible = false;
        btnAddColumn.Visible = false;

        _editorToolbar = new ToolStrip
        {
            Name = "iffEditorToolbar",
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(24, 24),
            AutoSize = false,
            Height = 44,
            Location = new Point(8, 47),
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
        };
        _editorToolbar.Width = pnlTopBar.ClientSize.Width - 16;

        _toolbarOpenArchive = CreateToolbarButton(Strings.IFFManager_OpenArchive, SystemIcons.Application.ToBitmap(),
            (_, _) => btnOpenArchive_Click(btnOpenArchive, EventArgs.Empty));
        _toolbarExtract = CreateToolbarButton(Strings.IFFManager_Extract, SystemIcons.WinLogo.ToBitmap(),
            async (_, _) => await ExtractCurrentIffAsync());
        _toolbarExtractAll = CreateToolbarButton(Strings.IFFManager_ExtractAll, SystemIcons.WinLogo.ToBitmap(),
            async (_, _) => await ExtractAllIffsAsync());
        _toolbarSave = CreateToolbarButton(Strings.IFFManager_Save, SystemIcons.Shield.ToBitmap(),
            (_, _) => btnSave_Click(btnSave, EventArgs.Empty));
        _toolbarPatch = CreateToolbarButton(Strings.IFFManager_Patch, SystemIcons.Information.ToBitmap(),
            async (_, _) => await PatchCurrentIffAsync());
        _toolbarAddRow = CreateToolbarButton(Strings.IFFManager_AddRow, SystemIcons.Information.ToBitmap(),
            (_, _) => btnAddRow_Click(btnAddRow, EventArgs.Empty));
        _toolbarDeleteRows = CreateToolbarButton(Strings.IFFManager_DeleteRows, SystemIcons.Error.ToBitmap(),
            (_, _) =>
            {
                if (_showFormView) DeleteSelectedFormRecord();
                else btnDeleteRows_Click(btnDeleteRows, EventArgs.Empty);
            });
        _toolbarManageSchema = CreateToolbarButton(Strings.IFFManager_ManageColumns, SystemIcons.WinLogo.ToBitmap(),
            (_, _) => btnAddColumn_Click(btnAddColumn, EventArgs.Empty));
        _toolbarSchemaUpdates = CreateToolbarButton(Strings.IFFManager_SchemaUpdates, CreateSchemaUpdateIcon(),
            async (_, _) => await CheckForSchemaUpdatesAsync(showWhenNone: true, CancellationToken.None));
        _toolbarRawRecord = CreateToolbarButton(Strings.IFFManager_RawRecord, SystemIcons.Application.ToBitmap(),
            async (_, _) => await OpenRawRecordWindowAsync());
        _toolbarFormView = CreateToolbarButton(Strings.IFFManager_FormView, SystemIcons.Question.ToBitmap(),
            (_, _) => SetEditorView(showFormView: true));
        _toolbarGridView = CreateToolbarButton(Strings.IFFManager_GridView, SystemIcons.Asterisk.ToBitmap(),
            (_, _) => SetEditorView(showFormView: false));
        _toolbarContainerKey = CreateToolbarSelector("toolbarIffKey");
        _toolbarRegion = CreateToolbarSelector("toolbarIffRegion");
        _toolbarStringEncoding = CreateToolbarSelector("toolbarIffStringEncoding", 180);
        _toolbarFormView.CheckOnClick = true;
        _toolbarGridView.CheckOnClick = true;

        _editorToolbar.Items.AddRange([
            _toolbarOpenArchive,
            _toolbarContainerKey,
            _toolbarRegion,
            _toolbarStringEncoding,
            new ToolStripSeparator(),
            _toolbarExtract,
            _toolbarExtractAll,
            _toolbarSave,
            _toolbarPatch,
            new ToolStripSeparator(),
            _toolbarAddRow,
            _toolbarDeleteRows,
            _toolbarManageSchema,
            _toolbarSchemaUpdates,
            _toolbarRawRecord,
            new ToolStripSeparator(),
            _toolbarFormView,
            _toolbarGridView
        ]);
        pnlTopBar.Controls.Add(_editorToolbar);
        pnlTopBar.Resize += (_, _) =>
        {
            if (_editorToolbar is not null) _editorToolbar.Width = pnlTopBar.ClientSize.Width - 16;
        };
        UpdateToolbarState();
    }

    private static ToolStripButton CreateToolbarButton(string text, Image image, EventHandler handler)
    {
        var button = new ToolStripButton(text, image)
        {
            DisplayStyle = ToolStripItemDisplayStyle.ImageAndText,
            TextImageRelation = TextImageRelation.ImageAboveText,
            AutoSize = false,
            Width = 88,
            Height = 42
        };
        button.Click += handler;
        return button;
    }

    private static ToolStripDropDownButton CreateToolbarSelector(string name, int width = 132) => new()
    {
        Name = name,
        DisplayStyle = ToolStripItemDisplayStyle.Text,
        AutoSize = false,
        Width = width,
        Height = 42,
        TextAlign = ContentAlignment.MiddleCenter
    };

    private static Bitmap CreateSchemaUpdateIcon()
    {
        var bitmap = new Bitmap(32, 32);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        using var schemaBrush = new SolidBrush(Color.FromArgb(0, 122, 204));
        using var updateBrush = new SolidBrush(Color.FromArgb(40, 167, 69));
        using var whitePen = new Pen(Color.White, 2F);
        using var updatePen = new Pen(updateBrush, 3F)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round
        };

        graphics.FillRectangle(schemaBrush, 3, 4, 19, 24);
        graphics.DrawRectangle(whitePen, 7, 8, 11, 4);
        graphics.DrawLine(whitePen, 7, 17, 18, 17);
        graphics.DrawLine(whitePen, 7, 22, 15, 22);

        graphics.DrawArc(updatePen, 13, 13, 16, 16, 205, 250);
        graphics.FillPolygon(updateBrush,
            [new Point(26, 11), new Point(30, 18), new Point(22, 18)]);
        return bitmap;
    }

    private void UpdateToolbarText()
    {
        if (_toolbarOpenArchive is null) return;
        _toolbarOpenArchive.Text = Strings.IFFManager_OpenArchive;
        _toolbarExtract!.Text = Strings.IFFManager_Extract;
        _toolbarExtractAll!.Text = Strings.IFFManager_ExtractAll;
        _toolbarSave!.Text = Strings.IFFManager_Save;
        _toolbarPatch!.Text = Strings.IFFManager_Patch;
        _toolbarAddRow!.Text = Strings.IFFManager_AddRow;
        _toolbarDeleteRows!.Text = Strings.IFFManager_DeleteRows;
        _toolbarManageSchema!.Text = Strings.IFFManager_ManageColumns;
        _toolbarSchemaUpdates!.Text = Strings.IFFManager_SchemaUpdates;
        _toolbarRawRecord!.Text = Strings.IFFManager_RawRecord;
        _toolbarFormView!.Text = Strings.IFFManager_FormView;
        _toolbarGridView!.Text = Strings.IFFManager_GridView;
        UpdateSelectorToolbarText();
    }

    private void UpdateSelectorToolbarText()
    {
        if (_toolbarContainerKey is not null)
        {
            string value = _selectedContainerKeyOption?.Label ?? Strings.IFFManager_KeyNone;
            _toolbarContainerKey.Text = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_ToolbarKeyFormat, value);
        }
        if (_toolbarRegion is not null)
        {
            string value = _selectedRegionOption?.Label ?? Strings.IFFManager_RegionAuto;
            _toolbarRegion.Text = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_ToolbarRegionFormat, value);
        }
        if (_toolbarStringEncoding is not null)
        {
            string value = _selectedStringEncodingOption?.Label ??
                IffStringEncodingPreferences.GetAvailableEncodings()
                    .First(option => option.CodePage == IffStringEncodingPreferences.DefaultCodePage).Label;
            _toolbarStringEncoding.Text = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_ToolbarStringEncodingFormat, value);
        }
    }

    private void btnBrowseIffDir_Click(object sender, EventArgs e)
    {
        AppLogger.Instance.Log(LogSource, "Browse directory button clicked.");
        using var dialog = new FolderBrowserDialog { Description = Strings.IFFManager_SelectTheExtractedFolderContainingThe };
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            AppLogger.Instance.Log(LogSource, "Directory selection was cancelled.", AppLogLevel.Warning);
            return;
        }

        if (!ConfirmDiscard())
        {
            AppLogger.Instance.Log(LogSource, "Directory change was cancelled to keep unsaved changes.", AppLogLevel.Warning);
            return;
        }
        _directoryPath = dialog.SelectedPath;
        txtIffDirectory.Text = dialog.SelectedPath;
        AppLogger.Instance.Log(LogSource, $"Scanning IFF directory: {dialog.SelectedPath}");
        LoadIffFiles(dialog.SelectedPath);
    }

    private async void btnOpenArchive_Click(object sender, EventArgs e)
    {
        AppLogger.Instance.Log(LogSource, "Open archive button clicked.");
        using var dialog = FileDialogFactory.CreateIffOpenDialog();
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            AppLogger.Instance.Log(LogSource, "Archive selection was cancelled.", AppLogLevel.Warning);
            return;
        }
        FileDialogFactory.RememberDirectory(FileDialogKind.Iff, dialog.FileName);

        if (!ConfirmDiscard())
        {
            AppLogger.Instance.Log(LogSource, "Opening the archive was cancelled to keep unsaved changes.", AppLogLevel.Warning);
            return;
        }
        try
        {
            UseWaitCursor = true;
            AppLogger.Instance.Log(LogSource, $"Opening IFF archive: {dialog.FileName}");
            await ReplaceContainerAsync(await IffContainer.OpenAsync(dialog.FileName));
            _directoryPath = null;
            txtIffDirectory.Text = dialog.FileName;
            FillEntryList(_container!.Entries);
            AppLogger.Instance.Log(LogSource,
                $"Opened archive '{dialog.FileName}' with {_container.Entries.Count} entries.");
        }
        catch (Exception ex) { ShowError(ex); }
        finally { UseWaitCursor = false; }
    }

    private void LoadIffFiles(string directoryPath)
    {
        _rebuildingEntryList = true;
        lstIffFiles.BeginUpdate();
        try
        {
            ClearDocument();
            lstIffFiles.Items.Clear();
            foreach (string file in Directory.EnumerateFiles(directoryPath, "*.iff", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName))
                lstIffFiles.Items.Add(Path.GetFileName(file));
            lblStatus.Text = $"{Strings.IFFManager_ScanComplete} {lstIffFiles.Items.Count} {Strings.IFFManager_IffFileSFound}";
            AppLogger.Instance.Log(LogSource,
                $"Found {lstIffFiles.Items.Count} IFF files in '{directoryPath}'.");
        }
        catch (Exception ex) { ShowError(ex); }
        finally
        {
            lstIffFiles.EndUpdate();
            _rebuildingEntryList = false;
        }
    }

    private void FillEntryList(IEnumerable<IffContainerEntry> entries)
    {
        _rebuildingEntryList = true;
        lstIffFiles.BeginUpdate();
        try
        {
            ClearDocument();
            lstIffFiles.Items.Clear();
            foreach (IffContainerEntry entry in entries.OrderBy(item => item.Name)) lstIffFiles.Items.Add(entry.Name);
            lblStatus.Text = $"{Strings.IFFManager_ScanComplete} {lstIffFiles.Items.Count} {Strings.IFFManager_IffFileSFound}";
            AppLogger.Instance.Log(LogSource, $"Displayed {lstIffFiles.Items.Count} archive entries.");
        }
        finally
        {
            lstIffFiles.EndUpdate();
            _rebuildingEntryList = false;
        }
    }

    private async void lstIffFiles_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_rebuildingEntryList || lstIffFiles.SelectedItem is not string name) return;
        AppLogger.Instance.Log(LogSource, $"IFF entry '{name}' selected.");
        if (!ConfirmDiscard())
        {
            AppLogger.Instance.Log(LogSource, $"Loading '{name}' was cancelled to keep unsaved changes.", AppLogLevel.Warning);
            return;
        }
        using var loadCancellation = new CancellationTokenSource();
        CancellationToken loadToken = loadCancellation.Token;
        CancellationTokenSource? previousCancellation =
            Interlocked.Exchange(ref _loadCancellation, loadCancellation);
        previousCancellation?.Cancel();
        try
        {
            UseWaitCursor = true;
            if (_directoryPath is not null)
                await ReplaceContainerAsync(await IffContainer.OpenAsync(
                    Path.Combine(_directoryPath, name), cancellationToken: loadToken));
            IffContainerEntry entry = _container!.Entries.Single(item =>
                item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            await LoadEntryAsync(entry, loadToken);
        }
        catch (OperationCanceledException)
        {
            AppLogger.Instance.Log(LogSource, $"Loading '{name}' was cancelled.", AppLogLevel.Warning);
        }
        catch (Exception ex) { ShowError(ex); }
        finally
        {
            Interlocked.CompareExchange(ref _loadCancellation, null, loadCancellation);
            UseWaitCursor = false;
        }
    }

    private async Task EnsureSchemasSeededAsync(CancellationToken token)
    {
        if (_schemasSeeded) return;
        try
        {
            await Task.Run(IffSchemaPreferences.SeedDefaults, token);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            AppLogger.Instance.Log(LogSource,
                $"Could not seed default JSON schemas: {ex.Message}", AppLogLevel.Warning);
        }
        _schemasSeeded = true;
    }

    private async Task CheckForSchemaUpdatesAsync(bool showWhenNone, CancellationToken token)
    {
        if (!BeginSchemaUpdateCheck(showWhenNone)) return;
        await EnsureSchemasSeededAsync(token);
        try
        {
            IReadOnlyList<IffSchemaUpdateCandidate> candidates = await Task.Run(
                _schemaDefaultManager.FindUpdates, token);
            if (candidates.Count == 0)
            {
                SetSchemaUpdatesAvailable(false);
                if (showWhenNone)
                    MessageBox.Show(Strings.IFFManager_SchemaUpdateNoneAvailable,
                        Strings.IFFManager_SchemaUpdates, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetSchemaUpdatesAvailable(true);
            using var dialog = new IffSchemaUpdateDialog(candidates);
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            IffSchemaUpdateSelection[] selections = dialog.Selections.ToArray();
            IffSchemaUpdateResult result = await Task.Run(
                () => _schemaDefaultManager.ApplyUpdates(selections), token);
            foreach (IffSchemaUpdateSelection selection in selections)
                AppLogger.Instance.Log(LogSource,
                    $"Schema update '{selection.Candidate.FileName}' ({selection.Candidate.Region}): {selection.Action}.");
            if (result.BackupDirectory is not null)
                AppLogger.Instance.Log(LogSource, $"Schema update backups saved to '{result.BackupDirectory}'.");

            if (_document is not null && result.ReplacedCount + result.PreferredLocalCount > 0)
                await ReloadResolvedSchemaAsync(CurrentSchemaRegions());
            string summary = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_SchemaUpdateAppliedFormat, result.ReplacedCount,
                result.PreferredLocalCount, result.BackupDirectory ?? "-");
            AppLogger.Instance.Log(LogSource, summary);
            if (_document?.SchemaWarning is null) lblStatus.Text = summary;
            IReadOnlyList<IffSchemaUpdateCandidate> remainingCandidates = await Task.Run(
                _schemaDefaultManager.FindUpdates, token);
            SetSchemaUpdatesAvailable(remainingCandidates.Count > 0);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException
                                   or ArgumentException or AggregateException)
        {
            AppLogger.Instance.Log(LogSource, $"Schema update failed: {ex.Message}", AppLogLevel.Error);
            ShowError(ex);
        }
    }

    internal bool BeginSchemaUpdateCheck(bool manualRequest)
    {
        if (manualRequest) return true;
        if (_schemaUpdatesChecked) return false;
        _schemaUpdatesChecked = true;
        return true;
    }

    internal void SetSchemaUpdatesAvailable(bool available)
    {
        _schemaUpdatesAvailable = available;
        if (_toolbarSchemaUpdates is not null) _toolbarSchemaUpdates.Enabled = available;
    }

    private async Task LoadEntryAsync(IffContainerEntry entry, CancellationToken token)
    {
        string? selectedRegion = SelectedSchemaRegion;
        Encoding selectedEncoding = SelectedStringEncoding;
        await EnsureSchemasSeededAsync(token);
        await CheckForSchemaUpdatesAsync(showWhenNone: false, token);
        await using Stream stream = await entry.OpenAsync(token);
        (IffDocumentInfo document, List<IffRecord> records, string? resolvedSelection) =
            await ReadDocumentAsync(stream, Path.GetFileName(entry.Name), selectedRegion,
                _container?.FileNameRegion, token);

        ClearDocument();
        _entry = entry;
        _document = document;
        _records.AddRange(records);
        _documentStringEncoding = selectedEncoding;
        selectedRegion = resolvedSelection;
        RefreshRegionComboBox(selectedRegion, selectedRegion is null ? _document.Region : null);
        if (!string.IsNullOrEmpty(_document.SchemaWarning))
        {
            AppLogger.Instance.Log(LogSource, _document.SchemaWarning, AppLogLevel.Warning);
            MessageBox.Show($"{Strings.IFFManager_SchemaWarning}\n{_document.SchemaWarning}",
                Strings.IFFManager_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        await RefreshSchemaViewAsync(token);
        lblNoFileSelected.Visible = false;
        SetEditorView(_showFormView);
        btnSave.Enabled = _document.Schema?.IsEditable == true;
        btnAddRow.Enabled = _document.Schema?.IsEditable == true;
        btnAddColumn.Enabled = true;
        UpdateToolbarState();
        lblStatus.Text = $"{Strings.IFFManager_EditingStructureOf} {entry.Name} — {_document.Region}, {_records.Count} records, {_document.RecordSize} bytes";
        AppLogger.Instance.Log(LogSource,
            $"Loaded '{entry.Name}' using {_documentStringEncoding.EncodingName}: region {_document.Region}, {_records.Count} records, {_document.RecordSize} bytes per record.");
    }

    private async Task<(IffDocumentInfo Document, List<IffRecord> Records, string? SelectedRegion)> ReadDocumentAsync(
        Stream stream,
        string fileName,
        string? selectedRegion,
        string? fallbackRegion,
        CancellationToken token)
    {
        await using (IffReader probe = await Task.Run(() => IffReader.Open(stream, fileName,
            new(LeaveOpen: true, SchemaProvider: _schemaProvider, SchemaRegion: selectedRegion,
                FallbackSchemaRegion: fallbackRegion)), token))
        {
            if (RequiresRegionSelection(probe.Info))
            {
                using var dialog = new IffUnknownRegionDialog(probe.Info);
                if (dialog.ShowDialog(this) != DialogResult.OK) throw new OperationCanceledException(token);
                selectedRegion = dialog.SelectedRegion;
            }
        }

        await using IffReader reader = await Task.Run(() => IffReader.Open(stream, fileName,
            new(LeaveOpen: true, SchemaProvider: _schemaProvider, SchemaRegion: selectedRegion,
                FallbackSchemaRegion: fallbackRegion)), token);
        var records = new List<IffRecord>(reader.Info.Header.RecordCount);
        await foreach (IffRecord record in reader.ReadRecordsAsync(token)) records.Add(record);
        return (reader.Info, records, selectedRegion);
    }

    internal static bool RequiresRegionSelection(IffDocumentInfo document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.Region.Equals("Unknown", StringComparison.OrdinalIgnoreCase);
    }

    private void EnsureFormEditor()
    {
        if (_formEditor is not null) return;
        _formEditor = new IffFormRecordEditor { Visible = false };
        _formEditor.AddRequested += (_, _) => btnAddRow_Click(btnAddRow, EventArgs.Empty);
        _formEditor.DeleteRequested += (_, _) => DeleteSelectedFormRecord();
        _formEditor.CopyRequested += (_, _) => CopySelectedFormRecord();
        _formEditor.SaveRequested += (_, _) => btnSave_Click(btnSave, EventArgs.Empty);
        _formEditor.DataRootChangeRequested += path => _ = ChangeDataRootAsync(path);
        _formEditor.PendingChangesChanged += (_, _) => UpdateDirtyState();
        _formEditor.Applied += (_, _) =>
        {
            gridRecords.Invalidate();
            UpdateDirtyState();
        };
        pnlEditorContainer.Controls.Add(_formEditor);
        _formEditor.BringToFront();
    }

    private void LoadFormEditor()
    {
        if (_document is null) return;
        EnsureFormEditor();
        _formEditor!.LoadDocument(_document, _records, _documentStringEncoding);
        _formEditor.SetDataRootPath(_dataRootOverride);
    }

    private async Task ChangeDataRootAsync(string dataRoot)
    {
        _dataRootOverride = dataRoot;
        PathTextBoxPreferences.SavePath(PathTextBoxKind.IffDataRoot, dataRoot);
        if (_formEditor is not null) _formEditor.SetDataRootPath(dataRoot);
        if (_document is null) return;

        using CancellationTokenSource tokenSource = new();
        await ConfigureReferenceResolverAsync(tokenSource.Token);
    }

    private async Task ConfigureReferenceResolverAsync(CancellationToken token)
    {
        if (_formEditor is null || _document is null)
        {
            return;
        }

        _formEditor.SetReferenceResolver(null);
        if (!IffReferenceResolver.Supports(_document))
        {
            _formEditor.SetDataRootPath(_dataRootOverride);
            return;
        }

        try
        {
            IIffReferenceResolver? resolver = await IffReferenceResolver.CreateAsync(
                _document,
                _container,
                _directoryPath,
                txtIffDirectory.Text,
                _document.Region,
                _documentStringEncoding,
                _schemaProvider,
                token,
                _dataRootOverride);
            _formEditor.SetReferenceResolver(resolver);
            if (resolver is null) _formEditor.SetDataRootPath(_dataRootOverride);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException
            or ArgumentException or NotSupportedException)
        {
            AppLogger.Instance.Log(LogSource, $"Could not prepare IFF reference previews: {ex.Message}", AppLogLevel.Warning);
            _formEditor.SetReferenceResolver(null);
        }
    }

    private async Task RefreshSchemaViewAsync(CancellationToken token)
    {
        BuildColumns();
        LoadFormEditor();
        await ConfigureReferenceResolverAsync(token);
    }

    private void RefreshFormEditor(bool selectLast = false)
    {
        if (_formEditor is null) return;
        _formEditor.RefreshRecords();
        if (selectLast) _formEditor.SelectLastRecord();
    }

    private void SetEditorView(bool showFormView)
    {
        if (!showFormView && _showFormView && _formEditor?.HasPendingChanges == true &&
            !_formEditor.ApplyChanges())
            return;
        _showFormView = showFormView;
        if (_document is null)
        {
            gridRecords.Visible = false;
            if (_formEditor is not null) _formEditor.Visible = false;
            lblNoFileSelected.Visible = true;
        }
        else
        {
            EnsureFormEditor();
            lblNoFileSelected.Visible = false;
            _formEditor!.Visible = showFormView;
            gridRecords.Visible = !showFormView;
            if (showFormView)
            {
                _formEditor.RefreshRecords();
                _formEditor.BringToFront();
            }
            else gridRecords.BringToFront();
        }
        if (_toolbarFormView is not null) _toolbarFormView.Checked = showFormView;
        if (_toolbarGridView is not null) _toolbarGridView.Checked = !showFormView;
        UpdateToolbarState();
    }

    private void BuildColumns()
    {
        _visibleFields.Clear();
        if (_document?.Schema is { } schema)
        {
            _visibleFields.AddRange(schema.Fields.Where(field =>
                field.IsVisible && !IffSchemaCoverage.IsCatchAllRawRecord(field, _document.RecordSize)));
        }
        gridRecords.Columns.Clear();
        gridRecords.Columns.Add(new DataGridViewTextBoxColumn { Name = "Record", HeaderText = "#", ReadOnly = true, Width = 70, Resizable = DataGridViewTriState.True });
        foreach (IffField field in VisibleFields)
        {
            DataGridViewColumn column = field.Type switch
            {
                IffFieldType.DateTime => new DataGridViewDateTimePickerColumn(),
                IffFieldType.Boolean or IffFieldType.BooleanBitField or IffFieldType.ZeroBoolean or
                    IffFieldType.ByteRangeBoolean => new DataGridViewCheckBoxColumn(),
                _ => new DataGridViewTextBoxColumn()
            };
            column.Name = field.Name;
            column.HeaderText = $"{field.Name} @{field.Offset} [{field.Width} B]";
            column.ReadOnly = !field.IsEditable;
            column.Width = 140;
            column.Resizable = DataGridViewTriState.True;
            gridRecords.Columns.Add(column);
        }
        gridRecords.RowCount = _records.Count;
        gridRecords.Invalidate();
        UpdateSchemaCoverageLabel();
    }

    private void UpdateSchemaCoverageLabel()
    {
        if (_document?.Schema is not { } schema)
        {
            lblSchemaCoverage.Visible = false;
            return;
        }

        IffSchemaCoverageResult coverage = IffSchemaCoverage.Calculate(schema, _document.RecordSize);
        lblSchemaCoverage.Text = string.Format(LocalizationManager.CurrentCulture,
            Strings.IFFManager_UnrepresentedBytes, coverage.UnrepresentedBytes, coverage.RecordSize);
        lblSchemaCoverage.Visible = true;
    }

    private void GridRecords_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _records.Count) return;
        if (e.ColumnIndex == 0) { e.Value = e.RowIndex; return; }
        IffField field = VisibleFields[e.ColumnIndex - 1];
        e.Value = field.GetValue(_records[e.RowIndex].Bytes.Span, _documentStringEncoding);
    }

    private void GridRecords_CellValuePushed(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;
        try
        {
            IffField field = VisibleFields[e.ColumnIndex - 1];
            _records[e.RowIndex].SetValue(field, e.Value, _documentStringEncoding);
            UpdateDirtyState();
            AppLogger.Instance.Log(LogSource,
                $"Edited '{_entry?.Name}', record {e.RowIndex}, field '{field.Name}' = {e.Value ?? "<null>"}.");
        }
        catch (Exception ex) { ShowError(ex); gridRecords.InvalidateRow(e.RowIndex); }
    }

    private async Task SaveRawFieldAsync(IffSchema schema, IffFieldDefinition selectedField,
        int selectedRecordIndex)
    {
        if (_document is null) return;
        IffSchemaDefinition current = IffSchemaJson.FromSchema(_document.FileName, _document.Region, schema);
        List<IffFieldDefinition> fields = RemoveCatchAllRawFields(current.Fields, _document.RecordSize).ToList();
        if (fields.Any(existing => existing.Name.Equals(selectedField.Name,
            StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(Strings.IFFManager_DuplicateColumnName, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        try
        {
            fields = AddFieldFromRawRecordWindow(fields, _document.RecordSize, selectedField).ToList();
            IffSchemaDefinition updated = current with { Fields = fields };
            IffSchemaJson.ValidateDefinition(updated, _document.RecordSize);
            _schemaProvider.SaveValidated(updated, CurrentSchemaRegions(), _document.RecordSize);
            IffSchemaResolution resolution = _schemaProvider.Resolve(_document.FileName, _document.Region,
                _document.RecordSize);
            _document = _document with
            {
                Schema = resolution.Schema,
                SchemaWarning = resolution.Warning
            };
            await RefreshSchemaViewAsync(CancellationToken.None);
            SelectRecordIndex(selectedRecordIndex);
            lblStatus.Text = Strings.IFFManager_Saved;
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
        {
            ShowError(ex);
        }
    }

    private void SelectRecordIndex(int recordIndex)
    {
        if (recordIndex < 0 || recordIndex >= _records.Count) return;
        if (_formEditor is not null) _formEditor.SelectRecord(recordIndex);
        if (gridRecords.RowCount > recordIndex)
        {
            gridRecords.CurrentCell = gridRecords.Rows[recordIndex].Cells[0];
            gridRecords.Rows[recordIndex].Selected = true;
        }
    }

    private void GridRecords_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex <= 0 || e.ColumnIndex > VisibleFields.Count ||
            VisibleFields[e.ColumnIndex - 1].Type != IffFieldType.Raw ||
            gridRecords.IsCurrentCellInEditMode && gridRecords.CurrentCellAddress == new Point(e.ColumnIndex, e.RowIndex)) return;
        if (e.CellStyle is not { } style || e.Graphics is not { } graphics) return;
        Font font = style.Font ?? gridRecords.Font;
        string text = Convert.ToString(e.FormattedValue) ?? string.Empty;
        e.Paint(e.CellBounds, DataGridViewPaintParts.Background | DataGridViewPaintParts.Border | DataGridViewPaintParts.SelectionBackground);
        int x = e.CellBounds.Left + style.Padding.Left + 3;
        int y = e.CellBounds.Top + (e.CellBounds.Height - font.Height) / 2;
        int? previousGroup = null;
        for (int index = 0; index < text.Length; index += 2)
        {
            string pair = text.Substring(index, Math.Min(2, text.Length - index));
            int byteIndex = index / 2;
            (int? group, bool overlaps) = RawByteFieldVisual(VisibleFields[e.ColumnIndex - 1], byteIndex);
            int pairWidth = TextRenderer.MeasureText(graphics, pair, font, Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix).Width;
            Color textColor = e.State.HasFlag(DataGridViewElementStates.Selected)
                ? SystemColors.HighlightText
                : style.ForeColor;
            if (!e.State.HasFlag(DataGridViewElementStates.Selected) && group is int groupIndex)
            {
                Color band = overlaps ? Color.Red : RawFieldColor(groupIndex);
                using var brush = new SolidBrush(Color.FromArgb(72, band));
                graphics.FillRectangle(brush, x, e.CellBounds.Top + 1, pairWidth, e.CellBounds.Height - 2);
                if (previousGroup != group)
                {
                    using var pen = new Pen(Color.FromArgb(180, band));
                    graphics.DrawLine(pen, x, e.CellBounds.Top + 1, x, e.CellBounds.Bottom - 2);
                }
            }
            TextRenderer.DrawText(graphics, pair, font, new Point(x, y), textColor,
                TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix);
            x += pairWidth;
            previousGroup = group;
        }
        e.Handled = true;
    }

    private (int? Group, bool Overlaps) RawByteFieldVisual(IffField rawField, int rawByteIndex)
    {
        if (_document?.Schema is not { } schema) return (null, false);
        int recordOffset = rawField.Offset + rawByteIndex;
        int? group = null;
        int matches = 0;
        for (int index = 0; index < schema.Fields.Count; index++)
        {
            IffField field = schema.Fields[index];
            if (ReferenceEquals(field, rawField) ||
                IffSchemaCoverage.IsCatchAllRawRecord(field, _document.RecordSize)) continue;
            if (recordOffset >= field.Offset && recordOffset < field.Offset + field.Width)
            {
                group ??= index;
                matches++;
            }
        }
        return (group, matches > 1);
    }

    private static Color RawFieldColor(int index)
    {
        Color[] palette =
        [
            Color.DodgerBlue, Color.OrangeRed, Color.MediumSeaGreen, Color.MediumPurple,
            Color.Goldenrod, Color.DeepPink, Color.Teal, Color.SlateBlue
        ];
        return palette[index % palette.Length];
    }

    private async void GridRecords_MouseWheel(object? sender, MouseEventArgs e)
    {
        bool changeOffset = ModifierKeys.HasFlag(Keys.Control);
        bool changeWidth = ModifierKeys.HasFlag(Keys.Alt);
        bool selectedFieldOnly = ModifierKeys.HasFlag(Keys.Shift);
        if (changeOffset == changeWidth || e.Delta == 0 || _document?.Schema is not { } schema) return;
        DataGridView.HitTestInfo hit = gridRecords.HitTest(e.X, e.Y);
        if (hit.Type != DataGridViewHitTestType.ColumnHeader || hit.ColumnIndex <= 0 ||
            hit.ColumnIndex > VisibleFields.Count) return;
        if (e is HandledMouseEventArgs handled) handled.Handled = true;

        IffField hovered = VisibleFields[hit.ColumnIndex - 1];
        IffSchemaDefinition current = IffSchemaJson.FromSchema(_document.FileName, _document.Region, schema);
        int fieldIndex = current.Fields.ToList().FindIndex(field =>
            field.Name.Equals(hovered.Name, StringComparison.OrdinalIgnoreCase));
        if (fieldIndex < 0) return;
        int direction = Math.Sign(e.Delta);
        try
        {
            IffFieldDefinition selected = current.Fields[fieldIndex];
            IReadOnlyList<IffFieldDefinition> fields;
            if (changeOffset)
            {
                IffFieldDefinition replacement = selected with { Offset = checked(selected.Offset + direction) };
                fields = selectedFieldOnly
                    ? IffSchemaManagerDialog.ReplaceFieldWithoutAdjustingFollowing(current.Fields, fieldIndex,
                        replacement, _document.RecordSize, current.DefaultStringSize)
                    : IffSchemaManagerDialog.MoveFieldAndFollowingOffsets(current.Fields, fieldIndex,
                        direction, _document.RecordSize, current.DefaultStringSize);
            }
            else
            {
                IffFieldDefinition replacement = selected with { Width = checked(selected.Width + direction) };
                fields = selectedFieldOnly
                    ? IffSchemaManagerDialog.ReplaceFieldWithoutAdjustingFollowing(current.Fields, fieldIndex,
                        replacement, _document.RecordSize, current.DefaultStringSize)
                    : IffSchemaManagerDialog.AdjustFollowingOffsets(current.Fields, fieldIndex,
                        replacement, _document.RecordSize, current.DefaultStringSize);
            }
            IffSchemaDefinition updated = current with { Fields = fields };
            IffSchemaJson.ValidateDefinition(updated, _document.RecordSize);
            _schemaProvider.SaveValidated(updated, CurrentSchemaRegions(), _document.RecordSize);
            IffSchemaResolution resolution = _schemaProvider.Resolve(_document.FileName, _document.Region,
                _document.RecordSize);
            _document = _document with
            {
                Schema = resolution.Schema,
                SchemaWarning = resolution.Warning
            };
            await RefreshSchemaViewAsync(CancellationToken.None);
            gridRecords.Columns[Math.Min(hit.ColumnIndex, gridRecords.Columns.Count - 1)].Selected = true;
            lblStatus.Text = Strings.IFFManager_Saved;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or OverflowException)
        {
            System.Media.SystemSounds.Beep.Play();
            lblStatus.Text = ex.Message;
        }
    }

    private async Task PatchCurrentIffAsync()
    {
        if (_document is null || _entry is null || !CanEditDocument) return;
        if (!CommitPendingEdit()) return;

        using OpenFileDialog fileDialog = FileDialogFactory.CreateIffPatchSourceDialog();
        if (fileDialog.ShowDialog(this) != DialogResult.OK) return;
        FileDialogFactory.RememberDirectory(FileDialogKind.Iff, fileDialog.FileName);
        string targetName = Path.GetFileNameWithoutExtension(_document.FileName);
        string sourceName = Path.GetFileNameWithoutExtension(fileDialog.FileName);
        if (!Path.GetExtension(fileDialog.FileName).Equals(".iff", StringComparison.OrdinalIgnoreCase) ||
            !sourceName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(string.Format(LocalizationManager.CurrentCulture,
                    Strings.IFFManager_PatchNameMismatch, targetName),
                Strings.IFFManager_Patch, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var optionsDialog = new IffPatchSourceOptionsDialog(
            _documentStringEncoding.CodePage, Path.GetFileName(fileDialog.FileName));
        if (optionsDialog.ShowDialog(this) != DialogResult.OK) return;
        Encoding sourceEncoding = IffStringEncodingPreferences.GetEncoding(optionsDialog.SelectedCodePage);

        try
        {
            UseWaitCursor = true;
            await EnsureSchemasSeededAsync(CancellationToken.None);
            await using var sourceStream = new FileStream(fileDialog.FileName, FileMode.Open, FileAccess.Read,
                FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            (IffDocumentInfo sourceDocument, List<IffRecord> sourceRecords, _) =
                await ReadDocumentAsync(sourceStream, Path.GetFileName(fileDialog.FileName), null, null,
                    CancellationToken.None);
            IffPatchAnalysis analysis = await Task.Run(() => IffPatchAnalyzer.Analyze(
                _document, _records, _documentStringEncoding,
                sourceDocument, sourceRecords, sourceEncoding));

            if (analysis.Candidates.Count == 0)
            {
                MessageBox.Show(Strings.IFFManager_PatchNoSharedItems, Strings.IFFManager_Patch,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (analysis.SelectableFields.Count == 0)
            {
                MessageBox.Show(Strings.IFFManager_PatchNoCompatibleFields, Strings.IFFManager_Patch,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var selectionDialog = new IffPatchSelectionDialog(analysis);
            if (selectionDialog.ShowDialog(this) != DialogResult.OK) return;
            HashSet<uint> selectedIds = selectionDialog.SelectedItemIds.ToHashSet();
            IffPatchCandidate[] selected = analysis.Candidates
                .Where(candidate => selectedIds.Contains(candidate.ItemId))
                .ToArray();
            if (selected.Length == 0)
            {
                MessageBox.Show(Strings.IFFManager_PatchNoItemsSelected, Strings.IFFManager_Patch,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var fieldDialog = new IffPatchFieldSelectionDialog(analysis, selectedIds.ToArray());
            if (fieldDialog.ShowDialog(this) != DialogResult.OK) return;
            IReadOnlyList<string> selectedFields = fieldDialog.SelectedFieldNames;
            IffPatchSelectionSummary selectionSummary = analysis.Summarize(selectedIds, selectedFields);

            string summary = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_PatchSummaryFormat,
                selectionSummary.SelectedRecordCount,
                selectionSummary.ChangedFieldCount,
                selectionSummary.SkippedFieldCount,
                selectionSummary.TruncatedFieldCount,
                analysis.SourceOnlyRecordCount,
                analysis.TargetOnlyRecordCount);
            if (MessageBox.Show(summary, Strings.IFFManager_PatchReview, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int changedRecords = analysis.Apply(_records, selectedIds, selectedFields);
            gridRecords.Invalidate();
            RefreshFormEditor();
            UpdateDirtyState();
            lblStatus.Text = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_PatchAppliedFormat, changedRecords);
            AppLogger.Instance.Log(LogSource,
                $"Patched '{_entry.Name}' from '{fileDialog.FileName}': {changedRecords} records.");
        }
        catch (OperationCanceledException)
        {
            AppLogger.Instance.Log(LogSource, "IFF patch cancelled.", AppLogLevel.Warning);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException
                                   or ArgumentException or InvalidOperationException)
        {
            AppLogger.Instance.Log(LogSource, $"IFF patch failed: {ex.Message}", AppLogLevel.Error);
            ShowError(ex);
        }
        finally
        {
            UseWaitCursor = false;
            UpdateToolbarState();
        }
    }

    private async void btnSave_Click(object sender, EventArgs e)
    {
        AppLogger.Instance.Log(LogSource, "Save button clicked.");
        if (_isSaving)
        {
            AppLogger.Instance.Log(LogSource, "Save ignored because another save is already running.", AppLogLevel.Warning);
            return;
        }

        if (_container is null || _entry is null || _document is null)
        {
            string message = Strings.IFFManager_NoEditableEntryLoaded;
            AppLogger.Instance.Log(LogSource, message, AppLogLevel.Warning);
            MessageBox.Show(message, Strings.IFFManager_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!CommitPendingEdit())
        {
            AppLogger.Instance.Log(LogSource, "Save stopped because the current cell edit could not be committed.", AppLogLevel.Warning);
            MessageBox.Show(Strings.IFFManager_InvalidValue, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (!_structureDirty && !_containerEncodingDirty && !_records.Any(item => item.IsDirty))
        {
            AppLogger.Instance.Log(LogSource, "Save requested, but the current IFF has no changes.", AppLogLevel.Warning);
            MessageBox.Show(Strings.IFFManager_NoChanges, Strings.IFFManager_Warning,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (MessageBox.Show(Strings.IFFManager_ConfirmOverwrite, Strings.IFFManager_Warning,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            AppLogger.Instance.Log(LogSource, $"Save of '{_entry.Name}' was cancelled.", AppLogLevel.Warning);
            return;
        }
        string sourcePath = txtIffDirectory.Text;
        string entryName = _entry.Name;
        try
        {
            _isSaving = true;
            btnSave.Enabled = false;
            UseWaitCursor = true;
            int changedRecordCount = _records.Count(item => item.IsDirty);
            AppLogger.Instance.Log(LogSource,
                $"Saving '{entryName}' with {changedRecordCount} changed records to '{sourcePath}'.");
            await _container.SaveEntryAsync(entryName, _document.Header, _records,
                saveOptions: _selectedSaveOptions);
            foreach (IffRecord record in _records) record.AcceptChanges();
            _structureDirty = false;
            _formEditor?.RefreshRecords();
            UpdateDirtyState();
            _container = null;
            if (_directoryPath is null)
            {
                await ReplaceContainerAsync(await IffContainer.OpenAsync(sourcePath));
                FillEntryList(_container!.Entries);
            }
            else LoadIffFiles(_directoryPath);
            lblStatus.Text = Strings.IFFManager_Saved;
            AppLogger.Instance.Log(LogSource, $"Saved '{entryName}' successfully.");
        }
        catch (Exception ex) { ShowError(ex); }
        finally
        {
            _isSaving = false;
            UseWaitCursor = false;
            if (CanSaveDocument) btnSave.Enabled = true;
        }
    }

    private async Task ExtractCurrentIffAsync()
    {
        if (_entry is null || _isExtracting) return;

        using SaveFileDialog dialog = FileDialogFactory.CreateIffExtractSaveDialog(
            Path.GetFileName(_entry.Name));
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            AppLogger.Instance.Log(LogSource, "IFF extraction was cancelled before it started.",
                AppLogLevel.Warning);
            return;
        }

        FileDialogFactory.RememberDirectory(FileDialogKind.IffExtract, dialog.FileName);
        string sourcePath = _directoryPath is null
            ? txtIffDirectory.Text
            : Path.Combine(_directoryPath, _entry.Name);
        string entryName = _entry.Name;
        using var extractCancellation = new CancellationTokenSource();
        CancellationToken extractToken = extractCancellation.Token;
        CancellationTokenSource? previousCancellation =
            Interlocked.Exchange(ref _extractCancellation, extractCancellation);
        previousCancellation?.Cancel();

        try
        {
            _isExtracting = true;
            UseWaitCursor = true;
            lstIffFiles.Enabled = false;
            btnBrowseIffDir.Enabled = false;
            UpdateToolbarState();
            AppLogger.Instance.Log(LogSource,
                $"Extracting original IFF entry '{entryName}' to '{dialog.FileName}'.");
            await ExtractEntryAsync(_entry, dialog.FileName, sourcePath, extractToken);
            lblStatus.Text = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_ExtractedFormat, Path.GetFileName(dialog.FileName));
            AppLogger.Instance.Log(LogSource,
                $"Extracted original IFF entry '{entryName}' to '{dialog.FileName}'.");
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed) lblStatus.Text = Strings.IFFManager_ExtractCancelled;
            AppLogger.Instance.Log(LogSource, $"Extraction of '{entryName}' was cancelled.",
                AppLogLevel.Warning);
        }
        catch (Exception ex)
        {
            AppLogger.Instance.Log(LogSource, $"Extraction of '{entryName}' failed: {ex.Message}",
                AppLogLevel.Error);
            ShowError(ex);
        }
        finally
        {
            Interlocked.CompareExchange(ref _extractCancellation, null, extractCancellation);
            _isExtracting = false;
            UseWaitCursor = false;
            lstIffFiles.Enabled = true;
            btnBrowseIffDir.Enabled = true;
            UpdateToolbarState();
        }
    }

    private async Task ExtractAllIffsAsync()
    {
        if (_container is not { Kind: not IffContainerKind.LooseFile, Entries.Count: > 0 } container ||
            _isExtracting)
            return;

        string sourcePath = txtIffDirectory.Text;
        using FolderBrowserDialog dialog = FileDialogFactory.CreateIffExtractFolderDialog(sourcePath);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            AppLogger.Instance.Log(LogSource, "Bulk IFF extraction was cancelled before it started.",
                AppLogLevel.Warning);
            return;
        }

        FileDialogFactory.RememberFolder(FileDialogKind.IffExtract, dialog.SelectedPath);
        using var extractCancellation = new CancellationTokenSource();
        CancellationToken extractToken = extractCancellation.Token;
        CancellationTokenSource? previousCancellation =
            Interlocked.Exchange(ref _extractCancellation, extractCancellation);
        previousCancellation?.Cancel();

        try
        {
            _isExtracting = true;
            UseWaitCursor = true;
            lstIffFiles.Enabled = false;
            btnBrowseIffDir.Enabled = false;
            UpdateToolbarState();
            AppLogger.Instance.Log(LogSource,
                $"Extracting {container.Entries.Count} original IFF entries to '{dialog.SelectedPath}'.");
            int extractedCount = await ExtractAllEntriesAsync(
                container.Entries, dialog.SelectedPath, sourcePath, extractToken);
            lblStatus.Text = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_ExtractedAllFormat, extractedCount, dialog.SelectedPath);
            AppLogger.Instance.Log(LogSource,
                $"Extracted {extractedCount} original IFF entries to '{dialog.SelectedPath}'.");
        }
        catch (OperationCanceledException)
        {
            if (!IsDisposed) lblStatus.Text = Strings.IFFManager_ExtractCancelled;
            AppLogger.Instance.Log(LogSource, "Bulk IFF extraction was cancelled.",
                AppLogLevel.Warning);
        }
        catch (Exception ex)
        {
            string message = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_ExtractAllFailedFormat, ex.Message);
            AppLogger.Instance.Log(LogSource, $"Bulk IFF extraction failed: {ex.Message}",
                AppLogLevel.Error);
            ShowError(new IOException(message, ex));
        }
        finally
        {
            Interlocked.CompareExchange(ref _extractCancellation, null, extractCancellation);
            _isExtracting = false;
            UseWaitCursor = false;
            lstIffFiles.Enabled = true;
            btnBrowseIffDir.Enabled = true;
            UpdateToolbarState();
        }
    }

    internal static async Task<int> ExtractAllEntriesAsync(
        IReadOnlyList<IffContainerEntry> entries,
        string destinationDirectory,
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        string destinationFullPath = Path.GetFullPath(destinationDirectory);
        if (!Directory.Exists(destinationFullPath))
            throw new DirectoryNotFoundException(destinationFullPath);

        string sourceFullPath = Path.GetFullPath(sourcePath);
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var extractionPlan = new List<(IffContainerEntry Entry, string Destination)>(entries.Count);
        foreach (IffContainerEntry entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string fileName = Path.GetFileName(entry.Name);
            if (string.IsNullOrWhiteSpace(fileName) ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidDataException(string.Format(
                    LocalizationManager.CurrentCulture,
                    Strings.IFFManager_ExtractAllInvalidNameFormat,
                    entry.Name));
            }

            string destinationPath = Path.GetFullPath(Path.Combine(destinationFullPath, fileName));
            if (!destinations.Add(destinationPath))
            {
                throw new InvalidDataException(string.Format(
                    LocalizationManager.CurrentCulture,
                    Strings.IFFManager_ExtractAllNameCollisionFormat,
                    fileName));
            }

            if (destinationPath.Equals(sourceFullPath, StringComparison.OrdinalIgnoreCase))
                throw new IOException(Strings.IFFManager_ExtractSourceConflict);
            extractionPlan.Add((entry, destinationPath));
        }

        int extractedCount = 0;
        foreach ((IffContainerEntry entry, string destinationPath) in extractionPlan)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ExtractEntryAsync(entry, destinationPath, sourceFullPath, cancellationToken);
            extractedCount++;
        }
        return extractedCount;
    }

    internal static async Task ExtractEntryAsync(
        IffContainerEntry entry,
        string destinationPath,
        string sourcePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        string destinationFullPath = Path.GetFullPath(destinationPath);
        string sourceFullPath = Path.GetFullPath(sourcePath);
        if (destinationFullPath.Equals(sourceFullPath, StringComparison.OrdinalIgnoreCase))
            throw new IOException(Strings.IFFManager_ExtractSourceConflict);

        string? destinationDirectory = Path.GetDirectoryName(destinationFullPath);
        if (string.IsNullOrEmpty(destinationDirectory) || !Directory.Exists(destinationDirectory))
            throw new DirectoryNotFoundException(destinationDirectory);

        string temporaryPath = Path.Combine(destinationDirectory,
            $".{Path.GetFileName(destinationFullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using Stream source = await entry.OpenAsync(cancellationToken);
            await using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.WriteThrough))
            {
                await source.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, destinationFullPath, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporaryPath); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private bool CommitPendingEdit()
    {
        try
        {
            if (_showFormView && _formEditor?.HasPendingChanges == true && !_formEditor.ApplyChanges())
            {
                return false;
            }

            if (gridRecords.IsCurrentCellInEditMode && !gridRecords.EndEdit())
            {
                return false;
            }

            if (gridRecords.IsCurrentCellDirty)
            {
                return gridRecords.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }

            return true;
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return false;
        }
    }

    private bool ConfirmDiscard()
    {
        if (!_structureDirty && !_containerEncodingDirty && !_records.Any(item => item.IsDirty) &&
            _formEditor?.HasPendingChanges != true)
            return true;
        bool discard = MessageBox.Show(Strings.IFFManager_DiscardChanges, Strings.IFFManager_Warning,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
        if (discard && _containerEncodingDirty) RefreshContainerKeyComboBox();
        AppLogger.Instance.Log(LogSource,
            discard ? $"Discarded unsaved changes to '{_entry?.Name}'." : $"Kept editing '{_entry?.Name}'.",
            AppLogLevel.Warning);
        return discard;
    }

    private void FrmIFFManager_FormClosing(object? sender, FormClosingEventArgs e)
    {
        AppLogger.Instance.Log(LogSource, "IFF editor close requested.");
        if (!ConfirmDiscard())
        {
            e.Cancel = true;
            AppLogger.Instance.Log(LogSource, "IFF editor close was cancelled.", AppLogLevel.Warning);
        }
    }

    private Task ReplaceContainerAsync(IffContainer container)
    {
        _container?.Dispose();
        _container = container;
        RefreshContainerKeyComboBox();
        return Task.CompletedTask;
    }

    private void ClearDocument()
    {
        _records.Clear(); _visibleFields.Clear(); _document = null; _entry = null; _structureDirty = false;
        gridRecords.RowCount = 0; gridRecords.Columns.Clear(); gridRecords.Visible = false;
        _formEditor?.ClearDocument();
        if (_formEditor is not null) _formEditor.Visible = false;
        lblSchemaCoverage.Visible = false;
        lblNoFileSelected.Visible = true; btnSave.Enabled = false; btnAddRow.Enabled = false; btnDeleteRows.Enabled = false; btnAddColumn.Enabled = false;
        UpdateToolbarState();
    }

    private void UpdateDirtyState()
    {
        bool dirty = _structureDirty || _containerEncodingDirty || _records.Any(item => item.IsDirty) ||
            _formEditor?.HasPendingChanges == true;
        btnSave.Enabled = CanSaveDocument && !_isSaving;
        Text = Strings.Iff_Title + (dirty ? " *" : string.Empty);
        UpdateToolbarState();
    }

    private void UpdateToolbarState()
    {
        if (_toolbarOpenArchive is null) return;
        bool hasDocument = _document is not null;
        bool canEdit = CanEditDocument;
        bool canInteract = !_isExtracting;
        _toolbarOpenArchive.Enabled = canInteract;
        _toolbarContainerKey!.Enabled = _container?.Kind != IffContainerKind.LooseFile &&
            _container is not null && canInteract;
        _toolbarRegion!.Enabled = canInteract;
        _toolbarStringEncoding!.Enabled = canInteract;
        _toolbarExtract!.Enabled = _entry is not null && canInteract && !_isSaving;
        _toolbarExtractAll!.Enabled = _container is
            { Kind: not IffContainerKind.LooseFile, Entries.Count: > 0 } && canInteract && !_isSaving;
        _toolbarSave!.Enabled = CanSaveDocument && !_isSaving && canInteract;
        _toolbarPatch!.Enabled = canEdit && !_isSaving && canInteract;
        _toolbarAddRow!.Enabled = canEdit && canInteract;
        _toolbarDeleteRows!.Enabled = canEdit && canInteract && (_showFormView
            ? _formEditor?.SelectedRecordIndex >= 0
            : gridRecords.SelectedRows.Count > 0);
        _toolbarManageSchema!.Enabled = hasDocument && canInteract;
        _toolbarSchemaUpdates!.Enabled = _schemaUpdatesAvailable && canInteract;
        _toolbarRawRecord!.Enabled = hasDocument && canInteract && SelectedRecordIndex() >= 0;
        _toolbarFormView!.Enabled = hasDocument && canInteract;
        _toolbarGridView!.Enabled = hasDocument && canInteract;
        _toolbarFormView.Checked = _showFormView;
        _toolbarGridView.Checked = !_showFormView;
    }

    private void btnAddRow_Click(object sender, EventArgs e)
    {
        AppLogger.Instance.Log(LogSource, "Add row button clicked.");
        if (_document is not { } document || !CanEditDocument) return;
        if (_records.Count >= ushort.MaxValue)
        {
            MessageBox.Show(Strings.IFFManager_MaximumRows, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppLogger.Instance.Log(LogSource, Strings.IFFManager_MaximumRows, AppLogLevel.Warning);
            return;
        }

        _records.Add(IffRecord.CreateBlank(_records.Count, document.RecordSize, document.Schema));
        _structureDirty = true;
        gridRecords.RowCount = _records.Count;
        gridRecords.CurrentCell = gridRecords.Rows[^1].Cells[0];
        RefreshFormEditor(selectLast: true);
        UpdateDirtyState();
        AppLogger.Instance.Log(LogSource, $"Added row {_records.Count - 1} to '{_entry?.Name}'.");
    }

    private void btnDeleteRows_Click(object sender, EventArgs e)
    {
        AppLogger.Instance.Log(LogSource, "Delete rows button clicked.");
        int[] indices = gridRecords.SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.Index).Where(index => index >= 0 && index < _records.Count)
            .Distinct().OrderDescending().ToArray();
        if (indices.Length == 0)
        {
            AppLogger.Instance.Log(LogSource, "No rows were selected for deletion.", AppLogLevel.Warning);
            return;
        }

        foreach (int index in indices) _records.RemoveAt(index);
        _structureDirty = true;
        gridRecords.RowCount = _records.Count;
        RefreshFormEditor();
        UpdateDirtyState();
        AppLogger.Instance.Log(LogSource, $"Deleted {indices.Length} rows from '{_entry?.Name}'.");
    }

    private void DeleteSelectedFormRecord()
    {
        AppLogger.Instance.Log(LogSource, "Delete form record button clicked.");
        if (_formEditor is null || !CanEditDocument) return;
        int index = _formEditor.SelectedRecordIndex;
        if (index < 0 || index >= _records.Count) return;
        _records.RemoveAt(index);
        _structureDirty = true;
        gridRecords.RowCount = _records.Count;
        RefreshFormEditor();
        UpdateDirtyState();
        AppLogger.Instance.Log(LogSource, $"Deleted row {index} from '{_entry?.Name}'.");
    }

    private void CopySelectedFormRecord()
    {
        AppLogger.Instance.Log(LogSource, "Copy form record button clicked.");
        if (_document is not { } document || _formEditor is null || !CanEditDocument) return;
        int index = _formEditor.SelectedRecordIndex;
        if (index < 0 || index >= _records.Count) return;
        if (_records.Count >= ushort.MaxValue)
        {
            MessageBox.Show(Strings.IFFManager_MaximumRows, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        _records.Add(IffRecord.CreateCopy(_records.Count, _records[index].Bytes, document.Schema));
        _structureDirty = true;
        gridRecords.RowCount = _records.Count;
        RefreshFormEditor(selectLast: true);
        UpdateDirtyState();
        AppLogger.Instance.Log(LogSource, $"Copied row {index} to {_records.Count - 1} in '{_entry?.Name}'.");
    }

    private int SelectedRecordIndex()
    {
        if (_showFormView) return _formEditor?.SelectedRecordIndex ?? -1;
        if (gridRecords.CurrentCell?.RowIndex is int current && current >= 0 && current < _records.Count) return current;
        return gridRecords.SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.Index)
            .FirstOrDefault(index => index >= 0 && index < _records.Count, -1);
    }

    private async Task OpenRawRecordWindowAsync()
    {
        if (_document?.Schema is not { } schema) return;
        int recordIndex = SelectedRecordIndex();
        if (recordIndex < 0 || recordIndex >= _records.Count) return;

        using var dialog = new RawRecordColumnDialog(_document.RecordSize, _records[recordIndex].Bytes,
            schema.DefaultStringSize, schema, _documentStringEncoding, schema.DefaultLongStringSize);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        await SaveRawFieldAsync(schema, dialog.SelectedField, recordIndex);
    }

    private async void btnAddColumn_Click(object sender, EventArgs e)
    {
        if (_document?.Schema is not { } schema) return;
        IReadOnlyList<string> schemaRegions = CurrentSchemaRegions();
        IffSavedSchemaSource? savedSource = !string.IsNullOrEmpty(_document.SchemaWarning)
            ? _schemaProvider.ReadSavedSource(_document.FileName, schemaRegions, _document.RecordSize)
            : null;
        if (savedSource is not null &&
            (savedSource.Definition is null || !CanDisplayStructuredSchema(savedSource.Definition, _document.RecordSize) ||
             !SavedSourceIdentityMatches(savedSource, savedSource.Definition)))
        {
            using var recovery = new IffSchemaRecoveryDialog(savedSource with
            {
                Error = savedSource.Error ?? _document.SchemaWarning
            }, json => _schemaProvider.SaveJson(savedSource, json, schemaRegions, _document.RecordSize));
            if (recovery.ShowDialog(this) != DialogResult.OK) return;
            await ReloadResolvedSchemaAsync(schemaRegions);
            return;
        }

        IffSchemaDefinition current = savedSource?.Definition ??
            IffSchemaJson.FromSchema(_document.FileName, _document.Region, schema);
        IffFieldDefinition[] inheritedFields = [];
        if (current.Base is { } baseReference)
        {
            IffSchemaResolution baseResolution = ((IIffSchemaProvider)_schemaProvider).ResolveBase(baseReference, schemaRegions,
                _document.RecordSize);
            inheritedFields = baseResolution.Schema?.Fields
                .Where(field => !IffSchemaCoverage.IsCatchAllRawRecord(field, _document.RecordSize))
                .Select(IffSchemaJson.FromField).ToArray() ?? [];
        }
        using var dialog = new IffSchemaManagerDialog(_document.RecordSize, current.Fields,
            current.DefaultStringSize, IffSchemaPreferences.LoadTemplateSchemas(), CurrentIffFileNames(),
            current.DefaultLongStringSize, current.Base,
            savedSource is null
                ? schema.Fields.Where(field => field.IsInherited).Select(IffSchemaJson.FromField)
                : inheritedFields,
            _document.Region, _schemaProvider.Save);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (dialog.Fields.Count == 0 && dialog.BaseReference is null)
        {
            MessageBox.Show(Strings.IFFManager_SchemaRequiresColumn, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var updated = current with
            {
                SchemaVersion = IffSchemaJson.CurrentVersion,
                IsEditable = true,
                Fields = RemoveCatchAllRawFields(dialog.Fields, _document.RecordSize).ToArray(),
                DefaultStringSize = dialog.DefaultStringSize,
                DefaultLongStringSize = dialog.DefaultLongStringSize,
                Base = dialog.BaseReference
            };
            _schemaProvider.SaveValidated(updated, schemaRegions, _document.RecordSize);
            await ReloadResolvedSchemaAsync(schemaRegions);
            AppLogger.Instance.Log(LogSource, $"Saved JSON schema for '{_document.FileName}' ({_document.Region}).");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

    private IReadOnlyList<string> CurrentSchemaRegions()
    {
        if (SelectedSchemaRegion is { } selected) return IffHeader.GetSchemaRegions(selected);
        if (_document?.Region == IffHeader.Japan983Region)
            return IffHeader.GetSchemaRegions(_document.Region);
        return _document?.Header.FormatProfile?.SchemaRegions ??
               (_document is null ? [] : [_document.Region]);
    }

    private async Task ReloadResolvedSchemaAsync(IReadOnlyList<string> schemaRegions)
    {
        if (_document is null) return;
        IffSchemaResolution resolution = _schemaProvider.Resolve(_document.FileName, schemaRegions,
            _document.RecordSize);
        _document = _document with { Schema = resolution.Schema, SchemaWarning = resolution.Warning };
        foreach (IffRecord record in _records) record.UpdateSchema(resolution.Schema);
        await RefreshSchemaViewAsync(CancellationToken.None);
        btnSave.Enabled = resolution.Schema?.IsEditable == true;
        btnAddRow.Enabled = resolution.Schema?.IsEditable == true;
        UpdateToolbarState();
        lblStatus.Text = resolution.Schema is not null && resolution.Warning is null
            ? Strings.IFFManager_Saved
            : resolution.Warning ?? Strings.IFFManager_SchemaWarning;
    }

    internal static bool CanDisplayStructuredSchema(IffSchemaDefinition definition, int recordSize)
    {
        if (recordSize <= 0 || definition.MinimumRecordSize is <= 0 || definition.MinimumRecordSize > recordSize ||
            definition.DefaultStringSize is <= 0 || definition.DefaultStringSize > recordSize ||
            definition.DefaultLongStringSize <= 0)
            return false;
        return definition.Fields is not null && definition.Fields.All(field =>
            field.Offset >= 0 && field.Width > 0 && field.Offset <= recordSize - field.Width &&
            field.BitShift is >= 0 and <= 31);
    }

    private static bool SavedSourceIdentityMatches(IffSavedSchemaSource source, IffSchemaDefinition definition) =>
        definition.FileName.Equals(source.FileName, StringComparison.OrdinalIgnoreCase) &&
        (definition.Region.Equals(source.CandidateRegion, StringComparison.OrdinalIgnoreCase) ||
         definition.Region == "*");

    private IReadOnlyList<string> CurrentIffFileNames() =>
        lstIffFiles.Items.Cast<object>()
            .Select(Convert.ToString)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static IEnumerable<IffFieldDefinition> RemoveCatchAllRawFields(
        IEnumerable<IffFieldDefinition> fields, int recordSize) =>
        fields.Where(field => !(field.Type == IffFieldType.Raw && !field.IsEditable &&
                                field.Offset == 0 && field.Width == recordSize &&
                                field.Name.Equals("Raw record", StringComparison.OrdinalIgnoreCase)));

    internal static IReadOnlyList<IffFieldDefinition> AddFieldFromRawRecordWindow(
        IEnumerable<IffFieldDefinition> currentFields, int recordSize, IffFieldDefinition selectedField)
    {
        List<IffFieldDefinition> fields = RemoveCatchAllRawFields(currentFields, recordSize)
            .Select((field, index) => (field, index))
            .OrderBy(item => item.field.Offset)
            .ThenBy(item => item.index)
            .Select(item => item.field)
            .ToList();
        int insertIndex = fields.FindIndex(existing =>
            existing.Offset > selectedField.Offset ||
            existing.Offset == selectedField.Offset && existing.Width > selectedField.Width);
        fields.Insert(insertIndex < 0 ? fields.Count : insertIndex, selectedField);
        return fields;
    }

    private static void ShowError(Exception ex)
    {
        AppLogger.Instance.Log(LogSource, ex.ToString(), AppLogLevel.Error);
        MessageBox.Show(ex.Message, Strings.IFFManager_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
