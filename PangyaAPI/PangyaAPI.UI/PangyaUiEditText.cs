using System.Text;

namespace PangyaAPI.UI;

public sealed record PangyaUiEditLine(string Text, int Style = 0, bool Justified = false);

public sealed class PangyaUiEditText
{
    private static readonly Encoding TextEncoding = CreateEncoding();
    private readonly List<PangyaUiEditLine> _lines = [];
    public bool Multiline { get; }
    public int CharacterLimit { get; private set; }
    public uint FontColor { get; set; } = 0xff808080;
    public uint OutlineColor { get; set; } = 0xffc0c0c0;
    public IReadOnlyList<PangyaUiEditLine> Lines => _lines.AsReadOnly();

    public PangyaUiEditText(bool multiline = true, int characterLimit = 255)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(characterLimit);
        Multiline = multiline;
        CharacterLimit = characterLimit;
    }

    public void SetCharacterLimit(int limit, bool onlyShrink = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(limit);
        CharacterLimit = onlyShrink ? Math.Min(CharacterLimit, limit) : limit;
    }

    public string? AddLine(string text, uint color = 0, bool justified = false, int style = 0)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!Multiline && _lines.Count > 0) return null;
        string result = Prepare(text, color, CharacterLimit);
        _lines.Add(new(result, style, justified));
        return result;
    }

    public string? SetLine(int line, string text, uint color = 0, bool justified = false,
        int style = 0, int compositionByteCount = 0)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(compositionByteCount);
        if (_lines.Count < line)
        {
            while (_lines.Count < line && AddLine(string.Empty) is not null) { }
            line = _lines.Count;
        }
        if (_lines.Count == 0) return null;
        int index = Math.Max(0, line - 1);
        string result = Prepare(text, color, (int)Math.Min(int.MaxValue,
            (long)CharacterLimit + compositionByteCount));
        _lines[index] = new(result, style, justified);
        return result;
    }

    public string GetLine(int line, bool createMissing = false)
    {
        if (createMissing && _lines.Count < line)
        {
            while (_lines.Count < line && AddLine(string.Empty) is not null) { }
            line = _lines.Count;
        }
        int index = Math.Max(0, line - 1);
        if (index >= _lines.Count) return string.Empty;
        string text = Limit(_lines[index].Text, CharacterLimit);
        _lines[index] = _lines[index] with { Text = text };
        return text;
    }

    public void Clear() => _lines.Clear();

    public string? InsertLine(int index, string text, uint color = 0, bool justified = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= _lines.Count || !Multiline && _lines.Count > 0) return null;
        string result = Prepare(text, color, CharacterLimit);
        _lines.Insert(index, new(result, Justified: justified));
        return result;
    }

    public int DeleteLine(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        if (index >= _lines.Count) return checked(index + 1);
        _lines.RemoveAt(index);
        return index;
    }

    public void AddText(string text, float availableWidth, Func<string, float> measureText,
        bool spaceAlign = false, bool justified = true, uint color = 0, int style = 0)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(measureText);
        if (!float.IsFinite(availableWidth)) throw new ArgumentOutOfRangeException(nameof(availableWidth));
        if (text.Length == 0) return;
        string[] paragraphs = text.Split('\n');
        int count = paragraphs.Length - (paragraphs[^1].Length == 0 ? 1 : 0);
        for (int index = 0; index < count; index++)
        {
            string remaining = paragraphs[index];
            if (remaining.EndsWith('\r')) remaining = remaining[..^1];
            if (remaining.Length == 0)
            {
                AddLine(string.Empty, color, false, style);
                continue;
            }
            int step = GetStep(remaining, 0), position = step, space = -1;
            while (remaining.Length > 0)
            {
                string visible = PangyaUiText.Tokens.Replace(remaining[..position], string.Empty);
                if (visible.Length > 0 && visible[^1] is ' ' or '\t') visible = visible[..^1];
                if (measureText(visible) >= availableWidth)
                {
                    if (position <= step) break;
                    bool wordBreak = spaceAlign && space >= 0;
                    int cut = wordBreak ? space : position - step;
                    AddLine(remaining[..cut], color, justified, style);
                    bool dropSpace = remaining[cut] is ' ' or '\t';
                    int consumed = cut + (dropSpace ? 1 : 0);
                    position = wordBreak ? position - consumed : step - (dropSpace ? 1 : 0);
                    remaining = remaining[consumed..];
                    if (remaining.Length == 0) break;
                    if (wordBreak) space = -1;
                }
                else
                {
                    if (position >= remaining.Length)
                    {
                        AddLine(remaining, color, false, style);
                        break;
                    }
                    if (remaining[position] == ' ') space = position;
                    step = GetStep(remaining, position);
                    position += step;
                }
            }
        }
    }

    private static int GetStep(string text, int offset)
    {
        if (text.AsSpan(offset).StartsWith("\\c", StringComparison.Ordinal))
        {
            int end = text.IndexOf("\\c", offset + 2, StringComparison.Ordinal);
            return end < 0 ? text.Length - offset : end + 2 - offset;
        }
        return char.IsHighSurrogate(text[offset]) && offset + 1 < text.Length &&
            char.IsLowSurrogate(text[offset + 1]) ? 2 : 1;
    }

    public void DeleteFirstLine() { if (_lines.Count > 0) _lines.RemoveAt(0); }
    public void DeleteEndLine() { if (_lines.Count > 0) _lines.RemoveAt(_lines.Count - 1); }

    public PangyaUiRenderOptions ApplyTo(PangyaUiNode node, PangyaUiRenderOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(node);
        options ??= new();
        var texts = options.EditLines?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var styles = options.EditLineStyles?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        var justified = options.JustifiedEditLines?.ToDictionary(pair => pair.Key, pair => pair.Value) ?? [];
        texts[node] = _lines.Select(line => line.Text).ToArray();
        styles[node] = _lines.Select(line => line.Style).ToArray();
        justified[node] = _lines.Select((line, index) => (line, index))
            .Where(item => item.line.Justified).Select(item => item.index).ToHashSet();
        return options with { EditLines = texts, EditLineStyles = styles, JustifiedEditLines = justified };
    }

    private string Prepare(string text, uint color, int limit)
    {
        string limited = Limit(text, limit);
        return color == 0 ? limited :
            $"\\c0x{color:x8}\\c{limited}\\c0x{FontColor:x8},0x{OutlineColor:x8}\\c";
    }

    internal static string Limit(string text, int limit)
    {
        int bytes = 0, length = 0;
        foreach (Rune rune in text.EnumerateRunes())
        {
            int count = TextEncoding.GetByteCount(rune.ToString());
            if (count > limit - bytes) break;
            bytes += count;
            length += rune.Utf16SequenceLength;
        }
        return text[..length];
    }

    private static Encoding CreateEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(932);
    }
}
