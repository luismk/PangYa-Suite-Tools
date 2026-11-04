using System.Drawing;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using PangYa_Suite_Tools.Localization;
using PangYa_Suite_Tools.Logging;
using PangYa_Suite_Tools.Shop;
using PangyaAPI.IFF;
using PangyaAPI.UI;
using PangyaAPI.PAK.Models;
using Xunit;

namespace PangYa_Suite_Tools.Tests;

public sealed class ShopTests : IDisposable
{
    [Theory]
    [InlineData("pos='5 7'", 5, 7, 10, 10)]
    [InlineData("pos='5 7' size='0 0'", 5, 7, 0, 0)]
    [InlineData("pos='5 7' size='20 30'", 5, 7, 20, 30)]
    [InlineData("pos='5 7' rect='1 2 21 32'", 5, 7, 10, 10)]
    [InlineData("position='5 7'", 5, 7, 0, 0)]
    [InlineData("pos='invalid'", 0, 0, 0, 0)]
    public void UiBoundsUseFreshDefaultForPositionOnlyItems(
        string attributes, int x, int y, int width, int height)
    {
        var document = new XmlDocument();
        document.LoadXml($"<item {attributes}/>");
        var item = Assert.IsType<XmlElement>(document.DocumentElement);

        Assert.Equal(new Rectangle(x, y, width, height), PangyaUiNode.ParseBounds(item));
    }

    private readonly string _directory = Path.Combine(Path.GetTempPath(), "PangYaShopTests", Guid.NewGuid().ToString("N"));

    public ShopTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void ShopDataFiles_UsesDiscoveredUiFilesFromDataFolderOrGameRoot()
    {
        string dataRoot = Path.Combine(_directory, "client", "data");
        string uiRoot = Path.Combine(dataRoot, "ui");
        string nestedUi = Path.Combine(uiRoot, "layouts");
        Directory.CreateDirectory(nestedUi);
        string shopXml = Path.Combine(nestedUi, "shop.xml");
        string predefinedXml = Path.Combine(uiRoot, "predefined.xml");
        string iffPath = Path.Combine(dataRoot, "pangya_th.iff");
        File.WriteAllText(shopXml, "<resource/>");
        File.WriteAllText(predefinedXml, "<resource/>");
        File.WriteAllBytes(iffPath, []);

        ShopDataFiles fromGameRoot =
            ShopDataFiles.Resolve(Path.Combine(_directory, "client"));
        ShopDataFiles fromDataRoot = ShopDataFiles.Resolve(dataRoot);

        Assert.Equal(dataRoot, fromGameRoot.DataRoot);
        Assert.Equal(shopXml, fromGameRoot.ShopXml);
        Assert.Equal(predefinedXml, fromGameRoot.PredefinedXml);
        Assert.Equal(iffPath, fromGameRoot.IffPath);
        Assert.Equal(fromGameRoot, fromDataRoot);
    }

    [Fact]
    public void ShopDataFiles_RejectsAmbiguousNestedShopXmlFiles()
    {
        string uiRoot = Path.Combine(_directory, "data", "ui");
        Directory.CreateDirectory(Path.Combine(uiRoot, "one"));
        Directory.CreateDirectory(Path.Combine(uiRoot, "two"));
        File.WriteAllText(Path.Combine(uiRoot, "one", "shop.xml"),
            "<resource/>");
        File.WriteAllText(Path.Combine(uiRoot, "two", "shop.xml"),
            "<resource/>");
        File.WriteAllText(Path.Combine(uiRoot, "predefined.xml"),
            "<resource/>");
        File.WriteAllBytes(Path.Combine(_directory, "data", "pangya_th.iff"),
            []);

        Assert.Throws<InvalidDataException>(() =>
            ShopDataFiles.Resolve(Path.Combine(_directory, "data")));
    }

    [Fact]
    public void ShopDataFiles_UsesPakPathManifestToFindSiblingUiTree()
    {
        string extractionRoot = Path.Combine(_directory, "extracted-client");
        string dataDirectory = Path.Combine(extractionRoot, "data");
        string uiDirectory = Path.Combine(extractionRoot, "ui");
        Directory.CreateDirectory(dataDirectory);
        Directory.CreateDirectory(uiDirectory);
        string iffPath = Path.Combine(dataDirectory, "pangya_jp.iff");
        string shopXml = Path.Combine(uiDirectory, "shop.xml");
        string predefinedXml = Path.Combine(uiDirectory, "predefined.xml");
        File.WriteAllBytes(iffPath, []);
        File.WriteAllText(shopXml, "<resource/>");
        File.WriteAllText(predefinedXml, "<resource/>");
        PakExtractionSidecar.WriteManifest(
        [
            (new PakFileEntry { Name = "data/pangya_jp.iff" }, iffPath),
            (new PakFileEntry { Name = "ui/shop.xml" }, shopXml),
            (new PakFileEntry { Name = "ui/predefined.xml" }, predefinedXml),
        ]);

        ShopDataFiles files = ShopDataFiles.Resolve(dataDirectory);

        Assert.Equal(extractionRoot, files.DataRoot);
        Assert.Equal(shopXml, files.ShopXml);
        Assert.Equal(predefinedXml, files.PredefinedXml);
        Assert.Equal(iffPath, files.IffPath);
        Assert.True(File.Exists(Path.Combine(dataDirectory,
            PakExtractionSidecar.ManifestFileName)));
    }

    [Fact]
    public void ShopDataFiles_RejectsMultiplePangyaIffArchives()
    {
        string dataDirectory = Path.Combine(_directory, "client", "data");
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllBytes(Path.Combine(dataDirectory, "pangya_jp.iff"), []);
        File.WriteAllBytes(Path.Combine(dataDirectory, "pangya_th.iff"), []);

        InvalidDataException error = Assert.Throws<InvalidDataException>(() =>
            ShopDataFiles.Resolve(Path.Combine(_directory, "client")));

        Assert.Contains("pangya_jp.iff", error.Message,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains("pangya_th.iff", error.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("pangya_jp.iff", 999, 42, "JP")]
    [InlineData("pangya_gb.iff", 999, 42, "Global")]
    [InlineData("pangya_jp.iff", 30447, 11, "Global")]
    [InlineData("pangya.iff", 30447, 11, "Global")]
    [InlineData("pangya.iff", 0, 11, "TH")]
    public async Task ShopRegionProbe_UsesFilenameThenKnownHeader(
        string fileName, ushort revision, byte magic, string expected)
    {
        string path = Path.Combine(_directory, fileName);
        byte[] bytes =
            [1, 0, (byte)revision, (byte)(revision >> 8), magic, 0, 0, 0, 0];
        if (magic is 11 or 12 or 13)
            await File.WriteAllBytesAsync(path, bytes);
        else
        {
            using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create);
            ZipArchiveEntry entry = archive.CreateEntry("Item.iff");
            await using Stream output = entry.Open();
            await output.WriteAsync(bytes);
        }

        ShopRegionProbe probe = await ShopCatalogLoader.ProbeRegionAsync(path,
            CancellationToken.None);

        Assert.Equal(expected, probe.Region);
    }

    [Fact]
    public async Task ShopRegionProbe_LeavesUnknownHeaderForInteractiveSelection()
    {
        string path = Path.Combine(_directory, "pangya.iff");
        using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            ZipArchiveEntry entry = archive.CreateEntry("Item.iff");
            await using Stream output = entry.Open();
            await output.WriteAsync(new byte[] { 1, 0, 231, 3, 42, 0, 0, 0, 0 });
        }

        ShopRegionProbe probe = await ShopCatalogLoader.ProbeRegionAsync(path,
            CancellationToken.None);

        Assert.Null(probe.Region);
        Assert.NotNull(probe.Document);
        Assert.Equal("Unknown", probe.Document.Region);
    }

