using System.Text.Json;
using Backend.Handler;
using Data;
using DTOs.Report;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Report;
using Npgsql;

namespace Backend.Services;

public interface IReportService
{
    /// <summary>
    /// Relatório de ocupação e ociosidade de um mês já encerrado. Usa o relatório gravado quando existir;
    /// caso contrário calcula, grava e devolve.
    /// </summary>
    public Task<ReportResponseDTO> GetMonthlyReport(ReportRequestDTO query);
}

public class ReportService(AppDbContext dbCtx) : IReportService
{
    /// <summary>Suba este número quando mudar o cálculo ou o formato do DTO: os relatórios antigos são refeitos.</summary>
    private const int CacheVersion = 1;

    /// <summary>O Brasil não tem mais horário de verão, então um deslocamento fixo evita depender do tzdata do servidor.</summary>
    private static readonly TimeSpan BrazilOffset = TimeSpan.FromHours(-3);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ReportResponseDTO> GetMonthlyReport(ReportRequestDTO query)
    {
        if (query.Year < 2000 || query.Year > 9999 || query.Month < 1 || query.Month > 12)
        {
            throw new BadRequestException("Mês ou ano inválido.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow + BrazilOffset);
        var monthStart = new DateOnly(query.Year, query.Month, 1);

        // Sem o mês fechado os dados ainda mudam (confirmações, cancelamentos, novas reservas).
        if (monthStart >= new DateOnly(today.Year, today.Month, 1))
        {
            throw new BadRequestException("O relatório só está disponível para meses já encerrados.");
        }

        var stored = await dbCtx.Reports
            .FirstOrDefaultAsync(r => r.Year == query.Year && r.Month == query.Month);

        if (stored != null && !query.Refresh && stored.Version == CacheVersion)
        {
            var cached = TryDeserialize(stored.Data);
            if (cached != null)
            {
                return cached;
            }
        }

        var report = await BuildReport(monthStart, today);
        var json = JsonSerializer.Serialize(report, JsonOptions);

        if (stored == null)
        {
            dbCtx.Reports.Add(new ReportModel
            {
                Year = query.Year,
                Month = query.Month,
                Version = CacheVersion,
                GeneratedAt = report.GeneratedAt,
                Data = json
            });
        }
        else
        {
            stored.Version = CacheVersion;
            stored.GeneratedAt = report.GeneratedAt;
            stored.Data = json;
        }

        try
        {
            await dbCtx.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (stored == null
            && ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Duas requisições calcularam o mesmo mês ao mesmo tempo e a outra gravou primeiro.
            // O relatório calculado aqui continua válido; só não precisa ser gravado de novo.
        }

        return report;
    }

    private static ReportResponseDTO? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ReportResponseDTO>(json, JsonOptions);
        }
        catch (JsonException)
        {
            // Dado gravado ilegível: trata como se não existisse e recalcula.
            return null;
        }
    }

    private async Task<ReportResponseDTO> BuildReport(DateOnly monthStart, DateOnly today)
    {
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        var daysInMonth = monthEnd.Day;

        // Reservas que tocam o mês. Canceladas, recusadas e bloqueadas nunca entram.
        var rows = await dbCtx.SpaceReserves.AsNoTracking()
            .Where(sr => sr.DateFrom <= monthEnd && sr.DateTo >= monthStart)
            .Where(sr => sr.Status == EReserveStatus.Booking || sr.Status == EReserveStatus.Confirmed)
            .Select(sr => new ReportReserveRow(
                sr.Id,
                sr.FkSpaceId,
                sr.Space.Name,
                sr.FkAccountId,
                sr.Account.UserName,
                sr.Account.Registration,
                sr.DateFrom,
                sr.DateTo,
                sr.Status,
                sr.SpaceReserveSchedules.Select(s => s.FkScheduleId).ToList()))
            .ToListAsync();

        var spaces = await dbCtx.Spaces.AsNoTracking()
            .Select(s => new ReportSpaceRow(s.Id, s.Name, s.Locked))
            .ToListAsync();

        var enabledScheduleIds = await dbCtx.Schedules.AsNoTracking()
            .Where(s => s.Enabled)
            .Select(s => s.Id)
            .ToListAsync();

        var counted = new List<ReportCountedReserve>();
        foreach (var row in rows)
        {
            var status = Classify(row, today);
            if (status == null)
            {
                continue;
            }

            // Só os dias que caem dentro do mês contam para a ocupação daquele mês.
            var from = row.DateFrom > monthStart ? row.DateFrom : monthStart;
            var to = row.DateTo < monthEnd ? row.DateTo : monthEnd;
            var days = Math.Max(0, to.DayNumber - from.DayNumber + 1);

            counted.Add(new ReportCountedReserve(row, status.Value, days * row.ScheduleIds.Count));
        }

        // Capacidade de cada espaço no mês = horários por dia x dias do mês. Horários desativados depois
        // que a reserva foi feita continuam valendo para quem os usou, então entram na conta.
        var slotsPerDay = enabledScheduleIds
            .Concat(counted.SelectMany(c => c.Row.ScheduleIds))
            .Distinct()
            .Count();
        double capacity = (double)slotsPerDay * daysInMonth;

        // Espaço bloqueado só aparece se teve reserva contabilizada; os demais entram mesmo sem reservas
        // (0% de ocupação), senão o "menos disputado" nunca seria um espaço parado.
        var countedSpaceIds = counted.Select(c => c.Row.SpaceId).ToHashSet();
        var bySpace = counted.ToLookup(c => c.Row.SpaceId);

        var spaceStats = spaces
            .Where(s => !s.Locked || countedSpaceIds.Contains(s.Id))
            .Select(space =>
            {
                var items = bySpace[space.Id].ToList();
                var absent = items.Count(i => i.Status == EReportReserveStatus.Absent);
                var slotDays = items.Sum(i => i.SlotDays);

                return new ReportSpaceStatDTO(
                    space.Id,
                    space.Name,
                    Percent(slotDays, capacity),
                    items.Count,
                    items.Count - absent,
                    absent,
                    Percent(absent, items.Count));
            })
            .ToList();

        // Desempates: sempre por critérios determinísticos, para o mesmo mês dar sempre o mesmo resultado.
        var highestDemand = spaceStats
            .Where(s => s.OccupancyRate > 0)
            .OrderByDescending(s => s.OccupancyRate)
            .ThenByDescending(s => s.TotalReserves)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        // O desempate final inverso garante que "menos" e "mais" disputado nunca sejam o mesmo espaço
        // quando há pelo menos dois espaços empatados em tudo.
        var lowestDemand = spaceStats.Count < 2
            ? null
            : spaceStats
                .OrderBy(s => s.OccupancyRate)
                .ThenBy(s => s.TotalReserves)
                .ThenByDescending(s => s.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

        var mostIdleSpace = spaceStats
            .Where(s => s.AbsentReserves > 0)
            .OrderByDescending(s => s.AbsentReserves)
            .ThenByDescending(s => s.IdleRate)
            .ThenBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var userStats = counted
            .GroupBy(c => c.Row.AccountId)
            .Select(group =>
            {
                var first = group.First().Row;
                var total = group.Count();
                var absent = group.Count(i => i.Status == EReportReserveStatus.Absent);

                return new ReportUserStatDTO(
                    group.Key,
                    first.UserName ?? "Sem nome",
                    first.Registration,
                    total,
                    total - absent,
                    absent,
                    Percent(absent, total));
            })
            .ToList();

        var mostReservesUser = userStats
            .OrderByDescending(u => u.TotalReserves)
            .ThenBy(u => u.IdleRate)
            .ThenBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var mostIdleUser = userStats
            .Where(u => u.AbsentReserves > 0)
            .OrderByDescending(u => u.AbsentReserves)
            .ThenByDescending(u => u.IdleRate)
            .ThenBy(u => u.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        var totalAbsent = counted.Count(c => c.Status == EReportReserveStatus.Absent);
        var averageOccupancy = spaceStats.Count == 0
            ? 0
            : Math.Round(spaceStats.Average(s => s.OccupancyRate), 1, MidpointRounding.AwayFromZero);

        var reserves = counted
            .OrderBy(c => c.Row.DateFrom)
            .ThenBy(c => c.Row.SpaceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Row.Id)
            .Select(c => new ReportReserveDTO(
                c.Row.Id,
                c.Row.SpaceName,
                c.Row.UserName ?? "Sem nome",
                c.Row.DateFrom,
                c.Row.DateTo,
                c.Status))
            .ToList();

        return new ReportResponseDTO(
            monthStart.Year,
            monthStart.Month,
            DateTimeOffset.UtcNow,
            new ReportSummaryDTO(
                averageOccupancy,
                Percent(totalAbsent, counted.Count),
                counted.Count,
                counted.Count - totalAbsent,
                totalAbsent,
                highestDemand),
            new ReportRankingDTO(highestDemand, lowestDemand, mostIdleSpace),
            new ReportUsersDTO(mostReservesUser, mostIdleUser),
            reserves);
    }

    /// <summary>
    /// Regras do relatório:
    /// confirmada = Confirmada;
    /// em "Booking" cuja data de início já passou = Ausente (a confirmação deveria ter chegado antes da reserva);
    /// em "Booking" que ainda não chegou ao dia = fora do relatório (retorna null).
    /// </summary>
    private static EReportReserveStatus? Classify(ReportReserveRow row, DateOnly today)
    {
        if (row.Status == EReserveStatus.Confirmed)
        {
            return EReportReserveStatus.Confirmed;
        }

        return row.DateFrom < today ? EReportReserveStatus.Absent : null;
    }

    /// <summary>Porcentagem de 0 a 100 com uma casa decimal (0 quando o total é zero).</summary>
    private static double Percent(double part, double total)
    {
        if (total <= 0)
        {
            return 0;
        }

        return Math.Round(Math.Min(part / total * 100, 100), 1, MidpointRounding.AwayFromZero);
    }
}

internal sealed record ReportReserveRow(
    long Id,
    long SpaceId,
    string SpaceName,
    string AccountId,
    string? UserName,
    string Registration,
    DateOnly DateFrom,
    DateOnly DateTo,
    EReserveStatus Status,
    List<long> ScheduleIds);

internal sealed record ReportSpaceRow(long Id, string Name, bool Locked);

/// <param name="SlotDays">Horários ocupados no mês: dias da reserva dentro do mês x horários da reserva.</param>
internal sealed record ReportCountedReserve(ReportReserveRow Row, EReportReserveStatus Status, int SlotDays);
