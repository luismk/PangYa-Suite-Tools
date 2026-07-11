using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Shop;
using PangyaAPI.IFF;
using System.Globalization;
using System.Media;
using System.Text;

namespace PangYa_Suite_Tools;

internal sealed class IffFormRecordEditor : UserControl
{
    private sealed record FieldBinding(IffField Field, Control Editor);

    private readonly SplitContainer _splitContainer = new();
    private readonly TextBox _txtSearch = new();
    private readonly ListBox _lstRecords = new();
    private readonly TabControl _tabs = new();
    private readonly FlowLayoutPanel _actionPanel = new();
    private readonly Button _btnNew = new();
    private readonly Button _btnDelete = new();
    private readonly Button _btnCopy = new();
    private readonly Button _btnApply = new();
    private readonly Button _btnUpdate = new();
    private readonly Panel _dataRootPanel = new();
    private readonly TextBox _txtDataRoot = new();
    private readonly Button _btnBrowseDataRoot = new();
    private readonly GroupBox _setItemsGroup = new();
    private readonly FlowLayoutPanel _setItemsList = new();
    private readonly ToolTip _toolTip = new();
    private readonly List<FieldBinding> _bindings = [];
    private readonly List<int> _filteredIndices = [];
    private SoundPlayer? _soundPlayer;

    private IffDocumentInfo? _document;
    private IList<IffRecord>? _records;
    private IIffReferenceResolver? _referenceResolver;
    private string? _dataRootPath;
    private Encoding _encoding = Encoding.Latin1;
    private bool _loadingRecord;
    private bool _hasPendingChanges;
    private int _selectedRecordIndex = -1;

    public event EventHandler? AddRequested;
    public event EventHandler? DeleteRequested;
    public event EventHandler? CopyRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler? Applied;
    public event Action<string>? DataRootChangeRequested;

    public IffFormRecordEditor()
    {
        Name = "iffFormRecordEditor";
        Dock = DockStyle.Fill;

        _splitContainer.Dock = DockStyle.Fill;
        _splitContainer.FixedPanel = FixedPanel.Panel1;
        _splitContainer.SplitterDistance = 220;

        _txtSearch.Name = "txtFormRecordSearch";
        _txtSearch.Dock = DockStyle.Top;
        _txtSearch.Margin = new Padding(6);
        _txtSearch.PlaceholderText = Strings.IFFManager_FormRecordSearch;
        _txtSearch.TextChanged += (_, _) => RefreshRecordList(keepSelection: true);

        _lstRecords.Name = "lstFormRecords";
        _lstRecords.Dock = DockStyle.Fill;
        _lstRecords.IntegralHeight = false;
        _lstRecords.SelectedIndexChanged += RecordList_SelectedIndexChanged;

        var leftPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
        leftPanel.Controls.Add(_lstRecords);
        leftPanel.Controls.Add(_txtSearch);

        _tabs.Name = "tabFormFields";
        _tabs.Dock = DockStyle.Fill;

        _dataRootPanel.Name = "pnlIffDataRoot";
        _dataRootPanel.Dock = DockStyle.Top;
        _dataRootPanel.Height = 34;
        _dataRootPanel.Padding = new Padding(0, 3, 0, 5);

        var dataRootLabel = new Label
        {
            Name = "lblIffDataRoot",
            Text = Strings.IFFManager_DataFolder,
            Dock = DockStyle.Left,
            Width = 90,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _btnBrowseDataRoot.Name = "btnBrowseIffDataRoot";
        _btnBrowseDataRoot.Text = Strings.Iff_Browse;
        _btnBrowseDataRoot.Dock = DockStyle.Right;
        _btnBrowseDataRoot.Width = 92;
        _btnBrowseDataRoot.UseVisualStyleBackColor = true;
        _btnBrowseDataRoot.Click += (_, _) => BrowseDataRoot();
        _txtDataRoot.Name = "txtIffDataRoot";
        _txtDataRoot.Dock = DockStyle.Fill;
        _txtDataRoot.ReadOnly = true;
        _txtDataRoot.PlaceholderText = Strings.IFFManager_DataFolderNotSelected;
        _dataRootPanel.Controls.Add(_txtDataRoot);
        _dataRootPanel.Controls.Add(_btnBrowseDataRoot);
        _dataRootPanel.Controls.Add(dataRootLabel);

        _actionPanel.Dock = DockStyle.Bottom;
        _actionPanel.Height = 58;
        _actionPanel.FlowDirection = FlowDirection.RightToLeft;
        _actionPanel.Padding = new Padding(6);
        _actionPanel.WrapContents = false;

        ConfigureActionButton(_btnUpdate, Strings.IFFManager_FormUpdate, (_, _) => SaveRequested?.Invoke(this, EventArgs.Empty));
        ConfigureActionButton(_btnApply, Strings.IFFManager_FormApply, (_, _) => ApplyChanges());
        ConfigureActionButton(_btnCopy, Strings.IFFManager_FormCopy, (_, _) => CopyRequested?.Invoke(this, EventArgs.Empty));
        ConfigureActionButton(_btnDelete, Strings.IFFManager_FormDelete, (_, _) => DeleteRequested?.Invoke(this, EventArgs.Empty));
        ConfigureActionButton(_btnNew, Strings.IFFManager_FormNew, (_, _) => AddRequested?.Invoke(this, EventArgs.Empty));
        _actionPanel.Controls.AddRange([_btnUpdate, _btnApply, _btnCopy, _btnDelete, _btnNew]);

        _setItemsGroup.Name = "grpSetItemPreview";
        _setItemsGroup.Text = Strings.IFFManager_ReferencesPreview;
        _setItemsGroup.Dock = DockStyle.Bottom;
        _setItemsGroup.Height = 150;
        _setItemsGroup.Padding = new Padding(8);
        _setItemsGroup.Visible = false;

        _setItemsList.Name = "flowSetItemPreview";
        _setItemsList.Dock = DockStyle.Fill;
        _setItemsList.AutoScroll = true;
        _setItemsList.WrapContents = false;
        _setItemsGroup.Controls.Add(_setItemsList);

        var rightPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(6) };
        rightPanel.Controls.Add(_tabs);
        rightPanel.Controls.Add(_dataRootPanel);
        rightPanel.Controls.Add(_setItemsGroup);
        rightPanel.Controls.Add(_actionPanel);

        _splitContainer.Panel1.Controls.Add(leftPanel);
        _splitContainer.Panel2.Controls.Add(rightPanel);
        Controls.Add(_splitContainer);
    }

