using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal sealed class IffSchemaManagerDialog : Form
{
    private sealed record BaseOption(string Label, IffSchemaBaseReference? Reference,
        IReadOnlyList<IffFieldDefinition> Fields, IffSchemaDefinition? Definition = null)
    {
        public override string ToString() => Label;
    }

    private readonly int _recordSize;
    private readonly List<IffFieldDefinition> _fields;
    private readonly List<IffFieldDefinition> _inheritedFields = [];
    private readonly IReadOnlyList<IffSchemaDefinition> _templateSchemas;
    private readonly IReadOnlyList<string> _availableIffFiles;
    private readonly ListBox _list = new() { Dock = DockStyle.Fill, DrawMode = DrawMode.OwnerDrawFixed };
    private readonly NumericUpDown _defaultStringSize = new() { Minimum = 1, Dock = DockStyle.Fill };
    private readonly NumericUpDown _defaultLongStringSize = new() { Minimum = 1, Maximum = 65535, Dock = DockStyle.Fill };
    private readonly ComboBox _baseSchema = new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
    private readonly Button _editBase = new() { AutoSize = true };
    private readonly Action<IffSchemaDefinition>? _saveBase;

    public IReadOnlyList<IffFieldDefinition> Fields => _fields;
    public int DefaultStringSize => decimal.ToInt32(_defaultStringSize.Value);
    public int DefaultLongStringSize => decimal.ToInt32(_defaultLongStringSize.Value);
    public IffSchemaBaseReference? BaseReference => (_baseSchema.SelectedItem as BaseOption)?.Reference;

    public IffSchemaManagerDialog(int recordSize, IEnumerable<IffFieldDefinition> fields, int defaultStringSize = 32,
        IReadOnlyList<IffSchemaDefinition>? templateSchemas = null,
        IReadOnlyList<string>? availableIffFiles = null, int defaultLongStringSize = 512,
        IffSchemaBaseReference? baseReference = null,
        IEnumerable<IffFieldDefinition>? inheritedFields = null,
        string? resolvedRegion = null,
        Action<IffSchemaDefinition>? saveBase = null)
    {
        _recordSize = recordSize;
        _fields = [.. fields.Where(field => !IsCatchAllRaw(field, recordSize))];
        _templateSchemas = templateSchemas ?? [];
        _availableIffFiles = availableIffFiles ?? [];
        _saveBase = saveBase;
        _inheritedFields.AddRange((inheritedFields ?? []).Where(field => !IsCatchAllRaw(field, recordSize)));
        _defaultStringSize.Maximum = recordSize;
        _defaultStringSize.Value = Math.Clamp(defaultStringSize, 1, recordSize);
        _defaultLongStringSize.Value = Math.Clamp(defaultLongStringSize, 1, 65535);
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
        save.Click += (_, _) => SaveIfValid(showMessage: true);
        buttons.Controls.AddRange([add, clone, edit, remove, up, down, sort, save, cancel]);
        _editBase.Text = Strings.IFFManager_EditBase;
        _editBase.Click += (_, _) => EditBaseSchema();
        InitializeBaseOptions(baseReference, resolvedRegion);
        var settings = new TableLayoutPanel { Dock = DockStyle.Top, Height = 70, ColumnCount = 4, RowCount = 2, Padding = new Padding(6) };
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        settings.Controls.Add(new Label { Text = Strings.IFFManager_DefaultStringSize, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        settings.Controls.Add(_defaultStringSize, 1, 0);
        settings.Controls.Add(new Label { Text = Strings.IFFManager_DefaultLongStringSize, AutoSize = true, Anchor = AnchorStyles.Left }, 2, 0);
        settings.Controls.Add(_defaultLongStringSize, 3, 0);
        settings.Controls.Add(new Label { Text = Strings.IFFManager_BaseSchema, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
        settings.Controls.Add(_baseSchema, 1, 1);
        settings.Controls.Add(_editBase, 2, 1);
        Controls.Add(_list);
        Controls.Add(settings);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
        RefreshList();
    }

    private void InitializeBaseOptions(IffSchemaBaseReference? selected, string? resolvedRegion)
    {
        _baseSchema.Items.Add(new BaseOption(Strings.IFFManager_BaseNone, null, []));
        IffSchemaDefinition? autoDefinition = _templateSchemas.FirstOrDefault(definition =>
            definition.FileName.Equals("Common.iff", StringComparison.OrdinalIgnoreCase) &&
            definition.Region.Equals(resolvedRegion, StringComparison.OrdinalIgnoreCase));
        _baseSchema.Items.Add(new BaseOption(
            string.Format(Strings.IFFManager_BaseAuto, resolvedRegion ?? Strings.IFFManager_RegionUnknown),
            new IffSchemaBaseReference("Common"), _inheritedFields.ToArray(), autoDefinition));
        foreach (IffSchemaDefinition definition in _templateSchemas.Where(definition =>
                     definition.FileName.Equals("Common.iff", StringComparison.OrdinalIgnoreCase))
                 .OrderBy(definition => definition.Region, StringComparer.OrdinalIgnoreCase))
            _baseSchema.Items.Add(new BaseOption($"Common.{definition.Region}",
                new IffSchemaBaseReference("Common", definition.Region),
                definition.Fields.Where(field => !IsCatchAllRaw(field, definition.MinimumRecordSize)).ToArray(), definition));
        _baseSchema.SelectedIndex = selected is null ? 0 : selected.Region is null ? 1 :
            Enumerable.Range(0, _baseSchema.Items.Count).FirstOrDefault(index =>
                (_baseSchema.Items[index] as BaseOption)?.Reference?.Region?.Equals(selected.Region,
                    StringComparison.OrdinalIgnoreCase) == true, 1);
        _baseSchema.SelectedIndexChanged += (_, _) => ApplySelectedBase();
        ApplySelectedBase();
    }

    private void ApplySelectedBase()
    {
        if (_baseSchema.SelectedItem is not BaseOption option) return;
        _inheritedFields.Clear();
        _inheritedFields.AddRange(option.Fields.Where(field => !IsCatchAllRaw(field, _recordSize)));
        _editBase.Enabled = option.Definition is not null && _saveBase is not null;
        RefreshList();
    }

    private void EditBaseSchema()
    {
        if (_baseSchema.SelectedItem is not BaseOption { Definition: { } definition } || _saveBase is null) return;
        using var dialog = new IffSchemaManagerDialog(definition.MinimumRecordSize, definition.Fields,
            definition.DefaultStringSize, _templateSchemas, _availableIffFiles, definition.DefaultLongStringSize);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        IffSchemaDefinition updated = definition with
        {
            SchemaVersion = IffSchemaJson.CurrentVersion,
            Fields = dialog.Fields,
            DefaultStringSize = dialog.DefaultStringSize,
            DefaultLongStringSize = dialog.DefaultLongStringSize,
            Base = null
        };
        _saveBase(updated);
        _inheritedFields.Clear();
        _inheritedFields.AddRange(updated.Fields.Where(field => !IsCatchAllRaw(field, updated.MinimumRecordSize)));
        RefreshList();
    }

    private int SelectedLocalIndex => _list.SelectedIndex - _inheritedFields.Count;

    private void AddField()
    {
        int selectedIndex = SelectedLocalIndex;
        int previousIndex = selectedIndex >= 0 ? selectedIndex : _fields.Count - 1;
        int initialOffset = previousIndex >= 0
            ? Math.Min(_recordSize - 1, checked(_fields[previousIndex].Offset + 1))
            : 0;
        using var dialog = new CustomIffColumnDialog(_recordSize, defaultStringSize: DefaultStringSize,
            initialOffset: initialOffset,
            previousFieldEnd: previousIndex >= 0
                ? checked(_fields[previousIndex].Offset + _fields[previousIndex].Width)
                : null,
            availableIffFiles: _availableIffFiles, defaultLongStringSize: DefaultLongStringSize);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!ValidateName(dialog.FieldDefinition.Name, -1)) return;
        int destination = previousIndex + 1;
        _fields.Insert(destination, dialog.FieldDefinition);
        RefreshList();
        _list.SelectedIndex = _inheritedFields.Count + destination;
    }

    private void CloneField()
    {
        var currentSchema = new IffSchemaDefinition(IffSchemaJson.CurrentVersion,
            Strings.IFFManager_CurrentSchema, "*", _recordSize, true, _fields, DefaultStringSize,
            DefaultLongStringSize: DefaultLongStringSize);
        using var picker = new IffFieldTemplateDialog(_recordSize, [currentSchema, .. _templateSchemas]);
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        IffFieldDefinition source = picker.SelectedField;
        int index = SelectedLocalIndex;
        using var dialog = new CustomIffColumnDialog(_recordSize,
            source with { Name = source.Name + " Copy" }, DefaultStringSize,
            previousFieldEnd: index >= 0
                ? checked(_fields[index].Offset + _fields[index].Width)
                : null,
            availableIffFiles: _availableIffFiles, defaultLongStringSize: DefaultLongStringSize);
        if (dialog.ShowDialog(this) != DialogResult.OK || !ValidateName(dialog.FieldDefinition.Name, -1)) return;
        int destination = index < 0 ? _fields.Count : index + 1;
        _fields.Insert(destination, dialog.FieldDefinition);
        RefreshList();
        _list.SelectedIndex = _inheritedFields.Count + destination;
    }

    private void MoveField(int direction)
    {
        int index = SelectedLocalIndex;
        int destination = index + direction;
        if (index < 0 || destination < 0 || destination >= _fields.Count) return;
        (_fields[index], _fields[destination]) = (_fields[destination], _fields[index]);
        RefreshList();
        _list.SelectedIndex = _inheritedFields.Count + destination;
    }

    private void SortFieldsByOffset()
    {
        int selectedIndex = SelectedLocalIndex;
        string? selectedName = selectedIndex >= 0 && selectedIndex < _fields.Count ? _fields[selectedIndex].Name : null;
        IffFieldDefinition[] sorted = SortByOffset(_fields);
        _fields.Clear();
        _fields.AddRange(sorted);
        RefreshList();
        if (selectedName is not null)
            _list.SelectedIndex = _inheritedFields.Count + _fields.FindIndex(field =>
                field.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
    }

    internal static IffFieldDefinition[] SortByOffset(IEnumerable<IffFieldDefinition> fields) =>
        fields.Select((field, index) => (field, index))
            .OrderBy(item => item.field.Offset)
            .ThenBy(item => item.index)
            .Select(item => item.field)
            .ToArray();

    private void EditField()
    {
        int index = SelectedLocalIndex;
        if (index < 0) return;
        IffFieldDefinition selected = _fields[index];
        int? previousFieldEnd = index > 0
            ? checked(_fields[index - 1].Offset + _fields[index - 1].Width)
            : null;
        using var dialog = new CustomIffColumnDialog(_recordSize, selected,
            previousFieldEnd: previousFieldEnd,
            availableIffFiles: _availableIffFiles, defaultLongStringSize: DefaultLongStringSize);
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
        _list.SelectedIndex = _inheritedFields.Count + index;
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
            IffFieldType.UInt32 or IffFieldType.ItemIdReference when width == 2 => IffFieldType.UInt16,
            IffFieldType.UInt32 or IffFieldType.ItemIdReference or IffFieldType.UInt16 when width == 1 => IffFieldType.Byte,
            IffFieldType.ItemIdReference when width == 4 => IffFieldType.ItemIdReference,
            IffFieldType.Int32 when width == 2 => IffFieldType.Int16,
            IffFieldType.Int64 when width == 4 => IffFieldType.Int32,
            IffFieldType.FixedString or IffFieldType.LongString or IffFieldType.Icon or IffFieldType.ImageResource or IffFieldType.Sound or IffFieldType.Raw or IffFieldType.ByteRangeBoolean => field.Type,
            IffFieldType.ZeroBoolean when width is 1 or 2 or 4 => field.Type,
            IffFieldType.BitField when width is >= 1 and <= 4 &&
                field.BitMask is uint bitMask && (bitMask & ~BitFieldWidthMask(width)) == 0 => field.Type,
            IffFieldType.BooleanBitField when width is >= 1 and <= 4 &&
                field.BitMask is uint mask && (mask & ~BitFieldWidthMask(width)) == 0 => field.Type,
            _ => IffFieldType.Raw
        };
        bool keepBitDefinition = type is IffFieldType.BitField or IffFieldType.BooleanBitField;
        return field with
        {
            Width = width,
            Type = type,
            EncodingCodePage = type is IffFieldType.FixedString or IffFieldType.LongString or IffFieldType.Icon or IffFieldType.ImageResource or IffFieldType.Sound ? field.EncodingCodePage : null,
            BitMask = keepBitDefinition ? field.BitMask : null,
            BitShift = keepBitDefinition ? field.BitShift : 0
        };
    }

    private static uint BitFieldWidthMask(int width) => width switch
    {
        1 => byte.MaxValue,
        2 => ushort.MaxValue,
        3 => 0x00FF_FFFFu,
        4 => uint.MaxValue,
        _ => 0
    };

    private void RemoveField()
    {
        int index = SelectedLocalIndex;
        if (index < 0) return;
        _fields.RemoveAt(index);
        RefreshList();
    }

    internal bool SaveIfValid(bool showMessage)
    {
        try
        {
            var definition = new IffSchemaDefinition(IffSchemaJson.CurrentVersion, "Validation.iff", "*",
                _recordSize, true, _fields, DefaultStringSize, Base: BaseReference);
            IffSchemaJson.ValidateDefinition(definition, _recordSize);
            DialogResult = DialogResult.OK;
            return true;
        }
        catch (Exception ex) when (ex is InvalidDataException or ArgumentException or OverflowException)
        {
            if (showMessage)
                MessageBox.Show(ex.Message, Strings.IFFManager_Error,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
    }

    private bool ValidateName(string name, int excludedIndex)
    {
        bool duplicate = _inheritedFields.Any(field => field.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)) ||
            _fields.Where((_, index) => index != excludedIndex)
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
        foreach (IffFieldDefinition field in _inheritedFields)
            _list.Items.Add($"[{Strings.IFFManager_BaseField}] {field.Name} — {IffFieldTypeDisplay.GetName(field.Type)}, {field.Offset}, {field.Width} byte(s)");
        foreach (IffFieldDefinition field in _fields)
            _list.Items.Add($"{field.Name} — {IffFieldTypeDisplay.GetName(field.Type)}, {field.Offset}, {field.Width} byte(s)");
        _list.EndUpdate();
    }

    private void DrawFieldItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _list.Items.Count) return;
        bool selected = e.State.HasFlag(DrawItemState.Selected);
        bool inherited = e.Index < _inheritedFields.Count;
        bool overlaps = !inherited && FindOverlappingFields(_fields, _recordSize)[e.Index - _inheritedFields.Count];
        Color background = overlaps
            ? selected ? Color.Goldenrod : Color.LightYellow
            : inherited ? selected ? SystemColors.Highlight : SystemColors.ControlLight
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
