using System.Globalization;
using PangYa_Suite_Tools.Configuration;
using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal sealed class CustomIffColumnDialog : Form
{
    private sealed record SchemaEncodingOption(int? CodePage, string Label);
    private readonly int _recordSize;
    private readonly TextBox _name = new();
    private readonly NumericUpDown _offset = new();
    private readonly NumericUpDown _width = new();
    private readonly ComboBox _type = new();
    private readonly CheckBox _editable = new();
    private readonly ComboBox _encoding = new();
    private readonly TextBox _minimum = new();
    private readonly TextBox _maximum = new();
    private readonly TextBox _bitMask = new();
    private readonly NumericUpDown _bitShift = new();

    public IffFieldDefinition FieldDefinition { get; private set; } = null!;

    public CustomIffColumnDialog(int recordSize, IffFieldDefinition? field = null)
    {
        if (recordSize <= 0) throw new ArgumentOutOfRangeException(nameof(recordSize));
        _recordSize = recordSize;
        Text = field is null ? Strings.IFFManager_AddColumnTitle : Strings.IFFManager_EditColumnTitle;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(470, 455);

        _offset.Maximum = recordSize - 1;
        _offset.Hexadecimal = true;
        _width.Minimum = 1;
        _width.Maximum = recordSize;
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        _type.Items.AddRange(Enum.GetValues<IffFieldType>().Cast<object>().ToArray());
        _encoding.DropDownStyle = ComboBoxStyle.DropDownList;
        _encoding.DisplayMember = nameof(SchemaEncodingOption.Label);
        _encoding.Items.Add(new SchemaEncodingOption(null, Strings.IFFManager_EncodingDocumentDefault));
        foreach (PakEncodingOption option in IffStringEncodingPreferences.GetAvailableEncodings())
            _encoding.Items.Add(new SchemaEncodingOption(option.CodePage, option.Label));
        _editable.Text = Strings.IFFManager_FieldEditable;
        _editable.Checked = field?.IsEditable ?? true;
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
        }
        else
        {
            _type.SelectedItem = IffFieldType.Raw;
            _encoding.SelectedIndex = 0;
        }
        _type.SelectedIndexChanged += (_, _) => UpdateEncodingState();
        UpdateEncodingState();

        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 2, RowCount = 11 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddRow(layout, 0, Strings.IFFManager_ColumnName, _name);
        AddRow(layout, 1, Strings.IFFManager_ByteOffset, _offset);
        AddRow(layout, 2, Strings.IFFManager_ByteWidth, _width);
        AddRow(layout, 3, Strings.IFFManager_FieldType, _type);
        AddRow(layout, 4, string.Empty, _editable);
        AddRow(layout, 5, Strings.IFFManager_EncodingCodePage, _encoding);
        AddRow(layout, 6, Strings.IFFManager_MinimumValue, _minimum);
        AddRow(layout, 7, Strings.IFFManager_MaximumValue, _maximum);
        AddRow(layout, 8, Strings.IFFManager_BitMask, _bitMask);
        AddRow(layout, 9, Strings.IFFManager_BitShift, _bitShift);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var ok = new Button { Text = Strings.Common_OK, AutoSize = true };
        var cancel = new Button { Text = Strings.Options_Cancel, DialogResult = DialogResult.Cancel, AutoSize = true };
        ok.Click += (_, _) => AcceptField();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 0, 10);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = ok;
        CancelButton = cancel;
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
                (SelectedType == IffFieldType.FixedString ? (_encoding.SelectedItem as SchemaEncodingOption)?.CodePage : null),
                ParseNullableLong(_minimum.Text), ParseNullableLong(_maximum.Text),
                ParseNullableUInt(_bitMask.Text), decimal.ToInt32(_bitShift.Value));
            var schema = new IffSchemaDefinition(1, "Validation.iff", "*", _recordSize, true, [FieldDefinition]);
            IffSchemaJson.ValidateDefinition(schema, _recordSize);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or InvalidDataException or ArgumentException)
        {
            ShowValidation(ex.Message);
        }
    }

    private IffFieldType SelectedType => (IffFieldType)_type.SelectedItem!;

    private void UpdateEncodingState()
    {
        _encoding.Enabled = SelectedType == IffFieldType.FixedString;
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

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(control, 1, row);
    }
}
