using System.Globalization;
using PangYa_Suite_Tools.Configuration;
using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal sealed class CustomIffColumnDialog : Form
{
    private sealed record SchemaEncodingOption(int? CodePage, string Label);
    private readonly int _recordSize;
    private readonly int? _previousFieldEnd;
    private readonly TextBox _name = new();
    private readonly NumericUpDown _offset = new();
    private readonly Button _usePreviousEnd = new();
    private readonly NumericUpDown _width = new();
    private readonly ComboBox _type = new();
    private readonly CheckBox _editable = new();
    private readonly CheckBox _visible = new();
    private readonly ComboBox _encoding = new();
    private readonly TextBox _minimum = new();
    private readonly TextBox _maximum = new();
    private readonly TextBox _bitMask = new();
    private readonly NumericUpDown _bitShift = new();
    private readonly ComboBox _referenceTargetFile = new();
    private readonly TextBox _referenceTargetKeyField = new();
    private readonly TextBox _referenceDisplayField = new();
    private readonly TextBox _referenceIconField = new();
    private readonly TextBox _iconPath = new();
    private readonly TextBox _soundPath = new();
    private readonly CheckBox _referencePickerEnabled = new();
    private TableLayoutPanel? _layout;
    private readonly Dictionary<int, Control[]> _rowControls = [];

    public IffFieldDefinition FieldDefinition { get; private set; } = null!;

    public CustomIffColumnDialog(int recordSize, IffFieldDefinition? field = null, int defaultStringSize = 32,
        int? initialOffset = null, int? previousFieldEnd = null, bool createFromTemplate = false,
        IEnumerable<string>? availableIffFiles = null, int defaultLongStringSize = 512)
    {
        if (recordSize <= 0) throw new ArgumentOutOfRangeException(nameof(recordSize));
        _recordSize = recordSize;
        _previousFieldEnd = previousFieldEnd;
        Text = field is null || createFromTemplate ? Strings.IFFManager_AddColumnTitle : Strings.IFFManager_EditColumnTitle;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 765);

        _offset.Maximum = recordSize - 1;
        _offset.Hexadecimal = false;
        _usePreviousEnd.Text = Strings.IFFManager_AfterPreviousField;
        _usePreviousEnd.AutoSize = true;
        _usePreviousEnd.Enabled = previousFieldEnd is >= 0 && previousFieldEnd < recordSize;
        _usePreviousEnd.Click += (_, _) => UsePreviousFieldEnd();
        _width.Minimum = 1;
        _width.Maximum = recordSize;
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        _type.Items.AddRange(Enum.GetValues<IffFieldType>().Cast<object>().ToArray());
        _referenceTargetFile.DropDownStyle = ComboBoxStyle.DropDown;
        foreach (string fileName in (availableIffFiles ?? []).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(name => name))
            _referenceTargetFile.Items.Add(fileName);
        _encoding.DropDownStyle = ComboBoxStyle.DropDownList;
        _encoding.DisplayMember = nameof(SchemaEncodingOption.Label);
        _encoding.Items.Add(new SchemaEncodingOption(null, Strings.IFFManager_EncodingDocumentDefault));
        foreach (PakEncodingOption option in IffStringEncodingPreferences.GetAvailableEncodings())
            _encoding.Items.Add(new SchemaEncodingOption(option.CodePage, option.Label));
        _editable.Text = Strings.IFFManager_FieldEditable;
        _editable.Checked = field?.IsEditable ?? true;
        _visible.Text = Strings.IFFManager_FieldVisible;
        _visible.Checked = field?.IsVisible ?? !(field?.Type == IffFieldType.Raw &&
            field.Offset == 0 && field.Width == recordSize &&
            field.Name.Equals("Raw record", StringComparison.OrdinalIgnoreCase));
        _bitShift.Maximum = 31;
        _offset.ValueChanged += (_, _) =>
        {
            _width.Maximum = recordSize - _offset.Value;
            if (_width.Value > _width.Maximum) _width.Value = _width.Maximum;
        };

        if (field is not null)
        {
            _name.Text = field.Name;
            _offset.Value = field.Offset;
            _width.Value = field.Width;
            _type.SelectedItem = field.Type;
            _encoding.SelectedItem = _encoding.Items.Cast<SchemaEncodingOption>()
                .FirstOrDefault(option => option.CodePage == field.EncodingCodePage) ?? _encoding.Items[0];
            _minimum.Text = field.Minimum?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            _maximum.Text = field.Maximum?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            _bitMask.Text = field.BitMask is uint mask ? $"0x{mask:X}" : string.Empty;
            _bitShift.Value = field.BitShift;
            SetReferenceTargetFile(field.Reference?.TargetFile ?? string.Empty);
            _referenceTargetKeyField.Text = field.Reference?.TargetKeyField ?? "ItemId";
            _referenceDisplayField.Text = field.Reference?.DisplayField ?? "Name";
            _referenceIconField.Text = field.Reference?.IconField ?? "Icon";
            _iconPath.Text = field.IconPath ?? string.Empty;
            _soundPath.Text = field.SoundPath ?? string.Empty;
            _referencePickerEnabled.Checked = field.Reference?.PickerEnabled ?? true;
        }
        else
        {
            _type.SelectedItem = IffFieldType.Raw;
            _encoding.SelectedIndex = 0;
            _offset.Value = Math.Clamp(initialOffset ?? 0, 0, recordSize - 1);
            _referenceTargetKeyField.Text = "ItemId";
            _referenceDisplayField.Text = "Name";
            _referenceIconField.Text = "Icon";
            _referencePickerEnabled.Checked = true;
        }
        IffFieldType previousType = SelectedType;
        _type.SelectedIndexChanged += (_, _) =>
        {
            if (field is null && SelectedType is IffFieldType.FixedString or IffFieldType.Icon or IffFieldType.Sound &&
                previousType is not (IffFieldType.FixedString or IffFieldType.Icon or IffFieldType.Sound))
                _width.Value = Math.Min(defaultStringSize, decimal.ToInt32(_width.Maximum));
            if (field is null && SelectedType == IffFieldType.LongString && previousType != IffFieldType.LongString)
                _width.Value = Math.Min(defaultLongStringSize, decimal.ToInt32(_width.Maximum));
            if (SelectedType == IffFieldType.ItemIdReference && previousType != IffFieldType.ItemIdReference)
            {
                _width.Value = Math.Min(4, decimal.ToInt32(_width.Maximum));
                if (string.IsNullOrWhiteSpace(_referenceTargetFile.Text))
                    SetReferenceTargetFile(_referenceTargetFile.Items.Cast<object>()
                        .Select(Convert.ToString)
                        .FirstOrDefault(item => string.Equals(item, "Item.iff", StringComparison.OrdinalIgnoreCase)) ?? "Item.iff");
            }
            previousType = SelectedType;
            UpdateTypeState();
        };
        UpdateTypeState();

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 19 };
        _layout = layout;
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(layout, 0, Strings.IFFManager_ColumnName, _name);
        var offsetPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = Padding.Empty };
        offsetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        offsetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _offset.Dock = DockStyle.Fill;
        offsetPanel.Controls.Add(_offset, 0, 0);
        offsetPanel.Controls.Add(_usePreviousEnd, 1, 0);
        AddRow(layout, 1, Strings.IFFManager_ByteOffset, offsetPanel);
        AddRow(layout, 2, Strings.IFFManager_ByteWidth, _width);
        AddRow(layout, 3, Strings.IFFManager_FieldType, _type);
        AddRow(layout, 4, string.Empty, _editable);
        AddRow(layout, 5, string.Empty, _visible);
        AddRow(layout, 6, Strings.IFFManager_EncodingCodePage, _encoding);
        AddRow(layout, 7, Strings.IFFManager_MinimumValue, _minimum);
        AddRow(layout, 8, Strings.IFFManager_MaximumValue, _maximum);
        AddRow(layout, 9, Strings.IFFManager_BitMask, _bitMask);
        AddRow(layout, 10, Strings.IFFManager_BitShift, _bitShift);
        AddRow(layout, 11, Strings.IFFManager_ReferenceTargetFile, _referenceTargetFile);
        AddRow(layout, 12, Strings.IFFManager_ReferenceTargetKeyField, _referenceTargetKeyField);
        AddRow(layout, 13, Strings.IFFManager_ReferenceDisplayField, _referenceDisplayField);
        AddRow(layout, 14, Strings.IFFManager_ReferenceIconField, _referenceIconField);
        AddRow(layout, 15, Strings.IFFManager_FieldIconPath, _iconPath);
        AddRow(layout, 16, Strings.IFFManager_FieldSoundPath, _soundPath);
        _referencePickerEnabled.Text = Strings.IFFManager_ReferencePickerEnabled;
        AddRow(layout, 17, string.Empty, _referencePickerEnabled);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var ok = new Button { Text = Strings.Common_OK, AutoSize = true };
        var cancel = new Button { Text = Strings.Options_Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
        ok.Click += (_, _) => AcceptField();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 18);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
        UpdateTypeState();
    }

    private void AcceptField()
    {
        if (string.IsNullOrWhiteSpace(_name.Text))
        {
            ShowValidation(Strings.IFFManager_ColumnNameRequired);
            return;
        }
        try
        {
            FieldDefinition = new IffFieldDefinition(
                _name.Text.Trim(), decimal.ToInt32(_offset.Value), decimal.ToInt32(_width.Value),
                (IffFieldType)_type.SelectedItem!, _editable.Checked,
                (SelectedType is IffFieldType.FixedString or IffFieldType.LongString or IffFieldType.Icon or IffFieldType.Sound ? (_encoding.SelectedItem as SchemaEncodingOption)?.CodePage : null),
                ParseNullableLong(_minimum.Text), ParseNullableLong(_maximum.Text),
                ParseNullableUInt(_bitMask.Text), decimal.ToInt32(_bitShift.Value), _visible.Checked,
                BuildReferenceDefinition(),
                SelectedType == IffFieldType.Icon && !string.IsNullOrWhiteSpace(_iconPath.Text)
                    ? _iconPath.Text.Trim()
                    : null,
                SelectedType == IffFieldType.Sound && !string.IsNullOrWhiteSpace(_soundPath.Text)
                    ? _soundPath.Text.Trim()
                    : null);
            var schema = new IffSchemaDefinition(1, "Validation.iff", "*", _recordSize, true, [FieldDefinition]);
            IffSchemaJson.ValidateDefinition(schema, _recordSize);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidDataException or ArgumentException)
        {
            ShowValidation(ex.Message);
        }
    }

    private void UsePreviousFieldEnd()
    {
        if (_previousFieldEnd is int offset) _offset.Value = offset;
    }

    private IffFieldType SelectedType => (IffFieldType)_type.SelectedItem!;

    private void UpdateTypeState()
    {
        bool isString = SelectedType is IffFieldType.FixedString or IffFieldType.LongString or IffFieldType.Icon or IffFieldType.Sound;
        bool isNumeric = SelectedType is IffFieldType.Byte or IffFieldType.UInt16 or IffFieldType.Int16 or
            IffFieldType.UInt32 or IffFieldType.ItemIdReference or IffFieldType.Int32 or IffFieldType.Single or
            IffFieldType.BitField;
        bool isBitField = SelectedType is IffFieldType.BitField or IffFieldType.BooleanBitField;
        bool isReference = SelectedType == IffFieldType.ItemIdReference || !string.IsNullOrWhiteSpace(_referenceTargetFile.Text);
        SetRowVisible(6, isString);
        SetRowVisible(7, isNumeric);
        SetRowVisible(8, isNumeric);
        SetRowVisible(9, isBitField);
        SetRowVisible(10, isBitField);
        SetRowVisible(11, isReference);
        SetRowVisible(12, isReference);
        SetRowVisible(13, isReference);
        SetRowVisible(14, isReference);
        SetRowVisible(15, SelectedType == IffFieldType.Icon);
        SetRowVisible(16, SelectedType == IffFieldType.Sound);
        SetRowVisible(17, isReference);
    }

    private IffFieldReferenceDefinition? BuildReferenceDefinition()
    {
        if (SelectedType != IffFieldType.ItemIdReference && string.IsNullOrWhiteSpace(_referenceTargetFile.Text))
            return null;

        return new IffFieldReferenceDefinition(
            _referenceTargetFile.Text.Trim(),
            string.IsNullOrWhiteSpace(_referenceTargetKeyField.Text) ? "ItemId" : _referenceTargetKeyField.Text.Trim(),
            string.IsNullOrWhiteSpace(_referenceDisplayField.Text) ? "Name" : _referenceDisplayField.Text.Trim(),
            string.IsNullOrWhiteSpace(_referenceIconField.Text) ? "Icon" : _referenceIconField.Text.Trim(),
            _referencePickerEnabled.Checked);
    }

    private void SetReferenceTargetFile(string value)
    {
        if (value.Length > 0 && !_referenceTargetFile.Items.Cast<object>()
                .Select(Convert.ToString)
                .Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
            _referenceTargetFile.Items.Add(value);
        _referenceTargetFile.Text = value;
    }

    private void SetRowVisible(int row, bool visible)
    {
        if (_layout is null || row >= _layout.RowStyles.Count) return;
        _layout.RowStyles[row].SizeType = SizeType.Absolute;
        _layout.RowStyles[row].Height = visible ? 38 : 0;
        if (_rowControls.TryGetValue(row, out Control[]? controls))
            foreach (Control control in controls)
            {
                control.Visible = visible;
                control.Enabled = visible;
            }
    }

    private static long? ParseNullableLong(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            ? checked((long)ulong.Parse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture))
            : long.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private static uint? ParseNullableUInt(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return uint.Parse(text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? text[2..] : text,
            text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? NumberStyles.HexNumber : NumberStyles.Integer,
            CultureInfo.InvariantCulture);
    }

    private static void ShowValidation(string message) => MessageBox.Show(message,
        Strings.IFFManager_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);

    private void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var labelControl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
        layout.Controls.Add(labelControl, 0, row);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 1, row);
        _rowControls[row] = [labelControl, control];
    }
}
