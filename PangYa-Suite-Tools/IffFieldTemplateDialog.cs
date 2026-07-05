using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal sealed class IffFieldTemplateDialog : Form
{
    private sealed record SchemaOption(IffSchemaDefinition Definition)
    {
        public override string ToString() => $"{Definition.FileName} ({Definition.Region})";
    }

    private sealed record FieldOption(IffFieldDefinition Definition)
    {
        public override string ToString() =>
            $"{Definition.Name} — {Definition.Type}, {Definition.Offset}, {Definition.Width} byte(s)";
    }

    private readonly int _recordSize;
    private readonly ComboBox _schemas = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ListBox _fields = new() { Dock = DockStyle.Fill };

    public IffFieldDefinition SelectedField { get; private set; } = null!;

    public IffFieldTemplateDialog(int recordSize, IEnumerable<IffSchemaDefinition> schemas)
    {
        _recordSize = recordSize;
        Text = Strings.IFFManager_SelectTemplate;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(560, 380);
        ClientSize = new Size(680, 440);

        foreach (IffSchemaDefinition schema in schemas
            .Where(schema => schema.Fields.Any(FieldFits))
            .GroupBy(schema => $"{schema.FileName}|{schema.Region}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First()))
            _schemas.Items.Add(new SchemaOption(schema));
        _schemas.SelectedIndexChanged += (_, _) => RefreshFields();
        _fields.DoubleClick += (_, _) => AcceptSelection();

        var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 42, ColumnCount = 2, Padding = new Padding(6) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.Controls.Add(new Label { Text = Strings.IFFManager_TemplateSchema, AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        header.Controls.Add(_schemas, 1, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft };
        var select = new Button { Text = Strings.IFFManager_Select, AutoSize = true };
        var cancel = new Button { Text = Strings.Options_Cancel, AutoSize = true, DialogResult = DialogResult.Cancel };
        select.Click += (_, _) => AcceptSelection();
        buttons.Controls.AddRange([select, cancel]);

        Controls.Add(_fields);
        Controls.Add(header);
        Controls.Add(buttons);
        AcceptButton = select;
        CancelButton = cancel;
        if (_schemas.Items.Count > 0) _schemas.SelectedIndex = 0;
    }

    private bool FieldFits(IffFieldDefinition field) =>
        field.Offset >= 0 && field.Width > 0 && field.Offset <= _recordSize - field.Width;

    private void RefreshFields()
    {
        _fields.Items.Clear();
        if (_schemas.SelectedItem is not SchemaOption schema) return;
        foreach (IffFieldDefinition field in schema.Definition.Fields.Where(FieldFits))
            _fields.Items.Add(new FieldOption(field));
        if (_fields.Items.Count > 0) _fields.SelectedIndex = 0;
    }

    private void AcceptSelection()
    {
        if (_fields.SelectedItem is not FieldOption field) return;
        SelectedField = field.Definition;
        DialogResult = DialogResult.OK;
    }
}
