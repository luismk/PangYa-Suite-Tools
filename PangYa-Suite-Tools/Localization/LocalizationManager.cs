using System.Globalization;
namespace PangYa_Suite_Tools.Localization;

internal static class LocalizationManager
{
    public const string English = "en";
    public const string PortugueseBrazil = "pt-BR";
    public const string Swedish = "sv";
    public const string Japonese = "ja";
    public const string French = "fr";

    private static readonly HashSet<string> SupportedCultures =
        new(StringComparer.OrdinalIgnoreCase) { English, PortugueseBrazil, Swedish, Japonese, French };

    internal static int CurrentCultureIndex => CurrentCulture.Name switch
    {
        PortugueseBrazil => 0,
        Swedish => 2, 
        Japonese => 3,
        French => 4,
        _ => 1, // Default: English
    };

    internal static string? PreferencePathOverride { get; set; }
    private static string PreferencePath => PreferencePathOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PangYa-Suite-Tools", "culture.txt");

    public static CultureInfo CurrentCulture { get; private set; } = CultureInfo.GetCultureInfo(English);
    public static event EventHandler? CultureChanged;

    public static void Initialize() => ApplyCulture(ReadPreference(), persist: false);

    public static void SetCulture(string cultureName) => ApplyCulture(cultureName, persist: true);

    internal static string Normalize(string? cultureName) =>
        cultureName != null && SupportedCultures.Contains(cultureName)
            ? CultureInfo.GetCultureInfo(cultureName).Name
            : English;

    private static void ApplyCulture(string? cultureName, bool persist)
    {
        var culture = CultureInfo.GetCultureInfo(Normalize(cultureName));
        CurrentCulture = culture;
        Strings.Culture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

        if (persist)
            SavePreference(culture.Name);

        CultureChanged?.Invoke(null, EventArgs.Empty);
    }

    private static string ReadPreference()
    {
        try { return File.Exists(PreferencePath) ? File.ReadAllText(PreferencePath).Trim() : English; }
        catch { return English; }
    }

    private static void SavePreference(string cultureName)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath)!);
            File.WriteAllText(PreferencePath, cultureName);
        }
        catch
        {
            // Localization must never prevent the application from running.
        }
    }
}