    public int SelectedRecordIndex => _selectedRecordIndex;

    public bool HasPendingChanges => _hasPendingChanges;

    public IReadOnlyList<string> TabNames => _tabs.TabPages.Cast<TabPage>().Select(page => page.Text).ToArray();

    public void LoadDocument(IffDocumentInfo document, IList<IffRecord> records, Encoding encoding)
    {
        _document = document;
        _records = records;
        _encoding = encoding;
        _referenceResolver = null;
        SetDataRootPath(null);
        _selectedRecordIndex = -1;
        _hasPendingChanges = false;
        BuildTabs();
        RefreshRecordList(keepSelection: false);
        if (records.Count > 0) SelectRecord(0);
        UpdateActionState();
    }

    public void SetReferenceResolver(IIffReferenceResolver? resolver)
    {
        _referenceResolver = resolver;
        if (resolver?.DataRoot is { } dataRoot) SetDataRootPath(dataRoot);
        RefreshReferencePreviews();
    }

    public void SetDataRootPath(string? path)
    {
        _dataRootPath = string.IsNullOrWhiteSpace(path) ? null : path;
        _txtDataRoot.Text = _dataRootPath ?? string.Empty;
        RefreshReferencePreviews();
    }

    public void ClearDocument()
    {
        _document = null;
        _records = null;
        _referenceResolver = null;
        SetDataRootPath(null);
        _bindings.Clear();
        _filteredIndices.Clear();
        _tabs.TabPages.Clear();
        _lstRecords.Items.Clear();
        _selectedRecordIndex = -1;
        _hasPendingChanges = false;
        ClearReferenceSummary();
        UpdateActionState();
    }

