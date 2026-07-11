using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;
using System.Text;

namespace PangYa_Suite_Tools;

internal sealed class RawRecordColumnDialog : Form
{
    private const int BytesPerRow = 16;
    private readonly int _recordSize;
    private readonly IffSchema? _schema;
    private readonly int _defaultStringSize;
    private readonly int _defaultLongStringSize;
    private readonly ReadOnlyMemory<byte> _recordBytes;
    private readonly Encoding? _stringEncoding;
    private readonly DataGridView _bytes = new();
    private readonly ComboBox _fieldType = new();
    private readonly TextBox _selectedValue = new();
    private bool _dragging;
    private int _dragStartOffset = -1;

    public IffFieldDefinition SelectedField { get; private set; } = null!;

    public RawRecordColumnDialog(int recordSize, ReadOnlyMemory<byte> recordBytes,
        int defaultStringSize, IffSchema? schema = null, Encoding? stringEncoding = null,
        int defaultLongStringSize = 512)
    {
        if (recordSize <= 0) throw new ArgumentOutOfRangeException(nameof(recordSize));
        _recordSize = recordSize;
        _schema = schema;
        _defaultStringSize = defaultStringSize;
        _defaultLongStringSize = defaultLongStringSize;
        _recordBytes = recordBytes;
        _stringEncoding = stringEncoding;
        Text = Strings.IFFManager_DefineRawColumnTitle;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(620, 420);
        ClientSize = new Size(760, 560);

        _fieldType.DropDownStyle = ComboBoxStyle.DropDownList;
        _fieldType.Name = "cboRawFieldType";
        _fieldType.Items.AddRange(Enum.GetValues<IffFieldType>().Cast<object>().ToArray());
        _fieldType.SelectedItem = IffFieldType.Raw;
        _fieldType.SelectedIndexChanged += (_, _) => ReselectFromCurrentStart();

        _selectedValue.ReadOnly = true;
        _selectedValue.Name = "txtSelectedRawValue";
        _selectedValue.Dock = DockStyle.Fill;
        _selectedValue.PlaceholderText = Strings.IFFManager_SelectedRawValue;

        var options = new TableLayoutPanel { Dock = DockStyle.Top, Height = 38, ColumnCount = 4, Padding = new Padding(6) };
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        options.Controls.Add(new Label { Text = Strings.IFFManager_FieldType, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        options.Controls.Add(_fieldType, 1, 0);
        options.Controls.Add(new Label { Text = Strings.IFFManager_SelectedRawValueLabel, AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
        options.Controls.Add(_selectedValue, 3, 0);

        _bytes.Dock = DockStyle.Fill;
        _bytes.Name = "gridRawBytes";
        _bytes.AllowUserToAddRows = false;
        _bytes.AllowUserToDeleteRows = false;
        _bytes.AllowUserToResizeRows = false;
        _bytes.MultiSelect = true;
        _bytes.ReadOnly = true;
        _bytes.RowHeadersVisible = true;
        _bytes.RowHeadersWidth = 70;
        _bytes.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _bytes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _bytes.CellMouseDown += Bytes_CellMouseDown;
        _bytes.CellMouseEnter += Bytes_CellMouseEnter;
        _bytes.MouseUp += (_, _) =>
        {
            _dragging = false;
            UpdateSelectedValue();
        };
        _bytes.CellFormatting += Bytes_CellFormatting;
        for (int column = 0; column < BytesPerRow; column++)
        {
            _bytes.Columns.Add("B" + column.ToString("X"), column.ToString("X"));
            _bytes.Columns[column].Width = 42;
            _bytes.Columns[column].SortMode = DataGridViewColumnSortMode.NotSortable;
        }
        ReadOnlySpan<byte> record = recordBytes.Span;
        if (record.Length < recordSize)
            throw new ArgumentException("The record byte buffer is shorter than the record size.", nameof(recordBytes));
        int rowCount = (recordSize + BytesPerRow - 1) / BytesPerRow;
        for (int row = 0; row < rowCount; row++)
        {
            int rowIndex = _bytes.Rows.Add();
            int rowOffset = row * BytesPerRow;
            _bytes.Rows[rowIndex].HeaderCell.Value = rowOffset.ToString("X4");
            for (int column = 0; column < BytesPerRow; column++)
            {
                int rawIndex = row * BytesPerRow + column;
                if (rawIndex >= recordSize)
                {
                    _bytes.Rows[rowIndex].Cells[column].ReadOnly = true;
                    _bytes.Rows[rowIndex].Cells[column].Style.BackColor = SystemColors.ControlDark;
                    continue;
                }
                int absoluteOffset = rawIndex;
                _bytes.Rows[rowIndex].Cells[column].Value = record[absoluteOffset].ToString("X2");
                _bytes.Rows[rowIndex].Cells[column].Tag = absoluteOffset;
            }
        }
        _bytes.ClearSelection();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft };
        var define = new Button { Text = Strings.IFFManager_DefineColumn, AutoSize = true };
        var clear = new Button { Text = Strings.IFFManager_ClearSelection, AutoSize = true };
        var cancel = new Button { Text = Strings.Options_Cancel, AutoSize = true, DialogResult = DialogResult.Cancel };
        define.Click += (_, _) => DefineColumn(defaultStringSize);
        clear.Click += (_, _) => ClearSelection();
        buttons.Controls.AddRange([define, clear, cancel]);
        Controls.Add(_bytes);
        Controls.Add(options);
        Controls.Add(buttons);
        CancelButton = cancel;
    }

    private void DefineColumn(int defaultStringSize)
    {
        int[] selectedOffsets = _bytes.SelectedCells.Cast<DataGridViewCell>()
            .Select(cell => CellOffset(cell))
            .OfType<int>()
            .ToArray();
        if (!TryGetSelection(selectedOffsets, _recordSize, out int offset, out int width))
        {
            MessageBox.Show(Strings.IFFManager_SelectContiguousBytes, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var template = new IffFieldDefinition($"Field{offset}", offset, width, SelectedFieldType(),
            IsVisible: true);
        using var editor = new CustomIffColumnDialog(_recordSize, template, defaultStringSize,
            createFromTemplate: true);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        SelectedField = editor.FieldDefinition;
        DialogResult = DialogResult.OK;
    }

    private void Bytes_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        int? offset = CellOffset(_bytes[e.ColumnIndex, e.RowIndex]);
        if (offset is not int start) return;
        if (_bytes[e.ColumnIndex, e.RowIndex].Selected && _bytes.SelectedCells.Count > 0)
        {
            ClearSelection();
            return;
        }
        _dragging = true;
        _dragStartOffset = start;
        SelectByteRange(start, TypeSelectionEnd(start));
    }

    private void Bytes_CellMouseEnter(object? sender, DataGridViewCellEventArgs e)
    {
        if (!_dragging || e.RowIndex < 0 || e.ColumnIndex < 0) return;
        int? offset = CellOffset(_bytes[e.ColumnIndex, e.RowIndex]);
        if (offset is int end) SelectByteRange(_dragStartOffset, end);
    }

    private void SelectByteRange(int startOffset, int endOffset)
    {
        int start = Math.Min(startOffset, endOffset);
        int end = Math.Max(startOffset, endOffset);
        _bytes.ClearSelection();
        foreach (DataGridViewRow row in _bytes.Rows)
        {
            foreach (DataGridViewCell cell in row.Cells)
            {
                int? offset = CellOffset(cell);
                if (offset is >= 0 && offset >= start && offset <= end) cell.Selected = true;
            }
        }
        UpdateSelectedValue();
    }

    private void ReselectFromCurrentStart()
    {
        int[] offsets = SelectedOffsets();
        if (offsets.Length == 0) return;
        SelectByteRange(offsets.Min(), TypeSelectionEnd(offsets.Min()));
    }

    private void ClearSelection()
    {
        _dragging = false;
        _dragStartOffset = -1;
        _bytes.ClearSelection();
        UpdateSelectedValue();
    }

    private void UpdateSelectedValue()
    {
        int[] offsets = SelectedOffsets();
        if (offsets.Length == 0 || !TryGetSelection(offsets, _recordSize, out int offset, out int width))
        {
            _selectedValue.Text = string.Empty;
            return;
        }

        string value = IsStringFieldType(SelectedFieldType())
            ? DecodeSelectedString(offset, width)
            : string.Join(" ", offsets.Order()
                .Select(selectedOffset => FindCellByOffset(selectedOffset)?.Value?.ToString())
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        _selectedValue.Text = string.Format(LocalizationManager.CurrentCulture,
            Strings.IFFManager_SelectedRawValueFormat, offset, width, value);
    }

    private void Bytes_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        DataGridViewCell cell = _bytes[e.ColumnIndex, e.RowIndex];
        if (CellOffset(cell) is not int offset) return;
        (int? group, bool overlaps) = RawByteFieldVisual(_schema, _recordSize, offset);
        ResetByteCellStyle(e.CellStyle, _bytes.Font);
        if (group is not int groupIndex) return;
        Color band = overlaps ? Color.Red : RawFieldColor(groupIndex);
        e.CellStyle.BackColor = Color.FromArgb(overlaps ? 120 : 64, band);
        e.CellStyle.SelectionBackColor = Color.FromArgb(180, band);
        e.CellStyle.SelectionForeColor = Color.Black;
    }

    private string DecodeSelectedString(int offset, int width)
    {
        var selected = new IffField("Selected", offset, width, SelectedFieldType(), false);
        return Convert.ToString(selected.GetValue(_recordBytes.Span, _stringEncoding),
            LocalizationManager.CurrentCulture) ?? string.Empty;
    }

    private static int? CellOffset(DataGridViewCell cell) =>
        cell.Tag is int offset ? offset : null;

    private DataGridViewCell? FindCellByOffset(int offset)
    {
        foreach (DataGridViewRow row in _bytes.Rows)
        foreach (DataGridViewCell cell in row.Cells)
            if (CellOffset(cell) == offset) return cell;
        return null;
    }

    private int[] SelectedOffsets() => _bytes.SelectedCells.Cast<DataGridViewCell>()
        .Select(CellOffset)
        .OfType<int>()
        .Distinct()
        .Order()
        .ToArray();

    private IffFieldType SelectedFieldType() =>
        _fieldType.SelectedItem is IffFieldType type ? type : IffFieldType.Raw;

    private static bool IsStringFieldType(IffFieldType type) =>
        type is IffFieldType.FixedString or IffFieldType.LongString or IffFieldType.Icon or IffFieldType.Sound;

    private int TypeSelectionEnd(int startOffset)
    {
        int rawEnd = checked(_recordSize - 1);
        int width = PreferredWidth(SelectedFieldType(), _defaultStringSize, _defaultLongStringSize);
        return Math.Min(rawEnd, checked(startOffset + width - 1));
    }

    internal static int PreferredWidth(IffFieldType type, int defaultStringSize, int defaultLongStringSize = 512) => type switch
    {
        IffFieldType.Boolean or IffFieldType.Byte or IffFieldType.BooleanBitField or
            IffFieldType.ZeroBoolean or IffFieldType.ByteRangeBoolean => 1,
        IffFieldType.UInt16 or IffFieldType.Int16 => 2,
        IffFieldType.UInt32 or IffFieldType.ItemIdReference or IffFieldType.Int32 or
            IffFieldType.Single => 4,
        IffFieldType.DateTime => 16,
        IffFieldType.FixedString or IffFieldType.Icon or IffFieldType.Sound => Math.Max(1, defaultStringSize),
        IffFieldType.LongString => Math.Max(1, defaultLongStringSize),
        _ => 1
    };

    internal static bool TryGetSelection(IEnumerable<int> selectedOffsets, int recordSize,
        out int offset, out int width)
    {
        int[] offsets = selectedOffsets.Distinct().Order().ToArray();
        bool contiguous = offsets.Length > 0 && offsets[^1] - offsets[0] + 1 == offsets.Length;
        offset = contiguous ? offsets[0] : 0;
        width = contiguous ? offsets.Length : 0;
        return contiguous && offsets[0] >= 0 && offsets[^1] < recordSize;
    }

    internal static (int? Group, bool Overlaps) RawByteFieldVisual(IffSchema? schema,
        int recordSize, int recordOffset)
    {
        if (schema is null) return (null, false);
        int? group = null;
        int matches = 0;
        for (int index = 0; index < schema.Fields.Count; index++)
        {
            IffField field = schema.Fields[index];
            if (IffSchemaCoverage.IsCatchAllRawRecord(field, recordSize)) continue;
            if (recordOffset >= field.Offset && recordOffset < field.Offset + field.Width)
            {
                group ??= index;
                matches++;
            }
        }
        return (group, matches > 1);
    }

    internal static Color RawFieldColor(int index)
    {
        Color[] palette =
        [
            Color.DodgerBlue, Color.OrangeRed, Color.MediumSeaGreen, Color.MediumPurple,
            Color.Goldenrod, Color.DeepPink, Color.Teal, Color.SlateBlue
        ];
        return palette[index % palette.Length];
    }

    internal static void ResetByteCellStyle(DataGridViewCellStyle style, Font font)
    {
        style.Font = font;
        style.BackColor = SystemColors.Window;
        style.ForeColor = SystemColors.WindowText;
        style.SelectionBackColor = SystemColors.Highlight;
        style.SelectionForeColor = SystemColors.HighlightText;
    }
}
