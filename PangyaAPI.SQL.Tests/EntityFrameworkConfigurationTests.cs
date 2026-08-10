using Microsoft.EntityFrameworkCore;
using PangyaAPI.SQL;
using PangyaAPI.SQL.EntityFramework;
using PangyaAPI.SQL.EntityFramework.Entities;

namespace PangyaAPI.SQL.Tests;

public sealed class EntityFrameworkConfigurationTests
{
    [Fact]
    public void LightweightEntitiesMatchLegacySqlColumnTypes()
    {
        Assert.Equal(typeof(short), typeof(AccountEntity).GetProperty(nameof(AccountEntity.Logon))!.PropertyType);
        Assert.Equal(typeof(string), typeof(AccountEntity).GetProperty(nameof(AccountEntity.GameServerId))!.PropertyType);
        Assert.Equal(typeof(short), typeof(AuthKeyLoginEntity).GetProperty(nameof(AuthKeyLoginEntity.Valid))!.PropertyType);
    }

    [Theory]
    [InlineData("SQLSERVER", "Server=localhost;Database=pangya;Integrated Security=true;TrustServerCertificate=true", "SqlServer")]
    [InlineData("MYSQL", "Server=localhost;Database=pangya;Uid=test;Pwd=test", "MySql")]
    [InlineData("POSTGRESQL", "Host=localhost;Database=pangya;Username=test;Password=test", "Npgsql")]
    public void ConfiguresSelectedProvider(string engine, string connectionString, string providerFragment)
    {
        var options = new DatabaseOptions { Engine = engine, ConnectionString = connectionString };
        var builder = new DbContextOptionsBuilder<PangyaDbContext>();

        PangyaDbContextOptions.Configure(builder, options);

        using var context = new PangyaDbContext(builder.Options);
        Assert.Contains(providerFragment, context.Database.ProviderName, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(context.Model.GetEntityTypes());
    }

    [Fact]
    public void RejectsUnsupportedProvider()
    {
        var options = new DatabaseOptions { Engine = "ORACLE", ConnectionString = "Data Source=test" };
        var builder = new DbContextOptionsBuilder<PangyaDbContext>();

        Assert.Throws<NotSupportedException>(() => PangyaDbContextOptions.Configure(builder, options));
    }

    [Fact]
    public void RejectsMissingConnectionString()
    {
        var options = new DatabaseOptions { Engine = "SQLSERVER", ConnectionString = string.Empty };

        Assert.Throws<InvalidOperationException>(() => DatabaseConnectionStringFactory.Validate(options));
    }
}
