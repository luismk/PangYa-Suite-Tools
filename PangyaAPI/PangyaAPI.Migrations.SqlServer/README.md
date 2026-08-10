# SQL Server migrations

This project contains the SQL Server EF Core migrations used by AuthServer at startup.

Before generating the baseline, put a working local SQL Server connection in
`../PangyaAPI.Network/appsettings.json` or override `ConnectionStrings__Pangya` in the environment. Never commit credentials.

Run `dotnet tool restore` once to install the repository-pinned EF CLI.

The authoritative baseline must be scaffolded from the live database before `InitialBaseline` is generated. EF scaffolding does not preserve stored procedures, so script procedure and function definitions separately into migration SQL resources.

```powershell
$connectionString = (Get-Content PangyaAPI/PangyaAPI.Network/appsettings.json -Raw | ConvertFrom-Json).ConnectionStrings.Pangya
dotnet ef dbcontext scaffold "$connectionString" Microsoft.EntityFrameworkCore.SqlServer --project PangyaAPI/PangyaAPI.SQL --startup-project PangyaAPI/PangyaAPI.Migrations.SqlServer --context PangyaSchemaDbContext --schema pangya --no-onconfiguring
dotnet ef migrations add InitialBaseline --project PangyaAPI/PangyaAPI.Migrations.SqlServer --startup-project PangyaAPI/PangyaAPI.Migrations.SqlServer --context PangyaSchemaDbContext
dotnet ef migrations script 0 InitialBaseline --project PangyaAPI/PangyaAPI.Migrations.SqlServer --startup-project PangyaAPI/PangyaAPI.Migrations.SqlServer --context PangyaSchemaDbContext --output artifacts/InitialBaseline.sql
```

Do not run `database update` against the source database. Validate the generated schema and stored-procedure scripts against an isolated empty database first. AuthServer automatically stamps `InitialBaseline` when it detects the legacy `pangya.account` table in a database with no migration history.
