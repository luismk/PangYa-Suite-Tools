using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;

namespace PangYa_Suite_Tools.Shop;

internal sealed class FrmShopMockup : Form
{
    internal const float ZoomFactor = 1.4f;
    private readonly ShopCanvas _canvas;

    private FrmShopMockup(ShopLayout layout, ShopAssetResolver assets, IReadOnlyList<ShopCatalogItem> catalog)
    {
        Text = Strings.Shop_Title;
        ClientSize = new Size((int)Math.Ceiling(layout.Size.Width * ZoomFactor), (int)Math.Ceiling(layout.Size.Height * ZoomFactor));
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        _canvas = new ShopCanvas(layout, assets, catalog, Path.Combine(assets.DataRoot, "pangya_th.iff")) { Dock = DockStyle.Fill };
        Controls.Add(_canvas);
        LocalizationManager.CultureChanged += LocalizationManager_CultureChanged;
        Disposed += (_, _) => LocalizationManager.CultureChanged -= LocalizationManager_CultureChanged;
    }

    public static async Task<FrmShopMockup> CreateAsync(string dataRoot, CancellationToken cancellationToken = default)
    {
        string fullRoot = Path.GetFullPath(dataRoot);
        string shopXml = Path.Combine(fullRoot, "ui", "shop.xml");
        string predefinedXml = Path.Combine(fullRoot, "ui", "predefined.xml");
        string iff = Path.Combine(fullRoot, "pangya_th.iff");
        foreach (string path in new[] { shopXml, predefinedXml, iff })
            if (!File.Exists(path)) throw new FileNotFoundException(Strings.Shop_MissingRequiredFile, path);

        (ShopLayout layout, ShopAssetResolver assets, IReadOnlyList<ShopCatalogItem> catalog) = await Task.Run(async () =>
        {
            ShopLayout parsedLayout = ShopLayoutParser.Load(shopXml, predefinedXml);
            var resolver = new ShopAssetResolver(fullRoot);
            ValidateSkinAssets(parsedLayout, resolver);
            ShopCatalogLoadResult loadedCatalog = await ShopCatalogLoader.LoadAsync(iff, resolver, cancellationToken);
            if (loadedCatalog.MissingIconCount != 0)
                AppLogger.Instance.Log("Shop", string.Format(LocalizationManager.CurrentCulture,
                    Strings.Shop_MissingIconsSkipped, loadedCatalog.MissingIconCount), AppLogLevel.Warning);
            return (parsedLayout, resolver, loadedCatalog.Items);
        }, cancellationToken);
        return new FrmShopMockup(layout, assets, catalog);
    }

    private static void ValidateSkinAssets(ShopLayout layout, ShopAssetResolver resolver)
    {
        string[] imageParameters = ["bgimg", "normal", "over", "selected", "below_over", "below_selected", "sepImg"];
        foreach (ShopLayoutElement element in layout.Elements)
            foreach (string key in imageParameters)
                if (element.Parameters.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
                    _ = resolver.Resolve(value);
    }

    private void LocalizationManager_CultureChanged(object? sender, EventArgs e)
    {
        Text = Strings.Shop_Title;
        _canvas.Invalidate();
    }
}

internal sealed class ShopCanvas : Control
{
    private const int ItemsPerPage = 24;
    private static readonly Rectangle CatalogBounds = new(384, 148, 405, 375);
    private static readonly Rectangle ScrollBarBounds = new(780, 150, 9, 366);
    private readonly ShopLayout _layout;
    private readonly ShopAssetResolver _assets;
    private readonly IReadOnlyList<ShopCatalogItem> _catalog;
    private readonly ShopSession _session = new();
    private readonly string _iffPath;
    private readonly Dictionary<string, Image> _images = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<(Rectangle Bounds, ShopCatalogItem Item)> _visibleItems = [];
    private string? _hoveredElement;
    private string _filter = string.Empty;
    private int _categoryIndex;
    private int _page;
    private bool _rental;
    private bool _editing;

