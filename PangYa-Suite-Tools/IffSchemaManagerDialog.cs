using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal sealed class IffSchemaManagerDialog : Form
{
    private readonly int _recordSize;
    private readonly List<IffFieldDefinition> _fields;
    private readonly ListBox _list = new() { Dock = DockStyle.Fill };

    public IReadOnlyList<IffFieldDefinition> Fields => _fields;

    public IffSchemaManagerDialog(int recordSize, IEnumerable<IffFieldDefinition> fields)
    {
        _recordSize = recordSize;
        _fields = [.. fields];
        Text = Strings.IFFManager_ManageColumnsTitle;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(620, 380);
        ClientSize = new Size(720, 440);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42 };
        var add = new Button { Text = Strings.IFFManager_AddColumn, AutoSize = true };
        var edit = new Button { Text = Strings.IFFManager_EditColumn, AutoSize = true };
        var remove = new Button { Text = Strings.IFFManager_RemoveColumn, AutoSize = true };
        var save = new Button { Text = Strings.IFFManager_SaveSchema, AutoSize = true };
        var cancel = new Button { Text = Strings.Options_Cancel, AutoSize = true, DialogResult = DialogResult.Cancel };
        add.Click += (_, _) => AddField();
        edit.Click += (_, _) => EditField();
        remove.Click += (_, _) => RemoveField();
        save.Click += (_, _) => DialogResult = DialogResult.OK;
        buttons.Controls.AddRange([add, edit, remove, save, cancel]);
        Controls.Add(_list);
        Controls.Add(buttons);
        AcceptButton = save;
        CancelButton = cancel;
        RefreshList();
    }

    private void AddField()
    {
        using var dialog = new CustomIffColumnDialog(_recordSize);
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        if (!ValidateName(dialog.FieldDefinition.Name, -1)) return;
        _fields.Add(dialog.FieldDefinition);
        RefreshList();
        _list.SelectedIndex = _fields.Count - 1;
    }

    private void EditField()
    {
        int index = _list.SelectedIndex;
        if (index < 0) return;
        IffFieldDefinition selected = _fields[index];
        using var dialog = new CustomIffColumnDialog(_recordSize, selected);
        if (dialog.ShowDialog(this) != DialogResult.OK || !ValidateName(dialog.FieldDefinition.Name, index)) return;
        _fields[index] = dialog.FieldDefinition;
        RefreshList();
        _list.SelectedIndex = index;
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
            _list.Items.Add($"{field.Name} — {field.Type}, 0x{field.Offset:X}, {field.Width} byte(s)");
        _list.EndUpdate();
    }
}
