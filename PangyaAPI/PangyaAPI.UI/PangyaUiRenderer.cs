using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using System.Xml;

namespace PangyaAPI.UI;

public sealed record PangyaUiCursorPreview(Point Position, int Index, int Frame = 0);

public sealed record PangyaUiTab(string Normal, string? Hover = null, string? Selected = null,
    PangyaUiButtonState State = PangyaUiButtonState.Normal, Size? Size = null);

public sealed record PangyaUiListPreview(int ItemCount, int TopRow,
    Action<Graphics, int, Rectangle> DrawItem);

public sealed record PangyaUiMenuState(int SelectedRow = -1,
    IReadOnlySet<int>? DisabledRows = null, byte SelectionAlpha = byte.MaxValue);

public sealed record PangyaUiImageButtonState(bool Enabled = true,
    double SecondsSincePress = double.PositiveInfinity, bool Blink = false);

public sealed record PangyaUiGaugeRange(int Lower, int Upper, int Origin = 0);
public sealed record PangyaUiComboItem(string Text, string? Image = null);
public sealed record PangyaUiEditCaret(int Line, int Column, TimeSpan Elapsed);
public enum PangyaUiComboDirection
{
    Auto,
    Above,
    Below
}

public sealed record PangyaUiComboPreview(IReadOnlyList<PangyaUiComboItem> Items, bool IsOpen = false,
    int SelectedIndex = -1, int HoveredIndex = -1, int TopRow = 0,
    PangyaUiComboDirection Direction = PangyaUiComboDirection.Auto);
public sealed record PangyaUiGaugeTransition(int From, int To, TimeSpan Elapsed, double Speed = 65,
    bool Achilles = false);

