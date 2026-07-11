using PangYa_Suite_Tools.Localization;
using System.Globalization;

namespace PangYa_Suite_Tools;

internal sealed class IffReferencePickerDialog : Form
{
    private readonly TextBox _search = new();
    private readonly ListView _items = new();
    private readonly ImageList _images = new() { ImageSize = new Size(32, 32), ColorDepth = ColorDepth.Depth32Bit };
    private readonly IReadOnlyList<IffReferenceCatalogItem> _catalog;
    private readonly uint _selectedKey;

    public IffReferencePickerDialog(IReadOnlyList<IffReferenceCatalogItem> catalog, string targetFile, uint selectedKey = 0)
    {
        _catalog = catalog;
        _selectedKey = selectedKey;
        Text = string.Format(CultureInfo.CurrentCulture, Strings.IFFManager_SelectReferenceFormat, targetFile);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(540, 430);

        _search.Dock = DockStyle.Top;
        _search.PlaceholderText = Strings.IFFManager_FormRecordSearch;
        _search.Margin = new Padding(8);
        _search.TextChanged += (_, _) => Populate();

        _items.Dock = DockStyle.Fill;
        _items.View = View.Details;
        _items.FullRowSelect = true;
        _items.MultiSelect = false;
        _items.SmallImageList = _images;
        _items.Columns.Add(Strings.IFFManager_ItemId, 90);
        _items.Columns.Add(Strings.IFFManager_ItemName, 210);
        _items.Columns.Add(Strings.IFFManager_ItemType, 70);
        _items.Columns.Add(Strings.IFFManager_Character, 120);
        _items.Columns.Add(Strings.IFFManager_LinkedIff, 90);
        _items.DoubleClick += (_, _) => AcceptSelection();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 46,
            Padding = new Padding(8)
        };
        var ok = new Button { Text = Strings.Common_OK, AutoSize = true };
        var cancel = new Button { Text = Strings.Options_Cancel, AutoSize = true, DialogResult = DialogResult.Cancel };
        ok.Click += (_, _) => AcceptSelection();
        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        Controls.Add(_items);
        Controls.Add(buttons);
        Controls.Add(_search);
        Populate();
    }

    public IffReferenceCatalogItem? SelectedItem { get; private set; }

    private void Populate()
    {
        string filter = _search.Text.Trim();
        _items.BeginUpdate();
        try
        {
            _items.Items.Clear();
            _images.Images.Clear();
            foreach (IffReferenceCatalogItem item in _catalog.Where(item => Matches(item, filter)))
            {
                int imageIndex = AddImage(item.IconPath);
                var row = new ListViewItem(item.Key.ToString(CultureInfo.CurrentCulture), imageIndex)
                {
                    Tag = item
                };
                row.SubItems.Add(item.Name);
                row.SubItems.Add(item.ItemIdInfo?.Type.ToString(CultureInfo.CurrentCulture) ?? string.Empty);
                row.SubItems.Add(item.ItemIdInfo is { } info
                    ? string.IsNullOrWhiteSpace(info.CharacterName)
                        ? info.CharacterSerial.ToString(CultureInfo.CurrentCulture)
                        : info.CharacterName
                    : string.Empty);
                row.SubItems.Add(item.TargetFile);
                _items.Items.Add(row);
                if (item.Key == _selectedKey) row.Selected = true;
            }
        }
        finally { _items.EndUpdate(); }
        if (_items.SelectedItems.Count == 0 && _items.Items.Count > 0) _items.Items[0].Selected = true;
    }

    private static bool Matches(IffReferenceCatalogItem item, string filter) =>
        filter.Length == 0 ||
        item.Key.ToString(CultureInfo.InvariantCulture).Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        item.Name.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ||
        item.IconId.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
        item.TargetFile.Contains(filter, StringComparison.OrdinalIgnoreCase);

    private int AddImage(string? path)
    {
        using Image? source = IffPreviewImageLoader.Load(path);
        _images.Images.Add(source is null ? CreatePlaceholder() : new Bitmap(source, _images.ImageSize));
        return _images.Images.Count - 1;
    }

    internal static Bitmap CreatePlaceholder()
    {
        var bitmap = new Bitmap(32, 32);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(SystemColors.ControlLight);
        using var pen = new Pen(SystemColors.GrayText);
        graphics.DrawRectangle(pen, 1, 1, 30, 30);
        graphics.DrawLine(pen, 6, 6, 26, 26);
        graphics.DrawLine(pen, 26, 6, 6, 26);
        return bitmap;
    }

    private void AcceptSelection()
    {
        if (_items.SelectedItems.Count == 0) return;
        SelectedItem = (IffReferenceCatalogItem)_items.SelectedItems[0].Tag!;
        DialogResult = DialogResult.OK;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _images.Dispose();
        base.Dispose(disposing);
    }
}
