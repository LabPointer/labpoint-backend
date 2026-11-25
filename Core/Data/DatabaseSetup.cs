using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Models;

namespace Data;

/// <summary>Bancos suportados. Escolhido em <c>Database:Provider</c> no appsettings.</summary>
public enum DatabaseProvider
{
    PostgreSql,
    Sqlite
}

/// <summary>
/// Registra o <see cref="AppDbContext"/> com o provedor do appsettings:
/// <code>
/// "Database": {
///   "Provider": "PostgreSql",            // ou "Sqlite"
///   "SqliteDirectory": "/home/discloud"  // opcional: pasta do arquivo do SQLite
/// },
/// "ConnectionStrings": {
///   "DbConnection": "Host=localhost;Port=5432;Database=pei;Username=...;Password=...",   // PostgreSQL
///   "SqliteConnection": "Data Source=labpoint.db"                                      // SQLite
/// }
/// </code>
/// Com SQLite o serviço injetado continua sendo <see cref="AppDbContext"/> (a instância é um <see cref="SqliteAppDbContext"/>).
/// </summary>
public static class DatabaseSetup
{
    /// <summary>Assembly com as migrations dos dois provedores.</summary>
    public const string MigrationsAssembly = "Backend";

    public static DatabaseProvider GetDatabaseProvider(this IConfiguration configuration)
    {
        var value = configuration["Database:Provider"];
        if (string.IsNullOrWhiteSpace(value))
            return DatabaseProvider.PostgreSql;

        return value.Trim().ToLowerInvariant() switch
        {
            "postgresql" or "postgres" or "npgsql" => DatabaseProvider.PostgreSql,
            "sqlite" => DatabaseProvider.Sqlite,
            _ => throw new InvalidOperationException(
                $"Database:Provider inválido: \"{value}\". Use \"PostgreSql\" ou \"Sqlite\".")
        };
    }

    public static DatabaseProvider AddLabpointDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetDatabaseProvider();

        if (provider == DatabaseProvider.Sqlite)
        {
            var connection = configuration.GetSqliteConnectionString();

            services.AddDbContext<AppDbContext, SqliteAppDbContext>(options =>
                ConfigureSqlite(options, connection));
        }
        else
        {
            var connection = configuration.GetConnectionString("DbConnection");
            if (string.IsNullOrWhiteSpace(connection))
                throw new InvalidOperationException("Configure ConnectionStrings:DbConnection para o PostgreSQL.");

            services.AddDbContext<AppDbContext>(options => ConfigurePostgreSql(options, connection));
        }

        return provider;
    }

    /// <summary>
    /// Connection string do SQLite: <c>ConnectionStrings:SqliteConnection</c> (padrão <c>Data Source=labpoint.db</c>).
    /// Se <c>Database:SqliteDirectory</c> estiver preenchido e o Data Source for um caminho relativo, o arquivo fica
    /// nessa pasta (criada se não existir). Ex.: na Discloud, "/home/discloud" → /home/discloud/labpoint.db.
    /// </summary>
    public static string GetSqliteConnectionString(this IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("SqliteConnection");
        if (string.IsNullOrWhiteSpace(connection))
            connection = "Data Source=labpoint.db";

        var directory = configuration["Database:SqliteDirectory"];
        if (string.IsNullOrWhiteSpace(directory))
            return connection;

        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connection);
        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource)
            || dataSource == ":memory:"
            || dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            || Path.IsPathRooted(dataSource))
            return connection;

        directory = directory.Trim();
        Directory.CreateDirectory(directory);
        builder.DataSource = Path.Combine(directory, dataSource);
        return builder.ToString();
    }

    public static DbContextOptionsBuilder ConfigurePostgreSql(DbContextOptionsBuilder options, string connection) =>
        options
            .UseNpgsql(connection, npgsql =>
            {
                npgsql.MapEnum<EShift>("shift_enum");
                npgsql.MapEnum<EReserveStatus>("reserve_status_enum");
                npgsql.MigrationsAssembly(MigrationsAssembly);
            })
            .UseLazyLoadingProxies();

    public static DbContextOptionsBuilder ConfigureSqlite(DbContextOptionsBuilder options, string connection) =>
        options
            .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly(MigrationsAssembly))
            .UseLazyLoadingProxies();

    /// <summary>Busca full-text em português (tsvector) só existe no PostgreSQL.</summary>
    public static bool SupportsFullTextSearch(this DatabaseFacade database) => database.IsNpgsql();

    /// <summary>LISTEN/NOTIFY (aviso imediato para o serviço de e-mail) só existe no PostgreSQL.</summary>
    public static bool SupportsNotify(this DatabaseFacade database) => database.IsNpgsql();

    /// <summary>Violação de índice único, nos dois provedores (ex.: duas instâncias criando a mesma linha).</summary>
    public static bool IsUniqueViolation(this DbUpdateException exception) => exception.InnerException switch
    {
        Npgsql.PostgresException pg => pg.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation,
        // SQLITE_CONSTRAINT (19) com SQLITE_CONSTRAINT_UNIQUE (2067) ou PRIMARYKEY (1555).
        Microsoft.Data.Sqlite.SqliteException sqlite => sqlite.SqliteErrorCode == 19
                                                       && sqlite.SqliteExtendedErrorCode is 2067 or 1555,
        _ => false
    };
}
