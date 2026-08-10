using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PangyaAPI.SQL;
using PangyaAPI.SQL.EntityFramework;

namespace PangyaAPI.Migrations.SqlServer
{
    public sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PangyaSchemaDbContext>
    {
        public PangyaSchemaDbContext CreateDbContext(string[] args)
        {
            var configurationPath = FindConfigurationPath();
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(configurationPath)!)
                .AddJsonFile(Path.GetFileName(configurationPath), optional: false)
                .AddEnvironmentVariables()
                .Build();

            var options = new DatabaseOptions
            {
                Engine = configuration["Database:Engine"] ?? "SQLSERVER",
                ConnectionString = configuration.GetConnectionString("Pangya") ?? string.Empty,
                LogCommands = !bool.TryParse(configuration["Database:LogCommands"], out var logCommands)
                    || logCommands
            };

            if (!options.Engine.Equals("MSSQL", StringComparison.OrdinalIgnoreCase)
                && !options.Engine.Equals("SQLSERVER", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The SQL Server migration project requires Database:Engine to be MSSQL or SQLSERVER.");
            }

            DatabaseConnectionStringFactory.Validate(options);
            var builder = new DbContextOptionsBuilder<PangyaSchemaDbContext>();
            builder.UseSqlServer(
                DatabaseConnectionStringFactory.Create(options),
                provider => provider
                    .MigrationsAssembly(typeof(SqlServerDesignTimeDbContextFactory).Assembly.FullName)
                    .EnableRetryOnFailure());
            builder.EnableDetailedErrors();
            return new PangyaSchemaDbContext(builder.Options);
        }

        private static string FindConfigurationPath()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                var candidate = Path.Combine(
                    directory.FullName,
                    "PangyaAPI",
                    "PangyaAPI.Network",
                    "appsettings.json");
                if (File.Exists(candidate))
                    return candidate;
                directory = directory.Parent;
            }

            throw new FileNotFoundException(
                "Could not locate PangyaAPI/PangyaAPI.Network/appsettings.json from the current directory.");
        }
    }
}
