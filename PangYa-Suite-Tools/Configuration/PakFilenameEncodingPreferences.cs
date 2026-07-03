using System.Globalization;
using System.Text;

namespace PangYa_Suite_Tools.Configuration;

internal sealed record PakEncodingOption(int CodePage, string Name, string DisplayName)
{
    public string Label => $"{DisplayName} ({Name} — {CodePage})";
}

internal static class PakFilenameEncodingPreferences
{
    internal const int DefaultCodePage = 51949;
    internal static string? PreferencePathOverride { get; set; }

    private static string PreferencePath => PreferencePathOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PangYa-Suite-Tools", "filename-encoding.txt");

    static PakFilenameEncodingPreferences() =>
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    internal static IReadOnlyList<PakEncodingOption> GetAvailableEncodings()
    {
        List<PakEncodingOption> encodings = Encoding.GetEncodings()
            .Select(info => new PakEncodingOption(info.CodePage, info.Name, info.DisplayName))
            .ToList();
        if (encodings.All(info => info.CodePage != DefaultCodePage))
        {
            Encoding defaultEncoding = Encoding.GetEncoding(DefaultCodePage);
            encodings.Add(new PakEncodingOption(
                defaultEncoding.CodePage, defaultEncoding.WebName, defaultEncoding.EncodingName));
        }
        return encodings
            .OrderBy(info => info.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(info => info.CodePage)
            .ToList();
    }

    internal static int LoadCodePage()
    {
        try
        {
            string value = File.Exists(PreferencePath)
                ? File.ReadAllText(PreferencePath).Trim()
                : string.Empty;
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int codePage) &&
                   IsAvailable(codePage)
                ? codePage
                : DefaultCodePage;
        }
        catch
        {
            return DefaultCodePage;
        }
    }

    internal static void SaveCodePage(int codePage)
    {
        if (!IsAvailable(codePage)) codePage = DefaultCodePage;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath)!);
            File.WriteAllText(PreferencePath, codePage.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            // A preference must never prevent PAK operations.
        }
    }

    internal static Encoding GetEncoding(int codePage) =>
        Encoding.GetEncoding(IsAvailable(codePage) ? codePage : DefaultCodePage);

    private static bool IsAvailable(int codePage)
    {
        try
        {
            _ = Encoding.GetEncoding(codePage);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
