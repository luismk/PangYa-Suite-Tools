namespace PangyaAPI.UI;

public sealed class PangyaUiScrollState
{
    public int RowCapacity { get; private set; }
    public int ColumnCapacity { get; private set; }
    public int ItemCount { get; private set; }
    public float TopRow { get; private set; }
    public bool FollowBottom { get; set; } = true;
    public bool ShowGuide { get; set; }
    public bool AtLeft { get; private set; }
    public int RowCount => ColumnCapacity == 0 ? 0 :
        (int)(((long)ItemCount + ColumnCapacity - 1) / ColumnCapacity);
    public int RoundedTopRow => (int)(TopRow + 0.5f);

    public PangyaUiScrollState(int rowCapacity, int columnCapacity = 1, bool atLeft = false)
        => Resize(rowCapacity, columnCapacity, atLeft);

    public void Resize(int rows, int columns = 1, bool atLeft = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rows);
        ArgumentOutOfRangeException.ThrowIfNegative(columns);
        RowCapacity = rows;
        ColumnCapacity = columns;
        AtLeft = atLeft;
        Recalculate();
    }

    public void SetTopRow(int row)
    {
        TopRow = Math.Min(row, Math.Max(RowCount - RowCapacity, 0));
        Recalculate();
    }

    public void AddItem()
    {
        ItemCount = checked(ItemCount + 1);
        if (FollowBottom) TopRow = Math.Max(RowCount - RowCapacity, 0);
        Recalculate();
    }

    public void DeleteItem()
    {
        if (ItemCount > 0)
        {
            ItemCount--;
            if (ColumnCapacity != 0 && ItemCount % ColumnCapacity == 0 && TopRow > 0) TopRow--;
        }
        Recalculate();
    }

    public void Clear()
    {
        ItemCount = 0;
        TopRow = 0;
    }

    public void ScrollToFirst() { TopRow = 0; FollowBottom = false; Recalculate(); }
    public void ScrollToBottom()
    {
        FollowBottom = true;
        TopRow = Math.Max(RowCount - RowCapacity, 0);
        Recalculate();
    }

    public void ScrollUp(int delta = -1)
    {
        if (delta < 0) delta = RowCapacity / 2;
        TopRow = Math.Max(TopRow - delta, 0);
        FollowBottom = false;
        Recalculate();
    }

    public void ScrollDown(int delta = -1)
    {
        if (delta < 0) delta = RowCapacity / 2;
        if (TopRow + RowCapacity + (double)delta < RowCount) TopRow += delta;
        else { TopRow = RowCount - RowCapacity; FollowBottom = true; }
        Recalculate();
    }

    public void DragToRow(float row)
    {
        if (!float.IsFinite(row)) throw new ArgumentOutOfRangeException(nameof(row));
        int maximum = Math.Max(0, RowCount - RowCapacity);
        TopRow = Math.Clamp(row, 0, maximum);
        FollowBottom = TopRow >= maximum;
    }

    private void Recalculate()
    {
        if (RowCount <= RowCapacity) return;
        float floor = MathF.Floor(TopRow);
        TopRow = TopRow - floor > 0.5f ? floor + 1 : floor;
    }

    public PangyaUiRenderOptions ApplyToViewer(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var rows = options.ViewerTopRows?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var fractional = options.ViewerScrollRows?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var guides = options.ScrollbarGuides?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var follow = options.ViewerFollowBottom?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        rows[node] = RoundedTopRow;
        fractional[node] = TopRow;
        guides[node] = ShowGuide;
        follow[node] = FollowBottom;
        return options with { ViewerTopRows = rows, ViewerScrollRows = fractional, ScrollbarGuides = guides,
            ViewerFollowBottom = follow };
    }

    public PangyaUiRenderOptions ApplyToEdit(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var rows = options.EditTopRows?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var follow = options.EditFollowBottom?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var guides = options.ScrollbarGuides?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        rows[node] = RoundedTopRow;
        follow[node] = FollowBottom;
        guides[node] = ShowGuide;
        return options with { EditTopRows = rows, EditFollowBottom = follow, ScrollbarGuides = guides };
    }

    public PangyaUiRenderOptions ApplyToList(PangyaUiNode node, PangyaUiListPreview preview,
        PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(preview);
        options ??= new();
        var lists = options.Lists?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var guides = options.ScrollbarGuides?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        lists[node] = preview with { ItemCount = ItemCount, TopRow = RoundedTopRow,
            ColumnCapacity = Math.Max(1, ColumnCapacity), ScrollbarAtLeft = AtLeft,
            TopItem = null, FollowBottom = false };
        guides[node] = ShowGuide;
        return options with { Lists = lists, ScrollbarGuides = guides };
    }
}