    [Fact]
    public async Task UiDocument_LoadEditAndAtomicSave_RoundTripsElementPropertiesAndStates()
    {
        string uiDirectory = Path.Combine(_directory, "ui");
        Directory.CreateDirectory(uiDirectory);
        string path = Path.Combine(uiDirectory, "shop.xml");
        await File.WriteAllTextAsync(path, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <element type="FORM" name="shopmain" size="800 600">
                <item type="BUTTON" name="buy" rect="10 20 110 60">
                  <param name="normal" var="buy_normal.tga"/>
                  <param name="over" var="buy_hover.tga"/>
                  <param name="selected" var="buy_selected.tga"/>
                </item>
              </element>
            </resource>
            """, Encoding.UTF8);

        PangyaUiDocument document = PangyaUiDocument.Load(path);
        Assert.Equal(new Size(800, 600), document.CanvasSize);
        PangyaUiNode button = Assert.Single(document.Nodes, node => node.Type == "BUTTON");
        Assert.Equal(new Rectangle(10, 20, 100, 40), button.Bounds);
        Assert.Equal("buy_hover.tga", button.GetResource(PangyaUiButtonState.Hover));
        Assert.Equal("buy_selected.tga", button.GetResource(PangyaUiButtonState.Selected));

        button.SetName("purchase");
        button.SetBounds(new Rectangle(25, 35, 120, 45));
        button.SetResource("purchase_normal.tga");
        await document.SaveAtomicAsync();

        PangyaUiNode saved = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Type == "BUTTON");
        Assert.Equal("purchase", saved.Name);
        Assert.Equal(new Rectangle(25, 35, 120, 45), saved.Bounds);
        Assert.Equal("purchase_normal.tga", saved.GetResource(PangyaUiButtonState.Normal));
        Assert.Empty(Directory.EnumerateFiles(uiDirectory, "*.tmp"));
    }

    [Fact]
    public async Task UiDocument_LoadsLegacyBareAmpersandsAndSavesValidXml()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(51949);
        string path = Path.Combine(_directory, "topicons.xml");
        string xml = """
            <?xml version="1.0" encoding="euc-kr" standalone="yes" ?>
            <TopIcons>
              <!-- Preserve comment A & B. -->
              <Icon Type="WEBICON" Name="WebEvent1"
                    Url="https://example.test/login?ID=%s&KID=%s&amp;valid=1" />
            </TopIcons>
            """;
        await File.WriteAllTextAsync(path, xml, encoding);

        PangyaUiDocument document = PangyaUiDocument.Load(path);
        await document.SaveAtomicAsync();

        string saved = await File.ReadAllTextAsync(path, encoding);
        Assert.Contains("ID=%s&amp;KID=%s&amp;valid=1", saved,
            StringComparison.Ordinal);
        Assert.DoesNotContain("&amp;amp;", saved, StringComparison.Ordinal);
        Assert.Contains("comment A & B", saved, StringComparison.Ordinal);
        var strict = new XmlDocument { XmlResolver = null };
        strict.LoadXml(saved);
    }

    [Fact]
    public void ShopLayout_LoadsBareAmpersandResourceValues()
    {
        string shop = Path.Combine(_directory, "shop.xml");
        string predefined = Path.Combine(_directory, "predefined.xml");
        File.WriteAllText(shop, """
            <resource>
              <FORM ID="shopmain" SIZE="800 600">
                <item type="BUTTON" name="web" rect="0 0 20 20">
                  <param name="normal" var="button?a=1&b=2" />
                </item>
              </FORM>
            </resource>
            """, Encoding.UTF8);
        File.WriteAllText(predefined, "<resource/>", Encoding.UTF8);

        ShopLayout layout = ShopLayoutParser.Load(shop, predefined);

        Assert.Equal("button?a=1&b=2",
            Assert.Single(layout.Elements).Parameters["normal"]);
    }

    [Fact]
    public void UiDocument_RejectsDocumentTypeDefinitions()
    {
        string path = Path.Combine(_directory, "unsafe.xml");
        File.WriteAllText(path, """
            <!DOCTYPE resource [<!ENTITY payload SYSTEM "file:///does-not-exist">]>
            <resource><element type="FORM" name="unsafe" size="640 480">&payload;</element></resource>
            """, Encoding.UTF8);

        Assert.Throws<System.Xml.XmlException>(() => PangyaUiDocument.Load(path));
    }

    [Fact]
    public void UiDocument_AcceptsStandardAndLegacyMultilineComments()
    {
        string path = Path.Combine(_directory, "comments.xml");
        File.WriteAllText(path, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <!--
                A standards-compliant multiline comment.
              -->
              <!--
                ---------------- UI FORM ----------------
              -->
              <element type="FORM" name="commented" size="640 480"/>
            </resource>
            """, Encoding.UTF8);

        PangyaUiDocument document = PangyaUiDocument.Load(path);

        PangyaUiNode form = Assert.Single(document.Nodes);
        Assert.Equal("commented", form.Name);
    }

    [Fact]
    public void UiDocument_AcceptsLegacyAttributesWithoutWhitespaceSeparator()
    {
        string path = Path.Combine(_directory, "adjacent-attributes.xml");
        File.WriteAllText(path, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <item type="STATIC" name="tooltip" caption="Legacy tooltip :"pos="326 261" size="120 20"/>
            </resource>
            """, Encoding.UTF8);

        PangyaUiDocument document = PangyaUiDocument.Load(path);

        PangyaUiNode item = Assert.Single(document.Nodes);
        Assert.Equal("Legacy tooltip :", item.Element.GetAttribute("caption"));
        Assert.Equal(new Rectangle(326, 261, 120, 20), item.Bounds);
    }

    [Fact]
    public async Task UiDocument_RepairsDuplicateLegacyAttributesAndKeepsFirstValue()
    {
        string path = Path.Combine(_directory, "duplicate-attributes.xml");
        await File.WriteAllTextAsync(path, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <!-- Preserve <item alias="comment-one" alias="comment-two">. -->
              <item type="STATIC" name="tooltip" alias="primary" alias="secondary"
                    caption="Legacy tooltip" rect="0 0 120 20"/>
            </resource>
            """, Encoding.UTF8);

        PangyaUiDocument document = PangyaUiDocument.Load(path);

        PangyaUiNode item = Assert.Single(document.Nodes);
        Assert.Equal("primary", item.Element.GetAttribute("alias"));
        Assert.Contains(document.Warnings, warning =>
            warning.Contains("duplicate XML attribute 'alias'", StringComparison.Ordinal));

        await document.SaveAtomicAsync();
        string savedXml = await File.ReadAllTextAsync(path, Encoding.UTF8);
        Assert.Contains("<item alias=\"comment-one\" alias=\"comment-two\">", savedXml,
            StringComparison.Ordinal);
        var strict = new XmlDocument { XmlResolver = null };
        strict.LoadXml(savedXml);
    }

