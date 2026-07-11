using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;
using PangYa_Suite_Tools.Configuration;
using PangyaAPI.IFF;
using PangyaAPI.PAK.Models;
using System.Text;

namespace PangYa_Suite_Tools;

public partial class FrmIFFManager : Form
{
    private sealed record RegionOption(string Label, string? Region, string? DetectedRegion = null);
    private sealed record ContainerKeyOption(string Label, IffContainerSaveOptions SaveOptions);
    private const string LogSource = "IFF Editor";
    private bool _initializingLanguages = true;
    private bool _rebuildingEntryList;
    private string? _directoryPath;
    private IffContainer? _container;
    private IffContainerEntry? _entry;
    private IffDocumentInfo? _document;
    private readonly List<IffRecord> _records = [];
    private CancellationTokenSource? _loadCancellation;
    private bool _isSaving;
    private bool _initializingEncodings = true;
    private bool _initializingRegions = true;
    private Encoding _documentStringEncoding = IffStringEncodingPreferences.GetEncoding(
        IffStringEncodingPreferences.DefaultCodePage);
    private bool _structureDirty;
    private readonly List<IffField> _visibleFields = [];
    private readonly DirectoryIffSchemaProvider _schemaProvider;
    private bool _schemasSeeded;
    private bool _initializingContainerKeys = true;
    private bool _containerEncodingDirty;
    private IffContainerSaveOptions? _selectedSaveOptions;
    private IffFormRecordEditor? _formEditor;
    private string? _dataRootOverride;
    private bool _showFormView = true;
    private ToolStrip? _editorToolbar;
    private ToolStripButton? _toolbarOpenArchive;
    private ToolStripButton? _toolbarSave;
    private ToolStripButton? _toolbarAddRow;
    private ToolStripButton? _toolbarDeleteRows;
    private ToolStripButton? _toolbarManageSchema;
    private ToolStripButton? _toolbarRawRecord;
    private ToolStripButton? _toolbarFormView;
    private ToolStripButton? _toolbarGridView;

    private IReadOnlyList<IffField> VisibleFields => _visibleFields;

    private bool CanEditDocument => _document?.Schema?.IsEditable == true;
    private bool CanSaveDocument => CanEditDocument || _containerEncodingDirty;

    private Encoding SelectedStringEncoding => cboStringEncoding.SelectedItem is PakEncodingOption option
        ? IffStringEncodingPreferences.GetEncoding(option.CodePage)
        : IffStringEncodingPreferences.GetEncoding(IffStringEncodingPreferences.DefaultCodePage);

    private string? SelectedSchemaRegion => cboRegion.SelectedItem is RegionOption option ? option.Region : null;
    private string? SelectedDetectedRegion => cboRegion.SelectedItem is RegionOption option
        ? option.DetectedRegion
        : null;

    public FrmIFFManager()
    {
        InitializeComponent();
        _schemaProvider = IffSchemaPreferences.CreateProvider();
        ConfigureGrid();
        ConfigureEditorToolbar();
        RefreshContainerKeyComboBox();
        InitializeEncodingComboBox();
        InitializeRegionComboBox();
        InitializeLanguageComboBox();
        LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
        FormClosing += FrmIFFManager_FormClosing;
        AppLogger.Instance.Log(LogSource, "IFF editor opened.");
        Disposed += (_, _) =>
        {
            AppLogger.Instance.Log(LogSource, "IFF editor closed.");
            LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
            _loadCancellation?.Cancel();
            _container?.Dispose();
        };
    }

