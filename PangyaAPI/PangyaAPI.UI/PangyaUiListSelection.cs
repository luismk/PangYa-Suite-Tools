namespace PangyaAPI.UI;

public enum PangyaUiListNavigation
{
    Previous,
    Next,
    RowUp,
    RowDown
}

public sealed class PangyaUiListSelection
{
    private readonly HashSet<int> _selected = [];
    private readonly HashSet<int> _underCursor = [];
    public int ItemCount { get; private set; }
    public bool MultiSelect { get; set; }
    public int? Current { get; private set; }
    public int? Previous { get; private set; }
    public int? ItemUnderCursor { get; private set; }
    public bool MouseEventsEnabled { get; set; } = true;
    public IReadOnlySet<int> SelectedItems => _selected.ToHashSet();

    public PangyaUiListSelection(int itemCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(itemCount);
        ItemCount = itemCount;
    }

    public int AddItem(PangyaUiScrollState? scroll = null)
    {
        ValidateScroll(scroll);
        int index = ItemCount;
        int count = checked(ItemCount + 1);
        scroll?.AddItem();
        ItemCount = count;
        return index;
    }

    public void DeleteItem(int index, PangyaUiScrollState? scroll = null)
    {
        Validate(index);
        ValidateScroll(scroll);
        if (Current == index)
        {
            if (!MultiSelect && ItemCount > 1)
            {
                int replacement = index + 1 < ItemCount ? index + 1 : index - 1;
                if (scroll is null) SelectItem(replacement);
                else SelectItem(replacement, scroll);
            }
            else Current = null;
        }
        int? Remap(int? value) => value == index ? null : value > index ? value - 1 : value;
        Current = Remap(Current);
        // Native can retain a deleted previous-item pointer; never expose a dangling item.
        Previous = Remap(Previous);
        ItemUnderCursor = Remap(ItemUnderCursor);
        int[] hovered = _underCursor.Where(item => item != index)
            .Select(item => item > index ? item - 1 : item).ToArray();
        _underCursor.Clear();
        _underCursor.UnionWith(hovered);
        int[] remaining = _selected.Where(item => item != index)
            .Select(item => item > index ? item - 1 : item).ToArray();
        _selected.Clear();
        _selected.UnionWith(remaining);
        ItemCount--;
        scroll?.DeleteItem();
    }

    public void Clear(PangyaUiScrollState? scroll = null)
    {
        ValidateScroll(scroll);
        ItemCount = 0;
        _selected.Clear();
        Current = null;
        Previous = null;
        ItemUnderCursor = null;
        _underCursor.Clear();
        scroll?.Clear();
    }

    private void ValidateScroll(PangyaUiScrollState? scroll)
    {
        if (scroll is not null && scroll.ItemCount != ItemCount)
            throw new ArgumentException("The scrollbar and list must contain the same item count.", nameof(scroll));
    }

