using PangyaAPI.IFF;

namespace PangYa_Suite_Tools.Configuration;

internal static class IffSchemaPreferences
{
    internal static string? SchemaDirectoryOverride { get; set; }

    internal static string SchemaDirectory => SchemaDirectoryOverride ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PangYa-Suite-Tools", "schemas");

    internal static DirectoryIffSchemaProvider CreateProvider()
        => new(SchemaDirectory, new EmbeddedIffSchemaProvider());

    internal static void SeedDefaults() => new EmbeddedIffSchemaProvider().SeedDirectory(SchemaDirectory);

    internal static IReadOnlyList<IffSchemaDefinition> LoadTemplateSchemas()
    {
        var schemas = new Dictionary<string, IffSchemaDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (IffSchemaDefinition definition in new EmbeddedIffSchemaProvider().LoadDefinitions())
            schemas[$"{definition.FileName}|{definition.Region}"] = definition;
        foreach (IffSchemaDefinition definition in new DirectoryIffSchemaProvider(SchemaDirectory).LoadDefinitions())
            schemas[$"{definition.FileName}|{definition.Region}"] = definition;
        return schemas.Values.OrderBy(item => item.FileName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Region, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}
