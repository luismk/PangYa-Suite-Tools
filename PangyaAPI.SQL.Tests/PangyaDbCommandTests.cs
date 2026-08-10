using PangyaAPI.SQL;
using PangyaAPI.SQL.EntityFramework;

namespace PangyaAPI.SQL.Tests;

public sealed class PangyaDbCommandTests
{
    [Fact]
    public void ProcedureValuesArePassedAsTypedParameters()
    {
        var fake = new FakeExecutor();
        DatabaseConfiguration.Configure(new DatabaseOptions
        {
            Engine = "SQLSERVER",
            ConnectionString = "Server=localhost;Database=pangya;Integrated Security=true;TrustServerCertificate=true",
            LogCommands = false
        });
        DatabaseConfiguration.ConfigureExecutor(fake);
        var command = new TestCommand();

        command.exec();

        Assert.Equal("pangya.TestProcedure", fake.ProcedureName);
        Assert.Collection(
            fake.Parameters,
            value => { Assert.Equal("@p0", value.Name); Assert.Equal(42, value.Value); },
            value => { Assert.Equal("@p1", value.Name); Assert.Equal("player", value.Value); });
    }

    [Fact]
    public void LegacyEmptyStringSentinelCallsProcedureWithoutParameters()
    {
        var fake = ConfigureFakeExecutor();
        var command = new NoParameterTestCommand();

        command.exec();

        Assert.Equal("pangya.ProcGetCommands", fake.ProcedureName);
        Assert.Empty(fake.Parameters);
    }

    private static FakeExecutor ConfigureFakeExecutor()
    {
        var fake = new FakeExecutor();
        DatabaseConfiguration.Configure(new DatabaseOptions
        {
            Engine = "SQLSERVER",
            ConnectionString = "Server=localhost;Database=pangya;Integrated Security=true;TrustServerCertificate=true",
            LogCommands = false
        });
        DatabaseConfiguration.ConfigureExecutor(fake);
        return fake;
    }

    private sealed class FakeExecutor : IRelationalCommandExecutor
    {
        public string ProcedureName { get; private set; } = string.Empty;
        public IReadOnlyList<RelationalParameter> Parameters { get; private set; } = Array.Empty<RelationalParameter>();

        public Response ExecuteText(string commandText, IReadOnlyList<RelationalParameter> parameters)
            => throw new NotSupportedException();

        public Response ExecuteStoredProcedure(string procedureName, IReadOnlyList<RelationalParameter> parameters)
        {
            ProcedureName = procedureName;
            Parameters = parameters;
            var response = new Response();
            response.setRowsAffected(1);
            return response;
        }
    }

    private sealed class TestCommand : Pangya_DB
    {
        protected override void lineResult(ctx_res result, uint index)
        {
        }

        protected override Response prepareConsulta()
            => procedure("pangya.TestProcedure", 42, "player");
    }

    private sealed class NoParameterTestCommand : Pangya_DB
    {
        protected override void lineResult(ctx_res result, uint index)
        {
        }

        protected override Response prepareConsulta()
            => procedure("pangya.ProcGetCommands", "");
    }
}