    public IReadOnlyList<int> SortItems(Comparison<int> comparison)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        // LINQ's stable ordering matches std::list::sort for equivalent items.
        int[] order = Enumerable.Range(0, ItemCount)
            .OrderBy(index => index, Comparer<int>.Create(comparison)).ToArray();
        ReorderItems(order);
        return Array.AsReadOnly(order);
    }

    public void ReorderItems(IReadOnlyList<int> order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Count != ItemCount)
            throw new ArgumentException("The order must contain every item exactly once.", nameof(order));
        var positions = new int[ItemCount];
        var seen = new HashSet<int>();
        for (int position = 0; position < order.Count; position++)
        {
            int oldIndex = order[position];
            if (oldIndex < 0 || oldIndex >= ItemCount || !seen.Add(oldIndex))
                throw new ArgumentException("The order must contain every item exactly once.", nameof(order));
            positions[oldIndex] = position;
        }
        int[] selected = _selected.Select(index => positions[index]).ToArray();
        int[] hovered = _underCursor.Select(index => positions[index]).ToArray();
        if (Current is int current) Current = positions[current];
        if (Previous is int previous) Previous = positions[previous];
        if (ItemUnderCursor is int cursor) ItemUnderCursor = positions[cursor];
        _selected.Clear();
        _selected.UnionWith(selected);
        _underCursor.Clear();
        _underCursor.UnionWith(hovered);
    }

    public void SetItemUnderCursor(int? index)
    {
        if (index is int item) Validate(item);
        _underCursor.Clear();
        if (index is int hovered) _underCursor.Add(hovered);
        ItemUnderCursor = index;
    }

    public void SetCursorToFirstItem()
    {
        if (ItemCount == 0) return;
        ItemUnderCursor = 0;
        _underCursor.Add(0);
    }

    public void SetCursorToLastItem()
    {
        if (ItemCount == 0) return;
        ItemUnderCursor = ItemCount - 1;
        _underCursor.Add(ItemCount - 1);
    }

    public bool NavigateCursor(PangyaUiListNavigation direction, int columnCapacity = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(columnCapacity);
        if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction));
        if (ItemCount == 0) return false;
        if (ItemUnderCursor is not int current)
        {
            SetCursorToFirstItem();
            return true;
        }
        long target = direction switch
        {
            PangyaUiListNavigation.Previous => (long)current - 1,
            PangyaUiListNavigation.Next => (long)current + 1,
            PangyaUiListNavigation.RowUp => (long)current - columnCapacity,
            _ => (long)current + columnCapacity
        };
        if (target < 0 || target >= ItemCount || target == current) return false;
        _underCursor.Remove(current);
        _underCursor.Add((int)target);
        ItemUnderCursor = (int)target;
        return true;
    }

    public void SelectItem(int? index, bool makeCurrent = true)
    {
        if (index is int item) { Validate(item); _selected.Add(item); }
        if (!MultiSelect && Current is int old && Current != index) _selected.Remove(old);
        if (makeCurrent) { Previous = Current; Current = index; }
    }

    public void UnselectItem(int index)
    {
        Validate(index);
        _selected.Remove(index);
        if (Current == index) { Current = null; Previous = null; }
    }

    public void SelectItem(int? index, PangyaUiScrollState scroll, bool makeCurrent = true)
    {
        ArgumentNullException.ThrowIfNull(scroll);
        SelectItem(index, makeCurrent);
        if (!makeCurrent || index is not int item) return;
        long columns = scroll.ColumnCapacity;
        long halfPage = (long)scroll.RowCapacity * columns / 2;
        if (halfPage == 0) return;
        long first = Math.Min(ItemCount, (long)scroll.RoundedTopRow * columns);
        long bottom = Math.Min(ItemCount, first + Math.Max(1L, scroll.RowCapacity) * columns);
        if (item < first)
        {
            long count = (first - item - 1 + halfPage) / halfPage;
            ScrollRepeated(scroll, count, down: false);
        }
        else if (item >= bottom)
        {
            long count = (item - first - halfPage) / halfPage;
            ScrollRepeated(scroll, count, down: true);
        }
    }

    private static void ScrollRepeated(PangyaUiScrollState scroll, long count, bool down)
    {
        if (count <= 0) return;
        // The first operation performs Fresh's fractional-position recalculation.
        if (down) scroll.ScrollDown(); else scroll.ScrollUp();
        int step = scroll.RowCapacity / 2;
        if (count == 1 || step == 0) return;
        int remaining = (int)Math.Min(int.MaxValue, (count - 1) * step);
        if (down) scroll.ScrollDown(remaining); else scroll.ScrollUp(remaining);
    }

    public void UnselectAll()
    {
        _selected.Clear();
        if (Current is not null) { Current = null; Previous = null; }
    }

    public void ToggleItem(int index)
    {
        Validate(index);
        if (_selected.Contains(index)) UnselectItem(index);
        else SelectItem(index);
    }

    public void ToggleItem(int index, PangyaUiScrollState scroll)
    {
        ArgumentNullException.ThrowIfNull(scroll);
        Validate(index);
        if (_selected.Contains(index)) UnselectItem(index);
        else SelectItem(index, scroll);
    }

    private void Validate(int index)
    {
        if (index < 0 || index >= ItemCount) throw new ArgumentOutOfRangeException(nameof(index));
    }

    public PangyaUiListPreview ApplyTo(PangyaUiListPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return preview with { ItemCount = ItemCount, SelectedItems = _selected.ToHashSet(),
            ItemUnderCursor = ItemUnderCursor, UnderCursorItems = _underCursor.ToHashSet(),
            MouseEventsEnabled = MouseEventsEnabled };
    }
}
