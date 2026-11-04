using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal sealed class IffSchemaUpdateDialog : Form
{
    private const int ActionColumnIndex = 4;
    private sealed record ActionOption(IffSchemaUpdateAction Action, string Label);

    private readonly IReadOnlyList<IffSchemaUpdateCandidate> _candidates;
    private readonly DataGridView _updates = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        AllowUserToResizeRows = false,
        AutoGenerateColumns = false,
        MultiSelect = false,
        ReadOnly = false,
        RowHeadersVisible = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect
    };
    private readonly TextBox _localJson = JsonBox();
    private readonly TextBox _bundledJson = JsonBox();
    private readonly IReadOnlyList<ActionOption> _actions;

    internal IReadOnlyList<IffSchemaUpdateSelection> Selections => _updates.Rows
        .Cast<DataGridViewRow>()
        .Select(row => new IffSchemaUpdateSelection(
            _candidates[row.Index],
            GetAction(row)))
        .ToArray();

    internal IffSchemaUpdateDialog(IReadOnlyList<IffSchemaUpdateCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count == 0) throw new ArgumentException("At least one schema update is required.", nameof(candidates));
        _candidates = candidates;
        _actions =
        [
            new(IffSchemaUpdateAction.KeepForNow, Strings.IFFManager_SchemaUpdateKeep),
            new(IffSchemaUpdateAction.ReplaceWithBundledDefault, Strings.IFFManager_SchemaUpdateReplace),
            new(IffSchemaUpdateAction.UseLocalDefinition, Strings.IFFManager_SchemaUpdateUseMine)
        ];

        Text = Strings.IFFManager_SchemaUpdates;
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(900, 560);
        ClientSize = new Size(1080, 680);

        ConfigureGrid();
        var details = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterDistance = ClientSize.Width / 2
        };
        details.Panel1.Controls.Add(WrapJsonBox(Strings.IFFManager_SchemaUpdateLocalDefinition, _localJson));
        details.Panel2.Controls.Add(WrapJsonBox(Strings.IFFManager_SchemaUpdateBundledDefinition, _bundledJson));

        var apply = new Button
        {
            Text = Strings.IFFManager_SchemaUpdateApply,
            AutoSize = true,
            DialogResult = DialogResult.OK
        };
        var cancel = new Button
        {
            Text = Strings.Options_Cancel,
            AutoSize = true,
            DialogResult = DialogResult.Cancel
        };
        var useForAll = new Button
        {
            Name = "btnSchemaUpdateUseForAll",
            Text = Strings.IFFManager_SchemaUpdateUseForAll,
            AutoSize = true
        };
        useForAll.Click += (_, _) => UseCurrentActionForAll();
        EventHandler refreshUseForAll = (_, _) => useForAll.Text = Strings.IFFManager_SchemaUpdateUseForAll;
        LocalizationManager.CultureChanged += refreshUseForAll;
        useForAll.Disposed += (_, _) => LocalizationManager.CultureChanged -= refreshUseForAll;
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false
        };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(apply);
        buttons.Controls.Add(useForAll);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(10)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label
        {
            Text = Strings.IFFManager_SchemaUpdateDescription,
            AutoSize = true,
            MaximumSize = new Size(1040, 0),
            Margin = new Padding(3, 3, 3, 10)
        }, 0, 0);
        layout.Controls.Add(_updates, 0, 1);
        layout.Controls.Add(details, 0, 2);
        layout.Controls.Add(buttons, 0, 3);
        Controls.Add(layout);
        AcceptButton = apply;
        CancelButton = cancel;

        PopulateRows();
        _updates.SelectionChanged += (_, _) => RefreshJsonPreview();
        _updates.Rows[0].Selected = true;
        _updates.CurrentCell = _updates.Rows[0].Cells[0];
        RefreshJsonPreview();
    }

    private void ConfigureGrid()
    {
        _updates.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = Strings.IFFManager_SchemaUpdateFile,
            ReadOnly = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 150
        });
        _updates.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = Strings.IFFManager_SchemaUpdateRegion,
            ReadOnly = true,
            Width = 90
        });
        _updates.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = Strings.IFFManager_SchemaUpdateLocalRevision,
            ReadOnly = true,
            Width = 110
        });
        _updates.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = Strings.IFFManager_SchemaUpdateBundledRevision,
            ReadOnly = true,
            Width = 120
        });
        var action = new DataGridViewComboBoxColumn
        {
            HeaderText = Strings.IFFManager_SchemaUpdateAction,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 170,
            DisplayMember = nameof(ActionOption.Label),
            ValueMember = nameof(ActionOption.Action),
            FlatStyle = FlatStyle.Flat
        };
        foreach (ActionOption option in _actions) action.Items.Add(option);
        _updates.Columns.Add(action);
    }

    private void PopulateRows()
    {
        foreach (IffSchemaUpdateCandidate candidate in _candidates)
            _updates.Rows.Add(candidate.FileName, candidate.Region, candidate.LocalRevision,
                candidate.BundledRevision, IffSchemaUpdateAction.KeepForNow);
    }

    private static IffSchemaUpdateAction GetAction(DataGridViewRow row) =>
        row.Cells[ActionColumnIndex].Value is IffSchemaUpdateAction action
            ? action
            : IffSchemaUpdateAction.KeepForNow;

    private void UseCurrentActionForAll()
    {
        if (!_updates.EndEdit() || _updates.CurrentRow is not { } currentRow) return;

        IffSchemaUpdateAction action = GetAction(currentRow);
        foreach (DataGridViewRow row in _updates.Rows)
            row.Cells[ActionColumnIndex].Value = action;
    }

    private void RefreshJsonPreview()
    {
        int index = _updates.CurrentRow?.Index ?? -1;
        if ((uint)index >= (uint)_candidates.Count) return;
        _localJson.Text = _candidates[index].LocalJson;
        _bundledJson.Text = _candidates[index].BundledJson;
    }

    private static Control WrapJsonBox(string title, TextBox box)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(4) };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font(SystemFonts.DefaultFont, FontStyle.Bold) }, 0, 0);
        panel.Controls.Add(box, 0, 1);
        return panel;
    }

    private static TextBox JsonBox() => new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        Font = new Font(FontFamily.GenericMonospace, 9)
    };
}