    [Fact]
    public async Task UiDocument_ConditionalElementsAreFilteredAndDirectivesArePreserved()
    {
        string path = Path.Combine(_directory, "conditional.xml");
        await File.WriteAllTextAsync(path, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <element type="FORM" name="form" size="200 120">
                <item type="LABEL" name="normal" rect="0 0 20 20"/>
                <!-- #ifdef _JAPAN_ -->
                <item type="LABEL" name="marker" rect="20 0 40 20"/>
                <!-- #ifdef _DETAIL_ -->
                <item type="LABEL" name="nested" rect="40 0 60 20"/>
                <!-- #endif -->
                <!-- #endif -->
                <!-- #ifdef _JAPAN_
                <item type="LABEL" name="preview" rect="60 0 80 20"/>
                #endif -->
                <!-- #ifdef _JAPAN_
                <item type="LABEL" name="broken" rect="80 0 100 20">
                #endif -->
              </element>
            </resource>
            """, Encoding.UTF8);

        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        var japan = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "_JAPAN_" };
        var all = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "_JAPAN_", "_DETAIL_" };

        Assert.Equal(["_DETAIL_", "_JAPAN_"],
            document.ConditionalSymbols.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        Assert.Equal(["form", "normal"],
            document.GetNodesForForm(form).Select(node => node.Name));
        Assert.Equal(["form", "normal", "marker", "preview"],
            document.GetNodesForForm(form, japan).Select(node => node.Name));
        Assert.Equal(["form", "normal", "marker", "nested", "preview"],
            document.GetNodesForForm(form, all).Select(node => node.Name));

        PangyaUiNode marker = Assert.Single(document.Nodes, node => node.Name == "marker");
        PangyaUiNode preview = Assert.Single(document.Nodes, node => node.Name == "preview");
        Assert.True(marker.IsEditable);
        Assert.True(preview.IsPreviewOnly);
        Assert.Throws<InvalidOperationException>(() => preview.SetName("changed_preview"));
        Assert.Contains(document.Warnings, warning =>
            warning.Contains("malformed #ifdef fragment", StringComparison.OrdinalIgnoreCase));

        marker.SetName("changed_marker");
        await document.SaveAtomicAsync();
        string savedXml = await File.ReadAllTextAsync(path, Encoding.UTF8);
        Assert.Contains("#ifdef _JAPAN_", savedXml, StringComparison.Ordinal);
        Assert.Contains("#endif", savedXml, StringComparison.Ordinal);

        PangyaUiDocument reloaded = PangyaUiDocument.Load(path);
        Assert.Contains(reloaded.GetNodesForForm(
                Assert.Single(reloaded.Nodes, node => node.IsForm), japan),
            node => node.Name == "changed_marker");
        Assert.Contains(reloaded.Nodes, node => node.Name == "preview" && node.IsPreviewOnly);

        RunSta(() =>
        {
            ConstructorInfo constructor = typeof(FrmPangyaUiEditor).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, binder: null,
                [typeof(string), typeof(IReadOnlyList<string>), typeof(PangyaFileImageProvider)],
                modifiers: null)!;
            using var form = (FrmPangyaUiEditor)constructor.Invoke(
                [_directory, new[] { path }, new PangyaFileImageProvider(_directory)]);
            typeof(FrmPangyaUiEditor).GetMethod("ConfigureIfdefControls",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, [document]);
            ToolStrip toolbar = Assert.Single(form.Controls.OfType<ToolStrip>(),
                control => control.Name == "uiEditorToolbar");
            CheckBox[] checkBoxes = toolbar.Items.OfType<ToolStripControlHost>()
                .Select(host => host.Control)
                .OfType<CheckBox>()
                .OrderBy(checkBox => checkBox.Text, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            Assert.Equal(["_DETAIL_", "_JAPAN_"], checkBoxes.Select(checkBox => checkBox.Text));
            Assert.All(checkBoxes, checkBox => Assert.False(checkBox.Checked));
        });
    }

    [Fact]
    public async Task UiDocument_RecoversOnlyCaseInsensitiveClosingTagMatches()
    {
        string path = Path.Combine(_directory, "tag-casing.xml");
        await File.WriteAllTextAsync(path, """
            <?xml version="1.0" encoding="utf-8"?>
            <resource>
              <!-- Preserve <item></ITEM> inside comments. -->
              <![CDATA[Preserve <item></ITEM> inside CDATA.]]>
              <item type="LABEL" name="case" caption="&lt;/ITEM&gt;" rect="0 0 20 20">
                <param name="normal" var="case_image"></PARAM>
              </ITEM>
            </resource>
            """, Encoding.UTF8);

        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode item = Assert.Single(document.Nodes);
        Assert.Equal("</ITEM>", item.Element.GetAttribute("caption"));
        await document.SaveAtomicAsync();

        string savedXml = await File.ReadAllTextAsync(path, Encoding.UTF8);
        Assert.Contains("Preserve <item></ITEM> inside comments.", savedXml, StringComparison.Ordinal);
        Assert.Contains("Preserve <item></ITEM> inside CDATA.", savedXml, StringComparison.Ordinal);
        Assert.Contains("</param>", savedXml, StringComparison.Ordinal);
        Assert.Contains("</item>", savedXml, StringComparison.Ordinal);

        string invalidPath = Path.Combine(_directory, "different-tags.xml");
        await File.WriteAllTextAsync(invalidPath,
            "<resource><item type=\"LABEL\" name=\"bad\"></element></resource>", Encoding.UTF8);
        Assert.Throws<XmlException>(() => PangyaUiDocument.Load(invalidPath));
    }

    [Fact]
    public void UiDocument_FindsXmlFilesOnlyUnderTheUiDirectory()
    {
        string uiDirectory = Path.Combine(_directory, "ui");
        Directory.CreateDirectory(Path.Combine(uiDirectory, "sub"));
        File.WriteAllText(Path.Combine(uiDirectory, "shop.xml"), "<resource/>");
        File.WriteAllText(Path.Combine(uiDirectory, "sub", "login.xml"), "<resource/>");
        File.WriteAllText(Path.Combine(_directory, "outside.xml"), "<resource/>");

        IReadOnlyList<string> files = PangyaUiDocument.FindUiFiles(_directory);

        Assert.Equal(2, files.Count);
        Assert.All(files, file => Assert.StartsWith(uiDirectory, file, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void UiDocument_GetNodesForForm_ExcludesOtherForms()
    {
        string path = Path.Combine(_directory, "forms.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="FORM" name="first" size="640 480">
                <item type="BUTTON" name="first_button" rect="0 0 20 20"/>
              </element>
              <element type="FORM" name="second" size="320 240">
                <item type="BUTTON" name="second_button" rect="0 0 20 20"/>
              </element>
            </resource>
            """);

        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode secondForm = Assert.Single(document.Nodes,
            node => node.IsForm && node.Name == "second");

        IReadOnlyList<PangyaUiNode> visible = document.GetNodesForForm(secondForm);

        Assert.Equal(["second", "second_button"], visible.Select(node => node.Name));
        Assert.All(visible, node => Assert.Same(secondForm, node.FindContainingForm()));
    }

    [Fact]
    public void ImageResources_ExposeAndLoadAllStandardRasterExtensions()
    {
        string filter = FileDialogFactory.BuildImageResourceFilter();
        foreach (string extension in new[] { ".tga", ".png", ".jpg", ".jpeg", ".gif", ".tif", ".tiff", ".ico" })
        {
            Assert.Contains(extension, IffPreviewImageLoader.SupportedExtensions, StringComparer.OrdinalIgnoreCase);
            Assert.Contains("*" + extension, filter, StringComparison.OrdinalIgnoreCase);
        }

        string gifPath = Path.Combine(_directory, "preview.gif");
        using (var bitmap = new Bitmap(8, 8)) bitmap.Save(gifPath, System.Drawing.Imaging.ImageFormat.Gif);
        using Image? loaded = IffPreviewImageLoader.Load(gifPath);
        Assert.NotNull(loaded);
        Assert.Equal(new Size(8, 8), loaded.Size);
    }

    [Fact]
    public void UiCanvas_FillsViewportAndUsesPaddedImageBounds()
    {
        string imageDirectory = Path.Combine(_directory, "ui", "buttons");
        Directory.CreateDirectory(imageDirectory);
        using (var bitmap = new Bitmap(8, 6))
        {
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Black);
            bitmap.Save(Path.Combine(imageDirectory, "intrinsic.png"));
        }

