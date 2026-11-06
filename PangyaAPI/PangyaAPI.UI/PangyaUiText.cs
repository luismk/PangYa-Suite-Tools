using System.Text.RegularExpressions;

namespace PangyaAPI.UI;

public enum PangyaUiTextPrintOption
{
    ExcludeTags,
    All
}

public static class PangyaUiText
{
    internal static readonly Regex Tokens = new(@"\\c(?<tag>.*?)\\c",
        RegexOptions.CultureInvariant);

    public static string GetPrintData(string text, PangyaUiTextPrintOption option)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (option == PangyaUiTextPrintOption.All) return text;
        if (option != PangyaUiTextPrintOption.ExcludeTags)
            throw new ArgumentOutOfRangeException(nameof(option));

        return string.Join("\n", ComposeLines(text).Select(line =>
            RemoveTags(line)));
    }

    internal static string[] ComposeLines(string text)
    {
        string[] lines = text.Replace("\\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n').Split('\n');
        if (lines[^1].Length == 0) lines = lines[..^1];
        for (int index = 0; index < lines.Length; index++)
            if (lines[index].Length == 0) lines[index] = " ";
        return lines;
    }

    public static float GetLongestLineWidth(string text, Func<string, float> measureText)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(measureText);
        float longest = 0;
        foreach (string line in ComposeLines(text))
        {
            string visible = RemoveTags(line);
            longest = Math.Max(longest, measureText(visible));
        }
        return longest;
    }

    internal static string RemoveTags(string text) => Tokens.Replace(text, match =>
        IsTag(match.Groups["tag"].Value) ? string.Empty : match.Value);

    internal static uint ParseTagColor(string value)
    {
        ReadOnlySpan<char> digits = value.AsSpan().TrimStart();
        if (digits.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) digits = digits[2..];
        ulong result = 0;
        foreach (char character in digits)
        {
            int digit = character is >= '0' and <= '9' ? character - '0' :
                character is >= 'a' and <= 'f' ? character - 'a' + 10 :
                character is >= 'A' and <= 'F' ? character - 'A' + 10 : -1;
            if (digit < 0) break;
            result = Math.Min(uint.MaxValue, result * 16 + (uint)digit);
        }
        return (uint)result;
    }

    internal static bool IsTag(string value)
    {
        value = value.Trim(' ', '\\', 'c');
        if (value.Length == 1 && "nNtToOsS".Contains(value[0])) return true;
        if (value.Length < 10 || !value.StartsWith("0x", StringComparison.Ordinal)) return false;
        int comma = value.IndexOf(',');
        return comma < 0 || value.Length == 21 &&
            value.AsSpan(comma + 1).StartsWith("0x", StringComparison.Ordinal);
    }
}