    public ShopCanvas(ShopLayout layout, ShopAssetResolver assets, IReadOnlyList<ShopCatalogItem> catalog, string iffPath)
    {
        _layout = layout;
        _assets = assets;
        _catalog = catalog;
        _iffPath = iffPath;
        DoubleBuffered = true;
        TabStop = true;
        SetStyle(ControlStyles.Selectable, true);
        Cursor = Cursors.Default;
        MouseEnter += (_, _) => Focus();
        MouseMove += ShopCanvas_MouseMove;
        MouseLeave += (_, _) => { _hoveredElement = null; Invalidate(); };
        MouseClick += ShopCanvas_MouseClick;
        MouseWheel += ShopCanvas_MouseWheel;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) foreach (Image image in _images.Values) image.Dispose();
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(Color.FromArgb(25, 28, 33));
        e.Graphics.ScaleTransform(FrmShopMockup.ZoomFactor, FrmShopMockup.ZoomFactor);
        e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
        foreach (ShopLayoutElement element in _layout.Elements.Where(element => element.Type is "AREA" or "BUTTON"))
            DrawElement(e.Graphics, element);
        DrawCatalog(e.Graphics);
        DrawCartAndBalances(e.Graphics);
    }

    private void DrawElement(Graphics graphics, ShopLayoutElement element)
    {
        string? resource = null;
        if (element.Type == "AREA") element.Parameters.TryGetValue("bgimg", out resource);
        else if (string.Equals(_hoveredElement, element.Name, StringComparison.OrdinalIgnoreCase))
            element.Parameters.TryGetValue("over", out resource);
        resource ??= element.Parameters.GetValueOrDefault("normal");
        if (string.IsNullOrWhiteSpace(resource)) return;
        Image image = GetImage(resource);
        Rectangle destination = element.Bounds;
        if (destination.Width == 0 || destination.Height == 0)
            destination = new Rectangle(destination.Location, image.Size);
        graphics.DrawImage(image, destination);
    }

    private void DrawCatalog(Graphics graphics)
    {
        string[] categories = _catalog.Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
        _visibleItems.Clear();
        if (categories.Length == 0)
        {
            DrawText(graphics, Strings.Shop_NoItems, new Rectangle(400, 180, 360, 80), Color.White, 11, ContentAlignment.MiddleCenter);
            return;
        }
        _categoryIndex = Math.Clamp(_categoryIndex, 0, categories.Length - 1);
        string category = categories[_categoryIndex];
        var categoryBounds = new Rectangle(392, 82, 330, 33);
        using (var categoryBrush = new System.Drawing.Drawing2D.LinearGradientBrush(categoryBounds,
            Color.FromArgb(235, 19, 125, 197), Color.FromArgb(235, 8, 65, 120), 90f))
            graphics.FillRectangle(categoryBrush, categoryBounds);
        graphics.DrawRectangle(Pens.LightSkyBlue, categoryBounds);
        DrawText(graphics, $"{category}  ({_categoryIndex + 1}/{categories.Length})", new Rectangle(401, 84, 310, 28), Color.White, 11, ContentAlignment.MiddleLeft);
        ShopCatalogItem[] filteredItems = GetFilteredItems(category);
        int maximumPage = GetMaximumPage(filteredItems.Length);
        _page = Math.Clamp(_page, 0, maximumPage);
        ShopCatalogItem[] pageItems = filteredItems.Skip(_page * ItemsPerPage).Take(ItemsPerPage).ToArray();
        for (int index = 0; index < pageItems.Length; index++)
        {
            int column = index % 4;
            int row = index / 4;
            var bounds = new Rectangle(392 + column * 98, 150 + row * 61, 92, 56);
            ShopCatalogItem item = pageItems[index];
            using var background = new SolidBrush(Color.FromArgb(185, 20, 24, 30));
            graphics.FillRectangle(background, bounds);
            graphics.DrawRectangle(Pens.Gray, bounds);
            Image icon = GetImageByPath(item.IconPath);
            graphics.DrawImage(icon, new Rectangle(bounds.X + 3, bounds.Y + 3, 38, 38));
            DrawText(graphics, item.Name, new Rectangle(bounds.X + 44, bounds.Y + 2, 45, 31), Color.White, 7, ContentAlignment.TopLeft);
            uint price = _rental && item.RentalPrice != 0 ? item.RentalPrice : item.PurchasePrice;
            DrawText(graphics, $"{price:N0}", new Rectangle(bounds.X + 43, bounds.Y + 31, 46, 14), item.IsCash ? Color.Gold : Color.LightGreen, 7, ContentAlignment.MiddleRight);
            DrawText(graphics, $"S:{item.ShopFlags:X2} M:{item.MoneyFlags:X2}", new Rectangle(bounds.X + 43, bounds.Y + 43, 47, 11),
                Color.LightSkyBlue, 6, ContentAlignment.MiddleRight);
            string? bannerResource = GetBannerResource(item);
            if (bannerResource is not null)
            {
                Image banner = GetImage(bannerResource);
                graphics.DrawImage(banner, new Rectangle(bounds.X + 3, bounds.Y + 3, 37, 37));
            }
            _visibleItems.Add((bounds, item));
        }
        DrawScrollBar(graphics, maximumPage);
    }