        string xmlPath = Path.Combine(_directory, "ui", "canvas.xml");
        File.WriteAllText(xmlPath, """
            <resource>
              <element type="FORM" name="form" size="100 80">
                <item type="IMAGE" name="explicit" rect="10 12 30 22">
                  <param name="normal" var="intrinsic.png"/>
                </item>
                <item type="IMAGE" name="intrinsic" pos="30 40" size="0 0">
                  <param name="normal" var="intrinsic.png"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(xmlPath);
        using var assets = new PangyaFileImageProvider(_directory);

        RunSta(() =>
        {
            using var canvas = new PangyaUiCanvas(assets);
            PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
            PangyaUiNode explicitNode = Assert.Single(document.Nodes, node => node.Name == "explicit");
            PangyaUiNode intrinsicNode = Assert.Single(document.Nodes, node => node.Name == "intrinsic");
            canvas.LoadDocument(document);
            canvas.SelectedForm = form;
            canvas.ViewportSize = new Size(200, 150);

            Assert.Equal(new Size(200, 150), canvas.Size);
            Assert.Equal(Point.Empty, canvas.LogicalPoint(
                new Point(PangyaUiCanvas.FormPadding, PangyaUiCanvas.FormPadding)));
            Assert.Equal(new Rectangle(10, 12, 8, 6), canvas.GetRenderedBounds(explicitNode));
            Assert.Equal(new Rectangle(30, 40, 8, 6), canvas.GetRenderedBounds(intrinsicNode));

            canvas.Zoom = 2f;
            Assert.Equal(new Size(264, 224), canvas.Size);
            canvas.Zoom = 1f;
            canvas.SelectedNode = intrinsicNode;
            using var rendered = new Bitmap(canvas.Width, canvas.Height);
            canvas.DrawToBitmap(rendered, canvas.ClientRectangle);
            Color outline = rendered.GetPixel(
                PangyaUiCanvas.FormPadding + intrinsicNode.Bounds.X,
                PangyaUiCanvas.FormPadding + intrinsicNode.Bounds.Y);
            Assert.Equal(Color.Orange.ToArgb(), outline.ToArgb());
        });
    }

    [Fact]
    public void UiDimensions_ParseSafelyAndUseRightBottomCoordinates()
    {
        Assert.Equal(new Point(-10, 25), PangyaUiDimensionHelper.ParsePoint("-10 25"));
        Assert.Equal(new Size(640, 480), PangyaUiDimensionHelper.ParseSize("640, 480"));
        Assert.Equal(new Rectangle(10, 20, 40, 60),
            PangyaUiDimensionHelper.ParseRectangle("10 20 50 80"));
        Assert.Null(PangyaUiDimensionHelper.ParsePoint("10"));
        Assert.Null(PangyaUiDimensionHelper.ParseSize("10 invalid"));
        Assert.Null(PangyaUiDimensionHelper.ParseRectangle("10 20 5 15"));
        Assert.Null(PangyaUiDimensionHelper.ParseRectangle(
            "999999999999999999999 0 10 10"));
    }

    [Fact]
    public void UiCanvas_UsesIntrinsicAreaSizeUnlessStretchIsEnabled()
    {
        string imageDirectory = Path.Combine(_directory, "ui", "images");
        Directory.CreateDirectory(imageDirectory);
        SaveSolidImage(Path.Combine(imageDirectory, "red.png"), Color.Red, 4, 3);

        string xmlPath = Path.Combine(_directory, "ui", "stretch.xml");
        File.WriteAllText(xmlPath, """
            <resource>
              <element type="FORM" name="form" size="80 40">
                <item type="GROUPBOX" name="container" rect="1 1 20 18"/>
                <item type="AREA" name="natural" rect="5 5 15 15">
                  <param name="bgimg" var="red.png"/>
                </item>
                <item type="AREA" name="stretched" rect="25 5 35 15">
                  <param name="bgimg" var="red.png"/>
                  <param name="stretch" var="1"/>
                </item>
                <item type="AREA" name="hidden" rect="45 5 55 15">
                  <param name="bgimg" var="red.png"/>
                  <param name="visible" var="0"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(xmlPath);
        using var assets = new PangyaFileImageProvider(_directory);

        RunSta(() =>
        {
            using var canvas = new PangyaUiCanvas(assets);
            PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
            PangyaUiNode group = Assert.Single(document.Nodes, node => node.Name == "container");
            PangyaUiNode natural = Assert.Single(document.Nodes, node => node.Name == "natural");
            PangyaUiNode stretched = Assert.Single(document.Nodes, node => node.Name == "stretched");
            PangyaUiNode hidden = Assert.Single(document.Nodes, node => node.Name == "hidden");
            canvas.LoadDocument(document);
            canvas.SelectedForm = form;

            Assert.Equal(new Rectangle(5, 5, 4, 3), canvas.GetRenderedBounds(natural));
            Assert.Equal(new Rectangle(25, 5, 10, 10), canvas.GetRenderedBounds(stretched));
            Assert.Equal(Rectangle.Empty, canvas.GetRenderedBounds(group));
            Assert.Equal(Rectangle.Empty, canvas.GetRenderedBounds(hidden));

            using var rendered = new Bitmap(canvas.Width, canvas.Height);
            canvas.DrawToBitmap(rendered, canvas.ClientRectangle);
            int padding = PangyaUiCanvas.FormPadding;
            Assert.Equal(Color.Red.ToArgb(), rendered.GetPixel(padding + 7, padding + 6).ToArgb());
            Assert.NotEqual(Color.Red.ToArgb(), rendered.GetPixel(padding + 12, padding + 6).ToArgb());
            Assert.Equal(Color.Red.ToArgb(), rendered.GetPixel(padding + 33, padding + 13).ToArgb());
            Assert.NotEqual(Color.Red.ToArgb(), rendered.GetPixel(padding + 47, padding + 6).ToArgb());

            canvas.CanvasMouseDown(canvas, new MouseEventArgs(MouseButtons.Left, 1,
                padding + 12, padding + 6, 0));
            Assert.Null(canvas.SelectedNode);
            canvas.CanvasMouseDown(canvas, new MouseEventArgs(MouseButtons.Left, 1,
                padding + 7, padding + 6, 0));
            Assert.Same(natural, canvas.SelectedNode);

            canvas.ShowDebugBounds = true;
            Assert.Equal(group.Bounds, canvas.GetRenderedBounds(group));
            using var debugRendered = new Bitmap(canvas.Width, canvas.Height);
            canvas.DrawToBitmap(debugRendered, canvas.ClientRectangle);
            Assert.NotEqual(rendered.GetPixel(padding + 1, padding + 1).ToArgb(),
                debugRendered.GetPixel(padding + 1, padding + 1).ToArgb());
        });
    }

