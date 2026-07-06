using System.Drawing;
using System.Text;
using PangYa_Suite_Tools.Shop;
using PangyaAPI.IFF;
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

    private static async IAsyncEnumerable<IffRecord> One(IffRecord record)
    {
        yield return record;
        await Task.CompletedTask;
    }

    private static async Task<IffRecord> Single(IAsyncEnumerable<IffRecord> records)
    {
        await foreach (IffRecord record in records) return record;
        throw new InvalidOperationException("No record was returned.");
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { }
    }
}