    public FrmIFFManager(string idiomaAtual) : this() => LocalizationManager.SetCulture(idiomaAtual);

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
        cboContainerKey.Items.Clear();
        cboContainerKey.ComboBox.DisplayMember = nameof(ContainerKeyOption.Label);
        if (_container?.Kind == IffContainerKind.LooseFile || _container is null)
        {
            cboContainerKey.Items.Add(new ContainerKeyOption(Strings.IFFManager_KeyNone,
                new IffContainerSaveOptions(IffContainerKind.LooseFile)));
        }
        else
        {
            cboContainerKey.Items.Add(new ContainerKeyOption(Strings.IFFManager_KeyPlainZip,
                new IffContainerSaveOptions(IffContainerKind.ZipArchive)));
            foreach ((string label, _) in PakKeys.All)
                cboContainerKey.Items.Add(new ContainerKeyOption(label,
                    new IffContainerSaveOptions(IffContainerKind.EncryptedZipArchive, label)));
        }
        ContainerKeyOption? selected = preserveSelection
            ? cboContainerKey.Items.Cast<ContainerKeyOption>().FirstOrDefault(option => option.SaveOptions == priorSelection)
            : cboContainerKey.Items.Cast<ContainerKeyOption>().FirstOrDefault(option =>
                _container?.Kind == option.SaveOptions.Kind &&
                (_container.Kind != IffContainerKind.EncryptedZipArchive ||
                 _container.EncryptionRegion == option.SaveOptions.EncryptionRegion));
        cboContainerKey.SelectedItem = selected ?? cboContainerKey.Items[0];
        _selectedSaveOptions = selected?.SaveOptions;
        _containerEncodingDirty = preserveSelection && priorDirty;
        cboContainerKey.Enabled = _container?.Kind != IffContainerKind.LooseFile && _container is not null;
        _initializingContainerKeys = false;
    }

    private void cboContainerKey_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_initializingContainerKeys || cboContainerKey.SelectedItem is not ContainerKeyOption option || _container is null) return;
        _selectedSaveOptions = option.SaveOptions;
        _containerEncodingDirty = _container.Kind != option.SaveOptions.Kind ||
            _container.Kind == IffContainerKind.EncryptedZipArchive &&
            _container.EncryptionRegion != option.SaveOptions.EncryptionRegion;
        UpdateDirtyState();
    }

    private void InitializeLanguageComboBox()
    {
        cboLanguage.ComboBox.DisplayMember = "Key";
        cboLanguage.ComboBox.ValueMember = "Value";
        cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_PortugueseBrazil, LocalizationManager.PortugueseBrazil));
        cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_EnglishUS, LocalizationManager.English));
        cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_Swedish, LocalizationManager.Swedish));
        cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_Japonese, LocalizationManager.Japonese));
		cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_French, LocalizationManager.French));
        SelectCurrentLanguage();
        _initializingLanguages = false;
        ApplyLocalization();
    }

    private void SelectCurrentLanguage()
    {
        string cultureName = LocalizationManager.CurrentCulture.Name;
        int index = cboLanguage.Items.Cast<object>()
            .Select((item, index) => (item, index))
            .FirstOrDefault(candidate => candidate.item is KeyValuePair<string, string> language &&
                language.Value.Equals(cultureName, StringComparison.OrdinalIgnoreCase)).index;
        bool found = index >= 0 && index < cboLanguage.Items.Count &&
            cboLanguage.Items[index] is KeyValuePair<string, string> language &&
            language.Value.Equals(cultureName, StringComparison.OrdinalIgnoreCase);
        cboLanguage.SelectedIndex = found ? index : cboLanguage.Items.Count > 0 ? 0 : -1;
    }

    private void InitializeEncodingComboBox()
    {
        IReadOnlyList<PakEncodingOption> encodings = IffStringEncodingPreferences.GetAvailableEncodings();
        int savedCodePage = IffStringEncodingPreferences.LoadCodePage();
        cboStringEncoding.ComboBox.DisplayMember = nameof(PakEncodingOption.Label);
        cboStringEncoding.ComboBox.ValueMember = nameof(PakEncodingOption.CodePage);
        cboStringEncoding.ComboBox.DataSource = encodings.ToList();
        cboStringEncoding.ComboBox.SelectedItem = encodings.First(option => option.CodePage == savedCodePage);
        _initializingEncodings = false;
    }

    private void InitializeRegionComboBox() => RefreshRegionComboBox(null, null);

    private void RefreshRegionComboBox(string? selectedRegion, string? detectedRegion = null)
    {
        if (string.IsNullOrWhiteSpace(detectedRegion) ||
            string.Equals(detectedRegion, "Unknown", StringComparison.OrdinalIgnoreCase))
            detectedRegion = null;
        _initializingRegions = true;
        cboRegion.Items.Clear();
        cboRegion.ComboBox.DisplayMember = nameof(RegionOption.Label);
        cboRegion.Items.Add(new RegionOption(Strings.IFFManager_RegionAuto, null));
        cboRegion.Items.Add(new RegionOption(Strings.IFFManager_RegionThailand, "TH"));
        cboRegion.Items.Add(new RegionOption(Strings.IFFManager_RegionJapan, "JP"));
        if (detectedRegion is not null)
            cboRegion.Items.Add(new RegionOption(detectedRegion, null, detectedRegion));
        cboRegion.SelectedItem = cboRegion.Items.Cast<RegionOption>()
            .First(option => detectedRegion is not null
                ? string.Equals(option.DetectedRegion, detectedRegion, StringComparison.OrdinalIgnoreCase)
                : string.Equals(option.Region, selectedRegion, StringComparison.OrdinalIgnoreCase));
        _initializingRegions = false;
    }

    private void cboRegion_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!_initializingRegions && _document is not null)
            lblStatus.Text = Strings.IFFManager_RegionAppliesNextLoad;
    }

    private void cboStringEncoding_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_initializingEncodings || cboStringEncoding.SelectedItem is not PakEncodingOption option) return;
        IffStringEncodingPreferences.SaveCodePage(option.CodePage);
        AppLogger.Instance.Log(LogSource, $"String encoding changed to '{option.Label}'.");
        if (_document is not null) lblStatus.Text = Strings.IFFManager_EncodingAppliesNextLoad;
    }

    private void cboLanguage_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (!_initializingLanguages && cboLanguage.SelectedItem is KeyValuePair<string, string> item)
        {
            AppLogger.Instance.Log(LogSource, $"Language changed to '{item.Value}'.");
            LocalizationManager.SetCulture(item.Value);
        }
    }

    private void LocalizationManager_CultureChanged(object? sender, EventArgs e)
    {
        _initializingLanguages = true;
        try
        {
            SelectCurrentLanguage();
            ApplyLocalization();
        }
        finally { _initializingLanguages = false; }
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
        lblContainerKey.Text = Strings.IFFManager_ContainerKey;
        grpIffFiles.Text = Strings.Iff_Files;
        lblLanguage.Text = Strings.Common_Language;
        lblStringEncoding.Text = Strings.IFFManager_StringEncoding;
        lblRegion.Text = Strings.IFFManager_Region;
        RefreshRegionComboBox(selectedRegion, detectedRegion);
        RefreshContainerKeyComboBox(preserveSelection: true);
        UpdateSchemaCoverageLabel();
        if (_document is null) lblStatus.Text = Strings.IFFManager_ReadySelectTheIFFFilesDirectory;
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
        _toolbarSave = CreateToolbarButton(Strings.IFFManager_Save, SystemIcons.Shield.ToBitmap(),
            (_, _) => btnSave_Click(btnSave, EventArgs.Empty));
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
        _toolbarRawRecord = CreateToolbarButton(Strings.IFFManager_RawRecord, SystemIcons.Application.ToBitmap(),
            async (_, _) => await OpenRawRecordWindowAsync());
        _toolbarFormView = CreateToolbarButton(Strings.IFFManager_FormView, SystemIcons.Question.ToBitmap(),
            (_, _) => SetEditorView(showFormView: true));
        _toolbarGridView = CreateToolbarButton(Strings.IFFManager_GridView, SystemIcons.Asterisk.ToBitmap(),
            (_, _) => SetEditorView(showFormView: false));
        _toolbarFormView.CheckOnClick = true;
        _toolbarGridView.CheckOnClick = true;

        _editorToolbar.Items.AddRange([
            _toolbarOpenArchive,
            _toolbarSave,
            new ToolStripSeparator(),
            _toolbarAddRow,
            _toolbarDeleteRows,
            _toolbarManageSchema,
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

    private void UpdateToolbarText()
    {
        if (_toolbarOpenArchive is null) return;
        _toolbarOpenArchive.Text = Strings.IFFManager_OpenArchive;
        _toolbarSave!.Text = Strings.IFFManager_Save;
        _toolbarAddRow!.Text = Strings.IFFManager_AddRow;
        _toolbarDeleteRows!.Text = Strings.IFFManager_DeleteRows;
        _toolbarManageSchema!.Text = Strings.IFFManager_ManageColumns;
        _toolbarRawRecord!.Text = Strings.IFFManager_RawRecord;
        _toolbarFormView!.Text = Strings.IFFManager_FormView;
        _toolbarGridView!.Text = Strings.IFFManager_GridView;
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
        try
        {
            UseWaitCursor = true;
            _loadCancellation?.Cancel();
            _loadCancellation = new CancellationTokenSource();
            if (_directoryPath is not null)
                await ReplaceContainerAsync(await IffContainer.OpenAsync(Path.Combine(_directoryPath, name), cancellationToken: _loadCancellation.Token));
            _entry = _container!.Entries.Single(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            await LoadEntryAsync(_entry, _loadCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            AppLogger.Instance.Log(LogSource, $"Loading '{name}' was cancelled.", AppLogLevel.Warning);
        }
        catch (Exception ex) { ShowError(ex); }
        finally { UseWaitCursor = false; }
    }

    private async Task LoadEntryAsync(IffContainerEntry entry, CancellationToken token)
    {
        string? selectedRegion = SelectedSchemaRegion;
        ClearDocument();
        _entry = entry;
        _documentStringEncoding = SelectedStringEncoding;
        if (!_schemasSeeded)
        {
            try
            {
                await Task.Run(IffSchemaPreferences.SeedDefaults, token);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                AppLogger.Instance.Log(LogSource, $"Could not seed default JSON schemas: {ex.Message}", AppLogLevel.Warning);
            }
            _schemasSeeded = true;
        }
        await using Stream stream = await entry.OpenAsync(token);
        await using IffReader reader = IffReader.Open(stream, Path.GetFileName(entry.Name),
            new(SchemaProvider: _schemaProvider, SchemaRegion: selectedRegion,
                FallbackSchemaRegion: _container?.FileNameRegion));
        _document = reader.Info;
        RefreshRegionComboBox(selectedRegion, selectedRegion is null ? _document.Region : null);
        if (!string.IsNullOrEmpty(_document.SchemaWarning))
        {
            AppLogger.Instance.Log(LogSource, _document.SchemaWarning, AppLogLevel.Warning);
            MessageBox.Show($"{Strings.IFFManager_SchemaWarning}\n{_document.SchemaWarning}",
                Strings.IFFManager_Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        await foreach (IffRecord record in reader.ReadRecordsAsync(token)) _records.Add(record);
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

    private void EnsureFormEditor()
    {
        if (_formEditor is not null) return;
        _formEditor = new IffFormRecordEditor { Visible = false };
        _formEditor.AddRequested += (_, _) => btnAddRow_Click(btnAddRow, EventArgs.Empty);
        _formEditor.DeleteRequested += (_, _) => DeleteSelectedFormRecord();
        _formEditor.CopyRequested += (_, _) => CopySelectedFormRecord();
        _formEditor.SaveRequested += (_, _) => btnSave_Click(btnSave, EventArgs.Empty);
        _formEditor.DataRootChangeRequested += path => _ = ChangeDataRootAsync(path);
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
            _schemaProvider.Save(updated);
            _document = _document with
            {
                Schema = IffSchemaJson.ToSchema(updated, _document.RecordSize),
                SchemaWarning = null
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
            _schemaProvider.Save(updated);
            _document = _document with
            {
                Schema = IffSchemaJson.ToSchema(updated, _document.RecordSize),
                SchemaWarning = null
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
        if (!_structureDirty && !_containerEncodingDirty && !_records.Any(item => item.IsDirty)) return true;
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
        bool dirty = _structureDirty || _containerEncodingDirty || _records.Any(item => item.IsDirty);
        btnSave.Enabled = CanSaveDocument && !_isSaving;
        Text = Strings.Iff_Title + (dirty ? " *" : string.Empty);
        UpdateToolbarState();
    }

    private void UpdateToolbarState()
    {
        if (_toolbarOpenArchive is null) return;
        bool hasDocument = _document is not null;
        bool canEdit = CanEditDocument;
        _toolbarOpenArchive.Enabled = true;
        _toolbarSave!.Enabled = CanSaveDocument && !_isSaving;
        _toolbarAddRow!.Enabled = canEdit;
        _toolbarDeleteRows!.Enabled = canEdit && (_showFormView
            ? _formEditor?.SelectedRecordIndex >= 0
            : gridRecords.SelectedRows.Count > 0);
        _toolbarManageSchema!.Enabled = hasDocument;
        _toolbarRawRecord!.Enabled = hasDocument && SelectedRecordIndex() >= 0;
        _toolbarFormView!.Enabled = hasDocument;
        _toolbarGridView!.Enabled = hasDocument;
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
        IffSchemaDefinition current = IffSchemaJson.FromSchema(_document.FileName, _document.Region, schema);
        using var dialog = new IffSchemaManagerDialog(_document.RecordSize, current.Fields,
            current.DefaultStringSize, IffSchemaPreferences.LoadTemplateSchemas(), CurrentIffFileNames(),
            current.DefaultLongStringSize);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (dialog.Fields.Count == 0)
        {
            MessageBox.Show(Strings.IFFManager_SchemaRequiresColumn, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            var updated = current with
            {
                IsEditable = true,
                Fields = RemoveCatchAllRawFields(dialog.Fields, _document.RecordSize).ToArray(),
                DefaultStringSize = dialog.DefaultStringSize,
                DefaultLongStringSize = dialog.DefaultLongStringSize
            };
            _schemaProvider.Save(updated);
            _document = _document with { Schema = IffSchemaJson.ToSchema(updated, _document.RecordSize), SchemaWarning = null };
            await RefreshSchemaViewAsync(CancellationToken.None);
            btnSave.Enabled = true;
            btnAddRow.Enabled = true;
            UpdateToolbarState();
            AppLogger.Instance.Log(LogSource, $"Saved JSON schema for '{_document.FileName}' ({_document.Region}).");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
    }

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
