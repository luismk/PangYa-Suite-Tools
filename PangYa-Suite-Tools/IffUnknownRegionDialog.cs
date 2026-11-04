using PangYa_Suite_Tools.Localization;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools;

internal sealed class IffUnknownRegionDialog : Form
{
    private sealed record Choice(string Label, string Region);
    private readonly ComboBox _region = new();

    public string SelectedRegion => ((Choice)_region.SelectedItem!).Region;

    public IffUnknownRegionDialog(IffDocumentInfo document)
    {
        Text = Strings.IFFManager_UnknownRegionTitle;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(460, 260);

        var description = new Label
        {
            Text = Strings.IFFManager_UnknownRegionDescription,
            AutoSize = false,
            Location = new Point(16, 14),
            Size = new Size(428, 42)
        };
        var header = new TextBox
        {
            Name = "txtUnknownRegionHeader",
            ReadOnly = true,
            Multiline = true,
            Location = new Point(16, 60),
            Size = new Size(428, 92),
            Text = string.Format(LocalizationManager.CurrentCulture,
                Strings.IFFManager_UnknownRegionHeaderFormat,
                document.Header.RecordCount,
                document.Header.Revision,
                document.Header.Magic,
                Convert.ToHexString(document.Header.Reserved),
                document.RecordSize)
        };
        var regionLabel = new Label
        {
            Text = Strings.IFFManager_UnknownRegionSelect,
            AutoSize = true,
            Location = new Point(16, 168)
        };
        _region.Name = "cboUnknownRegion";
        _region.DropDownStyle = ComboBoxStyle.DropDownList;
        _region.Location = new Point(170, 164);
        _region.Size = new Size(274, 24);
        _region.DisplayMember = nameof(Choice.Label);
        _region.Items.AddRange([
            new Choice(Strings.IFFManager_RegionThailand, "TH"),
            new Choice(Strings.IFFManager_RegionJapan, "JP"),
            new Choice(Strings.IFFManager_RegionJapan983, IffHeader.Japan983Region),
            new Choice(Strings.IFFManager_RegionGlobal, "Global")
        ]);
        _region.SelectedIndex = 0;

        var cancel = new Button
        {
            Text = Strings.Options_Cancel,
            DialogResult = DialogResult.Cancel,
            Location = new Point(350, 214),
            Size = new Size(94, 30)
        };
        var open = new Button
        {
            Text = Strings.IFFManager_UnknownRegionOpen,
            DialogResult = DialogResult.OK,
            Location = new Point(250, 214),
            Size = new Size(94, 30)
        };
        Controls.AddRange([description, header, regionLabel, _region, open, cancel]);
        AcceptButton = open;
        CancelButton = cancel;
    }
}