    private void BrowseDataRoot()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = Strings.Shop_SelectDataFolder,
            SelectedPath = _dataRootPath is { Length: > 0 } && Directory.Exists(_dataRootPath)
                ? _dataRootPath
                : string.Empty
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        SetDataRootPath(dialog.SelectedPath);
        DataRootChangeRequested?.Invoke(dialog.SelectedPath);
    }

    public void RefreshRecords(bool keepSelection = true)
    {
        RefreshRecordList(keepSelection);
        LoadSelectedRecord();
        UpdateActionState();
    }

    public void SelectLastRecord()
    {
        if (_records is not { Count: > 0 }) return;
        SelectRecord(_records.Count - 1);
    }

    public void SelectRecord(int recordIndex)
    {
        int filteredIndex = _filteredIndices.IndexOf(recordIndex);
        if (filteredIndex >= 0) _lstRecords.SelectedIndex = filteredIndex;
    }

    public bool ApplyChanges()
    {
        if (_document?.Schema is null || _records is null || _selectedRecordIndex < 0 ||
            _selectedRecordIndex >= _records.Count)
        {
            return true;
        }

        try
        {
            IffRecord record = _records[_selectedRecordIndex];
            foreach (FieldBinding binding in _bindings)
            {
                if (!binding.Field.IsEditable || !binding.Editor.Enabled) continue;
                record.SetValue(binding.Field, ReadEditorValue(binding.Field, binding.Editor), _encoding);
            }
            _hasPendingChanges = false;
            Applied?.Invoke(this, EventArgs.Empty);
            RefreshReferencePreviews();
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or FormatException or InvalidOperationException or OverflowException)
        {
            MessageBox.Show(ex.Message, Strings.IFFManager_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void ConfigureActionButton(Button button, string text, EventHandler handler)
    {
        button.Text = text;
        button.Width = 92;
        button.Height = 38;
        button.Margin = new Padding(4);
        button.UseVisualStyleBackColor = true;
        button.Click += handler;
    }

    private void BuildTabs()
    {
        _tabs.TabPages.Clear();
        _bindings.Clear();
        if (_document?.Schema is not { } schema) return;

        IReadOnlyList<IffFormTabDefinition> tabs = schema.Ui?.Tabs is { Count: > 0 } definedTabs
            ? definedTabs
            : [new IffFormTabDefinition(Strings.IFFManager_FormTabGeneral,
                schema.Fields
                    .Where(field => field.IsVisible && field.Type != IffFieldType.Raw)
                    .Select(field => new IffFormFieldDefinition(field.Name))
                    .ToArray())];

        foreach (IffFormTabDefinition tab in tabs)
        {
            var page = new TabPage(tab.Name) { Padding = new Padding(8) };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 4,
                Padding = new Padding(0, 4, 0, 4)
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            int row = 0;
            int column = 0;
            foreach (IffFormFieldDefinition metadata in tab.Fields
                .Where(field => field.IsVisible != false)
                .OrderBy(field => field.Order))
            {
                IffField? field = schema.Fields.FirstOrDefault(candidate =>
                    candidate.Name.Equals(metadata.Field, StringComparison.OrdinalIgnoreCase));
                if (field is null || field.Type == IffFieldType.Raw && string.IsNullOrWhiteSpace(metadata.Editor)) continue;
                AddField(layout, field, metadata.Label ?? field.Name, ref row, ref column);
            }

            var scroller = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            scroller.Controls.Add(layout);
            page.Controls.Add(scroller);
            _tabs.TabPages.Add(page);
        }
    }

    private void AddField(TableLayoutPanel layout, IffField field, string labelText, ref int row, ref int column)
    {
        bool fullWidth = field.Type == IffFieldType.LongString;
        if (fullWidth && column != 0)
        {
            row++;
            column = 0;
        }
        if (layout.RowCount <= row)
        {
            layout.RowCount = row + 1;
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        var label = new Label
        {
            Text = labelText,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Dock = DockStyle.Fill,
            Margin = new Padding(2, 4, 6, 4)
        };
        Control editor = CreateEditor(field);
        editor.Name = "field_" + field.Name.Replace(' ', '_');
        editor.Dock = DockStyle.Fill;
        editor.Margin = new Padding(2, 3, 8, 3);
        editor.Enabled = field.IsEditable;

        layout.Controls.Add(label, column, row);
        layout.Controls.Add(editor, column + 1, row);
        if (fullWidth)
        {
            layout.SetColumnSpan(editor, 3);
            row++;
            column = 0;
        }
        else if (column == 0)
        {
            column = 2;
        }
        else
        {
            row++;
            column = 0;
        }
        _bindings.Add(new FieldBinding(field, editor));
    }

    private Control CreateEditor(IffField field)
    {
        if (IsIconField(field)) return CreateIconEditor(field);
        if (field.Type == IffFieldType.Sound) return CreateSoundEditor(field);
        if (field.Reference is not null) return CreateReferenceEditor(field);

        Control editor = field.Type switch
        {
            IffFieldType.Boolean or IffFieldType.BooleanBitField or IffFieldType.ZeroBoolean or
                IffFieldType.ByteRangeBoolean => new CheckBox { AutoSize = true, Anchor = AnchorStyles.Left },
            IffFieldType.Byte or IffFieldType.UInt16 or IffFieldType.Int16 or IffFieldType.UInt32 or
                IffFieldType.ItemIdReference or IffFieldType.Int32 or IffFieldType.BitField => CreateNumericEditor(field),
            IffFieldType.DateTime => CreateDateTimeEditor(),
            IffFieldType.LongString => CreateLongStringEditor(),
            _ => new TextBox()
        };

        switch (editor)
        {
            case CheckBox checkBox:
                checkBox.CheckedChanged += (_, _) => MarkPending();
                break;
            case NumericUpDown numeric:
                numeric.ValueChanged += (_, _) => MarkPending();
                break;
            case DateTimePicker picker:
                picker.ValueChanged += (_, _) => MarkPending();
                break;
            case TextBox textBox:
                textBox.TextChanged += (_, _) => MarkPending();
                break;
        }
        return editor;
    }

    private static TextBox CreateLongStringEditor()
    {
        var textBox = new TextBox
        {
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            WordWrap = true,
            AcceptsReturn = true
        };
        int height = textBox.Font.Height * 5 + 8;
        textBox.Height = height;
        textBox.MinimumSize = new Size(0, height);
        return textBox;
    }

    private Control CreateReferenceEditor(IffField field)
    {
        var panel = new Panel { Height = 72 };
        NumericUpDown numeric = CreateNumericEditor(field);
        numeric.Name = "num_" + field.Name;
        numeric.Location = new Point(0, 0);
        numeric.Size = new Size(112, 24);
        numeric.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        numeric.ValueChanged += (_, _) =>
        {
            MarkPending();
            UpdateReferencePreview(panel, field, numeric.Value);
        };
        numeric.DoubleClick += (_, _) => ShowReferencePicker(field);
        var picker = new Button
        {
            Name = "btnPick_" + field.Name,
            Text = "...",
            Location = new Point(118, 0),
            Size = new Size(32, 24),
            UseVisualStyleBackColor = true
        };
        picker.Enabled = field.IsEditable && field.Reference?.PickerEnabled != false;
        picker.Click += (_, _) => ShowReferencePicker(field);
        var picture = new PictureBox
        {
            Name = "picReference_" + field.Name,
            Location = new Point(0, 28),
            Size = new Size(36, 36),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = SystemColors.ControlLight,
            Cursor = Cursors.Hand
        };
        picture.Click += (_, _) => ShowReferencePicker(field);
        var details = new Label
        {
            Name = "lblReference_" + field.Name,
            Location = new Point(42, 28),
            Size = new Size(230, 38),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };
        details.Click += (_, _) => ShowReferencePicker(field);
        panel.Controls.Add(numeric);
        panel.Controls.Add(picker);
        panel.Controls.Add(picture);
        panel.Controls.Add(details);
        return panel;
    }

    private Control CreateSoundEditor(IffField field)
    {
        var panel = new Panel { Height = 30 };
        var textBox = new TextBox { Name = "txtSound_" + field.Name, Dock = DockStyle.Fill };
        var play = new Button
        {
            Name = "btnPlaySound_" + field.Name,
            Text = Strings.IFFManager_PlaySound,
            Dock = DockStyle.Right,
            Width = 72,
            UseVisualStyleBackColor = true
        };
        textBox.TextChanged += (_, _) =>
        {
            MarkPending();
            UpdateSoundTooltip(panel, field, textBox.Text);
        };
        play.Click += (_, _) => PlaySound(field, textBox.Text);
        panel.Controls.Add(textBox);
        panel.Controls.Add(play);
        return panel;
    }

    private Control CreateIconEditor(IffField field)
    {
        var panel = new Panel { Height = 56 };
        var textBox = new TextBox { Name = "txtIcon_" + field.Name, Dock = DockStyle.Fill };
        textBox.TextChanged += (_, _) =>
        {
            MarkPending();
            UpdateIconPreview(panel, field, textBox.Text);
        };
        var picture = new PictureBox
        {
            Name = field.Name.Equals("Icon", StringComparison.OrdinalIgnoreCase)
                ? "picSetItemRecordIcon"
                : "picRecordIcon_" + field.Name,
            Dock = DockStyle.Right,
            Width = 52,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = SystemColors.ControlLight,
            Cursor = Cursors.Hand
        };
        picture.Click += (_, _) => ChooseIcon(field);
        panel.Controls.Add(textBox);
        panel.Controls.Add(picture);
        return panel;
    }

    private static NumericUpDown CreateNumericEditor(IffField field)
    {
        (decimal minimum, decimal maximum) = NumericRange(field);
        return new NumericUpDown
        {
            Minimum = minimum,
            Maximum = maximum,
            ThousandsSeparator = true,
            DecimalPlaces = 0
        };
    }

    private static DateTimePicker CreateDateTimeEditor() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "yyyy-MM-dd HH:mm:ss",
        ShowCheckBox = true,
        Checked = false
    };

    private static (decimal Minimum, decimal Maximum) NumericRange(IffField field)
    {
        if (field.Minimum is long minimum && field.Maximum is long maximum) return (minimum, maximum);
        return field.Type switch
        {
            IffFieldType.Byte => (byte.MinValue, byte.MaxValue),
            IffFieldType.UInt16 => (ushort.MinValue, ushort.MaxValue),
            IffFieldType.Int16 => (short.MinValue, short.MaxValue),
            IffFieldType.UInt32 or IffFieldType.ItemIdReference => (uint.MinValue, uint.MaxValue),
            IffFieldType.Int32 => (int.MinValue, int.MaxValue),
            IffFieldType.BitField when field.BitMask is uint mask => (0, mask >> field.BitShift),
            _ => (long.MinValue, long.MaxValue)
        };
    }

    private void RefreshRecordList(bool keepSelection)
    {
        if (_records is null) return;
        int previous = keepSelection ? _selectedRecordIndex : -1;
        string filter = _txtSearch.Text.Trim();

        _lstRecords.BeginUpdate();
        try
        {
            _lstRecords.Items.Clear();
            _filteredIndices.Clear();
            for (int index = 0; index < _records.Count; index++)
            {
                string text = FormatRecord(index, _records[index]);
                if (filter.Length > 0 && text.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) < 0) continue;
                _filteredIndices.Add(index);
                _lstRecords.Items.Add(text);
            }
        }
        finally { _lstRecords.EndUpdate(); }

        if (previous >= 0) SelectRecord(previous);
        else if (_lstRecords.Items.Count > 0) _lstRecords.SelectedIndex = 0;
    }

    private string FormatRecord(int index, IffRecord record)
    {
        string? name = TryGetDisplayValue(record, "Name");
        string? id = TryGetDisplayValue(record, "ItemId") ?? TryGetDisplayValue(record, "ID");
        return string.IsNullOrWhiteSpace(name)
            ? string.Format(CultureInfo.CurrentCulture, Strings.IFFManager_FormRecordFormat, index, id ?? string.Empty)
            : string.Format(CultureInfo.CurrentCulture, Strings.IFFManager_FormRecordNameFormat, index, id ?? string.Empty, name);
    }

    private string? TryGetDisplayValue(IffRecord record, string fieldName)
    {
        return record.TryGetValue(fieldName, out object? value, _encoding)
            ? Convert.ToString(value, CultureInfo.CurrentCulture)
            : null;
    }

    private void RecordList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_lstRecords.SelectedIndex < 0 || _lstRecords.SelectedIndex >= _filteredIndices.Count) return;
        int nextIndex = _filteredIndices[_lstRecords.SelectedIndex];
        if (nextIndex == _selectedRecordIndex) return;

        if (_hasPendingChanges)
        {
            DialogResult result = MessageBox.Show(Strings.IFFManager_FormUnsavedChanges,
                Strings.IFFManager_Warning, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.Cancel)
            {
                SelectRecord(_selectedRecordIndex);
                return;
            }
            if (result == DialogResult.Yes && !ApplyChanges())
            {
                SelectRecord(_selectedRecordIndex);
                return;
            }
        }

        _selectedRecordIndex = nextIndex;
        LoadSelectedRecord();
        UpdateActionState();
    }

    private void LoadSelectedRecord()
    {
        if (_records is null || _selectedRecordIndex < 0 || _selectedRecordIndex >= _records.Count) return;
        _loadingRecord = true;
        try
        {
            IffRecord record = _records[_selectedRecordIndex];
            foreach (FieldBinding binding in _bindings)
                WriteEditorValue(binding.Field, binding.Editor, binding.Field.GetValue(record.Bytes.Span, _encoding));
            _hasPendingChanges = false;
            RefreshReferencePreviews();
        }
        finally { _loadingRecord = false; }
    }

    private void RefreshReferencePreviews()
    {
        foreach (FieldBinding binding in _bindings.Where(binding => binding.Field.Reference is not null))
            UpdateReferencePreview(binding.Editor, binding.Field, ReadEditorValue(binding.Field, binding.Editor));
        foreach (FieldBinding binding in _bindings.Where(binding => IsIconField(binding.Field)))
            UpdateIconPreview(binding.Editor, binding.Field, Convert.ToString(ReadEditorValue(binding.Field, binding.Editor), CultureInfo.CurrentCulture) ?? string.Empty);
        foreach (FieldBinding binding in _bindings.Where(binding => binding.Field.Type == IffFieldType.Sound))
            UpdateSoundTooltip(binding.Editor, binding.Field, Convert.ToString(ReadEditorValue(binding.Field, binding.Editor), CultureInfo.CurrentCulture) ?? string.Empty);
        RefreshReferenceSummary();
    }

    private void RefreshReferenceSummary()
    {
        ClearReferenceSummary();
        if (_document?.Schema is null || _records is null || _referenceResolver is null ||
            _selectedRecordIndex < 0 || _selectedRecordIndex >= _records.Count)
        {
            _setItemsGroup.Visible = false;
            return;
        }

        IffRecord record = _records[_selectedRecordIndex];
        IffReferenceDisplay[] entries = _document.Schema.Fields
            .Where(field => field.Reference is not null)
            .Select(field => _referenceResolver.Resolve(field, field.GetValue(record.Bytes.Span, _encoding)))
            .Where(entry => entry.Key != 0)
            .ToArray();
        if (entries.Length == 0)
        {
            _setItemsGroup.Visible = false;
            return;
        }

        foreach (IffReferenceDisplay entry in entries) _setItemsList.Controls.Add(CreateReferenceCard(entry));
        _setItemsGroup.Visible = true;
    }

    private void ClearReferenceSummary()
    {
        foreach (Control control in _setItemsList.Controls)
        {
            foreach (PictureBox pictureBox in control.Controls.Find("picReferenceIcon", true).OfType<PictureBox>())
            {
                pictureBox.Image?.Dispose();
                pictureBox.Image = null;
            }
            control.Dispose();
        }
        _setItemsList.Controls.Clear();
    }

    private Control CreateReferenceCard(IffReferenceDisplay entry)
    {
        var panel = new Panel
        {
            Width = 230,
            Height = 82,
            Margin = new Padding(0, 0, 8, 0),
            BorderStyle = BorderStyle.FixedSingle,
            Cursor = Cursors.Hand,
            Tag = entry.Field.Name
        };
        panel.Click += (_, _) => ShowReferencePicker(entry.Field);
        var icon = new PictureBox
        {
            Name = "picReferenceIcon",
            Location = new Point(8, 8),
            Size = new Size(48, 48),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = SystemColors.ControlLight,
            Cursor = Cursors.Hand
        };
        icon.Image = IffPreviewImageLoader.Load(entry.IconPath);
        _toolTip.SetToolTip(icon, entry.IconPath ?? string.Empty);
        icon.Click += (_, _) => ShowReferencePicker(entry.Field);

        var name = new Label
        {
            Name = "lblReferenceName",
            Text = entry.Name,
            Location = new Point(64, 7),
            Size = new Size(156, 34),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };
        name.Click += (_, _) => ShowReferencePicker(entry.Field);
        var details = new Label
        {
            Name = "lblReferenceDetails",
            Text = entry.ItemIdInfo?.Summary ?? string.Format(CultureInfo.CurrentCulture, Strings.IFFManager_ReferenceDetailsFormat,
                entry.Field.Name, entry.Key, entry.TargetFile),
            Location = new Point(64, 42),
            Size = new Size(156, 18),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };
        details.Click += (_, _) => ShowReferencePicker(entry.Field);
        var status = new Label
        {
            Name = "lblReferenceStatus",
            Text = entry.MissingRecord
                ? Strings.IFFManager_ReferenceMissingRecord
                : entry.MissingIcon ? Strings.IFFManager_SetItemMissingIcon : string.Empty,
            Location = new Point(8, 60),
            Size = new Size(212, 18),
            ForeColor = SystemColors.GrayText,
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };
        status.Click += (_, _) => ShowReferencePicker(entry.Field);
        panel.Controls.Add(icon);
        panel.Controls.Add(name);
        panel.Controls.Add(details);
        panel.Controls.Add(status);
        return panel;
    }

    internal bool TrySetItemReference(int slot, uint itemId)
    {
        string fieldName = "Item" + slot.ToString(CultureInfo.InvariantCulture);
        IffField? field = _document?.Schema?.Fields.FirstOrDefault(candidate =>
            candidate.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        return field is not null && TrySetReference(field, itemId);
    }

    internal bool TrySetReference(IffField field, uint key)
    {
        if (_records is null || _document?.Schema is null || _selectedRecordIndex < 0 ||
            _selectedRecordIndex >= _records.Count)
            return false;

        FieldBinding? binding = _bindings.FirstOrDefault(candidate =>
            candidate.Field.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase));
        if (binding is null) return false;

        try
        {
            _records[_selectedRecordIndex].SetValue(field, key, _encoding);
            bool wasLoading = _loadingRecord;
            _loadingRecord = true;
            try { WriteEditorValue(field, binding.Editor, key); }
            finally { _loadingRecord = wasLoading; }
            Applied?.Invoke(this, EventArgs.Empty);
            RefreshReferencePreviews();
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or FormatException or InvalidOperationException or OverflowException)
        {
            MessageBox.Show(ex.Message, Strings.IFFManager_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    internal bool TrySetSetItemIconFromPath(string selectedPath)
    {
        IffField? field = _document?.Schema?.Fields.FirstOrDefault(candidate =>
            candidate.Name.Equals("Icon", StringComparison.OrdinalIgnoreCase));
        return field is not null && TrySetIconFromPath(field, selectedPath);
    }

    internal bool TrySetIconFromPath(IffField field, string selectedPath)
    {
        if (_records is null || _document?.Schema is null || _selectedRecordIndex < 0 ||
            _selectedRecordIndex >= _records.Count || _referenceResolver?.DataRoot is not string dataRoot)
            return false;

        string root = Path.GetFullPath(dataRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string fullPath = Path.GetFullPath(selectedPath);
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return false;

        FieldBinding? binding = _bindings.FirstOrDefault(candidate =>
            candidate.Field.Name.Equals(field.Name, StringComparison.OrdinalIgnoreCase));
        if (binding is null) return false;

        string iconId = Path.GetFileNameWithoutExtension(fullPath);
        try
        {
            _records[_selectedRecordIndex].SetValue(field, iconId, _encoding);
            bool wasLoading = _loadingRecord;
            _loadingRecord = true;
            try { WriteEditorValue(field, binding.Editor, iconId); }
            finally { _loadingRecord = wasLoading; }
            Applied?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or ArgumentOutOfRangeException or FormatException or InvalidOperationException or OverflowException)
        {
            MessageBox.Show(ex.Message, Strings.IFFManager_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private void ShowReferencePicker(IffField field)
    {
        if (_referenceResolver is null || field.Reference is null || field.Reference.PickerEnabled == false) return;
        IReadOnlyList<IffReferenceCatalogItem> catalog = _referenceResolver.GetCatalog(field);
        if (catalog.Count == 0) return;
        uint selected = ReadEditorUInt(field.Name);
        using var dialog = new IffReferencePickerDialog(catalog, field.Reference.TargetFile, selected);
        if (dialog.ShowDialog(FindForm()) == DialogResult.OK && dialog.SelectedItem is { } item)
            TrySetReference(field, item.Key);
    }

    private void ChooseIcon(IffField field)
    {
        if (_referenceResolver?.DataRoot is not string dataRoot)
        {
            MessageBox.Show(Strings.Shop_IconMustBeInData, Strings.IFFManager_ReferencesPreview,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string iconId = ReadEditorString(field.Name);
        string? resolvedIconPath = _referenceResolver.TryResolveIconPath(field, iconId);
        string initialDirectory = GetIconInitialDirectory(field, dataRoot, resolvedIconPath);
        using var dialog = FileDialogFactory.CreateIconOpenDialog(initialDirectory);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        FileDialogFactory.RememberDirectory(FileDialogKind.Icon, dialog.FileName);
        if (!TrySetIconFromPath(field, dialog.FileName))
            MessageBox.Show(Strings.Shop_IconMustBeInData, Strings.IFFManager_ReferencesPreview,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    internal static string GetIconInitialDirectory(IffField field, string dataRoot, string? resolvedIconPath = null)
    {
        string? resolvedDirectory = Path.GetDirectoryName(resolvedIconPath);
        if (!string.IsNullOrWhiteSpace(resolvedDirectory) && Directory.Exists(resolvedDirectory))
            return resolvedDirectory;

        if (string.IsNullOrWhiteSpace(field.IconPath)) return dataRoot;

        string[] roots = [dataRoot, Path.Combine(dataRoot, "data"), Path.Combine(dataRoot, "pangya")];
        return roots.Select(root => Path.GetFullPath(Path.Combine(root, field.IconPath)))
            .FirstOrDefault(Directory.Exists) ?? dataRoot;
    }

    private string ReadEditorString(string fieldName)
    {
        FieldBinding? binding = _bindings.FirstOrDefault(candidate =>
            candidate.Field.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        return binding is null
            ? string.Empty
            : Convert.ToString(ReadEditorValue(binding.Field, binding.Editor), CultureInfo.CurrentCulture) ?? string.Empty;
    }

    private uint ReadEditorUInt(string fieldName)
    {
        FieldBinding? binding = _bindings.FirstOrDefault(candidate =>
            candidate.Field.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        if (binding is null) return 0;
        try { return Convert.ToUInt32(ReadEditorValue(binding.Field, binding.Editor), CultureInfo.InvariantCulture); }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException) { return 0; }
    }

    private static bool IsIconField(IffField field) =>
        field.Type == IffFieldType.Icon ||
        field.Type == IffFieldType.FixedString && field.Name.Equals("Icon", StringComparison.OrdinalIgnoreCase);

    private void UpdateIconPreview(Control editor, IffField field, string iconId)
    {
        PictureBox? picture = editor.Controls.Find("picSetItemRecordIcon", true).OfType<PictureBox>().FirstOrDefault()
            ?? editor.Controls.Find("picRecordIcon_" + field.Name, true).OfType<PictureBox>().FirstOrDefault();
        if (picture is null) return;
        picture.Image?.Dispose();
        string? resolvedPath = _referenceResolver?.TryResolveIconPath(field, iconId);
        picture.Image = IffPreviewImageLoader.Load(resolvedPath);
        _toolTip.SetToolTip(picture, resolvedPath ?? string.Empty);
    }

    private void UpdateSoundTooltip(Control editor, IffField field, string soundId)
    {
        string tooltip = ResolveSoundPath(field, soundId) ?? string.Empty;
        foreach (Control control in editor.Controls.Cast<Control>())
            _toolTip.SetToolTip(control, tooltip);
    }

    private void PlaySound(IffField field, string soundId)
    {
        string? path = ResolveSoundPath(field, soundId);
        if (path is null)
        {
            MessageBox.Show(Strings.IFFManager_SoundFileNotFound, Strings.IFFManager_ReferencesPreview,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _soundPlayer?.Stop();
            _soundPlayer?.Dispose();
            _soundPlayer = new SoundPlayer(path);
            _soundPlayer.Play();
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            MessageBox.Show(ex.Message, Strings.IFFManager_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private string? ResolveSoundPath(IffField field, string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId)) return null;
        string? dataRoot = _referenceResolver?.DataRoot ?? _dataRootPath;
        if (string.IsNullOrWhiteSpace(dataRoot)) return null;

        string fileName = Path.GetFileName(soundId.Trim());
        if (fileName.Length == 0) return null;
        if (!fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) fileName += ".wav";

        string root = Path.GetFullPath(dataRoot);
        string relativeFolder = field.SoundPath ?? string.Empty;
        string folder = Path.GetFullPath(Path.Combine(root, relativeFolder));
        string rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!folder.Equals(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase) &&
            !folder.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        string candidate = Path.Combine(folder, fileName);
        return File.Exists(candidate) ? candidate : null;
    }

    private void UpdateReferencePreview(Control editor, IffField field, object? value)
    {
        PictureBox? picture = editor.Controls.Find("picReference_" + field.Name, true).OfType<PictureBox>().FirstOrDefault();
        Label? label = editor.Controls.Find("lblReference_" + field.Name, true).OfType<Label>().FirstOrDefault();
        if (picture is null && label is null) return;

        IffReferenceDisplay? display = _referenceResolver?.Resolve(field, value);
        if (picture is not null)
        {
            picture.Image?.Dispose();
            picture.Image = IffPreviewImageLoader.Load(display?.IconPath);
            _toolTip.SetToolTip(picture, display?.IconPath ?? string.Empty);
        }

        if (label is not null)
        {
            label.Text = display is null || display.Key == 0
                ? field.Reference?.TargetFile ?? string.Empty
                : display.ItemIdInfo?.Summary ?? string.Format(CultureInfo.CurrentCulture, Strings.IFFManager_ReferenceDetailsFormat,
                    display.Name, display.Key, display.TargetFile);
        }
    }

    private static void WriteEditorValue(IffField field, Control editor, object value)
    {
        switch (editor)
        {
            case Panel panel when panel.Controls.Find("num_" + field.Name, true).OfType<NumericUpDown>().FirstOrDefault() is { } numeric:
                decimal panelValue = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                numeric.Value = Math.Min(numeric.Maximum, Math.Max(numeric.Minimum, panelValue));
                break;
            case Panel panel when panel.Controls.Find("txtIcon_" + field.Name, true).OfType<TextBox>().FirstOrDefault() is { } textBox:
                textBox.Text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
                break;
            case Panel panel when panel.Controls.Find("txtSound_" + field.Name, true).OfType<TextBox>().FirstOrDefault() is { } textBox:
                textBox.Text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
                break;
            case CheckBox checkBox:
                checkBox.Checked = Convert.ToBoolean(value, CultureInfo.InvariantCulture);
                break;
            case NumericUpDown numeric:
                decimal converted = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                numeric.Value = Math.Min(numeric.Maximum, Math.Max(numeric.Minimum, converted));
                break;
            case DateTimePicker picker:
                if (value is DateTime date)
                {
                    picker.Value = date;
                    picker.Checked = true;
                }
                else
                {
                    picker.Checked = false;
                }
                break;
            case TextBox textBox when value is DateTime textDate:
                textBox.Text = textDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture);
                break;
            case TextBox textBox:
                textBox.Text = Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
                break;
        }
    }

    private static object? ReadEditorValue(IffField field, Control editor) => editor switch
    {
        Panel panel when panel.Controls.Find("num_" + field.Name, true).OfType<NumericUpDown>().FirstOrDefault() is { } numeric
            && field.Type is IffFieldType.UInt32 or IffFieldType.ItemIdReference => (uint)numeric.Value,
        Panel panel when panel.Controls.Find("num_" + field.Name, true).OfType<NumericUpDown>().FirstOrDefault() is { } numeric => (long)numeric.Value,
        Panel panel when panel.Controls.Find("txtIcon_" + field.Name, true).OfType<TextBox>().FirstOrDefault() is { } textBox => textBox.Text,
        Panel panel when panel.Controls.Find("txtSound_" + field.Name, true).OfType<TextBox>().FirstOrDefault() is { } textBox => textBox.Text,
        CheckBox checkBox => checkBox.Checked,
        NumericUpDown numeric when field.Type is IffFieldType.UInt32 or IffFieldType.ItemIdReference => (uint)numeric.Value,
        NumericUpDown numeric => (long)numeric.Value,
        DateTimePicker picker => picker.Checked ? picker.Value : string.Empty,
        TextBox textBox => textBox.Text,
        _ => null
    };

    private void MarkPending()
    {
        if (!_loadingRecord) _hasPendingChanges = true;
    }

    private void UpdateActionState()
    {
        bool hasDocument = _document?.Schema?.IsEditable == true;
        bool hasRecord = _records is { Count: > 0 } && _selectedRecordIndex >= 0;
        _btnNew.Enabled = hasDocument;
        _btnDelete.Enabled = hasDocument && hasRecord;
        _btnCopy.Enabled = hasDocument && hasRecord;
        _btnApply.Enabled = hasDocument && hasRecord;
        _btnUpdate.Enabled = hasDocument || _document is not null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _soundPlayer?.Dispose();
            _toolTip.Dispose();
        }
        base.Dispose(disposing);
    }
}
