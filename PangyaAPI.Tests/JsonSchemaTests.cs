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
    public void JsonV2_RoundTripsBaseReference()
    {
        var definition = new IffSchemaDefinition(2, "Character.iff", "Global", 396, true,
            [new("Model", 168, 40, IffFieldType.FixedString)], 40,
            Base: new IffSchemaBaseReference("Common"));

        IffSchemaDefinition result = IffSchemaJson.Deserialize(IffSchemaJson.Serialize(definition));

        Assert.Equal(2, result.SchemaVersion);
        Assert.Equal(new IffSchemaBaseReference("Common"), result.Base);
    }

    [Fact]
    public void DefaultRevision_RoundTripsAndSurvivesMaterialization()
    {
        var definition = new IffSchemaDefinition(2, "Data.iff", "JP", 8, true,
            [new("Value", 0, 4, IffFieldType.UInt32)], DefaultRevision: 7);

        IffSchemaDefinition roundTrip = IffSchemaJson.Deserialize(IffSchemaJson.Serialize(definition));
        IffSchema schema = IffSchemaJson.ToSchema(roundTrip, 8);
        IffSchemaDefinition restored = IffSchemaJson.FromSchema("Data.iff", "JP", schema);

        Assert.Equal(7, roundTrip.DefaultRevision);
        Assert.Equal(7, schema.DefaultRevision);
        Assert.Equal(7, restored.DefaultRevision);
    }

    [Fact]
    public void MissingDefaultRevision_IsLegacyAndNegativeRevisionIsRejected()
    {
        IffSchemaDefinition legacy = IffSchemaJson.Deserialize(
            """{"schemaVersion":1,"fileName":"Data.iff","region":"JP","minimumRecordSize":4,"isEditable":true,"fields":[{"name":"Value","offset":0,"width":4,"type":"UInt32"}],"defaultStringSize":4}""");
        IffSchemaDefinition invalid = legacy with { DefaultRevision = -1 };

        Assert.Equal(0, legacy.DefaultRevision);
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ValidateDefinition(invalid, 4));
    }

    [Fact]
    public void Composition_MarksInheritedFieldsAndKeepsLocalFields()
    {
        var baseDefinition = new IffSchemaDefinition(2, "Common.iff", "Global", 8, true,
            [new("ItemId", 0, 4, IffFieldType.UInt32)]);
        IffSchema baseSchema = IffSchemaJson.ToSchema(baseDefinition, 12);
        var definition = new IffSchemaDefinition(2, "Item.iff", "Global", 12, true,
            [new("Value", 8, 4, IffFieldType.UInt32)], Base: new IffSchemaBaseReference("Common"));

        IffSchema schema = IffSchemaJson.ToSchema(definition, 12, baseSchema);

        Assert.True(schema.Fields.Single(field => field.Name == "ItemId").IsInherited);
        Assert.False(schema.Fields.Single(field => field.Name == "Value").IsInherited);
        Assert.Equal(["Value"], schema.LocalFields!.Select(field => field.Name));
        Assert.Equal(definition.Base, schema.BaseReference);
    }

    [Fact]
    public void Composition_RejectsDuplicateAndOverlappingLocalFields()
    {
        IffSchema baseSchema = IffSchemaJson.ToSchema(new IffSchemaDefinition(2, "Common.iff", "Global", 8,
            true, [new("ItemId", 0, 4, IffFieldType.UInt32)]), 12);
        var duplicate = new IffSchemaDefinition(2, "Item.iff", "Global", 12, true,
            [new("ItemId", 8, 4, IffFieldType.UInt32)], Base: new IffSchemaBaseReference("Common"));
        var overlap = duplicate with { Fields = [new("Other", 2, 4, IffFieldType.UInt32)] };

        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ToSchema(duplicate, 12, baseSchema));
        Assert.Throws<InvalidDataException>(() => IffSchemaJson.ToSchema(overlap, 12, baseSchema));
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
    public void DirectoryProvider_ComposesRegionalBaseAndReportsCycles()
    {
        var provider = new DirectoryIffSchemaProvider(_directory);
        provider.Save(new IffSchemaDefinition(2, "Common.iff", "Global", 4, true,
            [new("ItemId", 0, 4, IffFieldType.UInt32)]));
        provider.Save(new IffSchemaDefinition(2, "Data.iff", "Global", 8, true,
            [new("Value", 4, 4, IffFieldType.UInt32)],
            Base: new IffSchemaBaseReference("Common")));

        IffSchemaResolution composed = provider.Resolve("Data.iff", ["Global_30447", "Global"], 8);

        Assert.NotNull(composed.Schema);
        Assert.Equal(["ItemId", "Value"], composed.Schema.Fields.Select(field => field.Name));
        provider.Save(new IffSchemaDefinition(2, "Common.iff", "Global", 4, true,
            [new("ItemId", 0, 4, IffFieldType.UInt32)],
            Base: new IffSchemaBaseReference("Common")));

        IffSchemaResolution cycle = provider.Resolve("Data.iff", "Global", 8);

        Assert.Null(cycle.Schema);
        Assert.Contains("Circular", cycle.Warning, StringComparison.OrdinalIgnoreCase);
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
    public void SavedSource_PreservesMalformedJsonAndCandidateMetadata()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "Data.JP.json");
        const string malformed = "{ saved but invalid }";
        File.WriteAllText(path, malformed);
        var provider = new DirectoryIffSchemaProvider(_directory);

        IffSavedSchemaSource source = Assert.IsType<IffSavedSchemaSource>(
            provider.ReadSavedSource("Data.iff", ["JP"], 8));

        Assert.Equal("Data.iff", source.FileName);
        Assert.Equal("JP", source.CandidateRegion);
        Assert.Equal(path, source.SourcePath);
        Assert.Equal(path, source.DestinationPath);
        Assert.Equal(malformed, source.Json);
        Assert.Null(source.Definition);
        Assert.False(string.IsNullOrWhiteSpace(source.Error));
    }

    [Fact]
    public void SavedSource_ReturnsDefinitionEvenWhenItDoesNotFitCurrentRecord()
    {
        Directory.CreateDirectory(_directory);
        var provider = new DirectoryIffSchemaProvider(_directory);
        provider.Save(new IffSchemaDefinition(2, "Data.iff", "JP", 16, true,
            [new("Saved field", 12, 4, IffFieldType.UInt32)], 8));

        IffSavedSchemaSource source = Assert.IsType<IffSavedSchemaSource>(
            provider.ReadSavedSource("Data.iff", ["JP"], 8));

        Assert.NotNull(source.Definition);
        Assert.Equal("Saved field", Assert.Single(source.Definition.Fields).Name);
        Assert.False(string.IsNullOrWhiteSpace(source.Error));
        Assert.Null(provider.Resolve("Data.iff", "JP", 8).Schema);
    }

    [Fact]
    public void SavedSource_UsesExactThenFamilyThenDefaultPrecedence()
    {
        Directory.CreateDirectory(_directory);
        var provider = new DirectoryIffSchemaProvider(_directory);
        provider.Save(Schema("Data.iff", "Global_1", "Exact"));
        provider.Save(Schema("Data.iff", "Global", "Family"));
        provider.Save(Schema("Data.iff", "*", "Default"));

        Assert.Equal("Global_1", provider.ReadSavedSource("Data.iff", ["Global_1", "Global"])!.CandidateRegion);
        File.Delete(provider.GetSchemaPath("Data.iff", "Global_1"));
        Assert.Equal("Global", provider.ReadSavedSource("Data.iff", ["Global_1", "Global"])!.CandidateRegion);
        File.Delete(provider.GetSchemaPath("Data.iff", "Global"));
        Assert.Equal("*", provider.ReadSavedSource("Data.iff", ["Global_1", "Global"])!.CandidateRegion);
    }

    [Fact]
    public void EmbeddedSavedSource_WritesRepairAsLocalOverride()
    {
        var provider = new DirectoryIffSchemaProvider(_directory, new EmbeddedIffSchemaProvider());

        IffSavedSchemaSource source = Assert.IsType<IffSavedSchemaSource>(
            provider.ReadSavedSource("Ability.iff", ["JP"], 80));

        Assert.True(source.IsEmbedded);
        Assert.False(File.Exists(source.DestinationPath));
        provider.SaveJson(source, source.Json, ["JP"], 80);
        Assert.True(File.Exists(source.DestinationPath));
        Assert.NotNull(provider.Resolve("Ability.iff", "JP", 80).Schema);
    }

    [Fact]
    public void SaveJson_LeavesOriginalUntouchedUntilReplacementFullyMaterializes()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "Data.JP.json");
        const string malformed = "{ original invalid json }";
        File.WriteAllText(path, malformed);
        var provider = new DirectoryIffSchemaProvider(_directory);
        IffSavedSchemaSource source = provider.ReadSavedSource("Data.iff", ["JP"])!;
        string invalidReplacement = IffSchemaJson.Serialize(new IffSchemaDefinition(2, "Data.iff", "JP", 8,
            true, [new("Too wide", 6, 4, IffFieldType.UInt32)], 4));

        Assert.Throws<InvalidDataException>(() => provider.SaveJson(source, invalidReplacement, ["JP"], 8));
        Assert.Equal(malformed, File.ReadAllText(path));

        string validReplacement = IffSchemaJson.Serialize(new IffSchemaDefinition(2, "Data.iff", "JP", 8,
            true, [new("Value", 0, 4, IffFieldType.UInt32)], 4));
        IffSchemaDefinition saved = provider.SaveJson(source, validReplacement, ["JP"], 8);

        Assert.Equal("Value", Assert.Single(saved.Fields).Name);
        Assert.Equal(validReplacement, File.ReadAllText(path));
        Assert.Equal("Value", Assert.Single(provider.Resolve("Data.iff", "JP", 8).Schema!.Fields).Name);
    }

    [Fact]
    public void SaveValidated_RejectsBrokenInheritedCompositionWithoutOverwritingSchema()
    {
        Directory.CreateDirectory(_directory);
        var provider = new DirectoryIffSchemaProvider(_directory);
        provider.Save(new IffSchemaDefinition(2, "Common.iff", "JP", 8, true,
            [new("Base value", 0, 4, IffFieldType.UInt32)], 4));
        var original = new IffSchemaDefinition(2, "Data.iff", "JP", 8, true,
            [new("Local value", 4, 4, IffFieldType.UInt32)], 4, Base: new("Common"));
        provider.SaveValidated(original, ["JP"], 8);
        string path = provider.GetSchemaPath("Data.iff", "JP");
        string before = File.ReadAllText(path);
        IffSchemaDefinition overlapping = original with
        {
            Fields = [new("Overlapping", 2, 4, IffFieldType.UInt32)]
        };

        Assert.Throws<InvalidDataException>(() => provider.SaveValidated(overlapping, ["JP"], 8));
        Assert.Equal(before, File.ReadAllText(path));
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

        await using var unknownStream = new MemoryStream(IffBytes(revision: 1, magic: 42));
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

    [Fact]
    public void DefaultManager_DetectsOnlyNewerChangedDefinitionsAndStampsIdenticalLegacyFiles()
    {
        var provider = new DirectoryIffSchemaProvider(_directory);
        IffSchemaDefinition bundledChanged = Schema("Changed.iff", "JP", "Bundled") with { DefaultRevision = 2 };
        IffSchemaDefinition localChanged = Schema("Changed.iff", "JP", "Local") with { DefaultRevision = 1 };
        IffSchemaDefinition bundledSame = Schema("Same.iff", "JP", "Same") with { DefaultRevision = 1 };
        IffSchemaDefinition localSame = bundledSame with { DefaultRevision = 0 };
        IffSchemaDefinition bundledEqual = Schema("Equal.iff", "JP", "Equal") with { DefaultRevision = 2 };
        IffSchemaDefinition bundledOlder = Schema("Older.iff", "JP", "Older") with { DefaultRevision = 2 };
        provider.Save(localChanged);
        provider.Save(localSame);
        provider.Save(bundledEqual);
        provider.Save(bundledOlder with { DefaultRevision = 3 });
        provider.Save(Schema("Custom.iff", "JP", "Custom"));
        var manager = new IffSchemaDefaultManager(provider,
            [bundledChanged, bundledSame, bundledEqual, bundledOlder]);

        IffSchemaUpdateCandidate candidate = Assert.Single(manager.FindUpdates());

        Assert.Equal("Changed.iff", candidate.FileName);
        Assert.Equal(1, candidate.LocalRevision);
        Assert.Equal(2, candidate.BundledRevision);
        IffSchemaDefinition stamped = IffSchemaJson.Deserialize(
            File.ReadAllText(provider.GetSchemaPath("Same.iff", "JP")));
        Assert.Equal(1, stamped.DefaultRevision);
        Assert.Equal("Same", Assert.Single(stamped.Fields).Name);
    }

    [Fact]
    public void DefaultManager_DetectsChangedLegacyAndWildcardSchemas()
    {
        var provider = new DirectoryIffSchemaProvider(_directory);
        IffSchemaDefinition localLegacy = Schema("Data.iff", "*", "Local");
        IffSchemaDefinition bundled = Schema("Data.iff", "*", "Bundled") with { DefaultRevision = 1 };
        provider.Save(localLegacy);
        var manager = new IffSchemaDefaultManager(provider, [bundled]);

        IffSchemaUpdateCandidate candidate = Assert.Single(manager.FindUpdates());

        Assert.Equal("*", candidate.Region);
        Assert.Equal(provider.GetSchemaPath("Data.iff", "*"), candidate.LocalPath);
    }

    [Fact]
    public void DefaultManager_ReplacesOrKeepsCompleteUserDefinitionAndCreatesBackups()
    {
        var provider = new DirectoryIffSchemaProvider(_directory);
        IffSchemaDefinition localReplace = Schema("Replace.iff", "JP", "Local") with { DefaultRevision = 1 };
        IffSchemaDefinition localPreferred = Schema("Preferred.iff", "JP", "My field") with
        {
            DefaultRevision = 1,
            DefaultStringSize = 7,
            Ui = new IffSchemaUiDefinition([new("Custom", [new("My field")])])
        };
        IffSchemaDefinition bundledReplace = Schema("Replace.iff", "JP", "Bundled") with { DefaultRevision = 2 };
        IffSchemaDefinition bundledPreferred = Schema("Preferred.iff", "JP", "Default") with { DefaultRevision = 3 };
        provider.Save(localReplace);
        provider.Save(localPreferred);
        var manager = new IffSchemaDefaultManager(provider, [bundledReplace, bundledPreferred]);
        IffSchemaUpdateCandidate[] candidates = manager.FindUpdates().ToArray();

        IffSchemaUpdateResult result = manager.ApplyUpdates([
            new(candidates.Single(item => item.FileName == "Replace.iff"),
                IffSchemaUpdateAction.ReplaceWithBundledDefault),
            new(candidates.Single(item => item.FileName == "Preferred.iff"),
                IffSchemaUpdateAction.UseLocalDefinition)
        ]);

        Assert.Equal(1, result.ReplacedCount);
        Assert.Equal(1, result.PreferredLocalCount);
        Assert.NotNull(result.BackupDirectory);
        Assert.Equal(2, Directory.GetFiles(result.BackupDirectory!, "*.json").Length);
        Assert.Equal(IffSchemaJson.Serialize(bundledReplace), IffSchemaJson.Serialize(IffSchemaJson.Deserialize(
            File.ReadAllText(provider.GetSchemaPath("Replace.iff", "JP")))));
        IffSchemaDefinition preferred = IffSchemaJson.Deserialize(
            File.ReadAllText(provider.GetSchemaPath("Preferred.iff", "JP")));
        Assert.Equal(IffSchemaJson.Serialize(localPreferred with { DefaultRevision = 3 }),
            IffSchemaJson.Serialize(preferred));
        Assert.Empty(manager.FindUpdates());
    }

    [Fact]
    public void DefaultManager_RejectsInvalidStagedCompositionWithoutChangingFiles()
    {
        var provider = new DirectoryIffSchemaProvider(_directory);
        IffSchemaDefinition localBase = new(2, "Common.iff", "JP", 4, true,
            [new("Base", 0, 4, IffFieldType.UInt32)], DefaultRevision: 1);
        IffSchemaDefinition localDerived = new(2, "Data.iff", "JP", 8, true,
            [new("Local", 4, 4, IffFieldType.UInt32)], Base: new("Common"), DefaultRevision: 1);
        IffSchemaDefinition bundledBase = localBase with
        {
            MinimumRecordSize = 8,
            Fields = [new("Expanded base", 0, 8, IffFieldType.Raw)],
            DefaultRevision = 2
        };
        provider.Save(localBase);
        provider.Save(localDerived);
        string basePath = provider.GetSchemaPath("Common.iff", "JP");
        string before = File.ReadAllText(basePath);
        var manager = new IffSchemaDefaultManager(provider, [bundledBase]);
        IffSchemaUpdateCandidate candidate = Assert.Single(manager.FindUpdates());

        Assert.Throws<InvalidDataException>(() => manager.ApplyUpdates([
            new(candidate, IffSchemaUpdateAction.ReplaceWithBundledDefault)
        ]));

        Assert.Equal(before, File.ReadAllText(basePath));
        Assert.False(Directory.Exists(Path.Combine(_directory, "backups")));
    }

    [Fact]
    public void DefaultManager_RejectsAStaleCandidateBeforeWritingOrBackingUp()
    {
        var provider = new DirectoryIffSchemaProvider(_directory);
        IffSchemaDefinition local = Schema("Data.iff", "JP", "Local") with { DefaultRevision = 1 };
        IffSchemaDefinition bundled = Schema("Data.iff", "JP", "Bundled") with { DefaultRevision = 2 };
        provider.Save(local);
        var manager = new IffSchemaDefaultManager(provider, [bundled]);
        IffSchemaUpdateCandidate candidate = Assert.Single(manager.FindUpdates());
        IffSchemaDefinition externalEdit = local with { Fields = [new("External", 0, 4, IffFieldType.UInt32)] };
        provider.Save(externalEdit);

        Assert.Throws<InvalidDataException>(() => manager.ApplyUpdates([
            new(candidate, IffSchemaUpdateAction.ReplaceWithBundledDefault)
        ]));

        Assert.Equal("External", Assert.Single(IffSchemaJson.Deserialize(
            File.ReadAllText(provider.GetSchemaPath("Data.iff", "JP"))).Fields).Name);
        Assert.False(Directory.Exists(Path.Combine(_directory, "backups")));
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
            IffFieldType.Int64 => 8,
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
