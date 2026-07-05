using PangyaAPI.IFF;
using Xunit;

namespace PangyaAPI.Tests;

public sealed class JsonSchemaTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "pangya-json-schemas", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Json_RoundTripsEveryFieldType()
    {
        IffFieldDefinition[] fields = Enum.GetValues<IffFieldType>().Select((type, index) => Definition(type, index * 32)).ToArray();
        var definition = new IffSchemaDefinition(1, "AllTypes.iff", "TH", 512, true, fields);

        IffSchemaDefinition result = IffSchemaJson.Deserialize(IffSchemaJson.Serialize(definition));

        Assert.Equal(Enum.GetValues<IffFieldType>(), result.Fields.Select(field => field.Type));
        Assert.Equal(definition.FileName, result.FileName);
        Assert.Equal(definition.Region, result.Region);
        Assert.Equal(definition.Fields.Select(field => field.Name), result.Fields.Select(field => field.Name));
    }

    [Fact]
    public void Json_RoundTripsStringDefaultVisibilityAndFieldOrder()
    {
        var definition = new IffSchemaDefinition(1, "Data.iff", "JP", 16, true,
        [
            new("Second", 4, 4, IffFieldType.FixedString, IsVisible: false),
            new("First", 0, 4, IffFieldType.UInt32, IsVisible: true)
        ], DefaultStringSize: 7);

        IffSchemaDefinition result = IffSchemaJson.Deserialize(IffSchemaJson.Serialize(definition));

        Assert.Equal(7, result.DefaultStringSize);
        Assert.Equal(["Second", "First"], result.Fields.Select(field => field.Name));
        Assert.False(result.Fields[0].IsVisible);
        Assert.True(result.Fields[1].IsVisible);
    }

    [Fact]
    public void LegacyJson_DefaultsVisibilityAndStringSize()
    {
        const string json = """
            { "schemaVersion": 1, "fileName": "Data.iff", "region": "*",
              "minimumRecordSize": 4, "isEditable": true, "fields": [
                { "name": "Value", "offset": 0, "width": 4, "type": "UInt32" },
                { "name": "Raw record", "offset": 0, "width": 4, "type": "Raw" }
              ] }
            """;

        IffSchema schema = IffSchemaJson.ToSchema(IffSchemaJson.Deserialize(json), 4);

        Assert.Equal(4, schema.DefaultStringSize);
        Assert.True(schema.Fields[0].IsVisible);
        Assert.False(schema.Fields[1].IsVisible);
    }

    [Fact]
    public void DirectoryProvider_PrefersRegionThenUsesDefaultFallback()
    {
        Directory.CreateDirectory(_directory);
        var provider = new DirectoryIffSchemaProvider(_directory);
        provider.Save(Schema("Item.iff", "*", "Default"));
        provider.Save(Schema("Item.iff", "TH", "Thailand"));

        Assert.Equal("Thailand", Assert.Single(provider.Resolve("Item.iff", "TH", 8).Schema!.Fields).Name);
        Assert.Equal("Default", Assert.Single(provider.Resolve("Item.iff", "JP", 8).Schema!.Fields).Name);
    }

    [Fact]
    public void DirectoryProvider_UsesEmbeddedFallbackWhenUserSchemaIsMissing()
    {
        var provider = new DirectoryIffSchemaProvider(_directory, new EmbeddedIffSchemaProvider());

        IffSchemaResolution result = provider.Resolve("Character.iff", "TH", 628);

        Assert.NotNull(result.Schema);
        Assert.Equal(40, result.Schema.DefaultStringSize);
        Assert.Contains(result.Schema.Fields, field => field.Name == "ItemId");
    }

    [Fact]
    public void InvalidDefinitions_AreRejected()
    {
        IffSchemaDefinition valid = Schema("Item.iff", "TH", "Value");
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with { Fields = [valid.Fields[0], valid.Fields[0]] }, 8));
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with { Fields = [valid.Fields[0] with { Offset = 8 }] }, 8));
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with { Fields = [new("Flag", 0, 4, IffFieldType.BooleanBitField, BitMask: 3)] }, 8));
    }

    [Fact]
    public async Task InvalidJson_ProducesWarningAndReaderUsesReadOnlyRawSchema()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "Unknown.TH.json"), "{ invalid }");
        byte[] bytes = [1, 0, 0, 0, 11, 0, 0, 0, 1, 2, 3, 4];
        await using var stream = new MemoryStream(bytes);

        await using IffReader reader = IffReader.Open(stream, "Unknown.iff",
            new(SchemaProvider: new DirectoryIffSchemaProvider(_directory)));

        Assert.NotNull(reader.Info.SchemaWarning);
        Assert.False(reader.Info.Schema!.IsEditable);
        IffField raw = Assert.Single(reader.Info.Schema.Fields);
        Assert.Equal(IffFieldType.Raw, raw.Type);
        Assert.Equal(4, raw.Width);
    }

    [Fact]
    public async Task SelectedOrFilenameRegion_DeterminesSchemaInsteadOfHeader()
    {
        Directory.CreateDirectory(_directory);
        var provider = new DirectoryIffSchemaProvider(_directory);
        provider.Save(new IffSchemaDefinition(1, "Data.iff", "JP", 4, true,
            [new("Japanese", 0, 4, IffFieldType.UInt32)]));
        provider.Save(new IffSchemaDefinition(1, "Data.iff", "TH", 4, true,
            [new("Thailand", 0, 4, IffFieldType.UInt32)]));

        await using var unknownStream = new MemoryStream(IffBytes(revision: 1, magic: 1));
        await using IffReader selected = IffReader.Open(unknownStream, "Data.iff",
            new(SchemaProvider: provider, SchemaRegion: "JP"));
        Assert.Equal("JP", selected.Info.Region);
        Assert.Equal("Japanese", Assert.Single(selected.Info.Schema!.Fields).Name);

        await using var detectedStream = new MemoryStream(IffBytes(revision: 0, magic: 11));
        await using IffReader detected = IffReader.Open(detectedStream, "Data.iff",
            new(SchemaProvider: provider, SchemaRegion: "JP"));
        Assert.Equal("JP", detected.Info.Region);
        Assert.Equal("Japanese", Assert.Single(detected.Info.Schema!.Fields).Name);
    }

    [Fact]
    public void EmbeddedDefaults_SeedMissingFilesWithoutOverwritingUserEdits()
    {
        var embedded = new EmbeddedIffSchemaProvider();
        embedded.SeedDirectory(_directory);
        string item = Path.Combine(_directory, "Item.TH.json");
        Assert.True(File.Exists(item));
        File.WriteAllText(item, "user-owned");

        embedded.SeedDirectory(_directory);

        Assert.Equal("user-owned", File.ReadAllText(item));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
    }

    private static IffSchemaDefinition Schema(string file, string region, string field) =>
        new(1, file, region, 8, true, [new(field, 0, 4, IffFieldType.UInt32)]);

    private static IffFieldDefinition Definition(IffFieldType type, int offset)
    {
        int width = type switch
        {
            IffFieldType.Boolean or IffFieldType.Byte => 1,
            IffFieldType.UInt16 or IffFieldType.Int16 => 2,
            IffFieldType.UInt32 or IffFieldType.Int32 or IffFieldType.Single => 4,
            IffFieldType.DateTime => 16,
            IffFieldType.BitField or IffFieldType.BooleanBitField => 4,
            _ => 8
        };
        uint? mask = type switch
        {
            IffFieldType.BitField => 0x0Fu,
            IffFieldType.BooleanBitField => 0x01u,
            _ => null
        };
        return new IffFieldDefinition(type.ToString(), offset, width, type, BitMask: mask);
    }

    private static byte[] IffBytes(ushort revision, byte magic) =>
        [1, 0, (byte)revision, (byte)(revision >> 8), magic, 0, 0, 0, 1, 2, 3, 4];
}
