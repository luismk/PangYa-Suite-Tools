using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;
using PangYa_Suite_Tools.Configuration;
using PangyaAPI.IFF;
using System.Text;

namespace PangYa_Suite_Tools;

public partial class FrmIFFManager : Form
{
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
    private Encoding _documentStringEncoding = IffStringEncodingPreferences.GetEncoding(
        IffStringEncodingPreferences.DefaultCodePage);
    private bool _structureDirty;

    private Encoding SelectedStringEncoding => cboStringEncoding.SelectedItem is PakEncodingOption option
        ? IffStringEncodingPreferences.GetEncoding(option.CodePage)
        : IffStringEncodingPreferences.GetEncoding(IffStringEncodingPreferences.DefaultCodePage);

    public FrmIFFManager()
    {
        InitializeComponent();
        ConfigureGrid();
        InitializeEncodingComboBox();
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
        gridRecords.DataError += (_, e) =>
        {
            e.ThrowException = false;
            string message = e.Exception?.Message ?? Strings.IFFManager_InvalidValue;
            AppLogger.Instance.Log(LogSource, $"Invalid grid value: {message}", AppLogLevel.Warning);
            MessageBox.Show(message, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        };
        gridRecords.SelectionChanged += (_, _) =>
            btnDeleteRows.Enabled = _document?.Schema is not null && gridRecords.SelectedRows.Count > 0;
    }

    private void InitializeLanguageComboBox()
    {
        cboLanguage.ComboBox.DisplayMember = "Key";
        cboLanguage.ComboBox.ValueMember = "Value";
        cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_PortugueseBrazil, LocalizationManager.PortugueseBrazil));
        cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_EnglishUS, LocalizationManager.English));
        cboLanguage.Items.Add(new KeyValuePair<string, string>(Strings.Common_Swedish, LocalizationManager.Swedish));
        cboLanguage.SelectedIndex = LocalizationManager.CurrentCultureIndex;
        _initializingLanguages = false;
        ApplyLocalization();
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
        cboLanguage.SelectedIndex = LocalizationManager.CurrentCultureIndex;
        _initializingLanguages = false;
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        Text = Strings.Iff_Title;
        lblIffDir.Text = Strings.Iff_Directory;
        btnBrowseIffDir.Text = Strings.Iff_Browse;
        btnOpenArchive.Text = Strings.IFFManager_OpenArchive;
        btnSave.Text = Strings.IFFManager_Save;
        btnAddRow.Text = Strings.IFFManager_AddRow;
        btnDeleteRows.Text = Strings.IFFManager_DeleteRows;
        grpIffFiles.Text = Strings.Iff_Files;
        lblLanguage.Text = Strings.Common_Language;
        lblStringEncoding.Text = Strings.IFFManager_StringEncoding;
        if (_document is null) lblStatus.Text = Strings.IFFManager_ReadySelectTheIFFFilesDirectory;
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
        using var dialog = new OpenFileDialog { Filter = "PangYa IFF (*.iff;*.zip)|*.iff;*.zip|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            AppLogger.Instance.Log(LogSource, "Archive selection was cancelled.", AppLogLevel.Warning);
            return;
        }

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
        ClearDocument();
        _entry = entry;
        _documentStringEncoding = SelectedStringEncoding;
        await using Stream stream = await entry.OpenAsync(token);
        await using IffReader reader = IffReader.Open(stream, Path.GetFileName(entry.Name));
        _document = reader.Info;
        await foreach (IffRecord record in reader.ReadRecordsAsync(token)) _records.Add(record);
        BuildColumns();
        lblNoFileSelected.Visible = false;
        gridRecords.Visible = true;
        btnSave.Enabled = _document.Schema is not null;
        btnAddRow.Enabled = _document.Schema is not null;
        lblStatus.Text = $"{Strings.IFFManager_EditingStructureOf} {entry.Name} — {_document.Region}, {_records.Count} records, {_document.RecordSize} bytes";
        AppLogger.Instance.Log(LogSource,
            $"Loaded '{entry.Name}' using {_documentStringEncoding.EncodingName}: region {_document.Region}, {_records.Count} records, {_document.RecordSize} bytes per record.");
    }

    private void BuildColumns()
    {
        gridRecords.Columns.Clear();
        gridRecords.Columns.Add(new DataGridViewTextBoxColumn { Name = "Record", HeaderText = "#", ReadOnly = true, Width = 70, Resizable = DataGridViewTriState.True });
        foreach (IffField field in _document?.Schema?.Fields ?? [])
        {
            DataGridViewColumn column = field.Type is IffFieldType.Boolean or IffFieldType.BooleanBitField or IffFieldType.ZeroBoolean
                ? new DataGridViewCheckBoxColumn()
                : new DataGridViewTextBoxColumn();
            column.Name = field.Name;
            column.HeaderText = $"{field.Name} @0x{field.Offset:X}";
            column.ReadOnly = !field.IsEditable;
            column.Width = 140;
            column.Resizable = DataGridViewTriState.True;
            gridRecords.Columns.Add(column);
        }
        gridRecords.RowCount = _records.Count;
        gridRecords.Invalidate();
    }

    private void GridRecords_CellValueNeeded(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _records.Count) return;
        if (e.ColumnIndex == 0) { e.Value = e.RowIndex; return; }
        IffField field = _document!.Schema!.Fields[e.ColumnIndex - 1];
        e.Value = field.GetValue(_records[e.RowIndex].Bytes.Span, _documentStringEncoding);
    }

    private void GridRecords_CellValuePushed(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex <= 0) return;
        try
        {
            IffField field = _document!.Schema!.Fields[e.ColumnIndex - 1];
            _records[e.RowIndex].SetValue(field.Name, e.Value, _documentStringEncoding);
            UpdateDirtyState();
            AppLogger.Instance.Log(LogSource,
                $"Edited '{_entry?.Name}', record {e.RowIndex}, field '{field.Name}' = {e.Value ?? "<null>"}.");
        }
        catch (Exception ex) { ShowError(ex); gridRecords.InvalidateRow(e.RowIndex); }
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
        if (!_structureDirty && !_records.Any(item => item.IsDirty))
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
            await _container.SaveEntryAsync(entryName, _document.Header, _records);
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
            if (_document?.Schema is not null) btnSave.Enabled = true;
        }
    }

    private bool CommitPendingEdit()
    {
        try
        {
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
        if (!_structureDirty && !_records.Any(item => item.IsDirty)) return true;
        bool discard = MessageBox.Show(Strings.IFFManager_DiscardChanges, Strings.IFFManager_Warning,
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
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
        return Task.CompletedTask;
    }

    private void ClearDocument()
    {
        _records.Clear(); _document = null; _entry = null; _structureDirty = false;
        gridRecords.RowCount = 0; gridRecords.Columns.Clear(); gridRecords.Visible = false;
        lblNoFileSelected.Visible = true; btnSave.Enabled = false; btnAddRow.Enabled = false; btnDeleteRows.Enabled = false;
    }

    private void UpdateDirtyState()
    {
        bool dirty = _structureDirty || _records.Any(item => item.IsDirty);
        btnSave.Enabled = _document?.Schema is not null && !_isSaving;
        Text = Strings.Iff_Title + (dirty ? " *" : string.Empty);
    }

    private void btnAddRow_Click(object sender, EventArgs e)
    {
        AppLogger.Instance.Log(LogSource, "Add row button clicked.");
        if (_document?.Schema is null) return;
        if (_records.Count >= ushort.MaxValue)
        {
            MessageBox.Show(Strings.IFFManager_MaximumRows, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            AppLogger.Instance.Log(LogSource, Strings.IFFManager_MaximumRows, AppLogLevel.Warning);
            return;
        }

        _records.Add(IffRecord.CreateBlank(_records.Count, _document.RecordSize, _document.Schema));
        _structureDirty = true;
        gridRecords.RowCount = _records.Count;
        gridRecords.CurrentCell = gridRecords.Rows[^1].Cells[0];
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
        UpdateDirtyState();
        AppLogger.Instance.Log(LogSource, $"Deleted {indices.Length} rows from '{_entry?.Name}'.");
    }

    private static void ShowError(Exception ex)
    {
        AppLogger.Instance.Log(LogSource, ex.ToString(), AppLogLevel.Error);
        MessageBox.Show(ex.Message, Strings.IFFManager_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
