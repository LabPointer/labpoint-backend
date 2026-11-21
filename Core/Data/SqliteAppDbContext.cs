using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Data;

/// <summary>
/// Versão SQLite do <see cref="AppDbContext"/>: mesmo modelo, com migrations próprias em
/// <c>Backend/Migrations/Sqlite</c> (<c>dotnet ef ... --context SqliteAppDbContext</c>).
/// </summary>
public class SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : AppDbContext(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // O SQLite não compara nem ordena DateTimeOffset guardado como texto. Como o sistema grava sempre em UTC,
        // guardar como número (ticks) mantém a ordem e permite filtros como "check_out_closes_at <= agora".
        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<DateTimeOffsetToBinaryConverter>();
        configurationBuilder.Properties<DateTimeOffset?>().HaveConversion<DateTimeOffsetToBinaryConverter>();
    }
}
