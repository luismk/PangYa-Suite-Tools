using System.Text.RegularExpressions;

namespace PangyaAPI.IFF;

public static class IffRegionDetector
{
    public static string? FromFileName(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        string stem = Path.GetFileNameWithoutExtension(fileName);
        foreach (string token in Regex.Split(stem, "[^A-Za-z0-9]+"))
        {
            if (token.Equals("TH", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Thailand", StringComparison.OrdinalIgnoreCase)) return "TH";
            if (token.Equals("JP", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("JP2W", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Japan", StringComparison.OrdinalIgnoreCase)) return "JP";
            if (token.Equals("GB", StringComparison.OrdinalIgnoreCase) ||
                token.Equals("Global", StringComparison.OrdinalIgnoreCase)) return "Global";
        }
        return null;
    }
}