    [Fact]
    public void UiRenderer_ViewerClipsTintsAndScrollsInTwentyPixelRows()
    {
        string ui = Path.Combine(_directory, "ui");
        Directory.CreateDirectory(ui);
        using (var image = new Bitmap(40, 60))
        {
            using Graphics graphics = Graphics.FromImage(image);
            graphics.Clear(Color.White);
            graphics.FillRectangle(Brushes.Lime, 0, 20, 40, 40);
            image.Save(Path.Combine(ui, "bands.png"), System.Drawing.Imaging.ImageFormat.Png);
        }
        string path = Path.Combine(ui, "viewer.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" size="60 50">
              <item type="VIEWER" rect="5 5 25 25">
                <param name="image" var="bands.png"/>
                <param name="bgcolor" var="FFFF80FF"/>
                <param name="use_wheel" var="true"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode viewer = Assert.Single(document.Nodes, node => node.Type == "VIEWER");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(60, 50);
        using Graphics canvas = Graphics.FromImage(bitmap);

        Assert.Equal(2, renderer.GetViewerMaximumTopRow(viewer));
        renderer.Render(canvas, document, form);
        Assert.Equal(Color.FromArgb(255, 128, 255), Color.FromArgb(bitmap.GetPixel(10, 10).ToArgb()));
        Assert.Equal(Color.FromArgb(46, 49, 55), Color.FromArgb(bitmap.GetPixel(25, 10).ToArgb()));

        renderer.Render(canvas, document, form, new PangyaUiRenderOptions
        {
            ViewerTopRows = new Dictionary<PangyaUiNode, int> { [viewer] = 1 }
        });
        Assert.Equal(Color.FromArgb(0, 128, 0), Color.FromArgb(bitmap.GetPixel(10, 10).ToArgb()));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_PreviewsLayoutBackgroundAndStaticCaption(bool useImage)
    {
        string imageDirectory = Path.Combine(_directory, "ui", "images");
        Directory.CreateDirectory(imageDirectory);
        SaveSolidImage(Path.Combine(imageDirectory, "green.png"), Color.Lime, 4, 4);
        string path = Path.Combine(_directory, "ui", "layout.xml");
        File.WriteAllText(path, $"""
            <resource>
              <element type="LAYOUT" name="layout">
                <base width="80" height="40" color="FF0000" background="{(useImage ? "green.png" : "")}"/>
                <item type="STATIC" name="label" rect="4 4 64 28" caption="Hello">
                  <param name="color" var="FFFFFF"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode layout = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode label = Assert.Single(document.Nodes, node => node.Name == "label");
        Assert.Equal(new Size(80, 40), document.CanvasSize);
        Assert.Same(layout, label.FindContainingForm());

        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(80, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, layout);

        int background = (useImage ? Color.Lime : Color.Red).ToArgb();
        Assert.Equal(background, bitmap.GetPixel(70, 32).ToArgb());
        Assert.Equal(background, bitmap.GetPixel(1, 1).ToArgb());
        Assert.Equal(background, bitmap.GetPixel(79, 39).ToArgb());
        Assert.Contains(Enumerable.Range(4, 24).SelectMany(y =>
            Enumerable.Range(4, 60).Select(x => bitmap.GetPixel(x, y).ToArgb())),
            pixel => pixel != background);
        Assert.Same(label, renderer.HitTest(document, layout, new Point(8, 8)));
    }

    [Fact]
    public void UiRenderer_UsesTheSameGeometryForPaintingAndHitTesting()
    {
        string imageDirectory = Path.Combine(_directory, "ui", "images");
        Directory.CreateDirectory(imageDirectory);
        SaveSolidImage(Path.Combine(imageDirectory, "green.png"), Color.Lime, 6, 4);
        string path = Path.Combine(_directory, "ui", "renderer.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="FORM" name="form" size="40 30">
                <item type="AREA" name="image" rect="7 8 27 28">
                  <param name="bgimg" var="green.png"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode image = Assert.Single(document.Nodes, node => node.Name == "image");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions();

        Assert.Equal(new Rectangle(7, 8, 6, 4),
            renderer.GetRenderedBounds(image, options));
        Assert.Same(image, renderer.HitTest(document, form, new Point(9, 10), options));
        Assert.Null(renderer.HitTest(document, form, new Point(18, 10), options));

        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(9, 10).ToArgb());
        Assert.NotEqual(Color.Lime.ToArgb(), bitmap.GetPixel(18, 10).ToArgb());
    }

    [Fact]
    public void UiRenderer_PositionsNestedElementsRelativeToTheirParents()
    {
        string imageDirectory = Path.Combine(_directory, "ui", "images");
        Directory.CreateDirectory(imageDirectory);
        SaveSolidImage(Path.Combine(imageDirectory, "red.png"), Color.Red, 4, 3);
        string path = Path.Combine(_directory, "ui", "nested.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="FORM" name="form" size="80 60">
                <item type="GROUPBOX" name="outer" rect="20 15 60 50">
                  <item type="GROUPBOX" name="inner" rect="3 4 30 25">
                    <item type="AREA" name="image" pos="5 7" size="0 0">
                      <param name="bgimg" var="red.png"/>
                    </item>
                  </item>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode image = Assert.Single(document.Nodes, node => node.Name == "image");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions();

        Assert.Equal(new Rectangle(28, 26, 4, 3),
            renderer.GetRenderedBounds(image, options));
        Assert.Same(image, renderer.HitTest(document, form, new Point(29, 27), options));
        Assert.Null(renderer.HitTest(document, form, new Point(6, 8), options));

        using var bitmap = new Bitmap(80, 60);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(29, 27).ToArgb());
        Assert.NotEqual(Color.Red.ToArgb(), bitmap.GetPixel(6, 8).ToArgb());
    }

    [Fact]
    public void UiCanvas_SelectedElementRendersAndHitTestsInFront()
    {
        string imageDirectory = Path.Combine(_directory, "ui", "buttons");
        Directory.CreateDirectory(imageDirectory);
        SaveSolidImage(Path.Combine(imageDirectory, "red.png"), Color.Red, 20, 20);
        SaveSolidImage(Path.Combine(imageDirectory, "blue.png"), Color.Blue, 20, 20);
        string xmlPath = Path.Combine(_directory, "ui", "front.xml");
        File.WriteAllText(xmlPath, """
            <resource>
              <element type="FORM" name="form" size="80 60">
                <item type="AREA" name="red" rect="10 10 30 30">
                  <param name="bgimg" var="red.png"/>
                </item>
                <item type="AREA" name="blue" rect="10 10 30 30">
                  <param name="bgimg" var="blue.png"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(xmlPath);
        using var assets = new PangyaFileImageProvider(_directory);

        RunSta(() =>
        {
            using var canvas = new PangyaUiCanvas(assets);
            PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
            PangyaUiNode red = Assert.Single(document.Nodes, node => node.Name == "red");
            canvas.LoadDocument(document);
            canvas.SelectedForm = form;
            canvas.SelectedNode = red;
            canvas.ViewportSize = new Size(120, 100);

            Assert.Same(red, canvas.RenderOrderedNodes()[^1]);
            using var rendered = new Bitmap(canvas.Width, canvas.Height);
            canvas.DrawToBitmap(rendered, canvas.ClientRectangle);
            Assert.Equal(Color.Red.ToArgb(), rendered.GetPixel(
                PangyaUiCanvas.FormPadding + 20,
                PangyaUiCanvas.FormPadding + 20).ToArgb());

            canvas.CanvasMouseDown(canvas, new MouseEventArgs(MouseButtons.Left, 1,
                PangyaUiCanvas.FormPadding + 20, PangyaUiCanvas.FormPadding + 20, 0));
            Assert.Same(red, canvas.SelectedNode);
        });
    }

    [Fact]
    public void UiResourceCatalog_ConditionalIncludeTakesPrecedenceOnlyWhenEnabled()
    {
        Directory.CreateDirectory(_directory);
        string entry = Path.Combine(_directory, "all.xml");
        string variant = Path.Combine(_directory, "variant.xml");
        File.WriteAllText(entry, """
            <resource>
              <!-- #ifdef JP -->
              <element type="FRAME" name="panel" />
              <element type="INCLUDE" name="variant.xml" />
              <!-- #endif -->
              <element type="BITMAP" name="icon" filename="default.png" />
            </resource>
            """);
        File.WriteAllText(variant, """
            <resource>
              <element type="INCLUDE" name="all.xml" />
              <element type="BITMAP" name="icon" filename="variant.png" />
            </resource>
            """);
        PangyaUiResourceCatalog catalog = PangyaUiResourceCatalog.Load([entry, variant]);
        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal("default.png", catalog.TryResolve("icon", "BITMAP", symbols)?
            .Element.GetAttribute("filename"));
        symbols.Add("JP");
        Assert.Equal("variant.png", catalog.TryResolve("icon", "BITMAP", symbols)?
            .Element.GetAttribute("filename"));
    }

    [Fact]
    public void UiCanvas_ResolvesCrossFileMacroAndFrameResources()
    {
        string uiDirectory = Path.Combine(_directory, "ui");
        string imageDirectory = Path.Combine(uiDirectory, "images");
        Directory.CreateDirectory(imageDirectory);
        SaveSolidImage(Path.Combine(imageDirectory, "macro_blue.png"), Color.Blue, 8, 8);
        for (int index = 0; index < 9; index++)
            SaveSolidImage(Path.Combine(imageDirectory, $"frame{index:00}.png"), Color.Red, 2, 2);

        string definitionsPath = Path.Combine(uiDirectory, "definitions.xml");
        File.WriteAllText(definitionsPath, """
            <resource>
              <element type="FRAME" name="SHARED_FRAME">
                <bfrm filename="frame"/>
              </element>
              <element type="MACROITEM" name="SHARED_MACRO">
                <item type="AREA" name="macro_part" rect="10 12 18 20">
                  <param name="bgimg" var="macro_blue.png"/>
                </item>
              </element>
            </resource>
            """);
        string viewPath = Path.Combine(uiDirectory, "view.xml");
        File.WriteAllText(viewPath, """
            <resource>
              <element type="FORM" name="form" size="40 30" resource="SHARED_FRAME">
                <item type="MACROITEM" name="macro_instance" pos="22 24" resource="SHARED_MACRO"/>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(viewPath);
        PangyaUiResourceCatalog catalog = PangyaUiResourceCatalog.Load(
            [definitionsPath, viewPath]);
        using var assets = new PangyaFileImageProvider(_directory);

        RunSta(() =>
        {
            using var canvas = new PangyaUiCanvas(assets, catalog);
            PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
            PangyaUiNode macro = Assert.Single(document.Nodes, node => node.Name == "macro_instance");
            canvas.LoadDocument(document);
            canvas.SelectedForm = form;
            canvas.SelectedNode = form;
            canvas.ViewportSize = new Size(100, 80);

            Assert.NotNull(catalog.TryResolve("SHARED_FRAME", "FORM",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
            Assert.NotNull(catalog.TryResolve("SHARED_MACRO", "MACROITEM",
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
            Assert.Equal(new Rectangle(10, 12, 8, 8), canvas.GetRenderedBounds(macro));

            using var rendered = new Bitmap(canvas.Width, canvas.Height);
            canvas.DrawToBitmap(rendered, canvas.ClientRectangle);
            Assert.Equal(Color.Red.ToArgb(), rendered.GetPixel(
                PangyaUiCanvas.FormPadding + 2,
                PangyaUiCanvas.FormPadding + 2).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), rendered.GetPixel(
                PangyaUiCanvas.FormPadding + 12,
                PangyaUiCanvas.FormPadding + 14).ToArgb());
        });
    }

    [Fact]
    public void UiCanvas_DraggingDifferentElementUpdatesItsPropertyCoordinates()
    {
        string uiDirectory = Path.Combine(_directory, "ui");
        Directory.CreateDirectory(uiDirectory);
        string xmlPath = Path.Combine(uiDirectory, "drag.xml");
        File.WriteAllText(xmlPath, """
            <resource>
              <element type="FORM" name="form" size="100 80">
                <item type="LABEL" name="first" rect="5 5 15 15"/>
                <item type="LABEL" name="second" rect="40 30 50 40"/>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(xmlPath);
        using var assets = new PangyaFileImageProvider(_directory);

        RunSta(() =>
        {
            using var canvas = new PangyaUiCanvas(assets);
            using var properties = new PangyaUiPropertyPanel(assets);
            PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
            PangyaUiNode first = Assert.Single(document.Nodes, node => node.Name == "first");
            PangyaUiNode second = Assert.Single(document.Nodes, node => node.Name == "second");
            canvas.LoadDocument(document);
            canvas.SelectedForm = form;
            canvas.SelectedNode = first;
            canvas.ShowDebugBounds = true;
            properties.SelectedNode = first;
            canvas.SelectionChanged += (_, node) =>
            {
                canvas.SelectedNode = node;
                properties.SelectedNode = node;
            };
            canvas.ElementChanged += (_, e) => properties.SelectedNode = e.Node;

            var start = new Point(PangyaUiCanvas.FormPadding + 41, PangyaUiCanvas.FormPadding + 31);
            canvas.CanvasMouseDown(canvas, new MouseEventArgs(MouseButtons.Left, 1, start.X, start.Y, 0));
            canvas.CanvasMouseMove(canvas,
                new MouseEventArgs(MouseButtons.Left, 0, start.X + 5, start.Y + 7, 0));

            Assert.Same(second, canvas.SelectedNode);
            Assert.Same(second, properties.SelectedNode);
            Assert.Equal(new Point(45, 37), second.Bounds.Location);
            Assert.Equal(new Point(45, 37), properties.DisplayedLocation);

            canvas.CanvasMouseDown(canvas, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));

            Assert.Same(form, canvas.SelectedForm);
            Assert.Same(second, canvas.SelectedNode);
            Assert.Same(second, properties.SelectedNode);
        });
    }

    [Fact]
    public void UiResourcePicker_UsesCurrentDirectoryAndStoresOnlyFilename()
    {
        string imageDirectory = Path.Combine(_directory, "ui", "buttons");
        Directory.CreateDirectory(imageDirectory);
        string currentPath = Path.Combine(imageDirectory, "current.tga");
        string replacementPath = Path.Combine(imageDirectory, "replacement.png");
        File.WriteAllBytes(currentPath, [1]);
        File.WriteAllBytes(replacementPath, [2]);
        string xmlPath = Path.Combine(_directory, "ui", "resource.xml");
        File.WriteAllText(xmlPath, """
            <resource>
              <element type="FORM" name="form" size="100 80">
                <item type="IMAGE" name="image" rect="0 0 10 10">
                  <param name="normal" var="current.tga"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiNode image = Assert.Single(PangyaUiDocument.Load(xmlPath).Nodes,
            node => node.Name == "image");
        using var assets = new PangyaFileImageProvider(_directory);
        string outsidePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".png");
        File.WriteAllBytes(outsidePath, [3]);

        try
        {
            RunSta(() =>
            {
                using var properties = new PangyaUiPropertyPanel(assets) { SelectedNode = image };

                Assert.Equal(imageDirectory, properties.GetResourceInitialDirectory());
                Assert.True(properties.TrySetResourceFromPath(replacementPath));
                Assert.Equal("replacement.png", image.GetResource(PangyaUiButtonState.Normal));
                Assert.False(properties.TrySetResourceFromPath(outsidePath));
                Assert.Equal("replacement.png", image.GetResource(PangyaUiButtonState.Normal));
            });
        }
        finally
        {
            try { File.Delete(outsidePath); } catch { }
        }
    }

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
    public void ShopRenderer_ReturnsVisibleGeometryAndPaintsSkinAndCatalog()
    {
        string imageDirectory = Path.Combine(_directory, "ui", "shop_myroom");
        Directory.CreateDirectory(imageDirectory);
        string skinPath = Path.Combine(imageDirectory, "skin.png");
        string iconPath = Path.Combine(imageDirectory, "icon.png");
        string tabPath = Path.Combine(imageDirectory, "category_tab.png");
        SaveSolidImage(skinPath, Color.Red, 6, 4);
        SaveSolidImage(iconPath, Color.Blue, 8, 8);
        SaveSolidImage(tabPath, Color.Lime, 12, 9);
        var layout = new ShopLayout(new Size(800, 600),
        [
            new ShopLayoutElement("BUTTON", "button", new Rectangle(10, 12, 0, 0),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["normal"] = "skin.png"
                }),
            new ShopLayoutElement("TABBUTTON", "category_tab",
                new Rectangle(30, 40, 0, 0),
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["normal"] = "category_tab.png"
                })
        ]);
        var item = new ShopCatalogItem("Item", 1, "Test item", "icon", 100, 0,
            0, false, iconPath);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new ShopRenderer(images);
        var session = new ShopSession();
        var state = new ShopRenderState(0, 0, false, string.Empty, null, session);
        var text = new ShopRenderText("No items", "{0} {1} {2}", "{0} {1}",
            "{0}", "Edit");
        using var bitmap = new Bitmap(800, 600);
        using Graphics graphics = Graphics.FromImage(bitmap);

        ShopRenderResult result = renderer.Render(graphics, layout, [item], state,
            text, System.Globalization.CultureInfo.InvariantCulture);

        ShopVisibleItem visible = Assert.Single(result.VisibleItems);
        Assert.Same(item, visible.Item);
        Assert.Equal(new Rectangle(392, 150, 92, 56), visible.Bounds);
        Assert.Same(item, ShopRenderer.HitTestItem(result, new Point(400, 160))!.Item);
        Assert.Equal("button",
            renderer.HitTestElement(layout, new Point(12, 13))?.Name);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(12, 13).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(31, 41).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(396, 154).ToArgb());
    }

    [Fact]
    public void ShopRenderer_EditorModeSuppressesCommerceOverlaysWithoutChangingItems()
    {
        string imageDirectory = Path.Combine(_directory, "ui", "shop_myroom");
        Directory.CreateDirectory(imageDirectory);
        string iconPath = Path.Combine(imageDirectory, "icon.png");
        SaveSolidImage(iconPath, Color.Blue, 8, 8);
        var layout = new ShopLayout(new Size(800, 600), []);
        var item = new ShopCatalogItem("Item", 1, "Test item", "icon", 100,
            0, 0, false, iconPath);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new ShopRenderer(images);
        var session = new ShopSession();
        session.Add(item);
        var text = new ShopRenderText("No items", "Cart {0} {1} {2}",
            "Balance {0} {1}", "Filter {0}", "Edit");
        using var simulationBitmap = new Bitmap(800, 600);
        using var editorBitmap = new Bitmap(800, 600);
        using Graphics simulationGraphics = Graphics.FromImage(simulationBitmap);
        using Graphics editorGraphics = Graphics.FromImage(editorBitmap);

        ShopRenderResult simulation = renderer.Render(simulationGraphics, layout,
            [item], new ShopRenderState(0, 0, false, string.Empty, null,
                session), text);
        ShopRenderResult editor = renderer.Render(editorGraphics, layout, [item],
            new ShopRenderState(0, 0, false, string.Empty, null, session,
                ShopRenderMode.Editor), text);

        Assert.Equal(simulation.VisibleItems.Single().Bounds,
            editor.VisibleItems.Single().Bounds);
        Assert.True(HasVisiblePixel(simulationBitmap,
            new Rectangle(15, 408, 335, 97)));
        Assert.False(HasVisiblePixel(editorBitmap,
            new Rectangle(15, 408, 335, 97)));
    }

    [Fact]
    public void ShopRenderer_PaintsPlaceholderForItemWithoutResolvedIcon()
    {
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new ShopRenderer(images);
        var item = new ShopCatalogItem("Item", 1, "Missing icon", string.Empty,
            0, 0, 0, false, string.Empty);
        using var bitmap = new Bitmap(800, 600);
        using Graphics graphics = Graphics.FromImage(bitmap);

        ShopRenderResult result = renderer.Render(graphics,
            new ShopLayout(new Size(800, 600), []), [item],
            new ShopRenderState(0, 0, false, string.Empty, null,
                new ShopSession(), ShopRenderMode.Editor),
            new ShopRenderText("None", string.Empty, string.Empty,
                string.Empty, "Edit"));

        Assert.Single(result.VisibleItems);
        Assert.True(HasVisiblePixel(bitmap, new Rectangle(395, 153, 38, 38)));
    }

    [Fact]
    public void ShopRenderer_SkipsAndLogsMissingSkinResourcesOnce()
    {
        string missingResource = $"missing_skin_{Guid.NewGuid():N}.tga";
        var parameters = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["normal"] = missingResource
        };
        var layout = new ShopLayout(new Size(800, 600),
        [
            new ShopLayoutElement("BUTTON", "first",
                new Rectangle(10, 10, 20, 20), parameters),
            new ShopLayoutElement("TABBUTTON", "second",
                new Rectangle(40, 10, 20, 20), parameters)
        ]);
        using var images = new PangyaFileImageProvider(_directory);

        FrmShopMockup.LogMissingSkinAssets(layout, images);

        Assert.Single(AppLogger.Instance.GetEntries(), entry =>
            entry.Source == "Shop" &&
            entry.Level == AppLogLevel.Warning &&
            entry.Message.Contains(missingResource, StringComparison.Ordinal));
        using var bitmap = new Bitmap(800, 600);
        using Graphics graphics = Graphics.FromImage(bitmap);
        var renderer = new ShopRenderer(images);
        ShopRenderResult result = renderer.Render(graphics, layout, [],
            new ShopRenderState(0, 0, false, string.Empty, null,
                new ShopSession(), ShopRenderMode.Editor),
            new ShopRenderText("None", string.Empty, string.Empty,
                string.Empty, "Edit"));
        Assert.Empty(result.VisibleItems);
    }

    [Fact]
    public void ShopItemDialog_ExposesMetadataAndExistingEditableValues()
    {
        string iconPath = Path.Combine(_directory, "icon.png");
        SaveSolidImage(iconPath, Color.Magenta, 8, 8);
        var item = new ShopCatalogItem("Item", 0x1234, "Test item", "icon",
            100, 80, 25, false, iconPath, "Item.iff", 7, 0xA5, 0xD2,
            3, 9, new DateTime(2026, 1, 2), new DateTime(2027, 3, 4));

        RunSta(() =>
        {
            using var dialog = new ShopItemDialog(item, _directory);

            Assert.Equal(Strings.Shop_EditItem, dialog.Text);
            Assert.Equal("icon", dialog.IconId);
            Assert.Equal(iconPath, dialog.IconPath);
            Assert.Equal(100u, dialog.Price);
            Assert.Equal(80u, dialog.DiscountPrice);
            Assert.Equal(25u, dialog.RentalPrice);
            Assert.Equal((byte)0xA5, dialog.ShopFlags);
            Assert.Equal((byte)0xD2, dialog.MoneyFlags);
            Assert.Equal((byte)3, dialog.TimeFlag);
            Assert.Equal((byte)9, dialog.Time);
            Assert.Contains(dialog.Controls.OfType<TextBox>(),
                control => control.ReadOnly &&
                           control.Text.Contains("0x00001234",
                               StringComparison.Ordinal));
            Assert.Contains(dialog.Controls.OfType<TextBox>(),
                control => control.ReadOnly && control.Text == "Item.iff #7");
            Assert.NotNull(dialog.Controls.OfType<PictureBox>().Single().Image);
        });
    }

    [Fact]
    public void ShopCanvas_ItemClickRequestsEditingWithoutAddingToCart()
    {
        string imageDirectory = Path.Combine(_directory, "ui", "shop_myroom");
        Directory.CreateDirectory(imageDirectory);
        string iconPath = Path.Combine(imageDirectory, "icon.png");
        SaveSolidImage(iconPath, Color.Blue, 8, 8);
        var item = new ShopCatalogItem("Item", 1, "Test item", "icon", 100,
            0, 0, false, iconPath);

        RunSta(() =>
        {
            var images = new PangyaFileImageProvider(_directory);
            using var canvas = new ShopCanvas(
                new ShopLayout(new Size(800, 600), []), images, [item],
                Path.Combine(_directory, "pangya_th.iff"))
            {
                Size = new Size(1120, 840),
            };
            ShopCatalogItem? requestedItem = null;
            canvas.SetEditItemHandler(requested =>
            {
                requestedItem = requested;
                return Task.CompletedTask;
            });
            using var bitmap = new Bitmap(canvas.Width, canvas.Height);
            canvas.DrawToBitmap(bitmap, canvas.ClientRectangle);

            canvas.ShopCanvas_MouseClick(canvas, new MouseEventArgs(
                MouseButtons.Left, 1, 400 * 14 / 10, 160 * 14 / 10, 0));

            Assert.Same(item, requestedItem);
            var session = (ShopSession)typeof(ShopCanvas).GetField(
                "_emptySession", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(canvas)!;
            Assert.Empty(session.Cart);
        });
    }

    [Fact]
    public void MainMenu_SeparatesUiAndShopEditorsWithoutOverlappingControls()
    {
        RunSta(() =>
        {
            using var menu = new FrmMenu();
            Control uiEditor =
                menu.Controls.Find("btnOpenUiEditor", true).Single();
            Control shopEditor =
                menu.Controls.Find("btnOpenShopEditor", true).Single();
            Control fontViewer =
                menu.Controls.Find("btnOpenFontViewer", true).Single();
            Control options = menu.Controls.Find("btnOpenOptions", true).Single();
            Control log = menu.Controls.Find("btnOpenLog", true).Single();

            Assert.Equal(Strings.Menu_UiEditor, uiEditor.Text);
            Assert.Equal(Strings.Menu_ShopEditor, shopEditor.Text);
            Control[] ordered =
                [uiEditor, shopEditor, fontViewer, options, log];
            for (int index = 1; index < ordered.Length; index++)
                Assert.True(ordered[index - 1].Bottom < ordered[index].Top);
            Assert.True(log.Bottom < menu.ClientSize.Height);
        });
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

        using Bitmap bitmap = PangyaImageLoader.LoadTga(path);

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
        Assert.Throws<InvalidDataException>(() => PangyaImageLoader.LoadTga(path));
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
        using var resolver = new PangyaFileImageProvider(_directory);
        Assert.Equal(preferred, resolver.ResolvePath("button"));
        Assert.Equal(preferred, resolver.ResolvePath("button.tga"));
        Assert.Throws<FileNotFoundException>(() => resolver.ResolvePath("missing"));
        Assert.Null(resolver.TryResolvePath(string.Empty));
        Assert.Null(resolver.TryResolvePath("   "));
        Assert.Null(resolver.TryResolvePath("."));
    }

    [Fact]
    public void AssetResolver_PrefersTheExplicitExtensionOverDirectoryPriority()
    {
        string tga = Path.Combine(_directory, "ui", "roomlist", "gp_roomlist_background.tga");
        string jpg = Path.Combine(_directory, "ui", "frames", "gp_roomlist_background.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(tga)!);
        Directory.CreateDirectory(Path.GetDirectoryName(jpg)!);
        File.WriteAllBytes(tga, [1]);
        File.WriteAllBytes(jpg, [2]);
        using var resolver = new PangyaFileImageProvider(_directory);

        Assert.Equal(tga, resolver.ResolvePath("gp_roomlist_background.TGA"));
        Assert.Equal(jpg, resolver.ResolvePath("gp_roomlist_background.jpg"));
        Assert.Equal(jpg, resolver.ResolvePath("gp_roomlist_background"));
    }

    [Fact]
    public void AssetResolver_PrefersUiImageAndAllowsMissingOptionalImages()
    {
        string tga = Path.Combine(_directory, "ui", "lobby", "chat_emoti.tga");
        string jpg = Path.Combine(_directory, "2d", "640x480", "gui", "chat_emoti.jpg");
        Directory.CreateDirectory(Path.GetDirectoryName(tga)!);
        Directory.CreateDirectory(Path.GetDirectoryName(jpg)!);
        File.WriteAllBytes(tga, [1]);
        File.WriteAllBytes(jpg, [2]);
        using var resolver = new PangyaFileImageProvider(_directory);

        Assert.Equal(tga, resolver.ResolvePath("chat_emoti"));
        Assert.Equal(jpg, resolver.ResolvePath("chat_emoti.jpg"));
        Assert.Null(resolver.GetImage("sfrm"));
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
        Assert.Contains(AppLogger.Instance.GetEntries(), entry =>
            entry.Source == "Shop" &&
            entry.Message.Contains(path, StringComparison.OrdinalIgnoreCase) &&
            entry.Message.Contains("0x0000007B", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CatalogLoader_IncludesDisabledZeroPriceAndMissingIconRecords()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        string iconDirectory = Path.Combine(_directory, "ui", "shop_myroom");
        Directory.CreateDirectory(iconDirectory);
        string iconPath = Path.Combine(iconDirectory, "visible_icon.png");
        SaveSolidImage(iconPath, Color.Green, 8, 8);
        string path = Path.Combine(_directory, "Item.iff");
        var header = new IffHeader(2, 0, 12, [0, 0, 0]);
        IffSchema schema = IffSchemaRegistry.Resolve("Item.iff", header, 244)!;
        IffRecord hidden = IffRecord.CreateBlank(0, 244, schema);
        hidden.SetValue("Enabled", false, encoding);
        hidden.SetValue("ItemId", 100u, encoding);
        hidden.SetValue("Name", string.Empty, encoding);
        hidden.SetValue("Icon", ".", encoding);
        hidden.SetValue("Price", 0u, encoding);
        hidden.SetValue("DiscountPrice", 0u, encoding);
        hidden.SetValue("IsSaleable", false, encoding);
        hidden.SetValue("OnlyDisplay", true, encoding);
        IffRecord displayOnly = IffRecord.CreateBlank(1, 244, schema);
        displayOnly.SetValue("Enabled", false, encoding);
        displayOnly.SetValue("ItemId", 200u, encoding);
        displayOnly.SetValue("Name", "Display only", encoding);
        displayOnly.SetValue("Icon", "visible_icon", encoding);
        displayOnly.SetValue("Price", 0u, encoding);
        displayOnly.SetValue("DiscountPrice", 0u, encoding);
        displayOnly.SetValue("IsSaleable", false, encoding);
        displayOnly.SetValue("OnlyDisplay", true, encoding);
        await using (var output = File.Create(path))
            await IffWriter.WriteAsync(output, header,
                Many(hidden, displayOnly));
        using var assets = new PangyaFileImageProvider(_directory);

        ShopCatalogLoadResult result = await ShopCatalogLoader.LoadAsync(path,
            assets, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        ShopCatalogItem first =
            Assert.Single(result.Items, item => item.ItemId == 100u);
        Assert.Equal("0x00000064", first.Name);
        Assert.Empty(first.IconPath);
        Assert.Equal(0u, first.Price);
        Assert.Equal((byte)0x80, first.ShopFlags);
        ShopCatalogItem second =
            Assert.Single(result.Items, item => item.ItemId == 200u);
        Assert.Equal(iconPath, second.IconPath);
        Assert.Equal(1, result.MissingIconCount);
    }

    [Fact]
    public async Task CatalogLoader_LoadsGlobalCommonDerivedItems()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding encoding = Encoding.GetEncoding(949);
        string path = Path.Combine(_directory, "Item.iff");
        var header = new IffHeader(1, 30447, 11, [0, 0, 0]);
        IffSchema schema = IffSchemaRegistry.Resolve("Item.iff", header, 224)!;
        IffRecord record = IffRecord.CreateBlank(0, 224, schema);
        record.SetValue("ItemId", 321u, encoding);
        record.SetValue("Name", "Global item", encoding);
        record.SetValue("Price", 500u, encoding);
        record.SetValue("DiscountPrice", 450u, encoding);
        record.SetValue("UsedPrice", 25u, encoding);
        await using (var output = File.Create(path))
            await IffWriter.WriteAsync(output, header, One(record));
        using var assets = new PangyaFileImageProvider(_directory);

        ShopCatalogLoadResult result = await ShopCatalogLoader.LoadAsync(path,
            assets, "Global", CancellationToken.None);

        ShopCatalogItem item = Assert.Single(result.Items);
        Assert.Equal(321u, item.ItemId);
        Assert.Equal("Global item", item.Name);
        Assert.Equal(25u, item.RentalPrice);
    }

    [Fact]
    public async Task CatalogLoader_SkipsSchemasThatDoNotUseCommonBase()
    {
        string path = Path.Combine(_directory, "Item.iff");
        var header = new IffHeader(1, 0, 11, [0, 0, 0]);
        IffSchema schema = IffSchemaRegistry.Resolve("Item.iff", header, 196)!;
        Assert.Null(schema.BaseReference);
        IffRecord record = IffRecord.CreateBlank(0, 196, schema);
        await using (var output = File.Create(path))
            await IffWriter.WriteAsync(output, header, One(record));
        using var assets = new PangyaFileImageProvider(_directory);

        ShopCatalogLoadResult result = await ShopCatalogLoader.LoadAsync(path,
            assets, "TH", CancellationToken.None);

        Assert.Empty(result.Items);
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

    private static void SaveSolidImage(string path, Color color, int width, int height)
    {
        using var bitmap = new Bitmap(width, height);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(color);
        bitmap.Save(path);
    }

    private static bool HasVisiblePixel(Bitmap bitmap, Rectangle bounds)
    {
        for (int y = bounds.Top; y < bounds.Bottom; y++)
        for (int x = bounds.Left; x < bounds.Right; x++)
            if (bitmap.GetPixel(x, y).A != 0)
                return true;
        return false;
    }

    private sealed class StaticIffSchemaProvider(IffSchema schema) : IIffSchemaProvider
    {
        public IffSchemaResolution Resolve(string fileName, string region, int recordSize) =>
            fileName.Equals("Item.iff", StringComparison.OrdinalIgnoreCase)
                ? new IffSchemaResolution(schema)
                : new IffSchemaResolution(null);
    }

    private static void RunSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { failure = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null) throw new Xunit.Sdk.XunitException(failure.ToString());
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { }
    }
}