    private ShopCatalogItem[] GetFilteredItems(string category)
    {
        IEnumerable<ShopCatalogItem> query = _catalog.Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
        if (_filter.Length != 0) query = query.Where(item => item.Name.Contains(_filter, StringComparison.CurrentCultureIgnoreCase));
        return query.ToArray();
    }

    private int CurrentMaximumPage()
    {
        string[] categories = _catalog.Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
        if (categories.Length == 0) return 0;
        int categoryIndex = Math.Clamp(_categoryIndex, 0, categories.Length - 1);
        return GetMaximumPage(GetFilteredItems(categories[categoryIndex]).Length);
    }

    private static int GetMaximumPage(int itemCount) => Math.Max(0, (itemCount - 1) / ItemsPerPage);

    internal static string? GetBannerResource(ShopCatalogItem item)
    {
        if ((item.MoneyFlags & 0x40) != 0 || (item.ShopFlags & 0x08) != 0) return "mark_surprise_sale";
        if ((item.MoneyFlags & 0x20) != 0) return "mark_hot";
        if ((item.MoneyFlags & 0x02) != 0) return "mark_new";
        return null;
    }

    private void DrawScrollBar(Graphics graphics, int maximumPage)
    {
        using var track = new SolidBrush(Color.FromArgb(180, 18, 37, 52));
        graphics.FillRectangle(track, ScrollBarBounds);
        graphics.DrawRectangle(Pens.SteelBlue, ScrollBarBounds);
        int pageCount = maximumPage + 1;
        int thumbHeight = Math.Max(28, ScrollBarBounds.Height / pageCount);
        int travel = ScrollBarBounds.Height - thumbHeight;
        int thumbY = ScrollBarBounds.Y + (maximumPage == 0 ? 0 : travel * _page / maximumPage);
        using var thumb = new SolidBrush(Color.FromArgb(230, 67, 169, 230));
        graphics.FillRectangle(thumb, new Rectangle(ScrollBarBounds.X + 1, thumbY, ScrollBarBounds.Width - 1, thumbHeight));
    }

    private void DrawCartAndBalances(Graphics graphics)
    {
        (ulong pang, ulong cookies) = _session.Totals(_rental);
        DrawText(graphics, string.Format(Strings.Shop_CartSummary, _session.Cart.Count, pang, cookies),
            new Rectangle(15, 408, 335, 45), Color.White, 9, ContentAlignment.MiddleLeft);
        DrawText(graphics, string.Format(Strings.Shop_Balances, _session.Pang, _session.Cookies),
            new Rectangle(15, 465, 335, 40), Color.White, 9, ContentAlignment.MiddleLeft);
        if (_filter.Length != 0)
            DrawText(graphics, string.Format(Strings.Shop_Filter, _filter), new Rectangle(15, 330, 330, 25), Color.White, 9, ContentAlignment.MiddleLeft);
        DrawText(graphics, Strings.Shop_EditHint, new Rectangle(20, 360, 330, 35), Color.LightSkyBlue, 8, ContentAlignment.MiddleLeft);
    }

