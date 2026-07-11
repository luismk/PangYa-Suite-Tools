using System.Drawing;
using System.Text;
using PangYa_Suite_Tools.Shop;
using PangyaAPI.IFF;
using PangyaAPI.PAK.Models;
using Xunit;

namespace PangYa_Suite_Tools.Tests;

public sealed class ShopTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "PangYaShopTests", Guid.NewGuid().ToString("N"));

    public ShopTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void LayoutParser_ExpandsMacrosAndPreservesDuplicateNamesAndCoordinates()
    {
        string shop = Path.Combine(_directory, "shop.xml");
        string predefined = Path.Combine(_directory, "predefined.xml");
        File.WriteAllText(shop, """
            <?xml version="1.0" encoding="utf-8"?><resource><element type="FORM" name="shopmain" size="800 600">
            <item type="AREA" name="same" pos="10 20"><param name="bgimg" var="one"/></item>
            <item type="AREA" name="same" rect="30 40 80 90"/><item type="MACROITEM" resource="macro"/>
            </element></resource>
            """, Encoding.UTF8);
        File.WriteAllText(predefined, """
            <?xml version="1.0" encoding="utf-8"?><resource><element type="MACROITEM" name="macro">
            <item type="BUTTON" name="from_macro" rect="1 2 11 12"/></element></resource>
            """, Encoding.UTF8);

        ShopLayout layout = ShopLayoutParser.Load(shop, predefined);

        Assert.Equal(new Size(800, 600), layout.Size);
        Assert.Equal(3, layout.Elements.Count);
        Assert.Equal(2, layout.Elements.Count(element => element.Name == "same"));
        Assert.Equal(new Rectangle(10, 20, 0, 0), layout.Elements[0].Bounds);
        Assert.Equal(new Rectangle(30, 40, 50, 50), layout.Elements[1].Bounds);
        Assert.Equal("from_macro", layout.Elements[2].Name);
    }

    [Fact]
    public void LayoutParser_FindsShopMainFormCaseInsensitivelyWhenNested()
    {
        string shop = Path.Combine(_directory, "shop.xml");
        string predefined = Path.Combine(_directory, "predefined.xml");
        File.WriteAllText(shop, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <group>
                <ELEMENT type="form" name="ShopMain" size="1024 768">
                  <ITEM type="AREA" name="nested_area" rect="5 6 25 36">
                    <PARAM name="bgimg" var="panel"/>
                  </ITEM>
                </ELEMENT>
              </group>
            </resource>
            """, Encoding.UTF8);
        File.WriteAllText(predefined, """<?xml version="1.0" encoding="utf-8"?><resource/>""", Encoding.UTF8);

        ShopLayout layout = ShopLayoutParser.Load(shop, predefined);

        Assert.Equal(new Size(1024, 768), layout.Size);
        ShopLayoutElement element = Assert.Single(layout.Elements);
        Assert.Equal("nested_area", element.Name);
        Assert.Equal(new Rectangle(5, 6, 20, 30), element.Bounds);
        Assert.Equal("panel", element.Parameters["bgimg"]);
    }

    [Fact]
    public void LayoutParser_AcceptsFormTagWithIdAndUppercaseAttributes()
    {
        string shop = Path.Combine(_directory, "shop.xml");
        string predefined = Path.Combine(_directory, "predefined.xml");
        File.WriteAllText(shop, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <FORM ID="shopmain" SIZE="640 480">
                <ITEM TYPE="AREA" NAME="area_from_form_tag" RECT="1 2 21 32" />
              </FORM>
            </resource>
            """, Encoding.UTF8);
        File.WriteAllText(predefined, """<?xml version="1.0" encoding="utf-8"?><resource/>""", Encoding.UTF8);

        ShopLayout layout = ShopLayoutParser.Load(shop, predefined);

        Assert.Equal(new Size(640, 480), layout.Size);
        ShopLayoutElement element = Assert.Single(layout.Elements);
        Assert.Equal("AREA", element.Type);
        Assert.Equal("area_from_form_tag", element.Name);
        Assert.Equal(new Rectangle(1, 2, 20, 30), element.Bounds);
    }

    [Theory]
    [InlineData("""<FORM ID="shopmain" WIDTH="640" HEIGHT="480" />""", 640, 480)]
    [InlineData("""<FORM ID="shopmain" W="800" H="600" />""", 800, 600)]
    [InlineData("""<FORM ID="shopmain" RECT="10,20,1034,788" />""", 1024, 768)]
    [InlineData("""<FORM ID="shopmain" SIZE="width=1280 height=720" />""", 1280, 720)]
    public void LayoutParser_AcceptsAlternateShopMainSizeFormats(string formXml, int width, int height)
    {
        string shop = Path.Combine(_directory, "shop.xml");
        string predefined = Path.Combine(_directory, "predefined.xml");
        File.WriteAllText(shop, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <resource>{formXml}</resource>
            """, Encoding.UTF8);
        File.WriteAllText(predefined, """<?xml version="1.0" encoding="utf-8"?><resource/>""", Encoding.UTF8);

        ShopLayout layout = ShopLayoutParser.Load(shop, predefined);

        Assert.Equal(new Size(width, height), layout.Size);
        Assert.Empty(layout.Elements);
    }

    [Theory]
    [InlineData("""<base size="960 540" />""", 960, 540)]
    [InlineData("""<item type="BASE" rect="0 0 1024 768" />""", 1024, 768)]
    [InlineData("""<element name="base">1280 720</element>""", 1280, 720)]
    public void LayoutParser_UsesInlineBaseElementWhenShopMainHasNoSize(string baseXml, int width, int height)
    {
        string shop = Path.Combine(_directory, "shop.xml");
        string predefined = Path.Combine(_directory, "predefined.xml");
        File.WriteAllText(shop, $"""
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <FORM ID="shopmain">
                {baseXml}
              </FORM>
            </resource>
            """, Encoding.UTF8);
        File.WriteAllText(predefined, """<?xml version="1.0" encoding="utf-8"?><resource/>""", Encoding.UTF8);

        ShopLayout layout = ShopLayoutParser.Load(shop, predefined);

        Assert.Equal(new Size(width, height), layout.Size);
    }

    [Fact]
    public void LayoutParser_SkipsMissingMacrosInsteadOfFailingWholeShop()
    {
        string shop = Path.Combine(_directory, "shop.xml");
        string predefined = Path.Combine(_directory, "predefined.xml");
        File.WriteAllText(shop, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <FORM ID="shopmain" SIZE="640 480">
                <item type="MACROITEM" resource="under_tab_m" />
                <item type="AREA" name="still_loaded" rect="1 2 21 32" />
              </FORM>
            </resource>
            """, Encoding.UTF8);
        File.WriteAllText(predefined, """<?xml version="1.0" encoding="utf-8"?><resource/>""", Encoding.UTF8);

        ShopLayout layout = ShopLayoutParser.Load(shop, predefined);

        ShopLayoutElement element = Assert.Single(layout.Elements);
        Assert.Equal("still_loaded", element.Name);
    }

    [Fact]
    public void LayoutParser_ExpandsInlineShopMacroDefinitions()
    {
        string shop = Path.Combine(_directory, "shop.xml");
        string predefined = Path.Combine(_directory, "predefined.xml");
        File.WriteAllText(shop, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <FORM ID="shopmain" SIZE="640 480">
                <item type="MACROITEM" resource="under_tab_m" />
              </FORM>
              <under_tab_m>
                <ITEM TYPE="BUTTON" NAME="inline_macro_button" RECT="10 20 30 45" />
              </under_tab_m>
            </resource>
            """, Encoding.UTF8);
        File.WriteAllText(predefined, """<?xml version="1.0" encoding="utf-8"?><resource/>""", Encoding.UTF8);

        ShopLayout layout = ShopLayoutParser.Load(shop, predefined);

        ShopLayoutElement element = Assert.Single(layout.Elements);
        Assert.Equal("inline_macro_button", element.Name);
        Assert.Equal(new Rectangle(10, 20, 20, 25), element.Bounds);
    }

    [Fact]
    public void TgaDecoder_DecodesBottomOriginBgraAndAlpha()
    {
        string path = Path.Combine(_directory, "test.tga");
        byte[] bytes = new byte[18 + 8];
        bytes[2] = 2;
        bytes[12] = 1;
        bytes[14] = 2;
        bytes[16] = 32;
        bytes[17] = 8;
        bytes[18] = 30; bytes[19] = 20; bytes[20] = 10; bytes[21] = 40;
        bytes[22] = 70; bytes[23] = 60; bytes[24] = 50; bytes[25] = 80;
        File.WriteAllBytes(path, bytes);

        using Bitmap bitmap = TgaDecoder.Load(path);

        Assert.Equal(Color.FromArgb(80, 50, 60, 70), bitmap.GetPixel(0, 0));
        Assert.Equal(Color.FromArgb(40, 10, 20, 30), bitmap.GetPixel(0, 1));
    }

    [Fact]
    public void TgaDecoder_RejectsTruncatedPixels()
    {
        string path = Path.Combine(_directory, "bad.tga");
        byte[] bytes = new byte[18];
        bytes[2] = 2; bytes[12] = 1; bytes[14] = 1; bytes[16] = 32;
        File.WriteAllBytes(path, bytes);
        Assert.Throws<InvalidDataException>(() => TgaDecoder.Load(path));
    }

    [Fact]
    public void AssetResolver_PrefersShopAssetsAndRejectsMissingResources()
    {
        string preferred = Path.Combine(_directory, "ui", "shop_myroom", "button.tga");
        string other = Path.Combine(_directory, "ui", "other", "button.tga");
        Directory.CreateDirectory(Path.GetDirectoryName(preferred)!);
        Directory.CreateDirectory(Path.GetDirectoryName(other)!);
        File.WriteAllBytes(preferred, [1]);
        File.WriteAllBytes(other, [2]);
        var resolver = new ShopAssetResolver(_directory);
        Assert.Equal(preferred, resolver.Resolve("button"));
        Assert.Throws<FileNotFoundException>(() => resolver.Resolve("missing"));
    }

    [Fact]
    public void Session_ComputesBothCurrenciesAndChecksFundsAtomically()
    {
        var pang = new ShopCatalogItem("Item", 1, "Pang item", "a", 100, 80, 20, false, "a");
        var cash = new ShopCatalogItem("Item", 2, "Cash item", "b", 20_000, 0, 0, true, "b");
        var session = new ShopSession();
        session.Add(pang);
        session.Add(cash);
        Assert.Equal((80UL, 20_000UL), session.Totals(false));
        Assert.False(session.TryCheckout(false));
        Assert.Equal(2, session.Cart.Count);
        Assert.Equal(1_000_000UL, session.Pang);
        Assert.Equal(10_000UL, session.Cookies);
        session.Clear();
        session.Add(pang);
        Assert.True(session.TryCheckout(true));
        Assert.Equal(999_980UL, session.Pang);
        Assert.Empty(session.Cart);
    }

    [Theory]
    [InlineData(0x00, 0x02, "mark_new")]
    [InlineData(0x00, 0x20, "mark_hot")]
    [InlineData(0x00, 0x40, "mark_surprise_sale")]
    [InlineData(0x08, 0x00, "mark_surprise_sale")]
    [InlineData(0x00, 0x00, null)]
    public void BannerSelection_MapsShopDisplayFlags(byte shopFlags, byte moneyFlags, string? expected)
    {
        var item = new ShopCatalogItem("Item", 1, "Test", "icon", 1, 0, 0, false, "icon.tga",
            shopFlags: shopFlags, moneyFlags: moneyFlags);
        Assert.Equal(expected, ShopCanvas.GetBannerResource(item));
    }

    [Fact]
    public async Task CatalogEditor_PersistsPricesAndIconToLooseIff()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        string path = Path.Combine(_directory, "Item.iff");
        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema schema = IffSchemaRegistry.Resolve("Item.iff", header, 196)!;
        IffRecord record = IffRecord.CreateBlank(0, 196, schema);
        record.SetValue("Enabled", true, encoding);
        record.SetValue("ItemId", 123u, encoding);
        record.SetValue("Name", "Test", encoding);
        record.SetValue("Icon", "old_icon", encoding);
        await using (var output = File.Create(path))
            await IffWriter.WriteAsync(output, header, One(record));
        var item = new ShopCatalogItem("Item", 123, "Test", "old_icon", 1, 2, 3, false, "old.tga", "Item.iff", 0);

        DateTime start = new(2026, 1, 2, 3, 4, 5);
        DateTime end = new(2027, 6, 7, 8, 9, 10);
        await ShopCatalogEditor.SaveAsync(path, item, "new_icon", 100, 80, 25, 0xA5, 0xD2, 3, 7, start, end);

        await using IffContainer container = await IffContainer.OpenAsync(path);
        await using Stream stream = await container.Entries.Single().OpenAsync(default);
        await using IffReader reader = IffReader.Open(stream, "Item.iff", new(LeaveOpen: true, SchemaRegion: "TH"));
        IffRecord saved = await Single(reader.ReadRecordsAsync());
        Assert.Equal(100u, saved.GetValue("Price", encoding));
        Assert.Equal(80u, saved.GetValue("DiscountPrice", encoding));
        Assert.Equal(25u, saved.GetValue("UsedPrice", encoding));
        Assert.Equal("new_icon", saved.GetValue("Icon", encoding));
        Assert.Equal((byte)0xA5, saved.GetValue("ShopFlags", encoding));
        Assert.Equal((byte)0xD2, saved.GetValue("MoneyFlags", encoding));
        Assert.Equal((byte)3, saved.GetValue("TimeFlag", encoding));
        Assert.Equal((byte)7, saved.GetValue("Time", encoding));
        Assert.Equal(start, saved.GetValue("StartDate", encoding));
        Assert.Equal(end, saved.GetValue("EndDate", encoding));
    }

    [Fact]
    public async Task IffReferenceResolver_ResolvesLooseReferencedItemsAndMissingIcons()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        Directory.CreateDirectory(Path.Combine(_directory, "ui", "shop_myroom"));
        using (var bitmap = new Bitmap(8, 8))
        {
            bitmap.SetPixel(0, 0, Color.Red);
            bitmap.Save(Path.Combine(_directory, "ui", "shop_myroom", "item_icon.png"));
        }

        string itemPath = Path.Combine(_directory, "Item.iff");
        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema itemSchema = IffSchemaRegistry.Resolve("Item.iff", header, 196)!;
        IffRecord itemWithIcon = IffRecord.CreateBlank(0, 196, itemSchema);
        itemWithIcon.SetValue("ItemId", 123u, encoding);
        itemWithIcon.SetValue("Name", "Resolved Item", encoding);
        itemWithIcon.SetValue("Icon", "item_icon", encoding);
        IffRecord itemMissingIcon = IffRecord.CreateBlank(1, 196, itemSchema);
        itemMissingIcon.SetValue("ItemId", 456u, encoding);
        itemMissingIcon.SetValue("Name", "No Icon Item", encoding);
        itemMissingIcon.SetValue("Icon", "missing_icon", encoding);
        await using (var output = File.Create(itemPath))
            await IffWriter.WriteAsync(output, header, Many(itemWithIcon, itemMissingIcon));

        IffSchema setSchema = new("SetItem", 32,
        [
            new IffField("ItemCount", 0, 4, IffFieldType.UInt32),
            new IffField("Item1", 4, 4, IffFieldType.ItemIdReference,
                Reference: new IffFieldReference("Item.iff")),
            new IffField("Item2", 8, 4, IffFieldType.ItemIdReference,
                Reference: new IffFieldReference("Item.iff")),
            new IffField("Item3", 12, 4, IffFieldType.ItemIdReference,
                Reference: new IffFieldReference("Item.iff")),
            new IffField("Item1Count", 16, 2, IffFieldType.UInt16),
            new IffField("Item2Count", 18, 2, IffFieldType.UInt16),
            new IffField("Item3Count", 20, 2, IffFieldType.UInt16)
        ]);
        IffRecord setRecord = IffRecord.CreateBlank(0, 32, setSchema);
        setRecord.SetValue("ItemCount", 3u, encoding);
        setRecord.SetValue("Item1", 123u, encoding);
        setRecord.SetValue("Item2", 456u, encoding);
        setRecord.SetValue("Item3", 999u, encoding);
        setRecord.SetValue("Item1Count", (ushort)2, encoding);
        setRecord.SetValue("Item2Count", (ushort)4, encoding);
        setRecord.SetValue("Item3Count", (ushort)6, encoding);

        var document = new IffDocumentInfo("SetItem.iff", "TH", 32, setSchema, header);
        IIffReferenceResolver resolver = (await IffReferenceResolver.CreateAsync(
            document, null, _directory, Path.Combine(_directory, "SetItem.iff"), "TH", encoding,
            new EmbeddedIffSchemaProvider(), CancellationToken.None))!;

        IffReferenceCatalogItem[] catalog = resolver.GetCatalog(setSchema.Fields[1]).ToArray();
        Assert.Equal(2, catalog.Length);
        Assert.Contains(catalog, item => item.Key == 123u && item.Name == "Resolved Item" && item.IconPath is not null);

        IffReferenceDisplay resolved = resolver.Resolve(setSchema.Fields[1], setRecord.GetValue("Item1", encoding));
        IffReferenceDisplay missingIcon = resolver.Resolve(setSchema.Fields[2], setRecord.GetValue("Item2", encoding));
        IffReferenceDisplay missingRecord = resolver.Resolve(setSchema.Fields[3], setRecord.GetValue("Item3", encoding));

        Assert.Equal("Resolved Item", resolved.Name);
        Assert.NotNull(resolved.IconPath);
        Assert.Equal("No Icon Item", missingIcon.Name);
        Assert.True(missingIcon.MissingIcon);
        Assert.True(missingRecord.MissingRecord);
    }

    [Fact]
    public async Task IffReferenceResolver_UsesSelectedDataRootForLooseReferencedIffs()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        string dataRoot = Path.Combine(_directory, "selected-data");
        Directory.CreateDirectory(dataRoot);
        Directory.CreateDirectory(Path.Combine(dataRoot, "custom_icons"));
        using (var bitmap = new Bitmap(8, 8))
        {
            bitmap.SetPixel(0, 0, Color.Blue);
            bitmap.Save(Path.Combine(dataRoot, "custom_icons", "root_icon.png"));
        }

        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema itemSchema = IffSchemaRegistry.Resolve("Item.iff", header, 196)!;
        IffRecord item = IffRecord.CreateBlank(0, 196, itemSchema);
        item.SetValue("ItemId", 321u, encoding);
        item.SetValue("Name", "Data Root Item", encoding);
        item.SetValue("Icon", "root_icon", encoding);
        await using (var output = File.Create(Path.Combine(dataRoot, "Item.iff")))
            await IffWriter.WriteAsync(output, header, One(item));

        IffSchema setSchema = new("SetItem", 8,
        [
            new IffField("Item1", 0, 4, IffFieldType.ItemIdReference,
                Reference: new IffFieldReference("Item.iff"))
        ]);
        var document = new IffDocumentInfo("SetItem.iff", "TH", 8, setSchema, header);
        var schemaProvider = new StaticIffSchemaProvider(new IffSchema("Item", 196,
        [
            new IffField("ItemId", 4, 4, IffFieldType.UInt32),
            new IffField("Name", 8, 40, IffFieldType.FixedString, Encoding: Encoding.Latin1),
            new IffField("Icon", 49, 40, IffFieldType.Icon, Encoding: Encoding.Latin1, IconPath: "custom_icons")
        ]));

        IIffReferenceResolver resolver = (await IffReferenceResolver.CreateAsync(
            document, null, null, Path.Combine(_directory, "missing-source"), "TH", encoding,
            schemaProvider, CancellationToken.None, dataRoot))!;

        IffReferenceDisplay resolved = resolver.Resolve(setSchema.Fields[0], 321u);

        Assert.Equal(dataRoot, resolver.DataRoot);
        Assert.Equal("Data Root Item", resolved.Name);
        Assert.Equal(Path.Combine(dataRoot, "custom_icons", "root_icon.png"), resolved.IconPath);
    }

    [Fact]
    public async Task IffReferenceResolver_UsesPakExtractionSidecarAsDataRoot()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        string dataRoot = Path.Combine(_directory, "client");
        string dataDirectory = Path.Combine(dataRoot, "data");
        string iconDirectory = Path.Combine(dataRoot, "custom_icons");
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(iconDirectory);
        string pangyaPath = Path.Combine(dataDirectory, "pangya_th.iff");
        File.WriteAllBytes(pangyaPath, [1, 2, 3, 4]);
        PakExtractionSidecar.WriteForEntry(new PakFileEntry { Name = @"data\pangya_th.iff" }, pangyaPath);
        using (var bitmap = new Bitmap(8, 8))
        {
            bitmap.SetPixel(0, 0, Color.Green);
            bitmap.Save(Path.Combine(iconDirectory, "sidecar_icon.png"));
        }

        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema itemSchema = IffSchemaRegistry.Resolve("Item.iff", header, 196)!;
        IffRecord item = IffRecord.CreateBlank(0, 196, itemSchema);
        item.SetValue("ItemId", 654u, encoding);
        item.SetValue("Name", "Sidecar Item", encoding);
        item.SetValue("Icon", "sidecar_icon", encoding);
        await using (var output = File.Create(Path.Combine(dataRoot, "Item.iff")))
            await IffWriter.WriteAsync(output, header, One(item));

        IffSchema setSchema = new("SetItem", 4,
        [
            new IffField("Item1", 0, 4, IffFieldType.ItemIdReference,
                Reference: new IffFieldReference("Item.iff"))
        ]);
        var document = new IffDocumentInfo("SetItem.iff", "TH", 4, setSchema, header);
        var schemaProvider = new StaticIffSchemaProvider(new IffSchema("Item", 196,
        [
            new IffField("ItemId", 4, 4, IffFieldType.UInt32),
            new IffField("Name", 8, 40, IffFieldType.FixedString, Encoding: Encoding.Latin1),
            new IffField("Icon", 49, 40, IffFieldType.Icon, Encoding: Encoding.Latin1, IconPath: "custom_icons")
        ]));

        IIffReferenceResolver resolver = (await IffReferenceResolver.CreateAsync(
            document, null, dataDirectory, pangyaPath, "TH", encoding, schemaProvider, CancellationToken.None))!;
        IffReferenceDisplay resolved = resolver.Resolve(setSchema.Fields[0], 654u);

        Assert.Equal(dataRoot, resolver.DataRoot);
        Assert.Equal("Sidecar Item", resolved.Name);
        Assert.Equal(Path.Combine(iconDirectory, "sidecar_icon.png"), resolved.IconPath);
    }

    [Fact]
    public async Task IffReferenceResolver_AllowsMissingOptionalDisplayAndIconFields()
    {
        Encoding encoding = Encoding.ASCII;
        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema itemSchema = new("Item", 4,
            [new IffField("ItemId", 0, 4, IffFieldType.UInt32)]);
        IffRecord item = IffRecord.CreateBlank(0, 4, itemSchema);
        item.SetValue("ItemId", 42u, encoding);
        await using (var output = File.Create(Path.Combine(_directory, "Item.iff")))
            await IffWriter.WriteAsync(output, header, One(item));

        IffSchema setSchema = new("SetItem", 4,
        [
            new IffField("Item1", 0, 4, IffFieldType.ItemIdReference,
                Reference: new IffFieldReference("Item.iff", DisplayField: "MissingName", IconField: "MissingIcon"))
        ]);
        var document = new IffDocumentInfo("SetItem.iff", "TH", 4, setSchema, header);

        IIffReferenceResolver resolver = (await IffReferenceResolver.CreateAsync(
            document, null, _directory, Path.Combine(_directory, "SetItem.iff"), "TH", encoding,
            new StaticIffSchemaProvider(itemSchema), CancellationToken.None))!;

        IffReferenceCatalogItem catalogItem = Assert.Single(resolver.GetCatalog(setSchema.Fields[0]));
        IffReferenceDisplay display = resolver.Resolve(setSchema.Fields[0], 42u);

        Assert.Equal(42u, catalogItem.Key);
        Assert.Equal("42", catalogItem.Name);
        Assert.Equal("42", display.Name);
        Assert.False(display.MissingRecord);
        Assert.False(display.MissingIcon);
    }

    [Fact]
    public void IffReferenceResolver_BuildsItemIdTableRowWithCharacterName()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema schema = IffSchemaRegistry.Resolve("Item.iff", header, 196)!;
        IffRecord record = IffRecord.CreateBlank(0, 196, schema);
        record.SetValue("ItemId", 0u, encoding);
        record.SetValue("IFF Type", 0x2Au, encoding);
        record.SetValue("Character Serial", 7u, encoding);
        record.SetValue("Position", 3u, encoding);
        record.SetValue("Group", 2u, encoding);
        record.SetValue("Type", 1u, encoding);
        record.SetValue("Serial", 99u, encoding);
        record.SetValue("Name", "Item Name", encoding);
        record.SetValue("Icon", "item_icon", encoding);

        IffItemIdTableRow row = IffReferenceResolver.TryCreateItemIdRow(schema, record, encoding,
            "Item.iff", "Item Name", "item_icon", "icon.png",
            new Dictionary<uint, string> { [7] = "Nuri" })!;

        Assert.Equal("Item.iff", row.SourceFile);
        Assert.Equal(0x2Au, row.IffType);
        Assert.Equal(7u, row.CharacterSerial);
        Assert.Equal("Nuri", row.CharacterName);
        Assert.Equal(1u, row.Type);
        Assert.Equal(99u, row.Serial);
        Assert.Contains("Nuri", row.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void IffReferenceResolver_ItemIdTableRowIsOptionalWhenFieldsAreMissing()
    {
        var schema = new IffSchema("NoItemId", 8,
            [new IffField("Name", 0, 8, IffFieldType.FixedString)]);
        IffRecord record = IffRecord.CreateBlank(0, 8, schema);

        Assert.Null(IffReferenceResolver.TryCreateItemIdRow(schema, record, Encoding.ASCII,
            "NoItemId.iff", "Name", string.Empty, null));
    }

    private static async IAsyncEnumerable<IffRecord> One(IffRecord record)
    {
        yield return record;
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<IffRecord> Many(params IffRecord[] records)
    {
        foreach (IffRecord record in records) yield return record;
        await Task.CompletedTask;
    }

    private static async Task<IffRecord> Single(IAsyncEnumerable<IffRecord> records)
    {
        await foreach (IffRecord record in records) return record;
        throw new InvalidOperationException("No record was returned.");
    }

    private sealed class StaticIffSchemaProvider(IffSchema schema) : IIffSchemaProvider
    {
        public IffSchemaResolution Resolve(string fileName, string region, int recordSize) =>
            fileName.Equals("Item.iff", StringComparison.OrdinalIgnoreCase)
                ? new IffSchemaResolution(schema)
                : new IffSchemaResolution(null);
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { }
    }
}
