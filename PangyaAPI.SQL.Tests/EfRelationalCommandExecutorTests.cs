using PangyaAPI.SQL.EntityFramework;

namespace PangyaAPI.SQL.Tests;

public sealed class EfRelationalCommandExecutorTests
{
    [Theory]
    [InlineData(typeof(sbyte), -1, typeof(short), -1)]
    [InlineData(typeof(ushort), 65_535, typeof(int), 65_535)]
    [InlineData(typeof(uint), 4_294_967_295L, typeof(long), 4_294_967_295L)]
    public void NormalizeParameterValue_UnsupportedIntegerType_ReturnsSqlClientCompatibleType(
        Type inputType,
        object inputValue,
        Type expectedType,
        object expectedValue)
    {
        var input = Convert.ChangeType(inputValue, inputType);

        var result = EfRelationalCommandExecutor.NormalizeParameterValue(input);

        Assert.IsType(expectedType, result);
        Assert.Equal(Convert.ChangeType(expectedValue, expectedType), result);
    }

    [Fact]
    public void NormalizeParameterValue_UInt64_ReturnsDecimal()
    {
        var result = EfRelationalCommandExecutor.NormalizeParameterValue(ulong.MaxValue);

        Assert.IsType<decimal>(result);
        Assert.Equal((decimal)ulong.MaxValue, result);
    }

    [Fact]
    public void NormalizeParameterValue_Null_ReturnsDbNull()
    {
        Assert.Same(DBNull.Value, EfRelationalCommandExecutor.NormalizeParameterValue(null));
    }
}
