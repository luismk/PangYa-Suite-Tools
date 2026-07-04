using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PangyaAPI.IFF;

public sealed record IffSchemaResolution(IffSchema? Schema, string? Warning = null);

public interface IIffSchemaProvider
{
    IffSchemaResolution Resolve(string fileName, string region, int recordSize);
}

public static class IffSchemaRegistry
{
    private static readonly IIffSchemaProvider EmbeddedProvider = new EmbeddedIffSchemaProvider();

    public static IffSchema? Resolve(string fileName, IffHeader header, int recordSize)
    {
        IffSchemaResolution resolution = ResolveDetailed(fileName, header, recordSize);
        if (resolution.Schema is not null) return resolution.Schema;
        return new IffSchema(Path.GetFileNameWithoutExtension(fileName), recordSize,
            [new IffField("Raw record", 0, recordSize, IffFieldType.Raw, false)], false);
    }

    public static IffSchemaResolution ResolveDetailed(string fileName, IffHeader header, int recordSize,
        IIffSchemaProvider? provider = null) =>
        (provider ?? EmbeddedProvider).Resolve(fileName, header.Region, recordSize);
}

public sealed record IffSchemaDefinition(
    int SchemaVersion,
    string FileName,
    string Region,
    int MinimumRecordSize,
    bool IsEditable,
    IReadOnlyList<IffFieldDefinition> Fields);

public sealed record IffFieldDefinition(
    string Name,
    int Offset,
    int Width,
    IffFieldType Type,
    bool IsEditable = true,
    int? EncodingCodePage = null,
    long? Minimum = null,
    long? Maximum = null,
    uint? BitMask = null,
    int BitShift = 0);

public static class IffSchemaJson
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    static IffSchemaJson() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public static string Serialize(IffSchemaDefinition definition) =>
        JsonSerializer.Serialize(definition, Options);

    public static IffSchemaDefinition Deserialize(string json) =>
        JsonSerializer.Deserialize<IffSchemaDefinition>(json, Options)
        ?? throw new InvalidDataException("The IFF schema JSON is empty.");

    public static IffSchemaDefinition FromSchema(string fileName, string region, IffSchema schema) =>
        new(CurrentVersion, Path.GetFileName(fileName), region, schema.MinimumRecordSize, schema.IsEditable,
            schema.Fields.Select(field => new IffFieldDefinition(
                field.Name, field.Offset, field.Width, field.Type, field.IsEditable,
                field.Encoding?.CodePage, field.Minimum, field.Maximum, field.BitMask, field.BitShift)).ToArray());

    public static IffSchema ToSchema(IffSchemaDefinition definition, int recordSize)
    {
        ValidateDefinition(definition, recordSize);
        IffField[] fields = definition.Fields.Select(field => new IffField(
            field.Name, field.Offset, field.Width, field.Type, field.IsEditable,
            field.EncodingCodePage is int codePage ? Encoding.GetEncoding(codePage) : null,
            field.Minimum, field.Maximum, field.BitMask, field.BitShift)).ToArray();
        return new IffSchema(Path.GetFileNameWithoutExtension(definition.FileName),
            definition.MinimumRecordSize, fields, definition.IsEditable);
    }

    public static void ValidateDefinition(IffSchemaDefinition definition, int recordSize)
    {
        if (definition.SchemaVersion != CurrentVersion)
            throw new InvalidDataException($"Unsupported IFF schema version {definition.SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(definition.FileName) || Path.GetFileName(definition.FileName) != definition.FileName)
            throw new InvalidDataException("An IFF schema must contain a filename without a directory path.");
        if (string.IsNullOrWhiteSpace(definition.Region))
            throw new InvalidDataException("An IFF schema must contain a region or '*'.");
        if (definition.MinimumRecordSize <= 0 || recordSize < definition.MinimumRecordSize)
            throw new InvalidDataException($"The schema requires records of at least {definition.MinimumRecordSize} bytes.");
        if (definition.Fields is null || definition.Fields.Count == 0)
            throw new InvalidDataException("An IFF schema must contain at least one field.");

        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (IffFieldDefinition field in definition.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Name) || !names.Add(field.Name))
                throw new InvalidDataException($"IFF schema field names must be non-empty and unique: '{field.Name}'.");
            if (field.Offset < 0 || field.Width <= 0 || field.Offset > recordSize - field.Width)
                throw new InvalidDataException($"Field '{field.Name}' exceeds the {recordSize}-byte record.");
            ValidateFieldShape(field);
            if (field.EncodingCodePage is int codePage)
                _ = Encoding.GetEncoding(codePage);
        }
    }

    private static void ValidateFieldShape(IffFieldDefinition field)
    {
        int expectedWidth = field.Type switch
        {
            IffFieldType.Boolean or IffFieldType.Byte => 1,
            IffFieldType.UInt16 or IffFieldType.Int16 => 2,
            IffFieldType.UInt32 or IffFieldType.Int32 or IffFieldType.Single => 4,
            IffFieldType.DateTime => 16,
            _ => 0
        };
        if (expectedWidth != 0 && field.Width != expectedWidth)
            throw new InvalidDataException($"Field '{field.Name}' must occupy {expectedWidth} bytes.");
        if (field.Minimum is long minimum && field.Maximum is long maximum && minimum > maximum)
            throw new InvalidDataException($"Field '{field.Name}' has an invalid numeric range.");
        if (field.Type is IffFieldType.BitField or IffFieldType.BooleanBitField)
        {
            uint widthMask = field.Width switch { 1 => byte.MaxValue, 2 => ushort.MaxValue, 4 => uint.MaxValue, _ => 0 };
            if (widthMask == 0 || field.BitMask is not uint mask || mask == 0 || (mask & ~widthMask) != 0 ||
                field.BitShift is < 0 or > 31 || (mask >> field.BitShift) == 0 ||
                field.Type == IffFieldType.BooleanBitField && (mask & (mask - 1)) != 0)
                throw new InvalidDataException($"Field '{field.Name}' has an invalid bit mask.");
        }
        if (field.Type == IffFieldType.BitField && field.Width != sizeof(uint))
            throw new InvalidDataException($"Bit field '{field.Name}' must occupy four bytes.");
        if (field.Type == IffFieldType.ZeroBoolean && field.Width is not (1 or 2 or 4))
            throw new InvalidDataException($"Zero-boolean field '{field.Name}' must occupy one, two, or four bytes.");
    }
}

