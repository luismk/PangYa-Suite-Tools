using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text;
using System.Xml;

namespace PangyaAPI.UI;

public sealed record PangyaUiFrameText(string? Caption = null, string? Description = null,
    bool UseCaptionOffset = false, bool UseDescriptionEmoticons = true,
    bool UseCaptionFocus = true, byte Opacity = byte.MaxValue);

public sealed record PangyaUiCursorPreview(Point Position, int Index, int Frame = 0);

public sealed record PangyaUiTooltipPreview(string Text, int Style = 0, Point? Position = null);

public sealed record PangyaUiTab(string Normal, string? Hover = null, string? Selected = null,
    PangyaUiButtonState State = PangyaUiButtonState.Normal, Size? Size = null)
{
    public uint? Identifier { get; init; }
    public string? Caption { get; init; }
    public float? LayoutWidth { get; init; }

    public static PangyaUiTab FromImagePrefix(string prefix, uint identifier, Size? size = null)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        return new PangyaUiTab(prefix + "_n", prefix + "_o", prefix + "_d", Size: size)
        {
            Identifier = identifier
        };
    }
}

public readonly record struct PangyaUiListItemState(int Index, int VisibleIndex, Rectangle Bounds,
    bool UnderCursor, bool Selected)
{
    public bool IsDummy { get; init; }
}

public sealed record PangyaUiListPreview(int ItemCount, int TopRow,
    Action<Graphics, int, Rectangle> DrawItem)
{
    public Action<Graphics, Rectangle>? DrawBackground { get; init; }
    public Action<Graphics, PangyaUiListItemState>? DrawItemState { get; init; }
    public IReadOnlySet<int>? SelectedItems { get; init; }
    public IReadOnlySet<int>? DummyItems { get; init; }
    public bool ExcludeDummyItemsFromHitTesting { get; init; }
    public int? ItemUnderCursor { get; init; }
    public IReadOnlySet<int>? UnderCursorItems { get; init; }
    public bool MouseEventsEnabled { get; init; } = true;
    public Size? ItemSize { get; init; }
    public int? ColumnCapacity { get; init; }
    public int? TopItem { get; init; }
    public bool FollowBottom { get; init; }
    public bool ScrollbarAtLeft { get; init; }
    public bool? ShowScrollbar { get; init; }
}

public sealed record PangyaUiMenuState(int SelectedRow = -1,
    IReadOnlySet<int>? DisabledRows = null, byte SelectionAlpha = byte.MaxValue)
{
    public IReadOnlySet<string>? DisabledTexts { get; init; }
    public IReadOnlyDictionary<string, string>? TextReplacements { get; init; }
    public byte Opacity { get; init; } = byte.MaxValue;
}

public sealed record PangyaUiImageButtonState(bool Enabled = true,
    double SecondsSincePress = double.PositiveInfinity, bool Blink = false)
{
    public int? Style { get; init; }
    public double? PushDelay { get; init; }
    public int? PushStyle { get; init; }
    public IReadOnlyDictionary<string, Image?>? BitmapImages { get; init; }
    public PangyaUiButtonState? Status { get; init; }
    public string? ReplacementMode { get; init; }
    public string? ReplacementImage { get; init; }
    public IReadOnlyDictionary<string, string>? Images { get; init; }
    public IReadOnlyDictionary<string, string?>? BelowImages { get; init; }
}

public sealed record PangyaUiGaugeRange(int Lower, int Upper, int Origin = 0);
public sealed record PangyaUiGaugeAppearance(Color? FillColor = null, int? BorderThickness = null,
    Color? ExpansionColor1 = null, Color? ExpansionColor2 = null);
public sealed record PangyaUiEditTextColors(Color? Foreground = null, Color? Outline = null);
public sealed record PangyaUiComboItem(string Text, string? Image = null);
public sealed record PangyaUiComboImageSelection(string? Image, byte CurrentIndex = 0);
public sealed record PangyaUiEditCaret(int Line, int Column, TimeSpan Elapsed);
public enum PangyaUiComboDirection
{
    Auto,
    Above,
    Below
}

public sealed record PangyaUiComboPreview(IReadOnlyList<PangyaUiComboItem> Items, bool IsOpen = false,
    int SelectedIndex = -1, int HoveredIndex = -1, int TopRow = 0,
    PangyaUiComboDirection Direction = PangyaUiComboDirection.Auto)
{
    public bool? AutomaticOpensAbove { get; init; }
    public int? IncreasedMaximumRows { get; init; }
    public float? ScrollThumbRow { get; init; }
    public PangyaUiComboImageSelection? ImageSelection { get; init; }
    public int? StoredTextRowCount { get; init; }
    public IReadOnlyList<string>? TextureImages { get; init; }
}
public sealed record PangyaUiGaugeTransition(int From, int To, TimeSpan Elapsed, double Speed = 65,
    bool Achilles = false);