public sealed record PangyaUiRenderOptions(
    PangyaUiButtonState ButtonState = PangyaUiButtonState.Normal,
    IReadOnlySet<string>? EnabledSymbols = null,
    bool ShowDebugBounds = false,
    PangyaUiNode? SelectedNode = null,
    float StrokeScale = 1f)
{
    public IReadOnlyDictionary<PangyaUiNode, int>? ViewerTopRows { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, string>? ViewerImages { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? ViewerScrollRows { get; init; }
    public IReadOnlySet<PangyaUiNode>? DisabledNodes { get; init; }
    public IReadOnlySet<PangyaUiNode>? TopmostNodes { get; init; }
    public IReadOnlySet<PangyaUiNode>? PrivateNodes { get; init; }
    public bool HidePrivacy { get; init; }
    public string? StartupWallpaper { get; init; }
    public Image? StartupWallpaperImage { get; init; }
    public Color DesktopWallpaperTint { get; init; } = Color.White;
    public bool DesktopWallpaperVisible { get; init; } = true;
    public bool ShowFrameBlink { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, TimeSpan>? FrameBlinkElapsed { get; init; }
    public bool IsViewFocused { get; init; } = true;
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiImageButtonState>? ImageButtonStates { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, long>? ButtonPressTicks { get; init; }
    public long CurrentTick { get; init; }
    public PangyaUiNode? PressedButton { get; init; }
    public IReadOnlySet<PangyaUiNode>? LatchedButtons { get; init; }
    public PangyaUiNode? HoveredNode { get; init; }
    public Point? Pointer { get; init; }
    public bool ShowTooltip { get; init; }
    public PangyaUiCursorPreview? CursorPreview { get; init; }
    public int TooltipStyle { get; init; }
    public byte TooltipAlpha { get; init; } = 220;
    public PangyaUiNode? TooltipNode { get; init; }
    public Point? TooltipPointer { get; init; }
    public TimeSpan LayoutAnimationTime { get; init; }
    public TimeSpan? EmoticonAnimationTime { get; init; }
    public TimeSpan? EditEmoticonAnimationTime { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlyList<long>>? EditLineAnimationStartTicks { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? CaptionScrollOffsets { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiMenuState>? MenuStates { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlyList<string>>? EditLines { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlySet<int>>? JustifiedEditLines { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiEditCaret>? EditCarets { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? EditTopRows { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? GaugeValues { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? ExtendedGaugeSteps { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? GaugeExpansions { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiGaugeRange>? GaugeRanges { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiComboPreview>? Combos { get; init; }
    public int ComboViewportHeight { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiGaugeTransition>? GaugeTransitions { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlyList<PangyaUiTab>>? Tabs { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiListPreview>? Lists { get; init; }
}

public sealed class PangyaUiRenderer
{
    private const int ViewerRowHeight = 20;
    private readonly IPangyaImageProvider _images;
    private readonly PangyaUiResourceCatalog _resources;

    public PangyaUiRenderer(IPangyaImageProvider images,
        PangyaUiResourceCatalog? resources = null)
    {
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _resources = resources ?? PangyaUiResourceCatalog.Empty;
    }

    public void Render(Graphics graphics, PangyaUiDocument document,
        PangyaUiNode? selectedForm = null, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(document);
        options ??= new PangyaUiRenderOptions();
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        using var canvasBrush = new SolidBrush(Color.FromArgb(46, 49, 55));
        Size canvasSize = selectedForm?.Bounds.Size is { Width: > 0, Height: > 0 } size
            ? size
            : document.CanvasSize;
        if (options.ComboViewportHeight <= 0)
            options = options with { ComboViewportHeight = canvasSize.Height };
        graphics.FillRectangle(canvasBrush, new Rectangle(Point.Empty, canvasSize));
        if (options.DesktopWallpaperVisible && selectedForm?.IsLayout != true &&
            GetStartupWallpaper(options, Symbols(options)) is Image wallpaper)
            DrawDesktopWallpaper(graphics, wallpaper, new Rectangle(Point.Empty, canvasSize), options);

        var dropdowns = new List<(PangyaUiNode Node, Rectangle Bounds, PangyaUiComboPreview Combo)>();
        var topmost = new List<(PangyaUiNode Node, Point Offset, HashSet<string> Chain)>();
        foreach (PangyaUiNode node in GetRenderOrder(document, selectedForm,
                     options.SelectedNode, options.EnabledSymbols, options.TopmostNodes))
        {
            DrawNode(graphics, node, GetParentOffset(node), options,
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), dropdowns, topmost);
            if (options.ShowDebugBounds)
                DrawDebugBounds(graphics, node, GetAbsoluteBounds(node), options.StrokeScale);
        }
        for (int index = 0; index < topmost.Count; index++)
        {
            var entry = topmost[index];
            DrawNode(graphics, entry.Node, entry.Offset, options, entry.Chain, dropdowns, topmost,
                drawTopmost: true);
        }
        foreach (var dropdown in dropdowns)
            DrawComboList(graphics, dropdown.Node, dropdown.Bounds, dropdown.Combo, Symbols(options), options);

        if (options.SelectedNode is not null)
        {
            Rectangle selectedBounds = GetRenderedBounds(options.SelectedNode, options);
            if (selectedBounds.Width <= 0 || selectedBounds.Height <= 0)
                selectedBounds = GetAbsoluteBounds(options.SelectedNode);
            DrawOutline(graphics, selectedBounds, Color.Orange, 2f * options.StrokeScale);
        }
        if (options.ShowTooltip && options.TooltipAlpha > 0 &&
            (options.TooltipPointer ?? options.Pointer) is Point pointer &&
            (options.TooltipNode ?? options.HoveredNode)?.Parameters.GetValueOrDefault("tooltip")
                is { Length: > 0 } tooltip)
            DrawTooltip(graphics, tooltip, pointer, canvasSize, options);
        if (options.CursorPreview is { } cursor) DrawCursor(graphics, cursor, Symbols(options));
    }

    public bool CanDrawCursor(int index, IReadOnlySet<string> symbols) => GetCursorSprite(index, symbols) is not null;

    private Image? GetCursorSprite(int index, IReadOnlySet<string> symbols)
    {
        ReadOnlySpan<string> suffixes = ["1", "2", "3", "4", "_up", "_down", "_zoom", "_both",
            "_move", "_move_no", "_rotate", "_rotate_no", "_caret", "_context", "_rotate2"];
        if ((uint)index >= (uint)suffixes.Length) return null;
        Image? sprite = GetBitmap("[s_pointer" + suffixes[index] + ".png", symbols);
        return sprite is { Width: >= 32, Height: >= 32 } ? sprite : null;
    }

    private void DrawCursor(Graphics graphics, PangyaUiCursorPreview cursor, IReadOnlySet<string> symbols)
    {
        if (GetCursorSprite(cursor.Index, symbols) is not Image sprite) return;
        const int size = 32;
        int frame = Math.Max(0, cursor.Frame) % (sprite.Width / size);
        int offset = cursor.Index switch { 4 => -4, 5 => -28, _ => 0 };
        var destination = new Rectangle(cursor.Position.X, ClampCoordinate((long)cursor.Position.Y + offset), size, size);
        graphics.DrawImage(sprite, destination, new Rectangle(frame * size, 0, size, size), GraphicsUnit.Pixel);
    }

    private void DrawTooltip(Graphics graphics, string text, Point pointer, Size canvas,
        PangyaUiRenderOptions options)
    {
        const int cap = 8;
        IReadOnlySet<string> symbols = Symbols(options);
        int style = Math.Clamp(options.TooltipStyle, 0, 3);
        byte alpha = options.TooltipAlpha;
        using var font = new Font(SystemFonts.DefaultFont.FontFamily, 11, GraphicsUnit.Pixel);
        SizeF measured = graphics.MeasureString(CaptionTags.Replace(text, ""), font,
            Math.Max(1, canvas.Width - cap * 2));
        int width = Math.Min(canvas.Width, (int)Math.Ceiling(measured.Width) + cap * 2);
        int height = Math.Min(canvas.Height, Math.Max(cap * 2, (int)Math.Ceiling(measured.Height) + cap));
        int x = (int)Math.Clamp((long)pointer.X + 20, 0, canvas.Width - width);
        int y = (int)Math.Clamp((long)pointer.Y + 30, 0, canvas.Height - height);
        var bounds = new Rectangle(x, y, width, height);
        Image?[] skin = Enumerable.Range(0, 9)
            .Select(index => GetBitmap(FormattableString.Invariant($"tooltip_frm{style:00}_{index:00}"), symbols))
            .ToArray();
        if (skin.All(image => image is null))
        {
            using var background = new SolidBrush(Color.FromArgb(alpha, 255, 255, 225));
            graphics.FillRectangle(background, bounds);
        }
        int[] widths = [Math.Min(cap, width), Math.Max(0, width - cap * 2), Math.Min(cap, Math.Max(0, width - cap))];
        int[] heights = [Math.Min(cap, height), Math.Max(0, height - cap * 2), Math.Min(cap, Math.Max(0, height - cap))];
        int left = x;
        for (int column = 0; column < 3; column++)
        {
            int top = y;
            for (int row = 0; row < 3; row++)
            {
                if (skin[column * 3 + row] is Image slice && widths[column] > 0 && heights[row] > 0)
                    DrawTintedImage(graphics, slice, new Rectangle(left, top, widths[column], heights[row]),
                        0, slice.Width, slice.Height, Color.FromArgb(alpha, Color.White));
                top += heights[row];
            }
            left += widths[column];
        }
        using var foreground = new SolidBrush(Color.FromArgb(alpha, Color.Black));
        if (!CaptionTags.IsMatch(text))
        {
            graphics.DrawString(text, font, foreground, new RectangleF(x + cap, y + 5,
                Math.Max(0, width - cap * 2), Math.Max(0, height - 5)));
            return;
        }
        using var outline = new SolidBrush(Color.FromArgb(alpha, Color.White));
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        GraphicsState state = graphics.Save();
        try
        {
            graphics.SetClip(bounds, CombineMode.Intersect);
            DrawTaggedCaption(graphics, text, foreground, outline, new PointF(x + cap, y + 5),
                format, 0, font, alpha);
        }
        finally { graphics.Restore(state); }
    }

    public IReadOnlyList<PangyaUiNode> GetRenderOrder(PangyaUiDocument document,
        PangyaUiNode? selectedForm = null, PangyaUiNode? selectedNode = null,
        IReadOnlySet<string>? enabledSymbols = null, IReadOnlySet<PangyaUiNode>? topmostNodes = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        IReadOnlyList<PangyaUiNode> visible = selectedForm is null
            ? []
            : document.GetNodesForForm(selectedForm, enabledSymbols)
                .Where(node => HasVisibleParents(node, selectedForm)).ToArray();
        if (selectedNode is not null && !selectedNode.IsForm && visible.Contains(selectedNode))
            visible = [.. visible.Where(node => !ReferenceEquals(node, selectedNode)), selectedNode];
        if (topmostNodes is null || topmostNodes.Count == 0) return visible;
        return visible.OrderBy(node => IsTopmost(node, topmostNodes)).ToArray();
    }

    private static bool IsTopmost(PangyaUiNode node, IReadOnlySet<PangyaUiNode> topmostNodes)
    {
        for (PangyaUiNode? current = node; current is not null; current = current.Parent)
            if (topmostNodes.Contains(current)) return true;
        return false;
    }

    private static bool HasVisibleParents(PangyaUiNode node, PangyaUiNode root)
    {
        if (ReferenceEquals(node, root)) return true;
        for (PangyaUiNode? parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            if (!parent.IsRenderVisible) return false;
            if (ReferenceEquals(parent, root)) break;
        }
        return true;
    }

    public bool HasAnimatedControls(PangyaUiNode root, IReadOnlySet<string> symbols)
    {
        var pending = new Stack<PangyaUiNode>();
        var visited = new HashSet<PangyaUiNode>(ReferenceEqualityComparer.Instance);
        pending.Push(root);
        while (pending.TryPop(out PangyaUiNode? node))
        {
            if (!visited.Add(node) || !node.IsVisible(symbols) || !node.IsRenderVisible) continue;
            if (node.LayoutBase is not null && node.Element["anibg"] is XmlElement animation &&
                PangyaUiDimensionHelper.ParsePoint(animation.GetAttribute("vel")) is Point velocity &&
                !velocity.IsEmpty)
                return true;
            if (_resources.Emoticons is not null && IsEdit(node) &&
                node.Parameters.GetValueOrDefault("readonly") == "true")
                return true;
            if ((HasButtonSkin(node) && !string.IsNullOrWhiteSpace(node.Parameters.GetValueOrDefault("blink"))) ||
                (IsArea(node) && !string.IsNullOrWhiteSpace(node.Parameters.GetValueOrDefault("entrance_effect"))))
                return true;

            foreach (PangyaUiNode child in node.Children) pending.Push(child);
            if (_resources.TryResolve(GetRenderResource(node, PangyaUiButtonState.Normal),
                    node.Type, symbols) is PangyaUiNode definition)
                pending.Push(definition);
        }
        return false;
    }

    public PangyaUiNode? HitTest(PangyaUiDocument document, PangyaUiNode? selectedForm,
        Point point, PangyaUiRenderOptions? options = null)
    {
        options ??= new PangyaUiRenderOptions();
        return GetRenderOrder(document, selectedForm, options.SelectedNode,
                options.EnabledSymbols, options.TopmostNodes)
            .LastOrDefault(node =>
            {
                Rectangle bounds = GetRenderedBounds(node, options);
                return bounds.Width > 0 && bounds.Height > 0 && bounds.Contains(point);
            });
    }

    public Rectangle GetRenderedBounds(PangyaUiNode node,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new PangyaUiRenderOptions();
        IReadOnlySet<string> symbols = Symbols(options);
        Rectangle absoluteBounds = GetAbsoluteBounds(node);
        if (node.IsRenderVisible && node.ContextMenuCaption.Length > 0)
        {
            using var bitmap = new Bitmap(1, 1);
            using Graphics metrics = Graphics.FromImage(bitmap);
            return RenderContextMenu(metrics, node, absoluteBounds.Location, options, draw: false, out _);
        }
        if (node.IsRenderVisible && GetList(node, options) is not null)
            return absoluteBounds;
        if (node.IsRenderVisible && GetTabs(node, options) is { } tabs)
            return RenderTabs(null, node, absoluteBounds.Location, tabs, symbols);
        if (!node.IsRenderVisible || IsTransparent(node, options.ButtonState))
            return options.ShowDebugBounds ? absoluteBounds : Rectangle.Empty;
        if (node.LayoutBase is not null || node.StaticCaption.Length > 0 ||
            node.TextButtonCaption.Length > 0)
            return absoluteBounds;
        if (IsViewer(node))
            return GetViewerImage(node, symbols, options) is not null ? absoluteBounds : Rectangle.Empty;
        if (IsEdit(node)) return GetEditBounds(node, absoluteBounds, symbols);
        if (IsGauge(node)) return absoluteBounds;
        if (IsImageGauge(node))
            return GetGaugeImage(node, "normal", symbols) is Image gaugeSkin
                ? new Rectangle(absoluteBounds.Location, gaugeSkin.Size) : Rectangle.Empty;
        if (HasButtonSkin(node))
            return GetButtonBounds(node, absoluteBounds, symbols);

        string resource = GetRenderResource(node, options.ButtonState);
        PangyaUiNode? definition = _resources.TryResolve(resource, node.Type, symbols);
        if (GetBitmapFilename(definition) is string bitmapFilename)
        {
            resource = bitmapFilename;
            definition = null;
        }
        if (IsMacroItem(node))
        {
            if (definition is null) return Rectangle.Empty;
            Rectangle expanded = GetReferencedBounds(definition, options,
                new HashSet<PangyaUiNode>(ReferenceEqualityComparer.Instance));
            if (!expanded.IsEmpty) expanded.Offset(GetParentOffset(node));
            return expanded;
        }
        if (definition is not null)
        {
            Rectangle bounds = absoluteBounds;
            if (definition.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase))
                return GetBaseFrameBounds(bounds, definition, symbols, node.Parameters.GetValueOrDefault("bgimg"));
            if (definition.IsForm && !definition.IsLayout &&
                definition.Bounds.Size is { Width: > 0, Height: > 0 } formSize)
                return GetFormFrameBounds(definition, new Rectangle(bounds.Location, formSize), options);
            if (bounds.Width > 0 && bounds.Height > 0) return bounds;
            Rectangle referenced = GetReferencedBounds(definition, options,
                new HashSet<PangyaUiNode>(ReferenceEqualityComparer.Instance));
            referenced.Offset(bounds.Location);
            return referenced;
        }
        Image? image = string.IsNullOrWhiteSpace(resource) ? null : _images.GetImage(resource);
        if (image is null) return options.ShowDebugBounds ? absoluteBounds : Rectangle.Empty;
        Rectangle destination = ImageBounds(node, absoluteBounds, image);
        return ApplyAreaEntrance(node, absoluteBounds, image, options, ref destination) ??
            ClipImageBounds(node, absoluteBounds, destination);
    }

    private void DrawNode(Graphics graphics, PangyaUiNode node, Point offset,
        PangyaUiRenderOptions options, HashSet<string> resourceChain,
        List<(PangyaUiNode Node, Rectangle Bounds, PangyaUiComboPreview Combo)> dropdowns,
        List<(PangyaUiNode Node, Point Offset, HashSet<string> Chain)> topmost, bool drawTopmost = false)
    {
        if (!drawTopmost && resourceChain.Count > 0 && options.TopmostNodes?.Contains(node) == true)
        {
            topmost.Add((node, offset, new HashSet<string>(resourceChain, StringComparer.OrdinalIgnoreCase)));
            return;
        }
        if (node.IsRenderVisible && GetList(node, options) is PangyaUiListPreview list)
        {
            var bounds = new Rectangle(ClampCoordinate((long)offset.X + node.Bounds.X),
                ClampCoordinate((long)offset.Y + node.Bounds.Y), node.Bounds.Width, node.Bounds.Height);
            DrawList(graphics, node, bounds, list, options);
            return;
        }
        if (node.IsRenderVisible && GetTabs(node, options) is { } tabs)
        {
            RenderTabs(graphics, node, new Point(ClampCoordinate((long)offset.X + node.Bounds.X),
                ClampCoordinate((long)offset.Y + node.Bounds.Y)), tabs, Symbols(options));
            return;
        }
        if (IsGroupBox(node))
        {
            // The top-level render order already includes inline children.
            if (node.IsRenderVisible && resourceChain.Count > 0)
            {
                Point childOffset = new(ClampCoordinate((long)offset.X + node.Bounds.X),
                    ClampCoordinate((long)offset.Y + node.Bounds.Y));
                foreach (PangyaUiNode child in node.Children.Where(child => child.IsVisible(Symbols(options))))
                    DrawNode(graphics, child, childOffset, options, resourceChain, dropdowns, topmost, drawTopmost);
            }
            return;
        }
        if (!node.IsRenderVisible || IsTransparent(node, options.ButtonState)) return;
        IReadOnlySet<string> symbols = Symbols(options);
        Rectangle nodeBounds = node.Bounds;
        nodeBounds.Offset(offset);
        if (node.ContextMenuCaption.Length > 0)
        {
            RenderContextMenu(graphics, node, nodeBounds.Location, options, draw: true, out _);
            return;
        }
        if (IsGauge(node) || IsImageGauge(node))
        {
            PangyaUiGaugeRange? range = options.GaugeRanges?.GetValueOrDefault(node);
            double value = options.GaugeValues?.GetValueOrDefault(node) ?? 0;
            if (IsExtendedGauge(node) && options.ExtendedGaugeSteps?.TryGetValue(node, out int step) == true)
            {
                int lower = range?.Lower ?? 0;
                int upper = range?.Upper ?? 100;
                value = lower + (upper - (double)lower) * Math.Clamp(step / (double)GetExtendedGaugeStepCount(node), 0, 1);
            }
            DrawGauge(graphics, node, nodeBounds, value, symbols, options,
                enabled: options.DisabledNodes?.Contains(node) != true,
                expansion: options.GaugeExpansions?.GetValueOrDefault(node) ?? 0,
                range: range, transition: options.GaugeTransitions?.GetValueOrDefault(node));
            return;
        }
        if (IsEdit(node))
        {
            Rectangle editBounds = GetEditBounds(node, nodeBounds, symbols);
            PangyaUiComboPreview? combo = IsCombo(node) ? options.Combos?.GetValueOrDefault(node) : null;
            IReadOnlyList<string>? lines = options.EditLines?.GetValueOrDefault(node);
            PangyaUiComboItem? selected = combo is not null && (uint)combo.SelectedIndex < (uint)combo.Items.Count
                ? combo.Items[combo.SelectedIndex] : null;
            bool imageSelection = IsExtendedCombo(node) && node.Parameters.GetValueOrDefault("liststyle") == "1";
            if (lines is null && selected is not null && !imageSelection)
                lines = [selected.Text];
            DrawEdit(graphics, node, editBounds, lines,
                enabled: options.DisabledNodes?.Contains(node) != true,
                caret: options.EditCarets?.GetValueOrDefault(node), options: options);
            if (IsCombo(node) && node.Parameters.ContainsKey("normal"))
            {
                Rectangle buttonBounds = GetComboButtonBounds(node, nodeBounds, editBounds, symbols);
                if (!buttonBounds.IsEmpty)
                {
                    PangyaUiRenderOptions buttonOptions = options;
                    if (options.ButtonState == PangyaUiButtonState.Normal)
                    {
                        if (ReferenceEquals(options.PressedButton, node))
                            buttonOptions = options with { ButtonState = PangyaUiButtonState.Selected };
                        else if (ReferenceEquals(options.HoveredNode, node) &&
                            options.Pointer is Point pointer && buttonBounds.Contains(pointer))
                            buttonOptions = options with { ButtonState = PangyaUiButtonState.Hover };
                    }
                    DrawButton(graphics, node, buttonBounds, buttonOptions);
                    if (imageSelection && selected?.Image is { Length: > 0 } filename &&
                        GetBitmap(filename, symbols) is Image icon)
                        graphics.DrawImage(icon, GetComboIconBounds(node, buttonBounds.Location, editBounds.Width, icon.Size));
                }
            }
            if (combo?.IsOpen == true)
                dropdowns.Add((node, editBounds, combo));
            return;
        }
        if (node.TextButtonCaption is { Length: > 0 } textButton)
        {
            DrawTextButton(graphics, node, nodeBounds, textButton, options);
            return;
        }
        if (HasButtonSkin(node))
        {
            PangyaUiRenderOptions buttonOptions = options;
            if (options.ButtonState == PangyaUiButtonState.Normal)
            {
                if (ReferenceEquals(options.PressedButton, node) ||
                    options.LatchedButtons?.Contains(node) == true)
                    buttonOptions = options with { ButtonState = PangyaUiButtonState.Selected };
                else if (ReferenceEquals(options.HoveredNode, node))
                    buttonOptions = options with { ButtonState = PangyaUiButtonState.Hover };
            }
            DrawButton(graphics, node, GetButtonBounds(node, nodeBounds, symbols), buttonOptions);
            return;
        }
        if (IsViewer(node))
        {
            DrawViewer(graphics, node, nodeBounds, options);
            return;
        }
        if (node.LayoutBase is XmlElement layoutBase)
        {
            using var background = new SolidBrush(ParseColor(layoutBase.GetAttribute("color"),
                Color.FromArgb(46, 49, 55)));
            graphics.FillRectangle(background, nodeBounds);
            Image? layoutImage = node.Name.Equals("LOADING", StringComparison.OrdinalIgnoreCase) &&
                (options.StartupWallpaperImage is not null || !string.IsNullOrEmpty(options.StartupWallpaper))
                ? GetStartupWallpaper(options, symbols)
                : GetBitmap(layoutBase.GetAttribute("background"), symbols);
            if (options.DesktopWallpaperVisible && layoutImage is not null)
                DrawDesktopWallpaper(graphics, layoutImage, nodeBounds, options);
            if (options.DesktopWallpaperVisible && node.Element["anibg"] is XmlElement animation &&
                GetBitmap(animation.GetAttribute("img"), symbols) is Image animationImage)
            {
                Rectangle destination = PangyaUiNode.ParseBounds(animation);
                destination.Offset(nodeBounds.Location);
                int width = Math.Min(destination.Width, animationImage.Width);
                int height = Math.Min(destination.Height, animationImage.Height);
                if (width > 0 && height > 0)
                {
                    var imageBounds = new Rectangle(destination.X, destination.Y, width, height);
                    Rectangle clip = Rectangle.Intersect(imageBounds, nodeBounds);
                    if (clip.Width > 0 && clip.Height > 0)
                    {
                        Point velocity = PangyaUiDimensionHelper.ParsePoint(animation.GetAttribute("vel")) ?? Point.Empty;
                        double elapsed = Math.Max(0, options.LayoutAnimationTime.TotalSeconds);
                        int sourceX = AnimatedSourceOffset(velocity.X, elapsed, animationImage.Width - width);
                        int sourceY = AnimatedSourceOffset(velocity.Y, elapsed, animationImage.Height - height);
                        graphics.DrawImage(animationImage, clip, sourceX + clip.X - imageBounds.X,
                            sourceY + clip.Y - imageBounds.Y, clip.Width, clip.Height, GraphicsUnit.Pixel);
                    }
                }
            }
            return;
        }

        string resource = GetRenderResource(node, options.ButtonState);
        PangyaUiNode? definition = _resources.TryResolve(resource, node.Type, symbols);
        if (GetBitmapFilename(definition) is string bitmapFilename)
        {
            resource = bitmapFilename;
            definition = null;
        }
        if (definition is not null)
        {
            if (!resourceChain.Add(resource)) return;
            if (definition.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase))
                DrawFrame(graphics, nodeBounds, definition, symbols, IsFrameBlinkVisible(node, options),
                    node.Parameters.GetValueOrDefault("bgimg"), node, options.IsViewFocused,
                    hideDescription: IsPrivacyHidden(node, options));
            else
            {
                if (definition.IsForm && !definition.IsLayout)
                {
                    if (definition.Bounds.Size is { Width: > 0, Height: > 0 } formSize)
                        nodeBounds.Size = formSize;
                    string frameResource = definition.GetResource(options.ButtonState);
                    PangyaUiNode? frame = _resources.TryResolve(frameResource, "FRAME", symbols);
                    if (frame?.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) == true)
                        DrawFrame(graphics, nodeBounds, frame, symbols, IsFrameBlinkVisible(node, options),
                            textSource: definition, isViewFocused: options.IsViewFocused,
                            hideDescription: IsPrivacyHidden(node, options) || IsPrivacyHidden(definition, options));
                }
                // Fresh creates macro children directly under the instance's parent.
                Point childOffset = IsMacroItem(node) ? offset : nodeBounds.Location;
                foreach (PangyaUiNode child in definition.Children.Where(child =>
                             child.IsVisible(symbols)))
                    DrawNode(graphics, child, childOffset, options, resourceChain, dropdowns, topmost, drawTopmost);
            }
            resourceChain.Remove(resource);
            return;
        }

        if (IsMacroItem(node)) return;
        Image? image = string.IsNullOrWhiteSpace(resource) ? null : _images.GetImage(resource);
        if (image is not null)
        {
            Rectangle destination = ImageBounds(node, nodeBounds, image);
            Rectangle clip = ApplyAreaEntrance(node, nodeBounds, image, options, ref destination) ??
                ClipImageBounds(node, nodeBounds, destination);
            if (clip.Width <= 0 || clip.Height <= 0) return;
            GraphicsState state = graphics.Save();
            try
            {
                graphics.SetClip(clip, CombineMode.Intersect);
                if (IsArea(node) && node.Parameters.TryGetValue("color", out string? color))
                    DrawTintedImage(graphics, image, destination, 0, image.Width, image.Height,
                        ParseColor(color, Color.White));
                else
                    graphics.DrawImage(image, destination);
            }
            finally { graphics.Restore(state); }
        }
        string caption = node.StaticCaption;
        if (caption.Length > 0 && !IsPrivacyHidden(node, options))
        {
            node.Parameters.TryGetValue("color", out string? color);
            bool enabled = options.DisabledNodes?.Contains(node) != true;
            Color foreground = ParseColor(color, Color.Gray);
            using var brush = new SolidBrush(enabled ? foreground : Color.FromArgb(foreground.A * 2 / 5, foreground));
            using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
            format.Alignment = node.Parameters.GetValueOrDefault("align") switch
            {
                "1" => StringAlignment.Center,
                "2" => StringAlignment.Far,
                _ => StringAlignment.Near
            };
            int.TryParse(node.Parameters.GetValueOrDefault("style"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int style);
            using var outline = new SolidBrush(enabled
                ? ParseColor(node.Parameters.GetValueOrDefault("color2"), Color.White) : Color.White);
            if (IsScrollingCaption(node))
            {
                float width = MeasureCaptionLine(graphics, CaptionTags.Replace(caption, ""),
                    SystemFonts.DefaultFont, format, symbols, options.EmoticonAnimationTime);
                float scroll = Math.Clamp(options.CaptionScrollOffsets?.GetValueOrDefault(node) ?? 0,
                    0, Math.Max(0, width - nodeBounds.Width));
                GraphicsState state = graphics.Save();
                try
                {
                    graphics.SetClip(nodeBounds, CombineMode.Intersect);
                    DrawTaggedCaption(graphics, caption, brush, outline,
                        new PointF(nodeBounds.X - scroll, nodeBounds.Y), format, style,
                        emoticonSymbols: symbols, animationElapsed: options.EmoticonAnimationTime);
                }
                finally { graphics.Restore(state); }
            }
            else
                DrawTaggedCaption(graphics, caption, brush, outline, nodeBounds.Location, format, style,
                    emoticonSymbols: symbols, animationElapsed: options.EmoticonAnimationTime);
        }
    }

    private static readonly System.Text.RegularExpressions.Regex CaptionTags = new(
        @"\\c(?<tag>[nNtToOsS]|0x[0-9a-fA-F]{8}(?:\s*,\s*0x[0-9a-fA-F]{8})?)\\c",
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private static readonly System.Text.RegularExpressions.Regex EmoticonTokens = new(
        @"\((?<name>[^()\r\n]+)\)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    private bool TryGetEmoticon(string name, IReadOnlySet<string>? symbols, out Image? image,
        out Rectangle source, out bool flip, TimeSpan? animationElapsed = null)
    {
        flip = name.Contains('-');
        image = null;
        source = Rectangle.Empty;
        if (symbols is null || _resources.Emoticons is not { } emoticons)
            return false;
        if (!emoticons.TryGetFrame(name, animationElapsed, out string resource, out source))
            return false;
        image = GetBitmap(resource, symbols);
        return image is not null && new Rectangle(Point.Empty, image.Size).Contains(source);
    }

    private static bool HasEditAnimationMarker(ReadOnlySpan<char> text, bool startsWord)
    {
        for (int index = 0; index < text.Length; index++)
        {
            if (text[index] == '\b' && startsWord) return true;
            startsWord = text[index] is ' ' or '\t';
        }
        return false;
    }

    private static float GetEditAlignmentOffset(StringAlignment alignment, float difference) => alignment switch
    {
        StringAlignment.Center => MathF.Truncate(MathF.Truncate(difference) / 2),
        StringAlignment.Far => MathF.Truncate(difference),
        _ => 0
    };

    private static string TrimEditAlignmentSpace(string line) =>
        line.Length > 0 && line[^1] is (' ' or '\t') ? line[..^1] : line;

    private float MeasureCaptionLine(Graphics graphics, string line, Font font, StringFormat format,
        IReadOnlySet<string>? symbols, TimeSpan? animationElapsed = null)
    {
        int start = 0;
        float width = 0;
        foreach (System.Text.RegularExpressions.Match token in EmoticonTokens.Matches(line))
        {
            if (!TryGetEmoticon(token.Groups["name"].Value, symbols, out _, out Rectangle source,
                    out _, animationElapsed)) continue;
            width += graphics.MeasureString(line[start..token.Index], font, PointF.Empty, format).Width + source.Width;
            start = token.Index + token.Length;
        }
        return width + graphics.MeasureString(line[start..], font, PointF.Empty, format).Width;
    }

    private (Color Fill, Color Edge, int Style) DrawTaggedCaption(Graphics graphics, string caption, SolidBrush foreground,
        SolidBrush outline, PointF origin, StringFormat format, int style, Font? font = null,
        byte tagAlpha = byte.MaxValue, IReadOnlySet<string>? emoticonSymbols = null,
        TimeSpan? animationElapsed = null, bool useEditAnimationMarker = false, Color? defaultTagEdge = null, string? editAlignmentText = null, float editAvailableWidth = 0, bool justifyEditLine = false, bool splitEditWords = false)
    {
        font ??= SystemFonts.DefaultFont;
        if (editAlignmentText is null && !useEditAnimationMarker && !CaptionTags.IsMatch(caption) && (emoticonSymbols is null || !EmoticonTokens.IsMatch(caption)))
        {
            DrawStyledText(graphics, caption, foreground, outline, origin, format, style, font);
            return (foreground.Color, outline.Color, style);
        }

        using var runFormat = (StringFormat)format.Clone();
        runFormat.Alignment = StringAlignment.Near;
        runFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        using var fill = new SolidBrush(foreground.Color);
        using var edge = new SolidBrush(outline.Color);
        float y = origin.Y;
        foreach (string line in caption.Replace("\r\n", "\n").Split('\n'))
        {
            string visibleLine = editAlignmentText ?? CaptionTags.Replace(line, "");
            if (useEditAnimationMarker) visibleLine = visibleLine.Replace("\b", "");
            TimeSpan? runAnimation = useEditAnimationMarker ? null : animationElapsed;
            float width = MeasureCaptionLine(graphics, visibleLine, font, runFormat,
                emoticonSymbols, runAnimation);
            float x = editAlignmentText is not null
                ? origin.X + GetEditAlignmentOffset(format.Alignment, editAvailableWidth - width)
                : origin.X - (format.Alignment == StringAlignment.Center ? width / 2 :
                    format.Alignment == StringAlignment.Far ? width : 0);
            int gaps = justifyEditLine && format.Alignment == StringAlignment.Near
                ? visibleLine.Count(character => character is ' ' or '\t') : 0;
            float difference = MathF.Truncate(editAvailableWidth - width);
            float extraSpace = gaps > 0 ? MathF.Truncate(difference / gaps) : 0;
            float remainingSpace = gaps > 0 ? difference % gaps : 0;
            float spaceWidth = splitEditWords
                ? MathF.Truncate(graphics.MeasureString(" ", font, PointF.Empty, runFormat).Width) : 0;
            int start = 0;
            foreach (System.Text.RegularExpressions.Match match in CaptionTags.Matches(line))
            {
                DrawRun(line[start..match.Index]);
                string tag = match.Groups["tag"].Value;
                if (tag.Length == 1)
                    style = char.ToLowerInvariant(tag[0]) switch { 't' => 1, 'o' => 2, 's' => 4, _ => 0 };
                else
                {
                    string[] colors = tag.Split(',');
                    Color fillColor = ParseColor(colors[0], Color.Black);
                    Color edgeColor = colors.Length > 1 ? ParseColor(colors[1], Color.White) : defaultTagEdge ?? Color.White;
                    fill.Color = Color.FromArgb(fillColor.A * tagAlpha / 255, fillColor);
                    edge.Color = Color.FromArgb(edgeColor.A * tagAlpha / 255, edgeColor);
                }
                start = match.Index + match.Length;
            }
            string finalRun = line[start..];
            DrawRun(splitEditWords ? TrimEditAlignmentSpace(finalRun) : finalRun);
            y += font.GetHeight(graphics);

            void DrawRun(string run)
            {
                int start = 0;
                foreach (System.Text.RegularExpressions.Match token in EmoticonTokens.Matches(run))
                {
                    if (splitEditWords && token.ValueSpan.ContainsAny(' ', '\t')) continue;
                    if (useEditAnimationMarker && HasEditAnimationMarker(run.AsSpan(start, token.Index - start), start == 0))
                        runAnimation = animationElapsed;
                    if (!TryGetEmoticon(token.Groups["name"].Value, emoticonSymbols, out Image? image,
                            out Rectangle source, out bool flip, runAnimation) || image is null) continue;
                    DrawPlain(run[start..token.Index]);
                    float top = y + font.GetHeight(graphics) - source.Height;
                    float right = x + source.Width;
                    PointF[] corners = flip ? [new(right, top), new(x, top), new(right, top + source.Height)]
                        : [new(x, top), new(right, top), new(x, top + source.Height)];
                    graphics.DrawImage(image, corners, source, GraphicsUnit.Pixel);
                    x = right;
                    start = token.Index + token.Length;
                }
                if (useEditAnimationMarker && HasEditAnimationMarker(run.AsSpan(start), start == 0))
                    runAnimation = animationElapsed;
                DrawPlain(run[start..]);
            }

            void DrawPlain(string run)
            {
                if (useEditAnimationMarker) run = run.Replace("\b", "");
                if (splitEditWords)
                {
                    int start = 0;
                    for (int index = 0; index < run.Length; index++)
                    {
                        if (run[index] != ' ') continue;
                        DrawTextRun(run[start..index]);
                        x += spaceWidth;
                        if (gaps > 0)
                        {
                            x += extraSpace;
                            if (--gaps == 0) x += remainingSpace;
                        }
                        start = index + 1;
                    }
                    run = run[start..];
                }
                DrawTextRun(run);
            }

            void DrawTextRun(string run)
            {
                if (run.Length == 0) return;
                DrawStyledText(graphics, run, fill, edge, new PointF(x, y), runFormat, style, font);
                float runWidth = graphics.MeasureString(run, font, PointF.Empty, runFormat).Width;
                x += editAlignmentText is not null ? MathF.Truncate(runWidth) : runWidth;
            }
        }
        return (fill.Color, edge.Color, style);
    }

    private static bool IsPrivacyHidden(PangyaUiNode node, PangyaUiRenderOptions options) =>
        options.HidePrivacy && options.PrivateNodes?.Contains(node) == true;

    public static bool IsScrollingCaption(PangyaUiNode node) =>
        node.StaticCaption.Length > 0 && node.Bounds.Width > 0 && node.Bounds.Height > 0 &&
        string.Equals(node.Parameters.GetValueOrDefault("scrolling"), "true", StringComparison.OrdinalIgnoreCase);

    public float GetCaptionScrollLimit(PangyaUiNode node, IReadOnlySet<string> symbols)
    {
        if (!IsScrollingCaption(node)) return 0;
        using var bitmap = new Bitmap(1, 1);
        using Graphics graphics = Graphics.FromImage(bitmap);
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        return Math.Max(0, MeasureCaptionLine(graphics, CaptionTags.Replace(node.StaticCaption, ""),
            SystemFonts.DefaultFont, format, symbols) - node.Bounds.Width);
    }

    private static void DrawStyledText(Graphics graphics, string text, Brush foreground, Brush outline,
        PointF origin, StringFormat format, int style, Font? font = null)
    {
        font ??= SystemFonts.DefaultFont;
        void DrawOffset(Brush brush, int dx, int dy) =>
            graphics.DrawString(text, font, brush, new PointF(origin.X + dx, origin.Y + dy), format);

        switch (style)
        {
            case 1:
                DrawOffset(foreground, 1, 0);
                break;
            case 2:
                for (int y = -1; y <= 1; y++)
                    for (int x = -1; x <= 1; x++)
                        if (x != 0 || y != 0) DrawOffset(outline, x, y);
                break;
            case 4:
                DrawOffset(outline, 1, 1);
                break;
        }
        graphics.DrawString(text, font, foreground, origin, format);
    }

    private static Rectangle? ApplyAreaEntrance(PangyaUiNode node, Rectangle bounds, Image image,
        PangyaUiRenderOptions options, ref Rectangle destination)
    {
        if (!IsArea(node) || node.Parameters.GetValueOrDefault("entrance_effect") is not { Length: > 0 } effect)
            return null;

        double opening = 1;
        double duration = -1;
        string direction = "top";
        foreach (string token in effect.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = token.IndexOf(':');
            if (separator < 0) continue;
            string key = token[..separator].ToLowerInvariant();
            string value = token[(separator + 1)..];
            if (key == "from") direction = value.ToLowerInvariant();
            else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                         out double seconds) && double.IsFinite(seconds))
            {
                if (key == "openingtime") opening = Math.Max(0, seconds);
                if (key == "duration") duration = seconds;
            }
        }

        double elapsed = Math.Max(0, options.LayoutAnimationTime.TotalSeconds);
        double closingElapsed = elapsed - opening - duration;
        if (duration >= 0 && closingElapsed >= 0.2) return Rectangle.Empty;
        double displacement = opening > 0 && elapsed < opening ? 1 - elapsed / opening :
            duration >= 0 && closingElapsed > 0 ? closingElapsed / 0.2 : 0;
        var viewport = new Rectangle(bounds.Location, image.Size);
        long dx = direction is "left" or "right" ? (long)(image.Width * displacement) : 0;
        long dy = direction is "top" or "bottom" ? (long)(image.Height * displacement) : 0;
        destination = new Rectangle(
            ClampCoordinate(bounds.X + (direction == "left" ? -dx : dx)),
            ClampCoordinate(bounds.Y + (direction == "top" ? -dy : dy)),
            image.Width, image.Height);
        return Rectangle.Intersect(viewport, destination);
    }

    private static int AnimatedSourceOffset(int velocity, double elapsed, int travel)
    {
        if (travel <= 0) return 0;
        double offset = (velocity * elapsed) % travel;
        return (int)(offset < 0 ? offset + travel : offset);
    }

    public int HitTestMenuRow(PangyaUiNode node, Point point, PangyaUiRenderOptions options)
    {
        if (!node.IsRenderVisible || node.ContextMenuCaption.Length == 0) return -1;
        using var bitmap = new Bitmap(1, 1);
        using Graphics metrics = Graphics.FromImage(bitmap);
        RenderContextMenu(metrics, node, GetAbsoluteBounds(node).Location,
            options with { HoveredNode = node, Pointer = point }, draw: false, out int row);
        return row;
    }

    private Rectangle RenderContextMenu(Graphics graphics, PangyaUiNode node, Point origin,
        PangyaUiRenderOptions options, bool draw, out int hoveredRow)
    {
        Font font = SystemFonts.DefaultFont;
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        var rows = new List<(string Text, Image? Icon, bool Separator, float Top, float Height)>();
        bool hasIcons = false;
        float width = 0;
        float y = 8;
        foreach (string entry in node.ContextMenuCaption.Split('|'))
        {
            if (entry.Length == 0) break;
            int split = entry.IndexOf('@');
            string label = split < 0 ? entry : entry[..split];
            hasIcons |= split >= 0;
            Image? icon = split >= 0 && split + 1 < entry.Length ? GetBitmap(entry[(split + 1)..], Symbols(options)) : null;
            bool separator = label.Contains("separator", StringComparison.Ordinal) ||
                label.Contains("seperator", StringComparison.Ordinal);
            if (!separator) y += 4;
            float height = separator ? 10 : font.GetHeight(graphics) + 3;
            rows.Add((label, icon, separator, y, height));
            y += height;
            width = Math.Max(width, graphics.MeasureString(label, font, PointF.Empty, format).Width +
                (icon?.Width ?? 0));
        }

        var bounds = new Rectangle(origin, new Size((int)Math.Ceiling(width + 36), (int)Math.Ceiling(y + 10)));
        PangyaUiMenuState? menuState = options.MenuStates?.GetValueOrDefault(node);
        hoveredRow = -1;
        if (ReferenceEquals(options.HoveredNode, node) &&
            options.Pointer is Point pointer && bounds.Contains(pointer))
            hoveredRow = rows.FindIndex(row => !row.Separator &&
                pointer.Y >= origin.Y + row.Top && pointer.Y < origin.Y + row.Top + row.Height);
        if (menuState?.DisabledRows?.Contains(hoveredRow) == true) hoveredRow = -1;
        IReadOnlySet<string> symbols = Symbols(options);
        PangyaUiNode? frame = _resources.TryResolve(GetRenderResource(node, options.ButtonState), "FRAME", symbols);
        if (frame?.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) == true)
        {
            Rectangle frameBounds = DrawFrame(draw ? graphics : null, bounds, frame, symbols, showBlink: false,
                node.Parameters.GetValueOrDefault("bgimg"), contextMenu: true);
            if (!draw) return frameBounds;
        }
        if (!draw) return bounds;
        using var brush = new SolidBrush(ParseColor(node.Parameters.GetValueOrDefault("fontcolor"), Color.Black));
        using var highlight = new SolidBrush(Color.FromArgb(255, 204, 0));
        using var selection = new SolidBrush(Color.FromArgb(menuState?.SelectionAlpha ?? byte.MaxValue,
            255, 204, 0));
        using var disabled = new SolidBrush(Color.FromArgb(204, 204, 204));
        using var pen = new Pen(Color.FromArgb(243, 243, 243));
        for (int index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            float top = origin.Y + row.Top;
            if (row.Separator)
                graphics.DrawLine(pen, origin.X + 3f, top + row.Height / 2,
                    origin.X + bounds.Width - 3f, top + row.Height / 2);
            else
            {
                bool enabled = menuState?.DisabledRows?.Contains(index) != true;
                if (row.Icon is Image icon)
                {
                    var iconBounds = new Rectangle(
                        ClampCoordinate((long)(origin.X + 7.0 - (20.0 - icon.Width) / 2)),
                        ClampCoordinate((long)(top - (row.Height - icon.Height) / 2 - 3)),
                        icon.Width, icon.Height);
                    DrawTintedImage(graphics, icon, iconBounds, 0, icon.Width, icon.Height,
                        enabled ? Color.White : Color.FromArgb(51, Color.White));
                }
                Brush rowBrush = !enabled ? disabled : hoveredRow == index ? highlight :
                    menuState?.SelectedRow == index ? selection : brush;
                graphics.DrawString(row.Text, font, rowBrush,
                    new PointF(origin.X + (hasIcons ? 30f : 10f), top), format);
            }
        }
        return bounds;
    }

    private static PangyaUiListPreview? GetList(PangyaUiNode node, PangyaUiRenderOptions options) =>
        node.Type.Equals("LISTBOX", StringComparison.OrdinalIgnoreCase)
            ? options.Lists?.GetValueOrDefault(node) : null;

    private void DrawList(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        PangyaUiListPreview list, PangyaUiRenderOptions options)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || list.ItemCount <= 0 ||
            PangyaUiDimensionHelper.ParseSize(node.Parameters.GetValueOrDefault("itemsize")) is not Size item ||
            item.Width <= 0 || item.Height <= 0)
            return;

        int columns = int.TryParse(node.Parameters.GetValueOrDefault("column"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Math.Max(1, value) : 1;
        int capacity = bounds.Height / item.Height;
        int totalRows = (list.ItemCount - 1) / columns + 1;
        int topRow = Math.Clamp(list.TopRow, 0, Math.Max(0, totalRows - Math.Max(1, capacity)));
        long first = (long)topRow * columns;
        int rows = Math.Max(1, capacity);
        GraphicsState state = graphics.Save();
        try
        {
            graphics.SetClip(bounds, CombineMode.Intersect);
            for (int row = 0; row < rows && first < list.ItemCount; row++, first += columns)
            {
                for (int column = 0; column < columns && first + column < list.ItemCount &&
                    (long)column * item.Width < bounds.Width; column++)
                {
                    var cell = new Rectangle(ClampCoordinate((long)bounds.X + (long)column * item.Width),
                        ClampCoordinate((long)bounds.Y + (long)row * item.Height), item.Width, item.Height);
                    GraphicsState itemState = graphics.Save();
                    try { list.DrawItem(graphics, (int)(first + column), cell); }
                    finally { graphics.Restore(itemState); }
                }
            }
        }
        finally { graphics.Restore(state); }

        if (!int.TryParse(node.Parameters.GetValueOrDefault("show_scrollbar"),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out int show) || show != 0)
            DrawScrollBar(graphics, bounds, capacity, totalRows, topRow, Symbols(options), showGuide: false,
                focused: options.IsViewFocused && ReferenceEquals(options.HoveredNode, node));
    }

    private static IReadOnlyList<PangyaUiTab>? GetTabs(PangyaUiNode node, PangyaUiRenderOptions options) =>
        node.Type.Equals("TABBUTTON", StringComparison.OrdinalIgnoreCase)
            ? options.Tabs?.GetValueOrDefault(node) : null;

    private Rectangle RenderTabs(Graphics? graphics, PangyaUiNode node, Point origin,
        IReadOnlyList<PangyaUiTab> tabs, IReadOnlySet<string> symbols)
    {
        Image? GetImage(string? filename) =>
            string.IsNullOrWhiteSpace(filename) ? null : GetBitmap(filename, symbols);
        Image? separator = GetImage(node.Parameters.GetValueOrDefault("sepImg"));
        Rectangle? result = null;
        long x = origin.X;
        for (int index = 0; index < tabs.Count; index++)
        {
            PangyaUiTab tab = tabs[index];
            Image? normal = GetImage(tab.Normal);
            Size size = tab.Size ?? normal?.Size ?? Size.Empty;
            if (index > 0 && separator is not null)
            {
                var separatorBounds = new Rectangle(new Point(ClampCoordinate(x), origin.Y), separator.Size);
                graphics?.DrawImage(separator, separatorBounds);
                IncludeBounds(ref result, separatorBounds);
                x += separator.Width;
            }
            if (size.Width <= 0 || size.Height <= 0) continue;
            var bounds = new Rectangle(new Point(ClampCoordinate(x), origin.Y), size);
            Image? variant = GetImage(tab.State switch
            {
                PangyaUiButtonState.Hover => tab.Hover,
                PangyaUiButtonState.Selected => tab.Selected,
                _ => null
            });
            string below = variant is null ? "below_normal" :
                tab.State == PangyaUiButtonState.Selected ? "below_selected" : "below_over";
            if (GetImage(node.Parameters.GetValueOrDefault(below)) is Image background)
                graphics?.DrawImage(background, bounds);
            if ((variant ?? normal) is Image foreground)
                graphics?.DrawImage(foreground, bounds);
            IncludeBounds(ref result, bounds);
            x += size.Width;
        }
        return result ?? Rectangle.Empty;
    }

    private static bool IsGauge(PangyaUiNode node) =>
        node.Type.Equals("GAUGEBAR", StringComparison.OrdinalIgnoreCase) || IsExtendedGauge(node);

    private static bool IsExtendedGauge(PangyaUiNode node) =>
        node.Type.Equals("GAUGEBAREX", StringComparison.OrdinalIgnoreCase);

    public static int GetExtendedGaugeStepCount(PangyaUiNode node) =>
        int.TryParse(node.Parameters.GetValueOrDefault("steps"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int steps) ? Math.Max(1, steps) : 1;

    public int HitTestExtendedGaugeButton(PangyaUiNode node, Point point, IReadOnlySet<string> symbols)
    {
        if (!IsExtendedGauge(node) || !node.IsRenderVisible) return 0;
        Rectangle bounds = GetAbsoluteBounds(node);
        foreach (bool left in new[] { true, false })
            if (node.Parameters.GetValueOrDefault(left ? "left" : "right") == "true" &&
                GetGaugeImage(node, left ? "normal" : "rnormal", symbols) is Image button &&
                GetGaugeButtonBounds(bounds, button.Size, left).Contains(point))
                return left ? -1 : 1;
        return 0;
    }

    private static Rectangle GetGaugeButtonBounds(Rectangle bounds, Size size, bool left)
    {
        int x = left ? ClampCoordinate((long)bounds.X + 2) :
            ClampCoordinate((long)bounds.X + bounds.Width - size.Width - 2);
        int y = ClampCoordinate((long)bounds.Y + ((long)bounds.Height - size.Height + 1) / 2);
        return new Rectangle(new Point(x, y), size);
    }

    private static bool IsImageGauge(PangyaUiNode node) =>
        node.Type.Equals("GAUGEBARIMAGE", StringComparison.OrdinalIgnoreCase);

    private Image? GetGaugeImage(PangyaUiNode node, string key, IReadOnlySet<string> symbols) =>
        node.Parameters.TryGetValue(key, out string? name) ? GetBitmap(name, symbols) : null;

    private void DrawGauge(Graphics graphics, PangyaUiNode node, Rectangle bounds, double value,
        IReadOnlySet<string> symbols, PangyaUiRenderOptions options, bool enabled, int expansion, PangyaUiGaugeRange? range,
        PangyaUiGaugeTransition? transition)
    {
        if (IsImageGauge(node))
        {
            if (node.Parameters.GetValueOrDefault("drawstyle") == "1" ||
                GetGaugeImage(node, "normal", symbols) is not Image skin ||
                GetGaugeImage(node, "mask", symbols) is not Image mask)
                return;
            graphics.DrawImage(skin, new Rectangle(bounds.Location, skin.Size));
            if (transition is not null)
                value = GetGaugeTransitionValue(transition, range, skin.Width);
            int start = (int)(GaugeRatio(value, range, skin.Width) * mask.Width);
            int width = mask.Width - start;
            if (width > 0)
                graphics.DrawImage(mask, new Rectangle(ClampCoordinate((long)bounds.X + start),
                    bounds.Y, width, mask.Height), start, 0, width, mask.Height, GraphicsUnit.Pixel);
            return;
        }

        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        if (transition is not null)
            value = GetGaugeTransitionValue(transition, range, 100);
        using var background = new SolidBrush(ParseColor(node.Parameters.GetValueOrDefault("bgcolor"),
            Color.FromArgb(64, 64, 64)));
        graphics.FillRectangle(background, bounds);
        Rectangle track = bounds;
        bool extended = IsExtendedGauge(node);
        if (extended)
        {
            Color outline = ParseColor(node.Parameters.GetValueOrDefault("bordercolor"), Color.Transparent);
            using var pen = new Pen(outline);
            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            foreach (bool left in new[] { true, false })
            {
                if (node.Parameters.GetValueOrDefault(left ? "left" : "right") != "true") continue;
                Image? button = GetGaugeImage(node, left ? "normal" : "rnormal", symbols);
                int width = button?.Width ?? 0;
                if (button is not null)
                {
                    Rectangle buttonBounds = GetGaugeButtonBounds(bounds, button.Size, left);
                    PangyaUiRenderOptions buttonOptions = options;
                    if (options.ButtonState == PangyaUiButtonState.Normal &&
                        ReferenceEquals(options.HoveredNode, node) &&
                        options.Pointer is Point pointer && buttonBounds.Contains(pointer))
                        buttonOptions = options with { ButtonState = PangyaUiButtonState.Hover };
                    DrawButton(graphics, node, buttonBounds, buttonOptions, rightGaugeButton: !left);
                }
                int reserved = Math.Min(track.Width, width + 2);
                if (left) track.X = ClampCoordinate((long)track.X + reserved);
                track.Width -= reserved;
            }
        }
        int border = int.TryParse(node.Parameters.GetValueOrDefault("border_thickness"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int thickness) ? thickness : 2;
        border = Math.Clamp(border, 0, Math.Min(track.Width, track.Height) / 2);
        Rectangle interior = Rectangle.Inflate(track, -border, -border);
        float ratio = (float)GaugeRatio(value, range, 100);
        float origin = (float)GaugeRatio(range?.Origin ?? 0, range, 100);
        float startOffset = interior.Width * Math.Min(origin, ratio);
        float widthFilled = interior.Width * Math.Abs(ratio - origin);
        if (interior.Width <= 0 || interior.Height <= 0) return;
        Color startColor = ParseColor(node.Parameters.GetValueOrDefault("color"), Color.White);
        var fill = new RectangleF(interior.X + startOffset, interior.Y, widthFilled, interior.Height);
        if (!extended)
        {
            using var foreground = new SolidBrush(startColor);
            double span = (double)(range?.Upper ?? 100) - (range?.Lower ?? 0);
            float change = span > 0 ? (float)(Math.Abs((double)expansion) * bounds.Width / span) : 0;
            float extra = Math.Min(change, expansion < 0 ? widthFilled :
                Math.Max(0, interior.Width - startOffset - widthFilled));
            float mainWidth = expansion < 0 ? widthFilled - extra : widthFilled;
            if (mainWidth > 0)
                graphics.FillRectangle(foreground, fill.X, fill.Y, mainWidth, fill.Height);
            if (extra > 0)
            {
                string colorKey = expansion < 0 ? "excolor2" : "excolor1";
                using var changeBrush = new SolidBrush(ParseColor(node.Parameters.GetValueOrDefault(colorKey),
                    Color.FromArgb(112, 112, 112)));
                graphics.FillRectangle(changeBrush, fill.X + mainWidth, fill.Y, extra, fill.Height);
            }
            return;
        }

        if (widthFilled <= 0) return;
        Color target = ParseColor(node.Parameters.GetValueOrDefault("color2"), startColor);
        int Blend(int start, int end) => (int)(start * (1f - ratio) + end * ratio);
        Color endColor = Color.FromArgb(Blend(startColor.A, target.A), Blend(startColor.R, target.R),
            Blend(startColor.G, target.G), Blend(startColor.B, target.B));
        if (!enabled)
        {
            startColor = DisabledGaugeColor(startColor);
            endColor = DisabledGaugeColor(endColor);
        }
        using var gradient = new LinearGradientBrush(fill, startColor, endColor, LinearGradientMode.Horizontal);
        graphics.FillRectangle(gradient, fill);

        if (int.TryParse(node.Parameters.GetValueOrDefault("steps"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int steps) && steps > 1 &&
            steps - 1 <= interior.Width / 2)
        {
            float pitch = (interior.Width - (steps - 1) * 2f) / steps;
            for (int step = 1; step < steps; step++)
            {
                float offset = step * pitch + (step - 1) * 2f;
                if (offset >= startOffset + widthFilled) break;
                graphics.FillRectangle(background, interior.X + offset, interior.Y, 2f, interior.Height);
            }
        }
    }

    private static double GetGaugeTransitionValue(PangyaUiGaugeTransition transition, PangyaUiGaugeRange? range,
        int defaultUpper)
    {
        int lower = range?.Lower ?? 0;
        int upper = range?.Upper ?? defaultUpper;
        if (upper <= lower) return lower;
        double from = Math.Clamp(transition.From, lower, upper);
        double to = Math.Clamp(transition.To, lower, upper);
        double speed = double.IsFinite(transition.Speed) ? Math.Max(0, transition.Speed) : 65;
        double elapsed = Math.Max(0, transition.Elapsed.TotalSeconds);
        double span = Math.Abs(to - from);
        if (transition.Achilles && span > 0 && elapsed > 0)
        {
            // Replay the per-frame decay at 60 Hz, plus the final partial frame.
            double frames = elapsed * 60;
            double wholeFrames = Math.Floor(frames);
            double fraction = frames - wholeFrames;
            double step = speed / (60 * span);
            double remaining = Math.Pow(Math.Max(0, 1 - step), wholeFrames) *
                Math.Max(0, 1 - step * fraction);
            return to + (from - to) * remaining;
        }
        double distance = Math.Min(span, speed * elapsed);
        return from + Math.Sign(to - from) * distance;
    }

    private static double GaugeRatio(double value, PangyaUiGaugeRange? range, int defaultUpper)
    {
        int lower = range?.Lower ?? 0;
        int upper = range?.Upper ?? defaultUpper;
        return upper > lower ? (Math.Clamp(value, lower, upper) - (double)lower) / (upper - (double)lower) : 0;
    }

    private static Color DisabledGaugeColor(Color color) =>
        Color.FromArgb(80, color.R * 176 / 255, color.G * 176 / 255, color.B * 176 / 255);

    private static bool IsEdit(PangyaUiNode node) =>
        node.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase) || IsCombo(node);

    private static bool IsExtendedCombo(PangyaUiNode node) =>
        node.Type.Equals("COMBOCTLEX", StringComparison.OrdinalIgnoreCase);

    private void DrawComboList(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        PangyaUiComboPreview combo, IReadOnlySet<string> symbols, PangyaUiRenderOptions options)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || combo.Items.Count == 0) return;
        IReadOnlyDictionary<string, string> parameters = node.Parameters;
        int Read(string name, int fallback) => int.TryParse(parameters.GetValueOrDefault(name),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        bool extended = IsExtendedCombo(node);
        var (listBounds, lineHeight, capacity, topRow, margin) =
            GetComboListGeometry(node, bounds, combo, options.ComboViewportHeight);
        int y = listBounds.Y;
        Color background = ParseColor(parameters.GetValueOrDefault("bgcolor"), Color.Transparent);
        Color foreground = ParseColor(parameters.GetValueOrDefault("fontcolor"), Color.Gray);
        int fontStyle = ReadHexTextStyle(parameters.GetValueOrDefault("font"));
        using var back = new SolidBrush(background);
        using var text = new SolidBrush(foreground);
        using var selectedText = new SolidBrush(background);
        using var outline = new SolidBrush(ParseColor(parameters.GetValueOrDefault("fontcolor2"), Color.Silver));
        using var highlight = new SolidBrush(ParseColor(parameters.GetValueOrDefault("selectcolor"),
            Color.FromArgb(unchecked((int)0xff000000) | (~background.ToArgb() & 0xffffff))));
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        GraphicsState state = graphics.Save();
        try
        {
            graphics.SetClip(listBounds, CombineMode.Intersect);
            if (!extended)
            {
                graphics.FillRectangle(back, listBounds);
                using var border = new Pen(ParseColor(parameters.GetValueOrDefault("bordercolor"), Color.Transparent));
                graphics.DrawRectangle(border, listBounds.X, listBounds.Y, listBounds.Width - 1, listBounds.Height - 1);
            }
            for (int row = 0; row < capacity; row++)
            {
                int index = topRow + row;
                int rowY = ClampCoordinate((long)y + row * (long)lineHeight);
                bool hovered = combo.HoveredIndex == index;
                PangyaUiComboItem item = combo.Items[index];
                if (extended)
                {
                    if (GetGaugeImage(node, "normal", symbols) is Image skin)
                        graphics.DrawImage(skin, new Rectangle(new Point(bounds.X, rowY), skin.Size));
                    if (hovered && GetGaugeImage(node, "over", symbols) is Image hover)
                        graphics.DrawImage(hover, new Rectangle(new Point(bounds.X, rowY), hover.Size));
                    if (parameters.GetValueOrDefault("liststyle") == "1" && item.Image is { Length: > 0 } filename)
                    {
                        if (GetBitmap(filename, symbols) is Image icon)
                            graphics.DrawImage(icon, GetComboIconBounds(node,
                                new Point(bounds.X, ClampCoordinate((long)rowY + margin)), bounds.Width, icon.Size));
                        continue;
                    }
                }
                else if (hovered)
                    graphics.FillRectangle(highlight, bounds.X, rowY + margin / 2f, bounds.Width, lineHeight);
                DrawStyledText(graphics, item.Text, hovered && !extended ? selectedText : text, outline,
                    new PointF(bounds.X + (float)Read("leftmargin", 0), rowY + (extended ? 0 : margin)),
                    format, hovered && !extended ? 2 : fontStyle);
            }
            DrawScrollBar(graphics, listBounds, capacity, combo.Items.Count, topRow, symbols, showGuide: false,
                focused: options.IsViewFocused && ReferenceEquals(options.HoveredNode, node));
        }
        finally { graphics.Restore(state); }
    }

    private static Rectangle GetComboIconBounds(PangyaUiNode node, Point origin, int itemWidth, Size icon)
    {
        int.TryParse(node.Parameters.GetValueOrDefault("leftmargin"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int margin);
        long offset = node.Parameters.GetValueOrDefault("align") switch
        {
            "1" => itemWidth / 2L + margin,
            "2" => (long)itemWidth - margin,
            _ => margin
        };
        return new Rectangle(ClampCoordinate(origin.X + offset - icon.Width / 2),
            ClampCoordinate((long)origin.Y + 2), icon.Width, icon.Height);
    }

    public int HitTestComboRow(PangyaUiNode node, Point point, PangyaUiRenderOptions options)
    {
        if (!IsCombo(node) || !node.IsRenderVisible ||
            options.Combos?.GetValueOrDefault(node) is not { IsOpen: true } combo ||
            combo.Items.Count == 0)
            return -1;
        Rectangle edit = GetEditBounds(node, GetAbsoluteBounds(node), Symbols(options));
        if (edit.Width <= 0 || edit.Height <= 0) return -1;
        var (bounds, lineHeight, capacity, topRow, _) =
            GetComboListGeometry(node, edit, combo, options.ComboViewportHeight);
        if (!bounds.Contains(point)) return -1;
        long row = ((long)point.Y - bounds.Y) / lineHeight;
        return row < capacity ? topRow + (int)row : -1;
    }

    public bool HitTestComboUnfold(PangyaUiNode node, Point point, IReadOnlySet<string> symbols)
    {
        if (!IsCombo(node) || !node.IsRenderVisible || !node.Parameters.ContainsKey("normal"))
            return false;
        Rectangle bounds = GetAbsoluteBounds(node);
        return GetComboButtonBounds(node, bounds, GetEditBounds(node, bounds, symbols), symbols)
            .Contains(point);
    }

    private Rectangle GetComboButtonBounds(PangyaUiNode node, Rectangle bounds, Rectangle editBounds,
        IReadOnlySet<string> symbols)
    {
        Rectangle button = GetButtonBounds(node, editBounds, symbols);
        if (button.IsEmpty) return button;
        button.X = ClampCoordinate((long)editBounds.X + editBounds.Width - button.Width);
        button.Y = ClampCoordinate((long)bounds.Y + ((long)bounds.Height - button.Height + 1) / 2);
        return button;
    }

    private static (Rectangle Bounds, int LineHeight, int Capacity, int TopRow, int Margin)
        GetComboListGeometry(PangyaUiNode node, Rectangle bounds, PangyaUiComboPreview combo, int viewportHeight)
    {
        int Read(string name, int fallback) => int.TryParse(node.Parameters.GetValueOrDefault(name),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        bool extended = IsExtendedCombo(node);
        int margin = Math.Clamp(Read("topmargin", 0), 0, 4096);
        int lineHeight = (int)Math.Clamp(extended ? bounds.Height + (long)margin : Read("lineheight", 16),
            1, 8192);
        int capacity = GetComboRowCapacity(node, combo);
        int topRow = Math.Clamp(combo.TopRow, 0, combo.Items.Count - capacity);
        int height = capacity * lineHeight + (extended ? 0 : margin * 2);
        bool above = !extended && (combo.Direction == PangyaUiComboDirection.Above ||
            (combo.Direction == PangyaUiComboDirection.Auto && viewportHeight > 0 && bounds.Y > viewportHeight / 2));
        int y = ClampCoordinate(above ? (long)bounds.Y - height - 2 :
            (long)bounds.Y + bounds.Height + (extended ? margin : 2));
        return (new Rectangle(bounds.X, y, bounds.Width, height), lineHeight, capacity, topRow, margin);
    }

    public static int GetComboMaximumTopRow(PangyaUiNode node, PangyaUiComboPreview combo) =>
        combo.Items.Count - GetComboRowCapacity(node, combo);

    private static int GetComboRowCapacity(PangyaUiNode node, PangyaUiComboPreview combo)
    {
        int limit = int.TryParse(node.Parameters.GetValueOrDefault("maxlistnum"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int value) ? Math.Clamp(value, 1, 256) : 10;
        return Math.Min(combo.Items.Count, limit);
    }

    private static bool IsCombo(PangyaUiNode node) =>
        node.Type.Equals("COMBOBOX", StringComparison.OrdinalIgnoreCase) || IsExtendedCombo(node);

    private Rectangle GetEditBounds(PangyaUiNode node, Rectangle bounds, IReadOnlySet<string> symbols) =>
        IsExtendedCombo(node) && GetGaugeImage(node, "normal", symbols) is Image skin
            ? new Rectangle(bounds.Location, skin.Size) : bounds;

    private static string MaskEditPassword(string text)
    {
        var masked = new StringBuilder(text.Length);
        int start = 0;
        foreach (System.Text.RegularExpressions.Match tag in CaptionTags.Matches(text))
        {
            masked.Append('*', text[start..tag.Index].EnumerateRunes().Count());
            masked.Append(tag.Value);
            start = tag.Index + tag.Length;
        }
        masked.Append('*', text[start..].EnumerateRunes().Count());
        return masked.ToString();
    }

    private void DrawEdit(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        IReadOnlyList<string>? lines, bool enabled, PangyaUiEditCaret? caret, PangyaUiRenderOptions options)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        IReadOnlyDictionary<string, string> parameters = node.Parameters;
        Color background = ParseColor(parameters.GetValueOrDefault("bgcolor"), Color.Transparent, implicitAlpha: false);
        Color border = ParseColor(parameters.GetValueOrDefault("bordercolor"), Color.Transparent, implicitAlpha: false);
        if (background.A > 0)
        {
            using var brush = new SolidBrush(background);
            graphics.FillRectangle(brush, bounds);
        }
        if (border.A > 0)
        {
            using var pen = new Pen(border);
            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }
        if (IsPrivacyHidden(node, options)) return;
        if (caret is not null && (!enabled || parameters.GetValueOrDefault("readonly") == "true" ||
            parameters.GetValueOrDefault("focusoff") == "true"))
            caret = null;
        if (lines is null || lines.Count == 0)
        {
            if (caret is null) return;
            lines = [string.Empty];
        }

        int ReadInt(string name, int fallback) =>
            int.TryParse(parameters.GetValueOrDefault(name), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int value) ? value : fallback;

        int lineHeight = ReadInt("lineheight", 16);
        if (lineHeight <= 0) return;
        int leftMargin = ReadInt("leftmargin", 0);
        int topMargin = ReadInt("topmargin", 0);
        bool multiline = !IsCombo(node) && ReadInt("multiline", 1) != 0;
        int count = multiline ? lines.Count : 1;
        int vertical = ReadInt("valign", string.Equals(parameters.GetValueOrDefault("vcenteralign"),
            "true", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        int capacity = GetEditRowCapacity(bounds.Height, topMargin, lineHeight);
        int maximum = Math.Max(0, count - Math.Max(1, capacity));
        int firstLine = options.EditTopRows?.TryGetValue(node, out int requested) == true
            ? Math.Clamp(requested, 0, maximum)
            : Math.Clamp((caret?.Line ?? 0) - Math.Max(1, capacity) + 1, 0, maximum);
        long remaining = firstLine == 0
            ? Math.Max(0L, (long)bounds.Height - (long)count * lineHeight - topMargin) : 0;
        long y = (long)bounds.Y + topMargin + (vertical == 1 ? remaining / 2 : vertical == 2 ? remaining : 0);
        using var format = new StringFormat(StringFormatFlags.NoWrap)
        {
            Alignment = ReadInt("align", 0) switch
            {
                1 => StringAlignment.Center,
                2 => StringAlignment.Far,
                _ => StringAlignment.Near
            }
        };
        Color foreground = ParseColor(parameters.GetValueOrDefault("fontcolor"), Color.Gray, implicitAlpha: false);
        using var text = new SolidBrush(enabled ? foreground : Color.FromArgb(foreground.A * 2 / 5, foreground));
        using var outline = new SolidBrush(enabled
            ? ParseColor(parameters.GetValueOrDefault("fontcolor2"), Color.Silver, implicitAlpha: false) : Color.White);
        int fontStyle = ReadHexTextStyle(parameters.GetValueOrDefault("font"));
        float left = ClampCoordinate((long)bounds.X + leftMargin);
        bool symmetricMargins = string.Equals(parameters.GetValueOrDefault("symmetry", "true"), "true",
            StringComparison.OrdinalIgnoreCase);
        int scrollbarWidth = multiline ? GetBitmap("scroll_n_1", Symbols(options))?.Width ?? 0 : 0;
        float available = (long)bounds.Width - leftMargin * (symmetricMargins ? 2L : 1L) - scrollbarWidth;
        float anchor = left + GetEditAlignmentOffset(format.Alignment, available);
        bool password = string.Equals(parameters.GetValueOrDefault("password"), "true",
            StringComparison.OrdinalIgnoreCase);
        IReadOnlySet<string>? emoticonSymbols = !password && parameters.GetValueOrDefault("readonly") == "true"
            ? Symbols(options)
            : null;
        GraphicsState state = graphics.Save();
        try
        {
            Rectangle content = border.A > 0
                ? new Rectangle(ClampCoordinate((long)bounds.X + 1), ClampCoordinate((long)bounds.Y + 1),
                    bounds.Width - 1, bounds.Height - 1)
                : bounds;
            if (content.Width <= 0 || content.Height <= 0) return;
            graphics.SetClip(content, CombineMode.Intersect);
            long bottom = (long)bounds.Y + bounds.Height;
            Font font = SystemFonts.DefaultFont;
            float scroll = 0;
            float caretX = anchor;
            if (caret is not null && (uint)caret.Line < (uint)count)
            {
                string originalLine = lines[caret.Line];
                string line = password ? MaskEditPassword(originalLine) : originalLine;
                using var measure = (StringFormat)StringFormat.GenericTypographic.Clone();
                measure.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
                string visibleLine = CaptionTags.Replace(line, "");
                int visibleColumn = Math.Clamp(caret.Column, 0, originalLine.Length);
                string untaggedLine = CaptionTags.Replace(originalLine, "");
                foreach (System.Text.RegularExpressions.Match tag in CaptionTags.Matches(originalLine))
                    visibleColumn -= Math.Clamp(caret.Column - tag.Index, 0, tag.Length);
                if (password)
                    visibleColumn = untaggedLine[..Math.Clamp(visibleColumn, 0, untaggedLine.Length)]
                        .EnumerateRunes().Count();
                visibleColumn = Math.Clamp(visibleColumn, 0, visibleLine.Length);
                float prefixWidth = MeasureCaptionLine(graphics, visibleLine[..visibleColumn],
                    font, measure, emoticonSymbols);
                caretX += prefixWidth;
                float viewportWidth = (long)bounds.Width - leftMargin * 2L;
                scroll = MathF.Truncate(Math.Max(0, prefixWidth - viewportWidth));
            }
            for (int index = firstLine; index < count && y + lineHeight <= bottom; index++, y += lineHeight)
            {
                if (y + lineHeight <= bounds.Y) continue;
                string originalLine = lines[index];
                string line = password ? MaskEditPassword(originalLine) : originalLine.Replace('\t', ' ');
                TimeSpan? animationElapsed = options.EditEmoticonAnimationTime ?? options.EmoticonAnimationTime;
                if (emoticonSymbols is not null &&
                    options.EditLineAnimationStartTicks?.GetValueOrDefault(node) is { } starts && index < starts.Count)
                    animationElapsed = TimeSpan.FromMilliseconds(unchecked((uint)(options.CurrentTick - starts[index])));
                var formatting = DrawTaggedCaption(graphics, line, text, outline,
                    new PointF(left - scroll, ClampCoordinate(y)), format, fontStyle,
                    emoticonSymbols: emoticonSymbols,
                    animationElapsed: animationElapsed,
                    useEditAnimationMarker: emoticonSymbols is not null,
                    editAvailableWidth: available,
                    splitEditWords: !password,
                    justifyEditLine: !password && options.JustifiedEditLines?.GetValueOrDefault(node)?.Contains(index) == true,
                    editAlignmentText: parameters.GetValueOrDefault("readonly") == "true"
                        ? TrimEditAlignmentSpace(CaptionTags.Replace(originalLine, "")) : string.Empty,
                    defaultTagEdge: ParseColor(parameters.GetValueOrDefault("fontcolor2"), Color.Silver, implicitAlpha: false));
                text.Color = formatting.Fill;
                outline.Color = formatting.Edge;
                fontStyle = formatting.Style;
                if (caret?.Line == index)
                {
                    double phase = Math.Max(0, caret.Elapsed.TotalSeconds) % 1;
                    if (phase > 0 && phase < 0.8)
                    {
                        float x = caretX - scroll;
                        float caretHeight = font.GetHeight(graphics);
                        float top = ClampCoordinate(y);
                        using var gradient = new LinearGradientBrush(new RectangleF(x, top, 1, caretHeight),
                            ParseColor(parameters.GetValueOrDefault("caretcolor"), Color.Gray, implicitAlpha: false),
                            ParseColor(parameters.GetValueOrDefault("caretcolor2"), Color.Silver, implicitAlpha: false),
                            LinearGradientMode.Vertical);
                        graphics.FillRectangle(gradient, x, top, 1, caretHeight);
                    }
                }
            }
        }
        finally { graphics.Restore(state); }
        if (!IsCombo(node))
            DrawScrollBar(graphics, bounds, capacity, count, firstLine, Symbols(options), showGuide: false,
                focused: options.IsViewFocused && ReferenceEquals(options.HoveredNode, node));
    }

    private static int GetEditRowCapacity(int height, int margin, int lineHeight) =>
        lineHeight <= 0 ? 0 : (int)Math.Clamp(((long)height - margin) / lineHeight, 0, int.MaxValue);

    public static int GetEditMaximumTopRow(PangyaUiNode node, int lineCount)
    {
        if (!node.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase) ||
            node.Parameters.GetValueOrDefault("multiline") == "0") return 0;
        int Read(string name, int fallback) => int.TryParse(node.Parameters.GetValueOrDefault(name),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        return Math.Max(0, lineCount - Math.Max(1,
            GetEditRowCapacity(node.Bounds.Height, Read("topmargin", 0), Read("lineheight", 16))));
    }

    private static int ReadHexTextStyle(string? value, int fallback = 0)
    {
        string hex = value?.Trim() ?? string.Empty;
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        return int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int style)
            ? style : fallback;
    }

    public int HitTestTextButtonSegment(PangyaUiNode node, Point point, PangyaUiRenderOptions options)
    {
        Rectangle bounds = GetAbsoluteBounds(node);
        if (!node.IsRenderVisible || !bounds.Contains(point) || node.TextButtonCaption.Length == 0)
            return -1;
        using var bitmap = new Bitmap(1, 1);
        using Graphics metrics = Graphics.FromImage(bitmap);
        return DrawTextButton(metrics, node, bounds, node.TextButtonCaption,
            options with { HoveredNode = node, Pointer = point }, draw: false);
    }

    private static int DrawTextButton(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        string caption, PangyaUiRenderOptions options, bool draw = true)
    {
        Font font = SystemFonts.DefaultFont;
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        string[] segments = caption.Split('|');
        float x = bounds.X;
        if (!node.Parameters.TryGetValue("center", out string? center) ||
            center.Equals("true", StringComparison.OrdinalIgnoreCase))
            x += (int)((bounds.Width - graphics.MeasureString(caption, font, PointF.Empty, format).Width + 1) / 2);

        using var normal = new SolidBrush(ParseColor(node.Parameters.GetValueOrDefault("color"), Color.White));
        using var highlight = new SolidBrush(ParseColor(
            node.Parameters.GetValueOrDefault("select_color"), Color.FromArgb(255, 204, 0)));
        using var outline = new SolidBrush(ParseColor(node.Parameters.GetValueOrDefault("color2"), Color.Black));
        using var selectedOutline = new SolidBrush(ParseColor(
            node.Parameters.GetValueOrDefault("select_color2"), Color.Black));
        PangyaUiMenuState? selection = options.MenuStates?.GetValueOrDefault(node);
        byte alpha = selection?.SelectionAlpha ?? byte.MaxValue;
        using var pulsedHighlight = new SolidBrush(Color.FromArgb(highlight.Color.A * alpha / 255, highlight.Color));
        using var pulsedOutline = new SolidBrush(Color.FromArgb(selectedOutline.Color.A * alpha / 255,
            selectedOutline.Color));
        int style = ReadHexTextStyle(node.Parameters.GetValueOrDefault("style"), 1);
        int selectedStyle = ReadHexTextStyle(node.Parameters.GetValueOrDefault("sel_style"), 1);
        float separatorWidth = graphics.MeasureString("|", font, PointF.Empty, format).Width;
        for (int index = 0; index < segments.Length; index++)
        {
            string segment = segments[index];
            if (segment.Length == 0) break;
            float segmentWidth = graphics.MeasureString(segment, font, PointF.Empty, format).Width;
            bool hovered = ReferenceEquals(options.HoveredNode, node) && options.Pointer is Point pointer &&
                pointer.X >= x && pointer.X < x + segmentWidth + separatorWidth;
            if (!draw && hovered) return index;
            bool forced = index == 0 && options.ButtonState != PangyaUiButtonState.Normal &&
                !ReferenceEquals(options.HoveredNode, node);
            bool selected = selection?.SelectedRow == index;
            if (draw)
                DrawStyledText(graphics, segment, hovered || forced ? highlight : selected ? pulsedHighlight : normal,
                    hovered || forced ? selectedOutline : selected ? pulsedOutline : outline,
                    new PointF(x, bounds.Y), format, hovered || forced || selected ? selectedStyle : style);
            x += segmentWidth;
            if (index + 1 < segments.Length && segments[index + 1].Length > 0)
            {
                if (draw) DrawStyledText(graphics, "|", normal, outline, new PointF(x, bounds.Y), format, style);
                x += separatorWidth;
            }
        }
        return -1;

    }

    public static bool HasButtonSkin(PangyaUiNode node) =>
        node.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase) &&
        node.Parameters.ContainsKey("normal");

    private Rectangle GetButtonBounds(PangyaUiNode node, Rectangle bounds, IReadOnlySet<string> symbols) =>
        GetBitmap(node.Parameters["normal"], symbols) is Image normal
            ? new Rectangle(bounds.Location, normal.Size)
            : Rectangle.Empty;

    private void DrawButton(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        PangyaUiRenderOptions options, bool rightGaugeButton = false)
    {
        if (bounds.IsEmpty) return;
        PangyaUiButtonState state = options.ButtonState;
        node.Parameters.TryGetValue("style", out string? value);
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int style);
        string mode = style == 1 ? state switch
        {
            PangyaUiButtonState.Hover => "over",
            PangyaUiButtonState.Selected => "selected",
            _ => "normal"
        } : "normal";
        Color tint = style == 0 ? state switch
        {
            PangyaUiButtonState.Hover => Color.FromArgb(224, 224, 224),
            PangyaUiButtonState.Selected => Color.FromArgb(176, 176, 176),
            _ => Color.White
        } : Color.White;
        PangyaUiImageButtonState? runtime = options.ImageButtonStates?.GetValueOrDefault(node);
        double elapsed = runtime?.SecondsSincePress ?? double.PositiveInfinity;
        if (runtime is null && options.ButtonPressTicks?.TryGetValue(node, out long pressedAt) == true)
            elapsed = (options.CurrentTick - pressedAt) / 1000.0;
        bool enabled = runtime?.Enabled ?? options.DisabledNodes?.Contains(node) != true;
        if ((!enabled && style is 0 or 1) || elapsed <= GetButtonPushDelay(node))
            tint = Color.FromArgb(80, 176, 176, 176);
        bool blinkPhase = options.ImageButtonStates?.GetValueOrDefault(node)?.Blink ??
            options.LayoutAnimationTime.TotalMilliseconds % 1400 >= 700;
        if (!rightGaugeButton && blinkPhase && node.Parameters.GetValueOrDefault("blink") is { Length: > 0 } blink &&
            GetBitmap(blink, Symbols(options)) is not null)
            mode = "blink";
        else if (blinkPhase && options.ImageButtonStates?.GetValueOrDefault(node)?.Blink == true)
            tint = Color.FromArgb(tint.A / 2, tint);
        if (!options.IsViewFocused)
            tint = Color.FromArgb(tint.A / 2, tint);

        foreach (string key in rightGaugeButton ? new[] { "r" + mode } : new[] { "below_" + mode, mode })
        {
            if (node.Parameters.TryGetValue(key, out string? filename) &&
                GetBitmap(filename, Symbols(options)) is Image image)
                DrawTintedImage(graphics, image, bounds, 0, image.Width, image.Height, tint);
        }
    }

    public static double GetButtonPushDelay(PangyaUiNode node)
    {
        if (!HasButtonSkin(node)) return 0;
        return double.TryParse(node.Parameters.GetValueOrDefault("pushdelay"), NumberStyles.Float,
            CultureInfo.InvariantCulture, out double delay) && double.IsFinite(delay) && delay != 0
            ? delay : 1.2;
    }

    private static bool IsViewer(PangyaUiNode node) =>
        node.Type.Equals("VIEWER", StringComparison.OrdinalIgnoreCase);

    private Image? GetViewerImage(PangyaUiNode node, IReadOnlySet<string> symbols, PangyaUiRenderOptions? options = null)
    {
        string? filename = options?.ViewerImages?.GetValueOrDefault(node);
        if (!string.IsNullOrWhiteSpace(filename) && GetBitmap(filename, symbols) is Image image)
            return image;
        filename = node.Parameters.GetValueOrDefault("image");
        return string.IsNullOrWhiteSpace(filename) ? null : GetBitmap(filename, symbols);
    }

    public int GetViewerMaximumTopRow(PangyaUiNode node, IReadOnlySet<string>? symbols = null, PangyaUiRenderOptions? options = null) =>
        IsViewer(node) && node.Parameters.TryGetValue("use_wheel", out string? wheel) &&
        wheel == "true" && node.Bounds.Height >= ViewerRowHeight && GetViewerImage(node, symbols ?? Symbols(options ?? new PangyaUiRenderOptions()), options) is Image image
            ? Math.Max(0, image.Height / ViewerRowHeight - node.Bounds.Height / ViewerRowHeight)
            : 0;

    private static float GetViewerScrollRow(PangyaUiNode node, PangyaUiRenderOptions options) =>
        options.ViewerScrollRows?.TryGetValue(node, out float row) == true && float.IsFinite(row)
            ? row : options.ViewerTopRows?.GetValueOrDefault(node) ?? 0;

    private void DrawViewer(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        PangyaUiRenderOptions options)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || GetViewerImage(node, Symbols(options), options) is not Image image)
            return;
        float row = GetViewerScrollRow(node, options);
        int sourceY = (int)(Math.Clamp(row, 0, GetViewerMaximumTopRow(node, Symbols(options), options)) * ViewerRowHeight);
        int width = Math.Min(bounds.Width, image.Width);
        int height = Math.Min(bounds.Height, image.Height - sourceY);
        node.Parameters.TryGetValue("bgcolor", out string? color);
        Color tint = options.DisabledNodes?.Contains(node) == true
            ? Color.FromArgb(80, 176, 176, 176) : ParseColor(color, Color.White, implicitAlpha: false);
        DrawTintedImage(graphics, image, new Rectangle(bounds.X, bounds.Y, width, height),
            sourceY, width, height, tint);
        if (node.Parameters.GetValueOrDefault("use_wheel") == "true")
            DrawScrollBar(graphics, bounds, bounds.Height / ViewerRowHeight,
                image.Height / ViewerRowHeight, row, Symbols(options), showGuide: true,
                focused: options.IsViewFocused && ReferenceEquals(options.HoveredNode, node));
    }

    public Rectangle GetViewerScrollThumb(PangyaUiNode node, PangyaUiRenderOptions options)
    {
        IReadOnlySet<string> symbols = Symbols(options);
        if (GetViewerMaximumTopRow(node, symbols, options) <= 0 || GetViewerImage(node, symbols, options) is not Image image)
            return Rectangle.Empty;
        Rectangle bounds = GetAbsoluteBounds(node);
        return DrawScrollBar(null, bounds, bounds.Height / ViewerRowHeight,
            image.Height / ViewerRowHeight, GetViewerScrollRow(node, options),
            symbols, showGuide: false);
    }

    private Rectangle DrawScrollBar(Graphics? graphics, Rectangle bounds, int capacity, int rows,
        float topRow, IReadOnlySet<string> symbols, bool showGuide, bool focused = false)
    {
        if (capacity <= 0 || (rows <= capacity && !showGuide)) return Rectangle.Empty;
        Image? top = GetBitmap("scroll_n_1", symbols);
        Image? middle = GetBitmap("scroll_n_2", symbols);
        Image? bottom = GetBitmap("scroll_n_3", symbols);
        if (top is null || middle is null || bottom is null) return Rectangle.Empty;

        int x = ClampCoordinate((long)bounds.X + bounds.Width - top.Width + 2);
        int thumbHeight = bounds.Height;
        int thumbY = bounds.Y;
        if (rows > capacity)
        {
            Image? trackTop = GetBitmap("scrollbg1", symbols);
            Image? trackMiddle = GetBitmap("scrollbg2", symbols);
            Image? trackBottom = GetBitmap("scrollbg3", symbols);
            if (trackTop is not null && trackMiddle is not null && trackBottom is not null)
                DrawStrip(trackTop, trackMiddle, trackBottom, x, bounds.Y, trackTop.Width, bounds.Height,
                    reservedBottomHeight: trackMiddle.Height);

            int caps = Math.Min(bounds.Height, top.Height + bottom.Height);
            thumbHeight = caps + (int)((long)(bounds.Height - caps) * capacity / rows);
            int maxRow = rows - capacity;
            float travel = (bounds.Height - thumbHeight) * Math.Clamp(topRow, 0, maxRow) / maxRow;
            thumbY = ClampCoordinate((long)bounds.Y + (long)(travel + 0.5f));
            x = ClampCoordinate((long)x + 1);
        }
        if (focused)
        {
            Image focusedTop = GetBitmap("scroll_o_1", symbols) ?? top;
            Image focusedMiddle = GetBitmap("scroll_o_2", symbols) ?? middle;
            Image focusedBottom = GetBitmap("scroll_o_3", symbols) ?? bottom;
            DrawStrip(focusedTop, focusedMiddle, focusedBottom, x, thumbY, top.Width,
                thumbHeight, top.Height, bottom.Height);
        }
        else DrawStrip(top, middle, bottom, x, thumbY, top.Width, thumbHeight);
        return new Rectangle(x, thumbY, top.Width, thumbHeight);

        void DrawStrip(Image first, Image center, Image last, int left, int y, int width, int height,
            int? firstCap = null, int? lastCap = null, int? reservedBottomHeight = null)
        {
            if (graphics is null) return;
            int firstHeight = Math.Min(firstCap ?? first.Height, height);
            int lastHeight = Math.Min(lastCap ?? last.Height, height - firstHeight);
            int centerHeight = Math.Max(0, height - firstHeight - (reservedBottomHeight ?? lastHeight));
            if (firstHeight > 0) graphics.DrawImage(first, new Rectangle(left, y, width, firstHeight));
            if (centerHeight > 0) graphics.DrawImage(center,
                new Rectangle(left, ClampCoordinate((long)y + firstHeight), width, centerHeight));
            if (lastHeight > 0) graphics.DrawImage(last,
                new Rectangle(left, ClampCoordinate((long)y + firstHeight + centerHeight), width, lastHeight));
        }
    }

    private static void DrawTintedImage(Graphics graphics, Image image, Rectangle destination,
        int sourceY, int width, int height, Color tint)
    {
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix
        {
            Matrix00 = tint.R / 255f,
            Matrix11 = tint.G / 255f,
            Matrix22 = tint.B / 255f,
            Matrix33 = tint.A / 255f
        });
        graphics.DrawImage(image, destination,
            0, sourceY, width, height, GraphicsUnit.Pixel, attributes);
    }

    private static Color ParseColor(string? value, Color fallback, bool implicitAlpha = true)
    {
        string hex = value?.Trim() ?? string.Empty;
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if ((implicitAlpha && hex.Length != 6 && hex.Length != 8) ||
            !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint color))
            return fallback;
        return Color.FromArgb(unchecked((int)(implicitAlpha && hex.Length == 6 ? color | 0xff000000 : color)));
    }

    public bool CanPreviewFrameBlink(PangyaUiNode node, IReadOnlySet<string> symbols)
    {
        string resource = GetRenderResource(node, PangyaUiButtonState.Normal);
        PangyaUiNode? frame = _resources.TryResolve(resource, node.Type, symbols);
        if (frame?.IsForm == true && !frame.IsLayout)
            frame = _resources.TryResolve(frame.GetResource(PangyaUiButtonState.Normal), "FRAME", symbols);
        XmlElement? layer = frame?.Element.ChildNodes.OfType<XmlElement>()
            .FirstOrDefault(child => child.LocalName.Equals("layer", StringComparison.OrdinalIgnoreCase));
        return frame?.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) == true &&
            ReadLayerValue(layer, "type", 0) == 3 &&
            !string.IsNullOrWhiteSpace(frame.Element["cfrm"]?.GetAttribute("filename"));
    }

    private static void DrawDesktopWallpaper(Graphics graphics, Image image, Rectangle bounds,
        PangyaUiRenderOptions options)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || options.DesktopWallpaperTint.A == 0) return;
        GraphicsState state = graphics.Save();
        try
        {
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            if (options.DesktopWallpaperTint.ToArgb() == Color.White.ToArgb())
                graphics.DrawImage(image, bounds, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            else
                DrawTintedImage(graphics, image, bounds, 0, image.Width, image.Height, options.DesktopWallpaperTint);
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    private Image? GetStartupWallpaper(PangyaUiRenderOptions options, IReadOnlySet<string> symbols) =>
        options.StartupWallpaperImage ?? (options.StartupWallpaper is { Length: > 0 } name
            ? Path.IsPathFullyQualified(name) ? _images.GetImageByPath(name) : GetBitmap(name, symbols)
            : null);

    private Image? GetBitmap(string name, IReadOnlySet<string> symbols)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        string? alias = GetBitmapFilename(_resources.TryResolve(name, "BITMAP", symbols));
        return _images.GetImage(string.IsNullOrWhiteSpace(alias) ? name : alias);
    }

    private static string? GetBitmapFilename(PangyaUiNode? definition) =>
        definition?.Type.Equals("BITMAP", StringComparison.OrdinalIgnoreCase) == true
            ? definition.Element.GetAttribute("filename") : null;

    private static bool IsFrameBlinkVisible(PangyaUiNode node, PangyaUiRenderOptions options)
    {
        if (options.FrameBlinkElapsed?.TryGetValue(node, out TimeSpan elapsed) == true)
            return Math.Max(0, elapsed.Ticks) % (1600 * TimeSpan.TicksPerMillisecond) <
                800 * TimeSpan.TicksPerMillisecond;
        return options.ShowFrameBlink;
    }

    private static string GetRenderResource(PangyaUiNode node, PangyaUiButtonState state)
    {
        if ((node.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) ||
             node.Type.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase)) &&
            node.Parameters.TryGetValue("resource", out string? resource) &&
            !string.IsNullOrWhiteSpace(resource))
            return resource;
        return node.GetResource(state);
    }

    private Rectangle DrawFrame(Graphics? graphics, Rectangle bounds, PangyaUiNode definition, IReadOnlySet<string> symbols, bool showBlink,
        string? baseImageOverride = null, PangyaUiNode? textSource = null, bool isViewFocused = true,
        bool contextMenu = false, bool hideDescription = false)
    {
        XmlElement[] layers = definition.Element.ChildNodes.OfType<XmlElement>()
            .Where(child => child.LocalName.Equals("layer", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Rectangle? measured = bounds;
        int baseType = ReadLayerValue(layers.FirstOrDefault(), "type", 0);
        if (ReadLayerValue(layers.FirstOrDefault(), "invisible", 0) == 0)
            IncludeBounds(ref measured, DrawFrameLayer(graphics, bounds, definition, symbols, "bfrm",
                baseType, showBlink: showBlink,
                imageOverride: baseImageOverride, dim: !isViewFocused && !showBlink));
        if (graphics is not null && textSource is not null)
        {
            using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
            using var title = new SolidBrush(Color.White);
            using var outline = new SolidBrush(Color.Gray);
            using var descriptionBrush = new SolidBrush(Color.FromArgb(64, 64, 64));
            string caption = textSource.Element.GetAttribute("caption");
            string description = textSource.Parameters.GetValueOrDefault("description") ??
                textSource.Element.GetAttribute("description");
            if (caption.Length > 0)
                DrawStyledText(graphics, caption, title, outline,
                    new PointF(bounds.X + 10f, bounds.Y + 7f), format, 2);
            if (!hideDescription && description.Length > 0)
                DrawTaggedCaption(graphics, description, descriptionBrush, title,
                    new PointF(bounds.X + 10f, bounds.Y + (float)bounds.Height - 26f), format, 0);
        }
        foreach (XmlElement layer in contextMenu && baseType != 0 ? [] : layers.Skip(1))
        {
            if (ReadLayerValue(layer, "invisible", 0) != 0) continue;
            int height = ReadLayerValue(layer, "height", -1);
            int position = ReadLayerValue(layer, "pos", 0);
            int bottom = ReadLayerValue(layer, "bottompos", 0);
            Rectangle layerBounds = bounds;
            if (height >= 0) layerBounds.Height = height;
            long y = bottom != 0 ? (long)bounds.Y + bounds.Height - bottom - layerBounds.Height :
                position < 0 ? (long)bounds.Y + bounds.Height + position - layerBounds.Height :
                (long)bounds.Y + position;
            layerBounds.Y = ClampCoordinate(y);
            IncludeBounds(ref measured, DrawFrameLayer(graphics, layerBounds, definition, symbols, "sfrm",
                ReadLayerValue(layer, "type", 0), bottom != 0 || position < 0,
                imageOverride: contextMenu ? "sfrm" : null,
                dim: !isViewFocused && !showBlink));
        }
        return measured ?? bounds;
    }

    private static int ReadLayerValue(XmlElement? layer, string name, int fallback) =>
        int.TryParse(layer?.GetAttribute(name), NumberStyles.Integer, CultureInfo.InvariantCulture,
            out int value) ? value : fallback;

    private Rectangle GetFormFrameBounds(PangyaUiNode form, Rectangle bounds, PangyaUiRenderOptions options)
    {
        IReadOnlySet<string> symbols = Symbols(options);
        PangyaUiNode? frame = _resources.TryResolve(form.GetResource(options.ButtonState), "FRAME", symbols);
        return frame?.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) == true
            ? GetBaseFrameBounds(bounds, frame, symbols, imageOverride: null) : bounds;
    }

    private Rectangle GetBaseFrameBounds(Rectangle bounds, PangyaUiNode definition,
        IReadOnlySet<string> symbols, string? imageOverride) =>
        DrawFrame(null, bounds, definition, symbols, showBlink: false, baseImageOverride: imageOverride);

    private Rectangle DrawFrameLayer(Graphics? graphics, Rectangle bounds, PangyaUiNode definition,
        IReadOnlySet<string> symbols, string tag, int type, bool alignBottom = false, bool showBlink = false,
        string? imageOverride = null, bool dim = false)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return bounds;
        Color tint = ParseColor(definition.Element.GetAttribute("color"), Color.White);
        if (dim)
        {
            int argb = tint.ToArgb();
            tint = Color.FromArgb((argb & unchecked((int)0xff000000)) | ((argb & 0xffffff) * 4 / 5));
        }
        if (tint.A == 0 && graphics is not null) return bounds;

        void DrawSlice(Image image, Rectangle destination)
        {
            if (graphics is null) return;
            if (tint.ToArgb() == Color.White.ToArgb())
                graphics.DrawImage(image, destination);
            else
                DrawTintedImage(graphics, image, destination, 0, image.Width, image.Height, tint);
        }
        XmlElement? frame = definition.Element.ChildNodes.OfType<XmlElement>()
            .LastOrDefault(child => child.LocalName.Equals(tag,
                StringComparison.OrdinalIgnoreCase));
        string baseName = imageOverride ?? frame?.GetAttribute("filename") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseName)) return Rectangle.Empty;
        if (Path.HasExtension(baseName))
        {
            Image? direct = GetBitmap(baseName, symbols);
            if (direct is not null) DrawSlice(direct, bounds);
            return direct is not null ? bounds : Rectangle.Empty;
        }

        Image?[] slices = Enumerable.Range(0, 9)
            .Select(index => (type == 1 && index >= 3 || type == 2 && index < 6)
                ? null : GetBitmap(baseName + index.ToString("00", CultureInfo.InvariantCulture), symbols))
            .ToArray();
        if (slices.All(image => image is null))
        {
            Image? direct = GetBitmap(baseName, symbols);
            if (direct is not null) DrawSlice(direct, bounds);
            return direct is not null ? bounds : Rectangle.Empty;
        }

        int left = slices[0]?.Width ?? slices[3]?.Width ?? slices[6]?.Width ?? 0;
        int right = slices[2]?.Width ?? slices[5]?.Width ?? slices[8]?.Width ?? 0;
        int top = slices[0]?.Height ?? slices[1]?.Height ?? 0;
        int bottom = slices[6]?.Height ?? slices[7]?.Height ?? 0;
        int[] widths = [left, (int)Math.Max(0L, (long)bounds.Width - left - right), right];
        int middle = type is 1 or 2 ? 0 : (int)Math.Max(0L, (long)bounds.Height - top - bottom);
        int[] heights = [top, middle, bottom];
        if (graphics is null)
            return new Rectangle(bounds.X,
                alignBottom ? ClampCoordinate((long)bounds.Y + bounds.Height - top - middle - bottom) : bounds.Y,
                ClampCoordinate(Math.Max(bounds.Width, (long)left + right)),
                ClampCoordinate((long)top + middle + bottom));
        Image?[]? blink = null;
        if (showBlink && type == 3)
        {
            string prefix = definition.Element["cfrm"]?.GetAttribute("filename") ?? string.Empty;
            if (prefix.Length > 0 && GetBitmap(prefix + "00", symbols) is Image first)
                blink = [first, GetBitmap(prefix + "01", symbols), GetBitmap(prefix + "02", symbols)];
        }
        int slice = 0;
        int y = alignBottom
            ? ClampCoordinate((long)bounds.Y + bounds.Height - top - middle - bottom)
            : bounds.Y;
        for (int row = 0; row < 3; row++)
        {
            int x = bounds.X;
            for (int column = 0; column < 3; column++)
            {
                Image? image = row == 0 && blink is not null ? blink[column] : slices[slice];
                slice++;
                var target = new Rectangle(x, y, widths[column], heights[row]);
                if (image is not null && target.Width > 0 && target.Height > 0)
                    DrawSlice(image, target);
                x = ClampCoordinate((long)x + widths[column]);
            }
            y = ClampCoordinate((long)y + heights[row]);
        }
        return bounds;
    }

    private Rectangle GetReferencedBounds(PangyaUiNode node,
        PangyaUiRenderOptions options, HashSet<PangyaUiNode> visited)
    {
        if (!node.IsRenderVisible || !visited.Add(node)) return Rectangle.Empty;
        Rectangle? result = null;
        IReadOnlySet<string> symbols = Symbols(options);
        string resource = GetRenderResource(node, options.ButtonState);
        PangyaUiNode? definition = _resources.TryResolve(resource, node.Type, symbols);
        if (GetBitmapFilename(definition) is string bitmapFilename)
        {
            resource = bitmapFilename;
            definition = null;
        }
        if (node.ContextMenuCaption.Length > 0)
        {
            using var bitmap = new Bitmap(1, 1);
            using Graphics metrics = Graphics.FromImage(bitmap);
            IncludeBounds(ref result,
                RenderContextMenu(metrics, node, node.Bounds.Location, options, draw: false, out _));
        }
        else if (GetTabs(node, options) is { } tabs)
            IncludeBounds(ref result, RenderTabs(null, node, node.Bounds.Location, tabs, symbols));
        else if (GetList(node, options) is not null || node.StaticCaption.Length > 0 ||
                 node.TextButtonCaption.Length > 0 || node.LayoutBase is not null)
            IncludeBounds(ref result, node.Bounds);
        else if (IsViewer(node) && GetViewerImage(node, symbols) is not null)
            IncludeBounds(ref result, node.Bounds);
        else if (IsEdit(node))
            IncludeBounds(ref result, GetEditBounds(node, node.Bounds, symbols));
        else if (IsGauge(node))
            IncludeBounds(ref result, node.Bounds);
        else if (IsImageGauge(node) && GetGaugeImage(node, "normal", symbols) is Image gaugeSkin)
            IncludeBounds(ref result, new Rectangle(node.Bounds.Location, gaugeSkin.Size));
        else if (HasButtonSkin(node))
            IncludeBounds(ref result, GetButtonBounds(node, node.Bounds, symbols));
        else if (definition?.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) == true)
            IncludeBounds(ref result, GetBaseFrameBounds(node.Bounds, definition, symbols,
                node.Parameters.GetValueOrDefault("bgimg")));
        else if (definition?.IsForm == true && !definition.IsLayout &&
                 definition.Bounds.Size is { Width: > 0, Height: > 0 } formSize)
            IncludeBounds(ref result, GetFormFrameBounds(definition,
                new Rectangle(node.Bounds.Location, formSize), options));
        else if (definition is not null)
        {
            Rectangle referenced = GetReferencedBounds(definition, options, visited);
            if (!IsMacroItem(node)) referenced.Offset(node.Bounds.Location);
            IncludeBounds(ref result, referenced);
        }
        else if (!IsMacroItem(node))
        {
            Image? image = string.IsNullOrWhiteSpace(resource) ? null : _images.GetImage(resource);
            if (image is not null && node.IsRenderVisible &&
                !IsTransparent(node, options.ButtonState))
            {
                Rectangle destination = ImageBounds(node, node.Bounds, image);
                IncludeBounds(ref result,
                    ApplyAreaEntrance(node, node.Bounds, image, options, ref destination) ??
                    ClipImageBounds(node, node.Bounds, destination));
            }
        }

        foreach (PangyaUiNode child in node.Children.Where(child => child.IsVisible(symbols)))
        {
            Rectangle childBounds = GetReferencedBounds(child, options, visited);
            if (IsGroupBox(node) && !childBounds.IsEmpty)
                childBounds.Offset(node.Bounds.Location);
            IncludeBounds(ref result, childBounds);
        }
        visited.Remove(node);
        return result ?? Rectangle.Empty;
    }

    private static bool IsMacroItem(PangyaUiNode node) =>
        node.Type.Equals("MACROITEM", StringComparison.OrdinalIgnoreCase);

    private static bool IsArea(PangyaUiNode node) =>
        node.Type.Equals("AREA", StringComparison.OrdinalIgnoreCase);

    private static Rectangle ImageBounds(PangyaUiNode node, Rectangle bounds, Image image)
    {
        if (IsArea(node) && node.Parameters.TryGetValue("center", out string? center) &&
            int.TryParse(center, out int centered) && centered != 0)
        {
            int x = ClampCoordinate(bounds.X + ((long)bounds.Width - image.Width + 1) / 2);
            int y = ClampCoordinate(bounds.Y + ((long)bounds.Height - image.Height + 1) / 2);
            return new Rectangle(new Point(x, y), image.Size);
        }
        return node.IsExplicitlyStretched && (IsArea(node) || (bounds.Width > 0 && bounds.Height > 0))
            ? bounds
            : new Rectangle(bounds.Location, image.Size);
    }

    private static Rectangle ClipImageBounds(PangyaUiNode node, Rectangle bounds, Rectangle destination)
    {
        if (!IsArea(node) || !node.Parameters.TryGetValue("enable_clip", out string? clip) ||
            !int.TryParse(clip, out int enabled) || enabled != 1)
            return destination;

        if (node.Parameters.TryGetValue("clip_area", out string? area) &&
            PangyaUiDimensionHelper.ParseRectangle(area) is Rectangle explicitClip)
            return Rectangle.Intersect(destination, explicitClip);
        return Rectangle.Intersect(destination, bounds);
    }

    private static bool IsGroupBox(PangyaUiNode node) =>
        node.Type.Equals("GROUPBOX", StringComparison.OrdinalIgnoreCase);

    private static bool IsTransparent(PangyaUiNode node, PangyaUiButtonState state)
    {
        if (node.Type.Equals("TABBUTTON", StringComparison.OrdinalIgnoreCase))
            return state == PangyaUiButtonState.Normal;
        return IsGroupBox(node) ||
               node.Type.Equals("LISTBOX", StringComparison.OrdinalIgnoreCase) ||
               (node.IsForm && !node.IsLayout && string.IsNullOrWhiteSpace(node.GetResource(state)));
    }

    private static IReadOnlySet<string> Symbols(PangyaUiRenderOptions options) =>
        options.EnabledSymbols ??
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private static void IncludeBounds(ref Rectangle? aggregate, Rectangle bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        aggregate = aggregate is Rectangle existing ? Rectangle.Union(existing, bounds) : bounds;
    }

    private static void DrawOutline(Graphics graphics, Rectangle bounds, Color color, float width)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        using var pen = new Pen(color, width);
        graphics.DrawRectangle(pen, bounds);
    }

    private static Point GetParentOffset(PangyaUiNode node)
    {
        long x = 0;
        long y = 0;
        for (PangyaUiNode? parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            x += parent.Bounds.X;
            y += parent.Bounds.Y;
        }
        return new Point(ClampCoordinate(x), ClampCoordinate(y));
    }

    private static Rectangle GetAbsoluteBounds(PangyaUiNode node)
    {
        Point offset = GetParentOffset(node);
        return new Rectangle(
            ClampCoordinate((long)node.Bounds.X + offset.X),
            ClampCoordinate((long)node.Bounds.Y + offset.Y),
            node.Bounds.Width,
            node.Bounds.Height);
    }

    private static int ClampCoordinate(long value) =>
        (int)Math.Clamp(value, int.MinValue, int.MaxValue);

    private static void DrawDebugBounds(Graphics graphics, PangyaUiNode node,
        Rectangle bounds, float scale)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        DrawOutline(graphics, bounds, Color.FromArgb(150, Color.DeepSkyBlue), scale);
        using var font = new Font(FontFamily.GenericSansSerif, 8f * scale);
        SizeF labelSize = graphics.MeasureString(node.DisplayName, font);
        var labelBounds = new RectangleF(bounds.X, bounds.Y, labelSize.Width, labelSize.Height);
        using var background = new SolidBrush(Color.FromArgb(180, 35, 75, 95));
        using var foreground = new SolidBrush(Color.White);
        graphics.FillRectangle(background, labelBounds);
        graphics.DrawString(node.DisplayName, font, foreground, labelBounds.Location);
    }
}
