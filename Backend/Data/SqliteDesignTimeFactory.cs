using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Data;

/// <summary>
/// Usado só pelo <c>dotnet ef</c> para gerar e aplicar as migrations do SQLite, mesmo quando o appsettings está em
/// PostgreSQL:
/// <code>
/// dotnet ef migrations add Nome --context SqliteAppDbContext --output-dir Migrations/Sqlite
/// dotnet ef database update --context SqliteAppDbContext
/// </code>
/// O arquivo do banco vem de <c>ConnectionStrings:SqliteConnection</c> (padrão <c>Data Source=labpoint.db</c>), lido de
/// appsettings.json e appsettings.Development.json.
/// </summary>
public sealed class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<SqliteAppDbContext>
{
    public SqliteAppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connection = configuration.GetConnectionString("SqliteConnection");
        if (string.IsNullOrWhiteSpace(connection))
            connection = "Data Source=labpoint.db";

        var options = new DbContextOptionsBuilder<SqliteAppDbContext>();
        DatabaseSetup.ConfigureSqlite(options, connection);
        return new SqliteAppDbContext(options.Options);
    }
}
