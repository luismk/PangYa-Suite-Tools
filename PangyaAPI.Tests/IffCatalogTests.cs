using System.Buffers.Binary;
using System.IO.Compression;
using PangyaAPI.IFF;
using Xunit;

namespace PangyaAPI.Tests;

public sealed class IffCatalogTests
{
    [Theory]
    [InlineData("JP")]
    [InlineData(IffHeader.Japan983Region)]
    public void JpSchemas_MaterializeWithCompleteCoverage(string region)
    {
        var provider = new EmbeddedIffSchemaProvider();
        IffSchemaDefinition[] definitions = provider.LoadDefinitions()
            .Where(definition => definition.Region == region)
            .ToArray();

        Assert.NotEmpty(definitions);
        foreach (IffSchemaDefinition definition in definitions)
        {
            IffSchemaResolution resolution = provider.Resolve(definition.FileName, region, definition.MinimumRecordSize);
            Assert.True(resolution.Schema is not null, $"{definition.FileName}: {resolution.Warning}");
            IffSchema schema = resolution.Schema!;
            Assert.Equal(0, IffSchemaCoverage.Calculate(schema, definition.MinimumRecordSize).UnrepresentedBytes);
            if (!schema.IsOpaque)
                Assert.DoesNotContain(schema.Fields, field =>
                    IffSchemaCoverage.IsCatchAllRawRecord(field, definition.MinimumRecordSize));
        }
    }

    [Theory]
    [InlineData("AddonPart.iff")]
    [InlineData("ArtifactManaInfo.iff")]
    [InlineData("CharacterMastery.iff")]
    [InlineData("ClubSetWorkShopLevelUpLimit.iff")]
    [InlineData("ClubSetWorkShopLevelUpProb.iff")]
    [InlineData("ClubSetWorkShopRankUpExp.iff")]
    [InlineData("CounterItem.iff")]
    [InlineData("ErrorCodeInfo.iff")]
    [InlineData("GrandPrixAIOptionalData.iff")]
    [InlineData("GrandPrixConditionEquip.iff")]
    [InlineData("MemorialShopRareItemSff.iff")]
    [InlineData("PointShop.iff")]
    [InlineData("TimeLimitItem.iff")]
    [InlineData("Title.iff")]
    [InlineData("u_part_type.iff")]
    public void FormerTypedModels_HaveEmbeddedJpSchemas(string fileName)
    {
        IffSchemaDefinition definition = new EmbeddedIffSchemaProvider().LoadDefinitions()
            .Single(candidate => candidate.Region == "JP" && candidate.FileName == fileName);

        IffSchemaResolution resolution = new EmbeddedIffSchemaProvider().Resolve(fileName, "JP", definition.MinimumRecordSize);
        Assert.True(resolution.Schema is not null, resolution.Warning);
    }

    [Fact]
    public void SignedAndUnsigned64Fields_RoundTripWithoutChangingNeighbors()
    {
        var schema = new IffSchema("Numeric", 10,
        [
            new IffField("Signed", 1, 1, IffFieldType.SByte),
            new IffField("Unsigned", 2, 8, IffFieldType.UInt64)
        ]);
        IffRecord record = IffRecord.CreateBlank(0, 10, schema);
        record.SetValue("Signed", -12);
        record.SetValue("Unsigned", ulong.MaxValue);

        Assert.Equal(-12, record.GetSByte("Signed"));
        Assert.Equal(ulong.MaxValue, record.GetUInt64("Unsigned"));
        Assert.Equal(0, record.Bytes.Span[0]);
    }

    [Fact]
    public async Task Catalog_LoadsAndIndexesLooseJpTable()
    {
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "PointShop.iff");
        WritePointShop(path, 42);

        IffCatalog catalog = await IffCatalog.LoadAsync(path,
            new IffCatalogOptions("JP", ["PointShop"]));

