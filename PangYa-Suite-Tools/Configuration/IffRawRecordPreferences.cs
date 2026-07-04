namespace PangYa_Suite_Tools.Configuration;

internal static class IffRawRecordPreferences
{
    internal static string? PreferencePathOverride { get; set; }

    private static string PreferencePath => PreferencePathOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PangYa-Suite-Tools", "iff-show-raw-record.txt");

    internal static bool LoadShowRawRecord()
    {
        try
        {
            return File.Exists(PreferencePath) &&
                bool.TryParse(File.ReadAllText(PreferencePath).Trim(), out bool value) && value;
        }
        catch
        {
            return false;
        }
    }

    internal static void SaveShowRawRecord(bool value)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencePath)!);
            File.WriteAllText(PreferencePath, value.ToString());
        }
        catch
        {
            // A display preference must never prevent IFF operations.
        }
    }
}
