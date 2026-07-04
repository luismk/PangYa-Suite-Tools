using PangyaAPI.IFF;
using Xunit;

namespace PangyaAPI.Tests;

public sealed class IffSchemaCoverageTests
{
    [Fact]
    public void OrdinaryFields_CoverTheirCompleteByteRanges()
    {
        var schema = Schema(new IffField("Value", 1, 2, IffFieldType.UInt16));

        IffSchemaCoverageResult result = IffSchemaCoverage.Calculate(schema, 4);

        Assert.Equal(2, result.RepresentedBytes);
        Assert.Equal(2, result.UnrepresentedBytes);
    }

    [Fact]
    public void PartialBitMasks_RequireTheirUnionToCoverTheWholeByte()
    {
        var partial = Schema(new IffField("Low", 0, 4, IffFieldType.BitField, BitMask: 0x0F));
        var complete = Schema(
            new IffField("Low", 0, 4, IffFieldType.BitField, BitMask: 0x0F),
            new IffField("High", 0, 4, IffFieldType.BitField, BitMask: 0xF0));

        Assert.Equal(4, IffSchemaCoverage.Calculate(partial, 4).UnrepresentedBytes);
        IffSchemaCoverageResult combined = IffSchemaCoverage.Calculate(complete, 4);
        Assert.Equal(1, combined.RepresentedBytes);
        Assert.Equal(3, combined.UnrepresentedBytes);
    }

    [Fact]
    public void OrdinaryAndBitFields_UnionTheirCoverage()
    {
        var schema = Schema(
            new IffField("Value", 0, 1, IffFieldType.Byte),
            new IffField("Flag", 0, 4, IffFieldType.BitField, BitMask: 0x100));

        IffSchemaCoverageResult result = IffSchemaCoverage.Calculate(schema, 4);

        Assert.Equal(1, result.RepresentedBytes);
    }

    [Fact]
    public void CatchAllRawRecord_IsIgnoredButSmallerRawRangesCount()
    {
        var schema = Schema(
            new IffField("Raw record", 0, 4, IffFieldType.Raw, false),
            new IffField("Known raw", 1, 2, IffFieldType.Raw, false));

        IffSchemaCoverageResult result = IffSchemaCoverage.Calculate(schema, 4);

        Assert.Equal(2, result.RepresentedBytes);
        Assert.Equal(2, result.UnrepresentedBytes);
        Assert.True(IffSchemaCoverage.IsCatchAllRawRecord(schema.Fields[0], 4));
        Assert.False(IffSchemaCoverage.IsCatchAllRawRecord(schema.Fields[1], 4));
    }

    private static IffSchema Schema(params IffField[] fields) => new("Test", 1, fields);
}