public sealed class DirectoryIffSchemaProvider(string directoryPath) : IIffSchemaProvider
{
    public string DirectoryPath { get; } = Path.GetFullPath(directoryPath);

    public IffSchemaResolution Resolve(string fileName, string region, int recordSize)
    {
        fileName = Path.GetFileName(fileName);
        foreach (string candidate in CandidatePaths(fileName, region))
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                IffSchemaDefinition definition = IffSchemaJson.Deserialize(File.ReadAllText(candidate));
                if (!definition.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
                    !(definition.Region.Equals(region, StringComparison.OrdinalIgnoreCase) || definition.Region == "*"))
                    throw new InvalidDataException("The schema filename or region does not match its JSON content.");
                return new IffSchemaResolution(IffSchemaJson.ToSchema(definition, recordSize));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or InvalidDataException
                or ArgumentException or NotSupportedException)
            {
                return new IffSchemaResolution(null, $"Could not load IFF schema '{candidate}': {ex.Message}");
            }
        }
        return new IffSchemaResolution(null, $"No JSON schema is defined for {fileName} ({region}).");
    }

    public string GetSchemaPath(string fileName, string region) =>
        Path.Combine(DirectoryPath, $"{Path.GetFileNameWithoutExtension(fileName)}.{NormalizeRegion(region)}.json");

    public void Save(IffSchemaDefinition definition)
    {
        IffSchemaJson.ValidateDefinition(definition, definition.MinimumRecordSize);
        Directory.CreateDirectory(DirectoryPath);
        string destination = GetSchemaPath(definition.FileName, definition.Region);
        string temporary = destination + ".tmp." + Guid.NewGuid().ToString("N");
        try
        {
            File.WriteAllText(temporary, IffSchemaJson.Serialize(definition));
            File.Move(temporary, destination, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private IEnumerable<string> CandidatePaths(string fileName, string region)
    {
        yield return GetSchemaPath(fileName, region);
        yield return GetSchemaPath(fileName, "*");
    }

    private static string NormalizeRegion(string region) => region == "*" ? "default" : region.ToUpperInvariant();
}

public sealed class EmbeddedIffSchemaProvider(Assembly? assembly = null, string resourcePrefix = "PangyaAPI.IFF.Schemas.Defaults") : IIffSchemaProvider
{
    private readonly Assembly _assembly = assembly ?? typeof(EmbeddedIffSchemaProvider).Assembly;

    public IffSchemaResolution Resolve(string fileName, string region, int recordSize)
    {
        fileName = Path.GetFileName(fileName);
        foreach (string suffix in CandidateSuffixes(fileName, region))
        {
            string? resource = _assembly.GetManifestResourceNames()
                .FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (resource is null) continue;
            try
            {
                using Stream stream = _assembly.GetManifestResourceStream(resource)!;
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return new IffSchemaResolution(IffSchemaJson.ToSchema(IffSchemaJson.Deserialize(reader.ReadToEnd()), recordSize));
            }
            catch (Exception ex) when (ex is JsonException or InvalidDataException or ArgumentException or NotSupportedException)
            {
                return new IffSchemaResolution(null, $"Could not load embedded IFF schema '{resource}': {ex.Message}");
            }
        }
        return new IffSchemaResolution(null, $"No JSON schema is defined for {fileName} ({region}).");
    }

    public void SeedDirectory(string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        foreach (string resource in _assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(resourcePrefix, StringComparison.Ordinal)))
        {
            string marker = resourcePrefix + ".";
            string fileName = resource[(resource.IndexOf(marker, StringComparison.Ordinal) + marker.Length)..];
            string destination = Path.Combine(destinationDirectory, fileName);
            if (File.Exists(destination)) continue;
            using Stream source = _assembly.GetManifestResourceStream(resource)!;
            using FileStream target = new(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            source.CopyTo(target);
        }
    }

    private static IEnumerable<string> CandidateSuffixes(string fileName, string region)
    {
        string stem = Path.GetFileNameWithoutExtension(fileName);
        yield return $".{stem}.{region.ToUpperInvariant()}.json";
        yield return $".{stem}.default.json";
    }
}
