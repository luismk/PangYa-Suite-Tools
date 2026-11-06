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
    [Fact]
    public void UiBitmapFont_UsesShiftJisGlyphIndicesAndNativeAdvances()
    {
        string path = Path.Combine(_directory, "shift-jis-preview.wft");
        using (var stream = File.Create(path))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(0x544E4657u);
            writer.Write(4u); writer.Write(0u); writer.Write(0u);
            stream.SetLength(16L + PangyaAPI.WFT.WftFont.LegacyGlyphCount * 6L);
            void Glyph(int code, byte firstRow, ushort width)
            {
                stream.Position = 16L + (code - 0x20) * 6L;
                writer.Write(firstRow); writer.Write(new byte[3]); writer.Write(width);
            }
            Glyph('A', 0x80, 2);
            Glyph(0x82a0, 0x40, 3); // Shift-JIS for Japanese a.
            Glyph(0x3042, 0x20, 1); // A deliberately different glyph at the Unicode index.
            Glyph(0xb1, 0x80, 1);   // Half-width katakana a.
            Glyph('?', 0x80, 1);
        }
        using var source = PangyaAPI.WFT.WftFontReader.Open(path);
        var font = new PangyaPreviewBitmapFont(source, CancellationToken.None);
        Assert.Equal(11, font.Measure("Aあｱ 😀"));
        using var bitmap = new Bitmap(20, 8);
        using Graphics graphics = Graphics.FromImage(bitmap);
        using var brush = new SolidBrush(Color.Lime);
        font.Draw(graphics, "Aあｱ 😀", brush, PointF.Empty);
        foreach (int x in new[] { 0, 4, 6, 9 })
            Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(x, 0).ToArgb());
        Assert.Equal(0, bitmap.GetPixel(5, 0).A);
        Assert.Equal(1, font.SetSpace(3));
        Assert.Equal(19, font.Measure("Aあｱ 😀"));
        Assert.Equal(3, font.SetSpace(1));
        Assert.Equal(11, font.Measure("Aあｱ 😀"));
    }

    [Fact]
    public void UiFontState_ScaleSnapshotsKeepMetricsAndDrawTransformsConsistent()
    {
        PointF? transformed = null;
        bool fail = false;
        var font = new PangyaUiFontState(10, text => text.Length * 6, (graphics, _, _, origin) =>
        {
            PointF[] points = [new(origin.X + 1, origin.Y + 1)];
            using var transform = graphics.Transform;
            transform.TransformPoints(points);
            transformed = points[0];
            if (fail) throw new InvalidOperationException("Draw failed");
        });
        font.SetScale(2);
        PangyaUiRenderOptions doubled = font.ApplyTo();
        font.SetScale(0.5f);
        PangyaUiRenderOptions small = font.ApplyTo(smallFont: true);
        Assert.Equal(24, doubled.MeasureCaptionText!("AB"));
        Assert.Equal(10, doubled.CaptionFontHeight);
        Assert.Equal(6, small.MeasureTooltipText!("AB"));
        Assert.Equal(10, small.TooltipFontHeight);
        using var bitmap = new Bitmap(40, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        using var brush = new SolidBrush(Color.Black);
        doubled.DrawCaptionText!(graphics, "A", brush, new PointF(10, 12));
        Assert.Equal(new PointF(12, 14), transformed);
        using (var transform = graphics.Transform) Assert.True(transform.IsIdentity);
        fail = true;
        Assert.Throws<InvalidOperationException>(() => doubled.DrawCaptionText!(graphics, "A", brush, new PointF(10, 12)));
        using (var transform = graphics.Transform) Assert.True(transform.IsIdentity);
        Assert.Throws<ArgumentOutOfRangeException>(() => font.SetScale(float.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => font.SetScale(0));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_TextOutlinePassesFollowNativeOrderForBothFonts(bool smallFont)
    {
        string path = Path.Combine(_directory, "native-text-outline-order.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 40">
              <item type="STATIC" name="label" caption="A" rect="10 10 50 30">
                <param name="style" var="2"/><param name="color" var="ffffffff"/>
                <param name="color2" var="ff000000"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode label = Assert.Single(document.Nodes, node => node.Name == "label");
        var drawn = new List<(PointF Position, int Color)>();
        void Draw(Graphics _, string text, Brush brush, PointF position) =>
            drawn.Add((position, ((SolidBrush)brush).Color.ToArgb()));
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(60, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, new PangyaUiRenderOptions
        {
            SmallFontCaptions = smallFont ? new HashSet<PangyaUiNode> { label } : null,
            MeasureCaptionText = text => text.Length * 6,
            MeasureTooltipText = text => text.Length * 6,
            DrawCaptionText = Draw, DrawTooltipText = Draw
        });
        PointF[] expected = [new(10, 9), new(9, 10), new(11, 10), new(10, 11),
            new(9, 9), new(11, 9), new(9, 11), new(11, 11), new(10, 10)];
        Assert.Equal(expected, drawn.Select(pass => pass.Position));
        Assert.All(drawn.Take(8), pass => Assert.Equal(Color.Black.ToArgb(), pass.Color));
        Assert.Equal(Color.White.ToArgb(), drawn[^1].Color);
    }

    [Theory]
    [InlineData(false, 14)]
    [InlineData(true, 34)]
    public void UiRenderer_FrameDescriptionUsesInitializedSliceHeight(bool loadSkin, int expectedY)
    {
        if (loadSkin)
            for (int index = 0; index < 9; index++)
                SaveSolidImage(Path.Combine(_directory, $"description-skin{index:00}.png"), Color.Blue, 2, 2);
        string path = Path.Combine(_directory, "frame-description-height.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="FRAME" name="skin" color="ffffffff">
                <layer type="0"/><bfrm filename="description-skin"/>
              </element>
              <element type="FORM" name="form" size="60 80">
                <item type="FRAME" name="frame" resource="skin" rect="10 40 40 60">
                  <param name="description" var="Details"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load([path]));
        var drawn = new List<(string Text, PointF Position)>();
        using var bitmap = new Bitmap(60, 80);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, new PangyaUiRenderOptions
        {
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, text, _, point) => drawn.Add((text, point))
        });
        Assert.Equal(("Details", new PointF(20, expectedY)), Assert.Single(drawn));
    }

    [Fact]
    public void UiRenderer_ViewFocusTransitionsTargetContainingFormAndRespectModalAndStopGuards()
    {
        string path = Path.Combine(_directory, "view-focus-transitions.xml");
        File.WriteAllText(path, """
            <resource><element type="LAYOUT" name="desktop" size="100 100">
              <item type="FORM" name="left" rect="0 0 40 40">
                <item type="AREA" name="child" rect="0 0 10 10"/>
                <item type="FORM" name="inner" rect="0 0 10 10"/>
              </item>
              <item type="FORM" name="right" rect="40 0 80 40"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode Node(string name) => Assert.Single(document.Nodes, node => node.Name == name);
        PangyaUiNode child = Node("child"), left = Node("left"), right = Node("right");
        PangyaUiRenderOptions first = PangyaUiRenderer.SetWindowViewFocus(child);
        Assert.Contains(left, first.ViewFocusedNodes!);
        Assert.DoesNotContain(child, first.ViewFocusedNodes!);
        Assert.True(PangyaUiRenderer.IsWindowViewFocused(child, first));
        Assert.False(PangyaUiRenderer.IsWindowViewFocused(Node("inner"), first));
        Assert.False(PangyaUiRenderer.IsWindowViewFocused(right, first));
        PangyaUiRenderOptions both = PangyaUiRenderer.SetWindowViewFocus(right, false, first);
        Assert.True(PangyaUiRenderer.IsWindowViewFocused(left, both));
        Assert.True(PangyaUiRenderer.IsWindowViewFocused(right, both));
        Assert.Same(both, PangyaUiRenderer.SetWindowViewFocus(right, options: both, modalForm: left));
        PangyaUiRenderOptions reset = PangyaUiRenderer.ResetWindowViewFocus(Node("desktop"), both, right);
        Assert.False(PangyaUiRenderer.IsWindowViewFocused(left, reset));
        Assert.True(PangyaUiRenderer.IsWindowViewFocused(right, reset));
        Assert.True(PangyaUiRenderer.IsWindowViewFocused(left, first));
        Assert.Same(first, PangyaUiRenderer.ResetWindowViewFocus(left, first, left));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(-1, true)]
    public void UiCanvas_AreaMouseEventsControlHandCursor(int mouseEvent, bool hand)
    {
        SaveSolidImage(Path.Combine(_directory, "area-cursor.png"), Color.Lime, 20, 20);
        string path = Path.Combine(_directory, "area-cursor.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="50 40">
              <item type="AREA" name="area" rect="5 5 25 25">
                <param name="bgimg" var="area-cursor.png"/><param name="mouse_event" var="{mouseEvent}"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode area = Assert.Single(document.Nodes, node => node.Name == "area");
        Assert.Equal(hand, PangyaUiRenderer.AreaAcceptsMouse(area));
        PangyaUiRenderOptions active = PangyaUiRenderer.SetAreaMouseEvent(area, true);
        Assert.True(PangyaUiRenderer.AreaAcceptsMouse(area, active));
        Assert.False(PangyaUiRenderer.AreaAcceptsMouse(area, PangyaUiRenderer.SetAreaMouseEvent(area, false, active)));
        Assert.True(PangyaUiRenderer.AreaAcceptsMouse(area, active));
        using var images = new PangyaFileImageProvider(_directory);
        RunSta(() =>
        {
            using var canvas = new PangyaUiCanvas(images, PangyaUiResourceCatalog.Empty);
            canvas.LoadDocument(document);
            canvas.SelectedForm = form;
            canvas.CanvasMouseMove(canvas, new MouseEventArgs(MouseButtons.None, 0,
                PangyaUiCanvas.FormPadding + 10, PangyaUiCanvas.FormPadding + 10, 0));
            Assert.Equal(hand ? Cursors.Hand : Cursors.Default, canvas.Cursor);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_ReferencedAreaBoundsApplyAbsoluteClipsAtTheInstanceOrigin(bool runtimeClip)
    {
        SaveSolidImage(Path.Combine(_directory, "referenced-clip.png"), Color.Lime, 8, 6);
        string clipParameter = runtimeClip ? "" : "<param name='clip_area' var='30 21 34 24'/>";
        string path = Path.Combine(_directory, "referenced-absolute-clip.xml");
        File.WriteAllText(path, $"""
            <resource>
              <element type="MACROITEM" name="tiles">
                <item type="GROUPBOX" name="inside" rect="3 4 30 30">
                  <item type="AREA" name="image" pos="5 6">
                    <param name="bgimg" var="referenced-clip.png"/><param name="enable_clip" var="1"/>
                    {clipParameter}
                  </item>
                </item>
              </element>
              <element type="FORM" name="host" size="80 60">
                <item type="GROUPBOX" name="outer" rect="20 10 60 50">
                  <item type="MACROITEM" name="instance" resource="tiles" pos="99 99"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.Name == "host");
        PangyaUiNode instance = Assert.Single(document.Nodes, node => node.Name == "instance");
        PangyaUiResourceCatalog catalog = PangyaUiResourceCatalog.Load([path]);
        PangyaUiNode definition = Assert.IsType<PangyaUiNode>(catalog.TryResolve("tiles", "MACROITEM", new HashSet<string>()));
        PangyaUiNode area = Assert.Single(Assert.Single(definition.Children).Children);
        PangyaUiRenderOptions options = runtimeClip
            ? PangyaUiRenderer.SetAreaClipArea(area, new Rectangle(30, 21, 4, 3)) : new();
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, catalog);
        Assert.Equal(new Rectangle(30, 21, 4, 3), renderer.GetRenderedBounds(instance, options));
        Assert.Same(instance, renderer.HitTest(document, form, new Point(31, 22), options));
        using var bitmap = new Bitmap(80, 60);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(31, 22).ToArgb());
        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(28, 20).ToArgb());
    }

    [Theory]
    [InlineData("10 10 5 15")]
    [InlineData("5 15 10 10")]
    [InlineData("5 6 5 12")]
    [InlineData("invalid")]
    [InlineData("2147483647 0 -2147483648 10")]
    public void UiRenderer_ExplicitInvalidAreaClipDoesNotFallBackToWindowBounds(string clip)
    {
        SaveSolidImage(Path.Combine(_directory, "invalid-clip.png"), Color.Lime, 8, 6);
        string path = Path.Combine(_directory, "explicit-invalid-area-clip.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="30 20">
              <item type="AREA" name="area" rect="5 6 13 12">
                <param name="bgimg" var="invalid-clip.png"/>
                <param name="enable_clip" var="1"/><param name="clip_area" var="{clip}"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode area = Assert.Single(document.Nodes, node => node.Name == "area");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        Assert.Equal(Rectangle.Empty, renderer.GetRenderedBounds(area));
        Assert.Null(renderer.HitTest(document, form, new Point(6, 7)));
        using var bitmap = new Bitmap(30, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form);
        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(6, 7).ToArgb());
        PangyaUiRenderOptions disabled = PangyaUiRenderer.EnableAreaClipping(area, false);
        Assert.Equal(new Rectangle(5, 6, 8, 6), renderer.GetRenderedBounds(area, disabled));
        renderer.Render(graphics, document, form, disabled);
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(6, 7).ToArgb());
    }

    [Theory]
    [InlineData(1, 7)]
    [InlineData(2, 15)]
    [InlineData(3, 22)]
    public void UiRenderer_ViewerThumbTravelUsesUnroundedThumbHeight(int row, int expectedY)
    {
        SaveSolidImage(Path.Combine(_directory, "scroll_n_1.png"), Color.Red, 3, 3);
        SaveSolidImage(Path.Combine(_directory, "scroll_n_2.png"), Color.Red, 3, 1);
        SaveSolidImage(Path.Combine(_directory, "scroll_n_3.png"), Color.Red, 3, 3);
        SaveSolidImage(Path.Combine(_directory, "thumb-content.png"), Color.Blue, 20, 100);
        string path = Path.Combine(_directory, "fractional-thumb-travel.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 60">
              <item type="VIEWER" name="viewer" rect="0 0 20 43">
                <param name="image" var="thumb-content.png"/><param name="use_wheel" var="true"/>
              </item>
            </element></resource>
            """);
        PangyaUiNode viewer = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "viewer");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            ViewerTopRows = new Dictionary<PangyaUiNode, int> { [viewer] = row }
        };
        // Native height is 6 + 37 * 2 / 5 = 20.8, so travel rounds to 22,
        // rather than 23 obtained by truncating height before calculating travel.
        Rectangle thumb = renderer.GetViewerScrollThumb(viewer, options);
        Assert.Equal(expectedY, thumb.Y);
        Assert.Equal(21, thumb.Height);
    }

    [Fact]
    public void UiRenderer_ReplacingContextMenuListResetsRowsAndRetainsIndependentAppearance()
    {
        string path = Path.Combine(_directory, "context-menu-rebuild.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 100">
              <item type="CONTEXTMENU" name="menu" caption="Old" rect="5 6 50 60"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode menu = Assert.Single(document.Nodes, node => node.Name == "menu");
        var original = new PangyaUiRenderOptions
        {
            HoveredNode = menu,
            MenuStates = new Dictionary<PangyaUiNode, PangyaUiMenuState>
            {
                [menu] = new(0, new HashSet<int> { 0 }, 77)
                {
                    Opacity = 123, DisabledTexts = new HashSet<string> { "New" },
                    TextReplacements = new Dictionary<string, string> { ["New"] = "Stale" }
                }
            },
            ContextMenuItemTexts = new Dictionary<PangyaUiNode, IReadOnlyDictionary<int, string>>
            {
                [menu] = new Dictionary<int, string> { [0] = "Stale" }
            },
            CaptionFontHeight = 10, MeasureCaptionText = text => text.Length * 6
        };
        PangyaUiRenderOptions updated = PangyaUiRenderer.SetContextMenuList(menu, "New|Second", original);
        Assert.Equal(-1, updated.MenuStates![menu].SelectedRow);
        Assert.Null(updated.MenuStates[menu].DisabledRows);
        Assert.Null(updated.MenuStates[menu].DisabledTexts);
        Assert.Null(updated.MenuStates[menu].TextReplacements);
        Assert.Equal(123, updated.MenuStates[menu].Opacity);
        Assert.Equal(77, updated.MenuStates[menu].SelectionAlpha);
        Assert.Null(updated.HoveredNode);
        Assert.False(updated.ContextMenuItemTexts!.ContainsKey(menu));
        Assert.Equal(0, original.MenuStates![menu].SelectedRow);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        Assert.Equal(new Rectangle(5, 6, 72, 52), renderer.GetRenderedBounds(menu, updated));
        Assert.Equal(0, renderer.HitTestMenuRow(menu, new Point(10, 20), updated));
        var drawn = new List<string>();
        using var bitmap = new Bitmap(100, 100);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, updated with
        {
            DrawCaptionText = (_, text, _, _) => drawn.Add(text)
        });
        Assert.Equal(new[] { "New", "Second" }, drawn);
    }

    [Theory]
    [InlineData("FRAME", true)]
    [InlineData("CONTEXTMENU", false)]
    public void UiRenderer_TypeThreeFrameLayersDrawOnlyForNormalFrames(string type, bool drawsSkin)
    {
        for (int index = 0; index < 9; index++)
            SaveSolidImage(Path.Combine(_directory, $"type-three{index:00}.png"), Color.Red, 2, 2);
        string path = Path.Combine(_directory, "context-frame-layer-types.xml");
        File.WriteAllText(path, $"""
            <resource>
              <element type="FRAME" name="skin" color="ffffffff">
                <layer type="3"/><bfrm filename="type-three"/>
              </element>
              <element type="FORM" name="form" size="80 60">
                <item type="{type}" name="control" resource="skin" caption="A" rect="0 0 30 30"/>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load([path]));
        using var bitmap = new Bitmap(80, 60);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, new PangyaUiRenderOptions
        {
            CaptionFontHeight = 10, MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, _, _, _) => { }
        });
        Assert.Equal((drawsSkin ? Color.Red : Color.FromArgb(46, 49, 55)).ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
    }

    [Theory]
    [InlineData("pos='5'", 6, 20)]
    [InlineData("pos='-5'", 20, 6)]
    [InlineData("bottompos='5'", 20, 6)]
    [InlineData("bottompos='-5'", 6, 20)]
    [InlineData("pos='-5' bottompos='-5'", 6, 20)]
    public void UiRenderer_FrameLayerAnchoringUsesNormalizedPositionAndMinimumSliceHeight(
        string anchor, int redY, int blueY)
    {
        for (int index = 0; index < 9; index++)
        {
            SaveSolidImage(Path.Combine(_directory, $"anchor-base{index:00}.png"), Color.Blue, 2, 2);
            SaveSolidImage(Path.Combine(_directory, $"anchor-sub{index:00}.png"), Color.Red, 3, 3);
        }
        string path = Path.Combine(_directory, "frame-layer-anchor.xml");
        File.WriteAllText(path, $"""
            <resource>
              <element type="FRAME" name="skin" color="ffffffff">
                <layer type="0"/><layer type="0" height="2" {anchor}/>
                <bfrm filename="anchor-base"/><sfrm filename="anchor-sub"/>
              </element>
              <element type="FORM" name="form" resource="skin" size="30 30"/>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load([path]));
        using var bitmap = new Bitmap(30, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(15, redY).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(15, blueY).ToArgb());
    }

    [Fact]
    public void UiRenderer_AreaClipStorageDoesNotEnableClippingAndMovementRetainsInitialClip()
    {
        SaveSolidImage(Path.Combine(_directory, "fixed-clip.png"), Color.Lime, 8, 4);
        string path = Path.Combine(_directory, "area-fixed-clip.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="30 20">
              <item type="AREA" name="area" rect="0 0 4 4">
                <param name="bgimg" var="fixed-clip.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode area = Assert.Single(document.Nodes, node => node.Name == "area");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        PangyaUiRenderOptions stored = PangyaUiRenderer.SetAreaClipArea(area, new Rectangle(1, 0, 2, 4));
        Assert.Equal(new Rectangle(0, 0, 8, 4), renderer.GetRenderedBounds(area, stored));
        PangyaUiRenderOptions enabled = PangyaUiRenderer.EnableAreaClipping(area, true, stored);
        Assert.Equal(new Rectangle(1, 0, 2, 4), renderer.GetRenderedBounds(area, enabled));
        Assert.Equal(new Rectangle(0, 0, 8, 4), renderer.GetRenderedBounds(area,
            PangyaUiRenderer.EnableAreaClipping(area, false, enabled)));
        PangyaUiRenderOptions moved = PangyaUiRenderer.MoveWindow(area, new Point(2, 0),
            PangyaUiRenderer.EnableAreaClipping(area, true));
        Assert.Equal(new Rectangle(0, 0, 4, 4), moved.AreaClipOverrides![area]);
        Assert.Equal(new Rectangle(2, 0, 2, 4), renderer.GetRenderedBounds(area, moved));
        using var bitmap = new Bitmap(30, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, moved);
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(3, 1).ToArgb());
        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(5, 1).ToArgb());
        Assert.Null(renderer.HitTest(document, form, new Point(5, 1), moved));
    }

    [Fact]
    public void UiRenderer_BackgroundPathsRemainIndependentOfSharedGraphicsAlpha()
    {
        SaveSolidImage(Path.Combine(_directory, "background-viewer.png"), Color.Blue, 10, 10);
        SaveSolidImage(Path.Combine(_directory, "gdi-area.png"), Color.Red, 10, 10);
        string path = Path.Combine(_directory, "background-alpha-paths.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="VIEWER" name="viewer" rect="0 0 10 10">
                <param name="image" var="background-viewer.png"/>
              </item>
              <item type="AREA" name="area" rect="15 0 25 10">
                <param name="bgimg" var="gdi-area.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode viewer = Assert.Single(document.Nodes, node => node.Name == "viewer");
        using var wallpaper = new Bitmap(40, 30);
        using (Graphics background = Graphics.FromImage(wallpaper)) background.Clear(Color.Lime);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        var options = new PangyaUiRenderOptions
        {
            GraphicsAlpha = 0, StartupWallpaperImage = wallpaper, DesktopWallpaperVisible = true
        };
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(20, 5).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(30, 20).ToArgb());
        renderer.Render(graphics, document, form, options with
        {
            WindowAlpha = new Dictionary<PangyaUiNode, float> { [viewer] = 0 }
        });
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void UiRenderer_TooltipFrameSlicesUseNativeRowMajorPositions()
    {
        Color[] colors = [Color.Red, Color.Lime, Color.Blue, Color.Yellow, Color.Cyan,
            Color.Magenta, Color.White, Color.Orange, Color.Brown];
        for (int index = 0; index < colors.Length; index++)
            SaveSolidImage(Path.Combine(_directory, $"tooltip_frm00_{index:00}.png"), colors[index], 8, 8);
        string path = Path.Combine(_directory, "tooltip-frame-positions.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 100">
              <item type="AREA" name="anchor" rect="0 0 2 2"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode anchor = Assert.Single(document.Nodes, node => node.Name == "anchor");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(100, 100);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, new PangyaUiRenderOptions
        {
            ShowTooltip = true, TooltipNode = anchor, TooltipPointer = Point.Empty, TooltipAlpha = 255,
            TooltipFontHeight = 32, MeasureTooltipText = _ => 24,
            DrawTooltipText = (_, _, _, _) => { },
            Tooltips = new Dictionary<PangyaUiNode, PangyaUiTooltipPreview>
            {
                [anchor] = new("T", Style: 0, Position: new Point(10, 10))
            }
        });
        int[] positions = [11, 25, 43];
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                Assert.Equal(colors[row * 3 + column].ToArgb(), bitmap.GetPixel(positions[column], positions[row]).ToArgb());
    }

    [Fact]
    public void UiRenderer_TooltipUsesItsOwnAlphaInsteadOfGlobalGraphicsAlpha()
    {
        for (int index = 0; index < 9; index++)
            SaveSolidImage(Path.Combine(_directory, $"tooltip_frm00_{index:00}.png"), Color.White, 8, 8);
        string path = Path.Combine(_directory, "tooltip-independent-alpha.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 100">
              <item type="STATIC" name="anchor" caption="A" rect="0 0 20 10">
                <param name="color" var="ffffffff"/><param name="tooltip" var="Tip"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode anchor = Assert.Single(document.Nodes, node => node.Name == "anchor");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(100, 100);
        using Graphics graphics = Graphics.FromImage(bitmap);
        int? framePixel = null;
        foreach (float globalAlpha in new[] { 0f, 0.5f, 1f })
        {
            var captionAlphas = new List<int>();
            var tooltipAlphas = new List<int>();
            renderer.Render(graphics, document, form, new PangyaUiRenderOptions
            {
                GraphicsAlpha = globalAlpha,
                ShowTooltip = true, TooltipNode = anchor, TooltipPointer = new Point(5, 5),
                TooltipAlpha = 128, TooltipStyle = 0, TooltipFontHeight = 11,
                MeasureTooltipText = text => text.Length * 6,
                DrawCaptionText = (_, _, brush, _) => captionAlphas.Add(((SolidBrush)brush).Color.A),
                DrawTooltipText = (_, _, brush, _) => tooltipAlphas.Add(((SolidBrush)brush).Color.A)
            });
            Assert.Equal(new[] { (int)(255 * globalAlpha) }, captionAlphas);
            Assert.Equal(new[] { 128 }, tooltipAlphas);
            int pixel = bitmap.GetPixel(26, 26).ToArgb();
            if (framePixel is int expected) Assert.Equal(expected, pixel);
            else framePixel = pixel;
            Assert.NotEqual(Color.FromArgb(46, 49, 55).ToArgb(), pixel);
        }
    }

    [Theory]
    [InlineData(256, 20, -64, 236)]
    [InlineData(0, 276, 256, -215)]
    public void UiProperties_PositionEditsPreserveReversedRectangleEdges(int x, int y, int width, int height)
    {
        string path = Path.Combine(_directory, "signed-property-rectangle.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="500 450">
              <item type="AREA" name="image" rect="{x} {y} {x + width} {y + height}">
                <param name="bgimg" var="tile.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiNode node = Assert.Single(PangyaUiDocument.Load(path).Nodes, item => item.Name == "image");
        using var images = new PangyaFileImageProvider(_directory);
        RunSta(() =>
        {
            using var panel = new PangyaUiPropertyPanel(images) { SelectedNode = node };
            NumericUpDown Field(string name) => Assert.IsType<NumericUpDown>(
                typeof(PangyaUiPropertyPanel).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(panel));
            Assert.Equal((decimal)width, Field("_width").Value);
            Assert.Equal((decimal)height, Field("_height").Value);
            Field("_x").Value = x + 1;
            Assert.Equal(new Rectangle(x + 1, y, width, height), node.Bounds);
            Assert.Equal($"{x + 1} {y} {x + 1 + width} {y + height}", node.Element.GetAttribute("rect"));
            Assert.Equal("tile.png", node.GetResource(PangyaUiButtonState.Normal));
        });
    }

    [Fact]
    public void UiRenderer_ReversedAreaEdgesPreserveSiblingLayoutOriginsAndAllowImageOverflow()
    {
        SaveSolidImage(Path.Combine(_directory, "tile-left.png"), Color.Blue, 4, 4);
        SaveSolidImage(Path.Combine(_directory, "tile-right.png"), Color.Lime, 4, 4);
        SaveSolidImage(Path.Combine(_directory, "tile-bottom.png"), Color.Red, 4, 4);
        string path = Path.Combine(_directory, "legacy-tile-rectangles.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="GROUPBOX" name="parent" rect="10 5 30 25">
                <item type="AREA" name="left" rect="0 2 4 6"><param name="bgimg" var="tile-left.png"/></item>
                <item type="AREA" name="right" rect="4 2 2 6"><param name="bgimg" var="tile-right.png"/></item>
                <item type="AREA" name="bottom" rect="0 6 4 2"><param name="bgimg" var="tile-bottom.png"/></item>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode right = Assert.Single(document.Nodes, node => node.Name == "right");
        PangyaUiNode bottom = Assert.Single(document.Nodes, node => node.Name == "bottom");
        Assert.Equal(new Rectangle(4, 2, -2, 4), right.Bounds);
        Assert.Equal(new Rectangle(0, 6, 4, -4), bottom.Bounds);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions();
        Assert.Equal(new Rectangle(14, 7, 4, 4), renderer.GetRenderedBounds(right, options));
        Assert.Equal(new Rectangle(10, 11, 4, 4), renderer.GetRenderedBounds(bottom, options));
        Assert.Same(right, renderer.HitTest(document, form, new Point(17, 9), options));
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(12, 9).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(17, 9).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(12, 14).ToArgb());
        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
    }

    [Theory]
    [InlineData("shared", false)]
    [InlineData("SHARED", false)]
    [InlineData("shared", true)]
    [InlineData("SHARED", true)]
    public void UiRenderer_ResourceCycleGuardDistinguishesTypedAndCaseSensitiveDefinitions(string formName, bool topmost)
    {
        SaveSolidImage(Path.Combine(_directory, "typed-reference.png"), Color.Red, 6, 5);
        string path = Path.Combine(_directory, "typed-reference-cycle.xml");
        File.WriteAllText(path, $"""
            <resource>
              <element type="FRAME" name="empty-frame"/>
              <element type="FORM" name="{formName}" resource="empty-frame" size="20 20">
                <item type="AREA" name="image" pos="3 4"><param name="bgimg" var="typed-reference.png"/></item>
              </element>
              <element type="MACROITEM" name="shared">
                <item type="FORM" name="form-instance" resource="{formName}" pos="5 6"/>
                <item type="MACROITEM" name="real-cycle" resource="shared"/>
              </element>
              <element type="FORM" name="host" size="100 80">
                <item type="GROUPBOX" name="group" rect="20 10 80 70">
                  <item type="MACROITEM" name="instance" resource="shared" pos="60 50"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode host = Assert.Single(document.Nodes, node => node.Name == "host");
        PangyaUiNode instance = Assert.Single(document.Nodes, node => node.Name == "instance");
        PangyaUiNode formInstance = Assert.Single(document.Nodes, node => node.Name == "form-instance");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load([path]));
        var options = new PangyaUiRenderOptions
        {
            TopmostNodes = topmost ? new HashSet<PangyaUiNode> { formInstance } : null
        };
        Assert.Equal(new Rectangle(25, 16, 20, 20), renderer.GetRenderedBounds(instance, options));
        using var bitmap = new Bitmap(100, 80);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, host, options);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(29, 21).ToArgb());
    }

    [Fact]
    public void UiRenderer_CrossFileFormAndMacroImagesUseParentOriginsWithoutSiblingAccumulation()
    {
        SaveSolidImage(Path.Combine(_directory, "relative-red.png"), Color.Red, 4, 3);
        SaveSolidImage(Path.Combine(_directory, "relative-green.png"), Color.Lime, 4, 3);
        string definitions = Path.Combine(_directory, "relative-definitions.xml");
        File.WriteAllText(definitions, """
            <resource>
              <element type="FRAME" name="empty-frame"/>
              <element type="FORM" name="panel" resource="empty-frame" size="10 10">
                <item type="GROUPBOX" name="inside" rect="3 4 30 30">
                  <item type="AREA" name="red" pos="5 6"><param name="bgimg" var="relative-red.png"/></item>
                </item>
                <item type="AREA" name="green" pos="2 1"><param name="bgimg" var="relative-green.png"/></item>
              </element>
              <element type="MACROITEM" name="macro-definition">
                <item type="FORM" name="panel-instance" resource="panel" pos="11 12"/>
              </element>
            </resource>
            """);
        string path = Path.Combine(_directory, "relative-main.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="main" size="120 100">
              <item type="GROUPBOX" name="host" rect="20 15 100 90">
                <item type="MACROITEM" name="macro" resource="macro-definition" pos="70 60"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode macro = Assert.Single(document.Nodes, node => node.Name == "macro");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load([definitions, path]));
        var options = new PangyaUiRenderOptions();
        Assert.Equal(new Rectangle(31, 27, 12, 13), renderer.GetRenderedBounds(macro, options));
        Assert.Same(macro, renderer.HitTest(document, form, new Point(40, 38), options));
        using var bitmap = new Bitmap(120, 100);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(40, 38).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(34, 29).ToArgb());
        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(90, 75).ToArgb());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_SetWindowRectangleResetsImmediateLoadedViewerScroll(bool followBottom)
    {
        SaveSolidImage(Path.Combine(_directory, "resize-viewer.png"), Color.Lime, 20, 200);
        string path = Path.Combine(_directory, "viewer-resize-callbacks.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 100">
              <item type="VIEWER" name="direct" rect="0 0 20 40">
                <param name="image" var="resize-viewer.png"/><param name="use_wheel" var="true"/>
              </item>
              <item type="GROUPBOX" name="group" rect="30 0 60 80">
                <item type="VIEWER" name="nested" rect="0 0 20 40">
                  <param name="image" var="resize-viewer.png"/><param name="use_wheel" var="true"/>
                </item>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode direct = Assert.Single(document.Nodes, node => node.Name == "direct");
        PangyaUiNode nested = Assert.Single(document.Nodes, node => node.Name == "nested");
        var original = new PangyaUiRenderOptions
        {
            ViewerTopRows = new Dictionary<PangyaUiNode, int> { [direct] = 5, [nested] = 5 },
            ViewerScrollRows = new Dictionary<PangyaUiNode, float> { [direct] = 5.5f, [nested] = 5.5f },
            ViewerFollowBottom = new Dictionary<PangyaUiNode, bool> { [direct] = followBottom }
        };
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        PangyaUiRenderOptions resized = renderer.SetWindowRectangle(form, new Rectangle(0, 0, 120, 120), original);
        Assert.Equal(new Size(120, 120), resized.WindowSizes![form]);
        Assert.Equal(followBottom ? 8 : 0, resized.ViewerTopRows![direct]);
        Assert.Equal(followBottom ? 8 : 0, resized.ViewerScrollRows![direct]);
        Assert.Equal(5, resized.ViewerTopRows[nested]);
        Assert.Equal(5.5f, resized.ViewerScrollRows[nested]);
        PangyaUiRenderOptions ownResize = renderer.SetWindowRectangle(nested, new Rectangle(30, 0, 20, 60), resized);
        Assert.Equal(0, ownResize.ViewerScrollRows![nested]);
        Assert.Equal(5.5f, original.ViewerScrollRows[direct]);
        Assert.Throws<ArgumentOutOfRangeException>(() => renderer.SetWindowRectangle(form, new Rectangle(0, 0, -1, 10)));
    }

    [Fact]
    public void UiRenderer_ComboAutomaticOpeningSideSurvivesMovementAndRowUpdates()
    {
        string path = Path.Combine(_directory, "combo-retained-side.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 100">
              <item type="COMBOBOX" name="combo" rect="5 5 45 25">
                <param name="lineheight" var="16"/>
              </item>
            </element></resource>
            """);
        PangyaUiNode node = Assert.Single(PangyaUiDocument.Load(path).Nodes, item => item.Name == "combo");
        var model = new PangyaUiComboList();
        model.AddString("A");
        model.ToggleDropdown();
        PangyaUiRenderOptions original = model.ApplyTo(node, new PangyaUiRenderOptions { ComboViewportHeight = 100 });
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        Assert.Equal(new Rectangle(5, 27, 40, 16), renderer.GetComboDropdownBounds(node, original));
        PangyaUiRenderOptions moved = PangyaUiRenderer.MoveWindow(node, new Point(5, 70), original);
        Assert.Equal(new Rectangle(5, 92, 40, 16), renderer.GetComboDropdownBounds(node, moved));
        PangyaUiRenderOptions initialized = renderer.InitializeComboOpeningSide(node, 100, moved);
        Assert.Equal(new Rectangle(5, 52, 40, 16), renderer.GetComboDropdownBounds(node, initialized));
        PangyaUiRenderOptions returned = PangyaUiRenderer.MoveWindow(node, new Point(5, 5), initialized);
        Assert.Equal(new Rectangle(5, -13, 40, 16), renderer.GetComboDropdownBounds(node, returned));
        model.AddString("B");
        PangyaUiRenderOptions updated = model.ApplyTo(node, returned);
        Assert.Equal(new Rectangle(5, -29, 40, 32), renderer.GetComboDropdownBounds(node, updated));
        model.Direction = PangyaUiComboDirection.Below;
        Assert.Equal(new Rectangle(5, 27, 40, 32), renderer.GetComboDropdownBounds(node, model.ApplyTo(node, updated)));
        Assert.Null(original.Combos![node].AutomaticOpensAbove);
    }

    [Fact]
    public void UiRenderer_MoveWindowKeepsNestedFormsStationaryAndMovesScrollingClips()
    {
        string path = Path.Combine(_directory, "move-window-forms.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="root" size="100 100">
              <item type="GROUPBOX" name="group" rect="10 20 90 90">
                <item type="STATIC" name="moving" caption="Moving" rect="5 6 25 16">
                  <param name="scrolling" var="true"/>
                </item>
                <item type="FORM" name="stationary" rect="30 40 60 70">
                  <item type="STATIC" name="stationaryText" caption="Stays" rect="1 2 20 12">
                    <param name="scrolling" var="true"/>
                  </item>
                </item>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode root = Assert.Single(document.Nodes, node => node.Name == "root");
        PangyaUiNode moving = Assert.Single(document.Nodes, node => node.Name == "moving");
        PangyaUiNode stationary = Assert.Single(document.Nodes, node => node.Name == "stationary");
        PangyaUiNode stationaryText = Assert.Single(document.Nodes, node => node.Name == "stationaryText");
        PangyaUiRenderOptions moved = PangyaUiRenderer.MoveWindow(root, new Point(100, 50));
        Assert.Equal(new Point(100, 50), moved.WindowOffsets![root]);
        Assert.Equal(new Point(-100, -50), moved.WindowOffsets[stationary]);
        Assert.Equal(new Rectangle(115, 76, 20, 10), moved.CaptionClips![moving]);
        Assert.True(moved.CaptionClippingEnabled![moving]);
        Assert.False(moved.CaptionClips.ContainsKey(stationaryText));
        PangyaUiRenderOptions repeated = PangyaUiRenderer.MoveWindow(root, new Point(20, 10), moved);
        Assert.Equal(new Point(20, 10), repeated.WindowOffsets![root]);
        Assert.Equal(new Point(-20, -10), repeated.WindowOffsets[stationary]);
        Assert.Equal(new Rectangle(35, 36, 20, 10), repeated.CaptionClips![moving]);
        Assert.Equal(new Point(100, 50), moved.WindowOffsets[root]);
    }

    [Fact]
    public void UiComboList_KeyboardWrapAndDropdownFocusHaveIndependentState()
    {
        var combo = new PangyaUiComboList();
        Assert.False(combo.SelectNext());
        Assert.False(combo.SelectPrevious());
        combo.AddString("One");
        combo.AddString("Two");
        Assert.True(combo.SelectPrevious());
        Assert.Equal(1, combo.SelectedIndex);
        Assert.Equal("Two", combo.DisplayedText);
        Assert.True(combo.SelectNext());
        Assert.Equal(0, combo.SelectedIndex);
        combo.ToggleDropdown();
        combo.SelectNext();
        Assert.True(combo.IsOpen);
        combo.LoseDropdownFocus(true, false);
        Assert.True(combo.IsOpen);
        combo.LoseDropdownFocus(false, true);
        Assert.True(combo.IsOpen);
        combo.LoseDropdownFocus(false, false);
        Assert.False(combo.IsOpen);
        combo.ToggleDropdown();
        Assert.True(combo.ClickRow(0));
        Assert.False(combo.IsOpen);
        combo.ToggleDropdown();
        Assert.False(combo.ClickRow(99));
        Assert.False(combo.IsOpen);
        Assert.Equal("One", combo.DisplayedText);
        combo.ToggleDropdown();
        combo.ClearList();
        Assert.True(combo.IsOpen);
        Assert.Equal("One", combo.DisplayedText);
    }

    [Fact]
    public void UiRenderer_TabStateControlsMouseEventsAndAuthoritativeHover()
    {
        SaveSolidImage(Path.Combine(_directory, "state-tab_n.png"), Color.Lime, 10, 10);
        SaveSolidImage(Path.Combine(_directory, "state-tab_o.png"), Color.Red, 10, 10);
        string path = Path.Combine(_directory, "tab-state-mouse.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="TABBUTTON" name="tabs" rect="0 0 30 20"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode node = Assert.Single(document.Nodes, item => item.Name == "tabs");
        var state = new PangyaUiTabState();
        state.AddTab(PangyaUiTab.FromImagePrefix("state-tab", 1, new Size(10, 10)));
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var original = new PangyaUiRenderOptions { HoveredNode = node, Pointer = new Point(5, 5) };
        PangyaUiRenderOptions disabled = state.ApplyTo(node, original);
        Assert.Equal(-1, renderer.HitTestTab(node, new Point(5, 5), disabled));
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, disabled);
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        state.Activation = PangyaUiTabActivation.ButtonUp;
        state.SetHoveredIndex(0);
        PangyaUiRenderOptions hovered = state.ApplyTo(node, original);
        Assert.Equal(0, renderer.HitTestTab(node, new Point(5, 5), hovered));
        renderer.Render(graphics, document, form, hovered);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        state.SetHoveredIndex(null);
        renderer.Render(graphics, document, form, state.ApplyTo(node, original));
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        renderer.Render(graphics, document, form, hovered);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void UiTabState_RetainsFirstHeightAndSelectsFirstDuplicateIdentifier()
    {
        var state = new PangyaUiTabState(3);
        state.AddTab(PangyaUiTab.FromImagePrefix("first", 7), new Size(10, 5));
        state.AddTab(PangyaUiTab.FromImagePrefix("second", 7, new Size(20, 15)));
        Assert.Equal(new Size(33, 5), state.Size);
        Assert.True(state.Select(7));
        Assert.Equal(0, state.SelectedIndex);
        Assert.False(state.Select(99));
        Assert.Equal(0, state.SelectedIndex);
        Assert.False(state.Unselect(99));
        Assert.True(state.Unselect(7));
        Assert.Null(state.SelectedIndex);
        state.Clear();
        Assert.Empty(state.Tabs);
        Assert.Equal(Size.Empty, state.Size);
        Assert.Null(state.HoveredIndex);
        Assert.Null(state.PressedIndex);
    }

    [Fact]
    public void UiTabState_ReleaseSelectionRequiresMatchingPressedTab()
    {
        var state = new PangyaUiTabState { Activation = PangyaUiTabActivation.ButtonUp };
        state.AddTab(PangyaUiTab.FromImagePrefix("first", 1, new Size(10, 5)));
        state.AddTab(PangyaUiTab.FromImagePrefix("second", 2, new Size(10, 5)));
        state.ButtonDown(0);
        Assert.Null(state.SelectedIndex);
        Assert.False(state.ButtonUp(1));
        Assert.Null(state.SelectedIndex);
        Assert.Null(state.PressedIndex);
        state.ButtonDown(1);
        Assert.True(state.ButtonUp(1));
        Assert.Equal(1, state.SelectedIndex);
        state.Activation = PangyaUiTabActivation.ButtonDown;
        state.ButtonDown(0);
        Assert.Equal(0, state.SelectedIndex);
        Assert.False(state.ButtonUp(1));
        Assert.Equal(0, state.SelectedIndex);
    }

    [Fact]
    public void UiTabState_ApplyProducesIndependentRenderSnapshots()
    {
        string path = Path.Combine(_directory, "tab-state.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 30">
              <item type="TABBUTTON" name="tabs" rect="0 0 50 20"/>
            </element></resource>
            """);
        PangyaUiNode node = Assert.Single(PangyaUiDocument.Load(path).Nodes, item => item.Name == "tabs");
        var state = new PangyaUiTabState(2);
        state.AddTab(PangyaUiTab.FromImagePrefix("first", 7, new Size(10, 5)));
        state.Select(7);
        PangyaUiRenderOptions first = state.ApplyTo(node);
        state.Clear();
        PangyaUiRenderOptions cleared = state.ApplyTo(node, first);
        Assert.Equal(PangyaUiButtonState.Selected, Assert.Single(first.Tabs![node]).State);
        Assert.Equal(new Size(10, 5), first.WindowSizes![node]);
        Assert.Empty(cleared.Tabs![node]);
        Assert.Equal(Size.Empty, cleared.WindowSizes![node]);
        Assert.Equal(2, cleared.TabSeparatorWidths![node]);
    }

    [Fact]
    public void UiRenderer_DeferredDisplayDrawsChildrenAfterNormalWindowsAndSkipsContainer()
    {
        SaveSolidImage(Path.Combine(_directory, "deferred-red.png"), Color.Red, 20, 20);
        SaveSolidImage(Path.Combine(_directory, "deferred-green.png"), Color.Lime, 10, 10);
        SaveSolidImage(Path.Combine(_directory, "deferred-blue.png"), Color.Blue, 10, 10);
        string path = Path.Combine(_directory, "deferred-display.xml");
        File.WriteAllText(path, """
            <resource><element type="LAYOUT" name="layout" size="40 30">
              <item type="FORM" name="deferred" resource="deferred-red.png" rect="0 0 20 20">
                <item type="AREA" name="child" resource="deferred-green.png" rect="0 0 10 10"/>
                <item type="GROUPBOX" name="nested" rect="0 0 20 20">
                  <item type="AREA" name="suppressed" resource="deferred-red.png" rect="0 0 10 10"/>
                </item>
              </item>
              <item type="AREA" name="normal" resource="deferred-blue.png" rect="0 0 10 10"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode layout = Assert.Single(document.Nodes, node => node.Name == "layout");
        PangyaUiNode container = Assert.Single(document.Nodes, node => node.Name == "deferred");
        PangyaUiNode nested = Assert.Single(document.Nodes, node => node.Name == "nested");
        PangyaUiNode child = Assert.Single(document.Nodes, node => node.Name == "child");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        var options = new PangyaUiRenderOptions
        {
            DeferredDisplayContainers = new HashSet<PangyaUiNode> { container, nested }
        };
        renderer.Render(graphics, document, layout, options);
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(15, 15).ToArgb());
        renderer.Render(graphics, document, layout, options with
        {
            TopmostNodes = new HashSet<PangyaUiNode> { child }
        });
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        renderer.Render(graphics, document, layout, options with
        {
            HiddenNodes = new HashSet<PangyaUiNode> { container }
        });
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
    }

    [Fact]
    public void UiRenderer_GlobalGraphicsAlphaAffectsTexturesAndTextButNotPrimitiveFills()
    {
        SaveSolidImage(Path.Combine(_directory, "graphics-alpha.png"), Color.Lime, 10, 10);
        string path = Path.Combine(_directory, "graphics-alpha.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 30">
              <item type="AREA" name="image" resource="graphics-alpha.png" rect="0 0 10 10"/>
              <item type="STATIC" name="text" caption="Text" rect="20 0 50 10">
                <param name="color" var="50ffffff"/>
              </item>
              <item type="GAUGEBAR" name="gauge" rect="0 15 10 25">
                <param name="bgcolor" var="ffff0000"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode text = Assert.Single(document.Nodes, node => node.Name == "text");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var alphas = new List<int>();
        var options = new PangyaUiRenderOptions
        {
            GraphicsAlpha = 0,
            WindowAlpha = new Dictionary<PangyaUiNode, float> { [text] = 0.5f },
            DrawCaptionText = (_, _, brush, _) => alphas.Add(((SolidBrush)brush).Color.A)
        };
        using var bitmap = new Bitmap(60, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(5, 20).ToArgb());
        Assert.Equal(new[] { 0 }, alphas);
        alphas.Clear();
        renderer.Render(graphics, document, form, options with { GraphicsAlpha = 0.5f });
        // Static manager captions force opaque text before applying graphics alpha.
        Assert.Equal(new[] { 127 }, alphas);
        alphas.Clear();
        renderer.Render(graphics, document, form, options with { GraphicsAlpha = 1 });
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        Assert.Equal(new[] { 255 }, alphas);
    }

    [Fact]
    public void UiRenderer_GlobalGraphicsAlphaIsRestoredAfterNestedRender()
    {
        string path = Path.Combine(_directory, "nested-graphics-alpha.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 30">
              <item type="STATIC" name="text" caption="A" rect="0 0 20 10">
                <param name="color" var="50ffffff"/><param name="style" var="1"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(60, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        var outer = new List<int>();
        var inner = new List<int>();
        renderer.Render(graphics, document, form, new PangyaUiRenderOptions
        {
            GraphicsAlpha = 0.5f,
            DrawCaptionText = (surface, _, brush, _) =>
            {
                outer.Add(((SolidBrush)brush).Color.A);
                if (outer.Count == 1)
                    renderer.Render(surface, document, form, new PangyaUiRenderOptions
                    {
                        GraphicsAlpha = 0.25f,
                        DrawCaptionText = (_, _, nestedBrush, _) => inner.Add(((SolidBrush)nestedBrush).Color.A)
                    });
            }
        });
        Assert.Equal(new[] { 127, 127 }, outer);
        Assert.Equal(new[] { 63, 63 }, inner);
    }

    [Fact]
    public void UiRenderer_ListHitTestingUsesScrolledCellsAndDummyPolicy()
    {
        string path = Path.Combine(_directory, "list-item-hit.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 40">
              <item type="LISTBOX" name="list" rect="0 0 20 25">
                <param name="itemsize" var="10 10"/><param name="column" var="2"/>
                <param name="show_scrollbar" var="0"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode list = Assert.Single(document.Nodes, node => node.Name == "list");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var preview = new PangyaUiListPreview(8, 1, (_, _, _) => { })
        {
            DummyItems = new HashSet<int> { 3 }
        };
        PangyaUiRenderOptions Options(PangyaUiListPreview value) => new()
        {
            Lists = new Dictionary<PangyaUiNode, PangyaUiListPreview> { [list] = value }
        };
        Assert.Equal(2, renderer.HitTestListItem(list, new Point(5, 5), Options(preview)));
        Assert.Equal(3, renderer.HitTestListItem(list, new Point(15, 5), Options(preview)));
        Assert.Equal(-1, renderer.HitTestListItem(list, new Point(15, 5),
            Options(preview with { ExcludeDummyItemsFromHitTesting = true })));
        Assert.Equal(-1, renderer.HitTestListItem(list, new Point(5, 22), Options(preview)));
        Assert.Equal(-1, renderer.HitTestListItem(list, new Point(5, 5),
            Options(preview with { MouseEventsEnabled = false })));
        Assert.Equal(7, renderer.HitTestListItem(list, new Point(15, 15),
            Options(preview with { FollowBottom = true })));
        Assert.Equal(1, renderer.HitTestListItem(list, new Point(5, 5),
            Options(preview with { TopItem = 1 })));
    }

    [Fact]
    public void UiListSelection_KeyboardNavigationMovesRolloverWithoutSelectingOrScrolling()
    {
        var selection = new PangyaUiListSelection(5);
        selection.SelectItem(4);
        Assert.True(selection.NavigateCursor(PangyaUiListNavigation.RowDown, 2));
        Assert.Equal(0, selection.ItemUnderCursor);
        Assert.True(selection.NavigateCursor(PangyaUiListNavigation.RowDown, 2));
        Assert.Equal(2, selection.ItemUnderCursor);
        Assert.True(selection.NavigateCursor(PangyaUiListNavigation.Next, 2));
        Assert.Equal(3, selection.ItemUnderCursor);
        Assert.False(selection.NavigateCursor(PangyaUiListNavigation.RowDown, 2));
        Assert.Equal(3, selection.ItemUnderCursor);
        Assert.True(selection.NavigateCursor(PangyaUiListNavigation.RowUp, 2));
        Assert.Equal(1, selection.ItemUnderCursor);
        Assert.True(selection.NavigateCursor(PangyaUiListNavigation.Previous));
        Assert.Equal(0, selection.ItemUnderCursor);
        Assert.False(selection.NavigateCursor(PangyaUiListNavigation.Previous));
        Assert.False(selection.NavigateCursor(PangyaUiListNavigation.RowDown, 0));
        Assert.Equal(4, selection.Current);
        Assert.Equal(new[] { 4 }, selection.SelectedItems);
        var preview = selection.ApplyTo(new PangyaUiListPreview(5, 2, (_, _, _) => { }));
        Assert.Equal(2, preview.TopRow);
        Assert.Equal(new[] { 0 }, preview.UnderCursorItems!);
        Assert.Throws<ArgumentOutOfRangeException>(() => selection.NavigateCursor((PangyaUiListNavigation)99));
        Assert.False(new PangyaUiListSelection(0).NavigateCursor(PangyaUiListNavigation.Next));
    }

    [Fact]
    public void UiListSelection_CursorFlagsSurviveReorderingAndAreRemovedWithItems()
    {
        var selection = new PangyaUiListSelection(3);
        var preview = new PangyaUiListPreview(3, 0, (_, _, _) => { });
        selection.SetCursorToFirstItem();
        selection.SetCursorToLastItem();
        PangyaUiListPreview original = selection.ApplyTo(preview);
        Assert.Equal(2, original.ItemUnderCursor);
        Assert.Equal(new[] { 0, 2 }, original.UnderCursorItems!.Order());
        selection.ReorderItems(new[] { 2, 0, 1 });
        Assert.Equal(0, selection.ItemUnderCursor);
        Assert.Equal(new[] { 0, 1 }, selection.ApplyTo(preview).UnderCursorItems!.Order());
        selection.DeleteItem(0);
        Assert.Null(selection.ItemUnderCursor);
        Assert.Equal(new[] { 0 }, selection.ApplyTo(preview).UnderCursorItems!.Order());
        selection.MouseEventsEnabled = false;
        selection.SetItemUnderCursor(null);
        PangyaUiListPreview cleared = selection.ApplyTo(preview);
        Assert.Empty(cleared.UnderCursorItems!);
        Assert.False(cleared.MouseEventsEnabled);
        Assert.Equal(new[] { 0, 2 }, Assert.IsAssignableFrom<IReadOnlySet<int>>(original.UnderCursorItems).Order());
        selection.Clear();
        selection.SetCursorToFirstItem();
        selection.SetCursorToLastItem();
        Assert.Null(selection.ItemUnderCursor);
    }

    [Fact]
    public void UiListSelection_StableSortPreservesSelectedItemIdentity()
    {
        var selection = new PangyaUiListSelection(4) { MultiSelect = true };
        selection.SelectItem(0);
        selection.SelectItem(3);
        int[] keys = [2, 1, 1, 0];
        IReadOnlyList<int> order = selection.SortItems((left, right) => keys[left].CompareTo(keys[right]));
        Assert.Equal(new[] { 3, 1, 2, 0 }, order);
        Assert.Equal(0, selection.Current);
        Assert.Equal(3, selection.Previous);
        Assert.Equal(new[] { 0, 3 }, selection.SelectedItems.Order());
        Assert.Throws<ArgumentException>(() => selection.ReorderItems(new[] { 0, 1, 1, 3 }));
        Assert.Equal(0, selection.Current);
        Assert.Equal(3, selection.Previous);
        Assert.Equal(new[] { 0, 3 }, selection.SelectedItems.Order());
    }

    [Fact]
    public void UiListSelection_MutationsSelectNeighborAndSynchronizeScroll()
    {
        var selection = new PangyaUiListSelection(0);
        var scroll = new PangyaUiScrollState(2);
        for (int index = 0; index < 4; index++) Assert.Equal(index, selection.AddItem(scroll));
        Assert.Equal(4, scroll.ItemCount);
        Assert.Equal(2, scroll.TopRow);
        selection.SelectItem(2);
        selection.DeleteItem(2, scroll);
        Assert.Equal(2, selection.Current);
        Assert.Contains(2, selection.SelectedItems);
        Assert.Null(selection.Previous);
        Assert.Equal(3, scroll.ItemCount);
        selection.DeleteItem(2, scroll);
        Assert.Equal(1, selection.Current);
        Assert.Equal(2, selection.ItemCount);
        selection.Clear(scroll);
        Assert.Equal(0, selection.ItemCount);
        Assert.Equal(0, scroll.ItemCount);
        Assert.Empty(selection.SelectedItems);
        Assert.Null(selection.Current);
        Assert.Null(selection.Previous);
    }

    [Fact]
    public void UiListSelection_MultiSelectDeletionRetainsOtherItemsAndRejectsMismatchedScroll()
    {
        var selection = new PangyaUiListSelection(4) { MultiSelect = true };
        selection.SelectItem(1);
        selection.SelectItem(3);
        selection.DeleteItem(3);
        Assert.Null(selection.Current);
        Assert.Equal(1, selection.Previous);
        Assert.Equal(new[] { 1 }, selection.SelectedItems);
        selection.DeleteItem(0);
        Assert.Equal(new[] { 0 }, selection.SelectedItems);
        Assert.Equal(0, selection.Previous);
        Assert.Throws<ArgumentException>(() => selection.AddItem(new PangyaUiScrollState(2)));
        Assert.Equal(2, selection.ItemCount);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(3, false)]
    public void UiRenderer_FrameRejectsBlinkWithoutInitializedBlinkSkin(int type, bool supplyBlinkImages)
    {
        for (int index = 0; index < 9; index++)
            SaveSolidImage(Path.Combine(_directory, $"blink-base{index:00}.png"), Color.White, 2, 2);
        if (supplyBlinkImages)
            for (int index = 0; index < 3; index++)
                SaveSolidImage(Path.Combine(_directory, $"blink-skin{index:00}.png"), Color.Red, 2, 2);
        string path = Path.Combine(_directory, "frame-blink-validation.xml");
        File.WriteAllText(path, $"""
            <resource>
              <element type="FRAME" name="skin" color="ffffffff">
                <layer type="{type}"/><bfrm filename="blink-base"/><cfrm filename="blink-skin"/>
              </element>
              <element type="FORM" name="form" size="40 30">
                <item type="FRAME" name="frame" resource="skin" rect="0 0 20 20"/>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode frame = Assert.Single(document.Nodes, node => node.Name == "frame");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load([path]));
        Assert.False(renderer.CanPreviewFrameBlink(frame, new HashSet<string>()));
        using var normal = new Bitmap(40, 30);
        using var requested = new Bitmap(40, 30);
        using Graphics first = Graphics.FromImage(normal);
        using Graphics second = Graphics.FromImage(requested);
        renderer.Render(first, document, form, new PangyaUiRenderOptions { IsViewFocused = false });
        renderer.Render(second, document, form, new PangyaUiRenderOptions { IsViewFocused = false, ShowFrameBlink = true });
        Assert.Equal(normal.GetPixel(1, 1).ToArgb(), requested.GetPixel(1, 1).ToArgb());
    }

    [Fact]
    public void UiRenderer_FormMessageResetsHiddenScrollingCaptionAndPreservesSmallFont()
    {
        string path = Path.Combine(_directory, "form-message-scrolling-setter.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 40">
              <item type="STATIC" name="message" caption="" rect="5 6 85 26">
                <param name="scrolling" var="true"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode label = Assert.Single(document.Nodes, node => node.Name == "message");
        var original = new PangyaUiRenderOptions
        {
            HiddenNodes = new HashSet<PangyaUiNode> { label },
            SmallFontCaptions = new HashSet<PangyaUiNode> { label },
            CaptionScrollOffsets = new Dictionary<PangyaUiNode, float> { [label] = 12 },
            CaptionClippingEnabled = new Dictionary<PangyaUiNode, bool> { [label] = false }
        };
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(1, 1);
        using Graphics graphics = Graphics.FromImage(bitmap);
        PangyaUiRenderOptions updated = renderer.SetFormMessage(graphics, form, new string('A', 110), options: original);
        Assert.Equal(new string('A', 100) + "...", updated.StaticCaptions![label]);
        Assert.Contains(label, updated.SmallFontCaptions!);
        Assert.Equal(0, updated.CaptionScrollOffsets![label]);
        Assert.True(updated.CaptionClippingEnabled![label]);
        Assert.Equal(new Rectangle(5, 6, 80, 20), updated.CaptionClips![label]);
        Assert.Equal(12, original.CaptionScrollOffsets[label]);
        Assert.False(original.CaptionClippingEnabled[label]);
    }

    [Fact]
    public void UiRenderer_StaticCaptionSmallFontPersistsAcrossNormalReplacement()
    {
        string path = Path.Combine(_directory, "static-caption-setter.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 40">
              <item type="STATIC" name="label" caption="Original" rect="0 0 80 20"/>
            </element></resource>
            """);
        PangyaUiNode label = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "label");
        string longCaption = new('A', 110);
        PangyaUiRenderOptions small = PangyaUiRenderer.SetStaticCaption(label, longCaption, smallFont: true);
        Assert.Equal(longCaption, small.StaticCaptions![label]);
        PangyaUiRenderOptions normal = PangyaUiRenderer.SetStaticCaption(label, longCaption, options: small);
        Assert.Equal(new string('A', 100) + "...", normal.StaticCaptions![label]);
        Assert.Contains(label, normal.SmallFontCaptions!);
        Assert.Equal(longCaption, small.StaticCaptions[label]);
        PangyaUiRenderOptions multibyte = PangyaUiRenderer.SetStaticCaption(label, new string('あ', 51));
        Assert.Equal(new string('あ', 50) + "...", multibyte.StaticCaptions![label]);
    }

    [Fact]
    public void UiRenderer_TextButtonCaptionCentersWithinRetainedGeometry()
    {
        string path = Path.Combine(_directory, "text-button-caption-geometry.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 40">
              <item type="TEXTBUTTON" name="button" caption="ABC" rect="10 0 90 20"/>
            </element></resource>
            """);
        PangyaUiNode button = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "button");
        var original = new PangyaUiRenderOptions { MeasureCaptionText = text => text.Length * 6 };
        PangyaUiRenderOptions shortCaption = PangyaUiRenderer.SetTextButtonCaption(button, "X", original);
        Assert.Equal(new Point(37, 0), shortCaption.WindowOffsets![button]);
        Assert.Equal(new Size(6, 20), shortCaption.WindowSizes![button]);
        PangyaUiRenderOptions longCaption = PangyaUiRenderer.SetTextButtonCaption(button, "XYZ", shortCaption);
        Assert.Equal(new Point(32, 0), longCaption.WindowOffsets![button]);
        PangyaUiRenderOptions repeated = PangyaUiRenderer.SetTextButtonCaption(button, "X", longCaption);
        Assert.Equal(new Point(38, 0), repeated.WindowOffsets![button]);
        Assert.Null(original.WindowOffsets);
        Assert.Equal(new Point(37, 0), shortCaption.WindowOffsets[button]);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        Assert.Equal(0, renderer.HitTestTextButtonSegment(button, new Point(48, 5), repeated));
        Assert.Equal(-1, renderer.HitTestTextButtonSegment(button, new Point(47, 5), repeated));
        Assert.Equal("X", renderer.GetTextButtonTextUnderCursor(button, new Point(48, 5), repeated));
        Assert.Null(renderer.GetTextButtonTextUnderCursor(button, new Point(47, 5), repeated));
    }

    [Fact]
    public void UiRenderer_TextButtonSelectionClampsAndCaptionReplacementClearsSelection()
    {
        string path = Path.Combine(_directory, "text-button-selection.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 40">
              <item type="TEXTBUTTON" name="button" caption="One|Two||Ignored" rect="0 0 80 20"/>
            </element></resource>
            """);
        PangyaUiNode button = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "button");
        PangyaUiRenderOptions first = PangyaUiRenderer.SelectTextButton(button, -10);
        Assert.Equal(0, first.MenuStates![button].SelectedRow);
        Assert.Equal("One", PangyaUiRenderer.GetSelectedTextButtonText(button, first));
        PangyaUiRenderOptions last = PangyaUiRenderer.SelectTextButton(button, int.MaxValue, options: first);
        Assert.Equal(1, last.MenuStates![button].SelectedRow);
        Assert.Equal("Two", PangyaUiRenderer.GetSelectedTextButtonText(button, last));
        Assert.Equal(0, first.MenuStates[button].SelectedRow);
        Assert.Same(last, PangyaUiRenderer.SelectTextButton(button, 0, false, last));
        PangyaUiRenderOptions cleared = PangyaUiRenderer.SelectTextButton(button, 100, false, last);
        Assert.Equal(-1, cleared.MenuStates![button].SelectedRow);
        PangyaUiRenderOptions replaced = PangyaUiRenderer.SetTextButtonCaption(button, "New", last);
        Assert.Equal("New", replaced.TextButtonCaptions![button]);
        Assert.Equal(-1, replaced.MenuStates![button].SelectedRow);
        Assert.Null(PangyaUiRenderer.GetSelectedTextButtonText(button, replaced));
        Assert.Equal(1, last.MenuStates[button].SelectedRow);
        PangyaUiRenderOptions empty = PangyaUiRenderer.SetTextButtonCaption(button, "", last);
        Assert.Same(empty, PangyaUiRenderer.SelectTextButton(button, 0, options: empty));
    }

    [Fact]
    public void UiRenderer_CaptionClipCanBeReplacedAndClearedWithoutMutatingPriorState()
    {
        string path = Path.Combine(_directory, "caption-clip-setter.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 40">
              <item type="TEXTBUTTON" name="button" caption="One" rect="0 0 80 20"/>
            </element></resource>
            """);
        PangyaUiNode button = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "button");
        var firstArea = new Rectangle(1, 2, 3, 4);
        PangyaUiRenderOptions first = PangyaUiRenderer.SetCaptionClippingArea(button, firstArea);
        PangyaUiRenderOptions replaced = PangyaUiRenderer.SetCaptionClippingArea(button, Rectangle.Empty, first);
        Assert.Equal(firstArea, first.CaptionClips![button]);
        Assert.Equal(Rectangle.Empty, replaced.CaptionClips![button]);
        Assert.True(replaced.CaptionClippingEnabled![button]);
        PangyaUiRenderOptions cleared = PangyaUiRenderer.SetCaptionClippingArea(button, null, replaced);
        Assert.False(cleared.CaptionClips!.ContainsKey(button));
        Assert.False(cleared.CaptionClippingEnabled![button]);
        Assert.True(replaced.CaptionClippingEnabled[button]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_GaugeArrowsUseIndependentChildAlpha(bool recursive)
    {
        SaveSolidImage(Path.Combine(_directory, "gauge-arrow.png"), Color.Lime, 4, 4);
        string path = Path.Combine(_directory, "gauge-child-alpha.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 20">
              <item type="GAUGEBAREX" name="gauge" rect="0 0 40 10">
                <param name="left" var="true"/><param name="right" var="true"/>
                <param name="normal" var="gauge-arrow.png"/><param name="rnormal" var="gauge-arrow.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode gauge = Assert.Single(document.Nodes, node => node.Name == "gauge");
        var options = new PangyaUiRenderOptions
        {
            WindowAlpha = new Dictionary<PangyaUiNode, float> { [gauge] = 0 },
            WindowAlpha2 = new Dictionary<PangyaUiNode, float> { [gauge] = 0 }
        };
        if (recursive) options = PangyaUiRenderer.SetWindowSecondaryAlphaToChildren(gauge, 0, options);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(40, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(!recursive, bitmap.GetPixel(3, 5).ToArgb() == Color.Lime.ToArgb());
        Assert.Equal(!recursive, bitmap.GetPixel(35, 5).ToArgb() == Color.Lime.ToArgb());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_ComboButtonUsesIndependentChildAlpha(bool recursive)
    {
        SaveSolidImage(Path.Combine(_directory, "combo-child.png"), Color.Lime, 20, 10);
        string path = Path.Combine(_directory, "combo-child-alpha.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="COMBOCTLEX" name="combo" rect="0 0 20 10"><param name="normal" var="combo-child.png"/></item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode combo = Assert.Single(document.Nodes, node => node.Name == "combo");
        var options = new PangyaUiRenderOptions
        {
            WindowAlpha = new Dictionary<PangyaUiNode, float> { [combo] = 0 },
            WindowAlpha2 = new Dictionary<PangyaUiNode, float> { [combo] = 0 }
        };
        if (recursive) options = PangyaUiRenderer.SetWindowSecondaryAlphaToChildren(combo, 0, options);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal((recursive ? Color.FromArgb(46, 49, 55) : Color.Lime).ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_GeneratedFormFrameUsesIndependentChildAlpha(bool recursive)
    {
        for (int index = 0; index < 3; index++)
            SaveSolidImage(Path.Combine(_directory, $"childframe{index:00}.png"), Color.Red, 2, 2);
        string path = Path.Combine(_directory, "form-child-alpha.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="FRAME" name="skin" color="ffffffff"><layer type="1"/><bfrm filename="childframe"/></element>
              <element type="FORM" name="form" resource="skin" size="20 20"/>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        var options = new PangyaUiRenderOptions
        {
            WindowAlpha = new Dictionary<PangyaUiNode, float> { [form] = 0 },
            WindowAlpha2 = new Dictionary<PangyaUiNode, float> { [form] = 0 }
        };
        if (recursive) options = PangyaUiRenderer.SetWindowSecondaryAlphaToChildren(form, 0, options);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.LoadEntry(path));
        using var bitmap = new Bitmap(20, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal((recursive ? Color.FromArgb(46, 49, 55) : Color.Red).ToArgb(), bitmap.GetPixel(1, 1).ToArgb());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_ScrollbarAlphaIsIndependentUnlessRecursivelyPropagated(bool recursive)
    {
        for (int index = 1; index <= 3; index++)
            SaveSolidImage(Path.Combine(_directory, $"scroll_n_{index}.png"), Color.Red, 4, 2);
        string path = Path.Combine(_directory, "scrollbar-child-alpha.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 40">
              <item type="VIEWER" name="viewer" rect="0 0 20 20"><param name="use_wheel" var="true"/></item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode viewer = Assert.Single(document.Nodes, node => node.Name == "viewer");
        var options = new PangyaUiRenderOptions
        {
            WindowAlpha = new Dictionary<PangyaUiNode, float> { [viewer] = 0 },
            WindowAlpha2 = new Dictionary<PangyaUiNode, float> { [viewer] = 0 }
        };
        if (recursive) options = PangyaUiRenderer.SetWindowSecondaryAlphaToChildren(viewer, 0, options);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(40, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal((recursive ? Color.FromArgb(46, 49, 55) : Color.Red).ToArgb(),
            bitmap.GetPixel(19, 1).ToArgb());
    }

    [Fact]
    public void UiListSelection_UnselectAllPreservesPreviousWhenNoCurrentItemExists()
    {
        var selection = new PangyaUiListSelection(3);
        selection.SelectItem(1);
        selection.SelectItem(null);
        Assert.Null(selection.Current);
        Assert.Equal(1, selection.Previous);
        selection.UnselectAll();
        Assert.Equal(1, selection.Previous);
        selection.SelectItem(2);
        selection.UnselectAll();
        Assert.Null(selection.Current);
        Assert.Null(selection.Previous);
    }

    [Fact]
    public void UiListSelection_CurrentSelectionScrollsByNativeHalfPages()
    {
        var scroll = new PangyaUiScrollState(4, 2) { FollowBottom = false };
        for (int index = 0; index < 30; index++) scroll.AddItem();
        var selection = new PangyaUiListSelection(30);
        selection.SelectItem(20, scroll);
        Assert.Equal(8, scroll.RoundedTopRow);
        selection.ToggleItem(20, scroll);
        Assert.Null(selection.Current);
        Assert.Equal(8, scroll.RoundedTopRow);
        selection.ToggleItem(20, scroll);
        Assert.Equal(20, selection.Current);
        selection.SelectItem(1, scroll);
        Assert.Equal(0, scroll.RoundedTopRow);
        Assert.False(scroll.FollowBottom);
        selection.SelectItem(29, scroll, makeCurrent: false);
        Assert.Equal(0, scroll.RoundedTopRow);
        Assert.Equal(1, selection.Current);
        var oneRow = new PangyaUiScrollState(1, 2) { FollowBottom = false };
        for (int index = 0; index < 30; index++) oneRow.AddItem();
        selection.SelectItem(20, oneRow);
        Assert.Equal(0, oneRow.RoundedTopRow);
    }

    [Fact]
    public void UiListSelection_PreservesNativeCurrentPreviousAndIndependentFlags()
    {
        var selection = new PangyaUiListSelection(3);
        selection.SelectItem(0);
        selection.SelectItem(1, makeCurrent: false);
        Assert.Equal(0, selection.Current);
        Assert.Equal(new[] { 1 }, selection.SelectedItems);
        selection.MultiSelect = true;
        selection.SelectItem(2);
        Assert.Equal(0, selection.Previous);
        Assert.Equal(2, selection.Current);
        Assert.True(selection.SelectedItems.SetEquals(new[] { 1, 2 }));
        var snapshot = selection.ApplyTo(new PangyaUiListPreview(3, 0, (_, _, _) => { }));
        selection.ToggleItem(2);
        Assert.Null(selection.Current);
        Assert.Null(selection.Previous);
        Assert.True(snapshot.SelectedItems!.SetEquals(new[] { 1, 2 }));
        Assert.Equal(new[] { 1 }, selection.SelectedItems);
        selection.UnselectAll();
        Assert.Empty(selection.SelectedItems);
    }

    [Fact]
    public void UiRenderer_StaticFontClipDoesNotClipEmoticonOverlayOrLeakAfterDrawing()
    {
        SaveSolidImage(Path.Combine(_directory, "emoticon.png"), Color.Red, 26, 24);
        string emoticons = Path.Combine(_directory, "emoticon.xml");
        File.WriteAllText(emoticons, "<emoticon><def name=\"face\"/></emoticon>");
        string path = Path.Combine(_directory, "static-emoticon-clip.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="STATIC" name="label" caption="(face)X" rect="0 0 40 24"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode label = Assert.Single(document.Nodes, node => node.Name == "label");
        var textClips = new List<bool>();
        var options = new PangyaUiRenderOptions
        {
            CaptionFontHeight = 24,
            CaptionClips = new Dictionary<PangyaUiNode, Rectangle> { [label] = new(0, 0, 3, 24) },
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (graphics, _, _, _) => textClips.Add(graphics.VisibleClipBounds.Contains(20, 10))
        };
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load(new[] { path, emoticons }));
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(20, 10).ToArgb());
        Assert.NotEmpty(textClips);
        Assert.All(textClips, clipped => Assert.False(clipped));
        Assert.True(graphics.VisibleClipBounds.Contains(20, 10));
    }

    [Fact]
    public void UiScrollState_DragRetainsFractionalRowsUntilNativeRecalculation()
    {
        var scroll = new PangyaUiScrollState(2);
        for (int index = 0; index < 6; index++) scroll.AddItem();
        scroll.DragToRow(0.5f);
        Assert.Equal(0.5f, scroll.TopRow);
        Assert.Equal(1, scroll.RoundedTopRow);
        Assert.False(scroll.FollowBottom);
        scroll.AddItem();
        Assert.Equal(0f, scroll.TopRow);
        scroll.DragToRow(100);
        Assert.Equal(5f, scroll.TopRow);
        Assert.True(scroll.FollowBottom);
    }

    [Fact]
    public void UiScrollState_ItemMutationsAndEndScrollingMatchNativeFollowMode()
    {
        var scroll = new PangyaUiScrollState(2, 2);
        for (int index = 0; index < 7; index++) scroll.AddItem();
        Assert.Equal(4, scroll.RowCount);
        Assert.Equal(2, scroll.RoundedTopRow);
        scroll.ScrollUp();
        Assert.Equal(1, scroll.RoundedTopRow);
        Assert.False(scroll.FollowBottom);
        scroll.AddItem();
        Assert.Equal(1, scroll.RoundedTopRow);
        scroll.ScrollDown();
        Assert.True(scroll.FollowBottom);
        Assert.Equal(2, scroll.RoundedTopRow);
        scroll.DeleteItem();
        scroll.DeleteItem();
        Assert.Equal(1, scroll.RoundedTopRow);
        scroll.Clear();
        Assert.True(scroll.FollowBottom);
        Assert.Equal(0, scroll.ItemCount);
        scroll.ScrollToFirst();
        Assert.False(scroll.FollowBottom);
    }

    [Fact]
    public void UiResourceCatalog_EntryScopePreservesIncludedConditionsWithoutStandaloneLeakage()
    {
        string entry = Path.Combine(_directory, "conditional-entry.xml");
        string variant = Path.Combine(_directory, "variant.xml");
        File.WriteAllText(entry, """
            <resource>
              <!-- #ifdef JP -->
              <element type="INCLUDE" name="variant.xml"/>
              <!-- #endif -->
            </resource>
            """);
        File.WriteAllText(variant, """
            <resource>
              <!-- #ifdef EXTRA -->
              <element type="MACROITEM" name="Variant"/>
              <!-- #endif -->
            </resource>
            """);
        var catalog = PangyaUiResourceCatalog.LoadEntry(entry, new[] { entry, variant });
        Assert.Contains("JP", catalog.ConditionalSymbols);
        Assert.Contains("EXTRA", catalog.ConditionalSymbols);
        var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "EXTRA" };
        Assert.Null(catalog.TryResolve("Variant", "MACROITEM", symbols));
        symbols.Add("JP");
        Assert.NotNull(catalog.TryResolve("Variant", "MACROITEM", symbols));
    }

    [Fact]
    public void UiResourceCatalog_DoesNotResolveUnrelatedDefinitionTypesWithSameName()
    {
        string path = Path.Combine(_directory, "typed-resources.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="MACROITEM" name="Shared"/>
              <element type="FRAME" name="Skin"/>
              <element type="BITMAP" name="Image" filename="image.png"/>
            </resource>
            """);
        var catalog = PangyaUiResourceCatalog.LoadEntry(path);
        var symbols = new HashSet<string>();
        Assert.Null(catalog.TryResolve("Shared", "FRAME", symbols));
        Assert.Null(catalog.TryResolve("Shared", "FORM", symbols));
        Assert.Null(catalog.TryResolve("Shared", "AREA", symbols));
        Assert.Null(catalog.TryResolve("Shared", "BUTTON", symbols));
        Assert.Equal("FRAME", catalog.TryResolve("Skin", "CONTEXTMENU", symbols)?.Type);
        Assert.Equal("BITMAP", catalog.TryResolve("Image", "AREA", symbols)?.Type);
        Assert.Equal("BITMAP", catalog.TryResolve("Image", "BUTTON", symbols)?.Type);
    }

    [Theory]
    [InlineData(PangyaUiButtonState.Normal, -65536)]
    [InlineData(PangyaUiButtonState.Hover, -13749961)]
    [InlineData(PangyaUiButtonState.Selected, -16776961)]
    public void UiRenderer_ExternalButtonUsesCallerBitmapsAndSuppressesNullXmlFallback(
        PangyaUiButtonState state, int expectedColor)
    {
        SaveSolidImage(Path.Combine(_directory, "fallback.png"), Color.Lime, 20, 20);
        string path = Path.Combine(_directory, "external-button.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="BUTTON" name="button" pos="2 3">
                <param name="normal" var="fallback.png"/><param name="over" var="fallback.png"/>
                <param name="selected" var="fallback.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode button = Assert.Single(document.Nodes, node => node.Name == "button");
        using var normal = new Bitmap(12, 8);
        using (Graphics fill = Graphics.FromImage(normal)) fill.Clear(Color.Red);
        using var selected = new Bitmap(4, 4);
        using (Graphics fill = Graphics.FromImage(selected)) fill.Clear(Color.Blue);
        var options = PangyaUiRenderer.InitializeExternalButton(button, normal, null, selected, 2, 3,
            new PangyaUiRenderOptions(ButtonState: state));
        Assert.Equal(3, PangyaUiRenderer.GetButtonPushStyle(button, options));
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        Assert.Equal(new Rectangle(2, 3, 12, 8), renderer.GetRenderedBounds(button, options));
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(expectedColor, bitmap.GetPixel(6, 6).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), normal.GetPixel(0, 0).ToArgb());
    }

    [Fact]
    public void UiResourceCatalog_EntryScopeExcludesUnrelatedAvailableFiles()
    {
        string entry = Path.Combine(_directory, "entry.xml");
        string common = Path.Combine(_directory, "common.xml");
        string unrelated = Path.Combine(_directory, "a-unrelated.xml");
        File.WriteAllText(entry, "<resource><element type=\"INCLUDE\" name=\"common.xml\"/></resource>");
        File.WriteAllText(common, "<resource><element type=\"MACROITEM\" name=\"Shared\"><item type=\"AREA\" name=\"included\"/></element></resource>");
        File.WriteAllText(unrelated, """
            <resource>
              <element type="MACROITEM" name="Shared"><item type="AREA" name="unrelated"/></element>
              <element type="MACROITEM" name="Outside"/>
            </resource>
            """);
        var catalog = PangyaUiResourceCatalog.LoadEntry(entry, new[] { unrelated, common, entry }, _directory);
        var symbols = new HashSet<string>();
        PangyaUiNode? definition = catalog.TryResolve("Shared", "MACROITEM", symbols);
        Assert.NotNull(definition);
        Assert.Equal("included", Assert.Single(definition.Children).Name);
        Assert.Equal(common, catalog.GetSourcePath(definition));
        Assert.Null(catalog.TryResolve("Outside", "MACROITEM", symbols));
        File.WriteAllText(common, "<resource><element type=\"MACROITEM\" name=\"Shared\"><item type=\"AREA\" name=\"saved-edit\"/></element></resource>");
        var refreshed = PangyaUiResourceCatalog.LoadEntry(entry, new[] { unrelated, common, entry }, _directory);
        Assert.Equal("saved-edit", Assert.Single(refreshed.TryResolve("Shared", "MACROITEM", symbols)!.Children).Name);
        Assert.Equal("included", Assert.Single(definition.Children).Name);
    }

    [Fact]
    public void UiResourceCatalog_ResourceNamesAreExactAndSameTypeDuplicatesUseFirstDefinition()
    {
        string path = Path.Combine(_directory, "exact-resource-names.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="MACROITEM" name="Shared"><item type="AREA" name="upper"/></element>
              <element type="MACROITEM" name="shared"><item type="AREA" name="lower"/></element>
              <element type="MACROITEM" name="Shared"><item type="AREA" name="later"/></element>
              <element type="MACROITEM" name=" Shared "><item type="AREA" name="spaced"/></element>
            </resource>
            """);
        var catalog = PangyaUiResourceCatalog.Load(new[] { path });
        var symbols = new HashSet<string>();
        Assert.Equal("upper", Assert.Single(catalog.TryResolve("Shared", "MACROITEM", symbols)!.Children).Name);
        Assert.Equal("lower", Assert.Single(catalog.TryResolve("shared", "MACROITEM", symbols)!.Children).Name);
        Assert.Equal("spaced", Assert.Single(catalog.TryResolve(" Shared ", "MACROITEM", symbols)!.Children).Name);
        Assert.Null(catalog.TryResolve("SHARED", "MACROITEM", symbols));
    }

    [Fact]
    public void UiResourceCatalog_FollowsUnlistedNestedIncludesInPlaceAndStopsCycles()
    {
        string shared = Path.Combine(_directory, "shared");
        Directory.CreateDirectory(shared);
        string entry = Path.Combine(_directory, "entry.xml");
        File.WriteAllText(entry, """
            <resource>
              <element type="INCLUDE" name="shared/common.xml"/>
              <element type="MACROITEM" name="duplicate"><item type="AREA" name="later"/></element>
            </resource>
            """);
        File.WriteAllText(Path.Combine(shared, "common.xml"), """
            <resource>
              <element type="INCLUDE" name="../entry.xml"/>
              <element type="MACROITEM" name="duplicate"><item type="AREA" name="included"/></element>
            </resource>
            """);
        PangyaUiResourceCatalog catalog = PangyaUiResourceCatalog.Load(new[] { entry });
        PangyaUiNode? resolved = catalog.TryResolve("duplicate", "MACROITEM", new HashSet<string>());
        Assert.NotNull(resolved);
        Assert.Equal("included", Assert.Single(resolved.Children).Name);
        Assert.Equal(Path.Combine(shared, "common.xml"), catalog.GetSourcePath(resolved));
    }

    [Fact]
    public void UiRenderer_TabOwnerDrawUsesAccumulatedWidthAndFirstTabHeight()
    {
        string path = Path.Combine(_directory, "tab-owner-size.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 100">
              <item type="TABBUTTON" name="tabs" rect="2 3 92 93"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode tabs = Assert.Single(document.Nodes, node => node.Name == "tabs");
        var bars = new List<Rectangle>();
        var options = new PangyaUiRenderOptions
        {
            Tabs = new Dictionary<PangyaUiNode, IReadOnlyList<PangyaUiTab>>
                { [tabs] = new[] { new PangyaUiTab("", Size: new Size(10, 8)), new PangyaUiTab("", Size: new Size(20, 16)) } },
            TabSeparatorWidths = new Dictionary<PangyaUiNode, float> { [tabs] = 3 },
            TabOwnerDraw = new Dictionary<PangyaUiNode, Action<Graphics, int?, Rectangle>>
                { [tabs] = (_, index, bounds) => { if (index is null) bars.Add(bounds); } }
        };
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(100, 100);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(new Rectangle(2, 3, 33, 8), Assert.Single(bars));
        bars.Clear();
        renderer.Render(graphics, document, form, options with
            { Tabs = new Dictionary<PangyaUiNode, IReadOnlyList<PangyaUiTab>> { [tabs] = Array.Empty<PangyaUiTab>() } });
        Assert.Equal(new Rectangle(2, 3, 0, 0), Assert.Single(bars));
    }

    [Theory]
    [InlineData("STATIC", false)]
    [InlineData("STATIC", true)]
    [InlineData("TEXTBUTTON", false)]
    public void UiRenderer_CaptionClippingCanBeDisabledWithoutRemovingRectangle(string type, bool smallFont)
    {
        string path = Path.Combine(_directory, "caption-clip-switch.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="30 30">
              <item type="{type}" name="caption" caption="X" rect="0 0 20 20"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode caption = Assert.Single(document.Nodes, node => node.Name == "caption");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var visible = new List<bool>();
        var options = new PangyaUiRenderOptions
        {
            CaptionClips = new Dictionary<PangyaUiNode, Rectangle> { [caption] = new(1, 1, 2, 2) },
            SmallFontCaptions = smallFont ? new HashSet<PangyaUiNode> { caption } : null,
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (graphics, _, _, _) => visible.Add(graphics.VisibleClipBounds.Contains(8, 8)),
            DrawTooltipText = (graphics, _, _, _) => visible.Add(graphics.VisibleClipBounds.Contains(8, 8))
        };
        using var bitmap = new Bitmap(30, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.NotEmpty(visible);
        Assert.All(visible, value => Assert.Equal(smallFont, value));
        visible.Clear();
        renderer.Render(graphics, document, form, options with
            { CaptionClippingEnabled = new Dictionary<PangyaUiNode, bool> { [caption] = false } });
        Assert.NotEmpty(visible);
        Assert.All(visible, value => Assert.True(value));
    }

    [Fact]
    public void UiRenderer_ImageComboDrawsCompactTextureRowsAndRetainsFoldedImageAfterClear()
    {
        SaveSolidImage(Path.Combine(_directory, "skin.png"), Color.Lime, 20, 10);
        SaveSolidImage(Path.Combine(_directory, "icon.png"), Color.Red, 4, 4);
        string path = Path.Combine(_directory, "combo-texture-render.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 120">
              <item type="COMBOCTLEX" name="combo" rect="0 0 20 10">
                <param name="normal" var="skin.png"/><param name="liststyle" var="1"/>
                <param name="leftmargin" var="4"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode combo = Assert.Single(document.Nodes, node => node.Name == "combo");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            Combos = new Dictionary<PangyaUiNode, PangyaUiComboPreview> { [combo] = new([], IsOpen: true) }
        };
        options = renderer.AddComboTexture(combo, "missing.png", options);
        options = renderer.AddComboTexture(combo, "icon.png", options);
        using var bitmap = new Bitmap(40, 120);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(3, 13).ToArgb());
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(3, 23).ToArgb());
        options = renderer.SelectComboImage(combo, 0, options);
        options = PangyaUiRenderer.ClearComboImageList(combo, options);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(3, 3).ToArgb());
        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(3, 13).ToArgb());
    }

    [Fact]
    public void UiRenderer_AddComboTextureKeepsRowsForFailedImagesAndNumberingAfterClear()
    {
        SaveSolidImage(Path.Combine(_directory, "texture.png"), Color.Red, 4, 4);
        string path = Path.Combine(_directory, "add-combo-texture.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form"><item type="COMBOCTLEX" name="combo">
              <param name="liststyle" var="1"/>
            </item></element></resource>
            """);
        PangyaUiNode combo = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "combo");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = renderer.AddComboTexture(combo, "missing.png", new());
        Assert.Equal(new PangyaUiComboItem("1"), Assert.Single(options.Combos![combo].Items));
        options = renderer.AddComboTexture(combo, "texture.png", options);
        Assert.Equal(new PangyaUiComboItem("2", "texture.png"), options.Combos![combo].Items[1]);
        Assert.Equal("texture.png", Assert.Single(options.Combos[combo].TextureImages!));
        var selected = renderer.SelectComboImage(combo, 0, options);
        Assert.Equal(new PangyaUiComboImageSelection("texture.png", 0), selected.Combos![combo].ImageSelection);
        options = PangyaUiRenderer.ClearComboImageList(combo, options);
        Assert.Empty(options.Combos![combo].TextureImages!);
        options = renderer.AddComboTexture(combo, "texture.png", options);
        Assert.Equal(new PangyaUiComboItem("3", "texture.png"), Assert.Single(options.Combos![combo].Items));
        Assert.Equal(3, options.Combos[combo].StoredTextRowCount);
        Assert.Single(options.Combos[combo].TextureImages!);
    }

    [Fact]
    public void UiRenderer_ImageComboSelectionSkipsFailedTexturesAndUsesByteIndex()
    {
        SaveSolidImage(Path.Combine(_directory, "texture.png"), Color.Red, 4, 4);
        string path = Path.Combine(_directory, "image-combo-select.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form"><item type="COMBOCTLEX" name="combo">
              <param name="liststyle" var="1"/>
            </item></element></resource>
            """);
        PangyaUiNode combo = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "combo");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var items = new[] { new PangyaUiComboItem("missing", "missing.png") }
            .Concat(Enumerable.Range(0, 260).Select(index => new PangyaUiComboItem(index.ToString(), "texture.png"))).ToArray();
        var original = new PangyaUiRenderOptions
        {
            Combos = new Dictionary<PangyaUiNode, PangyaUiComboPreview> { [combo] = new(items) }
        };
        var selected = renderer.SelectComboImage(combo, 259, original);
        Assert.Equal(new PangyaUiComboImageSelection("texture.png", 3), selected.Combos![combo].ImageSelection);
        Assert.Equal(-1, selected.Combos[combo].SelectedIndex);
        var invalid = renderer.SelectComboImage(combo, 260, selected);
        Assert.Equal(new PangyaUiComboImageSelection(null, 0), invalid.Combos![combo].ImageSelection);
        Assert.Null(original.Combos![combo].ImageSelection);
    }

    [Fact]
    public void UiRenderer_ClearImageComboListRetainsSelectedTextureAndIndex()
    {
        string path = Path.Combine(_directory, "image-combo-clear.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form"><item type="COMBOCTLEX" name="combo">
              <param name="liststyle" var="1"/>
            </item></element></resource>
            """);
        PangyaUiNode combo = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "combo");
        var original = new PangyaUiRenderOptions
        {
            Combos = new Dictionary<PangyaUiNode, PangyaUiComboPreview>
            {
                [combo] = new(new[] { new PangyaUiComboItem("1", "one.png"),
                    new PangyaUiComboItem("2", "two.png") }, SelectedIndex: 1)
            }
        };
        var cleared = PangyaUiRenderer.ClearComboImageList(combo, original);
        Assert.Empty(cleared.Combos![combo].Items);
        Assert.Equal(new PangyaUiComboImageSelection("two.png", 1), cleared.Combos[combo].ImageSelection);
        Assert.Equal(2, original.Combos![combo].Items.Count);
    }

    [Fact]
    public void UiComboList_MutationsPreserveNativeIndicesAndDisplayedText()
    {
        var model = new PangyaUiComboList(characterLimit: 3);
        model.AddString("AあB");
        model.AddString("Two");
        model.AddString("Two");
        Assert.Equal("Aあ", model.Items[0].Text);
        Assert.Equal(2, model.FindString("Two"));
        Assert.Equal(0, model.FindString("two"));
        Assert.True(model.SelectRow(2));
        model.DeleteString(0);
        Assert.Equal(1, model.SelectedIndex);
        Assert.True(model.IsSelectedString("Two"));
        model.DeleteString(1);
        Assert.Equal(-1, model.SelectedIndex);
        Assert.Equal("Two", model.DisplayedText);
        Assert.False(model.IsSelectedString("Two"));
        model.IncreaseMaximumRows(20);
        model.IncreaseMaximumRows(5);
        Assert.Equal(20, model.MaximumRows);
        model.ClearList();
        Assert.Empty(model.Items);
        Assert.Equal("Two", model.DisplayedText);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(2.0)]
    [InlineData(-1.0)]
    public void UiRenderer_ButtonPushDelaySetterResetsReadyTime(double delay)
    {
        string path = Path.Combine(_directory, "button-delay-setter.xml");
        File.WriteAllText(path, "<resource><element type=\"FORM\" name=\"form\"><item type=\"BUTTON\" name=\"button\"/></element></resource>");
        PangyaUiNode button = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "button");
        var original = new PangyaUiRenderOptions
        {
            ButtonPressTicks = new Dictionary<PangyaUiNode, long> { [button] = 123 },
            ImageButtonStates = new Dictionary<PangyaUiNode, PangyaUiImageButtonState>
                { [button] = new(SecondsSincePress: 0) { Status = PangyaUiButtonState.Selected } }
        };
        var changed = PangyaUiRenderer.SetButtonPushDelay(button, delay, original);
        Assert.Equal(delay, PangyaUiRenderer.GetButtonPushDelay(button, changed));
        Assert.Equal(delay + 0.1, changed.ImageButtonStates![button].SecondsSincePress);
        Assert.Equal(PangyaUiButtonState.Selected, changed.ImageButtonStates[button].Status);
        Assert.Equal(0, original.ImageButtonStates![button].SecondsSincePress);
        Assert.False(changed.ButtonPressTicks!.ContainsKey(button));
        Assert.Equal(123, original.ButtonPressTicks![button]);
    }

    [Fact]
    public void UiRenderer_ButtonImageSettersResizeOnlySuccessfulMainSlots()
    {
        SaveSolidImage(Path.Combine(_directory, "main.png"), Color.Red, 14, 9);
        SaveSolidImage(Path.Combine(_directory, "below.png"), Color.Blue, 30, 20);
        string path = Path.Combine(_directory, "button-image-setters.xml");
        File.WriteAllText(path, "<resource><element type=\"FORM\" name=\"form\"><item type=\"BUTTON\" name=\"button\" pos=\"2 3\"/></element></resource>");
        PangyaUiNode button = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "button");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var original = new PangyaUiRenderOptions
            { WindowSizes = new Dictionary<PangyaUiNode, Size> { [button] = new(40, 40) } };
        Assert.True(renderer.TrySetButtonImage(button, "main.png", "over", original, out var main));
        Assert.Equal(new Size(14, 9), main.WindowSizes![button]);
        Assert.Equal("main.png", main.ImageButtonStates![button].Images!["over"]);
        Assert.Equal(new Size(40, 40), original.WindowSizes[button]);
        Assert.False(renderer.TrySetButtonImage(button, "missing.png", "normal", main, out var failed));
        Assert.Same(main, failed);
        Assert.True(renderer.TrySetButtonBelowImage(button, "below.png", "normal", main, out var below));
        Assert.Equal(new Size(14, 9), below.WindowSizes![button]);
        Assert.True(renderer.TrySetButtonBelowImage(button, null, "normal", below, out var cleared));
        Assert.Null(cleared.ImageButtonStates![button].BelowImages!["normal"]);
        Assert.False(renderer.TrySetButtonImage(button, null, "normal", cleared, out var nullMain));
        Assert.Same(cleared, nullMain);
    }

    [Theory]
    [InlineData(true, PangyaUiButtonState.Hover, PangyaUiButtonState.Normal)]
    [InlineData(false, PangyaUiButtonState.Hover, PangyaUiButtonState.Normal)]
    [InlineData(false, PangyaUiButtonState.Selected, PangyaUiButtonState.Selected)]
    public void UiRenderer_ButtonEnableSynchronizesRuntimeStateAndClearsHover(
        bool enabled, PangyaUiButtonState initial, PangyaUiButtonState expected)
    {
        string path = Path.Combine(_directory, "button-enable.xml");
        File.WriteAllText(path, "<resource><element type=\"FORM\" name=\"form\"><item type=\"BUTTON\" name=\"button\"/></element></resource>");
        PangyaUiNode button = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "button");
        var state = new PangyaUiImageButtonState(Enabled: true) { Status = initial, Style = 3 };
        var original = new PangyaUiRenderOptions
            { ImageButtonStates = new Dictionary<PangyaUiNode, PangyaUiImageButtonState> { [button] = state } };
        var changed = PangyaUiRenderer.SetWindowEnabled(button, enabled, original);
        Assert.Equal(!enabled, changed.DisabledNodes!.Contains(button));
        Assert.Equal(enabled, changed.ImageButtonStates![button].Enabled);
        Assert.Equal(expected, changed.ImageButtonStates[button].Status);
        Assert.Equal(3, changed.ImageButtonStates[button].Style);
        Assert.Same(state, original.ImageButtonStates![button]);
    }

    [Fact]
    public void UiRenderer_ShowingContextMenuReenablesItemsAndRetainsSelectionAndText()
    {
        string path = Path.Combine(_directory, "menu-visibility.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form"><item type="CONTEXTMENU" name="menu" caption="One|Two"/></element></resource>
            """);
        PangyaUiNode menu = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "menu");
        var state = new PangyaUiMenuState(1, new HashSet<int> { 0 })
            { DisabledTexts = new HashSet<string> { "Two" }, TextReplacements = new Dictionary<string, string> { ["One"] = "First" } };
        var original = new PangyaUiRenderOptions
            { MenuStates = new Dictionary<PangyaUiNode, PangyaUiMenuState> { [menu] = state } };
        var hidden = PangyaUiRenderer.SetWindowVisible(menu, false, original);
        Assert.Contains(menu, hidden.HiddenNodes!);
        Assert.Same(state, hidden.MenuStates![menu]);
        var shown = PangyaUiRenderer.SetWindowVisible(menu, true, hidden);
        Assert.DoesNotContain(menu, shown.HiddenNodes!);
        Assert.Contains(menu, shown.VisibleNodes!);
        Assert.Null(shown.MenuStates![menu].DisabledRows);
        Assert.Null(shown.MenuStates[menu].DisabledTexts);
        Assert.Equal(1, shown.MenuStates[menu].SelectedRow);
        Assert.Same(state.TextReplacements, shown.MenuStates[menu].TextReplacements);
        Assert.NotNull(original.MenuStates![menu].DisabledRows);
    }

    [Fact]
    public void UiRenderer_TextButtonKeepsSelectedAlphaIndependentFromWindowAlpha()
    {
        string path = Path.Combine(_directory, "text-button-window-alpha.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 40">
              <item type="TEXTBUTTON" name="button" caption="X|Y" rect="0 0 80 20">
                <param name="color" var="50ffffff"/><param name="select_color" var="50ffffff"/>
                <param name="style" var="0"/><param name="sel_style" var="0"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode button = Assert.Single(document.Nodes, node => node.Name == "button");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawn = new List<(string Text, int Alpha)>();
        var options = new PangyaUiRenderOptions
        {
            MenuStates = new Dictionary<PangyaUiNode, PangyaUiMenuState> { [button] = new(SelectedRow: 0) },
            WindowAlpha = new Dictionary<PangyaUiNode, float> { [button] = 0.5f },
            WindowAlpha2 = new Dictionary<PangyaUiNode, float> { [button] = 0 },
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, text, brush, _) => drawn.Add((text, ((SolidBrush)brush).Color.A))
        };
        using var bitmap = new Bitmap(100, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(new[] { ("X", 80), ("|", 40), ("Y", 40) }, drawn);
    }

    [Theory]
    [InlineData("EDIT")]
    [InlineData("FRAME")]
    public void UiRenderer_TranslucentBackgroundMultipliesSourceAlphaBeforeRounding(string type)
    {
        for (int index = 0; index < 3; index++)
            SaveSolidImage(Path.Combine(_directory, $"precision{index:00}.png"), Color.Red, 2, 2);
        string path = Path.Combine(_directory, "edit-alpha-precision.xml");
        File.WriteAllText(path, $"""
            <resource>
            <element type="FRAME" name="skin" color="50ffffff">
              <layer type="1"/><bfrm filename="precision"/>
            </element>
            <element type="FORM" name="form" size="20 20">
              <item type="{type}" name="edit" resource="skin" rect="0 0 10 10">
                <param name="bgcolor" var="50ff0000"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode edit = Assert.Single(document.Nodes, node => node.Name == "edit");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load(new[] { path }));
        using var actual = new Bitmap(20, 20);
        using Graphics graphics = Graphics.FromImage(actual);
        renderer.Render(graphics, document, form, new PangyaUiRenderOptions
            { WindowAlpha = new Dictionary<PangyaUiNode, float> { [edit] = 0.5f } });
        using var expected = new Bitmap(20, 20);
        using Graphics expectedGraphics = Graphics.FromImage(expected);
        expectedGraphics.Clear(Color.FromArgb(46, 49, 55));
        using var brush = new SolidBrush(Color.FromArgb(40, Color.Red));
        expectedGraphics.FillRectangle(brush, 0, 0, 10, 10);
        Assert.Equal(expected.GetPixel(1, 1).ToArgb(), actual.GetPixel(1, 1).ToArgb());
    }

    [Theory]
    [InlineData("AREA", 46)]
    [InlineData("VIEWER", 150)]
    public void UiRenderer_WindowAlphaChannelsFollowControlSpecificRules(string type, int expectedRed)
    {
        SaveSolidImage(Path.Combine(_directory, "alpha.png"), Color.Red, 10, 10);
        string path = Path.Combine(_directory, "window-alpha.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="20 20">
              <item type="{type}" name="image" rect="0 0 10 10">
                <param name="bgimg" var="alpha.png"/><param name="image" var="alpha.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode image = Assert.Single(document.Nodes, node => node.Name == "image");
        var options = PangyaUiRenderer.SetWindowSecondaryAlphaToChildren(form, 0,
            new PangyaUiRenderOptions { WindowAlpha = new Dictionary<PangyaUiNode, float> { [image] = 0.5f } });
        Assert.Equal(0f, options.WindowAlpha2![image]);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(20, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.InRange((int)bitmap.GetPixel(5, 5).R, expectedRed - 1, expectedRed + 1);
    }

    [Fact]
    public void UiRenderer_MoveCursorUsesDirectChildRuntimeBoundsAndModalGuard()
    {
        string path = Path.Combine(_directory, "move-cursor.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" pos="10 20" size="100 100">
              <item type="AREA" name="target" rect="4 6 24 16"/>
              <item type="GROUPBOX" name="group"><item type="AREA" name="nested"/></item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode target = Assert.Single(document.Nodes, node => node.Name == "target");
        PangyaUiNode group = Assert.Single(document.Nodes, node => node.Name == "group");
        var options = new PangyaUiRenderOptions
        {
            CursorPreview = new(new Point(1, 2), 3, 2),
            WindowOffsets = new Dictionary<PangyaUiNode, Point> { [target] = new(2, 4) }
        };
        Assert.True(PangyaUiRenderer.TryMoveCursorToChild(form, "target", options, out var moved));
        Assert.Equal(new Point(26, 35), moved.Pointer);
        Assert.Equal(new PangyaUiCursorPreview(new Point(26, 35), 3, 2), moved.CursorPreview);
        Assert.False(PangyaUiRenderer.TryMoveCursorToChild(form, "nested", options, out _));
        Assert.False(PangyaUiRenderer.TryMoveCursorToChild(form, "Target", options, out _));
        Assert.False(PangyaUiRenderer.TryMoveCursorToChild(form, "target", options, out var blocked, group));
        Assert.Same(options, blocked);
        Assert.True(PangyaUiRenderer.TryMoveCursorToChild(form, "target", options, out _, form));
    }

    [Fact]
    public void UiRenderer_AreaImageSettersKeepNativeRememberedNameAndNullSemantics()
    {
        SaveSolidImage(Path.Combine(_directory, "area.png"), Color.Red, 4, 4);
        string path = Path.Combine(_directory, "area-image-setters.xml");
        File.WriteAllText(path, "<resource><element type=\"FORM\" name=\"form\"><item type=\"AREA\" name=\"area\" normal=\"area.png\"/></element></resource>");
        PangyaUiNode area = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "area");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        Assert.True(renderer.TrySetAreaImage(area, "area.png", new(), out var loaded));
        Assert.Equal("area.png", loaded.AreaImages![area]);
        Assert.True(renderer.TrySetAreaImage(area, null, loaded, out var cleared));
        Assert.Null(cleared.AreaImages![area]);
        Assert.Equal("area.png", cleared.AreaImageNames![area]);
        Assert.True(renderer.TrySetAreaImage(area, "AREA.PNG", cleared, out var sameName));
        Assert.Same(cleared, sameName);
        Assert.False(PangyaUiRenderer.TrySetAreaBitmap(area, null, loaded, out var nullBitmap));
        Assert.Same(loaded, nullBitmap);
        using var bitmap = new Bitmap(4, 4);
        Assert.True(PangyaUiRenderer.TrySetAreaBitmap(area, bitmap, loaded, out var bitmapOptions));
        Assert.False(renderer.TrySetAreaImage(area, "missing.png", bitmapOptions, out var failed));
        Assert.Empty(failed.AreaBitmapImages!);
        Assert.Equal("missing.png", failed.AreaImageNames![area]);
        Assert.True(renderer.TrySetAreaImage(area, "missing.png", failed, out var repeated));
        Assert.Same(failed, repeated);
    }

    [Theory]
    [InlineData(80, 70, 40, 40, -20, -10)]
    [InlineData(-10, -20, 40, 40, 0, 0)]
    [InlineData(0, 0, 140, 150, -40, -50)]
    public void UiRenderer_FormAdjustmentPreservesNativeRightBottomOnlyPolicy(
        int x, int y, int width, int height, int offsetX, int offsetY)
    {
        string path = Path.Combine(_directory, "form-adjust.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" pos="{x} {y}" size="{width} {height}"/></resource>
            """);
        PangyaUiNode form = Assert.Single(PangyaUiDocument.Load(path).Nodes);
        var original = new PangyaUiRenderOptions();
        var adjusted = PangyaUiRenderer.AdjustFormToViewport(form, new Size(100, 100), original);
        Point actual = adjusted.WindowOffsets?.GetValueOrDefault(form) ?? Point.Empty;
        Assert.Equal(new Point(offsetX, offsetY), actual);
        Assert.Null(original.WindowOffsets);
        Assert.Same(adjusted, PangyaUiRenderer.AdjustFormToViewport(form, new Size(100, 100), adjusted));
    }

    [Fact]
    public void UiRenderer_DesktopWallpaperLockAffectsFilenameSetterButNotBitmapSetter()
    {
        string path = Path.Combine(_directory, "wallpaper-lock.xml");
        File.WriteAllText(path, "<resource><element type=\"FORM\" name=\"desktop\" size=\"30 30\"/></resource>");
        PangyaUiNode desktop = Assert.Single(PangyaUiDocument.Load(path).Nodes);
        var original = PangyaUiRenderer.SetDesktopWallpaper(desktop, "original.png", true);
        var locked = original with { LockedDesktopWallpapers = new HashSet<PangyaUiNode> { desktop } };
        Assert.Same(locked, PangyaUiRenderer.SetDesktopWallpaper(desktop, null, false, locked));
        using var bitmap = new Bitmap(10, 10);
        var replaced = PangyaUiRenderer.SetDesktopWallpaperFromBitmap(desktop, bitmap, true, locked);
        Assert.Same(bitmap, replaced.DesktopWallpaperImages![desktop]);
        Assert.Equal(string.Empty, replaced.DesktopWallpapers![desktop]);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        Assert.True(renderer.IsDesktopWallpaperVisible(desktop, replaced));
        Assert.False(renderer.IsDesktopWallpaperVisible(desktop,
            replaced with { DesktopWallpaperTint = Color.FromArgb(0) }));
        Assert.True(renderer.IsDesktopWallpaperVisible(desktop,
            replaced with { DesktopWallpaperTint = Color.FromArgb(0, 0, 0, 1) }));
        var hidden = PangyaUiRenderer.SetDesktopWallpaperFromBitmap(desktop, null, false, replaced);
        Assert.Same(bitmap, hidden.DesktopWallpaperImages![desktop]);
        Assert.False(hidden.DesktopWallpaperVisibility![desktop]);
        Assert.False(renderer.IsDesktopWallpaperVisible(desktop, hidden));
        var cleared = PangyaUiRenderer.SetDesktopWallpaper(desktop, null, true,
            hidden with { LockedDesktopWallpapers = null });
        Assert.False(cleared.DesktopWallpaperImages!.ContainsKey(desktop));
        Assert.False(cleared.DesktopWallpaperVisibility![desktop]);
        Assert.Equal("original.png", original.DesktopWallpapers![desktop]);
    }

    [Fact]
    public void UiRenderer_TopmostRegistrationOrderControlsDrawingAndHitTesting()
    {
        SaveSolidImage(Path.Combine(_directory, "first.png"), Color.Red, 10, 10);
        SaveSolidImage(Path.Combine(_directory, "second.png"), Color.Blue, 10, 10);
        string path = Path.Combine(_directory, "topmost-order.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="30 30">
              <item type="AREA" name="first"><param name="bgimg" var="first.png"/></item>
              <item type="AREA" name="second"><param name="bgimg" var="second.png"/></item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode first = Assert.Single(document.Nodes, node => node.Name == "first");
        PangyaUiNode second = Assert.Single(document.Nodes, node => node.Name == "second");
        var options = PangyaUiRenderer.SetWindowTopmost(second, true);
        options = PangyaUiRenderer.SetWindowTopmost(first, true, options);
        options = PangyaUiRenderer.SetWindowTopmost(second, true, options);
        Assert.Equal(new[] { second, first }, options.TopmostOrder);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(30, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        Assert.Same(first, renderer.HitTest(document, form, new Point(5, 5), options));
        options = PangyaUiRenderer.SetWindowTopmost(second, false, options);
        options = PangyaUiRenderer.SetWindowTopmost(second, true, options);
        Assert.Equal(new[] { first, second }, options.TopmostOrder);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
        Assert.Same(second, renderer.HitTest(document, form, new Point(5, 5), options));
    }

    [Fact]
    public void UiRenderer_ClearingTopmostRemovesDescendantsAndPreservesOtherWindows()
    {
        string path = Path.Combine(_directory, "topmost-reset.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 100">
              <item type="GROUPBOX" name="parent"><item type="AREA" name="child"/></item>
              <item type="AREA" name="other"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode parent = Assert.Single(document.Nodes, node => node.Name == "parent");
        PangyaUiNode child = Assert.Single(document.Nodes, node => node.Name == "child");
        PangyaUiNode other = Assert.Single(document.Nodes, node => node.Name == "other");
        var original = new PangyaUiRenderOptions
            { TopmostNodes = new HashSet<PangyaUiNode> { parent, child, other } };
        var cleared = PangyaUiRenderer.SetWindowTopmost(parent, false, original);
        Assert.Equal(other, Assert.Single(cleared.TopmostNodes!));
        Assert.Equal(3, original.TopmostNodes.Count);
        var restored = PangyaUiRenderer.SetWindowTopmost(parent, true, cleared);
        Assert.Contains(parent, restored.TopmostNodes!);
        Assert.DoesNotContain(child, restored.TopmostNodes!);
    }

    [Fact]
    public void UiRenderer_ViewerOpenRetainsPreviousImageOnFailureAndResetsOnlyNewImages()
    {
        SaveSolidImage(Path.Combine(_directory, "replacement.png"), Color.Blue, 20, 80);
        string path = Path.Combine(_directory, "viewer-open.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 40">
              <item type="VIEWER" name="viewer" rect="0 0 20 20">
                <param name="image" var="missing-initial.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiNode viewer = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "viewer");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var original = new PangyaUiRenderOptions
        {
            ViewerTopRows = new Dictionary<PangyaUiNode, int> { [viewer] = 2 },
            ViewerScrollRows = new Dictionary<PangyaUiNode, float> { [viewer] = 2.5f }
        };
        Assert.False(renderer.TryOpenViewer(viewer, "missing-initial.png", original, out var initialFailure));
        Assert.Same(original, initialFailure);
        Assert.True(renderer.TryOpenViewer(viewer, string.Empty, original, out var emptyUnchanged));
        Assert.Same(original, emptyUnchanged);
        Assert.True(renderer.TryOpenViewer(viewer, "replacement.png", original, out var opened));
        Assert.Equal(0, opened.ViewerTopRows![viewer]);
        Assert.Equal(0f, opened.ViewerScrollRows![viewer]);
        Assert.Equal(2, original.ViewerTopRows[viewer]);
        Assert.False(renderer.TryOpenViewer(viewer, "missing.png", opened, out var failed));
        Assert.Same(opened, failed);
        Assert.Equal("replacement.png", failed.ViewerImages![viewer]);
        Assert.True(renderer.TryOpenViewer(viewer, "replacement.png", opened, out var unchanged));
        Assert.Same(opened, unchanged);
    }

    [Fact]
    public void UiRenderer_WindowFocusStopsAtFormsAndHonorsFixedDescendants()
    {
        string path = Path.Combine(_directory, "window-focus.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="outer" size="100 100">
              <item type="AREA" name="direct"/>
              <item type="FORM" name="inner"><item type="AREA" name="nested"/></item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode outer = Assert.Single(document.Nodes, node => node.Name == "outer");
        PangyaUiNode direct = Assert.Single(document.Nodes, node => node.Name == "direct");
        PangyaUiNode nested = Assert.Single(document.Nodes, node => node.Name == "nested");
        var options = new PangyaUiRenderOptions
        {
            ViewFocusedNodes = new HashSet<PangyaUiNode> { outer }
        };
        Assert.True(PangyaUiRenderer.IsWindowViewFocused(direct, options));
        Assert.False(PangyaUiRenderer.IsWindowViewFocused(nested, options));
        Assert.True(PangyaUiRenderer.IsWindowViewFocused(nested,
            options with { FixedNodes = new HashSet<PangyaUiNode> { nested } }));
    }

    [Fact]
    public void UiEditText_WordWrapRetainsNativeMeasurementPosition()
    {
        var model = new PangyaUiEditText();
        var measured = new List<string>();
        model.AddText("AB CDE", 5, text => { measured.Add(text); return text.Length; },
            spaceAlign: true);
        Assert.Equal(new[] { "AB", "CDE" }, model.Lines.Select(line => line.Text));
        Assert.Equal(new[] { "A", "AB", "AB", "AB C", "AB CD", "CD", "CDE" }, measured);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void UiRenderer_FormMessageTargetsLastDirectControlsAndWrapsEdit(bool followBottom, int expectedTop)
    {
        string path = Path.Combine(_directory, "form-message.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="80 60">
              <item type="STATIC" name="message" caption="first"/>
              <item type="STATIC" name="message" caption="last"/>
              <item type="EDIT" name="message" rect="0 0 4 16">
                <param name="multiline" var="1"/>
              </item>
              <item type="GROUPBOX" name="group"><item type="STATIC" name="message" caption="nested"/></item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode edit = Assert.Single(form.Children, node => node.Type == "EDIT");
        PangyaUiNode[] labels = form.Children.Where(node => node.Type == "STATIC").ToArray();
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(80, 60);
        using Graphics graphics = Graphics.FromImage(bitmap);
        var original = new PangyaUiRenderOptions
        {
            MeasureCaptionText = text => text.Length,
            EditTopRows = new Dictionary<PangyaUiNode, int> { [edit] = 99 },
            EditFollowBottom = new Dictionary<PangyaUiNode, bool> { [edit] = followBottom }
        };
        PangyaUiRenderOptions updated = renderer.SetFormMessage(graphics, form, "AB CD", true, original);
        Assert.Null(original.StaticCaptions);
        Assert.Equal("AB CD", updated.StaticCaptions![labels[1]]);
        Assert.False(updated.StaticCaptions.ContainsKey(labels[0]));
        Assert.Equal(new[] { "AB", "CD" }, updated.EditLines![edit]);
        Assert.Equal(new[] { 0, 0 }, updated.EditLineStyles![edit]);
        Assert.Equal(new[] { 0 }, updated.JustifiedEditLines![edit]);
        Assert.Equal(expectedTop, updated.EditTopRows![edit]);
        Assert.Equal(99, original.EditTopRows[edit]);
    }

    [Fact]
    public void UiEditText_AddTextWrapsAtStrictWidthAndRetainsStyles()
    {
        var model = new PangyaUiEditText(multiline: true);
        model.AddText("AB CD\nZ", 4, text => text.Length, spaceAlign: true, style: 2);
        Assert.Equal(new[] { "AB", "CD", "Z" }, model.Lines.Select(line => line.Text));
        Assert.Equal(new[] { true, false, false }, model.Lines.Select(line => line.Justified));
        Assert.All(model.Lines, line => Assert.Equal(2, line.Style));
        model.Clear();
        model.AddText("ABC", 3, text => text.Length);
        Assert.Equal(new[] { "AB", "C" }, model.Lines.Select(line => line.Text));
        model.Clear();
        model.AddText("X", 1, text => text.Length);
        Assert.Empty(model.Lines);
    }

    [Fact]
    public void UiEditText_NativeLineIndexingLimitsAndSingleLineCapacity()
    {
        var single = new PangyaUiEditText(multiline: false, characterLimit: 3);
        Assert.Equal("Aあ", single.AddLine("AあB"));
        Assert.Null(single.AddLine("ignored"));
        Assert.Equal("new", single.SetLine(5, "new value"));
        Assert.Equal("new", single.GetLine(1));
        var multi = new PangyaUiEditText(multiline: true);
        Assert.Equal("third", multi.SetLine(3, "third", justified: true, style: 2));
        Assert.Equal(3, multi.Lines.Count);
        Assert.Equal(string.Empty, multi.GetLine(2));
        Assert.Equal(new PangyaUiEditLine("third", 2, true), multi.Lines[2]);
        multi.DeleteFirstLine();
        multi.DeleteEndLine();
        Assert.Single(multi.Lines);
        multi.Clear();
        Assert.Empty(multi.Lines);
    }

    [Fact]
    public void UiEditText_InsertionAndDeletionUseZeroBasedExistingPositions()
    {
        var model = new PangyaUiEditText();
        Assert.True(model.Multiline);
        Assert.Null(model.InsertLine(0, "absent"));
        model.AddLine("last");
        Assert.Equal("first", model.InsertLine(0, "first", justified: true));
        Assert.Null(model.InsertLine(2, "past-end"));
        Assert.Equal(new[] { "first", "last" }, model.Lines.Select(line => line.Text));
        Assert.True(model.Lines[0].Justified);
        Assert.Equal(0, model.DeleteLine(0));
        Assert.Equal("last", Assert.Single(model.Lines).Text);
        Assert.Equal(2, model.DeleteLine(1));
        Assert.Single(model.Lines);
    }

    [Theory]
    [InlineData("\\c0xffff0000suffix\\cX", "X", -65536)]
    [InlineData("\\cunknown\\cX", "\\cunknown\\cX", -16777216)]
    public void UiRenderer_TooltipUsesSharedTagRecognition(string caption, string expectedText, int expectedColor)
    {
        string path = Path.Combine(_directory, "tooltip-tag-parser.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="200 80">
              <item type="AREA" name="anchor" rect="0 0 10 10"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode anchor = Assert.Single(document.Nodes, node => node.Name == "anchor");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawn = new List<(string Text, int Color)>();
        var options = new PangyaUiRenderOptions
        {
            ShowTooltip = true, TooltipNode = anchor, TooltipAlpha = 255,
            Tooltips = new Dictionary<PangyaUiNode, PangyaUiTooltipPreview>
                { [anchor] = new(caption, Position: new Point(10, 10)) },
            MeasureTooltipText = text => text.Length * 5,
            DrawTooltipText = (_, text, brush, _) => drawn.Add((text, ((SolidBrush)brush).Color.ToArgb()))
        };
        using var bitmap = new Bitmap(200, 80);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Contains((expectedText, expectedColor), drawn);
        Assert.All(drawn, item => Assert.Equal(expectedText, item.Text));
        drawn.Clear();
        var disabled = options with
        {
            TooltipEnabled = new Dictionary<PangyaUiNode, bool> { [anchor] = false }
        };
        renderer.Render(graphics, document, form, disabled);
        Assert.Empty(drawn);
        Assert.Equal(caption, disabled.Tooltips![anchor].Text);
        renderer.Render(graphics, document, form, disabled with
        {
            TooltipEnabled = new Dictionary<PangyaUiNode, bool> { [anchor] = true }
        });
        Assert.Contains((expectedText, expectedColor), drawn);
    }

    [Fact]
    public void UiText_LongestWidthMeasuresWholeTagExcludedLines()
    {
        var measured = new List<string>();
        float width = PangyaUiText.GetLongestLineWidth("A\\ct\\cV\nX", text =>
        {
            measured.Add(text);
            return text == "AV" ? 9 : 6;
        });
        Assert.Equal(9f, width);
        Assert.Equal(new[] { "AV", "X" }, measured);
        Assert.Equal(0f, PangyaUiText.GetLongestLineWidth("", _ => throw new InvalidOperationException()));
    }

    [Theory]
    [InlineData("A\\ct\\cB\\cn\\c", "AB")]
    [InlineData("A\\cunknown\\cB", "A\\cunknown\\cB")]
    [InlineData("\\c0xff001122\\cX", "X")]
    [InlineData("A\r\nB\\nC\n", "A\n \nB\nC")]
    [InlineData("", "")]
    [InlineData("\n\n", " \n ")]
    [InlineData("X\\c t \\cY", "XY")]
    [InlineData("X\\c0x11223344,0x55667788\\cY", "XY")]
    [InlineData("X\\c0x11223344,bad\\cY", "X\\c0x11223344,bad\\cY")]
    public void UiText_PrintOptionsPreserveSourceOrExcludeRecognizedTags(string source, string expected)
    {
        Assert.Equal(source, PangyaUiText.GetPrintData(source, PangyaUiTextPrintOption.All));
        Assert.Equal(expected, PangyaUiText.GetPrintData(source, PangyaUiTextPrintOption.ExcludeTags));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_MenuDisablingByTextMatchesEveryDuplicateCaseSensitively(bool renamed)
    {
        string path = Path.Combine(_directory, "disabled-menu-text.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 100">
              <item type="CONTEXTMENU" name="menu" caption="Same|Same|same|"/>
            </element></resource>
            """);
        PangyaUiNode menu = Assert.Single(PangyaUiDocument.Load(path).Nodes, node => node.Name == "menu");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            CaptionFontHeight = 10,
            MeasureCaptionText = text => text.Length * 6,
            MenuStates = new Dictionary<PangyaUiNode, PangyaUiMenuState>
            {
                [menu] = new()
                {
                    DisabledTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                        { renamed ? "Renamed" : "Same" },
                    TextReplacements = renamed
                        ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Same"] = "Renamed" }
                        : null
                }
            }
        };
        Assert.Equal(-1, renderer.HitTestMenuRow(menu, new Point(10, 15), options));
        Assert.Equal(-1, renderer.HitTestMenuRow(menu, new Point(10, 32), options));
        Assert.Equal(2, renderer.HitTestMenuRow(menu, new Point(10, 49), options));
    }

    [Fact]
    public void UiRenderer_TabImagePrefixResolvesNativeSuffixesAndExplicitSize()
    {
        SaveSolidImage(Path.Combine(_directory, "native_n.png"), Color.Red, 10, 10);
        SaveSolidImage(Path.Combine(_directory, "native_o.png"), Color.Green, 10, 10);
        SaveSolidImage(Path.Combine(_directory, "native_d.png"), Color.Blue, 10, 10);
        string path = Path.Combine(_directory, "tab-prefix.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 20">
              <item type="TABBUTTON" name="tabs" rect="0 0 20 10"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode tabs = Assert.Single(document.Nodes, node => node.Name == "tabs");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            Tabs = new Dictionary<PangyaUiNode, IReadOnlyList<PangyaUiTab>>
                { [tabs] = new[] { PangyaUiTab.FromImagePrefix("native", 42, new Size(20, 10)) } },
            SelectedTabIdentifiers = new Dictionary<PangyaUiNode, uint> { [tabs] = 42 }
        };
        using var bitmap = new Bitmap(40, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(15, 5).ToArgb());
        Assert.Equal(0, renderer.HitTestTab(tabs, new Point(15, 5), options));
    }

    [Theory]
    [InlineData(42u, null, 1)]
    [InlineData(99u, null, 0)]
    [InlineData(42u, 7u, 1)]
    [InlineData(42u, 42u, -1)]
    public void UiRenderer_TabSelectionUsesStoredIdentifierAndPreservesStateWhenMissing(
        uint identifier, uint? unselectedIdentifier, int selected)
    {
        SaveSolidImage(Path.Combine(_directory, "tab-normal.png"), Color.Red, 10, 10);
        SaveSolidImage(Path.Combine(_directory, "tab-selected.png"), Color.Blue, 10, 10);
        string path = Path.Combine(_directory, "tab-identifiers.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 20">
              <item type="TABBUTTON" name="tabs" rect="0 0 20 10"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode tabs = Assert.Single(document.Nodes, node => node.Name == "tabs");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            Tabs = new Dictionary<PangyaUiNode, IReadOnlyList<PangyaUiTab>>
            {
                [tabs] = new[]
                {
                    new PangyaUiTab("tab-normal.png", Selected: "tab-selected.png",
                        State: PangyaUiButtonState.Selected) { Identifier = 7 },
                    new PangyaUiTab("tab-normal.png", Selected: "tab-selected.png") { Identifier = 42 }
                }
            },
            SelectedTabIdentifiers = new Dictionary<PangyaUiNode, uint> { [tabs] = identifier },
            UnselectedTabIdentifiers = unselectedIdentifier is uint unselected
                ? new Dictionary<PangyaUiNode, uint> { [tabs] = unselected } : null
        };
        using var bitmap = new Bitmap(40, 20);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        for (int index = 0; index < 2; index++)
            Assert.Equal((index == selected ? Color.Blue : Color.Red).ToArgb(),
                bitmap.GetPixel(index * 10 + 5, 5).ToArgb());
    }

    [Theory]
    [InlineData(null, 20, 2)]
    [InlineData(null, 120, 2)]
    [InlineData(3, 20, 3)]
    public void UiRenderer_RuntimeListItemSizePreservesColumnsUnlessExplicitlyRecalculated(
        int? columns, int width, int expected)
    {
        string path = Path.Combine(_directory, "list-columns.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 40">
              <item type="LISTBOX" name="list" rect="0 0 100 20">
                <param name="itemsize" var="40 20"/><param name="column" var="2"/>
                <param name="show_scrollbar" var="0"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode list = Assert.Single(document.Nodes, node => node.Name == "list");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawn = new List<int>();
        var options = new PangyaUiRenderOptions
        {
            Lists = new Dictionary<PangyaUiNode, PangyaUiListPreview>
            {
                [list] = new(6, 0, (_, index, _) => drawn.Add(index))
                {
                    ItemSize = new Size(width, 20), ColumnCapacity = columns
                }
            }
        };
        using var bitmap = new Bitmap(100, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Enumerable.Range(0, expected), drawn);
    }

    [Fact]
    public void UiRenderer_HiddenViewerScrollbarRetainsRangeAndSourceOffset()
    {
        string imagePath = Path.Combine(_directory, "viewer-scroll.png");
        using (var image = new Bitmap(20, 80))
        {
            using Graphics imageGraphics = Graphics.FromImage(image);
            imageGraphics.Clear(Color.Red);
            imageGraphics.FillRectangle(Brushes.Blue, 0, 20, 20, 60);
            image.Save(imagePath);
        }
        string path = Path.Combine(_directory, "hidden-viewer-scrollbar.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 40">
              <item type="VIEWER" name="viewer" rect="0 0 20 20">
                <param name="image" var="viewer-scroll.png"/><param name="use_wheel" var="true"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode viewer = Assert.Single(document.Nodes, node => node.Name == "viewer");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            HiddenViewerScrollbars = new HashSet<PangyaUiNode> { viewer },
            ViewerTopRows = new Dictionary<PangyaUiNode, int> { [viewer] = 1 }
        };
        Assert.Equal(3, renderer.GetViewerMaximumTopRow(viewer, options: options));
        Assert.Equal(Rectangle.Empty, renderer.GetViewerScrollThumb(viewer, options));
        using var bitmap = new Bitmap(40, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
    }

    [Theory]
    [InlineData(false, -30)]
    [InlineData(true, 0)]
    public void UiRenderer_LongPasswordTextStaysAnchoredWhileEditableTextScrolls(bool password, float expectedX)
    {
        string path = Path.Combine(_directory, "password-scroll.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="60 40">
              <item type="EDIT" name="edit" rect="0 0 30 30">
                <param name="password" var="{password.ToString().ToLowerInvariant()}"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode edit = Assert.Single(document.Nodes, node => node.Name == "edit");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var origins = new List<PointF>();
        var texts = new List<string>();
        var options = new PangyaUiRenderOptions
        {
            EditLines = new Dictionary<PangyaUiNode, IReadOnlyList<string>> { [edit] = new[] { "ABCDEF" } },
            EditCarets = new Dictionary<PangyaUiNode, PangyaUiEditCaret>
                { [edit] = new(0, 6, TimeSpan.FromMilliseconds(100)) },
            MeasureCaptionText = text => text.Length * 10,
            DrawCaptionText = (_, text, _, origin) => { texts.Add(text); origins.Add(origin); }
        };
        using var bitmap = new Bitmap(60, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(password ? "******" : "ABCDEF", Assert.Single(texts));
        Assert.Equal(expectedX, Assert.Single(origins).X);
    }

    [Theory]
    [InlineData(true, 37)]
    [InlineData(false, 20)]
    public void UiRenderer_EditEmoticonStyleChangesMeasurementButKeepsImage(bool style, int imageX)
    {
        SaveSolidImage(Path.Combine(_directory, "emoticon.png"), Color.Red, 26, 24);
        string emoticons = Path.Combine(_directory, "emoticon.xml");
        File.WriteAllText(emoticons, "<emoticon><def name=\"face\"/></emoticon>");
        string path = Path.Combine(_directory, "edit-emoticon-style.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="100 40">
              <item type="EDIT" name="edit" rect="0 0 100 30">
                <param name="readonly" var="true"/><param name="align" var="1"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode edit = Assert.Single(document.Nodes, node => node.Name == "edit");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load(new[] { path, emoticons }));
        var options = new PangyaUiRenderOptions
        {
            EditLines = new Dictionary<PangyaUiNode, IReadOnlyList<string>> { [edit] = new[] { "(face)" } },
            EditEmoticonStyles = new Dictionary<PangyaUiNode, bool> { [edit] = style },
            MeasureCaptionText = text => text.Length * 10,
            DrawCaptionText = (_, _, _, _) => { }
        };
        using var bitmap = new Bitmap(100, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(imageX + 1, 5).ToArgb());
        Assert.NotEqual(Color.Red.ToArgb(), bitmap.GetPixel(imageX - 1, 5).ToArgb());
    }

    [Theory]
    [InlineData(false, true, 255)]
    [InlineData(true, false, 64)]
    public void UiRenderer_RuntimeReadOnlyStyleOverridesXmlTextAlpha(
        bool initialStyle, bool runtimeStyle, int expectedAlpha)
    {
        string path = Path.Combine(_directory, "runtime-edit-style.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="60 40">
              <item type="EDIT" name="edit" rect="0 0 50 30">
                <param name="readonly" var="{initialStyle.ToString().ToLowerInvariant()}"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode edit = Assert.Single(document.Nodes, node => node.Name == "edit");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var alphas = new List<int>();
        var options = new PangyaUiRenderOptions
        {
            EditLines = new Dictionary<PangyaUiNode, IReadOnlyList<string>> { [edit] = new[] { "X" } },
            EditReadOnlyStyles = new Dictionary<PangyaUiNode, bool> { [edit] = runtimeStyle },
            EditTextColors = new Dictionary<PangyaUiNode, PangyaUiEditTextColors>
                { [edit] = new(Color.FromArgb(64, Color.Red)) },
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, _, brush, _) => alphas.Add(((SolidBrush)brush).Color.A)
        };
        using var bitmap = new Bitmap(60, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.NotEmpty(alphas);
        Assert.All(alphas, alpha => Assert.Equal(expectedAlpha, alpha));
        Assert.Equal(initialStyle ? "true" : "false", edit.Parameters["readonly"]);
    }

    [Theory]
    [InlineData("MACROITEM")]
    [InlineData("FRAME")]
    [InlineData("CONTEXTMENU")]
    public async Task UiDocument_DefinitionResourceEditUpdatesDeclaredResourceAndPersists(string type)
    {
        string path = Path.Combine(_directory, "macro-edit.xml");
        await File.WriteAllTextAsync(path, $"""
            <resource><element type="FORM" name="form" size="40 40">
              <item type="{type}" name="instance" resource="original">
                <param name="normal" var="unrelated"/>
                <param name="resource" var="previous"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode instance = Assert.Single(document.Nodes, node => node.Name == "instance");

        instance.SetResource(" replacement ");
        Assert.Equal("replacement", instance.GetResource(PangyaUiButtonState.Normal));
        Assert.Equal("unrelated", instance.Parameters["normal"]);
        await document.SaveAtomicAsync();

        PangyaUiNode saved = Assert.Single(PangyaUiDocument.Load(path).Nodes,
            node => node.Name == "instance");
        foreach (PangyaUiButtonState state in Enum.GetValues<PangyaUiButtonState>())
            Assert.Equal("replacement", saved.GetResource(state));
        Assert.Equal("unrelated", saved.Parameters["normal"]);
    }

    [Theory]
    [InlineData(PangyaUiButtonState.Normal)]
    [InlineData(PangyaUiButtonState.Hover)]
    [InlineData(PangyaUiButtonState.Selected)]
    public void UiRenderer_MacroResourceDoesNotFollowButtonPreviewState(PangyaUiButtonState state)
    {
        SaveSolidImage(Path.Combine(_directory, "right.png"), Color.Red, 4, 3);
        SaveSolidImage(Path.Combine(_directory, "wrong.png"), Color.Lime, 4, 3);
        string path = Path.Combine(_directory, "macro-state.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="MACROITEM" name="right">
                <item type="AREA" name="right-image" pos="5 6">
                  <param name="bgimg" var="right.png"/>
                </item>
              </element>
              <element type="MACROITEM" name="wrong">
                <item type="AREA" name="wrong-image" pos="20 20">
                  <param name="bgimg" var="wrong.png"/>
                </item>
              </element>
              <element type="FORM" name="form" size="40 40">
                <item type="MACROITEM" name="instance" resource="right" pos="30 30">
                  <param name="normal" var="wrong"/>
                  <param name="over" var="wrong"/>
                  <param name="selected" var="wrong"/>
                </item>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode instance = Assert.Single(document.Nodes, node => node.Name == "instance");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load(new[] { path }));
        var options = new PangyaUiRenderOptions(ButtonState: state);
        using var bitmap = new Bitmap(40, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal("right", instance.GetResource(state));
        Assert.Equal(new Rectangle(5, 6, 4, 3), renderer.GetRenderedBounds(instance, options));
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(6, 7).ToArgb());
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(0u, true)]
    [InlineData(4u, false)]
    public void UiRenderer_RuntimeFormFlagsOverrideXmlModelessState(uint? flags, bool shaded)
    {
        string path = Path.Combine(_directory, "form-flags.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <param name="modal" var="false"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            ModalForms = new HashSet<PangyaUiNode> { form },
            FormFlags = flags.HasValue ? new Dictionary<PangyaUiNode, uint> { [form] = flags.Value } : null
        };
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Color pixel = bitmap.GetPixel(10, 10);
        if (shaded) Assert.True(pixel.R < 46 && pixel.G < 49 && pixel.B < 55);
        else Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), pixel.ToArgb());
    }

    [Fact]
    public void UiRenderer_RuntimeMenuItemTextPreservesOriginalMenuGeometry()
    {
        string path = Path.Combine(_directory, "menu-item-text.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="120 80">
              <item type="CONTEXTMENU" name="menu" caption="A" pos="5 6"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode menu = Assert.Single(document.Nodes, node => node.Name == "menu");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawn = new List<string>();
        var options = new PangyaUiRenderOptions
        {
            ContextMenuItemTexts = new Dictionary<PangyaUiNode, IReadOnlyDictionary<int, string>>
            {
                [menu] = new Dictionary<int, string> { [0] = "MuchLonger" }
            },
            CaptionFontHeight = 12,
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, text, _, _) => drawn.Add(text)
        };
        using var bitmap = new Bitmap(120, 80);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(new[] { "MuchLonger" }, drawn);
        Assert.Equal(new Rectangle(5, 6, 42, 37), renderer.GetRenderedBounds(menu, options));
        Assert.Equal(0, renderer.HitTestMenuRow(menu, new Point(10, 20), options));
        Assert.Equal(-1, renderer.HitTestMenuRow(menu, new Point(60, 20), options));
    }

    [Theory]
    [InlineData("EDIT", false)]
    [InlineData("EDIT", true)]
    [InlineData("COMBOBOX", false)]
    [InlineData("COMBOBOX", true)]
    public void UiRenderer_RuntimeEditColorsUpdateIndependently(string type, bool changeForeground)
    {
        string path = Path.Combine(_directory, "edit-colors.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="80 80">
              <item type="{type}" name="control" rect="5 5 75 45">
                <param name="font" var="2"/>
                <param name="fontcolor" var="ffff0000"/>
                <param name="fontcolor2" var="ff0000ff"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode control = Assert.Single(document.Nodes, node => node.Name == "control");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawn = new List<int>();
        var options = new PangyaUiRenderOptions
        {
            EditTextColors = new Dictionary<PangyaUiNode, PangyaUiEditTextColors>
            {
                [control] = changeForeground ? new(Foreground: Color.Lime) : new(Outline: Color.White)
            },
            EditLines = type == "EDIT"
                ? new Dictionary<PangyaUiNode, IReadOnlyList<string>> { [control] = new[] { "X" } } : null,
            Combos = type == "COMBOBOX"
                ? new Dictionary<PangyaUiNode, PangyaUiComboPreview>
                {
                    [control] = new(new[] { new PangyaUiComboItem("X") }, IsOpen: true)
                } : null,
            ComboViewportHeight = 80,
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, _, brush, _) => drawn.Add(Assert.IsType<SolidBrush>(brush).Color.ToArgb())
        };
        using var bitmap = new Bitmap(80, 80);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(9, drawn.Count);
        Assert.All(drawn.Take(8), color => Assert.Equal((changeForeground ? Color.Blue : Color.White).ToArgb(), color));
        Assert.Equal((changeForeground ? Color.Lime : Color.Red).ToArgb(), drawn[^1]);
    }

    [Fact]
    public void UiRenderer_RuntimeTabSeparatorWidthKeepsPixelsAndHitRegionsAligned()
    {
        SaveSolidImage(Path.Combine(_directory, "tab.png"), Color.Red, 4, 3);
        SaveSolidImage(Path.Combine(_directory, "separator.png"), Color.Blue, 3, 3);
        string path = Path.Combine(_directory, "tab-spacing.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="TABBUTTON" name="tabs" pos="5 6">
                <param name="sepImg" var="separator.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode tabs = Assert.Single(document.Nodes, node => node.Name == "tabs");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            Tabs = new Dictionary<PangyaUiNode, IReadOnlyList<PangyaUiTab>>
            {
                [tabs] = new[] { new PangyaUiTab("tab.png"), new("tab.png"), new("tab.png") }
            },
            TabSeparatorWidths = new Dictionary<PangyaUiNode, float> { [tabs] = 1.5f }
        };
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(new Rectangle(5, 6, 15, 3), renderer.GetRenderedBounds(tabs, options));
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(10, 7).ToArgb());
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(11, 7).ToArgb());
        Assert.Equal(Color.Blue.ToArgb(), bitmap.GetPixel(15, 7).ToArgb());
        Assert.Equal(-1, renderer.HitTestTab(tabs, new Point(10, 7), options));
        Assert.Equal(1, renderer.HitTestTab(tabs, new Point(11, 7), options));
        Assert.Equal(2, renderer.HitTestTab(tabs, new Point(16, 7), options));
    }

    [Theory]
    [InlineData(false, true, 0)]
    [InlineData(false, false, 128)]
    [InlineData(true, true, 128)]
    [InlineData(true, false, 0)]
    public void UiRenderer_StaticManagerTextForcesOpaqueAlpha(bool smallFont, bool enabled, int opacity)
    {
        string path = Path.Combine(_directory, "static-alpha.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="80 40">
              <item type="STATIC" name="label" caption="X" pos="5 6">
                <param name="style" var="2"/>
                <param name="color" var="0000ff00"/>
                <param name="color2" var="00ff0000"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode label = Assert.Single(document.Nodes, node => node.Name == "label");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var colors = new List<int>();
        var options = new PangyaUiRenderOptions
        {
            SmallFontCaptions = smallFont ? new HashSet<PangyaUiNode> { label } : null,
            DisabledNodes = enabled ? null : new HashSet<PangyaUiNode> { label },
            CaptionOpacity = new Dictionary<PangyaUiNode, byte> { [label] = (byte)opacity },
            MeasureCaptionText = text => text.Length * 6,
            MeasureTooltipText = text => text.Length * 5,
            DrawCaptionText = (_, _, brush, _) => colors.Add(Assert.IsType<SolidBrush>(brush).Color.ToArgb()),
            DrawTooltipText = (_, _, brush, _) => colors.Add(Assert.IsType<SolidBrush>(brush).Color.ToArgb())
        };
        using var bitmap = new Bitmap(80, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(9, colors.Count);
        Assert.All(colors.Take(8), color => Assert.Equal((enabled ? Color.Red : Color.White).ToArgb(), color));
        Assert.Equal(Color.Lime.ToArgb(), colors[^1]);
    }

    [Theory]
    [InlineData("EDIT", false)]
    [InlineData("EDIT", true)]
    [InlineData("LISTBOX", false)]
    [InlineData("LISTBOX", true)]
    [InlineData("VIEWER", false)]
    [InlineData("VIEWER", true)]
    public void UiRenderer_EmptyControlHonorsRuntimeScrollbarGuide(string type, bool showGuide)
    {
        for (int index = 1; index <= 3; index++)
        {
            SaveSolidImage(Path.Combine(_directory, $"scroll_n_{index}.png"), Color.Red, 4, 2);
            SaveSolidImage(Path.Combine(_directory, $"scroll_o_{index}.png"), Color.Blue, 4, 2);
        }
        string path = Path.Combine(_directory, "empty-guide.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="60 60">
              <item type="{type}" name="control" rect="5 5 45 45">
                <param name="use_wheel" var="true"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode control = Assert.Single(document.Nodes, node => node.Name == "control");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            HoveredNode = control,
            ScrollbarGuides = new Dictionary<PangyaUiNode, bool> { [control] = showGuide },
            Lists = new Dictionary<PangyaUiNode, PangyaUiListPreview>
            {
                [control] = new(0, 0, (_, _, _) => throw new InvalidOperationException("Empty list drew an item."))
                {
                    ItemSize = new Size(20, 16)
                }
            }
        };
        using var bitmap = new Bitmap(60, 60);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(showGuide, bitmap.GetPixel(44, 6).ToArgb() == Color.Red.ToArgb());
        Assert.Equal(showGuide, bitmap.GetPixel(44, 43).ToArgb() == Color.Red.ToArgb());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_RuntimeGaugeFillPreservesInitializedGradientEndpoint(bool explicitSecondColor)
    {
        string secondColor = explicitSecondColor ? "<param name='color2' var='ff0000ff'/>" : string.Empty;
        string path = Path.Combine(_directory, "gauge-endpoint.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="40 30">
              <item type="GAUGEBAREX" name="gauge" rect="5 5 25 15">
                <param name="color" var="ffff0000"/>
                <param name="bordercolor" var="00000000"/>
                <param name="border_thickness" var="0"/>
                {secondColor}
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode gauge = Assert.Single(document.Nodes, node => node.Name == "gauge");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            GaugeValues = new Dictionary<PangyaUiNode, int> { [gauge] = 100 },
            GaugeAppearances = new Dictionary<PangyaUiNode, PangyaUiGaugeAppearance>
            {
                [gauge] = new(FillColor: Color.Lime)
            }
        };
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Color left = bitmap.GetPixel(6, 10);
        Color right = bitmap.GetPixel(23, 10);
        Assert.True(left.G > left.R && left.G > left.B);
        if (explicitSecondColor) Assert.True(right.B > right.G && right.B > right.R);
        else Assert.True(right.R > right.G && right.R > right.B);
    }

    [Theory]
    [InlineData(20, 27, false)]
    [InlineData(-20, 20, true)]
    public void UiRenderer_RuntimeGaugeAppearanceChangesFillBorderAndExpansion(
        int expansion, int expansionX, bool contraction)
    {
        string path = Path.Combine(_directory, "gauge-appearance.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 40">
              <item type="GAUGEBAR" name="gauge" rect="5 5 45 25">
                <param name="color" var="ffff0000"/>
                <param name="bgcolor" var="ff000000"/>
                <param name="border_thickness" var="2"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode gauge = Assert.Single(document.Nodes, node => node.Name == "gauge");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            GaugeValues = new Dictionary<PangyaUiNode, int> { [gauge] = 50 },
            GaugeExpansions = new Dictionary<PangyaUiNode, int> { [gauge] = expansion },
            GaugeAppearances = new Dictionary<PangyaUiNode, PangyaUiGaugeAppearance>
            {
                [gauge] = new(FillColor: Color.Lime, BorderThickness: 0,
                    ExpansionColor1: Color.Blue, ExpansionColor2: Color.Yellow)
            }
        };
        using var bitmap = new Bitmap(60, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(6, 6).ToArgb());
        Assert.Equal((contraction ? Color.Yellow : Color.Blue).ToArgb(),
            bitmap.GetPixel(expansionX, 10).ToArgb());
    }

    [Theory]
    [InlineData(1, true, 224, 224, 224)]
    [InlineData(2, true, 0, 255, 0)]
    [InlineData(3, false, 255, 255, 255)]
    public void UiRenderer_RuntimeButtonStyleControlsStateImagesAndTint(
        int style, bool enabled, int red, int green, int blue)
    {
        SaveSolidImage(Path.Combine(_directory, "normal.png"), Color.White, 4, 3);
        SaveSolidImage(Path.Combine(_directory, "hover.png"), Color.Lime, 4, 3);
        string path = Path.Combine(_directory, "button-style.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="BUTTON" name="button" pos="5 6">
                <param name="style" var="1"/>
                <param name="normal" var="normal.png"/>
                <param name="over" var="hover.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode button = Assert.Single(document.Nodes, node => node.Name == "button");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            ImageButtonStates = new Dictionary<PangyaUiNode, PangyaUiImageButtonState>
            {
                [button] = new(Enabled: enabled) { Style = style, Status = PangyaUiButtonState.Hover }
            }
        };
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(Color.FromArgb(red, green, blue).ToArgb(), bitmap.GetPixel(6, 7).ToArgb());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_SmallStaticCaptionBypassesNormalCaptionByteLimit(bool smallFont)
    {
        string path = Path.Combine(_directory, "static-font-limit.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="80 40">
              <item type="STATIC" name="label" caption="XML" pos="5 6"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode label = Assert.Single(document.Nodes, node => node.Name == "label");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        string caption = new('A', 110);
        var drawn = new List<string>();
        var options = new PangyaUiRenderOptions
        {
            StaticCaptions = new Dictionary<PangyaUiNode, string> { [label] = caption },
            SmallFontCaptions = smallFont ? new HashSet<PangyaUiNode> { label } : null,
            MeasureCaptionText = text => text.Length * 6,
            MeasureTooltipText = text => text.Length * 5,
            DrawCaptionText = (_, text, _, _) => drawn.Add(text),
            DrawTooltipText = (_, text, _, _) => drawn.Add(text)
        };
        using var bitmap = new Bitmap(80, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(smallFont ? caption : new string('A', 100) + "...", Assert.Single(drawn));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, true)]
    public void UiRenderer_RuntimeStaticStylePreservesInitializedOutlineColor(int initialStyle, bool blueOutline)
    {
        string path = Path.Combine(_directory, "static-style.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="80 40">
              <item type="STATIC" name="label" caption="X" pos="5 6">
                <param name="style" var="{initialStyle}"/>
                <param name="color" var="ff00ff00"/>
                <param name="color2" var="ff0000ff"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode label = Assert.Single(document.Nodes, node => node.Name == "label");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var colors = new List<int>();
        var options = new PangyaUiRenderOptions
        {
            StaticTextStyles = new Dictionary<PangyaUiNode, int> { [label] = 2 },
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, _, brush, _) => colors.Add(Assert.IsType<SolidBrush>(brush).Color.ToArgb())
        };
        using var bitmap = new Bitmap(80, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(9, colors.Count);
        Assert.All(colors.Take(8), color => Assert.Equal((blueOutline ? Color.Blue : Color.White).ToArgb(), color));
        Assert.Equal(Color.Lime.ToArgb(), colors[^1]);
    }

    [Theory]
    [InlineData(PangyaUiButtonState.Normal)]
    [InlineData(PangyaUiButtonState.Hover)]
    [InlineData(PangyaUiButtonState.Selected)]
    public void UiRenderer_AreaBackgroundDoesNotFollowButtonPreviewState(PangyaUiButtonState state)
    {
        SaveSolidImage(Path.Combine(_directory, "area.png"), Color.Lime, 8, 6);
        SaveSolidImage(Path.Combine(_directory, "normal.png"), Color.Red, 4, 3);
        SaveSolidImage(Path.Combine(_directory, "over.png"), Color.Blue, 2, 2);
        SaveSolidImage(Path.Combine(_directory, "selected.png"), Color.Yellow, 3, 3);
        string path = Path.Combine(_directory, "area-button-state.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="AREA" name="area" pos="5 6">
                <param name="bgimg" var="area.png"/>
                <param name="normal" var="normal.png"/>
                <param name="over" var="over.png"/>
                <param name="selected" var="selected.png"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode area = Assert.Single(document.Nodes, node => node.Name == "area");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions(ButtonState: state);
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal("area.png", area.GetResource(state));
        Assert.Equal(new Rectangle(5, 6, 8, 6), renderer.GetRenderedBounds(area, options));
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(9, 9).ToArgb());
        Assert.Same(area, renderer.HitTest(document, form, new Point(9, 9), options));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_StaticCaptionNeverFallsThroughToAnImageResource(bool clearCaption)
    {
        SaveSolidImage(Path.Combine(_directory, "static-fallback.png"), Color.Magenta, 8, 6);
        string path = Path.Combine(_directory, "static-resource.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 40">
              <item type="STATIC" name="label" caption="XML" resource="static-fallback.png" rect="5 6 25 18"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode label = Assert.Single(document.Nodes, node => node.Name == "label");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawn = new List<string>();
        var options = new PangyaUiRenderOptions
        {
            StaticCaptions = new Dictionary<PangyaUiNode, string>
            {
                [label] = clearCaption ? string.Empty : "Label"
            },
            DrawCaptionText = (_, text, _, _) => drawn.Add(text)
        };
        using var bitmap = new Bitmap(60, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(6, 7).ToArgb());
        Assert.Equal(clearCaption ? Rectangle.Empty : new Rectangle(5, 6, 20, 12),
            renderer.GetRenderedBounds(label, options));
        if (clearCaption) Assert.Empty(drawn);
        else Assert.Equal(new[] { "Label" }, drawn);
    }

    [Fact]
    public void UiRenderer_ClearedContextMenuKeepsNativeEmptyGeometryWithoutOldCaption()
    {
        for (int index = 0; index < 9; index++)
            SaveSolidImage(Path.Combine(_directory, $"menu{index:00}.png"), Color.Red, 2, 2);
        string path = Path.Combine(_directory, "empty-menu.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="FRAME" name="skin" color="ffffffff">
                <layer type="0"/>
                <bfrm filename="menu"/>
              </element>
              <element type="FORM" name="form" size="120 80">
                <item type="CONTEXTMENU" name="menu" caption="Old" resource="skin" rect="5 5 105 65"/>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode menu = Assert.Single(document.Nodes, node => node.Name == "menu");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, PangyaUiResourceCatalog.Load(new[] { path }));
        var drawn = new List<string>();
        var options = new PangyaUiRenderOptions
        {
            ContextMenuCaptions = new Dictionary<PangyaUiNode, string> { [menu] = string.Empty },
            DrawCaptionText = (_, text, _, _) => drawn.Add(text)
        };
        using var bitmap = new Bitmap(120, 80);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(new Rectangle(5, 5, 36, 18), renderer.GetRenderedBounds(menu, options));
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(10, 10).ToArgb());
        Assert.Empty(drawn);
        Assert.Equal(-1, renderer.HitTestMenuRow(menu, new Point(10, 10), options));
    }

    [Theory]
    [InlineData(true, 40)]
    [InlineData(false, 1)]
    public void UiRenderer_EditRetainsScrollbarWhenTextIsSuppressed(bool hidePrivacy, int width)
    {
        for (int index = 1; index <= 3; index++)
            SaveSolidImage(Path.Combine(_directory, $"scroll_n_{index}.png"), Color.Red, 4, 2);
        string path = Path.Combine(_directory, "private-edit.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="60 60">
              <item type="EDIT" name="edit" rect="5 5 {5 + width} 45">
                <param name="bordercolor" var="ff000000"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode edit = Assert.Single(document.Nodes, node => node.Name == "edit");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawnText = new List<string>();
        var options = new PangyaUiRenderOptions
        {
            HidePrivacy = hidePrivacy,
            PrivateNodes = new HashSet<PangyaUiNode> { edit },
            EditLines = new Dictionary<PangyaUiNode, IReadOnlyList<string>>
            {
                [edit] = new[] { "A", "B", "C", "D", "E" }
            },
            EditTopRows = new Dictionary<PangyaUiNode, int> { [edit] = 0 },
            DrawCaptionText = (_, text, _, _) => drawnText.Add(text)
        };
        using var bitmap = new Bitmap(60, 60);
        using Graphics graphics = Graphics.FromImage(bitmap);

        renderer.Render(graphics, document, form, options);

        Assert.Empty(drawnText);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(5 + width, 6).ToArgb());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UiRenderer_EditTagsCombineStyleAndRawHexColors(bool password)
    {
        string path = Path.Combine(_directory, "mixed-edit-tags.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="80 50">
              <item type="EDIT" name="edit" rect="5 5 75 45">
                <param name="fontcolor" var="ffff0000"/>
                <param name="password" var="{password.ToString().ToLowerInvariant()}"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode edit = Assert.Single(document.Nodes, node => node.Name == "edit");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawn = new List<(string Text, int Color)>();
        var options = new PangyaUiRenderOptions
        {
            EditLines = new Dictionary<PangyaUiNode, IReadOnlyList<string>>
            {
                [edit] = new[] { @"\cT,ff00ff00,ff000000\cA", "B" }
            },
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, text, brush, _) =>
                drawn.Add((text, Assert.IsType<SolidBrush>(brush).Color.ToArgb()))
        };
        using var bitmap = new Bitmap(80, 50);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        string[] expected = password ? new[] { "*", "*", "*" } : new[] { "A", "A", "B" };
        Assert.Equal(expected, drawn.Select(run => run.Text).ToArray());
        Assert.All(drawn, run => Assert.Equal(Color.Lime.ToArgb(), run.Color));
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(0, 1)]
    public void UiRenderer_RuntimeGaugeStepCountControlsSeparators(int suppliedSteps, int effectiveSteps)
    {
        string path = Path.Combine(_directory, "gauge-steps.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="GAUGEBAREX" name="gauge" rect="5 5 25 15">
                <param name="steps" var="4"/>
                <param name="border_thickness" var="0"/>
                <param name="bordercolor" var="00000000"/>
                <param name="color" var="ff00ff00"/>
                <param name="bgcolor" var="ff000000"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode gauge = Assert.Single(document.Nodes, node => node.Name == "gauge");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            GaugeStepCounts = new Dictionary<PangyaUiNode, int> { [gauge] = suppliedSteps },
            GaugeValues = new Dictionary<PangyaUiNode, int> { [gauge] = 100 }
        };
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(4, PangyaUiRenderer.GetExtendedGaugeStepCount(gauge));
        Assert.Equal(effectiveSteps, PangyaUiRenderer.GetExtendedGaugeStepCount(gauge, options));
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(9, 9).ToArgb());
        Assert.Equal((effectiveSteps == 2 ? Color.Black : Color.Lime).ToArgb(),
            bitmap.GetPixel(14, 9).ToArgb());
    }

    [Fact]
    public void UiRenderer_RuntimeViewerSizeUpdatesPixelsViewportAndScrollRange()
    {
        SaveSolidImage(Path.Combine(_directory, "viewer-size.png"), Color.Lime, 40, 60);
        string path = Path.Combine(_directory, "viewer-size.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 60">
              <item type="VIEWER" name="viewer" rect="5 5 25 25">
                <param name="image" var="viewer-size.png"/>
                <param name="use_wheel" var="true"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode viewer = Assert.Single(document.Nodes, node => node.Name == "viewer");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            WindowSizes = new Dictionary<PangyaUiNode, Size> { [viewer] = new(20, 40) },
            ViewerTopRows = new Dictionary<PangyaUiNode, int> { [viewer] = 1 }
        };
        using var bitmap = new Bitmap(60, 60);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(2, renderer.GetViewerMaximumTopRow(viewer));
        Assert.Equal(1, renderer.GetViewerMaximumTopRow(viewer, options: options));
        Assert.Equal(new Rectangle(5, 5, 20, 40), renderer.GetViewerViewport(viewer, options));
        Assert.Equal(new Rectangle(5, 5, 20, 40), renderer.GetRenderedBounds(viewer, options));
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(10, 40).ToArgb());
        Assert.Same(viewer, renderer.HitTest(document, form, new Point(10, 40), options));
        Assert.Null(renderer.HitTest(document, form, new Point(10, 45), options));
    }

    [Fact]
    public void UiRenderer_WindowOffsetsMoveParentsAndChildrenConsistently()
    {
        SaveSolidImage(Path.Combine(_directory, "offset.png"), Color.Lime, 4, 3);
        string path = Path.Combine(_directory, "window-offset.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 40">
              <item type="GROUPBOX" name="group" pos="5 6" size="30 20">
                <item type="AREA" name="image" pos="2 3">
                  <param name="bgimg" var="offset.png"/>
                </item>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode group = Assert.Single(document.Nodes, node => node.Name == "group");
        PangyaUiNode area = Assert.Single(document.Nodes, node => node.Name == "image");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            WindowOffsets = new Dictionary<PangyaUiNode, Point>
            {
                [group] = new(10, 4), [area] = new(3, 2)
            }
        };
        using var bitmap = new Bitmap(60, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(new Rectangle(20, 15, 4, 3), renderer.GetRenderedBounds(area, options));
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(21, 16).ToArgb());
        Assert.NotEqual(Color.Lime.ToArgb(), bitmap.GetPixel(8, 10).ToArgb());
        Assert.Same(area, renderer.HitTest(document, form, new Point(21, 16), options));
        Assert.Null(renderer.HitTest(document, form, new Point(8, 10), options));
    }

    [Fact]
    public void UiRenderer_FrameWithoutLayersDoesNotDrawItsSkin()
    {
        for (int index = 0; index < 9; index++)
            SaveSolidImage(Path.Combine(_directory, $"frame{index:00}.png"), Color.Red, 2, 2);
        string path = Path.Combine(_directory, "frame-without-layers.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="FRAME" name="frame" color="ffffffff">
                <bfrm filename="frame"/>
              </element>
              <element type="FORM" name="form" size="40 30">
                <item type="FRAME" name="instance" resource="frame" rect="5 5 25 25"/>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiResourceCatalog resources = PangyaUiResourceCatalog.Load(new[] { path });
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, resources);
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);

        renderer.Render(graphics, document, form);

        Assert.Equal(Color.FromArgb(46, 49, 55).ToArgb(), bitmap.GetPixel(10, 10).ToArgb());
    }

    [Theory]
    [InlineData(7, true)]
    [InlineData(8, false)]
    public void UiRenderer_FrameSupportsEightLayers(int overlayLayerIndex, bool overlayVisible)
    {
        for (int index = 0; index < 9; index++)
        {
            SaveSolidImage(Path.Combine(_directory, $"base{index:00}.png"), Color.Red, 2, 2);
            SaveSolidImage(Path.Combine(_directory, $"overlay{index:00}.png"), Color.Lime, 2, 2);
        }
        string hiddenLayers = string.Concat(Enumerable.Repeat(
            "<layer type='0' height='20' pos='0' invisible='1'/>", overlayLayerIndex - 1));
        string path = Path.Combine(_directory, "frame-layer-limit.xml");
        File.WriteAllText(path, $$"""
            <resource>
              <element type="FRAME" name="frame" color="ffffffff">
                <layer type="0"/>
                {{hiddenLayers}}
                <layer type="0" height="20" pos="0"/>
                <bfrm filename="base"/>
                <sfrm filename="overlay"/>
              </element>
              <element type="FORM" name="form" size="40 30">
                <item type="FRAME" name="instance" resource="frame" rect="5 5 25 25"/>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiResourceCatalog resources = PangyaUiResourceCatalog.Load(new[] { path });
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, resources);
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);

        renderer.Render(graphics, document, form);

        Assert.Equal((overlayVisible ? Color.Lime : Color.Red).ToArgb(), bitmap.GetPixel(10, 10).ToArgb());
    }

    [Fact]
    public void UiRenderer_FrameUsesFirstMatchingImageDefinition()
    {
        for (int index = 0; index < 9; index++)
        {
            SaveSolidImage(Path.Combine(_directory, $"red{index:00}.png"), Color.Red, 2, 2);
            SaveSolidImage(Path.Combine(_directory, $"green{index:00}.png"), Color.Lime, 2, 2);
        }
        string path = Path.Combine(_directory, "duplicate-frame.xml");
        File.WriteAllText(path, """
            <resource>
              <element type="FRAME" name="frame" color="ffffffff">
                <layer type="0"/>
                <bfrm filename="red"/>
                <bfrm filename="green"/>
              </element>
              <element type="FORM" name="form" size="40 30">
                <item type="FRAME" name="instance" resource="frame" rect="5 5 25 25"/>
              </element>
            </resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiResourceCatalog resources = PangyaUiResourceCatalog.Load(new[] { path });
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images, resources);
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);

        renderer.Render(graphics, document, form);

        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(10, 10).ToArgb());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("missing.png")]
    public void UiRenderer_ButtonRetainsRuntimeSlotImageAndItsSize(string? replacementImage)
    {
        SaveSolidImage(Path.Combine(_directory, "normal.png"), Color.Red, 4, 3);
        SaveSolidImage(Path.Combine(_directory, "selected.png"), Color.Lime, 8, 6);
        string path = Path.Combine(_directory, "runtime-button.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="BUTTON" name="button" pos="5 6">
                <param name="style" var="2"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode button = Assert.Single(document.Nodes, node => node.Name == "button");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            ImageButtonStates = new Dictionary<PangyaUiNode, PangyaUiImageButtonState>
            {
                [button] = new()
                {
                    Status = PangyaUiButtonState.Selected,
                    ReplacementMode = "selected",
                    ReplacementImage = replacementImage,
                    Images = new Dictionary<string, string>
                    {
                        ["normal"] = "normal.png", ["selected"] = "selected.png"
                    }
                }
            }
        };
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(new Rectangle(5, 6, 8, 6), renderer.GetRenderedBounds(button, options));
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(11, 10).ToArgb());
        Assert.Same(button, renderer.HitTest(document, form, new Point(11, 10), options));
    }

    [Fact]
    public void UiRenderer_ComboMarginsContributeToScrollCapacity()
    {
        string path = Path.Combine(_directory, "combo-margin.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="80 100">
              <item type="COMBOBOX" name="combo" rect="5 5 45 25">
                <param name="maxlistnum" var="2"/>
                <param name="topmargin" var="8"/>
                <param name="lineheight" var="16"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode node = Assert.Single(document.Nodes, node => node.Name == "combo");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var combo = new PangyaUiComboPreview(
            new[] { new PangyaUiComboItem("A"), new("B"), new("C"), new("D") },
            IsOpen: true);
        var options = new PangyaUiRenderOptions
        {
            Combos = new Dictionary<PangyaUiNode, PangyaUiComboPreview> { [node] = combo },
            ComboViewportHeight = 100
        };

        Assert.Equal(new Rectangle(5, 27, 40, 48), renderer.GetComboDropdownBounds(node, options));
        Assert.Equal(1, PangyaUiRenderer.GetComboMaximumTopRow(node, combo));
        Assert.Equal(2, renderer.HitTestComboRow(node, new Point(10, 60), options));
        options = options with
        {
            Combos = new Dictionary<PangyaUiNode, PangyaUiComboPreview> { [node] = combo with { TopRow = 99 } }
        };
        Assert.Equal(3, renderer.HitTestComboRow(node, new Point(10, 60), options));
    }

    [Fact]
    public void UiRenderer_EmptyOpenComboRetainsItsOneRowDropdown()
    {
        string path = Path.Combine(_directory, "empty-combo.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 60">
              <item type="COMBOBOX" name="combo" rect="5 5 45 25">
                <param name="bgcolor" var="ff00ff00"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode combo = Assert.Single(document.Nodes, node => node.Name == "combo");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            Combos = new Dictionary<PangyaUiNode, PangyaUiComboPreview>
            {
                [combo] = new(Array.Empty<PangyaUiComboItem>(), IsOpen: true)
            },
            ComboViewportHeight = 60
        };
        using var bitmap = new Bitmap(60, 60);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);

        Assert.Equal(new Rectangle(5, 27, 40, 16), renderer.GetComboDropdownBounds(combo, options));
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(10, 30).ToArgb());
        Assert.Equal(-1, renderer.HitTestComboRow(combo, new Point(10, 30), options));
    }

    [Fact]
    public void UiRenderer_RuntimeEditKeyEventsEnableCaretAtReadOnlyAlignment()
    {
        string path = Path.Combine(_directory, "read-only-caret.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 40">
              <item type="EDIT" name="edit" rect="5 5 45 25">
                <param name="readonly" var="true"/>
                <param name="align" var="1"/>
                <param name="leftmargin" var="2"/>
                <param name="caretcolor" var="ffff0000"/>
                <param name="caretcolor2" var="ffff0000"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode edit = Assert.Single(document.Nodes, node => node.Name == "edit");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var options = new PangyaUiRenderOptions
        {
            EditLines = new Dictionary<PangyaUiNode, IReadOnlyList<string>> { [edit] = new[] { "AB" } },
            EditCarets = new Dictionary<PangyaUiNode, PangyaUiEditCaret>
            {
                [edit] = new(0, 1, TimeSpan.FromMilliseconds(100))
            },
            EditKeyEvents = new Dictionary<PangyaUiNode, bool> { [edit] = true },
            CaptionFontHeight = 8,
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, _, _, _) => { }
        };
        using var bitmap = new Bitmap(60, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(25, 8).ToArgb());
        Assert.NotEqual(Color.Red.ToArgb(), bitmap.GetPixel(31, 8).ToArgb());

        renderer.Render(graphics, document, form,
            options with { EditKeyEvents = new Dictionary<PangyaUiNode, bool> { [edit] = false } });
        Assert.NotEqual(Color.Red.ToArgb(), bitmap.GetPixel(25, 8).ToArgb());
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    public void UiRenderer_EditLineStyleDoesNotLeakFromPreviousInlineTags(bool overrideStyle, int secondLineDraws)
    {
        string path = Path.Combine(_directory, "edit-styles.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="80 50">
              <item type="EDIT" name="edit" rect="5 5 75 45"/>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode edit = Assert.Single(document.Nodes, node => node.Name == "edit");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawn = new List<string>();
        var options = new PangyaUiRenderOptions
        {
            EditLines = new Dictionary<PangyaUiNode, IReadOnlyList<string>>
            {
                [edit] = new[] { @"\cO\cA", "B" }
            },
            EditLineStyles = overrideStyle
                ? new Dictionary<PangyaUiNode, IReadOnlyList<int>> { [edit] = new[] { 0, 1 } }
                : null,
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, text, _, _) => drawn.Add(text)
        };
        using var bitmap = new Bitmap(80, 50);
        using Graphics graphics = Graphics.FromImage(bitmap);

        renderer.Render(graphics, document, form, options);

        Assert.Equal(9, drawn.Count(text => text == "A"));
        Assert.Equal(secondLineDraws, drawn.Count(text => text == "B"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void UiRenderer_AreaBitmapOverrideHonorsRuntimeClipping(bool xmlClipping, bool runtimeClipping)
    {
        SaveSolidImage(Path.Combine(_directory, "original.png"), Color.Red, 4, 3);
        string path = Path.Combine(_directory, "bitmap-area.xml");
        File.WriteAllText(path, $"""
            <resource><element type="FORM" name="form" size="40 30">
              <item type="AREA" name="image" pos="5 6" size="20 20">
                <param name="bgimg" var="original.png"/>
                <param name="enable_clip" var="{(xmlClipping ? 1 : 0)}"/>
                <param name="clip_area" var="6 7 10 10"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode area = Assert.Single(document.Nodes, node => node.Name == "image");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var replacement = new Bitmap(8, 6);
        using (Graphics replacementGraphics = Graphics.FromImage(replacement))
            replacementGraphics.Clear(Color.Lime);
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form);
        Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(7, 8).ToArgb());

        var options = new PangyaUiRenderOptions
        {
            AreaBitmapImages = new Dictionary<PangyaUiNode, Image> { [area] = replacement },
            AreaClippingEnabled = new Dictionary<PangyaUiNode, bool> { [area] = runtimeClipping },
            AreaClipOverrides = new Dictionary<PangyaUiNode, Rectangle> { [area] = new(6, 7, 4, 3) }
        };
        renderer.Render(graphics, document, form, options);
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(9, 9).ToArgb());
        Assert.Equal(!runtimeClipping, bitmap.GetPixel(10, 9).ToArgb() == Color.Lime.ToArgb());
        Assert.Equal(runtimeClipping ? new Rectangle(6, 7, 4, 3) : new Rectangle(5, 6, 8, 6),
            renderer.GetRenderedBounds(area, options));
        Assert.Same(area, renderer.HitTest(document, form, new Point(9, 9), options));
        Assert.Same(runtimeClipping ? null : area,
            renderer.HitTest(document, form, new Point(10, 9), options));
    }

    [Fact]
    public void UiRenderer_RuntimeTextButtonCaptionUpdatesDrawingBoundsAndSegments()
    {
        SaveSolidImage(Path.Combine(_directory, "fallback.png"), Color.Magenta, 6, 4);
        string path = Path.Combine(_directory, "text-button.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="60 40">
              <item type="TEXTBUTTON" name="button" caption="XML" resource="fallback.png" pos="5 6" size="40 12">
                <param name="center" var="false"/>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode button = Assert.Single(document.Nodes, node => node.Name == "button");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        var drawn = new List<string>();
        var options = new PangyaUiRenderOptions
        {
            TextButtonCaptions = new Dictionary<PangyaUiNode, string> { [button] = "AA|B" },
            MeasureCaptionText = text => text.Length * 6,
            DrawCaptionText = (_, text, _, _) => drawn.Add(text)
        };

        Assert.Equal(new Rectangle(5, 6, 24, 12), renderer.GetRenderedBounds(button, options));
        Assert.Equal(0, renderer.HitTestTextButtonSegment(button, new Point(19, 10), options));
        Assert.Equal(1, renderer.HitTestTextButtonSegment(button, new Point(24, 10), options));
        Assert.Same(button, renderer.HitTest(document, form, new Point(24, 10), options));
        Assert.Null(renderer.HitTest(document, form, new Point(35, 10), options));
        using var bitmap = new Bitmap(60, 40);
        using Graphics graphics = Graphics.FromImage(bitmap);
        renderer.Render(graphics, document, form, options);
        Assert.Equal(new[] { "AA", "|", "B" }, drawn.Distinct().ToArray());

        options = options with
        {
            TextButtonCaptions = new Dictionary<PangyaUiNode, string> { [button] = string.Empty }
        };
        drawn.Clear();
        renderer.Render(graphics, document, form, options);
        Assert.Empty(drawn);
        Assert.NotEqual(Color.Magenta.ToArgb(), bitmap.GetPixel(6, 7).ToArgb());
        Assert.Equal(Rectangle.Empty, renderer.GetRenderedBounds(button, options));
        Assert.Null(renderer.HitTest(document, form, new Point(24, 10), options));
    }

    [Fact]
    public void UiRenderer_RuntimeVisibilityControlsDescendantDrawingAndHitTesting()
    {
        string ui = Path.Combine(_directory, "ui");
        Directory.CreateDirectory(ui);
        SaveSolidImage(Path.Combine(ui, "visible.png"), Color.Lime, 6, 4);
        string path = Path.Combine(ui, "visibility.xml");
        File.WriteAllText(path, """
            <resource><element type="FORM" name="form" size="40 30">
              <item type="GROUPBOX" name="group" pos="5 6" size="20 20">
                <param name="visible" var="0"/>
                <item type="AREA" name="image" pos="2 3">
                  <param name="bgimg" var="visible.png"/>
                </item>
              </item>
            </element></resource>
            """);
        PangyaUiDocument document = PangyaUiDocument.Load(path);
        PangyaUiNode form = Assert.Single(document.Nodes, node => node.IsForm);
        PangyaUiNode group = Assert.Single(document.Nodes, node => node.Name == "group");
        PangyaUiNode image = Assert.Single(document.Nodes, node => node.Name == "image");
        using var images = new PangyaFileImageProvider(_directory);
        var renderer = new PangyaUiRenderer(images);
        using var bitmap = new Bitmap(40, 30);
        using Graphics graphics = Graphics.FromImage(bitmap);
        var point = new Point(9, 10);

        renderer.Render(graphics, document, form);
        Assert.NotEqual(Color.Lime.ToArgb(), bitmap.GetPixel(point.X, point.Y).ToArgb());
        Assert.Equal(Rectangle.Empty, renderer.GetRenderedBounds(image));
        Assert.Null(renderer.HitTest(document, form, point));

        var shown = new PangyaUiRenderOptions
        {
            VisibleNodes = new HashSet<PangyaUiNode> { group }
        };
        renderer.Render(graphics, document, form, shown);
        Assert.Equal(Color.Lime.ToArgb(), bitmap.GetPixel(point.X, point.Y).ToArgb());
        Assert.Equal(new Rectangle(7, 9, 6, 4), renderer.GetRenderedBounds(image, shown));
        Assert.Same(image, renderer.HitTest(document, form, point, shown));

        var hidden = shown with { HiddenNodes = new HashSet<PangyaUiNode> { group } };
        renderer.Render(graphics, document, form, hidden);
        Assert.NotEqual(Color.Lime.ToArgb(), bitmap.GetPixel(point.X, point.Y).ToArgb());
        Assert.Equal(Rectangle.Empty, renderer.GetRenderedBounds(image, hidden));
        Assert.Null(renderer.HitTest(document, form, point, hidden));
    }

    [Theory]
    [InlineData("pos='5 7'", 5, 7, 10, 10)]
    [InlineData("pos='5 7' size='0 0'", 5, 7, 0, 0)]
    [InlineData("pos='5 7' size='20 30'", 5, 7, 20, 30)]
    [InlineData("pos='5 7' rect='1 2 21 32'", 5, 7, 10, 10)]
    [InlineData("position='5 7'", 5, 7, 0, 0)]
    [InlineData("pos='invalid'", 0, 0, 0, 0)]
    [InlineData("rect='256 20 192 256'", 256, 20, -64, 236)]
    [InlineData("rect='0 276 256 61'", 0, 276, 256, -215)]
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
            Assert.Single(checkBoxes, checkBox => checkBox.Text == "_JAPAN_").Checked = true;
            typeof(FrmPangyaUiEditor).GetMethod("ConfigureIfdefControls",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(form, [document]);
            CheckBox[] refreshed = toolbar.Items.OfType<ToolStripControlHost>()
                .Select(host => host.Control).OfType<CheckBox>().ToArray();
            Assert.True(Assert.Single(refreshed, checkBox => checkBox.Text == "_JAPAN_").Checked);
            Assert.False(Assert.Single(refreshed, checkBox => checkBox.Text == "_DETAIL_").Checked);
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
                <base width="80" height="40" color="FFFF0000" background="{(useImage ? "green.png" : "")}"/>
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
                <layer type="0"/>
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