    private static void DrawText(Graphics graphics, string text, Rectangle bounds, Color color, float size, ContentAlignment alignment)
    {
        (StringAlignment horizontal, StringAlignment vertical) = alignment switch
        {
            ContentAlignment.MiddleCenter => (StringAlignment.Center, StringAlignment.Center),
            ContentAlignment.MiddleRight => (StringAlignment.Far, StringAlignment.Center),
            ContentAlignment.MiddleLeft => (StringAlignment.Near, StringAlignment.Center),
            _ => (StringAlignment.Near, StringAlignment.Near),
        };
        using var font = new Font("Segoe UI", size, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat
        {
            Alignment = horizontal,
            LineAlignment = vertical,
            Trimming = StringTrimming.EllipsisCharacter,
        };
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private void ShopCanvas_MouseMove(object? sender, MouseEventArgs e)
    {
        Point logicalPoint = ToLogical(e.Location);
        string? hovered = HitElement(logicalPoint)?.Name;
        if (hovered == _hoveredElement) return;
        _hoveredElement = hovered;
        Cursor = hovered is null && !_visibleItems.Any(item => item.Bounds.Contains(logicalPoint)) ? Cursors.Default : Cursors.Hand;
        Invalidate();
    }

    private async void ShopCanvas_MouseClick(object? sender, MouseEventArgs e)
    {
        if (_editing) return;
        Point logicalPoint = ToLogical(e.Location);
        if (ScrollBarBounds.Contains(logicalPoint))
        {
            int maximumPage = CurrentMaximumPage();
            _page = maximumPage == 0 ? 0 : Math.Clamp(
                (logicalPoint.Y - ScrollBarBounds.Y) * (maximumPage + 1) / ScrollBarBounds.Height, 0, maximumPage);
            Invalidate();
            return;
        }
        (Rectangle Bounds, ShopCatalogItem Item) visible = _visibleItems.FirstOrDefault(item => item.Bounds.Contains(logicalPoint));
        if (visible.Item is not null)
        {
            Rectangle iconBounds = new(visible.Bounds.X + 3, visible.Bounds.Y + 3, 38, 38);
            if (e.Button == MouseButtons.Left && iconBounds.Contains(logicalPoint))
                await ChangeIconAsync(visible.Item);
            else if (e.Button == MouseButtons.Right)
                await EditPricesAsync(visible.Item);
            else if (e.Button == MouseButtons.Left)
            {
                _session.Add(visible.Item);
                Invalidate();
            }
            return;
        }
        switch (HitElement(logicalPoint)?.Name)
        {
            case "close_wnd": FindForm()?.Close(); break;
            case "buy_reset": _session.Clear(); Invalidate(); break;
            case "sidetab_buy": _rental = false; _page = 0; Invalidate(); break;
            case "sidetab_rental": _rental = true; _page = 0; Invalidate(); break;
            case "scroll_up": ScrollByPage(-1); break;
            case "scroll_down": ScrollByPage(1); break;
            case "tab_main": CycleCategory(); break;
            case "item_search_btn":
            case "searchgoods": SetSearchFilter(); break;
            case "buy_all": Checkout(); break;
        }
    }

    private void ShopCanvas_MouseWheel(object? sender, MouseEventArgs e)
    {
        Point logicalPoint = ToLogical(e.Location);
        if (!CatalogBounds.Contains(logicalPoint)) return;
        int steps = Math.Max(1, Math.Abs(e.Delta) / SystemInformation.MouseWheelScrollDelta);
        ScrollByPage(e.Delta > 0 ? -steps : steps);
    }

    protected override bool IsInputKey(Keys keyData) => keyData is Keys.Up or Keys.Down or Keys.PageUp or Keys.PageDown
        || base.IsInputKey(keyData);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        int delta = e.KeyCode switch
        {
            Keys.Up or Keys.PageUp => -1,
            Keys.Down or Keys.PageDown => 1,
            _ => 0,
        };
        if (delta == 0) return;
        ScrollByPage(delta);
        e.Handled = true;
    }

    private void ScrollByPage(int delta)
    {
        _page = Math.Clamp(_page + delta, 0, CurrentMaximumPage());
        Invalidate();
    }

    private Point ToLogical(Point point) => new(
        (int)(point.X / FrmShopMockup.ZoomFactor), (int)(point.Y / FrmShopMockup.ZoomFactor));

    private async Task EditPricesAsync(ShopCatalogItem item)
    {
        using var dialog = new ShopPriceDialog(item);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        _editing = true;
        try
        {
            await ShopCatalogEditor.SaveAsync(_iffPath, item, null, dialog.Price, dialog.DiscountPrice, dialog.RentalPrice,
                dialog.ShopFlags, dialog.MoneyFlags, dialog.TimeFlag, dialog.Time, dialog.StartDate, dialog.EndDate);
            item.Price = dialog.Price;
            item.DiscountPrice = dialog.DiscountPrice;
            item.RentalPrice = dialog.RentalPrice;
            item.ShopFlags = dialog.ShopFlags;
            item.MoneyFlags = dialog.MoneyFlags;
            item.TimeFlag = dialog.TimeFlag;
            item.Time = dialog.Time;
            item.StartDate = dialog.StartDate;
            item.EndDate = dialog.EndDate;
            MessageBox.Show(Strings.Shop_EditSaved, Strings.Shop_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(string.Format(LocalizationManager.CurrentCulture, Strings.Shop_EditFailed, ex.Message),
                Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _editing = false; Invalidate(); }
    }

    private async Task ChangeIconAsync(ShopCatalogItem item)
    {
        using var dialog = new OpenFileDialog
        {
            InitialDirectory = Path.GetDirectoryName(item.IconPath),
            Filter = Strings.Shop_IconFilter,
            Title = Strings.Shop_SelectIcon,
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK) return;
        string selectedPath = Path.GetFullPath(dialog.FileName);
        string rootPrefix = _assets.DataRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!selectedPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(Strings.Shop_IconMustBeInData, Strings.Shop_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        string iconId = Path.GetFileNameWithoutExtension(selectedPath);
        _editing = true;
        try
        {
            using Image probe = selectedPath.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)
                ? TgaDecoder.Load(selectedPath)
                : Image.FromFile(selectedPath);
            await ShopCatalogEditor.SaveAsync(_iffPath, item, iconId, item.Price, item.DiscountPrice, item.RentalPrice,
                item.ShopFlags, item.MoneyFlags, item.TimeFlag, item.Time, item.StartDate, item.EndDate);
            item.IconId = iconId;
            item.IconPath = selectedPath;
            MessageBox.Show(Strings.Shop_EditSaved, Strings.Shop_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or OutOfMemoryException)
        {
            MessageBox.Show(string.Format(LocalizationManager.CurrentCulture, Strings.Shop_EditFailed, ex.Message),
                Strings.Common_Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _editing = false; Invalidate(); }
    }

    private void CycleCategory()
    {
        int count = _catalog.Select(item => item.Category).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (count != 0) _categoryIndex = (_categoryIndex + 1) % count;
        _page = 0;
        Invalidate();
    }

    private void SetSearchFilter()
    {
        using var prompt = new ShopSearchDialog(_filter);
        if (prompt.ShowDialog(FindForm()) != DialogResult.OK) return;
        _filter = prompt.Filter.Trim();
        _page = 0;
        Invalidate();
    }

    private void Checkout()
    {
        if (_session.Cart.Count == 0) { MessageBox.Show(Strings.Shop_EmptyCart, Strings.Shop_Title); return; }
        (ulong pang, ulong cookies) = _session.Totals(_rental);
        DialogResult answer = MessageBox.Show(string.Format(Strings.Shop_ConfirmPurchase, pang, cookies), Strings.Shop_Title,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;
        if (!_session.TryCheckout(_rental)) MessageBox.Show(Strings.Shop_InsufficientFunds, Strings.Shop_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        else MessageBox.Show(Strings.Shop_PurchaseComplete, Strings.Shop_Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        Invalidate();
    }

    private ShopLayoutElement? HitElement(Point point) => _layout.Elements.LastOrDefault(element =>
    {
        if (element.Type is not ("BUTTON" or "TEXTBUTTON" or "TABBUTTON")) return false;
        Rectangle bounds = element.Bounds;
        if (bounds.Width == 0 || bounds.Height == 0)
        {
            string? resource = element.Parameters.GetValueOrDefault("normal");
            if (resource is null) return false;
            bounds.Size = GetImage(resource).Size;
        }
        return bounds.Contains(point);
    });

    private Image GetImage(string id) => _images.TryGetValue(id, out Image? image)
        ? image
        : _images[id] = GetImageByPath(_assets.Resolve(id));

    private Image GetImageByPath(string path)
    {
        if (_images.TryGetValue(path, out Image? image)) return image;
        if (path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase)) image = TgaDecoder.Load(path);
        else { using Image source = Image.FromFile(path); image = new Bitmap(source); }
        _images[path] = image;
        return image;
    }
}

internal sealed class ShopPriceDialog : Form
{
    private readonly NumericUpDown _price = CreatePriceEditor();
    private readonly NumericUpDown _discount = CreatePriceEditor();
    private readonly NumericUpDown _rental = CreatePriceEditor();
    private readonly FlagCheckBoxEditor _shopFlags;
    private readonly FlagCheckBoxEditor _moneyFlags;
    private readonly FlagCheckBoxEditor _timeFlag;
    private readonly NumericUpDown _time = CreateByteEditor();
    private readonly DateTimePicker _startDate = CreateDateEditor();
    private readonly DateTimePicker _endDate = CreateDateEditor();
    public uint Price => decimal.ToUInt32(_price.Value);
    public uint DiscountPrice => decimal.ToUInt32(_discount.Value);
    public uint RentalPrice => decimal.ToUInt32(_rental.Value);
    public byte ShopFlags => _shopFlags.Value;
    public byte MoneyFlags => _moneyFlags.Value;
    public byte TimeFlag => _timeFlag.Value;
    public byte Time => decimal.ToByte(_time.Value);
    public DateTime? StartDate => _startDate.Checked ? _startDate.Value : null;
    public DateTime? EndDate => _endDate.Checked ? _endDate.Value : null;

    public ShopPriceDialog(ShopCatalogItem item)
    {
        _shopFlags = new FlagCheckBoxEditor([
            Strings.Shop_FlagIsCash, Strings.Shop_FlagCanSendMailAndPersonalShop,
            Strings.Shop_FlagCanDuplicate, Strings.Shop_FlagShopSpecial,
            Strings.Shop_FlagBlockMailAndPersonalShop, Strings.Shop_FlagIsSaleable,
            Strings.Shop_FlagIsGift, Strings.Shop_FlagOnlyDisplay]);
        _moneyFlags = new FlagCheckBoxEditor([
            Strings.Shop_FlagInStock, Strings.Shop_FlagShowNew, Strings.Shop_FlagDisplayOnly,
            UnknownFlag(0x08), UnknownFlag(0x10), Strings.Shop_FlagShowHot,
            Strings.Shop_FlagShowSpecial, UnknownFlag(0x80)]);
        _timeFlag = new FlagCheckBoxEditor(Enumerable.Range(0, 8).Select(bit => UnknownFlag(1 << bit)));
        Text = Strings.Shop_EditPrices;
        ClientSize = new Size(550, 650);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        _price.Value = item.Price; _discount.Value = item.DiscountPrice; _rental.Value = item.RentalPrice;
        _shopFlags.Value = item.ShopFlags; _moneyFlags.Value = item.MoneyFlags;
        _timeFlag.Value = item.TimeFlag; _time.Value = item.Time;
        SetDate(_startDate, item.StartDate); SetDate(_endDate, item.EndDate);
        AddRow(Strings.Shop_Price, _price, 18);
        AddRow(Strings.Shop_DiscountPrice, _discount, 58);
        AddRow(Strings.Shop_RentalPrice, _rental, 98);
        AddFlagRow(Strings.Shop_ShopFlags, _shopFlags, 138);
        AddFlagRow(Strings.Shop_MoneyFlags, _moneyFlags, 250);
        AddFlagRow(Strings.Shop_TimeFlag, _timeFlag, 362);
        AddRow(Strings.Shop_Time, _time, 474);
        AddRow(Strings.Shop_StartDate, _startDate, 514);
        AddRow(Strings.Shop_EndDate, _endDate, 554);
        var save = new Button { Text = Strings.Common_OK, DialogResult = DialogResult.OK, Location = new Point(380, 610), Size = new Size(75, 28) };
        var cancel = new Button { Text = Strings.Options_Cancel, DialogResult = DialogResult.Cancel, Location = new Point(461, 610), Size = new Size(75, 28) };
        Controls.AddRange([save, cancel]);
        AcceptButton = save; CancelButton = cancel;
    }

    private void AddRow(string text, Control editor, int y)
    {
        Controls.Add(new Label { Text = text, Location = new Point(12, y + 4), Size = new Size(175, 24) });
        editor.Location = new Point(210, y); editor.Size = new Size(326, 27); Controls.Add(editor);
    }

    private void AddFlagRow(string text, FlagCheckBoxEditor editor, int y)
    {
        Controls.Add(new Label { Text = text, Location = new Point(12, y), Size = new Size(185, 100), TextAlign = ContentAlignment.MiddleLeft });
        editor.Location = new Point(210, y); editor.Size = new Size(326, 104); Controls.Add(editor);
    }

    private static NumericUpDown CreatePriceEditor() => new()
    {
        Minimum = 0, Maximum = uint.MaxValue, ThousandsSeparator = true,
    };

    private static NumericUpDown CreateByteEditor() => new()
    {
        Minimum = byte.MinValue, Maximum = byte.MaxValue,
        Hexadecimal = true,
    };

    private static DateTimePicker CreateDateEditor() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "yyyy-MM-dd HH:mm:ss",
        ShowCheckBox = true,
    };

    private static void SetDate(DateTimePicker picker, DateTime? value)
    {
        picker.Checked = value.HasValue;
        picker.Value = value ?? DateTime.Today;
    }

    private static string UnknownFlag(int mask) => string.Format(LocalizationManager.CurrentCulture,
        Strings.Shop_FlagUnknown, mask);
}

internal sealed class FlagCheckBoxEditor : UserControl
{
    private readonly CheckBox[] _bits;

    public FlagCheckBoxEditor(IEnumerable<string> labels)
    {
        BorderStyle = BorderStyle.FixedSingle;
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            Margin = Padding.Empty,
            Padding = new Padding(3),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        for (int row = 0; row < 4; row++) table.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
        string[] labelArray = labels.Take(8).ToArray();
        if (labelArray.Length != 8) throw new ArgumentException("Exactly eight flag labels are required.", nameof(labels));
        _bits = Enumerable.Range(0, 8).Select(bit => new CheckBox
        {
            AutoSize = false,
            Dock = DockStyle.Fill,
            AutoEllipsis = true,
            Margin = new Padding(2, 0, 2, 0),
            Text = labelArray[bit],
            Tag = bit,
        }).ToArray();
        for (int bit = 0; bit < _bits.Length; bit++) table.Controls.Add(_bits[bit], bit % 2, bit / 2);
        Controls.Add(table);
    }

    [System.ComponentModel.Browsable(false)]
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public byte Value
    {
        get => (byte)_bits.Where(checkBox => checkBox.Checked)
            .Aggregate(0, (value, checkBox) => value | 1 << (int)checkBox.Tag!);
        set
        {
            foreach (CheckBox checkBox in _bits)
                checkBox.Checked = (value & (1 << (int)checkBox.Tag!)) != 0;
        }
    }
}

internal sealed class ShopSearchDialog : Form
{
    private readonly TextBox _text = new() { Dock = DockStyle.Top };
    public string Filter => _text.Text;
    public ShopSearchDialog(string current)
    {
        Text = Strings.Shop_Search;
        ClientSize = new Size(360, 85);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = MinimizeBox = false;
        _text.Text = current;
        var ok = new Button { Text = Strings.Common_OK, DialogResult = DialogResult.OK, Location = new Point(195, 42), Size = new Size(75, 28) };
        var cancel = new Button { Text = Strings.Options_Cancel, DialogResult = DialogResult.Cancel, Location = new Point(276, 42), Size = new Size(75, 28) };
        Controls.AddRange([_text, ok, cancel]);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}
