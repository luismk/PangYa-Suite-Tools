using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal sealed class IffSchemaManagerDialog : Form
{
    private readonly int _recordSize;
    private readonly List<IffFieldDefinition> _fields;
    private readonly IReadOnlyList<IffSchemaDefinition> _templateSchemas;
    private readonly ListBox _list = new() { Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed };
    private readonly NumericUpDown _defaultStringSize = new() { Minimum = 1, Dock = DockStyle.Fill };

    public IReadOnlyList<IffFieldDefinition> Fields => _fields;
    public int DefaultStringSize => decimal.ToInt32(_defaultStringSize.Value);

    public IffSchemaManagerDialog(int recordSize, IEnumerable<IffFieldDefinition> fields, int defaultStringSize = 32,
        IReadOnlyList<IffSchemaDefinition>? templateSchemas = null)
    {
        _recordSize = recordSize;
        _fields = [.. fields];
        _templateSchemas = templateSchemas ?? [];
        _defaultStringSize.Maximum = recordSize;
        _defaultStringSize.Value = Math.Clamp(defaultStringSize, 1, recordSize);
        _list.DrawItem += DrawFieldItem;
        Text = Strings.IFFManager_ManageColumnsTitle;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 380);
        ClientSize = new Size(900, 440);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42 };
        var add = new Button { Text = Strings.IFFManager_AddColumn, AutoSize = true };
        var clone = new Button { Text = Strings.IFFManager_CloneColumn, AutoSize = true };
        var edit = new Button { Text = Strings.IFFManager_EditColumn, AutoSize = true };
        var remove = new Button { Text = Strings.IFFManager_RemoveColumn, AutoSize = true };
        var up = new Button { Text = Strings.IFFManager_MoveUp, AutoSize = true };
        var down = new Button { Text = Strings.IFFManager_MoveDown, AutoSize = true };
        var sort = new Button { Text = Strings.IFFManager_SortByOffset, AutoSize = true };
        var save = new Button { Text = Strings.IFFManager_SaveSchema, AutoSize = true };
        var cancel = new Button { Text = Strings.Options_Cancel, AutoSize = true, DialogResult = DialogResult.Cancel };
        add.Click += (_, _) => AddField();
        clone.Click += (_, _) => CloneField();
        edit.Click += (_, _) => EditField();
        remove.Click += (_, _) => RemoveField();
        up.Click += (_, _) => MoveField(-1);
        down.Click += (_, _) => MoveField(1);
        sort.Click += (_, _) => SortFieldsByOffset();
        save.Click += (_, _) => DialogResult = DialogResult.OK;
        buttons.Controls.AddRange([add, clone, edit, remove, up, down, sort, save, cancel]);
        var settings = new TableLayoutPanel { Dock = DockStyle.Top, Height = 38, ColumnCount = 2, Padding = new Padding(6) };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        settings.Controls.Add(new Label { Text = Strings.IFFManager_DefaultStringSize, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        settings.Controls.Add(_defaultStringSize, 1, 0);
        Controls.Add(_list);
        Controls.Add(settings);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
        RefreshList();
    }

    private void AddField()
    {
        int selectedIndex = _list.SelectedIndex;
        int previousIndex = selectedIndex >= 0 ? selectedIndex : _fields.Count - 1;
        int initialOffset = previousIndex >= 0
            ? Math.Min(_recordSize - 1, checked(_fields[previousIndex].Offset + 1))
            : 0;
        using var dialog = new CustomIffColumnDialog(_recordSize, defaultStringSize: DefaultStringSize,
            initialOffset: initialOffset,
            previousFieldEnd: previousIndex >= 0
                ? checked(_fields[previousIndex].Offset + _fields[previousIndex].Width)
                : null);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!ValidateName(dialog.FieldDefinition.Name, -1)) return;
        int destination = previousIndex + 1;
        _fields.Insert(destination, dialog.FieldDefinition);
        RefreshList();
        _list.SelectedIndex = destination;
    }

    private void CloneField()
    {
        var currentSchema = new IffSchemaDefinition(IffSchemaJson.CurrentVersion,
            Strings.IFFManager_CurrentSchema, "*", _recordSize, true, _fields, DefaultStringSize);
        using var picker = new IffFieldTemplateDialog(_recordSize, [currentSchema, .. _templateSchemas]);
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        IffFieldDefinition source = picker.SelectedField;
        int index = _list.SelectedIndex;
        using var dialog = new CustomIffColumnDialog(_recordSize,
            source with { Name = source.Name + " Copy" }, DefaultStringSize,
            previousFieldEnd: index >= 0
                ? checked(_fields[index].Offset + _fields[index].Width)
                : null);
        if (dialog.ShowDialog(this) != DialogResult.OK || !ValidateName(dialog.FieldDefinition.Name, -1)) return;
        int destination = index < 0 ? _fields.Count : index + 1;
        _fields.Insert(destination, dialog.FieldDefinition);
        RefreshList();
        _list.SelectedIndex = destination;
    }

    private void MoveField(int direction)
    {
        int index = _list.SelectedIndex;
        int destination = index + direction;
        if (index < 0 || destination < 0 || destination >= _fields.Count) return;
        (_fields[index], _fields[destination]) = (_fields[destination], _fields[index]);
        RefreshList();
        _list.SelectedIndex = destination;
    }

    private void SortFieldsByOffset()
    {
        string? selectedName = _list.SelectedIndex >= 0 ? _fields[_list.SelectedIndex].Name : null;
        IffFieldDefinition[] sorted = SortByOffset(_fields);
        _fields.Clear();
        _fields.AddRange(sorted);
        RefreshList();
        if (selectedName is not null)
            _list.SelectedIndex = _fields.FindIndex(field =>
                field.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
    }

    internal static IffFieldDefinition[] SortByOffset(IEnumerable<IffFieldDefinition> fields) =>
        fields.Select((field, index) => (field, index))
            .OrderBy(item => IsRawRecord(item.field) ? 1 : 0)
            .ThenBy(item => item.field.Offset)
            .ThenBy(item => item.index)
            .Select(item => item.field)
            .ToArray();

    private static bool IsRawRecord(IffFieldDefinition field) =>
        field.Type == IffFieldType.Raw && field.Name.Equals("Raw record", StringComparison.OrdinalIgnoreCase);

    private void EditField()
    {
        int index = _list.SelectedIndex;
        if (index < 0) return;
        IffFieldDefinition selected = _fields[index];
        int? previousFieldEnd = index > 0
            ? checked(_fields[index - 1].Offset + _fields[index - 1].Width)
            : null;
        using var dialog = new CustomIffColumnDialog(_recordSize, selected,
            previousFieldEnd: previousFieldEnd);
        if (dialog.ShowDialog(this) != DialogResult.OK || !ValidateName(dialog.FieldDefinition.Name, index)) return;
        try
        {
            IReadOnlyList<IffFieldDefinition> adjusted = AdjustFollowingOffsets(
                _fields, index, dialog.FieldDefinition, _recordSize, DefaultStringSize);
            _fields.Clear();
            _fields.AddRange(adjusted);
        }
        catch (InvalidDataException ex)
        {
            MessageBox.Show(ex.Message, Strings.IFFManager_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        RefreshList();
        _list.SelectedIndex = index;
    }

    internal static IReadOnlyList<IffFieldDefinition> AdjustFollowingOffsets(
        IReadOnlyList<IffFieldDefinition> fields,
        int editedIndex,
        IffFieldDefinition replacement,
        int recordSize,
        int defaultStringSize)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if ((uint)editedIndex >= (uint)fields.Count) throw new ArgumentOutOfRangeException(nameof(editedIndex));
        IffFieldDefinition original = fields[editedIndex];
        int widthDelta = replacement.Width - original.Width;
        int originalEnd = checked(original.Offset + original.Width);
        IffFieldDefinition[] adjusted = fields.Select((field, index) =>
        {
            if (index == editedIndex) return replacement;
            bool followsEditedRange = index > editedIndex && field.Offset >= originalEnd;
            bool catchAllRaw = field.Type == IffFieldType.Raw && field.Offset == 0 &&
                field.Width == recordSize && field.Name.Equals("Raw record", StringComparison.OrdinalIgnoreCase);
            return widthDelta != 0 && followsEditedRange && !catchAllRaw
                ? field with { Offset = checked(field.Offset + widthDelta) }
                : field;
        }).ToArray();
        adjusted = FitTrailingFieldToRecord(adjusted, recordSize);
        var definition = new IffSchemaDefinition(IffSchemaJson.CurrentVersion, "Validation.iff", "*",
            recordSize, true, adjusted, defaultStringSize);
        IffSchemaJson.ValidateDefinition(definition, recordSize);
        return adjusted;
    }

    internal static IReadOnlyList<IffFieldDefinition> MoveFieldAndFollowingOffsets(
        IReadOnlyList<IffFieldDefinition> fields,
        int fieldIndex,
        int offsetDelta,
        int recordSize,
        int defaultStringSize)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if ((uint)fieldIndex >= (uint)fields.Count) throw new ArgumentOutOfRangeException(nameof(fieldIndex));
        IffFieldDefinition[] adjusted = fields.Select((field, index) =>
        {
            bool catchAllRaw = field.Type == IffFieldType.Raw && field.Offset == 0 &&
                field.Width == recordSize && field.Name.Equals("Raw record", StringComparison.OrdinalIgnoreCase);
            return index >= fieldIndex && !catchAllRaw
                ? field with { Offset = checked(field.Offset + offsetDelta) }
                : field;
        }).ToArray();
        adjusted = FitTrailingFieldToRecord(adjusted, recordSize);
        var definition = new IffSchemaDefinition(IffSchemaJson.CurrentVersion, "Validation.iff", "*",
            recordSize, true, adjusted, defaultStringSize);
        IffSchemaJson.ValidateDefinition(definition, recordSize);
        return adjusted;
    }

    internal static IReadOnlyList<IffFieldDefinition> ReplaceFieldWithoutAdjustingFollowing(
        IReadOnlyList<IffFieldDefinition> fields,
        int fieldIndex,
        IffFieldDefinition replacement,
        int recordSize,
        int defaultStringSize)
    {
        ArgumentNullException.ThrowIfNull(fields);
        if ((uint)fieldIndex >= (uint)fields.Count) throw new ArgumentOutOfRangeException(nameof(fieldIndex));
        IffFieldDefinition[] adjusted = fields.Select((field, index) =>
            index == fieldIndex ? replacement : field).ToArray();
        var definition = new IffSchemaDefinition(IffSchemaJson.CurrentVersion, "Validation.iff", "*",
            recordSize, true, adjusted, defaultStringSize);
        IffSchemaJson.ValidateDefinition(definition, recordSize);
        return adjusted;
    }

    private static IffFieldDefinition[] FitTrailingFieldToRecord(
        IffFieldDefinition[] fields, int recordSize)
    {
        for (int index = 0; index < fields.Length; index++)
        {
            IffFieldDefinition trailing = fields[index];
            if (IsCatchAllRaw(trailing, recordSize)) continue;
            int overflow = checked(trailing.Offset + trailing.Width - recordSize);
            if (overflow <= 0) continue;
            int reducedWidth = trailing.Width - overflow;
            if (reducedWidth <= 0)
                throw new InvalidDataException($"Field '{trailing.Name}' cannot absorb the {overflow}-byte record overflow.");
            fields[index] = NormalizeFieldWidth(trailing, reducedWidth);
        }
        return fields;
    }

    private static bool IsCatchAllRaw(IffFieldDefinition field, int recordSize) =>
        field.Type == IffFieldType.Raw && field.Offset == 0 && field.Width == recordSize &&
        field.Name.Equals("Raw record", StringComparison.OrdinalIgnoreCase);

    private static IffFieldDefinition NormalizeFieldWidth(IffFieldDefinition field, int width)
    {
        IffFieldType type = field.Type switch
        {
            IffFieldType.UInt32 when width == 2 => IffFieldType.UInt16,
            IffFieldType.UInt32 or IffFieldType.UInt16 when width == 1 => IffFieldType.Byte,
            IffFieldType.Int32 when width == 2 => IffFieldType.Int16,
            IffFieldType.FixedString or IffFieldType.Raw or IffFieldType.ByteRangeBoolean => field.Type,
            IffFieldType.ZeroBoolean when width is 1 or 2 or 4 => field.Type,
            IffFieldType.BooleanBitField when width is 1 or 2 or 4 &&
                field.BitMask is uint mask && (mask & ~(width == 1 ? byte.MaxValue : width == 2 ? ushort.MaxValue : uint.MaxValue)) == 0 => field.Type,
            _ => IffFieldType.Raw
        };
        bool keepBitDefinition = type is IffFieldType.BitField or IffFieldType.BooleanBitField;
        return field with
        {
            Width = width,
            Type = type,
            EncodingCodePage = type == IffFieldType.FixedString ? field.EncodingCodePage : null,
            BitMask = keepBitDefinition ? field.BitMask : null,
            BitShift = keepBitDefinition ? field.BitShift : 0
        };
    }

    private void RemoveField()
    {
        int index = _list.SelectedIndex;
        if (index < 0) return;
        _fields.RemoveAt(index);
        RefreshList();
    }

    private bool ValidateName(string name, int excludedIndex)
    {
        bool duplicate = _fields.Where((_, index) => index != excludedIndex)
            .Any(field => field.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!duplicate) return true;
        MessageBox.Show(Strings.IFFManager_DuplicateColumnName, Strings.IFFManager_Error,
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

    private void RefreshList()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (IffFieldDefinition field in _fields)
            _list.Items.Add($"{field.Name} — {field.Type}, {field.Offset}, {field.Width} byte(s)");
        _list.EndUpdate();
    }

    private void DrawFieldItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _list.Items.Count) return;
        bool selected = e.State.HasFlag(DrawItemState.Selected);
        bool overlaps = FindOverlappingFields(_fields, _recordSize)[e.Index];
        Color background = overlaps
            ? selected ? Color.Goldenrod : Color.LightYellow
            : selected ? SystemColors.Highlight : _list.BackColor;
        Color foreground = selected && !overlaps ? SystemColors.HighlightText : _list.ForeColor;
        using var brush = new SolidBrush(background);
        e.Graphics.FillRectangle(brush, e.Bounds);
        TextRenderer.DrawText(e.Graphics, _list.Items[e.Index]?.ToString() ?? string.Empty,
            e.Font ?? _list.Font, e.Bounds, foreground,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        e.DrawFocusRectangle();
    }

    internal static bool[] FindOverlappingFields(IReadOnlyList<IffFieldDefinition> fields, int recordSize)
    {
        bool[] overlaps = new bool[fields.Count];
        for (int left = 0; left < fields.Count; left++)
        {
            if (IsCatchAllRaw(fields[left], recordSize)) continue;
            int leftEnd = checked(fields[left].Offset + fields[left].Width);
            for (int right = left + 1; right < fields.Count; right++)
            {
                if (IsCatchAllRaw(fields[right], recordSize)) continue;
                int rightEnd = checked(fields[right].Offset + fields[right].Width);
                if (fields[left].Offset < rightEnd && fields[right].Offset < leftEnd)
                    overlaps[left] = overlaps[right] = true;
            }
        }
        return overlaps;
    }
}
