using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using PangyaAPI.SQL;
using PangyaAPI.SQL.EntityFramework;

namespace PangyaAPI.Migrations.PostgreSql
{
    public sealed class PostgreSqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PangyaSchemaDbContext>
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
                Engine = configuration["Database:Engine"] ?? "POSTGRESQL",
                ConnectionString = configuration.GetConnectionString("Pangya") ?? string.Empty,
                LogCommands = !bool.TryParse(configuration["Database:LogCommands"], out var logCommands)
                    || logCommands
            };

            if (!options.Engine.Equals("POSTGRESQL", StringComparison.OrdinalIgnoreCase)
                && !options.Engine.Equals("PGSQL", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "The PostgreSQL migration project requires Database:Engine to be POSTGRESQL or PGSQL.");
            }

            DatabaseConnectionStringFactory.Validate(options);
            var builder = new DbContextOptionsBuilder<PangyaSchemaDbContext>();
            builder.UseNpgsql(
                DatabaseConnectionStringFactory.Create(options),
                provider => provider
                    .MigrationsAssembly(typeof(PostgreSqlDesignTimeDbContextFactory).Assembly.FullName)
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
