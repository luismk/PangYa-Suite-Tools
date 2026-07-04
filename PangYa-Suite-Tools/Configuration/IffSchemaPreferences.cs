using PangyaAPI.IFF;

namespace PangYa_Suite_Tools.Configuration;

internal static class IffSchemaPreferences
{
    internal static string? SchemaDirectoryOverride { get; set; }

    internal static string SchemaDirectory => SchemaDirectoryOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PangYa-Suite-Tools", "schemas");

    internal static DirectoryIffSchemaProvider CreateProvider()
        => new(SchemaDirectory);

    internal static void SeedDefaults() => new EmbeddedIffSchemaProvider().SeedDirectory(SchemaDirectory);
}
