namespace PangyaAPI.UI;

public sealed class PangyaUiComboList
{
    private readonly List<PangyaUiComboItem> _items = [];
    public IReadOnlyList<PangyaUiComboItem> Items => _items.AsReadOnly();
    public int SelectedIndex { get; private set; } = -1;
    public string DisplayedText { get; private set; } = string.Empty;
    public int CharacterLimit { get; }
    public int MaximumRows { get; private set; }
    public bool IsOpen { get; private set; }
    public PangyaUiComboDirection Direction { get; set; }

    public PangyaUiComboList(int characterLimit = 255, int maximumRows = 10)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(characterLimit);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumRows, 1);
        CharacterLimit = characterLimit;
        MaximumRows = maximumRows;
    }

    public void AddString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _items.Add(new(PangyaUiEditText.Limit(text, CharacterLimit)));
    }

    public void DeleteString(int index)
    {
        index = Math.Max(0, index);
        if (index >= _items.Count) return;
        _items.RemoveAt(index);
        if (SelectedIndex == index) SelectedIndex = -1;
        else if (SelectedIndex > index) SelectedIndex--;
    }

    public int FindString(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        return _items.FindIndex(item => item.Text == text) + 1;
    }

    public bool IsSelectedString(string text) =>
        SelectedIndex >= 0 && _items[SelectedIndex].Text == text;

    public bool SelectRow(int index)
    {
        if (index < 0 || index >= _items.Count) return false;
        SelectedIndex = index;
        DisplayedText = _items[index].Text;
        return true;
    }

    public bool SelectNext() => _items.Count > 0 && SelectRow(SelectedIndex + 1 >= _items.Count ? 0 : SelectedIndex + 1);

    public bool SelectPrevious() => _items.Count > 0 && SelectRow(SelectedIndex <= 0 ? _items.Count - 1 : SelectedIndex - 1);

    public void ToggleDropdown() => IsOpen = !IsOpen;

    public void LoseDropdownFocus(bool pointerInsideList, bool pointerInsideButton)
    {
        if (!pointerInsideList && !pointerInsideButton) IsOpen = false;
    }

    public bool ClickRow(int index)
    {
        bool selected = SelectRow(index);
        IsOpen = false;
        return selected;
    }

    public void ClearList()
    {
        _items.Clear();
        SelectedIndex = -1;
    }

    public void IncreaseMaximumRows(int maximumRows) => MaximumRows = Math.Max(MaximumRows, maximumRows);

    public PangyaUiRenderOptions ApplyTo(PangyaUiNode node, PangyaUiRenderOptions? options = null,
        bool? isOpen = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var combos = options.Combos?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var edits = options.EditLines?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        combos[node] = new(_items.ToArray(), isOpen ?? IsOpen, SelectedIndex, Direction: Direction)
        {
            IncreasedMaximumRows = MaximumRows,
            AutomaticOpensAbove = combos.GetValueOrDefault(node)?.AutomaticOpensAbove
        };
        edits[node] = [DisplayedText];
        return options with { Combos = combos, EditLines = edits };
    }
}
