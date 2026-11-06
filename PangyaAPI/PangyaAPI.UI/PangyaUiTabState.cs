using System.Drawing;

namespace PangyaAPI.UI;

public enum PangyaUiTabActivation
{
    None,
    ButtonDown,
    ButtonUp
}

public sealed class PangyaUiTabState
{
    private readonly List<PangyaUiTab> _tabs = [];
    public int? SelectedIndex { get; private set; }
    public int? HoveredIndex { get; private set; }
    public int? PressedIndex { get; private set; }
    public PangyaUiTabActivation Activation { get; set; }
    public int SeparatorWidth { get; }
    public Size Size { get; private set; }
    public IReadOnlyList<PangyaUiTab> Tabs => _tabs.ToArray();

    public PangyaUiTabState(int separatorWidth = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(separatorWidth);
        SeparatorWidth = separatorWidth;
    }

    public void AddTab(PangyaUiTab tab, Size? normalImageSize = null)
    {
        ArgumentNullException.ThrowIfNull(tab);
        Size size = tab.Size ?? normalImageSize ??
            throw new ArgumentException("Supply the normal image size when the tab has no explicit size.", nameof(normalImageSize));
        if (size.Width < 0 || size.Height < 0) throw new ArgumentOutOfRangeException(nameof(tab));
        int width = checked(Size.Width + size.Width + (_tabs.Count > 0 ? SeparatorWidth : 0));
        _tabs.Add(tab with { Identifier = tab.Identifier ?? (uint)_tabs.Count,
            Size = size, LayoutWidth = size.Width, State = PangyaUiButtonState.Normal });
        Size = new Size(width, _tabs.Count == 1 ? size.Height : Size.Height);
    }

    public void Clear()
    {
        _tabs.Clear();
        Size = Size.Empty;
        SelectedIndex = HoveredIndex = PressedIndex = null;
    }

    public bool Select(uint identifier)
    {
        int index = _tabs.FindIndex(tab => tab.Identifier == identifier);
        if (index < 0) return false;
        SelectedIndex = index;
        return true;
    }

    public bool Unselect(uint identifier)
    {
        int index = _tabs.FindIndex(tab => tab.Identifier == identifier);
        if (index < 0) return false;
        if (SelectedIndex == index) SelectedIndex = null;
        return true;
    }

    public void SetHoveredIndex(int? index)
    {
        if (index is int value && (value < 0 || value >= _tabs.Count))
            throw new ArgumentOutOfRangeException(nameof(index));
        HoveredIndex = index;
    }

    public void ButtonDown(int? index)
    {
        SetHoveredIndex(index);
        if (Activation == PangyaUiTabActivation.ButtonDown) SelectedIndex = index ?? SelectedIndex;
        if (Activation == PangyaUiTabActivation.ButtonUp && index.HasValue) PressedIndex = index;
    }

    public bool ButtonUp(int? index)
    {
        SetHoveredIndex(index);
        if (Activation != PangyaUiTabActivation.ButtonUp) return false;
        bool activated = index == PressedIndex;
        if (activated) SelectedIndex = index;
        PressedIndex = null;
        return activated;
    }

    public PangyaUiRenderOptions ApplyTo(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var tabs = options.Tabs?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        tabs[node] = _tabs.Select((tab, index) => tab with
        {
            State = SelectedIndex == index ? PangyaUiButtonState.Selected :
                HoveredIndex == index ? PangyaUiButtonState.Hover : PangyaUiButtonState.Normal
        }).ToArray();
        var selected = options.SelectedTabIndices?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        selected.Remove(node);
        var instances = options.SelectedTabInstances?.Where(pair => !ReferenceEquals(pair.Key.Node, node))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var identifiers = options.SelectedTabIdentifiers?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        identifiers.Remove(node);
        var unselected = options.UnselectedTabIdentifiers?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        unselected.Remove(node);
        var sizes = options.WindowSizes?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        sizes[node] = Size;
        var separators = options.TabSeparatorWidths?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        separators[node] = SeparatorWidth;
        var mouseEvents = options.TabMouseEventsEnabled?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        mouseEvents[node] = Activation != PangyaUiTabActivation.None;
        var hovered = options.HoveredTabIndices?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        hovered[node] = HoveredIndex;
        return options with { Tabs = tabs, SelectedTabIndices = selected, SelectedTabIdentifiers = identifiers,
            UnselectedTabIdentifiers = unselected, SelectedTabInstances = instances,
            HoveredTab = options.HoveredTab is { } hover && ReferenceEquals(hover.Node, node) ? null : options.HoveredTab,
            WindowSizes = sizes, TabSeparatorWidths = separators,
            TabMouseEventsEnabled = mouseEvents, HoveredTabIndices = hovered };
    }
}
