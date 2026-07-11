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
        ], DefaultStringSize: 7, DefaultLongStringSize: 321);

        IffSchemaDefinition result = IffSchemaJson.Deserialize(IffSchemaJson.Serialize(definition));

        Assert.Equal(7, result.DefaultStringSize);
        Assert.Equal(321, result.DefaultLongStringSize);
        Assert.Equal(["Second", "First"], result.Fields.Select(field => field.Name));
        Assert.False(result.Fields[0].IsVisible);
        Assert.True(result.Fields[1].IsVisible);
    }

    [Fact]
    public void Json_RoundTripsOptionalFormMetadata()
    {
        var definition = new IffSchemaDefinition(1, "Data.iff", "JP", 16, true,
        [
            new("Name", 0, 8, IffFieldType.FixedString),
            new("Active", 8, 1, IffFieldType.Boolean)
        ], DefaultStringSize: 8, Ui: new IffSchemaUiDefinition(
        [
            new IffFormTabDefinition("Basic Info",
            [
                new IffFormFieldDefinition("Name", "Display Name", "text", Order: 1),
                new IffFormFieldDefinition("Active", "Active", "checkbox", Order: 2)
            ])
        ]));

        IffSchema schema = IffSchemaJson.ToSchema(IffSchemaJson.Deserialize(IffSchemaJson.Serialize(definition)), 16);

        IffFormTabDefinition tab = Assert.Single(schema.Ui!.Tabs);
        Assert.Equal("Basic Info", tab.Name);
        Assert.Equal(["Name", "Active"], tab.Fields.Select(field => field.Field));
        Assert.Equal("Display Name", tab.Fields[0].Label);
    }

    [Fact]
    public void Json_RoundTripsOptionalReferenceMetadata()
    {
        var definition = new IffSchemaDefinition(1, "SetItem.iff", "TH", 8, true,
        [
            new("Item1", 0, 4, IffFieldType.ItemIdReference,
                Reference: new IffFieldReferenceDefinition("Item.iff", "ItemId", "Name", "Icon",
                    PickerEnabled: true)),
            new("Icon", 4, 16, IffFieldType.Icon, IconPath: "ui/shop_myroom"),
            new("Sound", 20, 16, IffFieldType.Sound, SoundPath: "sound/effect"),
            new("Count", 36, 2, IffFieldType.UInt16)
        ]);

        IffSchema schema = IffSchemaJson.ToSchema(IffSchemaJson.Deserialize(IffSchemaJson.Serialize(definition)), 40);

        Assert.Equal(IffFieldType.ItemIdReference, schema.Fields[0].Type);
        IffFieldReference reference = schema.Fields[0].Reference!;
        Assert.Equal("Item.iff", reference.TargetFile);
        Assert.Equal("ItemId", reference.TargetKeyField);
        Assert.Equal("Name", reference.DisplayField);
        Assert.Equal("Icon", reference.IconField);
        Assert.True(reference.PickerEnabled);
        Assert.Equal(IffFieldType.Icon, schema.Fields[1].Type);
        Assert.Equal("ui/shop_myroom", schema.Fields[1].IconPath);
        Assert.Equal(IffFieldType.Sound, schema.Fields[2].Type);
        Assert.Equal("sound/effect", schema.Fields[2].SoundPath);
        Assert.Null(schema.Fields[3].Reference);
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
        Assert.Equal(512, schema.DefaultLongStringSize);
        Assert.Null(schema.Ui);
        Assert.True(schema.Fields[0].IsVisible);
        Assert.False(schema.Fields[1].IsVisible);
        Assert.Null(schema.Fields[0].Reference);
    }

    [Fact]
    public void EmbeddedPartSchema_ProvidesFormTabs()
    {
        IffSchema schema = new EmbeddedIffSchemaProvider().Resolve("Part.iff", "TH", 512).Schema!;

        Assert.Equal(["Basic Info", "TikiShop", "Part", "Desc Info", "Ability Info"],
            schema.Ui!.Tabs.Select(tab => tab.Name));
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
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with { Fields = [new("Reference", 0, 4, IffFieldType.ItemIdReference)] }, 8));
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with
            {
                Fields =
                [
                    new("Icon", 0, 4, IffFieldType.Icon, IconPath: @"..\ui")
                ]
            }, 8));
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with
            {
                Fields =
                [
                    new("Icon", 0, 4, IffFieldType.Icon, IconPath: Path.GetFullPath("ui"))
                ]
            }, 8));
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with
            {
                Fields =
                [
                    new("Name", 0, 4, IffFieldType.FixedString, IconPath: "ui")
                ]
            }, 8));
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with
            {
                Fields =
                [
                    new("Sound", 0, 4, IffFieldType.Sound, SoundPath: @"..\sound")
                ]
            }, 8));
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with
            {
                Fields =
                [
                    new("Sound", 0, 4, IffFieldType.Sound, SoundPath: Path.GetFullPath("sound"))
                ]
            }, 8));
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(
            valid with
            {
                Fields =
                [
                    new("Name", 0, 4, IffFieldType.FixedString, SoundPath: "sound")
                ]
            }, 8));
    }

    [Fact]
    public void BitFields_CanOccupyOneToFourBytes()
    {
        IffSchemaDefinition valid = Schema("Item.iff", "TH", "Value");

        foreach (int width in new[] { 1, 2, 3, 4 })
        {
            IffSchemaJson.ValidateDefinition(valid with
            {
                Fields = [new($"Bits{width}", 0, width, IffFieldType.BitField, BitMask: 1)]
            }, 8);
        }

        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(valid with
        {
            Fields = [new("TooWide", 0, 5, IffFieldType.BitField, BitMask: 1)]
        }, 8));
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
    public void HeaderSchemaCandidates_PreferExactThenFamilyAndHeaderOverFilename()
    {
        Directory.CreateDirectory(_directory);
        var provider = new DirectoryIffSchemaProvider(_directory);
        provider.Save(new IffSchemaDefinition(1, "Data.JP.iff", "Global", 4, true,
            [new("Family", 0, 4, IffFieldType.UInt32)]));
        provider.Save(new IffSchemaDefinition(1, "Data.JP.iff", "Global_30447", 4, true,
            [new("Exact", 0, 4, IffFieldType.UInt32)]));
        provider.Save(new IffSchemaDefinition(1, "Data.JP.iff", "JP", 4, true,
            [new("Filename", 0, 4, IffFieldType.UInt32)]));

        using (var stream = new MemoryStream(IffBytes(30447, 11)))
        using (IffReader exact = IffReader.Open(stream, "Data.JP.iff", new(SchemaProvider: provider)))
        {
            Assert.Equal("Global_30447", exact.Info.Region);
            Assert.Equal("Exact", Assert.Single(exact.Info.Schema!.Fields).Name);
            Assert.Equal(40, exact.Info.Schema.DefaultStringSize);
            Assert.Equal(512, exact.Info.Schema.DefaultLongStringSize);
        }

        File.Delete(provider.GetSchemaPath("Data.JP.iff", "Global_30447"));
        using var familyStream = new MemoryStream(IffBytes(30447, 11));
        using IffReader family = IffReader.Open(familyStream, "Data.JP.iff", new(SchemaProvider: provider));
        Assert.Equal("Family", Assert.Single(family.Info.Schema!.Fields).Name);
    }

    [Fact]
    public void UnknownHeader_UsesFilenameButKnownGlobalWithoutSchemaUsesRawFallback()
    {
        Directory.CreateDirectory(_directory);
        var provider = new DirectoryIffSchemaProvider(_directory);
        provider.Save(new IffSchemaDefinition(1, "Data.JP.iff", "JP", 4, true,
            [new("Japanese", 0, 4, IffFieldType.UInt32)]));

        using (var stream = new MemoryStream(IffBytes(999, 11)))
        using (IffReader filename = IffReader.Open(stream, "Data.JP.iff", new(SchemaProvider: provider)))
            Assert.Equal("Japanese", Assert.Single(filename.Info.Schema!.Fields).Name);

        using var globalStream = new MemoryStream(IffBytes(30447, 11));
        using IffReader global = IffReader.Open(globalStream, "Data.JP.iff", new(SchemaProvider: provider));
        Assert.Equal("Global_30447", global.Info.Region);
        Assert.False(global.Info.Schema!.IsEditable);
        Assert.Equal(IffFieldType.Raw, Assert.Single(global.Info.Schema.Fields).Type);
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
            IffFieldType.UInt32 or IffFieldType.ItemIdReference or IffFieldType.Int32 or IffFieldType.Single => 4,
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