public sealed record PangyaUiRenderOptions(
    PangyaUiButtonState ButtonState = PangyaUiButtonState.Normal,
    IReadOnlySet<string>? EnabledSymbols = null,
    bool ShowDebugBounds = false,
    PangyaUiNode? SelectedNode = null,
    float StrokeScale = 1f)
{
    internal Size RenderCanvasSize { get; init; }
    public float GraphicsAlpha { get; init; } = 1;
    public IReadOnlySet<PangyaUiNode>? DeferredDisplayContainers { get; init; }
    public IReadOnlySet<PangyaUiNode>? ModalForms { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, uint>? FormFlags { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? ViewerTopRows { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? ViewerFollowBottom { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, byte>? ButtonOpacity { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Action<Graphics, Rectangle, PangyaUiButtonState>>? ButtonOwnerDraw { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? SelectedTabIndices { get; init; }
    public IReadOnlyDictionary<(PangyaUiNode Node, Point Origin), int>? SelectedTabInstances { get; init; }
    public IReadOnlySet<PangyaUiNode>? MakeRoomMatchForms { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, byte>? TabOpacity { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? TabSeparatorWidths { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? TabMouseEventsEnabled { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int?>? HoveredTabIndices { get; init; }
    public Action<PangyaUiNode, int, Rectangle>? OnTabRendered { get; init; }
    public Action<PangyaUiNode, int, Rectangle, Point>? OnTabInstanceRendered { get; init; }
    public (PangyaUiNode Node, int Index, Rectangle Bounds)? HoveredTab { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Action<Graphics, int?, Rectangle>>? TabOwnerDraw { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Rectangle>? AreaClipOverrides { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? AreaClippingEnabled { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? AreaMouseEvents { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, byte>? AreaOpacity { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, string?>? AreaImages { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Image>? AreaBitmapImages { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, string>? AreaImageNames { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Action<Graphics, Rectangle>>? AreaOwnerDraw { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Action<Graphics, Rectangle>>? ImageGaugeOwnerDraw { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Action<Graphics, Rectangle>>? ViewerOwnerDraw { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, byte>? ViewerOpacity { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, byte>? EditOpacity { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, string>? ViewerImages { get; init; }
    public IReadOnlySet<PangyaUiNode>? HiddenViewerScrollbars { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? ViewerScrollRows { get; init; }
    public IReadOnlySet<PangyaUiNode>? DisabledNodes { get; init; }
    public IReadOnlySet<PangyaUiNode>? ViewFocusedNodes { get; init; }
    public IReadOnlySet<PangyaUiNode>? FixedNodes { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? WindowAlpha { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? WindowAlpha2 { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? ScrollbarAlpha { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? ScrollbarAlpha2 { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? FormFrameAlpha { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? FormFrameAlpha2 { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? ComboButtonAlpha { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? ComboButtonAlpha2 { get; init; }
    public IReadOnlyDictionary<(PangyaUiNode Node, bool Right), float>? GaugeButtonAlpha { get; init; }
    public IReadOnlyDictionary<(PangyaUiNode Node, bool Right), float>? GaugeButtonAlpha2 { get; init; }
    public IReadOnlySet<PangyaUiNode>? HiddenNodes { get; init; }
    public IReadOnlySet<PangyaUiNode>? VisibleNodes { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Point>? WindowOffsets { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Size>? WindowSizes { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? ScrollbarGuides { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? GaugeStepCounts { get; init; }
    public IReadOnlySet<PangyaUiNode>? TopmostNodes { get; init; }
    public IReadOnlyList<PangyaUiNode>? TopmostOrder { get; init; }
    public IReadOnlySet<PangyaUiNode>? PrivateNodes { get; init; }
    public bool HidePrivacy { get; init; }
    public string? StartupWallpaper { get; init; }
    public Image? StartupWallpaperImage { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, string?>? DesktopWallpapers { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Image>? DesktopWallpaperImages { get; init; }
    public IReadOnlySet<PangyaUiNode>? LockedDesktopWallpapers { get; init; }
    public Color DesktopWallpaperTint { get; init; } = Color.White;
    public IReadOnlyDictionary<PangyaUiNode, Color>? DesktopWallpaperTints { get; init; }
    public bool DesktopWallpaperVisible { get; init; } = true;
    public IReadOnlyDictionary<PangyaUiNode, bool>? DesktopWallpaperVisibility { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PointF>? DesktopAnimationOffsets { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiFrameText>? FrameText { get; init; }
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
    public bool CursorVisible { get; init; } = true;
    public int CursorIndex { get; init; }
    public int TooltipStyle { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiTooltipPreview>? Tooltips { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? TooltipEnabled { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, string>? StaticCaptions { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, string>? TextButtonCaptions { get; init; }
    public IReadOnlySet<PangyaUiNode>? TextButtonCaptionGeometryNodes { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, (Color Foreground, Color Outline)>? StaticTextColors { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? StaticTextStyles { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, Rectangle>? CaptionClips { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? CaptionClippingEnabled { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, byte>? CaptionOpacity { get; init; }
    public IReadOnlySet<PangyaUiNode>? SmallFontCaptions { get; init; }
    public int? CaptionFontHeight { get; init; }
    public Func<string, float>? MeasureCaptionText { get; init; }
    public Action<Graphics, string, Brush, PointF>? DrawCaptionText { get; init; }
    public int? TooltipFontHeight { get; init; }
    public Func<string, float>? MeasureTooltipText { get; init; }
    public Action<Graphics, string, Brush, PointF>? DrawTooltipText { get; init; }
    public byte TooltipAlpha { get; init; } = 220;
    public PangyaUiNode? TooltipNode { get; init; }
    public Point? TooltipPointer { get; init; }
    public TimeSpan LayoutAnimationTime { get; init; }
    public TimeSpan? EmoticonAnimationTime { get; init; }
    public TimeSpan? EditEmoticonAnimationTime { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlyList<long>>? EditLineAnimationStartTicks { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, float>? CaptionScrollOffsets { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, string>? ContextMenuCaptions { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiMenuState>? MenuStates { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlyList<string>>? EditLines { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlyList<int>>? EditLineStyles { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiEditTextColors>? EditTextColors { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlyDictionary<int, string>>? ContextMenuItemTexts { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? EditKeyEvents { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? EditReadOnlyStyles { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? EditEmoticonStyles { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, uint>? SelectedTabIdentifiers { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, uint>? UnselectedTabIdentifiers { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlySet<int>>? JustifiedEditLines { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiEditCaret>? EditCarets { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? EditTopRows { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, bool>? EditFollowBottom { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? GaugeValues { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? ExtendedGaugeSteps { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, int>? GaugeExpansions { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiGaugeRange>? GaugeRanges { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiGaugeAppearance>? GaugeAppearances { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiComboPreview>? Combos { get; init; }
    public int ComboViewportHeight { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiGaugeTransition>? GaugeTransitions { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, IReadOnlyList<PangyaUiTab>>? Tabs { get; init; }
    public IReadOnlyDictionary<PangyaUiNode, PangyaUiListPreview>? Lists { get; init; }
}

public sealed class PangyaUiRenderer
{
    private sealed class GraphicsAlphaState
    {
        public float Alpha = 1;
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Graphics, GraphicsAlphaState>
        GraphicsAlpha = new();

    private static float GetGraphicsAlpha(Graphics graphics) =>
        GraphicsAlpha.TryGetValue(graphics, out GraphicsAlphaState? state) ? state.Alpha : 1;
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
        options ??= new();
        if (!float.IsFinite(options.GraphicsAlpha))
            throw new ArgumentOutOfRangeException(nameof(options), "Graphics alpha must be finite.");
        GraphicsAlphaState state = GraphicsAlpha.GetValue(graphics, _ => new());
        float previous = state.Alpha;
        state.Alpha = Math.Clamp(options.GraphicsAlpha, 0, 1);
        try { RenderCore(graphics, document, selectedForm, options); }
        finally { state.Alpha = previous; }
    }

    private void RenderCore(Graphics graphics, PangyaUiDocument document,
        PangyaUiNode? selectedForm, PangyaUiRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(document);
        options ??= new PangyaUiRenderOptions();
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        using var canvasBrush = new SolidBrush(Color.FromArgb(46, 49, 55));
        Size canvasSize = selectedForm is not null &&
            GetWindowBounds(selectedForm, options).Size is { Width: > 0, Height: > 0 } size
            ? size
            : document.CanvasSize;
        options = options with { RenderCanvasSize = canvasSize };
        if (options.ComboViewportHeight <= 0)
            options = options with { ComboViewportHeight = canvasSize.Height };
        graphics.FillRectangle(canvasBrush, new Rectangle(Point.Empty, canvasSize));
        if (options.DesktopWallpaperVisible && selectedForm?.IsLayout != true &&
            GetStartupWallpaper(options, Symbols(options)) is Image wallpaper)
            DrawDesktopWallpaper(graphics, wallpaper, new Rectangle(Point.Empty, canvasSize), options.DesktopWallpaperTint);

        var dropdowns = new List<(PangyaUiNode Node, Rectangle Bounds, PangyaUiComboPreview Combo)>();
        var topmost = new List<(PangyaUiNode Node, Point Offset, HashSet<PangyaUiNode> Chain)>();
        var deferred = new List<PangyaUiNode>();
        foreach (PangyaUiNode node in GetRenderOrder(document, selectedForm, options.SelectedNode,
                     options.EnabledSymbols, options.TopmostNodes, options))
        {
            if (IsRuntimeHidden(node, options)) continue;
            PangyaUiNode? deferredContainer = GetDeferredDisplayContainer(node, options);
            if (deferredContainer is not null)
            {
                bool desktopChild = selectedForm?.IsLayout == true
                    ? ReferenceEquals(deferredContainer.Parent, selectedForm)
                    : ReferenceEquals(deferredContainer, selectedForm);
                if (!ReferenceEquals(node, deferredContainer) && desktopChild) deferred.Add(node);
                continue;
            }
            DrawNode(graphics, node, GetParentOffset(node, options), options,
                new HashSet<PangyaUiNode>(ReferenceEqualityComparer.Instance), dropdowns, topmost);
            if (options.ShowDebugBounds)
                DrawDebugBounds(graphics, node, GetAbsoluteBounds(node, options), options.StrokeScale);
        }
        for (int index = 0; index < topmost.Count; index++)
        {
            if (options.TopmostOrder is not null)
            {
                int next = index;
                for (int candidate = index + 1; candidate < topmost.Count; candidate++)
                    if (GetTopmostRank(topmost[candidate].Node, options.TopmostOrder) <
                        GetTopmostRank(topmost[next].Node, options.TopmostOrder)) next = candidate;
                if (next != index) (topmost[index], topmost[next]) = (topmost[next], topmost[index]);
            }
            var entry = topmost[index];
            DrawNode(graphics, entry.Node, entry.Offset, options, entry.Chain, dropdowns, topmost,
                drawTopmost: true);
        }
        foreach (var dropdown in dropdowns)
            DrawComboList(graphics, dropdown.Node, dropdown.Bounds, dropdown.Combo, Symbols(options), options);

        // Fresh's desktop pass draws the children of direct flagged containers,
        // without drawing those containers themselves.
        var deferredDropdowns = new List<(PangyaUiNode Node, Rectangle Bounds, PangyaUiComboPreview Combo)>();
        foreach (PangyaUiNode node in deferred)
            DrawNode(graphics, node, GetParentOffset(node, options), options,
                new HashSet<PangyaUiNode>(ReferenceEqualityComparer.Instance), deferredDropdowns, topmost);
        foreach (var dropdown in deferredDropdowns)
            DrawComboList(graphics, dropdown.Node, dropdown.Bounds, dropdown.Combo, Symbols(options), options);

        if (options.SelectedNode is not null && !IsRuntimeHidden(options.SelectedNode, options))
        {
            Rectangle selectedBounds = GetRenderedBounds(options.SelectedNode, options);
            if (selectedBounds.Width <= 0 || selectedBounds.Height <= 0)
                selectedBounds = GetAbsoluteBounds(options.SelectedNode, options);
            DrawOutline(graphics, selectedBounds, Color.Orange, 2f * options.StrokeScale);
        }
        if (options.ShowTooltip && options.TooltipAlpha > 0 &&
            (options.TooltipNode ?? options.HoveredNode) is { } tooltipNode &&
            options.TooltipEnabled?.GetValueOrDefault(tooltipNode, true) != false &&
            !IsRuntimeHidden(tooltipNode, options) && IsRenderVisible(tooltipNode, options) &&
            IsWindowViewFocused(tooltipNode, options))
        {
            PangyaUiTooltipPreview? tooltipState = options.Tooltips?.GetValueOrDefault(tooltipNode);
            string? tooltip = tooltipState?.Text ?? tooltipNode.Parameters.GetValueOrDefault("tooltip");
            Point? pointer = options.TooltipPointer ?? options.Pointer;
            if (tooltip is { Length: > 0 } && (pointer.HasValue || tooltipState?.Position is not null))
                DrawTooltip(graphics, tooltip, pointer ?? Point.Empty, canvasSize, options, tooltipState);
        }
        if (options.CursorVisible && options.CursorPreview is { } cursor) DrawCursor(graphics, cursor, Symbols(options));
    }

    public PangyaUiRenderOptions SetFormMessage(Graphics metrics, PangyaUiNode form, string message,
        bool spaceAlign = false, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(message);
        options ??= new();
        PangyaUiNode? label = form.Children.LastOrDefault(child => child.Name == "message" &&
            child.Type.Equals("STATIC", StringComparison.OrdinalIgnoreCase));
        if (label is not null)
            options = SetStaticCaption(label, message, options: options);
        PangyaUiNode? edit = form.Children.LastOrDefault(child => child.Name == "message" &&
            child.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase));
        if (edit is null) return options;
        int Read(string name, int fallback) => int.TryParse(edit.Parameters.GetValueOrDefault(name),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        var model = new PangyaUiEditText(IsMultilineEdit(edit), Math.Max(0, Read("charlimit", 255)));
        var colors = GetEditTextColors(edit, options);
        model.FontColor = unchecked((uint)colors.Foreground.ToArgb());
        model.OutlineColor = unchecked((uint)colors.Outline.ToArgb());
        int margin = Read("leftmargin", 0);
        bool symmetry = string.Equals(edit.Parameters.GetValueOrDefault("symmetry", "true"),
            "true", StringComparison.OrdinalIgnoreCase);
        float width = GetWindowBounds(edit, options).Width - (long)margin * (symmetry ? 2 : 1) -
            (GetBitmap("scroll_n_1", Symbols(options))?.Width ?? 0);
        bool readOnly = options.EditReadOnlyStyles?.GetValueOrDefault(edit,
            edit.Parameters.GetValueOrDefault("readonly") == "true") ??
            edit.Parameters.GetValueOrDefault("readonly") == "true";
        var symbols = readOnly && (options.EditEmoticonStyles?.GetValueOrDefault(edit, true) ?? true)
            ? Symbols(options) : null;
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        model.AddText(message, width, text => MeasureCaptionLine(metrics, text,
            SystemFonts.DefaultFont, format, symbols, measureText: options.MeasureCaptionText), spaceAlign);
        options = model.ApplyTo(edit, options);
        var topRows = options.EditTopRows?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        topRows[edit] = (options.EditFollowBottom?.GetValueOrDefault(edit, true) ?? true)
            ? GetEditMaximumTopRow(edit, model.Lines.Count, options) : 0;
        return options with { EditTopRows = topRows };
    }

    public bool IsDesktopWallpaperVisible(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        Color tint = options.DesktopWallpaperTints?.GetValueOrDefault(node, options.DesktopWallpaperTint)
            ?? options.DesktopWallpaperTint;
        return GetDesktopWallpaper(node, options, out _) is not null && tint.ToArgb() != 0;
    }

    private Image? GetDesktopWallpaper(PangyaUiNode node, PangyaUiRenderOptions options, out bool show)
    {
        Image? bitmap = options.DesktopWallpaperImages?.GetValueOrDefault(node);
        bool hasFilename = options.DesktopWallpapers?.TryGetValue(node, out _) == true;
        string? filename = options.DesktopWallpapers?.GetValueOrDefault(node);
        show = (options.DesktopWallpaperVisibility?.GetValueOrDefault(node,
            options.DesktopWallpaperVisible) ?? options.DesktopWallpaperVisible) &&
            (bitmap is not null || !hasFilename || !string.IsNullOrEmpty(filename));
        if (!show) return null;
        if (bitmap is not null) return bitmap;
        if (hasFilename) return GetBitmap(filename ?? string.Empty, Symbols(options));
        if (node.Name.Equals("LOADING", StringComparison.OrdinalIgnoreCase) &&
            (options.StartupWallpaperImage is not null || !string.IsNullOrEmpty(options.StartupWallpaper)))
            return GetStartupWallpaper(options, Symbols(options));
        return GetBitmap(node.LayoutBase?.GetAttribute("background") ?? string.Empty, Symbols(options));
    }

    public static PangyaUiRenderOptions SetDesktopWallpaper(PangyaUiNode node, string? filename,
        bool show, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        if (options.LockedDesktopWallpapers?.Contains(node) == true) return options;
        var filenames = options.DesktopWallpapers?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var images = options.DesktopWallpaperImages?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var visibility = options.DesktopWallpaperVisibility?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        string current = filenames.GetValueOrDefault(node) ?? node.LayoutBase?.GetAttribute("background") ?? string.Empty;
        if (string.IsNullOrEmpty(filename) || !string.Equals(current, filename, StringComparison.OrdinalIgnoreCase))
        {
            filenames[node] = filename ?? string.Empty;
            images.Remove(node);
        }
        visibility[node] = !string.IsNullOrEmpty(filename) && show;
        return options with { DesktopWallpapers = filenames, DesktopWallpaperImages = images,
            DesktopWallpaperVisibility = visibility };
    }

    public static PangyaUiRenderOptions SetDesktopWallpaperFromBitmap(PangyaUiNode node, Image? image,
        bool show, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var visibility = options.DesktopWallpaperVisibility?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        visibility[node] = show;
        if (image is null) return options with { DesktopWallpaperVisibility = visibility };
        var images = options.DesktopWallpaperImages?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var filenames = options.DesktopWallpapers?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        images[node] = image;
        filenames[node] = string.Empty;
        return options with { DesktopWallpapers = filenames, DesktopWallpaperImages = images,
            DesktopWallpaperVisibility = visibility };
    }

    private static byte GetWindowOpacity(PangyaUiNode node, PangyaUiRenderOptions options,
        byte opacity = byte.MaxValue, bool secondary = true) =>
        (byte)(opacity * GetWindowAlphaScale(node, options, secondary));

    private static double GetWindowAlphaScale(PangyaUiNode node, PangyaUiRenderOptions options,
        bool secondary = true)
    {
        float primary = options.WindowAlpha?.GetValueOrDefault(node, 1) ?? 1;
        float second = secondary ? options.WindowAlpha2?.GetValueOrDefault(node, 1) ?? 1 : 1;
        if (!float.IsFinite(primary)) primary = 1;
        if (!float.IsFinite(second)) second = 1;
        return (double)Math.Clamp(primary, 0, 1) * Math.Clamp(second, 0, 1);
    }

    private static byte GetScrollbarOpacity(PangyaUiNode node, PangyaUiRenderOptions options,
        byte opacity = byte.MaxValue)
    {
        float primary = options.ScrollbarAlpha?.GetValueOrDefault(node, 1) ?? 1;
        float secondary = options.ScrollbarAlpha2?.GetValueOrDefault(node, 1) ?? 1;
        if (!float.IsFinite(primary)) primary = 1;
        if (!float.IsFinite(secondary)) secondary = 1;
        return (byte)(opacity * (double)Math.Clamp(primary, 0, 1) * Math.Clamp(secondary, 0, 1));
    }

    public static PangyaUiRenderOptions SetWindowSecondaryAlphaToChildren(PangyaUiNode node, float alpha,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!float.IsFinite(alpha)) throw new ArgumentOutOfRangeException(nameof(alpha));
        options ??= new();
        var values = options.WindowAlpha2?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var scrollbars = options.ScrollbarAlpha2?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var frames = options.FormFrameAlpha2?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var comboButtons = options.ComboButtonAlpha2?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var gaugeButtons = options.GaugeButtonAlpha2?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var pending = new Stack<PangyaUiNode>();
        pending.Push(node);
        while (pending.TryPop(out PangyaUiNode? current))
        {
            values[current] = alpha;
            scrollbars[current] = alpha;
            if (current.IsForm && !current.IsLayout) frames[current] = alpha;
            if (IsCombo(current)) comboButtons[current] = alpha;
            if (IsExtendedGauge(current))
            {
                gaugeButtons[(current, false)] = alpha;
                gaugeButtons[(current, true)] = alpha;
            }
            foreach (PangyaUiNode child in current.Children) pending.Push(child);
        }
        return options with { WindowAlpha2 = values, ScrollbarAlpha2 = scrollbars,
            FormFrameAlpha2 = frames, ComboButtonAlpha2 = comboButtons, GaugeButtonAlpha2 = gaugeButtons };
    }

    public static bool IsWindowViewFocused(PangyaUiNode node, PangyaUiRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(options);
        if (options.ViewFocusedNodes is null && options.FixedNodes is null) return options.IsViewFocused;
        for (PangyaUiNode? current = node; current is not null; current = current.Parent)
        {
            if (options.ViewFocusedNodes?.Contains(current) == true || options.FixedNodes?.Contains(current) == true)
                return true;
            if (current.IsForm) return false;
        }
        return false;
    }

    public static PangyaUiRenderOptions SetWindowViewFocus(PangyaUiNode node, bool takeFromOthers = true,
        PangyaUiRenderOptions? options = null, PangyaUiNode? modalForm = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        if (modalForm is not null && !ReferenceEquals(modalForm, node)) return options;
        var focused = takeFromOthers ? new HashSet<PangyaUiNode>() : options.ViewFocusedNodes?.ToHashSet() ?? [];
        PangyaUiNode target = node;
        while (target.Parent is not null && !target.IsForm) target = target.Parent;
        focused.Add(target);
        return options with { ViewFocusedNodes = focused };
    }

    public static PangyaUiRenderOptions ResetWindowViewFocus(PangyaUiNode node,
        PangyaUiRenderOptions? options = null, PangyaUiNode? stop = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        if (ReferenceEquals(node, stop)) return options;
        var focused = options.ViewFocusedNodes?.ToHashSet() ?? [];
        var pending = new Stack<PangyaUiNode>();
        pending.Push(node);
        while (pending.TryPop(out PangyaUiNode? current))
        {
            if (ReferenceEquals(current, stop)) continue;
            focused.Remove(current);
            foreach (PangyaUiNode child in current.Children) pending.Push(child);
        }
        return options with { ViewFocusedNodes = focused };
    }

    public static bool TryMoveCursorToChild(PangyaUiNode parent, string name, PangyaUiRenderOptions options,
        out PangyaUiRenderOptions updatedOptions, PangyaUiNode? modalForm = null)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);
        updatedOptions = options;
        if (modalForm is not null && !ReferenceEquals(parent, modalForm)) return false;
        PangyaUiNode? child = parent.Children.FirstOrDefault(candidate => candidate.Name == name);
        if (child is null) return false;
        Rectangle bounds = GetAbsoluteBounds(child, options);
        Point position = new(ClampCoordinate(((long)bounds.X * 2 + bounds.Width) / 2),
            ClampCoordinate(((long)bounds.Y * 2 + bounds.Height) / 2));
        PangyaUiCursorPreview cursor = options.CursorPreview is { } current
            ? current with { Position = position } : new(position, options.CursorIndex);
        updatedOptions = options with { CursorPreview = cursor, Pointer = position };
        return true;
    }

    public bool CanDrawCursor(int index, IReadOnlySet<string> symbols) => GetCursorFrameCount(index, symbols) > 0;

    public int GetCursorFrameCount(int index, IReadOnlySet<string> symbols) =>
        GetCursorSprite(index, symbols)?.Width / 32 ?? 0;

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
        PangyaUiRenderOptions options, PangyaUiTooltipPreview? tooltipState)
    {
        const int cap = 8;
        IReadOnlySet<string> symbols = Symbols(options);
        int style = Math.Clamp(tooltipState?.Style ?? options.TooltipStyle, 0, 3);
        byte alpha = options.TooltipAlpha;
        const string tooltipFontFamily = "MS UI Gothic";
        using var gothicFont = new Font(tooltipFontFamily, 11, GraphicsUnit.Pixel);
        using Font? fallbackFont = gothicFont.Name.Equals(tooltipFontFamily, StringComparison.OrdinalIgnoreCase)
            ? null : new Font(SystemFonts.DefaultFont.FontFamily, 11, GraphicsUnit.Pixel);
        Font font = fallbackFont ?? gothicFont;
        const float lineSpacing = 2;
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        string[] lines = PangyaUiText.ComposeLines(text);
        if (lines.Length == 0) return;
        text = string.Join("\n", lines);
        float textWidth = PangyaUiText.GetLongestLineWidth(text, MeasureRun);
        float lineHeight = options.TooltipFontHeight is > 0 ? options.TooltipFontHeight.Value : font.GetHeight(graphics);
        float textHeight = lineHeight * lines.Length + lineSpacing * (lines.Length - 1);
        int width = (int)Math.Ceiling(textWidth) + cap * 2;
        int height = Math.Max(cap * 2, (int)Math.Ceiling(textHeight) + cap);
        int x = (int)Math.Min(Math.Max((long)pointer.X + 20, 0), canvas.Width - width);
        int cursorOffsetY = (options.CursorPreview?.Index ?? options.CursorIndex) != 0 ? 30 : 20;
        int y = (int)Math.Min(Math.Max((long)pointer.Y + cursorOffsetY, 0), canvas.Height - height);
        if (tooltipState?.Position is Point fixedPosition)
        {
            x = fixedPosition.X;
            y = fixedPosition.Y;
        }
        var bounds = new Rectangle(x, y, width, height);
        GraphicsState state = graphics.Save();
        GraphicsAlphaState alphaState = GraphicsAlpha.GetValue(graphics, _ => new());
        float previousAlpha = alphaState.Alpha;
        // Native tooltip skins bypass the shared GDI alpha, and tooltip text
        // temporarily replaces it with the tooltip's own alpha. Brushes/tints
        // below already carry that tooltip alpha, so do not multiply it again.
        alphaState.Alpha = 1;
        try
        {
            graphics.SetClip(new Rectangle(Point.Empty, canvas), CombineMode.Intersect);
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
                    if (skin[row * 3 + column] is Image slice && widths[column] > 0 && heights[row] > 0)
                        DrawTintedImage(graphics, slice, new Rectangle(left, top, widths[column], heights[row]),
                            0, slice.Width, slice.Height, Color.FromArgb(alpha, Color.White));
                    top += heights[row];
                }
                left += widths[column];
            }
            using var foreground = new SolidBrush(Color.FromArgb(alpha, Color.Black));
            using var outline = new SolidBrush(Color.FromArgb(alpha, Color.White));
            graphics.SetClip(bounds, CombineMode.Intersect);
            DrawTaggedCaption(graphics, text, foreground, outline, new PointF(x + cap, y + 5),
                format, 0, font, alpha, lineSpacing: lineSpacing, lineHeight: lineHeight, measureText: options.MeasureTooltipText, drawText: options.DrawTooltipText);
        }
        finally
        {
            alphaState.Alpha = previousAlpha;
            graphics.Restore(state);
        }

        float MeasureRun(string run) => options.MeasureTooltipText?.Invoke(run) ??
            graphics.MeasureString(run, font, PointF.Empty, format).Width;

    }

    public IReadOnlyList<PangyaUiNode> GetRenderOrder(PangyaUiDocument document,
        PangyaUiNode? selectedForm = null, PangyaUiNode? selectedNode = null,
        IReadOnlySet<string>? enabledSymbols = null, IReadOnlySet<PangyaUiNode>? topmostNodes = null) =>
        GetRenderOrderCore(document, selectedForm, selectedNode, enabledSymbols, topmostNodes, null);

    public IReadOnlyList<PangyaUiNode> GetRenderOrder(PangyaUiDocument document,
        PangyaUiNode? selectedForm, PangyaUiNode? selectedNode, IReadOnlySet<string>? enabledSymbols,
        IReadOnlySet<PangyaUiNode>? topmostNodes, PangyaUiRenderOptions options) =>
        GetRenderOrderCore(document, selectedForm, selectedNode, enabledSymbols, topmostNodes, options);

    private IReadOnlyList<PangyaUiNode> GetRenderOrderCore(PangyaUiDocument document,
        PangyaUiNode? selectedForm, PangyaUiNode? selectedNode, IReadOnlySet<string>? enabledSymbols,
        IReadOnlySet<PangyaUiNode>? topmostNodes, PangyaUiRenderOptions? options)
    {
        ArgumentNullException.ThrowIfNull(document);
        IReadOnlyList<PangyaUiNode> visible = selectedForm is null
            ? []
            : document.GetNodesForForm(selectedForm, enabledSymbols)
                .Where(node => HasVisibleParents(node, selectedForm, options) &&
                    !IsRuntimeHidden(node, options)).ToArray();
        if (selectedNode is not null && !selectedNode.IsForm && visible.Contains(selectedNode))
            visible = [.. visible.Where(node => !ReferenceEquals(node, selectedNode)), selectedNode];
        if (topmostNodes is null || topmostNodes.Count == 0) return visible;
        return visible.OrderBy(node => IsTopmost(node, topmostNodes))
            .ThenBy(node => IsTopmost(node, topmostNodes)
                ? GetTopmostRank(node, options?.TopmostOrder) : -1).ToArray();
    }

    public static PangyaUiRenderOptions AdjustFormToViewport(PangyaUiNode form, Size viewport,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(form);
        if (!form.IsForm) throw new ArgumentException("The node must be a form.", nameof(form));
        if (viewport.Width < 0 || viewport.Height < 0) throw new ArgumentOutOfRangeException(nameof(viewport));
        options ??= new();
        Rectangle bounds = GetAbsoluteBounds(form, options);
        long x = (long)bounds.X + bounds.Width > viewport.Width
            ? (long)viewport.Width - bounds.Width : bounds.X;
        long y = (long)bounds.Y + bounds.Height > viewport.Height
            ? (long)viewport.Height - bounds.Height : bounds.Y;
        if (x == bounds.X && y == bounds.Y) return options;
        Point previous = GetWindowOffset(form, options);
        var offsets = options.WindowOffsets?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        offsets[form] = new Point(ClampCoordinate(previous.X + x - bounds.X),
            ClampCoordinate(previous.Y + y - bounds.Y));
        return options with { WindowOffsets = offsets };
    }

    public static PangyaUiRenderOptions SetWindowEnabled(PangyaUiNode node, bool enabled,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var disabled = options.DisabledNodes?.ToHashSet() ?? [];
        if (enabled) disabled.Remove(node); else disabled.Add(node);
        options = options with { DisabledNodes = disabled };
        if (node.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase) || IsCombo(node) || IsExtendedGauge(node))
        {
            var buttons = options.ImageButtonStates?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
            PangyaUiImageButtonState state = buttons.GetValueOrDefault(node) ?? new();
            PangyaUiButtonState status = state.Status ?? options.ButtonState;
            buttons[node] = state with { Enabled = enabled,
                Status = status == PangyaUiButtonState.Hover ? PangyaUiButtonState.Normal : status };
            options = options with { ImageButtonStates = buttons };
        }
        return options;
    }

    public static PangyaUiRenderOptions MoveWindow(PangyaUiNode node, Point position,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        Rectangle current = GetAbsoluteBounds(node, options);
        long dx = (long)position.X - current.X;
        long dy = (long)position.Y - current.Y;
        var offsets = options.WindowOffsets?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        Point oldOffset = GetWindowOffset(node, options);
        offsets[node] = new Point(ClampCoordinate(oldOffset.X + dx), ClampCoordinate(oldOffset.Y + dy));
        var moved = new List<PangyaUiNode> { node };
        var pending = new Stack<PangyaUiNode>(node.Children);
        while (pending.TryPop(out PangyaUiNode? child))
        {
            if (child.IsForm && !child.IsLayout)
            {
                Point previous = GetWindowOffset(child, options);
                offsets[child] = new Point(ClampCoordinate(previous.X - dx), ClampCoordinate(previous.Y - dy));
                continue;
            }
            moved.Add(child);
            foreach (PangyaUiNode descendant in child.Children) pending.Push(descendant);
        }
        var areaClips = options.AreaClipOverrides?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        foreach (PangyaUiNode child in moved)
            if (IsArea(child) && !areaClips.ContainsKey(child) && !child.Parameters.ContainsKey("clip_area"))
                areaClips[child] = GetAbsoluteBounds(child, options);
        options = options with { WindowOffsets = offsets, AreaClipOverrides = areaClips };
        var clips = options.CaptionClips?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var enabled = options.CaptionClippingEnabled?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        foreach (PangyaUiNode child in moved)
        {
            if (!child.Type.Equals("STATIC", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(child.Parameters.GetValueOrDefault("scrolling"), "true", StringComparison.OrdinalIgnoreCase))
                continue;
            clips[child] = GetAbsoluteBounds(child, options);
            enabled[child] = true;
        }
        return options with { CaptionClips = clips, CaptionClippingEnabled = enabled };
    }

    public PangyaUiRenderOptions SetWindowRectangle(PangyaUiNode node, Rectangle rectangle,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (rectangle.Width < 0 || rectangle.Height < 0)
            throw new ArgumentOutOfRangeException(nameof(rectangle));
        options = MoveWindow(node, rectangle.Location, options);
        var sizes = options.WindowSizes?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        sizes[node] = rectangle.Size;
        options = options with { WindowSizes = sizes };
        var rows = options.ViewerTopRows?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var fractionalRows = options.ViewerScrollRows?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        // FrWnd::SetRect calls OnResize on itself and each immediate child.
        foreach (PangyaUiNode resized in node.Children.Prepend(node))
        {
            if (!IsViewer(resized) || resized.Parameters.GetValueOrDefault("use_wheel") != "true" ||
                GetViewerImage(resized, Symbols(options), options) is not Image image) continue;
            int top = options.ViewerFollowBottom?.GetValueOrDefault(resized) == true
                ? Math.Max(0, image.Height / ViewerRowHeight - GetWindowBounds(resized, options).Height / ViewerRowHeight) : 0;
            rows[resized] = top;
            fractionalRows[resized] = top;
        }
        return options with { ViewerTopRows = rows, ViewerScrollRows = fractionalRows };
    }

    public static PangyaUiRenderOptions SetWindowVisible(PangyaUiNode node, bool visible,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var hidden = options.HiddenNodes?.ToHashSet() ?? [];
        var shown = options.VisibleNodes?.ToHashSet() ?? [];
        if (visible) { hidden.Remove(node); shown.Add(node); }
        else { shown.Remove(node); hidden.Add(node); }
        options = options with { HiddenNodes = hidden, VisibleNodes = shown };
        if (visible && node.Type.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase) &&
            options.MenuStates?.TryGetValue(node, out PangyaUiMenuState? state) == true)
        {
            var menus = options.MenuStates.ToDictionary(pair => pair.Key, pair => pair.Value);
            menus[node] = state with { DisabledRows = null, DisabledTexts = null };
            options = options with { MenuStates = menus };
        }
        return options;
    }

    public static PangyaUiRenderOptions SetWindowTopmost(PangyaUiNode node, bool topmost,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var nodes = options.TopmostNodes?.ToHashSet() ?? [];
        var order = options.TopmostOrder?.ToList() ?? nodes.ToList();
        if (topmost)
        {
            if (nodes.Add(node)) order.Add(node);
        }
        else
        {
            nodes.RemoveWhere(candidate =>
            {
                for (PangyaUiNode? current = candidate; current is not null; current = current.Parent)
                    if (ReferenceEquals(current, node)) return true;
                return false;
            });
        }
        order.RemoveAll(candidate => !nodes.Contains(candidate));
        return options with { TopmostNodes = nodes, TopmostOrder = order };
    }

    private static int GetTopmostRank(PangyaUiNode node, IReadOnlyList<PangyaUiNode>? order)
    {
        if (order is null) return int.MaxValue;
        for (PangyaUiNode? current = node; current is not null; current = current.Parent)
            for (int index = 0; index < order.Count; index++)
                if (ReferenceEquals(order[index], current)) return index;
        return int.MaxValue;
    }

    private static bool IsTopmost(PangyaUiNode node, IReadOnlySet<PangyaUiNode> topmostNodes)
    {
        for (PangyaUiNode? current = node; current is not null; current = current.Parent)
            if (topmostNodes.Contains(current)) return true;
        return false;
    }

    private static bool HasVisibleParents(PangyaUiNode node, PangyaUiNode root,
        PangyaUiRenderOptions? options)
    {
        if (ReferenceEquals(node, root)) return true;
        for (PangyaUiNode? parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            if (!IsRenderVisible(parent, options)) return false;
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
            if ((HasButtonSkin(node) && node.Parameters.GetValueOrDefault("blink") is { Length: > 0 } blink &&
                 GetBitmap(blink, symbols) is not null) ||
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
                options.EnabledSymbols, options.TopmostNodes, options)
            .LastOrDefault(node =>
            {
                if (IsRuntimeHidden(node, options)) return false;
                if (GetList(node, options) is not null && !options.ShowDebugBounds)
                    return HitTestListItem(node, point, options) >= 0;
                if (GetTabs(node, options) is not null && !options.ShowDebugBounds)
                    return HitTestTab(node, point, options) >= 0;
                if (IsTextButton(node) && !options.ShowDebugBounds)
                    return HitTestTextButtonSegment(node, point, options) >= 0;
                Rectangle bounds = GetRenderedBounds(node, options);
                return bounds.Width > 0 && bounds.Height > 0 && bounds.Contains(point);
            });
    }

    public Rectangle GetRenderedBounds(PangyaUiNode node,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new PangyaUiRenderOptions();
        if (IsRuntimeHidden(node, options)) return Rectangle.Empty;
        IReadOnlySet<string> symbols = Symbols(options);
        Rectangle absoluteBounds = GetAbsoluteBounds(node, options);
        if (IsRenderVisible(node, options) && IsContextMenu(node))
        {
            using var bitmap = new Bitmap(1, 1);
            using Graphics metrics = Graphics.FromImage(bitmap);
            return RenderContextMenu(metrics, node, absoluteBounds.Location, options, draw: false, out _);
        }
        if (IsRenderVisible(node, options) && GetList(node, options) is not null)
            return absoluteBounds;
        if (IsRenderVisible(node, options) && GetTabs(node, options) is { } tabs)
            return RenderTabs(null, node, absoluteBounds.Location, tabs, symbols, options);
        if (!IsRenderVisible(node, options) || IsTransparent(node, options.ButtonState))
            return options.ShowDebugBounds ? absoluteBounds : Rectangle.Empty;
        if (IsTextButton(node) && !options.ShowDebugBounds)
        {
            string textButton = GetTextButtonCaption(node, options);
            if (textButton.Length == 0) return Rectangle.Empty;
            using var bitmap = new Bitmap(1, 1);
            using Graphics metrics = Graphics.FromImage(bitmap);
            DrawTextButton(metrics, node, absoluteBounds, textButton,
                options with { Pointer = null }, out Rectangle measuredBounds, draw: false);
            return measuredBounds;
        }
        if (node.LayoutBase is not null || GetStaticCaption(node, options).Length > 0 ||
            GetTextButtonCaption(node, options).Length > 0)
            return absoluteBounds;
        if (IsAreaEntranceFinished(node, options)) return Rectangle.Empty;
        bool hasAreaOwner = IsArea(node) && options.AreaOwnerDraw?.ContainsKey(node) == true;
        if (IsViewer(node))
            return DrawViewer(null, node, absoluteBounds, options);
        if (IsEdit(node)) return GetEditBounds(node, absoluteBounds, symbols, options);
        if (IsGauge(node)) return absoluteBounds;
        if (IsImageGauge(node))
            return RenderImageGauge(null, node, absoluteBounds.Location, symbols, options);
        if (HasButtonSkin(node, options))
            return GetButtonBounds(node, absoluteBounds, symbols, options);

        string resource = GetRenderResource(node, options.ButtonState, options.AreaImages);
        string resourceType = IsArea(node) && options.AreaImages?.ContainsKey(node) == true ? "BITMAP" : node.Type;
        Image? areaBitmap = IsArea(node) ? options.AreaBitmapImages?.GetValueOrDefault(node) : null;
        PangyaUiNode? definition = areaBitmap is null
            ? _resources.TryResolve(resource, resourceType, symbols) : null;
        if (GetBitmapFilename(definition) is string bitmapFilename)
        {
            resource = bitmapFilename;
            definition = null;
        }
        if (IsMacroItem(node))
        {
            if (definition is null) return Rectangle.Empty;
            Rectangle expanded = GetReferencedBounds(definition, options,
                new HashSet<PangyaUiNode>(ReferenceEqualityComparer.Instance), GetParentOffset(node, options));
            if (!expanded.IsEmpty) expanded.Offset(GetParentOffset(node, options));
            return expanded;
        }
        if (definition is not null)
        {
            Rectangle bounds = absoluteBounds;
            if (definition.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase))
                return GetBaseFrameBounds(bounds, definition, symbols, node.Parameters.GetValueOrDefault("bgimg"));
            if (definition.IsForm && !definition.IsLayout &&
                definition.Bounds.Size is { Width: > 0, Height: > 0 } formSize)
                return GetInstantiatedFormBounds(definition, new Rectangle(bounds.Location,
                    options.WindowSizes?.ContainsKey(node) == true ? bounds.Size : formSize), options,
                    new HashSet<PangyaUiNode>(ReferenceEqualityComparer.Instance));
            if (bounds.Width > 0 && bounds.Height > 0) return bounds;
            Rectangle referenced = GetReferencedBounds(definition, options,
                new HashSet<PangyaUiNode>(ReferenceEqualityComparer.Instance), bounds.Location);
            referenced.Offset(bounds.Location);
            return referenced;
        }
        Image? image = areaBitmap ?? (string.IsNullOrWhiteSpace(resource) ? null : _images.GetImage(resource));
        if (image is null) return hasAreaOwner || options.ShowDebugBounds ? absoluteBounds : Rectangle.Empty;
        Rectangle destination = ImageBounds(node, absoluteBounds, image);
        Rectangle? entranceClip = ApplyAreaEntrance(node, absoluteBounds, image, options, ref destination);
        if (hasAreaOwner) return entranceClip.HasValue ? destination : absoluteBounds;
        return entranceClip ?? ClipImageBounds(node, absoluteBounds, destination, options);
    }

    private void DrawNode(Graphics graphics, PangyaUiNode node, Point offset,
        PangyaUiRenderOptions options, HashSet<PangyaUiNode> resourceChain,
        List<(PangyaUiNode Node, Rectangle Bounds, PangyaUiComboPreview Combo)> dropdowns,
        List<(PangyaUiNode Node, Point Offset, HashSet<PangyaUiNode> Chain)> topmost, bool drawTopmost = false)
    {
        if (IsRuntimeHidden(node, options)) return;
        if (options.DeferredDisplayContainers?.Contains(node) == true) return;
        if (!drawTopmost && resourceChain.Count > 0 && options.TopmostNodes?.Contains(node) == true)
        {
            topmost.Add((node, offset, new HashSet<PangyaUiNode>(resourceChain, ReferenceEqualityComparer.Instance)));
            return;
        }
        Point windowOffset = GetWindowOffset(node, options);
        offset = new Point(ClampCoordinate((long)offset.X + windowOffset.X),
            ClampCoordinate((long)offset.Y + windowOffset.Y));
        if (IsRenderVisible(node, options) && GetList(node, options) is PangyaUiListPreview list)
        {
            var bounds = new Rectangle(ClampCoordinate((long)offset.X + GetWindowBounds(node, options).X),
                ClampCoordinate((long)offset.Y + GetWindowBounds(node, options).Y), GetWindowBounds(node, options).Width, GetWindowBounds(node, options).Height);
            DrawList(graphics, node, bounds, list, options);
            return;
        }
        if (IsRenderVisible(node, options) && GetTabs(node, options) is { } tabs)
        {
            RenderTabs(graphics, node, new Point(ClampCoordinate((long)offset.X + GetWindowBounds(node, options).X),
                ClampCoordinate((long)offset.Y + GetWindowBounds(node, options).Y)), tabs, Symbols(options), options);
            return;
        }
        if (IsGroupBox(node))
        {
            // The top-level render order already includes inline children.
            if (IsRenderVisible(node, options) && resourceChain.Count > 0)
            {
                Point childOffset = new(ClampCoordinate((long)offset.X + GetWindowBounds(node, options).X),
                    ClampCoordinate((long)offset.Y + GetWindowBounds(node, options).Y));
                foreach (PangyaUiNode child in node.Children.Where(child => child.IsVisible(Symbols(options))))
                    DrawNode(graphics, child, childOffset, options, resourceChain, dropdowns, topmost, drawTopmost);
            }
            return;
        }
        if (IsRenderVisible(node, options) && node.IsForm && !node.IsLayout &&
            options.ModalForms?.Contains(node) == true)
        {
            bool modeless = options.FormFlags?.TryGetValue(node, out uint flags) == true
                ? (flags & 0x4) != 0 : node.Parameters.ContainsKey("modal");
            if (!modeless)
            {
                using var backdrop = new SolidBrush(Color.FromArgb(110, Color.Black));
                graphics.FillRectangle(backdrop, new Rectangle(Point.Empty, options.RenderCanvasSize));
            }
        }
        if (!IsRenderVisible(node, options) || IsTransparent(node, options.ButtonState)) return;
        IReadOnlySet<string> symbols = Symbols(options);
        Rectangle nodeBounds = GetWindowBounds(node, options);
        nodeBounds.Offset(offset);
        if (IsContextMenu(node))
        {
            RenderContextMenu(graphics, node, nodeBounds.Location, options, draw: true, out _);
            return;
        }
        if (IsImageGauge(node))
        {
            RenderImageGauge(graphics, node, nodeBounds.Location, symbols, options);
            return;
        }
        if (IsGauge(node))
        {
            PangyaUiGaugeRange? range = options.GaugeRanges?.GetValueOrDefault(node);
            double value = options.GaugeValues?.GetValueOrDefault(node) ?? 0;
            if (IsExtendedGauge(node) && options.ExtendedGaugeSteps?.TryGetValue(node, out int step) == true)
            {
                int lower = range?.Lower ?? 0;
                int upper = range?.Upper ?? 100;
                value = lower + (upper - (double)lower) *
                    Math.Clamp(step / (double)GetExtendedGaugeStepCount(node, options), 0, 1);
            }
            DrawGauge(graphics, node, nodeBounds, value, symbols, options,
                enabled: options.DisabledNodes?.Contains(node) != true,
                expansion: options.GaugeExpansions?.GetValueOrDefault(node) ?? 0,
                range: range, transition: options.GaugeTransitions?.GetValueOrDefault(node));
            return;
        }
        if (IsEdit(node))
        {
            Rectangle editBounds = GetEditBounds(node, nodeBounds, symbols, options);
            PangyaUiComboPreview? combo = IsCombo(node) ? options.Combos?.GetValueOrDefault(node) : null;
            IReadOnlyList<string>? lines = options.EditLines?.GetValueOrDefault(node);
            PangyaUiComboItem? selected = combo is not null && (uint)combo.SelectedIndex < (uint)combo.Items.Count
                ? combo.Items[combo.SelectedIndex] : null;
            bool imageSelection = IsImageCombo(node);
            if (lines is null && selected is not null && !imageSelection)
                lines = [LimitComboText(node, selected.Text)];
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
                    string? selectedImage = combo?.ImageSelection is { } selection
                        ? selection.Image : selected?.Image;
                    if (imageSelection && selectedImage is { Length: > 0 } filename &&
                        GetBitmap(filename, symbols) is Image icon)
                        DrawGraphicsImage(graphics, icon, GetComboIconBounds(node, buttonBounds.Location, GetGaugeImage(node, "normal", symbols)?.Width ?? editBounds.Width, icon.Size));
                }
            }
            if (combo?.IsOpen == true)
                dropdowns.Add((node, editBounds, combo));
            return;
        }
        if (IsTextButton(node))
        {
            string textButton = GetTextButtonCaption(node, options);
            if (textButton.Length == 0) return;
            GraphicsState state = graphics.Save();
            try
            {
                if (options.CaptionClippingEnabled?.GetValueOrDefault(node, true) != false &&
                    options.CaptionClips?.TryGetValue(node, out Rectangle clip) == true)
                    graphics.SetClip(clip, CombineMode.Intersect);
                DrawTextButton(graphics, node, nodeBounds, textButton, options, out _);
            }
            finally
            {
                graphics.Restore(state);
            }
            return;
        }
        if (HasButtonSkin(node, options))
        {
            PangyaUiRenderOptions buttonOptions = options;
            if (options.ButtonState == PangyaUiButtonState.Normal)
            {
                if (ReferenceEquals(options.PressedButton, node) ||
                    options.LatchedButtons?.Contains(node) == true ||
                    IsDefaultMakeRoomModeButton(node, options))
                    buttonOptions = options with { ButtonState = PangyaUiButtonState.Selected };
                else if (ReferenceEquals(options.HoveredNode, node))
                    buttonOptions = options with { ButtonState = PangyaUiButtonState.Hover };
            }
            DrawButton(graphics, node, GetButtonBounds(node, nodeBounds, symbols, options), buttonOptions);
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
                Color.FromArgb(46, 49, 55), implicitAlpha: false));
            graphics.FillRectangle(background, nodeBounds);
            Image? layoutImage = GetDesktopWallpaper(node, options, out bool showWallpaper);
            if (layoutImage is not null)
                DrawDesktopWallpaper(graphics, layoutImage, nodeBounds,
                    options.DesktopWallpaperTints?.GetValueOrDefault(node, options.DesktopWallpaperTint) ??
                    options.DesktopWallpaperTint);
            if (showWallpaper && node.Element["anibg"] is XmlElement animation &&
                GetBitmap(animation.GetAttribute("img"), symbols) is Image animationImage)
            {
                Rectangle destination = PangyaUiNode.ParseBounds(animation);
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
                        PointF sourceOffset = options.DesktopAnimationOffsets?.TryGetValue(node, out PointF supplied) == true &&
                            float.IsFinite(supplied.X) && float.IsFinite(supplied.Y)
                            ? supplied
                            : new PointF(AnimatedSourceOffset(velocity.X, elapsed, animationImage.Width - width),
                                AnimatedSourceOffset(velocity.Y, elapsed, animationImage.Height - height));
                        PointF[] corners = [clip.Location, new(clip.Right, clip.Top), new(clip.Left, clip.Bottom)];
                        var source = new RectangleF(sourceOffset.X + clip.X - imageBounds.X,
                            sourceOffset.Y + clip.Y - imageBounds.Y, clip.Width, clip.Height);
                        graphics.DrawImage(animationImage, corners, source, GraphicsUnit.Pixel);
                    }
                }
            }
            return;
        }

        Rectangle areaOwnerBounds = nodeBounds;
        string resource = GetRenderResource(node, options.ButtonState, options.AreaImages);
        string resourceType = IsArea(node) && options.AreaImages?.ContainsKey(node) == true ? "BITMAP" : node.Type;
        Image? areaBitmap = IsArea(node) ? options.AreaBitmapImages?.GetValueOrDefault(node) : null;
        PangyaUiNode? definition = areaBitmap is null
            ? _resources.TryResolve(resource, resourceType, symbols) : null;
        if (GetBitmapFilename(definition) is string bitmapFilename)
        {
            resource = bitmapFilename;
            definition = null;
        }
        if (definition is not null)
        {
            if (!resourceChain.Add(definition)) return;
            if (definition.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase))
                DrawFrame(graphics, nodeBounds, definition, symbols, IsFrameBlinkVisible(node, options),
                    node.Parameters.GetValueOrDefault("bgimg"), node, IsWindowViewFocused(node, options),
                    hideDescription: IsPrivacyHidden(node, options), textOptions: options, frameInstance: node);
            else
            {
                if (definition.IsForm && !definition.IsLayout)
                {
                    if (options.WindowSizes?.ContainsKey(node) != true &&
                        definition.Bounds.Size is { Width: > 0, Height: > 0 } formSize)
                        nodeBounds.Size = formSize;
                    string frameResource = definition.GetResource(options.ButtonState);
                    PangyaUiNode? frame = _resources.TryResolve(frameResource, "FRAME", symbols);
                    if (frame?.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) == true)
                        DrawFrame(graphics, nodeBounds, frame, symbols, IsFrameBlinkVisible(node, options),
                            textSource: definition, isViewFocused: IsWindowViewFocused(node, options),
                            hideDescription: IsPrivacyHidden(node, options) || IsPrivacyHidden(definition, options),
                            textOptions: options, frameInstance: node);
                }
                // Fresh creates macro children directly under the instance's parent.
                Point childOffset = IsMacroItem(node) ? offset : nodeBounds.Location;
                foreach (PangyaUiNode child in definition.Children.Where(child =>
                             child.IsVisible(symbols)))
                    DrawNode(graphics, child, childOffset, options, resourceChain, dropdowns, topmost, drawTopmost);
            }
            resourceChain.Remove(definition);
            return;
        }

        if (IsMacroItem(node)) return;
        Image? image = areaBitmap ?? (string.IsNullOrWhiteSpace(resource) ? null : _images.GetImage(resource));
        if (image is not null)
        {
            Rectangle destination = ImageBounds(node, nodeBounds, image);
            Rectangle? entranceClip = ApplyAreaEntrance(node, nodeBounds, image, options, ref destination);
            if (entranceClip.HasValue) areaOwnerBounds = destination;
            Rectangle clip = entranceClip ?? ClipImageBounds(node, nodeBounds, destination, options);
            if (clip.Width > 0 && clip.Height > 0)
            {
                GraphicsState state = graphics.Save();
                try
                {
                    graphics.SetClip(clip, CombineMode.Intersect);
                    if (IsArea(node))
                    {
                        Color tint = ParseColor(node.Parameters.GetValueOrDefault("color"), Color.White, implicitAlpha: false);
                        byte opacity = options.AreaOpacity?.GetValueOrDefault(node, byte.MaxValue) ?? byte.MaxValue;
                        tint = Color.FromArgb((int)(tint.A * GetWindowAlphaScale(node, options) * opacity / 255), tint);
                        Rectangle visible = Rectangle.Intersect(clip, destination);
                        if (visible.Width > 0 && visible.Height > 0)
                        {
                            float scaleX = (float)image.Width / destination.Width;
                            float scaleY = (float)image.Height / destination.Height;
                            DrawTintedImage(graphics, image, visible, (visible.Y - destination.Y) * scaleY,
                                visible.Width * scaleX, visible.Height * scaleY, tint,
                                sourceX: (visible.X - destination.X) * scaleX);
                        }
                    }
                    else
                        DrawGraphicsImage(graphics, image, destination);
                }
                finally { graphics.Restore(state); }
            }
        }
        if (IsAreaEntranceFinished(node, options)) return;
        if (IsArea(node) && options.AreaOwnerDraw?.GetValueOrDefault(node) is { } drawArea)
        {
            GraphicsState state = graphics.Save();
            try { drawArea(graphics, areaOwnerBounds); }
            finally { graphics.Restore(state); }
        }
        string caption = GetStaticCaption(node, options);
        if (caption.Length > 0 && !IsPrivacyHidden(node, options))
        {
            node.Parameters.TryGetValue("color", out string? color);
            bool enabled = options.DisabledNodes?.Contains(node) != true;
            Color foreground = ParseColor(color, Color.Gray, implicitAlpha: false);
            (Color Foreground, Color Outline)? textColors =
                options.StaticTextColors?.TryGetValue(node, out var runtimeColors) == true ? runtimeColors : null;
            if (textColors.HasValue)
                foreground = textColors.Value.Foreground;
            double opacity = GetWindowAlphaScale(node, options) *
                (options.CaptionOpacity?.GetValueOrDefault(node, byte.MaxValue) ?? byte.MaxValue);
            using var brush = new SolidBrush(Color.FromArgb(enabled ? (int)(foreground.A * opacity / 255) : foreground.A * 2 / 5, foreground));
            using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
            int.TryParse(node.Parameters.GetValueOrDefault("align"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int alignment);
            format.Alignment = alignment switch
            {
                1 => StringAlignment.Center,
                2 => StringAlignment.Far,
                _ => StringAlignment.Near
            };
            int.TryParse(node.Parameters.GetValueOrDefault("style"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int style);
            int initialStyle = style;
            if (options.StaticTextStyles?.TryGetValue(node, out int runtimeStyle) == true)
                style = runtimeStyle;
            Color edge = (initialStyle & 6) != 0
                ? ParseColor(node.Parameters.GetValueOrDefault("color2"), Color.Transparent, implicitAlpha: false)
                : Color.White;
            if (textColors.HasValue)
                edge = textColors.Value.Outline;
            using var outline = new SolidBrush(enabled ? Color.FromArgb((int)(edge.A * opacity / 255), edge) : Color.White);
            bool smallFont = options.SmallFontCaptions?.Contains(node) == true;
            using Font? fallbackSmallFont = smallFont ? new Font(SystemFonts.DefaultFont.FontFamily, 11, GraphicsUnit.Pixel) : null;
            Font captionFont = fallbackSmallFont ?? SystemFonts.DefaultFont;
            var measureText = smallFont ? options.MeasureTooltipText : options.MeasureCaptionText;
            var drawText = smallFont ? options.DrawTooltipText : options.DrawCaptionText;
            int? fontHeight = smallFont ? options.TooltipFontHeight : options.CaptionFontHeight;
            Rectangle? clip = options.CaptionClips?.TryGetValue(node, out Rectangle customClip) == true ? customClip : null;
            float scroll = 0;
            if (IsScrollingCaption(node, options))
            {
                float width = MeasureCaptionLine(graphics, caption,
                    SystemFonts.DefaultFont, format, symbols: null, measureText: options.MeasureCaptionText);
                scroll = Math.Clamp(options.CaptionScrollOffsets?.GetValueOrDefault(node) ?? 0,
                    0, width > nodeBounds.Width ? width : 0);
                clip ??= nodeBounds;
            }
            if (smallFont || options.CaptionClippingEnabled?.GetValueOrDefault(node, true) == false)
                clip = null;

            using Region? iconClip = clip.HasValue ? graphics.Clip : null;
            GraphicsState? state = clip.HasValue ? graphics.Save() : null;
            try
            {
                if (clip is Rectangle area) graphics.SetClip(area, CombineMode.Intersect);
                DrawTaggedCaption(graphics, caption, brush, outline,
                    new PointF(nodeBounds.X - scroll, nodeBounds.Y), format, style, captionFont,
                    tagAlpha: enabled ? (byte)opacity : byte.MaxValue,
                    emoticonSymbols: symbols, animationElapsed: options.EmoticonAnimationTime,
                    lineHeight: fontHeight, measureText: measureText,
                    drawText: drawText, parseTags: false, forceOpaqueText: true,
                    emoticonClip: iconClip);
            }
            finally
            {
                if (state is not null) graphics.Restore(state);
            }
        }
    }

    private static readonly System.Text.RegularExpressions.Regex CaptionTags = PangyaUiText.Tokens;

    private static readonly System.Text.RegularExpressions.Regex EditCaptionTags = new(
        @"\\c(?<tag>[^\\\r\n]*)\\c",
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
        IReadOnlySet<string>? symbols, TimeSpan? animationElapsed = null, Func<string, float>? measureText = null)
    {
        int start = 0;
        float width = 0;
        foreach (System.Text.RegularExpressions.Match token in EmoticonTokens.Matches(line))
        {
            if (!TryGetEmoticon(token.Groups["name"].Value, symbols, out _, out Rectangle source,
                    out _, animationElapsed)) continue;
            width += MeasureRun(line[start..token.Index]) + source.Width;
            start = token.Index + token.Length;
        }
        return width + MeasureRun(line[start..]);

        float MeasureRun(string run) => measureText?.Invoke(run) ??
            graphics.MeasureString(run, font, PointF.Empty, format).Width;
    }

    private (Color Fill, Color Edge, int Style) DrawTaggedCaption(Graphics graphics, string caption, SolidBrush foreground,
        SolidBrush outline, PointF origin, StringFormat format, int style, Font? font = null,
        byte tagAlpha = byte.MaxValue, IReadOnlySet<string>? emoticonSymbols = null,
        TimeSpan? animationElapsed = null, bool useEditAnimationMarker = false, Color? defaultTagEdge = null, string? editAlignmentText = null, float editAvailableWidth = 0, bool justifyEditLine = false, bool splitEditWords = false, float lineSpacing = 0, float? lineHeight = null, Func<string, float>? measureText = null,
        Action<Graphics, string, Brush, PointF>? drawText = null, bool parseTags = true,
        bool forceOpaqueText = false, bool editTags = false, Color? defaultTagFill = null,
        bool measureEmoticons = true, Region? emoticonClip = null)
    {
        font ??= SystemFonts.DefaultFont;
        var tags = editTags ? EditCaptionTags : CaptionTags;
        if (!forceOpaqueText && !caption.Contains('\n') && drawText is null && lineSpacing == 0 && editAlignmentText is null && !useEditAnimationMarker && (!parseTags || !tags.IsMatch(caption)) && (emoticonSymbols is null || !EmoticonTokens.IsMatch(caption)))
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
            string visibleLine = editAlignmentText ?? (parseTags
                ? editTags ? tags.Replace(line, "") : PangyaUiText.RemoveTags(line) : line);
            if (useEditAnimationMarker) visibleLine = visibleLine.Replace("\b", "");
            TimeSpan? runAnimation = useEditAnimationMarker ? null : animationElapsed;
            float width = MeasureCaptionLine(graphics, visibleLine, font, runFormat,
                measureEmoticons ? emoticonSymbols : null, runAnimation, measureText);
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
                ? MathF.Truncate(measureText?.Invoke(" ") ?? graphics.MeasureString(" ", font, PointF.Empty, runFormat).Width) : 0;
            int start = 0;
            foreach (System.Text.RegularExpressions.Match match in parseTags ? tags.Matches(line).Cast<System.Text.RegularExpressions.Match>() : [])
            {
                if (!editTags && !PangyaUiText.IsTag(match.Groups["tag"].Value)) continue;
                DrawRun(line[start..match.Index]);
                string tag = editTags ? match.Groups["tag"].Value :
                    match.Groups["tag"].Value.Trim(' ', '\\', 'c');
                if (editTags)
                    ApplyEditTag(tag);
                else if (tag.Length == 1)
                    style = char.ToLowerInvariant(tag[0]) switch { 't' => 1, 'o' => 2, 's' => 4, _ => 0 };
                else
                {
                    string[] colors = tag.Split(',');
                    Color fillColor = Color.FromArgb(unchecked((int)PangyaUiText.ParseTagColor(colors[0])));
                    Color edgeColor = colors.Length > 1
                        ? Color.FromArgb(unchecked((int)PangyaUiText.ParseTagColor(colors[1])))
                        : defaultTagEdge ?? Color.White;
                    fill.Color = Color.FromArgb(fillColor.A * tagAlpha / 255, fillColor);
                    edge.Color = Color.FromArgb(edgeColor.A * tagAlpha / 255, edgeColor);
                }
                start = match.Index + match.Length;
            }
            string finalRun = line[start..];
            DrawRun(splitEditWords ? TrimEditAlignmentSpace(finalRun) : finalRun);
            y += (lineHeight ?? font.GetHeight(graphics)) + lineSpacing;

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
                    float top = y + (lineHeight ?? font.GetHeight(graphics)) - source.Height;
                    float right = x + source.Width;
                    PointF[] corners = flip ? [new(right, top), new(x, top), new(right, top + source.Height)]
                        : [new(x, top), new(right, top), new(x, top + source.Height)];
                    GraphicsState? imageState = emoticonClip is not null ? graphics.Save() : null;
                    try
                    {
                        if (emoticonClip is not null) graphics.SetClip(emoticonClip, CombineMode.Replace);
                        DrawGraphicsImage(graphics, image, corners, source);
                    }
                    finally { if (imageState is not null) graphics.Restore(imageState); }
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
                if (forceOpaqueText)
                {
                    fill.Color = Color.FromArgb(byte.MaxValue, fill.Color);
                    edge.Color = Color.FromArgb(byte.MaxValue, edge.Color);
                }
                DrawStyledText(graphics, run, fill, edge, new PointF(x, y), runFormat, style, font, drawText);
                float runWidth = measureText?.Invoke(run) ?? graphics.MeasureString(run, font, PointF.Empty, runFormat).Width;
                x += editAlignmentText is not null ? MathF.Truncate(runWidth) : runWidth;
            }
        }
        return (fill.Color, edge.Color, style);

        void ApplyEditTag(string tag)
        {
            Color fillColor = defaultTagFill ?? foreground.Color;
            Color edgeColor = defaultTagEdge ?? outline.Color;
            int colorCount = 0;
            foreach (string part in tag.Split(','))
            {
                string token = part.Trim();
                if (token.Length == 1)
                {
                    style = char.ToLowerInvariant(token[0]) switch
                    {
                        'n' => 0, 't' => 1, 'o' => 2, 's' => 4, _ => style
                    };
                    continue;
                }
                string hex = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? token[2..] : token;
                if (colorCount >= 2 || !uint.TryParse(hex, NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out uint value)) continue;
                Color color = Color.FromArgb(unchecked((int)value));
                if (colorCount == 0) fillColor = color;
                else edgeColor = color;
                colorCount++;
            }
            if (colorCount == 0) return;
            fill.Color = Color.FromArgb(fillColor.A * tagAlpha / byte.MaxValue, fillColor);
            edge.Color = Color.FromArgb(edgeColor.A * tagAlpha / byte.MaxValue, edgeColor);
        }
    }

    private static bool IsPrivacyHidden(PangyaUiNode node, PangyaUiRenderOptions options) =>
        options.HidePrivacy && options.PrivateNodes?.Contains(node) == true;

    private static bool IsRuntimeHidden(PangyaUiNode node, PangyaUiRenderOptions? options)
    {
        for (PangyaUiNode? current = node; current is not null; current = current.Parent)
            if (options?.HiddenNodes?.Contains(current) == true ||
                (!ReferenceEquals(current, node) && !IsRenderVisible(current, options)))
                return true;
        return false;
    }

    private static PangyaUiNode? GetDeferredDisplayContainer(PangyaUiNode node, PangyaUiRenderOptions options)
    {
        for (PangyaUiNode? current = node; current is not null; current = current.Parent)
        {
            if (options.DeferredDisplayContainers?.Contains(current) == true) return current;
            if (options.TopmostNodes?.Contains(current) == true) return null;
        }
        return null;
    }

    private static bool IsRenderVisible(PangyaUiNode node, PangyaUiRenderOptions? options) =>
        options?.VisibleNodes?.Contains(node) == true || node.IsRenderVisible;

    private static readonly System.Text.Encoding StaticCaptionEncoding =
        System.Text.CodePagesEncodingProvider.Instance.GetEncoding(932)
        ?? throw new InvalidOperationException("Shift-JIS encoding is unavailable.");

    private static string GetStaticCaption(PangyaUiNode node, PangyaUiRenderOptions? options)
    {
        if (!node.Type.Equals("STATIC", StringComparison.OrdinalIgnoreCase))
            return node.StaticCaption;
        string caption = options?.StaticCaptions?.GetValueOrDefault(node) ?? node.StaticCaption;
        if (options?.SmallFontCaptions?.Contains(node) == true) return caption;
        if (caption.Length <= 50 || StaticCaptionEncoding.GetByteCount(caption) <= 100) return caption;

        return TruncateShiftJis(caption, 100) + "...";
    }

    public static PangyaUiRenderOptions SetStaticCaption(PangyaUiNode node, string caption,
        bool smallFont = false, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(caption);
        if (!node.Type.Equals("STATIC", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Expected a static caption.", nameof(node));
        options ??= new();
        var captions = options.StaticCaptions?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        captions[node] = !smallFont && StaticCaptionEncoding.GetByteCount(caption) > 100
            ? TruncateShiftJis(caption, 100) + "..." : caption;
        var fonts = options.SmallFontCaptions?.ToHashSet() ?? [];
        if (smallFont) fonts.Add(node);
        if (smallFont || !string.Equals(node.Parameters.GetValueOrDefault("scrolling"), "true",
                StringComparison.OrdinalIgnoreCase))
            return options with { StaticCaptions = captions, SmallFontCaptions = fonts };
        var scrolling = options.CaptionScrollOffsets?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        scrolling[node] = 0;
        return SetCaptionClippingArea(node, GetAbsoluteBounds(node, options), options with
        {
            StaticCaptions = captions, SmallFontCaptions = fonts, CaptionScrollOffsets = scrolling
        });
    }

    public static PangyaUiRenderOptions SetTextButtonCaption(PangyaUiNode node, string caption,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(caption);
        if (!IsTextButton(node)) throw new ArgumentException("Expected a text button.", nameof(node));
        options ??= new();
        var captions = options.TextButtonCaptions?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var states = options.MenuStates?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        using var bitmap = new Bitmap(1, 1);
        using Graphics metrics = Graphics.FromImage(bitmap);
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        float Measure(string text) => options.MeasureCaptionText?.Invoke(text) ??
            metrics.MeasureString(text, SystemFonts.DefaultFont, PointF.Empty, format).Width;
        Rectangle bounds = GetWindowBounds(node, options);
        Rectangle current = bounds;
        bool initialized = options.TextButtonCaptionGeometryNodes?.Contains(node) == true;
        if (!initialized)
            DrawTextButton(metrics, node, bounds, GetTextButtonCaption(node, options),
                options with { Pointer = null }, out current, draw: false);
        bool centered = !node.Parameters.TryGetValue("center", out string? center) ||
            center.Equals("true", StringComparison.OrdinalIgnoreCase);
        int shift = centered ? (int)((current.Width - Measure(caption) + 1) / 2) : 0;
        string[] segments = caption.Split('|').TakeWhile(segment => segment.Length > 0).ToArray();
        float width = segments.Length > 0
            ? segments.Sum(Measure) + (segments.Length - 1) * Measure("|")
            : centered ? Measure(caption) : current.Width;
        var offsets = options.WindowOffsets?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        Point previousOffset = GetWindowOffset(node, options);
        offsets[node] = new Point(ClampCoordinate((long)previousOffset.X + current.X - bounds.X + shift), previousOffset.Y);
        var sizes = options.WindowSizes?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        sizes[node] = new Size(Math.Max(0, (int)Math.Ceiling(width)), bounds.Height);
        var geometry = options.TextButtonCaptionGeometryNodes?.ToHashSet() ?? [];
        geometry.Add(node);
        captions[node] = caption;
        states[node] = (states.GetValueOrDefault(node) ?? new()) with { SelectedRow = -1 };
        return options with { TextButtonCaptions = captions, MenuStates = states,
            WindowOffsets = offsets, WindowSizes = sizes, TextButtonCaptionGeometryNodes = geometry };
    }

    public static PangyaUiRenderOptions SelectTextButton(PangyaUiNode node, int index,
        bool selected = true, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!IsTextButton(node)) throw new ArgumentException("Expected a text button.", nameof(node));
        options ??= new();
        int count = GetTextButtonCaption(node, options).Split('|').TakeWhile(segment => segment.Length > 0).Count();
        if (count == 0) return options;
        int target = Math.Clamp(index, 0, count - 1);
        var states = options.MenuStates?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        PangyaUiMenuState state = states.GetValueOrDefault(node) ?? new();
        if (!selected && state.SelectedRow != target) return options;
        states[node] = state with { SelectedRow = selected ? target : -1 };
        return options with { MenuStates = states };
    }

    public static string? GetSelectedTextButtonText(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!IsTextButton(node)) throw new ArgumentException("Expected a text button.", nameof(node));
        int selected = options?.MenuStates?.GetValueOrDefault(node)?.SelectedRow ?? -1;
        return selected < 0 ? null : GetTextButtonCaption(node, options).Split('|')
            .TakeWhile(segment => segment.Length > 0).ElementAtOrDefault(selected);
    }

    public string? GetTextButtonTextUnderCursor(PangyaUiNode node, Point point, PangyaUiRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(options);
        if (!IsTextButton(node)) throw new ArgumentException("Expected a text button.", nameof(node));
        int index = HitTestTextButtonSegment(node, point, options);
        return index < 0 ? null : GetTextButtonCaption(node, options).Split('|')
            .TakeWhile(segment => segment.Length > 0).ElementAtOrDefault(index);
    }

    public static PangyaUiRenderOptions SetCaptionClippingArea(PangyaUiNode node, Rectangle? area,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var clips = options.CaptionClips?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var enabled = options.CaptionClippingEnabled?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        if (area is Rectangle rectangle) clips[node] = rectangle;
        else clips.Remove(node);
        enabled[node] = area.HasValue;
        return options with { CaptionClips = clips, CaptionClippingEnabled = enabled };
    }

    private static bool IsTextButton(PangyaUiNode node) =>
        node.Type.Equals("TEXTBUTTON", StringComparison.OrdinalIgnoreCase);

    private static string GetTextButtonCaption(PangyaUiNode node, PangyaUiRenderOptions? options) =>
        IsTextButton(node)
            ? options?.TextButtonCaptions?.GetValueOrDefault(node) ?? node.TextButtonCaption
            : node.TextButtonCaption;

    private static string LimitComboText(PangyaUiNode node, string text)
    {
        int limit = int.TryParse(node.Parameters.GetValueOrDefault("charlimit"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int value) ? Math.Max(0, value) : 255;
        return TruncateShiftJis(text, limit);
    }

    private static string TruncateShiftJis(string text, int limit)
    {
        if (limit == 0) return string.Empty;
        if (StaticCaptionEncoding.GetByteCount(text) <= limit) return text;
        int charsUsed = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            int length = rune.Utf16SequenceLength;
            int bytes = StaticCaptionEncoding.GetByteCount(text.AsSpan(charsUsed, length));
            if (bytes > limit) break;
            limit -= bytes;
            charsUsed += length;
        }
        return text[..charsUsed];
    }
    public static bool IsScrollingCaption(PangyaUiNode node, PangyaUiRenderOptions? options = null) =>
        !IsRuntimeHidden(node, options) && GetStaticCaption(node, options).Length > 0 &&
        GetWindowBounds(node, options).Width > 0 && GetWindowBounds(node, options).Height > 0 &&
        string.Equals(node.Parameters.GetValueOrDefault("scrolling"), "true", StringComparison.OrdinalIgnoreCase);

    public float GetCaptionScrollLimit(PangyaUiNode node, IReadOnlySet<string> symbols, PangyaUiRenderOptions? options = null)
    {
        if (!IsScrollingCaption(node, options)) return 0;
        using var bitmap = new Bitmap(1, 1);
        using Graphics graphics = Graphics.FromImage(bitmap);
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        return Math.Max(0, MeasureCaptionLine(graphics, GetStaticCaption(node, options),
            SystemFonts.DefaultFont, format, symbols: null,
            measureText: options?.MeasureCaptionText) - GetWindowBounds(node, options).Width);
    }

    private static void DrawStyledText(Graphics graphics, string text, Brush foreground, Brush outline,
        PointF origin, StringFormat format, int style, Font? font = null,
        Action<Graphics, string, Brush, PointF>? drawText = null)
    {
        font ??= SystemFonts.DefaultFont;
        void DrawOffset(Brush brush, int dx, int dy)
        {
            var point = new PointF(origin.X + dx, origin.Y + dy);
            using SolidBrush? faded = brush is SolidBrush solid && GetGraphicsAlpha(graphics) != 1
                ? new SolidBrush(Color.FromArgb((int)(solid.Color.A * GetGraphicsAlpha(graphics)), solid.Color)) : null;
            Brush effective = faded ?? brush;
            if (drawText is null) graphics.DrawString(text, font, effective, point, format);
            else drawText(graphics, text, effective, point);
        }
        switch (style)
        {
            case 1:
                DrawOffset(foreground, 1, 0);
                break;
            case 2:
                ReadOnlySpan<Point> offsets =
                [
                    new(0, -1), new(-1, 0), new(1, 0), new(0, 1),
                    new(-1, -1), new(1, -1), new(-1, 1), new(1, 1)
                ];
                foreach (Point offset in offsets) DrawOffset(outline, offset.X, offset.Y);
                break;
            case 4:
                DrawOffset(outline, 1, 1);
                break;
        }
        DrawOffset(foreground, 0, 0);
    }

    private static bool IsAreaEntranceFinished(PangyaUiNode node, PangyaUiRenderOptions options)
    {
        if (!IsArea(node) || node.Parameters.GetValueOrDefault("entrance_effect") is not { Length: > 0 } effect)
            return false;
        var (opening, duration, _) = ReadAreaEntrance(effect);
        return duration >= 0 && options.LayoutAnimationTime.TotalSeconds - opening - duration >= 0.2;
    }

    private static (double Opening, double Duration, string Direction) ReadAreaEntrance(string effect)
    {
        double opening = 1;
        double duration = -1;
        string direction = "top";
        bool startsOffscreen = false;
        foreach (string token in effect.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            int separator = token.IndexOf(':');
            if (separator < 0) continue;
            string key = token[..separator].ToLowerInvariant();
            string value = token[(separator + 1)..];
            if (key == "from")
            {
                startsOffscreen = true;
                string candidate = value.ToLowerInvariant();
                if (candidate is "top" or "bottom" or "left" or "right") direction = candidate;
            }
            else if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                         out double seconds) && double.IsFinite(seconds))
            {
                if (key == "openingtime") opening = Math.Max(0, seconds);
                if (key == "duration") duration = seconds;
            }
        }
        return (startsOffscreen ? opening : 0, duration, direction);
    }

    private static Rectangle? ApplyAreaEntrance(PangyaUiNode node, Rectangle bounds, Image image,
        PangyaUiRenderOptions options, ref Rectangle destination)
    {
        if (!IsArea(node) || node.Parameters.GetValueOrDefault("entrance_effect") is not { Length: > 0 } effect)
            return null;

        var (opening, duration, direction) = ReadAreaEntrance(effect);
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

    private static float AnimatedSourceOffset(int velocity, double elapsed, int travel)
    {
        if (travel <= 0 || velocity == 0 || elapsed <= 0) return 0;
        if (velocity < 0)
        {
            double offset = (velocity * elapsed) % travel;
            return (float)(offset < 0 ? offset + travel : offset);
        }

        // Replay Fresh's positive-edge reset at a nominal 60 Hz.
        const double framesPerSecond = 60;
        double frames = elapsed * framesPerSecond;
        double completed = Math.Floor(frames);
        double step = velocity / framesPerSecond;
        double cycle = Math.Max(1, Math.Ceiling(travel / step) - 1);
        double offsetAtFrame = completed > 0 ? ((completed - 1) % cycle + 1) * step : 0;
        double remainder = (frames - completed) * step;
        return (float)(remainder > 0 && offsetAtFrame + remainder >= travel
            ? remainder : offsetAtFrame + remainder);
    }
    private static bool IsContextMenu(PangyaUiNode node) =>
        node.Type.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase);

    public static PangyaUiRenderOptions SetContextMenuList(PangyaUiNode node, string caption,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(caption);
        if (!IsContextMenu(node)) throw new ArgumentException("Expected a context menu.", nameof(node));
        options ??= new();
        var captions = options.ContextMenuCaptions?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        captions[node] = caption;
        var states = options.MenuStates?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        states[node] = (states.GetValueOrDefault(node) ?? new()) with
        {
            SelectedRow = -1, DisabledRows = null, DisabledTexts = null, TextReplacements = null
        };
        var texts = options.ContextMenuItemTexts?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        texts.Remove(node);
        return options with
        {
            ContextMenuCaptions = captions, MenuStates = states, ContextMenuItemTexts = texts,
            HoveredNode = ReferenceEquals(options.HoveredNode, node) ? null : options.HoveredNode
        };
    }

    private static string GetContextMenuCaption(PangyaUiNode node, PangyaUiRenderOptions options) =>
        IsContextMenu(node)
            ? options.ContextMenuCaptions?.GetValueOrDefault(node) ?? node.ContextMenuCaption
            : node.ContextMenuCaption;
    public int HitTestMenuRow(PangyaUiNode node, Point point, PangyaUiRenderOptions options)
    {
        if (IsRuntimeHidden(node, options) || !IsRenderVisible(node, options) || GetContextMenuCaption(node, options).Length == 0) return -1;
        using var bitmap = new Bitmap(1, 1);
        using Graphics metrics = Graphics.FromImage(bitmap);
        RenderContextMenu(metrics, node, GetAbsoluteBounds(node, options).Location,
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
        PangyaUiMenuState? menuState = options.MenuStates?.GetValueOrDefault(node);
        foreach (string entry in GetContextMenuCaption(node, options).Split('|'))
        {
            if (entry.Length == 0) break;
            int split = entry.IndexOf('@');
            string label = split < 0 ? entry : entry[..split];
            hasIcons |= split >= 0;
            Image? icon = split >= 0 && split + 1 < entry.Length ? GetBitmap(entry[(split + 1)..], Symbols(options)) : null;
            bool separator = label.Contains("separator", StringComparison.Ordinal) ||
                label.Contains("seperator", StringComparison.Ordinal);
            if (!separator) y += 4;
            float height = separator ? 10 : (options.CaptionFontHeight ?? font.GetHeight(graphics)) + 3;
            string displayText = !separator &&
                options.ContextMenuItemTexts?.GetValueOrDefault(node)?.TryGetValue(rows.Count,
                    out string? replacementText) == true ? replacementText : label;
            if (menuState?.TextReplacements is { } replacements)
                foreach (var replacement in replacements)
                    if (string.Equals(TruncateShiftJis(displayText, 31), replacement.Key,
                            StringComparison.Ordinal))
                    {
                        displayText = replacement.Value;
                        break;
                    }
            rows.Add((TruncateShiftJis(displayText, 31), icon, separator, y, height));
            y += height;
            float labelWidth = options.MeasureCaptionText?.Invoke(label) ??
                graphics.MeasureString(label, font, PointF.Empty, format).Width;
            width = Math.Max(width, labelWidth + (icon?.Width ?? 0));
        }

        var bounds = new Rectangle(origin, new Size((int)Math.Ceiling(width + 36), (int)Math.Ceiling(y + 10)));
        bool IsDisabled(int index) => menuState?.DisabledRows?.Contains(index) == true ||
            index >= 0 && index < rows.Count && menuState?.DisabledTexts is { } disabledTexts &&
            disabledTexts.Any(text => string.Equals(text, rows[index].Text, StringComparison.Ordinal));
        hoveredRow = -1;
        if (ReferenceEquals(options.HoveredNode, node) &&
            options.Pointer is Point pointer && bounds.Contains(pointer))
            hoveredRow = rows.FindIndex(row => !row.Separator &&
                pointer.Y >= origin.Y + row.Top && pointer.Y < origin.Y + row.Top + row.Height);
        if (IsDisabled(hoveredRow)) hoveredRow = -1;
        IReadOnlySet<string> symbols = Symbols(options);
        PangyaUiNode? frame = _resources.TryResolve(GetRenderResource(node, options.ButtonState), "FRAME", symbols);
        Rectangle renderedBounds = bounds;
        if (frame?.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) == true)
        {
            renderedBounds = DrawFrame(draw ? graphics : null, bounds, frame, symbols, showBlink: false,
                node.Parameters.GetValueOrDefault("bgimg"), isViewFocused: IsWindowViewFocused(node, options),
                contextMenu: true, textOptions: options, frameInstance: node);
        }
        if (!draw) return renderedBounds;
        byte opacity = GetWindowOpacity(node, options, menuState?.Opacity ?? byte.MaxValue, secondary: false);
        Color normalColor = ParseColor(node.Parameters.GetValueOrDefault("fontcolor"), Color.Black, implicitAlpha: false);
        using var brush = new SolidBrush(Color.FromArgb((int)(normalColor.A *
            GetWindowAlphaScale(node, options, secondary: false) *
            (menuState?.Opacity ?? byte.MaxValue) / byte.MaxValue), normalColor));
        using var highlight = new SolidBrush(Color.FromArgb(opacity, 255, 204, 0));
        using var selection = new SolidBrush(Color.FromArgb(menuState?.SelectionAlpha ?? byte.MaxValue,
            255, 204, 0));
        using var disabled = new SolidBrush(Color.FromArgb(opacity, 204, 204, 204));
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
                bool enabled = !IsDisabled(index);
                if (row.Icon is Image icon)
                {
                    float x = origin.X + 7f - (20f - icon.Width) / 2f;
                    float yIcon = top - (row.Height - icon.Height) / 2f - 3f;
                    GraphicsState iconState = graphics.Save();
                    try
                    {
                        graphics.TranslateTransform(x, yIcon);
                        DrawTintedImage(graphics, icon, new Rectangle(Point.Empty, icon.Size),
                            0, icon.Width, icon.Height, enabled ? Color.White : Color.FromArgb(51, Color.White));
                    }
                    finally { graphics.Restore(iconState); }
                }
                Brush rowBrush = !enabled ? disabled : hoveredRow == index ? highlight :
                    menuState?.SelectedRow == index ? selection : brush;
                var labelOrigin = new PointF(origin.X + (hasIcons ? 30f : 10f), top);
                int style = enabled && (hoveredRow == index || menuState?.SelectedRow == index) ? 1 : 0;
                DrawStyledText(graphics, row.Text, rowBrush, brush, labelOrigin, format, style,
                    font, options.DrawCaptionText);
            }
        }
        return renderedBounds;
    }

    private static PangyaUiListPreview? GetList(PangyaUiNode node, PangyaUiRenderOptions options) =>
        node.Type.Equals("LISTBOX", StringComparison.OrdinalIgnoreCase)
            ? options.Lists?.GetValueOrDefault(node) : null;

    public int HitTestListItem(PangyaUiNode node, Point point, PangyaUiRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(options);
        if (IsRuntimeHidden(node, options) || !IsRenderVisible(node, options) ||
            options.DisabledNodes?.Contains(node) == true ||
            GetList(node, options) is not { MouseEventsEnabled: true, ItemCount: > 0 } list)
            return -1;
        Rectangle bounds = GetAbsoluteBounds(node, options);
        if (!bounds.Contains(point) ||
            (list.ItemSize ?? PangyaUiDimensionHelper.ParseSize(node.Parameters.GetValueOrDefault("itemsize")))
                is not Size item || item.Width <= 0 || item.Height <= 0)
            return -1;
        int columns = int.TryParse(node.Parameters.GetValueOrDefault("column"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Math.Max(1, value) : 1;
        bool configuredScrollbar = !int.TryParse(node.Parameters.GetValueOrDefault("show_scrollbar"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int show) || show != 0;
        int scrollbarWidth = (list.ShowScrollbar ?? configuredScrollbar)
            ? GetBitmap("scroll_n_1", Symbols(options))?.Width ?? 0 : 0;
        int fittingColumns = Math.Max(1, (bounds.Width - scrollbarWidth) / item.Width);
        columns = list.ColumnCapacity is int suppliedColumns ? Math.Max(1, suppliedColumns) :
            list.ItemSize.HasValue ? columns : Math.Min(columns, fittingColumns);
        int capacity = bounds.Height / item.Height;
        int totalRows = (list.ItemCount - 1) / columns + 1;
        if (capacity > 0 && totalRows > capacity && (list.ScrollbarAtLeft
                ? point.X < (long)bounds.X + scrollbarWidth
                : point.X >= (long)bounds.X + bounds.Width - scrollbarWidth)) return -1;
        int maximumTopRow = Math.Max(0, totalRows - capacity);
        int topRow = list.FollowBottom ? maximumTopRow : Math.Clamp(list.TopRow, 0, maximumTopRow);
        long first = list.TopItem is int topItem ? Math.Clamp(topItem, 0, list.ItemCount - 1) : (long)topRow * columns;
        int column = (point.X - bounds.X) / item.Width;
        int row = (point.Y - bounds.Y) / item.Height;
        if (column >= columns || row >= Math.Max(1, capacity)) return -1;
        long index = first + (long)row * columns + column;
        return index >= list.ItemCount || list.ExcludeDummyItemsFromHitTesting && list.DummyItems?.Contains((int)index) == true
            ? -1 : (int)index;
    }

    private void DrawList(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        PangyaUiListPreview list, PangyaUiRenderOptions options)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        if (list.DrawBackground is { } drawBackground)
        {
            GraphicsState backgroundState = graphics.Save();
            try { drawBackground(graphics, bounds); }
            finally { graphics.Restore(backgroundState); }
        }

        if ((list.ItemSize ?? PangyaUiDimensionHelper.ParseSize(node.Parameters.GetValueOrDefault("itemsize"))) is not Size item ||
            item.Width <= 0 || item.Height <= 0)
            return;

        int columns = int.TryParse(node.Parameters.GetValueOrDefault("column"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? Math.Max(1, value) : 1;
        IReadOnlySet<string> symbols = Symbols(options);
        bool configuredScrollbar = !int.TryParse(node.Parameters.GetValueOrDefault("show_scrollbar"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int show) || show != 0;
        bool showScrollbar = list.ShowScrollbar ?? configuredScrollbar;
        int scrollbarWidth = showScrollbar
            ? GetBitmap("scroll_n_1", symbols)?.Width ?? 0 : 0;
        int fittingColumns = Math.Max(1, (bounds.Width - scrollbarWidth) / item.Width);
        columns = list.ColumnCapacity is int suppliedColumns
            ? Math.Max(1, suppliedColumns)
            : list.ItemSize.HasValue ? columns : Math.Min(columns, fittingColumns);
        int capacity = bounds.Height / item.Height;
        bool showGuide = options.ScrollbarGuides?.GetValueOrDefault(node) == true;
        if (list.ItemCount <= 0)
        {
            if (showScrollbar)
                DrawScrollBar(graphics, bounds, capacity, 0, 0, symbols, showGuide,
                    atLeft: list.ScrollbarAtLeft, opacity: GetScrollbarOpacity(node, options));
            return;
        }
        int totalRows = (list.ItemCount - 1) / columns + 1;
        int maximumTopRow = Math.Max(0, totalRows - capacity);
        int topRow = list.FollowBottom ? maximumTopRow : Math.Clamp(list.TopRow, 0, maximumTopRow);
        long firstItem = list.TopItem is int topItem
            ? Math.Clamp(topItem, 0, list.ItemCount - 1) : (long)topRow * columns;
        int rows = Math.Max(1, capacity);
        for (int row = 0; row < rows; row++)
        {
            long first = firstItem + (long)row * columns;
            if (first >= list.ItemCount) break;
            for (int column = 0; column < columns && first + column < list.ItemCount; column++)
            {
                var cell = new Rectangle(ClampCoordinate((long)bounds.X + (long)column * item.Width),
                    ClampCoordinate((long)bounds.Y + (long)row * item.Height), item.Width, item.Height);
                GraphicsState itemState = graphics.Save();
                try
                {
                    int index = (int)(first + column);
                    if (list.DrawItemState is { } drawItemState)
                    {
                        bool hovered = list.UnderCursorItems is { } cursorItems ? cursorItems.Contains(index) :
                            list.ItemUnderCursor is int cursorIndex ? cursorIndex == index :
                            list.MouseEventsEnabled && ReferenceEquals(options.HoveredNode, node) &&
                            options.Pointer is Point pointer && bounds.Contains(pointer) && cell.Contains(pointer) &&
                            (capacity <= 0 || totalRows <= capacity ||
                             (list.ScrollbarAtLeft
                                 ? pointer.X >= (long)bounds.X + scrollbarWidth
                                 : pointer.X < (long)bounds.X + bounds.Width - scrollbarWidth));
                        int visibleIndex = (int)(index - firstItem);
                        drawItemState(graphics, new PangyaUiListItemState(index, visibleIndex, cell,
                            hovered, list.SelectedItems?.Contains(index) == true)
                        {
                            IsDummy = list.DummyItems?.Contains(index) == true
                        });
                    }
                    else
                        list.DrawItem(graphics, index, cell);
                }
                finally { graphics.Restore(itemState); }
            }
        }

        if (showScrollbar)
            DrawScrollBar(graphics, bounds, capacity, totalRows, topRow, symbols, showGuide,
                focused: capacity > 0 && totalRows > capacity &&
                    IsWindowViewFocused(node, options) && ReferenceEquals(options.HoveredNode, node),
                atLeft: list.ScrollbarAtLeft, opacity: GetScrollbarOpacity(node, options));
    }

    private static IReadOnlyList<PangyaUiTab>? GetTabs(PangyaUiNode node, PangyaUiRenderOptions options) =>
        node.Type.Equals("TABBUTTON", StringComparison.OrdinalIgnoreCase)
            ? options.Tabs?.GetValueOrDefault(node) ?? GetMakeRoomTabs(node, options) ??
                (options.TabOwnerDraw?.ContainsKey(node) == true ? Array.Empty<PangyaUiTab>() : null)
            : null;

    private static readonly PangyaUiTab[] MakeRoomUserTabs = CreateMakeRoomTabs([2, 3, 4], 50);
    private static readonly PangyaUiTab[] MakeRoomHoleTabs = CreateMakeRoomTabs([3, 6, 9, 18], 37.5);
    private static readonly PangyaUiTab[] MakeRoomTimeTabs = CreateMakeRoomTabs([40, 60, 120, 300], 37.5);

    private static PangyaUiTab[] CreateMakeRoomTabs(int[] values, double width, int selectedIndex = 0) =>
        values.Select((value, index) => new PangyaUiTab("select_button_n", "select_button_o", "select_button_d",
            index == selectedIndex ? PangyaUiButtonState.Selected : PangyaUiButtonState.Normal,
            new Size((int)Math.Ceiling(width), 20))
            { Caption = value.ToString(CultureInfo.InvariantCulture), LayoutWidth = (float)width }).ToArray();

    private static readonly PangyaUiTab[] MakeRoomGameTabs =
    [
        new("select_button_n", "select_button_o", "select_button_d", PangyaUiButtonState.Selected,
            new Size(75, 20)) { Caption = "스트로크" },
        new("select_button_n", "select_button_o", "select_button_d", Size: new Size(75, 20)) { Caption = "매치" }
    ];

    private static readonly PangyaUiTab[] MatchUserTabs = CreateMakeRoomTabs([2, 4], 75, selectedIndex: 1);
    private static readonly PangyaUiTab[] MatchHoleTabs = CreateMakeRoomTabs([6, 9, 18], 50);

    private static bool IsDefaultMakeRoomModeButton(PangyaUiNode node, PangyaUiRenderOptions options) =>
        node.Name == "make_vs" && node.Parent is { } room &&
        string.Equals(room.Name, "makeroom", StringComparison.OrdinalIgnoreCase) &&
        !room.Children.Any(child => child.Name.StartsWith("make_", StringComparison.Ordinal) &&
            options.LatchedButtons?.Contains(child) == true);

    private static IReadOnlyList<PangyaUiTab>? GetMakeRoomTabs(PangyaUiNode node, PangyaUiRenderOptions options) =>
        string.Equals(node.Parent?.Name, "makeroom", StringComparison.OrdinalIgnoreCase) ? node.Name switch
        {
            "gametype" => MakeRoomGameTabs,
            "userlimit" => options.MakeRoomMatchForms?.Contains(node.Parent!) == true ? MatchUserTabs : MakeRoomUserTabs,
            "hole" => options.MakeRoomMatchForms?.Contains(node.Parent!) == true ? MatchHoleTabs : MakeRoomHoleTabs,
            "timelimit" => MakeRoomTimeTabs,
            _ => null
        } : null;

    public int HitTestTab(PangyaUiNode node, Point point, PangyaUiRenderOptions options) =>
        HitTestTab(node, point, GetAbsoluteBounds(node, options).Location, options);

    public int HitTestTab(PangyaUiNode node, Point point, Point renderedOrigin, PangyaUiRenderOptions options)
    {
        if (IsRuntimeHidden(node, options) || !IsRenderVisible(node, options) || !node.IsVisible(Symbols(options)) ||
            options.DisabledNodes?.Contains(node) == true ||
            options.TabMouseEventsEnabled?.GetValueOrDefault(node, true) == false || GetTabs(node, options) is not { } tabs)
            return -1;
        int hit = -1;
        RenderTabs(null, node, renderedOrigin, tabs, Symbols(options), options,
            (index, bounds) => { if (bounds.Contains(point)) hit = index; });
        return hit;
    }

    private Rectangle RenderTabs(Graphics? graphics, PangyaUiNode node, Point origin,
        IReadOnlyList<PangyaUiTab> tabs, IReadOnlySet<string> symbols, PangyaUiRenderOptions options,
        Action<int, Rectangle>? visitTab = null)
    {
        Image? GetImage(string? filename) =>
            string.IsNullOrWhiteSpace(filename) ? null : GetBitmap(filename, symbols);
        Image? separator = GetImage(node.Parameters.GetValueOrDefault("sepImg"));
        float separatorWidth = ReferenceEquals(tabs, GetMakeRoomTabs(node, options)) ? 1 : separator?.Width ?? 0;
        if (options.TabSeparatorWidths?.TryGetValue(node, out float suppliedWidth) == true &&
            float.IsFinite(suppliedWidth))
            separatorWidth = Math.Clamp(suppliedWidth, 0, int.MaxValue);
        Rectangle? result = null;
        var drawOwner = options.TabOwnerDraw?.GetValueOrDefault(node);
        if (drawOwner is not null)
        {
            double width = 0;
            int height = 0;
            for (int index = 0; index < tabs.Count; index++)
            {
                PangyaUiTab tab = tabs[index];
                Size size = tab.Size ?? GetImage(tab.Normal)?.Size ?? Size.Empty;
                if (index == 0) height = Math.Max(0, size.Height);
                if (index > 0) width += separatorWidth;
                width += tab.LayoutWidth is > 0 and <= int.MaxValue ? tab.LayoutWidth.Value : Math.Max(0, size.Width);
            }
            Size controlSize = options.WindowSizes?.TryGetValue(node, out Size suppliedSize) == true
                ? new Size(Math.Max(0, suppliedSize.Width), Math.Max(0, suppliedSize.Height))
                : new Size((int)Math.Clamp(Math.Floor(width + 0.5), 0, int.MaxValue), height);
            var barBounds = new Rectangle(origin, controlSize);
            IncludeBounds(ref result, barBounds);
            DrawOwner(null, barBounds);
        }
        byte alpha = GetWindowOpacity(node, options,
            options.TabOpacity?.GetValueOrDefault(node, byte.MaxValue) ?? byte.MaxValue);
        int? selectedIndex = options.SelectedTabInstances?.TryGetValue((node, origin), out int instanceSelection) == true
            ? instanceSelection : options.SelectedTabIndices?.TryGetValue(node, out int selection) == true ? selection : null;
        if (selectedIndex is null &&
            options.SelectedTabIdentifiers?.TryGetValue(node, out uint identifier) == true)
        {
            for (int index = 0; index < tabs.Count; index++)
                if ((tabs[index].Identifier ?? (uint)index) == identifier)
                {
                    selectedIndex = index;
                    break;
                }
        }
        int? unselectedIndex = null;
        if (options.UnselectedTabIdentifiers?.TryGetValue(node, out uint unselectedIdentifier) == true)
        {
            for (int index = 0; index < tabs.Count; index++)
                if ((tabs[index].Identifier ?? (uint)index) == unselectedIdentifier)
                {
                    unselectedIndex = index;
                    break;
                }
        }
        double x = origin.X;
        for (int index = 0; index < tabs.Count; index++)
        {
            PangyaUiTab tab = tabs[index];
            Image? normal = GetImage(tab.Normal);
            Size size = tab.Size ?? normal?.Size ?? Size.Empty;
            if (index > 0 && separator is not null)
            {
                long separatorLeft = (long)Math.Floor(x + 0.5);
                int separatorPixels = ClampCoordinate((long)Math.Floor(x + separatorWidth + 0.5) - separatorLeft);
                var separatorBounds = new Rectangle(ClampCoordinate(separatorLeft), origin.Y,
                    separatorPixels, separator.Height);
                DrawSlice(separator, separatorBounds);
                IncludeBounds(ref result, separatorBounds);

            }
            if (index > 0) x += separatorWidth;
            if (size.Width <= 0 || size.Height <= 0) continue;
            float layoutWidth = tab.LayoutWidth is > 0 and <= int.MaxValue ? tab.LayoutWidth.Value : size.Width;
            long left = (long)Math.Floor(x + 0.5);
            int pixelWidth = ClampCoordinate((long)Math.Floor(x + layoutWidth + 0.5) - left);
            var bounds = new Rectangle(ClampCoordinate(left), origin.Y, pixelWidth, size.Height);
            PangyaUiButtonState state = selectedIndex is int selected
                ? selected == index ? PangyaUiButtonState.Selected : PangyaUiButtonState.Normal
                : tab.State;
            if (unselectedIndex == index && state == PangyaUiButtonState.Selected)
                state = PangyaUiButtonState.Normal;
            bool hovered = options.HoveredTabIndices?.TryGetValue(node, out int? retainedHover) == true
                ? retainedHover == index : options.HoveredTab is { } tabHit
                ? tabHit == (node, index, bounds)
                : options.TabMouseEventsEnabled?.GetValueOrDefault(node, true) != false &&
                  ReferenceEquals(options.HoveredNode, node) &&
                  options.Pointer is Point pointer && bounds.Contains(pointer);
            if (state == PangyaUiButtonState.Normal && hovered)
                state = PangyaUiButtonState.Hover;
            Image? variant = GetImage(state switch
            {
                PangyaUiButtonState.Hover => tab.Hover,
                PangyaUiButtonState.Selected => tab.Selected,
                _ => null
            });
            string below = variant is null ? "below_normal" :
                state == PangyaUiButtonState.Selected ? "below_selected" : "below_over";
            if (GetImage(node.Parameters.GetValueOrDefault(below)) is Image background)
                DrawSlice(background, bounds);
            if ((variant ?? normal) is Image foreground)
                DrawSlice(foreground, bounds);
            IncludeBounds(ref result, bounds);
            if (graphics is not null && tab.Caption is { Length: > 0 } caption)
            {
                using var font = new Font(SystemFonts.DefaultFont.FontFamily, 11, GraphicsUnit.Pixel);
                using var brush = new SolidBrush(Color.FromArgb(alpha, Color.White));
                using var format = new StringFormat { Alignment = StringAlignment.Center };
                DrawTaggedCaption(graphics, caption, brush, brush,
                    new PointF(bounds.X + bounds.Width / 2f, bounds.Y + 5), format, 0, font,
                    lineHeight: options.TooltipFontHeight, measureText: options.MeasureTooltipText,
                    drawText: options.DrawTooltipText, parseTags: false);
            }
            visitTab?.Invoke(index, bounds);
            if (graphics is not null)
            {
                options.OnTabRendered?.Invoke(node, index, bounds);
                options.OnTabInstanceRendered?.Invoke(node, index, bounds, origin);
            }
            DrawOwner(index, bounds);
            x += layoutWidth;
        }
        return result ?? Rectangle.Empty;

        void DrawSlice(Image image, Rectangle bounds)
        {
            if (graphics is null || alpha == 0 || bounds.Width <= 0 || bounds.Height <= 0) return;
            if (alpha == byte.MaxValue)
                DrawGraphicsImage(graphics, image, bounds);
            else
                DrawTintedImage(graphics, image, bounds, 0, image.Width, image.Height, Color.FromArgb(alpha, Color.White));
        }

        void DrawOwner(int? index, Rectangle bounds)
        {
            if (graphics is null || drawOwner is null) return;
            GraphicsState state = graphics.Save();
            try { drawOwner(graphics, index, bounds); }
            finally { graphics.Restore(state); }
        }
    }

    private static bool IsGauge(PangyaUiNode node) =>
        node.Type.Equals("GAUGEBAR", StringComparison.OrdinalIgnoreCase) || IsExtendedGauge(node);

    private static bool IsExtendedGauge(PangyaUiNode node) =>
        node.Type.Equals("GAUGEBAREX", StringComparison.OrdinalIgnoreCase);

    public static int GetExtendedGaugeStepCount(PangyaUiNode node) =>
        GetExtendedGaugeStepCount(node, null);

    public static int GetExtendedGaugeStepCount(PangyaUiNode node, PangyaUiRenderOptions? options)
    {
        if (options?.GaugeStepCounts?.TryGetValue(node, out int supplied) == true)
            return Math.Max(1, supplied);
        return int.TryParse(node.Parameters.GetValueOrDefault("steps"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int steps) ? Math.Max(1, steps) : 1;
    }

    public int HitTestExtendedGaugeButton(PangyaUiNode node, Point point, IReadOnlySet<string> symbols,
        PangyaUiRenderOptions? options = null)
    {
        if (IsRuntimeHidden(node, options) || !IsExtendedGauge(node) || !IsRenderVisible(node, options) ||
            options?.DisabledNodes?.Contains(node) == true) return 0;
        Rectangle bounds = GetAbsoluteBounds(node, options);
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

    private Rectangle RenderImageGauge(Graphics? graphics, PangyaUiNode node, Point origin,
        IReadOnlySet<string> symbols, PangyaUiRenderOptions options)
    {
        if (GetGaugeImage(node, "normal", symbols) is not Image skin ||
            GetGaugeImage(node, "mask", symbols) is not Image mask)
            return Rectangle.Empty;

        var skinBounds = new Rectangle(origin, skin.Size);
        if (int.TryParse(node.Parameters.GetValueOrDefault("drawstyle"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int drawStyle) && drawStyle == 1)
        {
            if (options.ImageGaugeOwnerDraw?.GetValueOrDefault(node) is not { } drawOwner)
                return Rectangle.Empty;
            if (graphics is not null)
            {
                GraphicsState state = graphics.Save();
                try { drawOwner(graphics, skinBounds); }
                finally { graphics.Restore(state); }
            }
            return skinBounds;
        }

        PangyaUiGaugeRange? range = options.GaugeRanges?.GetValueOrDefault(node);
        double value = options.GaugeTransitions?.GetValueOrDefault(node) is { } transition
            ? GetGaugeTransitionValue(transition, range, skin.Width)
            : options.GaugeValues?.GetValueOrDefault(node) ?? 0;
        float start = (float)(GaugeRatio(value, range, skin.Width) * mask.Width);
        float width = mask.Width - start;
        if (graphics is not null) DrawGraphicsImage(graphics, skin, skinBounds);
        if (width <= 0) return skinBounds;

        if (graphics is not null)
        {
            PointF[] corners =
            [
                new(origin.X + start, origin.Y),
                new(origin.X + (float)mask.Width, origin.Y),
                new(origin.X + start, origin.Y + (float)mask.Height)
            ];
            DrawGraphicsImage(graphics, mask, corners, new RectangleF(start, 0, width, mask.Height));
        }
        int measuredStart = (int)start;
        var maskBounds = new Rectangle(ClampCoordinate((long)origin.X + measuredStart),
            origin.Y, mask.Width - measuredStart, mask.Height);
        Rectangle? measured = skinBounds;
        IncludeBounds(ref measured, maskBounds);
        return measured ?? skinBounds;
    }
    private void DrawGauge(Graphics graphics, PangyaUiNode node, Rectangle bounds, double value,
        IReadOnlySet<string> symbols, PangyaUiRenderOptions options, bool enabled, int expansion, PangyaUiGaugeRange? range,
        PangyaUiGaugeTransition? transition)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        PangyaUiGaugeAppearance? appearance = options.GaugeAppearances?.GetValueOrDefault(node);
        if (transition is not null)
            value = GetGaugeTransitionValue(transition, range, 100);
        using var background = new SolidBrush(ParseColor(node.Parameters.GetValueOrDefault("bgcolor"),
            Color.FromArgb(64, 64, 64), implicitAlpha: false));
        graphics.FillRectangle(background, bounds);
        Rectangle track = bounds;
        bool extended = IsExtendedGauge(node);
        var buttons = new List<(Rectangle Bounds, PangyaUiRenderOptions Options, bool Right)>(2);
        if (extended)
        {
            Color outline = ParseColor(node.Parameters.GetValueOrDefault("bordercolor"), Color.Gray, implicitAlpha: false);
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
                    buttons.Add((buttonBounds, buttonOptions, !left));
                }
                int reserved = Math.Min(track.Width, width + 2);
                if (left) track.X = ClampCoordinate((long)track.X + reserved);
                track.Width -= reserved;
            }
        }
        int border = int.TryParse(node.Parameters.GetValueOrDefault("border_thickness"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int thickness) ? thickness : (extended ? 3 : 2);
        border = appearance?.BorderThickness ?? border;
        border = Math.Clamp(border, 0, Math.Min(track.Width, track.Height) / 2);
        Rectangle interior = Rectangle.Inflate(track, -border, -border);
        float ratio = (float)GaugeRatio(value, range, 100);
        float origin = (float)GaugeRatio(range?.Origin ?? 0, range, 100);
        float startOffset = interior.Width * Math.Min(origin, ratio);
        float widthFilled = interior.Width * Math.Abs(ratio - origin);
        if (interior.Width <= 0 || interior.Height <= 0)
        {
            DrawArrowButtons();
            return;
        }
        Color initialColor = ParseColor(node.Parameters.GetValueOrDefault("color"), Color.White, implicitAlpha: false);
        Color startColor = appearance?.FillColor ?? initialColor;
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
                Color changeColor = (expansion < 0 ? appearance?.ExpansionColor2 : appearance?.ExpansionColor1)
                    ?? ParseColor(node.Parameters.GetValueOrDefault(colorKey),
                        Color.FromArgb(112, 112, 112), implicitAlpha: false);
                using var changeBrush = new SolidBrush(changeColor);
                graphics.FillRectangle(changeBrush, fill.X + mainWidth, fill.Y, extra, fill.Height);
            }
            return;
        }

        Color target = ParseColor(node.Parameters.GetValueOrDefault("color2"), initialColor, implicitAlpha: false);
        double colorValue = transition is null ? value :
            ClampGaugeValue(transition.From, range?.Lower ?? 0, range?.Upper ?? 100);
        double colorRatio = double.IsNaN(colorValue) ? 0 : Math.Clamp(colorValue / 100, 0, 1);
        int Blend(int start, int end) => (int)(start * (1 - colorRatio) + end * colorRatio);
        Color endColor = Color.FromArgb(Blend(startColor.A, target.A), Blend(startColor.R, target.R),
            Blend(startColor.G, target.G), Blend(startColor.B, target.B));
        if (!enabled)
        {
            startColor = DisabledGaugeColor(startColor);
            endColor = DisabledGaugeColor(endColor);
        }
        if (widthFilled > 0)
        {
            using var gradient = new LinearGradientBrush(fill, startColor, endColor, LinearGradientMode.Horizontal);
            graphics.FillRectangle(gradient, fill);
        }

        int steps = GetExtendedGaugeStepCount(node, options);
        if (steps > 1 && steps - 1 <= interior.Width / 2)
        {
            float pitch = (interior.Width - (steps - 1) * 2f) / steps;
            for (int step = 1; step < steps; step++)
            {
                float offset = step * pitch + (step - 1) * 2f;
                graphics.FillRectangle(background, interior.X + offset, interior.Y, 2f, interior.Height);
            }
        }

        DrawArrowButtons();

        void DrawArrowButtons()
        {
            foreach (var button in buttons)
                DrawButton(graphics, node, button.Bounds, button.Options, rightGaugeButton: button.Right);
        }
    }

    private static double GetGaugeTransitionValue(PangyaUiGaugeTransition transition, PangyaUiGaugeRange? range,
        int defaultUpper)
    {
        int lower = range?.Lower ?? 0;
        int upper = range?.Upper ?? defaultUpper;
        if (upper <= lower) return lower;
        double from = ClampGaugeValue(transition.From, lower, upper);
        double to = ClampGaugeValue(transition.To, lower, upper);
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
        return upper > lower ? (ClampGaugeValue(value, lower, upper) - (double)lower) / (upper - (double)lower) : 0;
    }

    private static double ClampGaugeValue(double value, int lower, int upper) =>
        double.IsNaN(value) ? lower : Math.Clamp(value, lower, upper);

    private static Color DisabledGaugeColor(Color color) =>
        Color.FromArgb(80, color.R * 176 >> 8, color.G * 176 >> 8, color.B * 176 >> 8);

    private static bool IsEdit(PangyaUiNode node) =>
        node.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase) || IsCombo(node);

    private static (Color Foreground, Color Outline) GetEditTextColors(PangyaUiNode node,
        PangyaUiRenderOptions options)
    {
        PangyaUiEditTextColors? colors = options.EditTextColors?.GetValueOrDefault(node);
        return (
            colors?.Foreground ?? ParseColor(node.Parameters.GetValueOrDefault("fontcolor"),
                Color.Gray, implicitAlpha: false),
            colors?.Outline ?? ParseColor(node.Parameters.GetValueOrDefault("fontcolor2"),
                Color.Silver, implicitAlpha: false));
    }

    private static bool IsExtendedCombo(PangyaUiNode node) =>
        node.Type.Equals("COMBOCTLEX", StringComparison.OrdinalIgnoreCase);

    private static bool IsImageCombo(PangyaUiNode node) =>
        IsExtendedCombo(node) && int.TryParse(node.Parameters.GetValueOrDefault("liststyle"),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int style) && style == 1;

    private void DrawComboList(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        PangyaUiComboPreview combo, IReadOnlySet<string> symbols, PangyaUiRenderOptions options)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        IReadOnlyDictionary<string, string> parameters = node.Parameters;
        int Read(string name, int fallback) => int.TryParse(parameters.GetValueOrDefault(name),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        bool extended = IsExtendedCombo(node);
        var (listBounds, lineHeight, capacity, topRow, margin) =
            GetComboListGeometry(node, bounds, combo, options.ComboViewportHeight, Symbols(options));
        int y = listBounds.Y;
        Color background = ParseColor(parameters.GetValueOrDefault("bgcolor"), Color.Transparent, implicitAlpha: false);
        var textColors = GetEditTextColors(node, options);
        Color foreground = textColors.Foreground;
        int fontStyle = ReadHexTextStyle(parameters.GetValueOrDefault("font"));
        using var back = new SolidBrush(background);
        using var text = new SolidBrush(foreground);
        using var selectedText = new SolidBrush(background);
        using var outline = new SolidBrush(textColors.Outline);
        Color highlightColor = ParseColor(parameters.GetValueOrDefault("selectcolor"),
            Color.FromArgb(0), implicitAlpha: false);
        if (highlightColor.ToArgb() == 0)
            highlightColor = Color.FromArgb(unchecked((int)0xff000000) | (~background.ToArgb() & 0xffffff));
        using var highlight = new SolidBrush(highlightColor);
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        GraphicsState state = graphics.Save();
        try
        {
            graphics.SetClip(listBounds, CombineMode.Intersect);
            if (!extended)
            {
                graphics.FillRectangle(back, listBounds);
                using var border = new Pen(ParseColor(parameters.GetValueOrDefault("bordercolor"),
                    Color.Transparent, implicitAlpha: false));
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
                        DrawGraphicsImage(graphics, skin, new Rectangle(new Point(bounds.X, ClampCoordinate((long)rowY + margin)), skin.Size));
                    if (hovered && GetGaugeImage(node, "over", symbols) is Image hover)
                        DrawGraphicsImage(graphics, hover, new Rectangle(new Point(bounds.X, ClampCoordinate((long)rowY + margin)), hover.Size));
                    if (IsImageCombo(node))
                    {
                        string? rowImage = combo.TextureImages is { } textures
                            ? index < textures.Count ? textures[index] : null : item.Image;
                        if (rowImage is { Length: > 0 } filename && GetBitmap(filename, symbols) is Image icon)
                            DrawGraphicsImage(graphics, icon, GetComboIconBounds(node,
                                new Point(bounds.X, ClampCoordinate((long)rowY + margin)),
                                GetGaugeImage(node, "normal", symbols)?.Width ?? bounds.Width, icon.Size));
                        continue;
                    }
                }
                bool highlightText = hovered && !extended;
                if (highlightText)
                {
                    int scrollbarWidth = GetBitmap("scroll_n_1", symbols)?.Width ?? 0;
                    graphics.FillRectangle(highlight, bounds.X, rowY + margin / 2,
                        Math.Max(0, bounds.Width - scrollbarWidth), lineHeight);
                }
                int hoverOffset = highlightText ? -2 : 0;
                DrawStyledText(graphics, LimitComboText(node, item.Text), highlightText ? selectedText : text, outline,
                    extended ? new PointF(bounds.X + 0.5f, rowY + 0.5f)
                        : new PointF(bounds.X + (float)Read("leftmargin", 0) + hoverOffset,
                            rowY + margin + hoverOffset),
                    format, hovered && !extended ? 2 : fontStyle, drawText: options.DrawCaptionText);
            }
            DrawScrollBar(graphics, listBounds, Math.Max(1, capacity), combo.Items.Count,
                combo.ScrollThumbRow is float thumbRow && float.IsFinite(thumbRow) ? thumbRow : topRow,
                symbols, showGuide: options.ScrollbarGuides?.GetValueOrDefault(node) == true,
                focused: capacity > 0 && combo.Items.Count > capacity &&
                    IsWindowViewFocused(node, options) && (combo.ScrollThumbRow.HasValue ||
                    (options.Pointer is Point pointer && listBounds.Contains(pointer))),
                opacity: GetScrollbarOpacity(node, options));
        }
        finally { graphics.Restore(state); }
    }

    private static RectangleF GetComboIconBounds(PangyaUiNode node, Point origin, int itemWidth, Size icon)
    {
        int.TryParse(node.Parameters.GetValueOrDefault("leftmargin"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int margin);
        int.TryParse(node.Parameters.GetValueOrDefault("align"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int alignment);
        double offset = alignment switch
        {
            1 => itemWidth * 0.5 + margin,
            2 => (long)itemWidth - margin,
            _ => margin
        };
        return new RectangleF((float)Math.Clamp(origin.X + offset - icon.Width / 2, int.MinValue, int.MaxValue),
            ClampCoordinate((long)origin.Y + 2), icon.Width, icon.Height);
    }

    public int HitTestComboRow(PangyaUiNode node, Point point, PangyaUiRenderOptions options,
        bool includeScrollbar = false)
    {
        if (IsRuntimeHidden(node, options) || !IsCombo(node) || !IsRenderVisible(node, options) ||
            options.Combos?.GetValueOrDefault(node) is not { IsOpen: true } combo ||
            combo.Items.Count == 0)
            return -1;
        IReadOnlySet<string> symbols = Symbols(options);
        Rectangle edit = GetEditBounds(node, GetAbsoluteBounds(node, options), symbols, options);
        if (edit.Width <= 0 || edit.Height <= 0) return -1;
        var (bounds, lineHeight, capacity, topRow, _) =
            GetComboListGeometry(node, edit, combo, options.ComboViewportHeight, symbols);
        if (!bounds.Contains(point)) return -1;
        if (!includeScrollbar && TryGetComboScrollbarPage(node, point, options, out _, out _, out _))
            return -1;
        long row = ((long)point.Y - bounds.Y) / lineHeight;
        return row < capacity ? topRow + (int)row : -1;
    }

    public PangyaUiRenderOptions InitializeComboOpeningSide(PangyaUiNode node, int viewportHeight,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentOutOfRangeException.ThrowIfNegative(viewportHeight);
        if (!IsCombo(node)) throw new ArgumentException("Expected a combo control.", nameof(node));
        options ??= new();
        var combos = options.Combos?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        PangyaUiComboPreview combo = combos.GetValueOrDefault(node) ?? new([]);
        combos[node] = combo with
        {
            AutomaticOpensAbove = viewportHeight > 0 && GetAbsoluteBounds(node, options).Y > viewportHeight * 0.5
        };
        return options with { Combos = combos };
    }

    public Rectangle GetComboDropdownBounds(PangyaUiNode node, PangyaUiRenderOptions options)
    {
        if (IsRuntimeHidden(node, options) || !IsCombo(node) || !IsRenderVisible(node, options) ||
            options.Combos?.GetValueOrDefault(node) is not { IsOpen: true } combo)
            return Rectangle.Empty;
        IReadOnlySet<string> symbols = Symbols(options);
        Rectangle edit = GetEditBounds(node, GetAbsoluteBounds(node, options), symbols, options);
        return edit.Width > 0 && edit.Height > 0
            ? GetComboListGeometry(node, edit, combo, options.ComboViewportHeight, symbols).Bounds
            : Rectangle.Empty;
    }
    public bool TryGetComboScrollbarPage(PangyaUiNode node, Point point, PangyaUiRenderOptions options,
        out int rowDelta, out Rectangle thumbBounds, out Rectangle trackBounds)
    {
        rowDelta = 0;
        thumbBounds = trackBounds = Rectangle.Empty;
        if (IsRuntimeHidden(node, options) || !IsCombo(node) || !IsRenderVisible(node, options) ||
            options.Combos?.GetValueOrDefault(node) is not { IsOpen: true } combo) return false;
        IReadOnlySet<string> symbols = Symbols(options);
        Rectangle edit = GetEditBounds(node, GetAbsoluteBounds(node, options), symbols, options);
        var (bounds, _, capacity, topRow, _) =
            GetComboListGeometry(node, edit, combo, options.ComboViewportHeight, symbols);
        Rectangle thumb = DrawScrollBar(null, bounds, capacity, combo.Items.Count, topRow, symbols, showGuide: false);
        if (thumb.IsEmpty || !bounds.Contains(point) || point.X < (long)bounds.X + bounds.Width - thumb.Width) return false;
        thumbBounds = thumb;
        trackBounds = bounds;
        rowDelta = point.Y < thumb.Top ? -capacity / 2 : point.Y > thumb.Bottom ? capacity / 2 : 0;
        return true;
    }
    public bool HitTestComboUnfold(PangyaUiNode node, Point point, IReadOnlySet<string> symbols) =>
        HitTestComboUnfold(node, point, symbols, null);

    public bool HitTestComboUnfold(PangyaUiNode node, Point point, IReadOnlySet<string> symbols,
        PangyaUiRenderOptions? options)
    {
        if (IsRuntimeHidden(node, options) || !IsCombo(node) || !IsRenderVisible(node, options) ||
            options?.DisabledNodes?.Contains(node) == true ||
            options?.ImageButtonStates?.GetValueOrDefault(node)?.Enabled == false ||
            !node.Parameters.ContainsKey("normal"))
            return false;
        Rectangle bounds = GetAbsoluteBounds(node, options);
        return GetComboButtonBounds(node, bounds, GetEditBounds(node, bounds, symbols, options), symbols)
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

    private (Rectangle Bounds, int LineHeight, int Capacity, int TopRow, int Margin)
        GetComboListGeometry(PangyaUiNode node, Rectangle bounds, PangyaUiComboPreview combo, int viewportHeight,
            IReadOnlySet<string> symbols)
    {
        int Read(string name, int fallback) => int.TryParse(node.Parameters.GetValueOrDefault(name),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        bool extended = IsExtendedCombo(node);
        int margin = Math.Clamp(Read("topmargin", 0), 0, 4096);
        Image? rowSkin = extended ? GetGaugeImage(node, "normal", symbols) : null;
        int lineHeight = (int)Math.Clamp(rowSkin is not null
            ? rowSkin.Height + (long)margin : Read("lineheight", 16),
            1, 8192);
        int capacity = GetComboRowCapacity(node, combo);
        int topRow = Math.Clamp(combo.TopRow, 0, combo.Items.Count - capacity);
        int viewportRows = rowSkin is not null ? GetComboRowLimit(node, combo) :
            Math.Max(1, Math.Min(combo.Items.Count, GetComboRowLimit(node, combo)));
        int height = viewportRows * lineHeight + (extended ? 0 : margin * 2);
        bool lowerHalf = combo.AutomaticOpensAbove ??
            (viewportHeight > 0 && GetAbsoluteBounds(node, null).Y > viewportHeight * 0.5);
        bool above = combo.Direction == PangyaUiComboDirection.Above ||
            (combo.Direction == PangyaUiComboDirection.Auto && lowerHalf);
        long y = extended
            ? (long)bounds.Y + (rowSkin is not null ? rowSkin.Height + margin : lowerHalf ? -5 : 5)
            : above ? (long)bounds.Y - height - 2 : (long)bounds.Y + bounds.Height + 2;
        int width = rowSkin?.Width ?? bounds.Width;
        return (new Rectangle(bounds.X, ClampCoordinate(y), width, height), lineHeight, capacity, topRow, margin);
    }

    public PangyaUiRenderOptions AddComboTexture(PangyaUiNode node, string filename,
        PangyaUiRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(filename);
        ArgumentNullException.ThrowIfNull(options);
        if (!IsImageCombo(node)) return options;
        PangyaUiComboPreview combo = options.Combos?.GetValueOrDefault(node) ?? new([]);
        int count = checked((combo.StoredTextRowCount ?? combo.Items.Count) + 1);
        string? image = GetBitmap(filename, Symbols(options)) is not null ? filename : null;
        var textures = combo.TextureImages?.ToList() ?? combo.Items.Select(item => item.Image).OfType<string>()
            .Where(existing => existing.Length > 0 && GetBitmap(existing, Symbols(options)) is not null).ToList();
        if (image is not null) textures.Add(image);
        var items = combo.Items.ToList();
        items.Add(new(count.ToString(CultureInfo.InvariantCulture), image));
        var combos = options.Combos?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        combos[node] = combo with { Items = items.ToArray(), StoredTextRowCount = count,
            TextureImages = textures.ToArray() };
        return options with { Combos = combos };
    }

    public PangyaUiRenderOptions SelectComboImage(PangyaUiNode node, int index,
        PangyaUiRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(options);
        if (!IsImageCombo(node)) throw new ArgumentException("The node must be an image combo.", nameof(node));
        PangyaUiComboPreview combo = options.Combos?.GetValueOrDefault(node) ?? new([]);
        string? selected = null;
        int textureIndex = 0;
        foreach (string filename in combo.TextureImages ?? combo.Items.Select(item => item.Image).OfType<string>().ToArray())
        {
            if (filename is not { Length: > 0 } || GetBitmap(filename, Symbols(options)) is null)
                continue;
            if (textureIndex++ == index) { selected = filename; break; }
        }
        var combos = options.Combos?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        combos[node] = combo with { ImageSelection = new(selected,
            selected is not null ? unchecked((byte)index) : (byte)0) };
        return options with { Combos = combos };
    }

    public static PangyaUiRenderOptions ClearComboImageList(PangyaUiNode node,
        PangyaUiRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(options);
        if (!IsImageCombo(node)) throw new ArgumentException("The node must be an image combo.", nameof(node));
        PangyaUiComboPreview? combo = options.Combos?.GetValueOrDefault(node);
        if (combo is null) return options;
        var selected = combo.ImageSelection ?? new PangyaUiComboImageSelection(
            (uint)combo.SelectedIndex < (uint)combo.Items.Count ? combo.Items[combo.SelectedIndex].Image : null,
            combo.SelectedIndex >= 0 ? unchecked((byte)combo.SelectedIndex) : (byte)0);
        var combos = options.Combos!.ToDictionary(pair => pair.Key, pair => pair.Value);
        combos[node] = combo with { Items = Array.Empty<PangyaUiComboItem>(), ImageSelection = selected,
            StoredTextRowCount = combo.StoredTextRowCount ?? combo.Items.Count,
            TextureImages = Array.Empty<string>(),
            TopRow = 0, ScrollThumbRow = null };
        return options with { Combos = combos };
    }

    public static int GetComboMaximumTopRow(PangyaUiNode node, PangyaUiComboPreview combo) =>
        combo.Items.Count - GetComboRowCapacity(node, combo);

    private static int GetComboRowCapacity(PangyaUiNode node, PangyaUiComboPreview combo)
    {
        int rows = Math.Min(combo.Items.Count, GetComboRowLimit(node, combo));
        if (IsExtendedCombo(node)) return rows;
        int margin = int.TryParse(node.Parameters.GetValueOrDefault("topmargin"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int topMargin) ? Math.Clamp(topMargin, 0, 4096) : 0;
        int lineHeight = int.TryParse(node.Parameters.GetValueOrDefault("lineheight"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int height) ? Math.Clamp(height, 1, 8192) : 16;
        return Math.Min(combo.Items.Count, Math.Max(1, rows) + margin * 2 / lineHeight);
    }

    private static int GetComboRowLimit(PangyaUiNode node, PangyaUiComboPreview combo)
    {
        int configured = int.TryParse(node.Parameters.GetValueOrDefault("maxlistnum"), NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int value) ? Math.Clamp(value, 1, 256) : 10;
        return !IsExtendedCombo(node) && combo.IncreasedMaximumRows is int requested
            ? Math.Clamp(requested, configured, 256) : configured;
    }
    public static bool IsMultilineEdit(PangyaUiNode node) =>
        node.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase) &&
        (node.Parameters.GetValueOrDefault("multiline") is not string value ||
         (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int flag) && flag != 0));
    private static bool IsCombo(PangyaUiNode node) =>
        node.Type.Equals("COMBOBOX", StringComparison.OrdinalIgnoreCase) || IsExtendedCombo(node);

    private Rectangle GetEditBounds(PangyaUiNode node, Rectangle bounds, IReadOnlySet<string> symbols,
        PangyaUiRenderOptions? options) =>
        options?.WindowSizes?.ContainsKey(node) != true &&
        IsExtendedCombo(node) && GetGaugeImage(node, "normal", symbols) is Image skin
            ? new Rectangle(bounds.Location, skin.Size) : bounds;

    private static string MaskEditPassword(string text)
    {
        var masked = new StringBuilder(text.Length);
        int start = 0;
        foreach (System.Text.RegularExpressions.Match tag in EditCaptionTags.Matches(text))
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
        byte opacity = GetScrollbarOpacity(node, options,
            options.EditOpacity?.GetValueOrDefault(node, byte.MaxValue) ?? byte.MaxValue);
        double backgroundAlpha = GetWindowAlphaScale(node, options) *
            (options.EditOpacity?.GetValueOrDefault(node, byte.MaxValue) ?? byte.MaxValue) / byte.MaxValue;
        background = Color.FromArgb((int)(background.A * backgroundAlpha), background);
        Color borderTint = Color.FromArgb((int)(border.A * backgroundAlpha), border);
        if (background.A > 0)
        {
            using var brush = new SolidBrush(background);
            graphics.FillRectangle(brush, bounds);
        }
        if (border.A > 0)
        {
            using var pen = new Pen(borderTint);
            graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        }
        bool initializedReadOnlyStyle = parameters.GetValueOrDefault("readonly") == "true";
        bool readOnlyStyle = options.EditReadOnlyStyles?.GetValueOrDefault(node,
            initializedReadOnlyStyle) ?? initializedReadOnlyStyle;
        bool keyEvents = options.EditKeyEvents?.TryGetValue(node, out bool suppliedKeyEvents) == true
            ? suppliedKeyEvents
            : !initializedReadOnlyStyle &&
                parameters.GetValueOrDefault("focusoff") != "true";
        if (caret is not null && (!enabled || !keyEvents))
            caret = null;
        bool showGuide = options.ScrollbarGuides?.GetValueOrDefault(node) == true;
        if (lines is null || lines.Count == 0)
        {
            if (caret is null && !showGuide) return;
            lines = [string.Empty];
        }

        int ReadInt(string name, int fallback) =>
            int.TryParse(parameters.GetValueOrDefault(name), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int value) ? value : fallback;

        int lineHeight = ReadInt("lineheight", 16);
        if (lineHeight <= 0) return;
        int leftMargin = ReadInt("leftmargin", 0);
        int topMargin = ReadInt("topmargin", 0);
        bool multiline = IsMultilineEdit(node);
        int count = multiline ? lines.Count : 1;
        int vertical = ReadInt("valign", string.Equals(parameters.GetValueOrDefault("vcenteralign"),
            "true", StringComparison.OrdinalIgnoreCase) ? 1 : 0);
        int capacity = GetEditRowCapacity(bounds.Height, topMargin, lineHeight);
        int maximum = Math.Max(0, count - Math.Max(1, capacity));
        int firstLine = options.EditTopRows?.TryGetValue(node, out int requested) == true
            ? Math.Clamp(requested, 0, maximum)
            : caret is null ? maximum : Math.Clamp(caret.Line - Math.Max(1, capacity) + 1, 0, maximum);
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
        var textColors = GetEditTextColors(node, options);
        Color foreground = textColors.Foreground;
        using var text = new SolidBrush(enabled ? foreground : Color.FromArgb(foreground.A * 2 / 5, foreground));
        using var outline = new SolidBrush(enabled
            ? textColors.Outline : Color.White);
        int fontStyle = ReadHexTextStyle(parameters.GetValueOrDefault("font"));
        float left = ClampCoordinate((long)bounds.X + leftMargin);
        bool symmetricMargins = string.Equals(parameters.GetValueOrDefault("symmetry", "true"), "true",
            StringComparison.OrdinalIgnoreCase);
        int scrollbarWidth = multiline ? GetBitmap("scroll_n_1", Symbols(options))?.Width ?? 0 : 0;
        float available = (long)bounds.Width - leftMargin * (symmetricMargins ? 2L : 1L) - scrollbarWidth;
        float anchor = left + GetEditAlignmentOffset(format.Alignment, available);
        bool password = string.Equals(parameters.GetValueOrDefault("password"), "true",
            StringComparison.OrdinalIgnoreCase);
        IReadOnlySet<string>? emoticonSymbols = !password && readOnlyStyle
            ? Symbols(options)
            : null;
        bool measureEmoticons = options.EditEmoticonStyles?.GetValueOrDefault(node, true) ?? true;
        Rectangle content = border.A > 0
            ? new Rectangle(ClampCoordinate((long)bounds.X + 1), ClampCoordinate((long)bounds.Y + 1),
                bounds.Width - 1, bounds.Height - 1)
            : bounds;
        if (IsPrivacyHidden(node, options) || content.Width <= 0 || content.Height <= 0)
        {
            DrawScrollbar();
            return;
        }
        GraphicsState state = graphics.Save();
        try
        {
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
                string visibleLine = EditCaptionTags.Replace(line, "");
                if (readOnlyStyle)
                {
                    float lineWidth = MeasureCaptionLine(graphics,
                        TrimEditAlignmentSpace(EditCaptionTags.Replace(originalLine, "")),
                        font, measure, measureEmoticons ? emoticonSymbols : null,
                        measureText: options.MeasureCaptionText);
                    caretX = left + GetEditAlignmentOffset(format.Alignment, available - lineWidth);
                }
                int visibleColumn = Math.Clamp(caret.Column, 0, originalLine.Length);
                string untaggedLine = EditCaptionTags.Replace(originalLine, "");
                foreach (System.Text.RegularExpressions.Match tag in EditCaptionTags.Matches(originalLine))
                    visibleColumn -= Math.Clamp(caret.Column - tag.Index, 0, tag.Length);
                if (password)
                    visibleColumn = untaggedLine[..Math.Clamp(visibleColumn, 0, untaggedLine.Length)]
                        .EnumerateRunes().Count();
                visibleColumn = Math.Clamp(visibleColumn, 0, visibleLine.Length);
                float prefixWidth = MeasureCaptionLine(graphics, visibleLine[..visibleColumn],
                    font, measure, measureEmoticons ? emoticonSymbols : null,
                    measureText: options.MeasureCaptionText);
                caretX += prefixWidth;
                float viewportWidth = (long)bounds.Width - leftMargin * 2L;
                scroll = readOnlyStyle ? 0 : MathF.Truncate(Math.Max(0, prefixWidth - viewportWidth));
            }
            for (int index = firstLine; index < count && y + lineHeight <= bottom; index++, y += lineHeight)
            {
                if (y + lineHeight <= bounds.Y) continue;
                string originalLine = lines[index];
                string line = password ? MaskEditPassword(originalLine) : originalLine.Replace('\t', ' ');
                int lineStyle = options.EditLineStyles?.GetValueOrDefault(node) is { } styles &&
                    index < styles.Count ? styles[index] : fontStyle;
                TimeSpan? animationElapsed = options.EditEmoticonAnimationTime ?? options.EmoticonAnimationTime;
                if (emoticonSymbols is not null &&
                    options.EditLineAnimationStartTicks?.GetValueOrDefault(node) is { } starts && index < starts.Count)
                    animationElapsed = TimeSpan.FromMilliseconds(unchecked((uint)(options.CurrentTick - starts[index])));
                var formatting = DrawTaggedCaption(graphics, line, text, outline,
                    new PointF(left - (password ? 0 : scroll), ClampCoordinate(y)), format, lineStyle,
                    emoticonSymbols: emoticonSymbols,
                    animationElapsed: animationElapsed,
                    useEditAnimationMarker: emoticonSymbols is not null,
                    forceOpaqueText: emoticonSymbols is not null,
                    editTags: true, defaultTagFill: foreground,
                    lineHeight: options.CaptionFontHeight,
                    measureText: options.MeasureCaptionText,
                    drawText: options.DrawCaptionText,
                    editAvailableWidth: available,
                    splitEditWords: !password,
                    justifyEditLine: !password && options.JustifiedEditLines?.GetValueOrDefault(node)?.Contains(index) == true,
                    editAlignmentText: readOnlyStyle
                        ? TrimEditAlignmentSpace(EditCaptionTags.Replace(originalLine, "")) : string.Empty,
                    defaultTagEdge: textColors.Outline,
                    measureEmoticons: measureEmoticons);
                text.Color = formatting.Fill;
                outline.Color = formatting.Edge;
                if (caret?.Line == index)
                {
                    double phase = Math.Max(0, caret.Elapsed.TotalSeconds) % 1;
                    if (phase > 0 && phase < 0.8)
                    {
                        float x = caretX - scroll;
                        float caretHeight = options.CaptionFontHeight ?? font.GetHeight(graphics);
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
        DrawScrollbar();

        void DrawScrollbar()
        {
            if (!IsCombo(node))
                DrawScrollBar(graphics, bounds, capacity, count, firstLine, Symbols(options), showGuide,
                    focused: capacity > 0 && count > capacity &&
                        IsWindowViewFocused(node, options) && ReferenceEquals(options.HoveredNode, node), opacity: opacity);
        }
    }

    private static int GetEditRowCapacity(int height, int margin, int lineHeight) =>
        lineHeight <= 0 ? 0 : (int)Math.Clamp(((long)height - margin) / lineHeight, 0, int.MaxValue);

    public static int GetEditMaximumTopRow(PangyaUiNode node, int lineCount) =>
        GetEditMaximumTopRow(node, lineCount, null);

    public static int GetEditMaximumTopRow(PangyaUiNode node, int lineCount,
        PangyaUiRenderOptions? options)
    {
        if (!node.Type.Equals("EDIT", StringComparison.OrdinalIgnoreCase) ||
            !IsMultilineEdit(node)) return 0;
        int Read(string name, int fallback) => int.TryParse(node.Parameters.GetValueOrDefault(name),
            NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        return Math.Max(0, lineCount - Math.Max(1,
            GetEditRowCapacity(GetWindowBounds(node, options).Height, Read("topmargin", 0), Read("lineheight", 16))));
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
        Rectangle bounds = GetAbsoluteBounds(node, options);
        if (IsRuntimeHidden(node, options) || !IsRenderVisible(node, options) || point.Y < bounds.Top || (long)point.Y >= (long)bounds.Y + bounds.Height ||
            GetTextButtonCaption(node, options).Length == 0)
            return -1;
        using var bitmap = new Bitmap(1, 1);
        using Graphics metrics = Graphics.FromImage(bitmap);
        return DrawTextButton(metrics, node, bounds, GetTextButtonCaption(node, options),
            options with { HoveredNode = node, Pointer = point }, out _, draw: false);
    }

    private static int DrawTextButton(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        string caption, PangyaUiRenderOptions options, out Rectangle measuredBounds, bool draw = true)
    {
        Font font = SystemFonts.DefaultFont;
        using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
        format.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
        string[] segments = caption.Split('|');
        float x = bounds.X;
        if (options.TextButtonCaptionGeometryNodes?.Contains(node) != true &&
            (!node.Parameters.TryGetValue("center", out string? center) ||
            center.Equals("true", StringComparison.OrdinalIgnoreCase)))
            x += (int)((bounds.Width - Measure(caption) + 1) / 2);

        float startX = x;
        measuredBounds = Rectangle.Empty;
        using var normal = new SolidBrush(ParseColor(node.Parameters.GetValueOrDefault("color"), Color.White, implicitAlpha: false));
        using var highlight = new SolidBrush(ParseColor(
            node.Parameters.GetValueOrDefault("select_color"), Color.FromArgb(255, 204, 0), implicitAlpha: false));
        using var outline = new SolidBrush(ParseColor(node.Parameters.GetValueOrDefault("color2"), Color.Black, implicitAlpha: false));
        using var selectedOutline = new SolidBrush(ParseColor(
            node.Parameters.GetValueOrDefault("select_color2"), Color.Black, implicitAlpha: false));
        PangyaUiMenuState? selection = options.MenuStates?.GetValueOrDefault(node);
        byte alpha = selection?.SelectionAlpha ?? byte.MaxValue;
        using var pulsedHighlight = new SolidBrush(Color.FromArgb(highlight.Color.A * alpha / 255, highlight.Color));
        using var pulsedOutline = new SolidBrush(Color.FromArgb(selectedOutline.Color.A * alpha / 255,
            selectedOutline.Color));
        double opacity = GetWindowAlphaScale(node, options, secondary: false) *
            (selection?.Opacity ?? byte.MaxValue);
        normal.Color = ApplyOpacity(normal.Color);
        outline.Color = ApplyOpacity(outline.Color);
        highlight.Color = ApplyOpacity(highlight.Color);
        selectedOutline.Color = ApplyOpacity(selectedOutline.Color);
        int style = ReadHexTextStyle(node.Parameters.GetValueOrDefault("style"), 1);
        int selectedStyle = ReadHexTextStyle(node.Parameters.GetValueOrDefault("sel_style"), 1);
        float separatorWidth = Measure("|");
        for (int index = 0; index < segments.Length; index++)
        {
            string segment = segments[index];
            if (segment.Length == 0) break;
            float segmentWidth = Measure(segment);
            bool hasSeparator = index + 1 < segments.Length && segments[index + 1].Length > 0;
            bool hovered = ReferenceEquals(options.HoveredNode, node) && options.Pointer is Point pointer &&
                pointer.X >= x && pointer.X < x + segmentWidth + (hasSeparator ? separatorWidth : 0);
            if (!draw && hovered) return index;
            bool forced = index == 0 && options.ButtonState != PangyaUiButtonState.Normal &&
                !ReferenceEquals(options.HoveredNode, node);
            bool selected = selection?.SelectedRow == index;
            if (draw)
                DrawStyledText(graphics, segment, hovered || forced ? highlight : selected ? pulsedHighlight : normal,
                    hovered || forced ? selectedOutline : selected ? pulsedOutline : outline,
                    new PointF(x, bounds.Y), format, hovered || forced || selected ? selectedStyle : style, font, options.DrawCaptionText);
            x += segmentWidth;
            if (hasSeparator)
            {
                if (draw) DrawStyledText(graphics, "|", normal, outline, new PointF(x, bounds.Y), format, style, font, options.DrawCaptionText);
                x += separatorWidth;
            }
        }
        measuredBounds = Rectangle.Ceiling(new RectangleF(startX, bounds.Y, x - startX, bounds.Height));
        return -1;

        Color ApplyOpacity(Color color) => Color.FromArgb((int)(color.A * opacity / byte.MaxValue), color);

        float Measure(string text) => options.MeasureCaptionText?.Invoke(text) ??
            graphics.MeasureString(text, font, PointF.Empty, format).Width;

    }

    public static bool HasButtonSkin(PangyaUiNode node) =>
        node.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase) &&
        node.Parameters.ContainsKey("normal");

    public static bool HasButtonSkin(PangyaUiNode node, PangyaUiRenderOptions options) =>
        HasButtonSkin(node) || (node.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase) &&
            (options.ImageButtonStates?.ContainsKey(node) == true ||
             options.WindowSizes?.ContainsKey(node) == true));

    private Rectangle GetButtonBounds(PangyaUiNode node, Rectangle bounds, IReadOnlySet<string> symbols,
        PangyaUiRenderOptions? options = null)
    {
        if (options?.WindowSizes?.TryGetValue(node, out Size suppliedSize) == true)
            return new Rectangle(bounds.Location,
                new Size(Math.Max(0, suppliedSize.Width), Math.Max(0, suppliedSize.Height)));
        Image? image = GetReplacementButtonImage(options?.ImageButtonStates?.GetValueOrDefault(node), symbols)
            ?? GetRuntimeButtonImage(options?.ImageButtonStates?.GetValueOrDefault(node), "normal", symbols)
            ?? (node.Parameters.TryGetValue("normal", out string? normal) ? GetBitmap(normal, symbols) : null);
        return image is null ? Rectangle.Empty : new Rectangle(bounds.Location, image.Size);
    }

    public static int GetButtonPushStyle(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        return options?.ImageButtonStates?.GetValueOrDefault(node)?.PushStyle ??
            (int.TryParse(node.Parameters.GetValueOrDefault("pushstyle"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int style) ? style : 1);
    }

    public static PangyaUiRenderOptions InitializeExternalButton(PangyaUiNode node, Image normal,
        Image? hover, Image? selected, int style, int pushStyle, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(normal);
        options ??= new();
        ValidateButtonImageArguments(node, "normal", options);
        var states = options.ImageButtonStates?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var state = states.GetValueOrDefault(node) ?? new(Enabled: options.DisabledNodes?.Contains(node) != true);
        var bitmaps = state.BitmapImages?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        bitmaps["normal"] = normal;
        bitmaps["over"] = hover;
        bitmaps["selected"] = selected;
        states[node] = state with { BitmapImages = bitmaps, Style = style, PushStyle = pushStyle,
            ReplacementImage = null, ReplacementMode = null };
        var sizes = options.WindowSizes?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        sizes[node] = normal.Size;
        return options with { ImageButtonStates = states, WindowSizes = sizes };
    }

    public bool TrySetButtonImage(PangyaUiNode node, string? filename, string mode,
        PangyaUiRenderOptions options, out PangyaUiRenderOptions updatedOptions)
    {
        ValidateButtonImageArguments(node, mode, options);
        updatedOptions = options;
        if (filename is null || GetBitmap(filename, Symbols(options)) is not Image image) return false;
        var states = options.ImageButtonStates?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        PangyaUiImageButtonState state = states.GetValueOrDefault(node) ??
            new(Enabled: options.DisabledNodes?.Contains(node) != true);
        var slots = state.Images?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var bitmaps = state.BitmapImages?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        bitmaps.Remove(mode);
        slots[mode] = filename;
        states[node] = state with { Images = slots, BitmapImages = bitmaps,
            ReplacementMode = mode, ReplacementImage = filename };
        var sizes = options.WindowSizes?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        sizes[node] = image.Size;
        updatedOptions = options with { ImageButtonStates = states, WindowSizes = sizes };
        return true;
    }

    public bool TrySetButtonBelowImage(PangyaUiNode node, string? filename, string mode,
        PangyaUiRenderOptions options, out PangyaUiRenderOptions updatedOptions)
    {
        ValidateButtonImageArguments(node, mode, options);
        updatedOptions = options;
        if (filename is not null && GetBitmap(filename, Symbols(options)) is null) return false;
        var states = options.ImageButtonStates?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        PangyaUiImageButtonState state = states.GetValueOrDefault(node) ??
            new(Enabled: options.DisabledNodes?.Contains(node) != true);
        var slots = state.BelowImages?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        slots[mode] = filename;
        states[node] = state with { BelowImages = slots };
        updatedOptions = options with { ImageButtonStates = states };
        return true;
    }

    private static void ValidateButtonImageArguments(PangyaUiNode node, string mode, PangyaUiRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(options);
        if (!node.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The node must be a button.", nameof(node));
        if (mode is not ("normal" or "over" or "selected" or "blink"))
            throw new ArgumentOutOfRangeException(nameof(mode));
    }

    private Image? GetReplacementButtonImage(PangyaUiImageButtonState? state, IReadOnlySet<string> symbols)
    {
        if (state is not { ReplacementMode: "normal" or "over" or "selected" or "blink" })
            return null;
        Image? replacement = state.ReplacementImage is { Length: > 0 } filename
            ? GetBitmap(filename, symbols) : null;
        return replacement ?? GetRuntimeButtonImage(state, state.ReplacementMode, symbols);
    }

    private Image? GetRuntimeButtonImage(PangyaUiImageButtonState? state, string mode,
        IReadOnlySet<string> symbols)
    {
        if (state?.BitmapImages?.TryGetValue(mode, out Image? bitmap) == true) return bitmap;
        return state?.Images?.TryGetValue(mode, out string? filename) == true &&
            !string.IsNullOrWhiteSpace(filename) ? GetBitmap(filename, symbols) : null;
    }
    private void DrawButton(Graphics graphics, PangyaUiNode node, Rectangle bounds,
        PangyaUiRenderOptions options, bool rightGaugeButton = false)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        PangyaUiImageButtonState? runtime = options.ImageButtonStates?.GetValueOrDefault(node);
        bool gaugeArrow = IsExtendedGauge(node);
        bool enabled = (runtime?.Enabled ?? options.DisabledNodes?.Contains(node) != true) &&
            (!gaugeArrow || options.DisabledNodes?.Contains(node) != true);
        PangyaUiButtonState state = runtime?.Status ?? options.ButtonState;
        if (!enabled && runtime?.Status is null && state == PangyaUiButtonState.Hover)
            state = PangyaUiButtonState.Normal;
        node.Parameters.TryGetValue("style", out string? value);
        int style = int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedStyle)
            ? parsedStyle : 1;
        style = runtime?.Style ?? style;
        string mode = style == 2 ? state switch
        {
            PangyaUiButtonState.Hover => "over",
            PangyaUiButtonState.Selected => "selected",
            _ => "normal"
        } : "normal";
        Color tint = style == 1 ? state switch
        {
            PangyaUiButtonState.Hover => Color.FromArgb(224, 224, 224),
            PangyaUiButtonState.Selected => Color.FromArgb(176, 176, 176),
            _ => Color.White
        } : Color.White;
        double elapsed = runtime?.SecondsSincePress ?? double.PositiveInfinity;
        if (options.ButtonPressTicks?.TryGetValue(node, out long pressedAt) == true)
            elapsed = (options.CurrentTick - pressedAt) / 1000.0;
        double pushDelay = gaugeArrow ? 0 : runtime?.PushDelay ?? GetButtonPushDelay(node);
        if (style is >= 1 and <= 3 && ((!enabled && style is 1 or 2) || elapsed <= pushDelay))
            tint = Color.FromArgb(80, 176, 176, 176);
        Image? replacement = !gaugeArrow && HasButtonSkin(node, options) ? GetReplacementButtonImage(runtime, Symbols(options)) : null;
        bool blinkPhase = runtime?.Blink ?? options.LayoutAnimationTime.TotalMilliseconds % 1400 >= 700;
        if (style is < 1 or > 3) tint = Color.Transparent;
        if (!IsWindowViewFocused(node, options)) tint = Color.FromArgb(tint.A / 2, tint);
        double alphaModifier = GetWindowAlphaScale(node, options);
        if (gaugeArrow)
        {
            float primary = options.GaugeButtonAlpha?.GetValueOrDefault((node, rightGaugeButton), 1) ?? 1;
            float secondary = options.GaugeButtonAlpha2?.GetValueOrDefault((node, rightGaugeButton), 1) ?? 1;
            alphaModifier = (float.IsFinite(primary) ? Math.Clamp(primary, 0, 1) : 1) *
                (double)(float.IsFinite(secondary) ? Math.Clamp(secondary, 0, 1) : 1);
        }
        if (IsCombo(node))
        {
            float primary = options.ComboButtonAlpha?.GetValueOrDefault(node, 1) ?? 1;
            float secondary = options.ComboButtonAlpha2?.GetValueOrDefault(node, 1) ?? 1;
            alphaModifier = (float.IsFinite(primary) ? Math.Clamp(primary, 0, 1) : 1) *
                (double)(float.IsFinite(secondary) ? Math.Clamp(secondary, 0, 1) : 1);
        }
        if (!rightGaugeButton && blinkPhase)
        {
            bool hasBlinkImage = runtime?.Blink == true && runtime.ReplacementMode == "blink" &&
                replacement is not null;
            hasBlinkImage |= runtime?.Blink == true &&
                GetRuntimeButtonImage(runtime, "blink", Symbols(options)) is not null;
            if (hasBlinkImage ||
                (node.Parameters.GetValueOrDefault("blink") is { Length: > 0 } blink &&
                 GetBitmap(blink, Symbols(options)) is not null))
                mode = "blink";
            else if (runtime?.Blink == true)
                alphaModifier *= 0.5;
        }
        if (options.ButtonOpacity?.TryGetValue(node, out byte opacity) == true)
            alphaModifier *= opacity / 255.0;
        tint = Color.FromArgb((int)(tint.A * alphaModifier), tint);
        foreach (string key in rightGaugeButton ? new[] { "r" + mode } : new[] { "below_" + mode, mode })
        {
            if (!gaugeArrow && runtime?.BitmapImages?.TryGetValue(key, out Image? explicitBitmap) == true &&
                explicitBitmap is null) continue;
            Image? image = key == runtime?.ReplacementMode ? replacement : null;
            if (!gaugeArrow && image is null && !key.StartsWith("below_", StringComparison.Ordinal))
                image = GetRuntimeButtonImage(runtime, key, Symbols(options));
            if (key.StartsWith("below_", StringComparison.Ordinal) &&
                runtime?.BelowImages?.TryGetValue(mode, out string? below) == true)
            {
                if (below is null) continue;
                image = GetBitmap(below, Symbols(options));
            }
            if (image is null && node.Parameters.TryGetValue(key, out string? filename))
                image = GetBitmap(filename, Symbols(options));
            if (image is not null)
                DrawTintedImage(graphics, image, bounds, 0, image.Width, image.Height, tint);
        }

        if (HasButtonSkin(node, options) &&
            int.TryParse(node.Parameters.GetValueOrDefault("drawstyle"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int drawStyle) && drawStyle != 0 &&
            options.ButtonOwnerDraw?.GetValueOrDefault(node) is { } drawOwner)
        {
            GraphicsState ownerState = graphics.Save();
            try { drawOwner(graphics, bounds, state); }
            finally { graphics.Restore(ownerState); }
        }
    }

    public static PangyaUiRenderOptions SetButtonPushDelay(PangyaUiNode node, double delay,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!node.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The node must be a button.", nameof(node));
        if (!double.IsFinite(delay)) throw new ArgumentOutOfRangeException(nameof(delay));
        options ??= new();
        var states = options.ImageButtonStates?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var state = states.GetValueOrDefault(node) ?? new(Enabled: options.DisabledNodes?.Contains(node) != true);
        states[node] = state with { PushDelay = delay, SecondsSincePress = delay + 0.1 };
        var ticks = options.ButtonPressTicks?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        ticks.Remove(node);
        return options with { ImageButtonStates = states, ButtonPressTicks = ticks };
    }

    public static double GetButtonPushDelay(PangyaUiNode node, PangyaUiRenderOptions options) =>
        options.ImageButtonStates?.GetValueOrDefault(node)?.PushDelay ?? GetButtonPushDelay(node);

    public static double GetButtonPushDelay(PangyaUiNode node)
    {
        if (!node.Type.Equals("BUTTON", StringComparison.OrdinalIgnoreCase)) return 0;
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
        if (node.Name == "picture" && node.Parent is { } room &&
            string.Equals(room.Name, "makeroom", StringComparison.OrdinalIgnoreCase))
        {
            string modeImage = options?.MakeRoomMatchForms?.Contains(room) == true
                ? "picture_match.tga" : "picture_stroke.tga";
            if (GetBitmap(modeImage, symbols) is Image picture) return picture;
        }
        filename = node.Parameters.GetValueOrDefault("image");
        return string.IsNullOrWhiteSpace(filename) ? null : GetBitmap(filename, symbols);
    }

    public bool TrySetAreaImage(PangyaUiNode node, string? filename, PangyaUiRenderOptions options,
        out PangyaUiRenderOptions updatedOptions)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(options);
        if (!IsArea(node)) throw new ArgumentException("The node must be an area.", nameof(node));
        updatedOptions = options;
        string previous = options.AreaImageNames?.GetValueOrDefault(node) ??
            options.AreaImages?.GetValueOrDefault(node) ?? node.Parameters.GetValueOrDefault("bgimg") ?? string.Empty;
        if (filename is not null && string.Equals(previous, filename, StringComparison.OrdinalIgnoreCase))
            return true;
        var names = options.AreaImageNames?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var images = options.AreaImages?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var bitmaps = options.AreaBitmapImages?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        names[node] = filename ?? previous;
        images[node] = filename;
        bitmaps.Remove(node);
        updatedOptions = options with { AreaImageNames = names, AreaImages = images, AreaBitmapImages = bitmaps };
        return filename is null || GetBitmap(filename, Symbols(options)) is not null;
    }

    public static bool TrySetAreaBitmap(PangyaUiNode node, Image? image, PangyaUiRenderOptions options,
        out PangyaUiRenderOptions updatedOptions)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(options);
        if (!IsArea(node)) throw new ArgumentException("The node must be an area.", nameof(node));
        updatedOptions = options;
        if (image is null) return false;
        var bitmaps = options.AreaBitmapImages?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        bitmaps[node] = image;
        updatedOptions = options with { AreaBitmapImages = bitmaps };
        return true;
    }

    public bool TryOpenViewer(PangyaUiNode node, string filename, PangyaUiRenderOptions options,
        out PangyaUiRenderOptions updatedOptions)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(filename);
        ArgumentNullException.ThrowIfNull(options);
        if (!IsViewer(node)) throw new ArgumentException("The node must be a viewer.", nameof(node));
        updatedOptions = options;
        string current = options.ViewerImages?.GetValueOrDefault(node) ??
            node.Parameters.GetValueOrDefault("image") ?? string.Empty;
        if (current.Length > 0 && GetBitmap(current, Symbols(options)) is null)
        {
            current = node.Parameters.GetValueOrDefault("image") ?? string.Empty;
            if (current.Length > 0 && GetBitmap(current, Symbols(options)) is null)
                current = string.Empty;
        }
        if (string.Equals(current, filename, StringComparison.Ordinal)) return true;
        if (string.IsNullOrEmpty(filename) || GetBitmap(filename, Symbols(options)) is not Image image) return false;
        var images = options.ViewerImages?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var rows = options.ViewerTopRows?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var fractionalRows = options.ViewerScrollRows?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        images[node] = filename;
        int top = node.Parameters.GetValueOrDefault("use_wheel") == "true" &&
            options.ViewerFollowBottom?.GetValueOrDefault(node) == true
            ? Math.Max(0, image.Height / ViewerRowHeight - GetWindowBounds(node, options).Height / ViewerRowHeight) : 0;
        rows[node] = top;
        fractionalRows[node] = top;
        updatedOptions = options with { ViewerImages = images, ViewerTopRows = rows, ViewerScrollRows = fractionalRows };
        return true;
    }

    public int GetViewerMaximumTopRow(PangyaUiNode node, IReadOnlySet<string>? symbols = null,
        PangyaUiRenderOptions? options = null) =>
        IsViewer(node) && !IsRuntimeHidden(node, options)
            ? GetViewerScrollRange(node, GetViewerImage(node, symbols ?? Symbols(options ?? new()), options), options)
            : 0;

    private static int GetViewerScrollRange(PangyaUiNode node, Image? image,
        PangyaUiRenderOptions? options) =>
        node.Parameters.GetValueOrDefault("use_wheel") == "true" &&
        GetWindowBounds(node, options).Height >= ViewerRowHeight && image is not null
            ? Math.Max(0, image.Height / ViewerRowHeight - GetWindowBounds(node, options).Height / ViewerRowHeight)
            : 0;
    private static float GetViewerScrollRow(PangyaUiNode node, PangyaUiRenderOptions options) =>
        options.ViewerScrollRows?.TryGetValue(node, out float row) == true && float.IsFinite(row)
            ? row : options.ViewerTopRows?.GetValueOrDefault(node) ?? 0;

    private Rectangle DrawViewer(Graphics? graphics, PangyaUiNode node, Rectangle bounds,
        PangyaUiRenderOptions options)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0) return Rectangle.Empty;
        Rectangle? measured = null;
        Image? image = GetViewerImage(node, Symbols(options), options);
        float row = GetViewerScrollRow(node, options);
        byte opacity = GetScrollbarOpacity(node, options,
            options.ViewerOpacity?.GetValueOrDefault(node, byte.MaxValue) ?? byte.MaxValue);
        if (image is not null)
        {
            float sourceY = Math.Clamp(row, 0, GetViewerScrollRange(node, image, options)) * ViewerRowHeight;
            int width = Math.Min(bounds.Width, image.Width);
            int height = Math.Min(bounds.Height, (int)(image.Height - sourceY));
            node.Parameters.TryGetValue("bgcolor", out string? color);
            Color tint = options.DisabledNodes?.Contains(node) == true
                ? Color.FromArgb(80, 176, 176, 176) : ParseColor(color, Color.White, implicitAlpha: false);
            double imageAlpha = GetWindowAlphaScale(node, options, secondary: false) *
                (options.ViewerOpacity?.GetValueOrDefault(node, byte.MaxValue) ?? byte.MaxValue) / byte.MaxValue;
            tint = Color.FromArgb((int)(tint.A * imageAlpha), tint);
            var destination = new Rectangle(bounds.X, bounds.Y, width, height);
            IncludeBounds(ref measured, destination);
            if (graphics is not null) DrawTintedImage(graphics, image, destination, sourceY, width, height, tint,
                useGraphicsAlpha: false);
        }

        if (options.ViewerOwnerDraw?.GetValueOrDefault(node) is { } drawOwner)
        {
            IncludeBounds(ref measured, bounds);
            if (graphics is not null)
            {
                GraphicsState state = graphics.Save();
                try { drawOwner(graphics, bounds); }
                finally { graphics.Restore(state); }
            }
        }
        if (node.Parameters.GetValueOrDefault("use_wheel") == "true" &&
            options.HiddenViewerScrollbars?.Contains(node) != true)
            IncludeBounds(ref measured, DrawScrollBar(graphics, bounds, bounds.Height / ViewerRowHeight,
                (image?.Height ?? 0) / ViewerRowHeight, row, Symbols(options),
                showGuide: options.ScrollbarGuides?.GetValueOrDefault(node, true) ?? true,
                focused: IsWindowViewFocused(node, options) && ReferenceEquals(options.HoveredNode, node) &&
                    GetViewerScrollRange(node, image, options) > 0, opacity: opacity, measure: true));
        return measured ?? Rectangle.Empty;
    }

    public Rectangle GetViewerViewport(PangyaUiNode node) =>
        GetViewerViewport(node, null);

    public Rectangle GetViewerViewport(PangyaUiNode node, PangyaUiRenderOptions? options) =>
        IsViewer(node) && !IsRuntimeHidden(node, options) ? GetAbsoluteBounds(node, options) : Rectangle.Empty;

    public Rectangle GetViewerScrollThumb(PangyaUiNode node, PangyaUiRenderOptions options)
    {
        IReadOnlySet<string> symbols = Symbols(options);
        if (IsRuntimeHidden(node, options) || options.HiddenViewerScrollbars?.Contains(node) == true ||
            !IsViewer(node) || GetViewerImage(node, symbols, options) is not Image image ||
            GetViewerScrollRange(node, image, options) <= 0)
            return Rectangle.Empty;
        Rectangle bounds = GetViewerViewport(node, options);
        return DrawScrollBar(null, bounds, bounds.Height / ViewerRowHeight,
            image.Height / ViewerRowHeight, GetViewerScrollRow(node, options),
            symbols, showGuide: false);
    }

    private Rectangle DrawScrollBar(Graphics? graphics, Rectangle bounds, int capacity, int rows,
        float topRow, IReadOnlySet<string> symbols, bool showGuide, bool focused = false, byte opacity = byte.MaxValue, bool measure = false, bool atLeft = false)
    {
        if (capacity <= 0 || (rows <= capacity && !showGuide)) return Rectangle.Empty;
        Image? top = GetBitmap("scroll_n_1", symbols);
        Image? middle = GetBitmap("scroll_n_2", symbols);
        Image? bottom = GetBitmap("scroll_n_3", symbols);
        if (top is null || middle is null || bottom is null) return Rectangle.Empty;

        Rectangle? measured = null;
        int x = ClampCoordinate((long)bounds.X + (atLeft ? 0 : bounds.Width - top.Width) + 2);
        float thumbHeight = bounds.Height;
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
            double exactThumbHeight = caps + (double)(bounds.Height - caps) * capacity / rows;
            thumbHeight = (float)exactThumbHeight;
            int maxRow = rows - capacity;
            double row = float.IsNaN(topRow) ? 0 : Math.Clamp((double)topRow, 0, maxRow);
            double travel = (bounds.Height - exactThumbHeight) * row / maxRow;
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
        return measure ? measured ?? Rectangle.Empty : Rectangle.Ceiling(new RectangleF(x, thumbY, top.Width, thumbHeight));

        void DrawStrip(Image first, Image center, Image last, int left, int y, int width, float height,
            int? firstCap = null, int? lastCap = null, int? reservedBottomHeight = null)
        {
            if (graphics is null && !measure) return;
            float firstHeight = Math.Min(firstCap ?? first.Height, height);
            float lastHeight = Math.Min(lastCap ?? last.Height, height - firstHeight);
            float centerHeight = Math.Max(0, height - firstHeight - (reservedBottomHeight ?? lastHeight));
            if (firstHeight > 0) DrawFragment(first, new RectangleF(left, y, width, firstHeight));
            if (centerHeight > 0) DrawFragment(center,
                new RectangleF(left, (float)Math.Clamp((double)y + firstHeight, int.MinValue, int.MaxValue), width, centerHeight));
            if (lastHeight > 0) DrawFragment(last,
                new RectangleF(left, (float)Math.Clamp((double)y + firstHeight + centerHeight, int.MinValue, int.MaxValue), width, lastHeight));
        }

        void DrawFragment(Image image, RectangleF destination)
        {
            if (measure) IncludeBounds(ref measured, Rectangle.Ceiling(destination));
            if (graphics is null) return;
            if (opacity == byte.MaxValue) DrawGraphicsImage(graphics, image, destination);
            else
            {
                using var attributes = new ImageAttributes();
                attributes.SetColorMatrix(new ColorMatrix
                {
                    Matrix33 = (int)(opacity * GetGraphicsAlpha(graphics)) / 255f
                });
                PointF[] corners = [destination.Location, new(destination.Right, destination.Top),
                    new(destination.Left, destination.Bottom)];
                graphics.DrawImage(image, corners, new RectangleF(0, 0, image.Width, image.Height),
                    GraphicsUnit.Pixel, attributes);
            }
        }
    }

    private static void DrawTintedImage(Graphics graphics, Image image, Rectangle destination,
        float sourceY, float width, float height, Color tint, float sourceX = 0, bool useGraphicsAlpha = true)
    {
        if (useGraphicsAlpha) tint = Color.FromArgb((int)(tint.A * GetGraphicsAlpha(graphics)), tint);
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix
        {
            Matrix00 = tint.R / 255f,
            Matrix11 = tint.G / 255f,
            Matrix22 = tint.B / 255f,
            Matrix33 = tint.A / 255f
        });
        graphics.DrawImage(image, destination,
            sourceX, sourceY, width, height, GraphicsUnit.Pixel, attributes);
    }

    private static void DrawGraphicsImage(Graphics graphics, Image image, Rectangle destination)
    {
        if (GetGraphicsAlpha(graphics) == 1) graphics.DrawImage(image, destination);
        else DrawTintedImage(graphics, image, destination, 0, image.Width, image.Height, Color.White);
    }

    private static void DrawGraphicsImage(Graphics graphics, Image image, PointF[] destination, RectangleF source)
    {
        if (GetGraphicsAlpha(graphics) == 1)
        {
            graphics.DrawImage(image, destination, source, GraphicsUnit.Pixel);
            return;
        }
        using var attributes = new ImageAttributes();
        attributes.SetColorMatrix(new ColorMatrix { Matrix33 = (int)(255 * GetGraphicsAlpha(graphics)) / 255f });
        graphics.DrawImage(image, destination, source, GraphicsUnit.Pixel, attributes);
    }

    private static void DrawGraphicsImage(Graphics graphics, Image image, RectangleF destination)
    {
        if (GetGraphicsAlpha(graphics) == 1) graphics.DrawImage(image, destination);
        else DrawGraphicsImage(graphics, image,
            [destination.Location, new PointF(destination.Right, destination.Top),
                new PointF(destination.Left, destination.Bottom)],
            new RectangleF(0, 0, image.Width, image.Height));
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
            frame.Element["cfrm"]?.GetAttribute("filename") is { Length: > 0 } prefix &&
            GetBitmap(prefix + "00", symbols) is not null;
    }

    private static void DrawDesktopWallpaper(Graphics graphics, Image image, Rectangle bounds,
        Color tint)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0 || tint.A == 0) return;
        GraphicsState state = graphics.Save();
        try
        {
            graphics.PixelOffsetMode = PixelOffsetMode.Half;
            if (tint.ToArgb() == Color.White.ToArgb())
                graphics.DrawImage(image, bounds, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
            else
                DrawTintedImage(graphics, image, bounds, 0, image.Width, image.Height, tint, useGraphicsAlpha: false);
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

    private static string GetRenderResource(PangyaUiNode node, PangyaUiButtonState state,
        IReadOnlyDictionary<PangyaUiNode, string?>? areaImages = null)
    {
        if (node.Type.Equals("STATIC", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        if (IsArea(node) && areaImages?.TryGetValue(node, out string? image) == true)
            return image ?? string.Empty;
        if ((node.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) ||
             node.Type.Equals("CONTEXTMENU", StringComparison.OrdinalIgnoreCase)) &&
            node.Parameters.TryGetValue("resource", out string? resource) &&
            !string.IsNullOrWhiteSpace(resource))
            return resource;
        if (IsArea(node))
        {
            if (node.Name == "text" && string.Equals(node.Parent?.Name, "makeroom", StringComparison.OrdinalIgnoreCase))
                return "text_vs";
            if (node.Name == "text_map" && node.Parent?.Name == "map_ctrl" &&
                string.Equals(node.Parent.Parent?.Name, "makeroom", StringComparison.OrdinalIgnoreCase))
                return "text_vs_map";
        }
        return node.GetResource(state);
    }

    private Rectangle DrawFrame(Graphics? graphics, Rectangle bounds, PangyaUiNode definition, IReadOnlySet<string> symbols, bool showBlink,
        string? baseImageOverride = null, PangyaUiNode? textSource = null, bool isViewFocused = true,
        bool contextMenu = false, bool hideDescription = false, PangyaUiRenderOptions? textOptions = null, PangyaUiNode? frameInstance = null)
    {
        XmlElement[] layers = definition.Element.ChildNodes.OfType<XmlElement>()
            .Where(child => child.LocalName.Equals("layer", StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToArray();
        Rectangle? measured = bounds;
        PangyaUiNode? instance = frameInstance ?? textSource;
        PangyaUiFrameText? frameText = instance is null ? null : textOptions?.FrameText?.GetValueOrDefault(instance);
        double opacity = contextMenu && instance is not null
            ? textOptions?.MenuStates?.GetValueOrDefault(instance)?.Opacity ?? byte.MaxValue
            : frameText?.Opacity ?? byte.MaxValue;
        if (instance is not null && textOptions is not null)
        {
            if (instance.IsForm && !instance.IsLayout && !contextMenu)
            {
                float primary = textOptions.FormFrameAlpha?.GetValueOrDefault(instance, 1) ?? 1;
                float secondary = textOptions.FormFrameAlpha2?.GetValueOrDefault(instance, 1) ?? 1;
                opacity *= (float.IsFinite(primary) ? Math.Clamp(primary, 0, 1) : 1) *
                    (double)(float.IsFinite(secondary) ? Math.Clamp(secondary, 0, 1) : 1);
            }
            else opacity *= GetWindowAlphaScale(instance, textOptions, secondary: !contextMenu);
        }
        XmlElement? baseLayer = layers.FirstOrDefault();
        int baseType = ReadLayerValue(baseLayer, "type", 0);
        showBlink = showBlink && !contextMenu && baseType == 3 &&
            definition.Element["cfrm"]?.GetAttribute("filename") is { Length: > 0 } blinkPrefix &&
            GetBitmap(blinkPrefix + "00", symbols) is not null;
        bool dim = !(isViewFocused && (contextMenu || frameText?.UseCaptionFocus != false)) && !showBlink;
        bool baseVisible = baseLayer is not null && ReadLayerValue(baseLayer, "invisible", 0) == 0;
        Rectangle baseGeometry = baseLayer is null ? Rectangle.Empty :
            DrawFrameLayer(baseVisible ? graphics : null, bounds, definition, symbols, "bfrm",
            baseType, showBlink: showBlink, imageOverride: baseImageOverride, dim: dim, opacity: opacity, slicedOnly: contextMenu);
        if (baseVisible) IncludeBounds(ref measured, baseGeometry);
        int descriptionHeight = baseLayer is null || baseGeometry.IsEmpty ? 0 : baseGeometry.Height;
        if (graphics is not null && textSource is not null)
        {
            using var format = (StringFormat)StringFormat.GenericTypographic.Clone();
            using var title = new SolidBrush(Color.FromArgb((int)opacity, Color.White));
            using var outline = new SolidBrush(Color.FromArgb((int)opacity, Color.Gray));
            bool useDescriptionEmoticons = frameText?.UseDescriptionEmoticons != false;
            using var descriptionBrush = new SolidBrush(Color.FromArgb(useDescriptionEmoticons
                ? byte.MaxValue : (int)opacity, 64, 64, 64));
            string caption = frameText?.Caption ?? textSource.Element.GetAttribute("caption");
            string description = frameText?.Description ?? textSource.Parameters.GetValueOrDefault("description") ??
                textSource.Element.GetAttribute("description");
            if (caption.Length > 0)
                DrawStyledText(graphics, caption, title, outline,
                    new PointF(bounds.X + 10f, bounds.Y + 7f), format, 2, drawText: textOptions?.DrawCaptionText);
            if (!hideDescription && description.Length > 0)
                DrawTaggedCaption(graphics, description, descriptionBrush, title,
                    new PointF(bounds.X + 10f, bounds.Y + (float)descriptionHeight - (frameText?.UseCaptionOffset == true ? 17f : 26f)), format, 0,
                    emoticonSymbols: useDescriptionEmoticons ? symbols : null,
                    animationElapsed: textOptions?.EmoticonAnimationTime,
                    lineHeight: textOptions?.CaptionFontHeight, measureText: textOptions?.MeasureCaptionText,
                    drawText: textOptions?.DrawCaptionText, parseTags: false,
                    forceOpaqueText: useDescriptionEmoticons);
        }
        foreach (XmlElement layer in baseType == 0 ? layers.Skip(1) : [])
        {
            if (ReadLayerValue(layer, "invisible", 0) != 0) continue;
            int height = ReadLayerValue(layer, "height", -1);
            long position = ReadLayerValue(layer, "pos", 0);
            int bottom = ReadLayerValue(layer, "bottompos", 0);
            if (bottom != 0) position = -(long)bottom;
            Rectangle layerBounds = bounds;
            if (height >= 0) layerBounds.Height = height;
            long y = position < 0 ? (long)bounds.Y + bounds.Height + position - layerBounds.Height :
                (long)bounds.Y + position;
            layerBounds.Y = ClampCoordinate(y);
            IncludeBounds(ref measured, DrawFrameLayer(graphics, layerBounds, definition, symbols, "sfrm",
                ReadLayerValue(layer, "type", 0), position < 0, showBlink: showBlink,
                imageOverride: contextMenu ? "sfrm" : null,
                dim: dim, opacity: opacity, slicedOnly: contextMenu));
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

    private Rectangle GetInstantiatedFormBounds(PangyaUiNode form, Rectangle bounds,
        PangyaUiRenderOptions options, HashSet<PangyaUiNode> visited, Point parentOrigin = default)
    {
        Rectangle? result = GetFormFrameBounds(form, bounds, options);
        if (!visited.Add(form)) return result.Value;
        foreach (PangyaUiNode child in form.Children.Where(child => child.IsVisible(Symbols(options))))
        {
            Rectangle childBounds = GetReferencedBounds(child, options, visited, AddOrigins(parentOrigin, bounds.Location));
            if (childBounds.IsEmpty) continue;
            childBounds.X = ClampCoordinate((long)childBounds.X + bounds.X);
            childBounds.Y = ClampCoordinate((long)childBounds.Y + bounds.Y);
            IncludeBounds(ref result, childBounds);
        }
        visited.Remove(form);
        return result ?? bounds;
    }

    private Rectangle GetBaseFrameBounds(Rectangle bounds, PangyaUiNode definition,
        IReadOnlySet<string> symbols, string? imageOverride) =>
        DrawFrame(null, bounds, definition, symbols, showBlink: false, baseImageOverride: imageOverride);

    private Rectangle DrawFrameLayer(Graphics? graphics, Rectangle bounds, PangyaUiNode definition,
        IReadOnlySet<string> symbols, string tag, int type, bool alignBottom = false, bool showBlink = false,
        string? imageOverride = null, bool dim = false, double opacity = byte.MaxValue, bool slicedOnly = false)
    {
        // Context menus have their own native DrawFrame implementation: unlike
        // FrFrame, it has no type-3 drawing branches.
        if (slicedOnly && type == 3) return Rectangle.Empty;
        Color tint = ParseColor(definition.Element.GetAttribute("color"), Color.White, implicitAlpha: false);
        tint = Color.FromArgb((int)(tint.A * opacity / byte.MaxValue), tint);
        if (dim)
        {
            int argb = tint.ToArgb();
            tint = Color.FromArgb((argb & unchecked((int)0xff000000)) | ((argb & 0xffffff) * 4 / 5));
        }

        void DrawSlice(Image image, Rectangle destination)
        {
            if (graphics is null || tint.A == 0) return;
            if (tint.ToArgb() == Color.White.ToArgb())
                DrawGraphicsImage(graphics, image, destination);
            else
                DrawTintedImage(graphics, image, destination, 0, image.Width, image.Height, tint);
        }
        XmlElement? frame = definition.Element.ChildNodes.OfType<XmlElement>()
            .FirstOrDefault(child => child.LocalName.Equals(tag,
                StringComparison.OrdinalIgnoreCase));
        string baseName = imageOverride ?? frame?.GetAttribute("filename") ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseName)) return Rectangle.Empty;
        if (!slicedOnly && Path.HasExtension(baseName))
        {
            if (slicedOnly || bounds.Width <= 0 || bounds.Height <= 0) return Rectangle.Empty;
            Image? direct = GetBitmap(baseName, symbols);
            if (direct is not null) DrawSlice(direct, bounds);
            return direct is not null ? bounds : Rectangle.Empty;
        }

        Image?[] slices = Enumerable.Range(0, 9)
            .Select(index => (type == 1 && index >= 3 || type == 2 && index < 6 && (tag == "bfrm" || index is not (0 or 2)))
                ? null : GetBitmap(baseName + index.ToString("00", CultureInfo.InvariantCulture), symbols))
            .ToArray();
        if (slices.Skip(type == 2 ? 6 : 0).All(image => image is null))
        {
            if (slicedOnly || bounds.Width <= 0 || bounds.Height <= 0) return Rectangle.Empty;
            Image? direct = GetBitmap(baseName, symbols);
            if (direct is not null) DrawSlice(direct, bounds);
            return direct is not null ? bounds : Rectangle.Empty;
        }

        int left = slices[0]?.Width ?? slices[3]?.Width ?? slices[6]?.Width ?? 0;
        int right = slices[2]?.Width ?? slices[5]?.Width ?? slices[8]?.Width ?? 0;
        int top = type == 2 ? 0 : slices[0]?.Height ?? slices[1]?.Height ?? 0;
        int bottom = slices[6]?.Height ?? slices[7]?.Height ?? 0;
        int[] widths = [left, (int)Math.Max(0L, (long)bounds.Width - left - right), right];
        int middle = type is 1 or 2 ? 0 : (int)Math.Max(0L, (long)bounds.Height - top - bottom);
        int[] heights = [top, middle, bottom];
        var layerGeometry = new Rectangle(bounds.X,
                alignBottom ? ClampCoordinate((long)bounds.Y + bounds.Height - top - middle - bottom) : bounds.Y,
                ClampCoordinate(Math.Max(bounds.Width, (long)left + right)),
                ClampCoordinate((long)top + middle + bottom));
        if (graphics is null || tint.A == 0) return layerGeometry;
        Image?[]? blink = null;
        if (showBlink && type is 0 or 1 or 3)
        {
            string prefix = definition.Element["cfrm"]?.GetAttribute("filename") ?? string.Empty;
            if (prefix.Length > 0 && GetBitmap(prefix + "00", symbols) is Image first)
                blink = [first, GetBitmap(prefix + "01", symbols), GetBitmap(prefix + "02", symbols)];
        }
        int y = alignBottom
            ? ClampCoordinate((long)bounds.Y + bounds.Height - top - middle - bottom)
            : bounds.Y;
        for (int row = 0; row < 3; row++)
        {
            Image?[] rowImages = row == 0 && blink is not null ? blink : slices;
            int rowStart = ReferenceEquals(rowImages, slices) ? row * 3 : 0;
            int x = bounds.X;
            for (int column = 0; column < 3; column++)
            {
                Image? image = rowImages[rowStart] is null ? null : rowImages[rowStart + column];
                var target = new Rectangle(x, y, widths[column], heights[row]);
                if (image is not null && target.Width > 0 && target.Height > 0)
                    DrawSlice(image, target);
                x = ClampCoordinate((long)x + widths[column]);
            }
            y = ClampCoordinate((long)y + heights[row]);
        }
        return layerGeometry;
    }

    private Rectangle GetReferencedBounds(PangyaUiNode node,
        PangyaUiRenderOptions options, HashSet<PangyaUiNode> visited, Point parentOrigin = default)
    {
        if (IsRuntimeHidden(node, options) || !IsRenderVisible(node, options) || !visited.Add(node))
            return Rectangle.Empty;
        Rectangle? result = null;
        Point absoluteOrigin = AddOrigins(parentOrigin, GetWindowOffset(node, options));
        IReadOnlySet<string> symbols = Symbols(options);
        string resource = GetRenderResource(node, options.ButtonState, options.AreaImages);
        string resourceType = IsArea(node) && options.AreaImages?.ContainsKey(node) == true ? "BITMAP" : node.Type;
        Image? areaBitmap = IsArea(node) ? options.AreaBitmapImages?.GetValueOrDefault(node) : null;
        PangyaUiNode? definition = areaBitmap is null
            ? _resources.TryResolve(resource, resourceType, symbols) : null;
        if (GetBitmapFilename(definition) is string bitmapFilename)
        {
            resource = bitmapFilename;
            definition = null;
        }
        if (IsContextMenu(node))
        {
            using var bitmap = new Bitmap(1, 1);
            using Graphics metrics = Graphics.FromImage(bitmap);
            IncludeBounds(ref result,
                RenderContextMenu(metrics, node, GetWindowBounds(node, options).Location, options, draw: false, out _));
        }
        else if (GetTabs(node, options) is { } tabs)
            IncludeBounds(ref result, RenderTabs(null, node, GetWindowBounds(node, options).Location, tabs, symbols, options));
        else if (IsTextButton(node))
        {
            string textButton = GetTextButtonCaption(node, options);
            using var bitmap = new Bitmap(1, 1);
            using Graphics metrics = Graphics.FromImage(bitmap);
            DrawTextButton(metrics, node, GetWindowBounds(node, options), textButton, options with { Pointer = null },
                out Rectangle measuredBounds, draw: false);
            IncludeBounds(ref result, measuredBounds);
        }
        else if (GetList(node, options) is not null || GetStaticCaption(node, options).Length > 0 ||
                 node.LayoutBase is not null)
            IncludeBounds(ref result, GetWindowBounds(node, options));
        else if (IsArea(node) && options.AreaOwnerDraw?.ContainsKey(node) == true)
        {
            IncludeBounds(ref result, GetWindowBounds(node, options));
            if (node.Parameters.GetValueOrDefault("entrance_effect") is { Length: > 0 } effect &&
                GetBitmap(resource, symbols) is Image image)
            {
                var (opening, duration, direction) = ReadAreaEntrance(effect);
                var rest = new Rectangle(GetWindowBounds(node, options).Location, image.Size);
                IncludeBounds(ref result, rest);
                if (opening > 0 || duration >= 0)
                {
                    int x = direction switch { "left" => -image.Width, "right" => image.Width, _ => 0 };
                    int y = direction switch { "top" => -image.Height, "bottom" => image.Height, _ => 0 };
                    IncludeBounds(ref result, new Rectangle(ClampCoordinate((long)rest.X + x),
                        ClampCoordinate((long)rest.Y + y), rest.Width, rest.Height));
                }
            }
        }
        else if (IsViewer(node))
            IncludeBounds(ref result, DrawViewer(null, node, GetWindowBounds(node, options), options));
        else if (IsEdit(node))
            IncludeBounds(ref result, GetEditBounds(node, GetWindowBounds(node, options), symbols, options));
        else if (IsGauge(node))
            IncludeBounds(ref result, GetWindowBounds(node, options));
        else if (IsImageGauge(node))
            IncludeBounds(ref result, RenderImageGauge(null, node, GetWindowBounds(node, options).Location, symbols, options));
        else if (HasButtonSkin(node, options))
            IncludeBounds(ref result, GetButtonBounds(node, GetWindowBounds(node, options), symbols, options));
        else if (definition?.Type.Equals("FRAME", StringComparison.OrdinalIgnoreCase) == true)
            IncludeBounds(ref result, GetBaseFrameBounds(GetWindowBounds(node, options), definition, symbols,
                node.Parameters.GetValueOrDefault("bgimg")));
        else if (definition?.IsForm == true && !definition.IsLayout &&
                 definition.Bounds.Size is { Width: > 0, Height: > 0 } formSize)
            IncludeBounds(ref result, GetInstantiatedFormBounds(definition,
                new Rectangle(GetWindowBounds(node, options).Location,
                    options.WindowSizes?.ContainsKey(node) == true ? GetWindowBounds(node, options).Size : formSize),
                options, visited, absoluteOrigin));
        else if (definition is not null)
        {
            Rectangle referenced = GetReferencedBounds(definition, options, visited,
                IsMacroItem(node) ? absoluteOrigin : AddOrigins(absoluteOrigin, GetWindowBounds(node, options).Location));
            if (!IsMacroItem(node)) referenced.Offset(GetWindowBounds(node, options).Location);
            IncludeBounds(ref result, referenced);
        }
        else if (!IsMacroItem(node))
        {
            Image? image = areaBitmap ?? (string.IsNullOrWhiteSpace(resource) ? null : _images.GetImage(resource));
            if (image is not null && IsRenderVisible(node, options) &&
                !IsTransparent(node, options.ButtonState))
            {
                Rectangle destination = ImageBounds(node, GetWindowBounds(node, options), image);
                IncludeBounds(ref result,
                    ApplyAreaEntrance(node, GetWindowBounds(node, options), image, options, ref destination) ??
                    ClipImageBounds(node, GetWindowBounds(node, options), destination, options, absoluteOrigin));
            }
        }

        foreach (PangyaUiNode child in node.Children.Where(child => child.IsVisible(symbols)))
        {
            Rectangle childBounds = GetReferencedBounds(child, options, visited,
                IsMacroItem(node) ? absoluteOrigin : AddOrigins(absoluteOrigin, GetWindowBounds(node, options).Location));
            if (!IsMacroItem(node) && !childBounds.IsEmpty)
                childBounds.Offset(GetWindowBounds(node, options).Location);
            IncludeBounds(ref result, childBounds);
        }
        visited.Remove(node);
        if (result is Rectangle movedBounds)
        {
            Point windowOffset = GetWindowOffset(node, options);
            movedBounds.X = ClampCoordinate((long)movedBounds.X + windowOffset.X);
            movedBounds.Y = ClampCoordinate((long)movedBounds.Y + windowOffset.Y);
            result = movedBounds;
        }
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

    private static Rectangle ClipImageBounds(PangyaUiNode node, Rectangle bounds, Rectangle destination,
        PangyaUiRenderOptions options, Point absoluteOrigin = default)
    {
        if (!IsArea(node)) return destination;
        bool? clippingEnabled = options.AreaClippingEnabled?.TryGetValue(node, out bool supplied) == true
            ? supplied : null;
        if (clippingEnabled == false) return destination;
        bool xmlClipping = node.Parameters.TryGetValue("enable_clip", out string? clip) &&
            int.TryParse(clip, out int enabled) && enabled == 1;
        if (clippingEnabled != true && !xmlClipping)
            return destination;

        if (options.AreaClipOverrides?.TryGetValue(node, out Rectangle runtimeClip) == true)
            return IntersectAreaClip(destination, runtimeClip, absoluteOrigin);

        if (node.Parameters.TryGetValue("clip_area", out string? area))
            return PangyaUiDimensionHelper.ParseWindowRectangle(area) is Rectangle explicitClip
                ? IntersectAreaClip(destination, explicitClip, absoluteOrigin) : Rectangle.Empty;
        return IntersectAreaClip(destination, bounds);
    }

    private static Rectangle IntersectAreaClip(Rectangle destination, Rectangle clip, Point absoluteOrigin = default)
    {
        if (destination.Width <= 0 || destination.Height <= 0 || clip.Width <= 0 || clip.Height <= 0)
            return Rectangle.Empty;
        long left = Math.Max((long)destination.X + absoluteOrigin.X, clip.X);
        long top = Math.Max((long)destination.Y + absoluteOrigin.Y, clip.Y);
        long right = Math.Min((long)destination.X + absoluteOrigin.X + destination.Width, (long)clip.X + clip.Width);
        long bottom = Math.Min((long)destination.Y + absoluteOrigin.Y + destination.Height, (long)clip.Y + clip.Height);
        return right <= left || bottom <= top ? Rectangle.Empty :
            new Rectangle(ClampCoordinate(left - absoluteOrigin.X), ClampCoordinate(top - absoluteOrigin.Y),
                (int)(right - left), (int)(bottom - top));
    }

    private static Point AddOrigins(Point first, Point second) =>
        new(ClampCoordinate((long)first.X + second.X), ClampCoordinate((long)first.Y + second.Y));

    public static bool AreaAcceptsMouse(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!IsArea(node) || IsRuntimeHidden(node, options) || !IsRenderVisible(node, options) ||
            options?.DisabledNodes?.Contains(node) == true) return false;
        return options?.AreaMouseEvents?.TryGetValue(node, out bool enabled) == true ? enabled :
            int.TryParse(node.Parameters.GetValueOrDefault("mouse_event"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out int configured) && configured != 0;
    }

    public static PangyaUiRenderOptions SetAreaMouseEvent(PangyaUiNode node, bool active,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!IsArea(node)) throw new ArgumentException("Expected an AREA control.", nameof(node));
        options ??= new();
        var flags = options.AreaMouseEvents?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        flags[node] = active;
        return options with { AreaMouseEvents = flags };
    }

    public static PangyaUiRenderOptions SetAreaClipArea(PangyaUiNode node, Rectangle area,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!IsArea(node)) throw new ArgumentException("Expected an AREA control.", nameof(node));
        options ??= new();
        var clips = options.AreaClipOverrides?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        clips[node] = area;
        return options with { AreaClipOverrides = clips };
    }

    public static PangyaUiRenderOptions EnableAreaClipping(PangyaUiNode node, bool enabled,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!IsArea(node)) throw new ArgumentException("Expected an AREA control.", nameof(node));
        options ??= new();
        var flags = options.AreaClippingEnabled?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        flags[node] = enabled;
        return options with { AreaClippingEnabled = flags };
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

    private static Point GetWindowOffset(PangyaUiNode node, PangyaUiRenderOptions? options) =>
        IsMacroItem(node) ? Point.Empty : options?.WindowOffsets?.GetValueOrDefault(node) ?? Point.Empty;

    private static Rectangle GetWindowBounds(PangyaUiNode node, PangyaUiRenderOptions? options)
    {
        Rectangle bounds = node.Bounds;
        if (!IsMacroItem(node) && options?.WindowSizes?.TryGetValue(node, out Size size) == true)
            bounds.Size = new Size(Math.Max(0, size.Width), Math.Max(0, size.Height));
        return bounds;
    }

    private static Point GetParentOffset(PangyaUiNode node, PangyaUiRenderOptions? options)
    {
        long x = 0;
        long y = 0;
        for (PangyaUiNode? parent = node.Parent; parent is not null; parent = parent.Parent)
        {
            if (IsMacroItem(parent)) continue;
            x += parent.Bounds.X;
            y += parent.Bounds.Y;
            Point windowOffset = GetWindowOffset(parent, options);
            x += windowOffset.X;
            y += windowOffset.Y;
        }
        return new Point(ClampCoordinate(x), ClampCoordinate(y));
    }

    private static Rectangle GetAbsoluteBounds(PangyaUiNode node, PangyaUiRenderOptions? options)
    {
        Point offset = GetParentOffset(node, options);
        Point windowOffset = GetWindowOffset(node, options);
        return new Rectangle(
            ClampCoordinate((long)GetWindowBounds(node, options).X + offset.X + windowOffset.X),
            ClampCoordinate((long)GetWindowBounds(node, options).Y + offset.Y + windowOffset.Y),
            GetWindowBounds(node, options).Width,
            GetWindowBounds(node, options).Height);
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
