using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using PangyaAPI.IFF;

namespace PangYa_Suite_Tools.Shop;

internal sealed record ShopLayoutElement(string Type, string Name, Rectangle Bounds,
    IReadOnlyDictionary<string, string> Parameters);

internal sealed record ShopLayout(Size Size, IReadOnlyList<ShopLayoutElement> Elements);

internal static class ShopLayoutParser
{
    private const int MaximumElements = 2048;

    public static ShopLayout Load(string shopXmlPath, string predefinedXmlPath)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        XmlDocument shop = LoadDocument(shopXmlPath);
        XmlDocument predefined = LoadDocument(predefinedXmlPath);
        XmlElement form = FindShopMainForm(shop)
            ?? throw new InvalidDataException(CreateMissingShopMainMessage(shop));
        Size size = ParseFormSize(form);
        if (size.Width <= 0 || size.Height <= 0 || size.Width > 4096 || size.Height > 4096)
            throw new InvalidDataException("The shop form dimensions are invalid.");

        var result = new List<ShopLayoutElement>();
        AddItems(form, shop, predefined, result, []);
        if (result.Count > MaximumElements)
            throw new InvalidDataException("The expanded shop layout contains too many elements.");
        return new ShopLayout(size, result);
    }

    private static XmlDocument LoadDocument(string path)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 4 * 1024 * 1024,
        };
        using XmlReader reader = XmlReader.Create(path, settings);
        var document = new XmlDocument { XmlResolver = null };
        document.Load(reader);
        return document;
    }

    private static void AddItems(XmlElement owner, XmlDocument shop, XmlDocument predefined,
                                 List<ShopLayoutElement> output, HashSet<string> macroStack)
    {
        foreach (XmlElement item in ChildElements(owner, "item"))
        {
            string type = GetAttribute(item, "type");
            if (type.Equals("MACROITEM", StringComparison.OrdinalIgnoreCase))
            {
                string resource = GetAttribute(item, "resource");
                if (string.IsNullOrWhiteSpace(resource))
                    continue;

                XmlElement? macro = FindMacro(predefined, shop, resource);
                if (macro == null)
                    continue;

                if (!macroStack.Add(resource))
                    throw new InvalidDataException($"The layout macro '{resource}' contains a recursive reference.");
                AddItems(macro, shop, predefined, output, macroStack);
                macroStack.Remove(resource);
                continue;
            }

            Rectangle bounds = ParseBounds(item);
            var parameters = ChildElements(item, "param")
                .Where(param => HasAttribute(param, "name"))
                .GroupBy(param => GetAttribute(param, "name"), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => GetAttribute(group.Last(), "var"), StringComparer.OrdinalIgnoreCase);
            output.Add(new ShopLayoutElement(type, GetAttribute(item, "name"), bounds, parameters));
        }
    }

    private static XmlElement? FindMacro(XmlDocument predefined, XmlDocument shop, string resource) =>
        FindLayoutDefinition(predefined, resource) ?? FindLayoutDefinition(shop, resource);

    private static XmlElement? FindShopMainForm(XmlDocument document)
    {
        XmlElement[] matches = DescendantElements(document.DocumentElement)
            .Where(IsShopMainElement)
            .ToArray();

        return matches.FirstOrDefault(IsExplicitFormElement)
            ?? matches.FirstOrDefault(element => !HasExplicitNonFormType(element))
            ?? matches.FirstOrDefault();
    }

    private static XmlElement? FindElement(XmlDocument document, string name, string? type = null) =>
        DescendantElements(document.DocumentElement)
            .Where(element => element.LocalName.Equals("element", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault(element =>
                GetAttribute(element, "name").Equals(name, StringComparison.OrdinalIgnoreCase) &&
                (type == null || GetAttribute(element, "type").Equals(type, StringComparison.OrdinalIgnoreCase)));

    private static XmlElement? FindLayoutDefinition(XmlDocument document, string name) =>
        DescendantElements(document.DocumentElement)
            .FirstOrDefault(element =>
                element.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                GetAttribute(element, "name").Equals(name, StringComparison.OrdinalIgnoreCase) ||
                GetAttribute(element, "id").Equals(name, StringComparison.OrdinalIgnoreCase));

    private static bool IsShopMainElement(XmlElement element) =>
        element.LocalName.Equals("shopmain", StringComparison.OrdinalIgnoreCase) ||
        AttributeValueEquals(element, "name", "shopmain") ||
        AttributeValueEquals(element, "id", "shopmain") ||
        AttributeValueEquals(element, "form", "shopmain") ||
        element.Attributes.OfType<XmlAttribute>()
            .Any(attribute => attribute.Value.Equals("shopmain", StringComparison.OrdinalIgnoreCase));

    private static bool IsExplicitFormElement(XmlElement element) =>
        element.LocalName.Equals("form", StringComparison.OrdinalIgnoreCase) ||
        GetAttribute(element, "type").Equals("FORM", StringComparison.OrdinalIgnoreCase);

    private static bool HasExplicitNonFormType(XmlElement element) =>
        HasAttribute(element, "type") &&
        !GetAttribute(element, "type").Equals("FORM", StringComparison.OrdinalIgnoreCase);

    private static bool AttributeValueEquals(XmlElement element, string attributeName, string expected) =>
        GetAttribute(element, attributeName).Equals(expected, StringComparison.OrdinalIgnoreCase);

    private static bool IsBaseElement(XmlElement element) =>
        element.LocalName.Equals("base", StringComparison.OrdinalIgnoreCase) ||
        AttributeValueEquals(element, "name", "base") ||
        AttributeValueEquals(element, "id", "base") ||
        AttributeValueEquals(element, "type", "base");

    private static IEnumerable<XmlElement> ChildElements(XmlElement owner, string localName) =>
        owner.ChildNodes
            .OfType<XmlElement>()
            .Where(element => element.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<XmlElement> DescendantElements(XmlElement? root)
    {
        if (root == null) yield break;

        yield return root;
        foreach (XmlElement child in root.ChildNodes.OfType<XmlElement>())
        {
            foreach (XmlElement descendant in DescendantElements(child))
                yield return descendant;
        }
    }

    private static Rectangle ParseBounds(XmlElement item)
    {
        if (HasAttribute(item, "rect"))
        {
            int[] values = ParseNumbers(GetAttribute(item, "rect"), 4, $"{GetAttribute(item, "name")} rect");
            if (values[2] < values[0] || values[3] < values[1]) throw new InvalidDataException("A shop rectangle is inverted.");
            return Rectangle.FromLTRB(values[0], values[1], values[2], values[3]);
        }
        if (HasAttribute(item, "pos"))
        {
            Size point = ParsePair(GetAttribute(item, "pos"), $"{GetAttribute(item, "name")} pos");
            return new Rectangle(point.Width, point.Height, 0, 0);
        }
        return Rectangle.Empty;
    }

    private static Size ParseFormSize(XmlElement form)
    {
        if (TryParseElementSize(form, "shopmain", out Size formSize))
            return formSize;

        foreach (XmlElement baseElement in DescendantElements(form).Skip(1).Where(IsBaseElement))
        {
            if (TryParseElementSize(baseElement, "shopmain base", out Size baseSize))
                return baseSize;
        }

        string described = string.Join(", ", form.Attributes.OfType<XmlAttribute>()
            .Select(attribute => $"{attribute.Name}='{attribute.Value}'"));
        throw new InvalidDataException(
            string.IsNullOrWhiteSpace(described)
                ? "Invalid shopmain size: no size, width/height, rect, or inline base element was found."
                : $"Invalid shopmain size: no usable size, width/height, rect, or inline base element was found on shopmain ({described}).");
    }

    private static bool TryParseElementSize(XmlElement element, string label, out Size size)
    {
        if (HasAttribute(element, "size"))
        {
            size = ParsePair(GetAttribute(element, "size"), $"{label} size");
            return true;
        }

        string width = FirstAttribute(element, "width", "w", "cx");
        string height = FirstAttribute(element, "height", "h", "cy");
        if (!string.IsNullOrWhiteSpace(width) || !string.IsNullOrWhiteSpace(height))
        {
            if (int.TryParse(width, out int parsedWidth) &&
                int.TryParse(height, out int parsedHeight))
            {
                size = new Size(parsedWidth, parsedHeight);
                return true;
            }

            throw new InvalidDataException($"Invalid {label} size: width='{width}', height='{height}'.");
        }

        string rect = FirstAttribute(element, "rect", "bounds");
        if (!string.IsNullOrWhiteSpace(rect))
        {
            int[] values = ParseNumbers(rect, 4, $"{label} rect");
            if (values[2] < values[0] || values[3] < values[1])
                throw new InvalidDataException($"The {label} rectangle is inverted.");
            size = new Size(values[2] - values[0], values[3] - values[1]);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(element.InnerText) && !ChildElements(element, "item").Any())
        {
            int[] values = ParseNumbers(element.InnerText, 2, $"{label} text size");
            size = new Size(values[0], values[1]);
            return true;
        }

        size = Size.Empty;
        return false;
    }

    private static Size ParsePair(string value, string label)
    {
        int[] numbers = ParseNumbers(value, 2, label);
        return new Size(numbers[0], numbers[1]);
    }

    private static int[] ParseNumbers(string value, int count, string label)
    {
        int[] numbers = Regex.Matches(value, @"-?\d+")
            .Select(match => int.Parse(match.Value))
            .ToArray();
        if (numbers.Length != count)
            throw new InvalidDataException($"Invalid {label}: '{value}'.");
        return numbers;
    }

    private static bool HasAttribute(XmlElement element, string name) =>
        element.Attributes.OfType<XmlAttribute>()
            .Any(attribute => attribute.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static string GetAttribute(XmlElement element, string name) =>
        element.Attributes.OfType<XmlAttribute>()
            .FirstOrDefault(attribute => attribute.LocalName.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?.Value ?? string.Empty;

    private static string FirstAttribute(XmlElement element, params string[] names)
    {
        foreach (string name in names)
        {
            string value = GetAttribute(element, name);
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return string.Empty;
    }

    private static string CreateMissingShopMainMessage(XmlDocument document)
    {
        string[] candidates = DescendantElements(document.DocumentElement)
            .Where(element => element.LocalName.Equals("form", StringComparison.OrdinalIgnoreCase) ||
                              GetAttribute(element, "type").Equals("FORM", StringComparison.OrdinalIgnoreCase))
            .Select(DescribeElement)
            .Take(8)
            .ToArray();

        return candidates.Length == 0
            ? "shop.xml does not contain the shopmain form. No FORM elements were found."
            : $"shop.xml does not contain the shopmain form. FORM candidates found: {string.Join(", ", candidates)}.";
    }

    private static string DescribeElement(XmlElement element)
    {
        string name = GetAttribute(element, "name");
        string id = GetAttribute(element, "id");
        string type = GetAttribute(element, "type");
        string identifier = string.IsNullOrWhiteSpace(name)
            ? string.IsNullOrWhiteSpace(id) ? element.LocalName : $"id='{id}'"
            : $"name='{name}'";
        return string.IsNullOrWhiteSpace(type) ? $"<{element.LocalName} {identifier}>" : $"<{element.LocalName} type='{type}' {identifier}>";
    }

}

internal sealed class ShopAssetResolver
{
    private static readonly string[] PreferredSegments = ["ui\\shop_myroom", "ui\\frames", "ui\\buttons"];
    private readonly Dictionary<string, List<string>> _files;
    public string DataRoot { get; }

    public ShopAssetResolver(string dataRoot)
    {
        DataRoot = Path.GetFullPath(dataRoot);
        _files = Directory.EnumerateFiles(dataRoot, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
            .Select(path => (Path: path, Name: Path.GetFileNameWithoutExtension(path)))
            .Where(file => !string.IsNullOrEmpty(file.Name))
            .GroupBy(file => file.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(file => file.Path).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList(),
                StringComparer.OrdinalIgnoreCase);
    }

    public string Resolve(string resourceId)
    {
        if (!_files.TryGetValue(resourceId, out List<string>? matches) || matches.Count == 0)
            throw new FileNotFoundException($"The shop image resource '{resourceId}' was not found.");
        if (matches.Count == 1) return matches[0];
        foreach (string segment in PreferredSegments)
        {
            List<string> preferred = matches.Where(path => path.Contains(segment, StringComparison.OrdinalIgnoreCase)).ToList();
            if (preferred.Count == 1) return preferred[0];
        }
        if (matches.Select(path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))))
            .Distinct(StringComparer.Ordinal).Count() == 1)
            return matches[0];
        throw new InvalidDataException($"The shop image resource '{resourceId}' is ambiguous: {string.Join(", ", matches)}");
    }

    public string? TryResolve(string resourceId)
    {
        try { return Resolve(resourceId); }
        catch (FileNotFoundException) { return null; }
        catch (InvalidDataException) { return null; }
    }
}

internal static class TgaDecoder
{
    public static Bitmap Load(string path)
    {
        using FileStream input = File.OpenRead(path);
        Span<byte> header = stackalloc byte[18];
        input.ReadExactly(header);
        int idLength = header[0];
        int imageType = header[2];
        int width = header[12] | header[13] << 8;
        int height = header[14] | header[15] << 8;
        int bitsPerPixel = header[16];
        if (imageType != 2 || bitsPerPixel != 32) throw new InvalidDataException("Only uncompressed 32-bit TGA images are supported.");
        if (width <= 0 || height <= 0 || width > 8192 || height > 8192) throw new InvalidDataException("The TGA dimensions are invalid.");
        long byteCount = checked((long)width * height * 4);
        if (input.Length - 18 - idLength < byteCount) throw new InvalidDataException("The TGA pixel data is truncated.");
        input.Position = 18 + idLength;
        byte[] pixels = GC.AllocateUninitializedArray<byte>(checked((int)byteCount));
        input.ReadExactly(pixels);

        bool topOrigin = (header[17] & 0x20) != 0;
        bool rightOrigin = (header[17] & 0x10) != 0;
        bool hasAlpha = (header[17] & 0x0F) != 0;
        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
        try
        {
            byte[] destination = new byte[checked(data.Stride * height)];
            for (int sourceY = 0; sourceY < height; sourceY++)
            {
                int targetY = topOrigin ? sourceY : height - sourceY - 1;
                for (int sourceX = 0; sourceX < width; sourceX++)
                {
                    int targetX = rightOrigin ? width - sourceX - 1 : sourceX;
                    int sourceOffset = (sourceY * width + sourceX) * 4;
                    int targetOffset = targetY * data.Stride + targetX * 4;
                    Buffer.BlockCopy(pixels, sourceOffset, destination, targetOffset, 4);
                    if (!hasAlpha) destination[targetOffset + 3] = byte.MaxValue;
                }
            }
            Marshal.Copy(destination, 0, data.Scan0, destination.Length);
        }
        finally { bitmap.UnlockBits(data); }
        return bitmap;
    }
}

internal sealed class ShopCatalogItem
{
    public ShopCatalogItem(string category, uint itemId, string name, string iconId, uint price,
        uint discountPrice, uint rentalPrice, bool isCash, string iconPath, string entryName = "", int recordIndex = -1,
        byte shopFlags = 0, byte moneyFlags = 0, byte timeFlag = 0, byte time = 0,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        Category = category; ItemId = itemId; Name = name; IconId = iconId; Price = price;
        DiscountPrice = discountPrice; RentalPrice = rentalPrice; IsCash = isCash;
        IconPath = iconPath; EntryName = entryName; RecordIndex = recordIndex; ShopFlags = shopFlags;
        MoneyFlags = moneyFlags; TimeFlag = timeFlag; Time = time; StartDate = startDate; EndDate = endDate;
    }
    public string Category { get; }
    public uint ItemId { get; }
    public string Name { get; }
    public string IconId { get; set; }
    public uint Price { get; set; }
    public uint DiscountPrice { get; set; }
    public uint RentalPrice { get; set; }
    public bool IsCash { get; }
    public string IconPath { get; set; }
    public string EntryName { get; }
    public int RecordIndex { get; }
    public byte ShopFlags { get; set; }
    public byte MoneyFlags { get; set; }
    public byte TimeFlag { get; set; }
    public byte Time { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public uint PurchasePrice => DiscountPrice != 0 ? DiscountPrice : Price;
}

internal sealed record ShopCatalogLoadResult(IReadOnlyList<ShopCatalogItem> Items, int MissingIconCount);

internal static class ShopCatalogLoader
{
    public static async Task<ShopCatalogLoadResult> LoadAsync(string iffPath, ShopAssetResolver assets,
        CancellationToken cancellationToken)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        var items = new List<ShopCatalogItem>();
        int missingIconCount = 0;
        await using IffContainer container = await IffContainer.OpenAsync(iffPath, cancellationToken: cancellationToken);
        foreach (IffContainerEntry entry in container.Entries.OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using Stream stream = await entry.OpenAsync(cancellationToken);
            await using IffReader reader = IffReader.Open(stream, Path.GetFileName(entry.Name), new(LeaveOpen: true, SchemaRegion: "TH"));
            IffSchema? schema = reader.Info.Schema;
            string[] required = ["Enabled", "ItemId", "Name", "Icon", "Price", "DiscountPrice", "UsedPrice", "IsCash", "IsSaleable",
                "ShopFlags", "MoneyFlags", "TimeFlag", "Time", "StartDate", "EndDate"];
            if (schema is null || required.Any(name => !schema.Fields.Any(field => field.Name.Equals(name, StringComparison.OrdinalIgnoreCase))))
                continue;
            await foreach (IffRecord record in reader.ReadRecordsAsync(cancellationToken))
            {
                if (!(bool)record.GetValue("Enabled", encoding)!) continue;
                string name = Convert.ToString(record.GetValue("Name", encoding))?.Trim() ?? string.Empty;
                string icon = Convert.ToString(record.GetValue("Icon", encoding))?.Trim() ?? string.Empty;
                uint price = Convert.ToUInt32(record.GetValue("Price", encoding));
                uint discount = Convert.ToUInt32(record.GetValue("DiscountPrice", encoding));
                if (name.Length == 0 || icon.Length == 0 || (price == 0 && discount == 0)) continue;
                string? iconPath = assets.TryResolve(Path.GetFileNameWithoutExtension(icon));
                if (iconPath is null) { missingIconCount++; continue; }
                items.Add(new ShopCatalogItem(Path.GetFileNameWithoutExtension(entry.Name),
                    Convert.ToUInt32(record.GetValue("ItemId", encoding)), name, icon, price, discount,
                    Convert.ToUInt32(record.GetValue("UsedPrice", encoding)),
                    (bool)record.GetValue("IsCash", encoding)!, iconPath, entry.Name, record.Index,
                    Convert.ToByte(record.GetValue("ShopFlags", encoding)), Convert.ToByte(record.GetValue("MoneyFlags", encoding)),
                    Convert.ToByte(record.GetValue("TimeFlag", encoding)), Convert.ToByte(record.GetValue("Time", encoding)),
                    record.GetValue("StartDate", encoding) as DateTime?, record.GetValue("EndDate", encoding) as DateTime?));
            }
        }
        return new ShopCatalogLoadResult(items, missingIconCount);
    }
}

internal static class ShopCatalogEditor
{
    public static async Task SaveAsync(string iffPath, ShopCatalogItem item, string? iconId,
        uint price, uint discountPrice, uint rentalPrice, byte shopFlags, byte moneyFlags,
        byte timeFlag, byte time, DateTime? startDate, DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(item.EntryName) || item.RecordIndex < 0)
            throw new InvalidOperationException("The catalog item is not linked to an IFF record.");
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        await using IffContainer container = await IffContainer.OpenAsync(iffPath, cancellationToken: cancellationToken);
        IffContainerEntry entry = container.Entries.Single(candidate =>
            candidate.Name.Equals(item.EntryName, StringComparison.OrdinalIgnoreCase));
        var records = new List<IffRecord>();
        IffHeader header;
        await using (Stream stream = await entry.OpenAsync(cancellationToken))
        await using (IffReader reader = IffReader.Open(stream, Path.GetFileName(entry.Name),
            new(LeaveOpen: true, SchemaRegion: "TH")))
        {
            header = reader.Info.Header;
            await foreach (IffRecord record in reader.ReadRecordsAsync(cancellationToken)) records.Add(record);
        }
        IffRecord target = records.Single(record => record.Index == item.RecordIndex &&
            Convert.ToUInt32(record.GetValue("ItemId", encoding)) == item.ItemId);
        target.SetValue("Price", price, encoding);
        target.SetValue("DiscountPrice", discountPrice, encoding);
        target.SetValue("UsedPrice", rentalPrice, encoding);
        SetFlagByte(target, "ShopFlags", shopFlags, encoding);
        SetFlagByte(target, "MoneyFlags", moneyFlags, encoding);
        target.SetValue("TimeFlag", timeFlag, encoding);
        target.SetValue("Time", time, encoding);
        target.SetValue("StartDate", startDate, encoding);
        target.SetValue("EndDate", endDate, encoding);
        if (!string.IsNullOrWhiteSpace(iconId)) target.SetValue("Icon", iconId, encoding);
        await container.SaveEntryAsync(entry.Name, header, records, cancellationToken);
    }

    private static void SetFlagByte(IffRecord record, string aggregateName, byte value, Encoding encoding)
    {
        IffSchema schema = record.Schema ?? throw new InvalidDataException("The IFF record has no schema.");
        IffField aggregate = schema.Fields.Single(field => field.Name.Equals(aggregateName, StringComparison.OrdinalIgnoreCase));
        if (aggregate.Type != IffFieldType.Byte || aggregate.Width != 1 ||
            !MemoryMarshal.TryGetArray(record.Bytes, out ArraySegment<byte> bytes) || bytes.Array is null)
            throw new InvalidDataException($"The aggregate flag field '{aggregateName}' is not a writable byte.");
        bytes.Array[bytes.Offset + aggregate.Offset] = value;
    }
}

internal sealed class ShopSession
{
    private readonly List<ShopCatalogItem> _cart = [];
    public ulong Pang { get; private set; } = 1_000_000;
    public ulong Cookies { get; private set; } = 10_000;
    public IReadOnlyList<ShopCatalogItem> Cart => _cart;
    public void Add(ShopCatalogItem item) => _cart.Add(item);
    public void Clear() => _cart.Clear();
    public (ulong Pang, ulong Cookies) Totals(bool rental) =>
        (_cart.Where(item => !item.IsCash).Aggregate(0UL, (total, item) => total + (rental && item.RentalPrice != 0 ? item.RentalPrice : item.PurchasePrice)),
         _cart.Where(item => item.IsCash).Aggregate(0UL, (total, item) => total + (rental && item.RentalPrice != 0 ? item.RentalPrice : item.PurchasePrice)));
    public bool TryCheckout(bool rental)
    {
        (ulong pang, ulong cookies) = Totals(rental);
        if (pang > Pang || cookies > Cookies) return false;
        Pang -= pang;
        Cookies -= cookies;
        _cart.Clear();
        return true;
    }
}
