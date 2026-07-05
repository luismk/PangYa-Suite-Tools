using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal sealed class RawRecordColumnDialog : Form
{
    private readonly int _recordSize;
    private readonly IffField _rawField;
    private readonly DataGridView _bytes = new();

    public IffFieldDefinition SelectedField { get; private set; } = null!;

    public RawRecordColumnDialog(int recordSize, IffField rawField, ReadOnlyMemory<byte> recordBytes,
        int defaultStringSize)
    {
        _recordSize = recordSize;
        _rawField = rawField;
        Text = Strings.IFFManager_DefineRawColumnTitle;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(420, 420);
        ClientSize = new Size(500, 560);

        _bytes.Dock = DockStyle.Fill;
        _bytes.AllowUserToAddRows = false;
        _bytes.AllowUserToDeleteRows = false;
        _bytes.AllowUserToResizeRows = false;
        _bytes.MultiSelect = true;
        _bytes.ReadOnly = true;
        _bytes.RowHeadersVisible = false;
        _bytes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _bytes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _bytes.Columns.Add("Offset", Strings.IFFManager_ByteOffset);
        _bytes.Columns.Add("Value", Strings.IFFManager_ByteValue);
        ReadOnlySpan<byte> record = recordBytes.Span;
        for (int index = 0; index < rawField.Width; index++)
        {
            int absoluteOffset = rawField.Offset + index;
            _bytes.Rows.Add(absoluteOffset, record[absoluteOffset].ToString("X2"));
        }
        _bytes.ClearSelection();

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 44, FlowDirection = FlowDirection.RightToLeft };
        var define = new Button { Text = Strings.IFFManager_DefineColumn, AutoSize = true };
        var cancel = new Button { Text = Strings.Options_Cancel, AutoSize = true, DialogResult = DialogResult.Cancel };
        define.Click += (_, _) => DefineColumn(defaultStringSize);
        buttons.Controls.AddRange([define, cancel]);
        Controls.Add(_bytes);
        Controls.Add(buttons);
        CancelButton = cancel;
    }

    private void DefineColumn(int defaultStringSize)
    {
        int[] selectedRows = _bytes.SelectedRows.Cast<DataGridViewRow>()
            .Select(row => row.Index).Order().ToArray();
        if (!TryGetSelection(selectedRows, _rawField, out int offset, out int width))
        {
            MessageBox.Show(Strings.IFFManager_SelectContiguousBytes, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        var template = new IffFieldDefinition($"Field{offset}", offset, width, IffFieldType.Raw,
            IsVisible: true);
        using var editor = new CustomIffColumnDialog(_recordSize, template, defaultStringSize,
            createFromTemplate: true);
        if (editor.ShowDialog(this) != DialogResult.OK) return;
        SelectedField = editor.FieldDefinition;
        DialogResult = DialogResult.OK;
    }

    internal static bool TryGetSelection(IEnumerable<int> selectedRows, IffField rawField,
        out int offset, out int width)
    {
        int[] rows = selectedRows.Distinct().Order().ToArray();
        bool contiguous = rows.Length > 0 && rows[^1] - rows[0] + 1 == rows.Length;
        offset = contiguous ? checked(rawField.Offset + rows[0]) : 0;
        width = contiguous ? rows.Length : 0;
        return contiguous && rows[0] >= 0 && rows[^1] < rawField.Width;
    }
}