        IffRecord record = catalog.GetTable("PointShop").GetRequired(42);
        Assert.Equal((uint)900, record.GetUInt32("Points"));
        Assert.Equal("JP", catalog.Region);
    }

    [Fact]
    public void Japan983Schemas_IdentifyImageResourcesAndItemReferences()
    {
        var provider = new EmbeddedIffSchemaProvider();
        IReadOnlyList<string> regions = IffHeader.GetSchemaRegions(IffHeader.Japan983Region);
        IffSchema part = Assert.IsType<IffSchema>(provider.Resolve("Part.iff", regions, 568).Schema);
        Assert.Equal(IffFieldType.ImageResource, part.Fields.Single(field => field.Name == "Icon").Type);
        Assert.Equal(IffFieldType.ImageResource, part.Fields.Single(field => field.Name == "Texture1").Type);
        Assert.Equal(IffFieldType.UInt32, part.Fields.Single(field => field.Name == "ItemId").Type);
        IffField subPart = part.Fields.Single(field => field.Name == "SubPart1");
        Assert.Equal(IffFieldType.ItemIdReference, subPart.Type);
        Assert.Equal("Part.iff", subPart.Reference?.TargetFile);

        IffSchema furniture = Assert.IsType<IffSchema>(provider.Resolve("Furniture.iff", regions, 512).Schema);
        Assert.Equal(IffFieldType.ImageResource, furniture.Fields.Single(field => field.Name == "Sprite3").Type);

        IffSchema clubSet = Assert.IsType<IffSchema>(provider.Resolve("ClubSet.iff", regions, 260).Schema);
        IffField club = clubSet.Fields.Single(field => field.Name == "ClubIds1");
        Assert.Equal(IffFieldType.ItemIdReference, club.Type);
        Assert.Equal("Club.iff", club.Reference?.TargetFile);

        IffSchema reward = Assert.IsType<IffSchema>(provider.Resolve("GrandPrixRankReward.iff", regions, 76).Schema);
        IffField rewardItem = reward.Fields.Single(field => field.Name == "Reward1TypeId");
        Assert.Equal(IffFieldType.ItemIdReference, rewardItem.Type);
        Assert.Equal("Desc.iff", rewardItem.Reference?.TargetFile);
    }

    [Fact]
    public async Task Catalog_LoadsJapan983ArchiveWithRepeatedReferencesAndEmptyTable()
    {
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "pangya_jp.iff");
        using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            byte[] item = new byte[8 + 248];
            item[0] = 1;
            item[4] = 13;
            BinaryPrimitives.WriteUInt32LittleEndian(item.AsSpan(8 + 4), 42);
            BinaryPrimitives.WriteUInt32LittleEndian(item.AsSpan(8 + 116), 120);
            WriteEntry(archive, "Item.iff", item);

            byte[] abilities = new byte[8 + 2 * 60];
            abilities[0] = 2;
            abilities[2] = 57;
            abilities[4] = 13;
            BinaryPrimitives.WriteUInt32LittleEndian(abilities.AsSpan(8 + 4), 42);
            BinaryPrimitives.WriteUInt32LittleEndian(abilities.AsSpan(8 + 60 + 4), 42);
            WriteEntry(archive, "FurnitureAbility.iff", abilities);
            WriteEntry(archive, "ScratchRewardSetting.iff", [0, 0, 57, 0, 13, 0, 0, 0]);
        }

        IffCatalog catalog = await IffCatalog.LoadAsync(path);

        Assert.Equal(IffHeader.Japan983Region, catalog.Region);
        Assert.Equal(120u, catalog.GetTable("Item").GetRequired(42).GetUInt32("Price"));
        IffTable furnitureAbility = catalog.GetTable("FurnitureAbility");
        Assert.Null(furnitureAbility.KeyField);
        Assert.Equal(2, furnitureAbility.Records.Count);
        Assert.Empty(catalog.GetTable("ScratchRewardSetting").Records);

        static void WriteEntry(ZipArchive archive, string name, byte[] bytes)
        {
            using Stream entry = archive.CreateEntry(name).Open();
            entry.Write(bytes);
        }
    }

    [Fact]
    public async Task Catalog_RejectsDuplicateKeysAndMissingRequiredTables()
    {
        using var directory = new TemporaryDirectory();
        string path = Path.Combine(directory.Path, "PointShop.iff");
        WritePointShop(path, 42, 42);

        await Assert.ThrowsAsync<InvalidDataException>(() => IffCatalog.LoadAsync(path,
            new IffCatalogOptions("JP")));

        WritePointShop(path, 42);
        await Assert.ThrowsAsync<InvalidDataException>(() => IffCatalog.LoadAsync(path,
            new IffCatalogOptions("JP", ["Part"])));
    }

    private static void WritePointShop(string path, params uint[] ids)
    {
        byte[] data = new byte[8 + ids.Length * 20];
        BinaryPrimitives.WriteUInt16LittleEndian(data, checked((ushort)ids.Length));
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(2), 548);
        data[4] = 13;
        for (int index = 0; index < ids.Length; index++)
        {
            Span<byte> record = data.AsSpan(8 + index * 20, 20);
            BinaryPrimitives.WriteUInt32LittleEndian(record, 1);
            BinaryPrimitives.WriteUInt32LittleEndian(record[4..], ids[index]);
            BinaryPrimitives.WriteUInt32LittleEndian(record[8..], 900);
        }
        File.WriteAllBytes(path, data);
    }
}
