using System.Globalization;
using System.Text;

namespace PangYa_Suite_Tools.Configuration;

internal static class IffStringEncodingPreferences
{
    internal const int DefaultCodePage = PakFilenameEncodingPreferences.DefaultCodePage;
    internal static string? PreferencePathOverride { get; set; }

    private static string PreferencePath => PreferencePathOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PangYa-Suite-Tools", "iff-string-encoding.txt");

    internal static IReadOnlyList<PakEncodingOption> GetAvailableEncodings() =>
        PakFilenameEncodingPreferences.GetAvailableEncodings();

    internal static Encoding GetEncoding(int codePage) =>
        PakFilenameEncodingPreferences.GetEncoding(codePage);

    internal static int LoadCodePage()
    {
        try
        {
            string value = File.Exists(PreferencePath) ? File.ReadAllText(PreferencePath).Trim() : string.Empty;
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int codePage) &&
                   GetAvailableEncodings().Any(option => option.CodePage == codePage)
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
        if (GetAvailableEncodings().All(option => option.CodePage != codePage))
            codePage = DefaultCodePage;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath)!);
            File.WriteAllText(PreferencePath, codePage.ToString(CultureInfo.InvariantCulture));
        }
        catch
        {
            // A preference must never prevent IFF operations.
        }
    }
}
