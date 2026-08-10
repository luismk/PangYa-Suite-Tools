using PangyaAPI.WFT;
using PangYa_Suite_Tools.Localization;

namespace PangYa_Suite_Tools;

internal sealed class WftExportOptionsDialog : Form
{
    private readonly TextBox _familyName = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _style = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList
    };

    public WftExportOptionsDialog(string defaultFamilyName)
    {
        Name = "wftExportOptionsDialog";
        Text = Strings.WftViewer_ExportDialogTitle;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(430, 150);

        _familyName.Name = "txtWftExportFamily";
        _familyName.Text = defaultFamilyName;
        _style.Name = "cboWftExportStyle";
        _style.Items.AddRange(["Regular", "Bold", "Italic", "Bold Italic"]);
        _style.SelectedIndex = 0;

        var okButton = new Button
        {
            Name = "btnWftExportOk",
            Text = Strings.Common_OK,
            DialogResult = DialogResult.None,
            AutoSize = true
        };
        var cancelButton = new Button
        {
            Name = "btnWftExportCancel",
            Text = Strings.Common_Cancel,
            DialogResult = DialogResult.Cancel,
            AutoSize = true
        };
        okButton.Click += (_, _) => Accept();

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            ColumnCount = 2,
            RowCount = 3
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label
        {
            Text = Strings.WftViewer_ExportFamily,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);
        layout.Controls.Add(_familyName, 1, 0);
        layout.Controls.Add(new Label
        {
            Text = Strings.WftViewer_ExportStyle,
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 1);
        layout.Controls.Add(_style, 1, 1);
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true
        };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(okButton);
        layout.Controls.Add(buttons, 0, 2);
        layout.SetColumnSpan(buttons, 2);
        Controls.Add(layout);
        AcceptButton = okButton;
        CancelButton = cancelButton;
    }

    public WftTrueTypeExportOptions? Options { get; private set; }

    private void Accept()
    {
        string familyName = _familyName.Text.Trim();
        if (familyName.Length is 0 or > 128 || familyName.Any(char.IsControl))
        {
            MessageBox.Show(this, Strings.WftViewer_InvalidFamily, Strings.Common_Error,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _familyName.Focus();
            return;
        }
        Options = new WftTrueTypeExportOptions(familyName,
            (WftFontStyle)Math.Max(0, _style.SelectedIndex));
        DialogResult = DialogResult.OK;
        Close();
    }
}
