using System.Globalization;
using System.Net.Http.Json;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Models;
using Models.Calendar;
using Services;

namespace Backend.Services;

/// <summary>
/// Gera e guarda no banco os feriados de cada ano: nacionais fixos (calculados aqui), móveis e estaduais (BrasilAPI,
/// com cálculo local pela Páscoa se a API falhar) e municipais fixos (configuração "Holidays:Municipal").
/// Os feriados extras cadastrados pelo admin (<see cref="EHolidaySource.Custom"/>) nunca são tocados.
/// </summary>
public interface IHolidayProvider
{
    /// <summary>Garante que os feriados dos anos do período estão no banco (consulta a API no máximo uma vez por ano).</summary>
    Task EnsureYears(DateOnly start, DateOnly end, CancellationToken ct = default);
}

public class HolidayProvider(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    IOptions<HolidaySettings> settings,
    ILogger<HolidayProvider> logger) : IHolidayProvider
{
    public const string HttpClientName = "brasilapi";

    /// <summary>Faixa aceita pela BrasilAPI e pelo cálculo da Páscoa.</summary>
    private const int MinYear = 1900, MaxYear = 2199;

    /// <summary>Sem resposta da API, tenta de novo depois desse intervalo.</summary>
    private static readonly TimeSpan RetryAfterFailure = TimeSpan.FromHours(24);

    /// <summary>Evita duas gerações simultâneas do mesmo ano (worker + requisição) no mesmo processo.</summary>
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private sealed record BrasilApiHoliday(string Date, string Name, string Type);

    private sealed record GeneratedHoliday(DateOnly Date, string Name, EHolidaySource Source);

    public async Task EnsureYears(DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        var first = Math.Max(start.Year, MinYear);
        var last = Math.Min(end.Year, MaxYear);
        for (var year = first; year <= last; year++)
        {
            await EnsureYear(year, ct);
        }
    }

    private async Task EnsureYear(int year, CancellationToken ct)
    {
        var cacheKey = $"holiday-sync:{year}";
        if (cache.TryGetValue(cacheKey, out _))
        {
            return;
        }

        await SyncLock.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(cacheKey, out _))
            {
                return;
            }

            // Contexto próprio: não salva junto alterações pendentes de quem chamou (ex.: uma reserva em edição).
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbCtx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var sync = await dbCtx.HolidayYearSyncs.FirstOrDefaultAsync(s => s.Year == year, ct);
            var now = DateTimeOffset.UtcNow;
            if (sync != null && (sync.FromApi || now - sync.SyncedAt < RetryAfterFailure))
            {
                // Gerado só com o cálculo local: volta a conferir em 1 h (a nova tentativa na API respeita as 24 h).
                cache.Set(cacheKey, true, sync.FromApi ? TimeSpan.FromHours(12) : TimeSpan.FromHours(1));
                return;
            }

            var (holidays, fromApi) = await Generate(year, ct);

            // Regera só os feriados automáticos do ano; os extras do admin ficam.
            await using var transaction = await dbCtx.Database.BeginTransactionAsync(ct);
            var yearStart = new DateOnly(year, 1, 1);
            var yearEnd = new DateOnly(year, 12, 31);
            await dbCtx.Holidays
                .Where(h => h.Date >= yearStart && h.Date <= yearEnd && h.Source != EHolidaySource.Custom)
                .ExecuteDeleteAsync(ct);

            dbCtx.Holidays.AddRange(holidays.Select(h => new HolidayModel
            {
                Date = h.Date,
                Name = h.Name,
                Source = h.Source,
                CreatedAt = now
            }));

            if (sync == null)
            {
                dbCtx.HolidayYearSyncs.Add(new HolidayYearSyncModel { Year = year, SyncedAt = now, FromApi = fromApi });
            }
            else
            {
                sync.SyncedAt = now;
                sync.FromApi = fromApi;
            }

            await dbCtx.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            cache.Set(cacheKey, true, fromApi ? TimeSpan.FromHours(12) : TimeSpan.FromHours(1));
            logger.LogInformation("Feriados de {Year} gerados ({Count}, BrasilAPI: {FromApi}).", year, holidays.Count, fromApi);
        }
        catch (DbUpdateException ex)
        {
            // Outra instância gerou o mesmo ano ao mesmo tempo: o que ficou no banco vale.
            logger.LogWarning(ex, "Conflito ao gerar os feriados de {Year}.", year);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task<(List<GeneratedHoliday> Holidays, bool FromApi)> Generate(int year, CancellationToken ct)
    {
        var list = new List<GeneratedHoliday>();
        list.AddRange(FixedNational(year));

        var api = await FetchBrasilApi(year, ct);
        list.AddRange(api ?? MovableNational(year));
        list.AddRange(Municipal(year));

        // Um registro por data: o primeiro vence (nacional > estadual > municipal, nessa ordem de inserção).
        var holidays = list
            .GroupBy(h => h.Date)
            .Select(g => g.OrderBy(h => h.Source).First())
            .OrderBy(h => h.Date)
            .ToList();

        return (holidays, api != null);
    }

    /// <summary>Feriados nacionais de data fixa (Lei 662/1949, 6.802/1980 e 14.759/2023).</summary>
    private static IEnumerable<GeneratedHoliday> FixedNational(int year)
    {
        (int Month, int Day, string Name)[] fixedDays =
        [
            (1, 1, "Confraternização Universal"),
            (4, 21, "Tiradentes"),
            (5, 1, "Dia do Trabalho"),
            (9, 7, "Independência do Brasil"),
            (10, 12, "Nossa Senhora Aparecida"),
            (11, 2, "Finados"),
            (11, 15, "Proclamação da República"),
            (12, 25, "Natal")
        ];

        foreach (var (month, day, name) in fixedDays)
        {
            yield return new GeneratedHoliday(new DateOnly(year, month, day), name, EHolidaySource.National);
        }

        if (year >= 2024)
        {
            yield return new GeneratedHoliday(
                new DateOnly(year, 11, 20), "Dia Nacional de Zumbi e da Consciência Negra", EHolidaySource.National);
        }
    }

    /// <summary>Feriados móveis, a partir da Páscoa (usado quando a BrasilAPI não responde).</summary>
    private static IEnumerable<GeneratedHoliday> MovableNational(int year)
    {
        var easter = EasterSunday(year);
        yield return new GeneratedHoliday(easter.AddDays(-48), "Carnaval", EHolidaySource.National);
        yield return new GeneratedHoliday(easter.AddDays(-47), "Carnaval", EHolidaySource.National);
        yield return new GeneratedHoliday(easter.AddDays(-2), "Sexta-feira Santa", EHolidaySource.National);
        yield return new GeneratedHoliday(easter, "Páscoa", EHolidaySource.National);
        yield return new GeneratedHoliday(easter.AddDays(60), "Corpus Christi", EHolidaySource.National);
    }

    /// <summary>Domingo de Páscoa (algoritmo de Meeus/Jones/Butcher, calendário gregoriano).</summary>
    public static DateOnly EasterSunday(int year)
    {
        int a = year % 19, b = year / 100, c = year % 100, d = b / 4, e = b % 4;
        int f = (b + 8) / 25, g = (b - f + 1) / 3, h = (19 * a + b - d - g + 15) % 30;
        int i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31, day = (h + l - 7 * m + 114) % 31 + 1;
        return new DateOnly(year, month, day);
    }

    private IEnumerable<GeneratedHoliday> Municipal(int year)
    {
        foreach (var item in settings.Value.Municipal)
        {
            if (DateOnly.TryParseExact($"{year}-{item.Date}", "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
            {
                yield return new GeneratedHoliday(date, item.Name, EHolidaySource.Municipal);
            }
            else
            {
                logger.LogWarning("Feriado municipal com data inválida em Holidays:Municipal: {Date}", item.Date);
            }
        }
    }

    /// <summary>Nacionais (com os móveis) e estaduais da UF configurada. Nulo se a API falhar.</summary>
    private async Task<List<GeneratedHoliday>?> FetchBrasilApi(int year, CancellationToken ct)
    {
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            var state = Uri.EscapeDataString(settings.Value.State);
            var items = await client.GetFromJsonAsync<List<BrasilApiHoliday>>($"api/feriados/v1/{year}?uf={state}", ct);
            if (items is not { Count: > 0 })
            {
                return null;
            }

            var result = new List<GeneratedHoliday>();
            foreach (var item in items)
            {
                if (!DateOnly.TryParseExact(item.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var date) || date.Year != year)
                {
                    continue;
                }

                var source = item.Type == "state" ? EHolidaySource.State : EHolidaySource.National;
                result.Add(new GeneratedHoliday(date, item.Name.Trim(), source));
            }

            return result.Count > 0 ? result : null;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException
                                       or NotSupportedException)
        {
            logger.LogWarning(ex, "BrasilAPI indisponível para os feriados de {Year}; usando o cálculo local.", year);
            return null;
        }
    }
}
