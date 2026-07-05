using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;

namespace PangYa_Suite_Tools;

internal sealed class DataGridViewDateTimePickerColumn : DataGridViewColumn
{
    public DataGridViewDateTimePickerColumn() : base(new DataGridViewDateTimePickerCell()) =>
        DefaultCellStyle.Format = "g";
}

internal sealed class DataGridViewDateTimePickerCell : DataGridViewTextBoxCell
{
    public override Type EditType => typeof(DataGridViewDateTimePickerEditingControl);
    public override Type ValueType => typeof(object);
    public override object DefaultNewRowValue => string.Empty;

    public override void InitializeEditingControl(int rowIndex, object? initialFormattedValue,
        DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.InitializeEditingControl(rowIndex, initialFormattedValue, dataGridViewCellStyle);
        if (DataGridView?.EditingControl is not DataGridViewDateTimePickerEditingControl picker) return;
        object? value = DataGridView.Rows[rowIndex].Cells[ColumnIndex].Value ?? initialFormattedValue;
        if (value is DateTime date)
        {
            picker.InitializeValue(date);
        }
        else if (DateTime.TryParse(Convert.ToString(value, CultureInfo.CurrentCulture),
            CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime parsed))
        {
            picker.InitializeValue(parsed);
        }
        else
        {
            picker.InitializeValue(null);
        }
    }
}

internal sealed class DataGridViewDateTimePickerEditingControl : DateTimePicker, IDataGridViewEditingControl
{
    private bool _valueChanged;
    private bool _initializing;

    public DataGridViewDateTimePickerEditingControl()
    {
        Format = DateTimePickerFormat.Custom;
        ShowCheckBox = true;
        Checked = false;
        UpdateFormat();
    }

    [AllowNull]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public object EditingControlFormattedValue
    {
        get => Checked ? Value : string.Empty;
        set
        {
            if (value is DateTime date)
            {
                Value = date;
                Checked = true;
            }
            else
            {
                Checked = false;
            }
            UpdateFormat();
        }
    }

    public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context) =>
        EditingControlFormattedValue;

    public void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle)
    {
        Font = dataGridViewCellStyle.Font ?? Font;
        CalendarForeColor = dataGridViewCellStyle.ForeColor;
        CalendarMonthBackground = dataGridViewCellStyle.BackColor;
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int EditingControlRowIndex { get; set; }
    public bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right or Keys.Up or Keys.Down or Keys.Home or Keys.End or
            Keys.PageUp or Keys.PageDown;
    public void PrepareEditingControlForEdit(bool selectAll) { }
    public bool RepositionEditingControlOnValueChange => false;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public DataGridView? EditingControlDataGridView { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool EditingControlValueChanged { get => _valueChanged; set => _valueChanged = value; }
    public Cursor EditingPanelCursor => Cursors.Default;

    protected override void OnValueChanged(EventArgs eventargs)
    {
        base.OnValueChanged(eventargs);
        UpdateFormat();
        NotifyChanged();
    }

    internal void InitializeValue(DateTime? value)
    {
        _initializing = true;
        try
        {
            Value = value ?? DateTime.Today;
            Checked = value.HasValue;
            UpdateFormat();
            _valueChanged = false;
        }
        finally { _initializing = false; }
    }

    internal void UpdateFormat()
    {
        DateTimeFormatInfo format = CultureInfo.CurrentCulture.DateTimeFormat;
        CustomFormat = Checked ? $"{format.ShortDatePattern} {format.ShortTimePattern}" : " ";
    }

    private void NotifyChanged()
    {
        if (_initializing) return;
        _valueChanged = true;
        EditingControlDataGridView?.NotifyCurrentCellDirty(true);
    }
}
