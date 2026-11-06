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
    /// Relatório de ocupação e ociosidade de um mês ou de uma sequência de meses já encerrados.
    /// Usa o relatório gravado quando existir; caso contrário calcula, grava e devolve.
    /// </summary>
    public Task<ReportResponseDTO> GetReport(ReportRequestDTO query);

    /// <summary>Relatório de uso dos recursos reserváveis, com as mesmas regras e cache do relatório de salas.</summary>
    public Task<ResourceReportResponseDTO> GetResourceReport(ReportRequestDTO query);
}

public class ReportService(AppDbContext dbCtx) : IReportService
{
    /// <summary>Suba este número quando mudar o cálculo ou o formato do DTO: os relatórios antigos são refeitos.</summary>
    private const int CacheVersion = 4;

    /// <summary>Limite de meses por consulta, para uma única requisição não varrer anos de reservas.</summary>
    private const int MaxRangeMonths = 60;

    /// <summary>O Brasil não tem mais horário de verão, então um deslocamento fixo evita depender do tzdata do servidor.</summary>
    private static readonly TimeSpan BrazilOffset = BrazilClock.Offset;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<ReportResponseDTO> GetReport(ReportRequestDTO query) =>
        GetCached(EReportKind.Spaces, query, BuildReport, report => report.GeneratedAt);

    public Task<ResourceReportResponseDTO> GetResourceReport(ReportRequestDTO query) =>
        GetCached(EReportKind.Resources, query, BuildResourceReport, report => report.GeneratedAt);

    /// <summary>
    /// Usa o relatório gravado do tipo e período quando existir (e estiver na versão atual); senão calcula e grava.
    /// </summary>
    private async Task<T> GetCached<T>(
        EReportKind kind,
        ReportRequestDTO query,
        Func<DateOnly, DateOnly, DateOnly, Task<T>> build,
        Func<T, DateTimeOffset> generatedAt) where T : class
    {
        var (rangeStart, lastMonth) = ResolveRange(query);

        var today = DateOnly.FromDateTime(DateTime.UtcNow + BrazilOffset);

        // Sem o mês fechado os dados ainda mudam (confirmações, cancelamentos, novas reservas).
        if (lastMonth >= new DateOnly(today.Year, today.Month, 1))
        {
            throw new BadRequestException("O relatório só está disponível para meses já encerrados.");
        }

        var stored = await dbCtx.Reports.FirstOrDefaultAsync(r =>
            r.Kind == kind
            && r.FromYear == rangeStart.Year && r.FromMonth == rangeStart.Month
            && r.ToYear == lastMonth.Year && r.ToMonth == lastMonth.Month);

        if (stored != null && !query.Refresh && stored.Version == CacheVersion)
        {
            var cached = TryDeserialize<T>(stored.Data);
            if (cached != null)
            {
                return cached;
            }
        }

        var report = await build(rangeStart, lastMonth, today);
        var json = JsonSerializer.Serialize(report, JsonOptions);

        if (stored == null)
        {
            dbCtx.Reports.Add(new ReportModel
            {
                Kind = kind,
                FromYear = rangeStart.Year,
                FromMonth = rangeStart.Month,
                ToYear = lastMonth.Year,
                ToMonth = lastMonth.Month,
                Version = CacheVersion,
                GeneratedAt = generatedAt(report),
                Data = json
            });
        }
        else
        {
            stored.Version = CacheVersion;
            stored.GeneratedAt = generatedAt(report);
            stored.Data = json;
        }

        try
        {
            await dbCtx.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (stored == null
            && ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Duas requisições calcularam o mesmo período ao mesmo tempo e a outra gravou primeiro.
            // O relatório calculado aqui continua válido; só não precisa ser gravado de novo.
        }

        return report;
    }

    /// <summary>
    /// Valida o período pedido e devolve o primeiro dia do mês inicial e o primeiro dia do mês final.
    /// Sem fim informado, o período é só o mês inicial.
    /// </summary>
    private static (DateOnly FirstMonth, DateOnly LastMonth) ResolveRange(ReportRequestDTO query)
    {
        if (query.FromYear < 2000 || query.FromYear > 9999 || query.FromMonth < 1 || query.FromMonth > 12)
        {
            throw new BadRequestException("Mês ou ano inicial inválido.");
        }

        if (query.ToYear.HasValue != query.ToMonth.HasValue)
        {
            throw new BadRequestException("Informe o ano e o mês finais juntos, ou nenhum dos dois.");
        }

        var toYear = query.ToYear ?? query.FromYear;
        var toMonth = query.ToMonth ?? query.FromMonth;

        if (toYear < 2000 || toYear > 9999 || toMonth < 1 || toMonth > 12)
        {
            throw new BadRequestException("Mês ou ano final inválido.");
        }

        var first = new DateOnly(query.FromYear, query.FromMonth, 1);
        var last = new DateOnly(toYear, toMonth, 1);

        if (last < first)
        {
            throw new BadRequestException("O mês final não pode ser anterior ao mês inicial.");
        }

        var months = (last.Year - first.Year) * 12 + last.Month - first.Month + 1;
        if (months > MaxRangeMonths)
        {
            throw new BadRequestException($"O período do relatório pode ter no máximo {MaxRangeMonths} meses.");
        }

        return (first, last);
    }

    private static T? TryDeserialize<T>(string json) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            // Dado gravado ilegível: trata como se não existisse e recalcula.
            return null;
        }
    }

    private async Task<ReportResponseDTO> BuildReport(DateOnly rangeStart, DateOnly lastMonth, DateOnly today)
    {
        // Do primeiro dia do mês inicial ao último dia do mês final.
        var rangeEnd = lastMonth.AddMonths(1).AddDays(-1);
        var daysInRange = rangeEnd.DayNumber - rangeStart.DayNumber + 1;

        // Reservas que tocam o período. Canceladas, recusadas e bloqueadas nunca entram.
        var rows = await dbCtx.SpaceReserves.AsNoTracking()
            .Where(sr => sr.DateFrom <= rangeEnd && sr.DateTo >= rangeStart)
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

        var shifts = await LoadScheduleShifts();
        var sessions = await LoadSessions(rangeStart, rangeEnd);

        var counted = new List<ReportCountedReserve>();
        foreach (var row in rows)
        {
            // Só os dias que caem dentro do período contam para a ocupação dele.
            var (slotDays, absentSlotDays) = Measure(
                (row.Id, null), row.Status, row.DateFrom, row.DateTo, row.ScheduleIds,
                rangeStart, rangeEnd, today, shifts, sessions);
            if (slotDays == 0)
            {
                continue;
            }

            counted.Add(new ReportCountedReserve(row, StatusOf(slotDays, absentSlotDays), slotDays, absentSlotDays));
        }

        // Capacidade de cada espaço no período = horários por dia x dias do período. Horários desativados depois
        // que a reserva foi feita continuam valendo para quem os usou, então entram na conta.
        var slotsPerDay = enabledScheduleIds
            .Concat(counted.SelectMany(c => c.Row.ScheduleIds))
            .Distinct()
            .Count();
        double capacity = (double)slotsPerDay * daysInRange;

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
                var absentSlotDays = items.Sum(i => i.AbsentSlotDays);

                return new ReportSpaceStatDTO(
                    space.Id,
                    space.Name,
                    Percent(slotDays, capacity),
                    items.Count,
                    items.Count - absent,
                    absent,
                    // Ausência = horários ausentes / horários reservados (reserva maior pesa mais).
                    Percent(absentSlotDays, slotDays));
            })
            .ToList();

        // Empates: cada ranking devolve todos que empatam no critério (comparado já arredondado, como aparece na tela),
        // sempre ordenados pelo nome para o mesmo período dar sempre o mesmo resultado.
        var highestDemand = TopTied(
            spaceStats.Where(s => s.OccupancyRate > 0),
            s => s.OccupancyRate,
            s => s.Name);

        // Quando todos os espaços empatam na mesma taxa não existe um "menos disputado" diferente do "mais disputado".
        var lowestDemand = spaceStats.Count < 2
            ? new List<ReportSpaceStatDTO>()
            : TopTied(spaceStats, s => s.OccupancyRate, s => s.Name, descending: false);
        if (highestDemand.Count > 0 && lowestDemand.Count > 0
            && lowestDemand[0].OccupancyRate >= highestDemand[0].OccupancyRate)
        {
            lowestDemand = [];
        }

        var mostIdleSpaces = TopTied(
            spaceStats.Where(s => s.AbsentReserves > 0),
            s => (s.AbsentReserves, s.IdleRate),
            s => s.Name);

        var userStats = counted
            .GroupBy(c => c.Row.AccountId)
            .Select(group =>
            {
                var first = group.First().Row;
                var total = group.Count();
                var absent = group.Count(i => i.Status == EReportReserveStatus.Absent);
                var slots = group.Sum(i => i.SlotDays);
                var absentSlots = group.Sum(i => i.AbsentSlotDays);

                return new ReportUserStatDTO(
                    group.Key,
                    first.UserName ?? "Sem nome",
                    first.Registration,
                    total,
                    total - absent,
                    absent,
                    Percent(absentSlots, slots));
            })
            .ToList();

        var mostReservesUsers = TopTied(userStats, u => u.TotalReserves, u => u.Name);

        var mostIdleUsers = TopTied(
            userStats.Where(u => u.AbsentReserves > 0),
            u => (u.AbsentReserves, u.IdleRate),
            u => u.Name);

        var totalAbsent = counted.Count(c => c.Status == EReportReserveStatus.Absent);
        var totalSlotDays = counted.Sum(c => c.SlotDays);
        var absentSlotDaysTotal = counted.Sum(c => c.AbsentSlotDays);
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
            rangeStart.Year,
            rangeStart.Month,
            lastMonth.Year,
            lastMonth.Month,
            DateTimeOffset.UtcNow,
            new ReportSummaryDTO(
                averageOccupancy,
                Percent(absentSlotDaysTotal, totalSlotDays),
                counted.Count,
                counted.Count - totalAbsent,
                totalAbsent,
                highestDemand),
            new ReportRankingDTO(highestDemand, lowestDemand, mostIdleSpaces),
            new ReportUsersDTO(mostReservesUsers, mostIdleUsers),
            reserves);
    }

    private async Task<ResourceReportResponseDTO> BuildResourceReport(DateOnly rangeStart, DateOnly lastMonth, DateOnly today)
    {
        var rangeEnd = lastMonth.AddMonths(1).AddDays(-1);
        var daysInRange = rangeEnd.DayNumber - rangeStart.DayNumber + 1;

        // Mesmas regras das salas: só pendentes e confirmadas entram (canceladas e recusadas nunca).
        var rows = await dbCtx.ResourceReserves.AsNoTracking()
            .Where(rr => rr.DateFrom <= rangeEnd && rr.DateTo >= rangeStart)
            .Where(rr => rr.Status == EReserveStatus.Booking || rr.Status == EReserveStatus.Confirmed)
            .Where(rr => rr.Resource.Reservable)
            .Select(rr => new ReportResourceReserveRow(
                rr.Id,
                rr.FkResourceId,
                rr.Resource.Name,
                rr.FkAccountId,
                rr.Account.UserName,
                rr.Account.Registration,
                rr.Quantity,
                rr.SpaceReserve == null ? null : rr.SpaceReserve.Space.Name,
                rr.FkSpaceReserveId,
                rr.DateFrom,
                rr.DateTo,
                rr.Status,
                rr.ResourceReserveSchedules.Select(s => s.FkScheduleId).ToList()))
            .AsSplitQuery()
            .ToListAsync();

        var resources = await dbCtx.Resources.AsNoTracking()
            .Where(r => r.Reservable && r.Quantity != null)
            .Select(r => new ReportResourceRow(r.Id, r.Name, r.Quantity!.Value, r.Enabled))
            .ToListAsync();

        var enabledScheduleIds = await dbCtx.Schedules.AsNoTracking()
            .Where(s => s.Enabled)
            .Select(s => s.Id)
            .ToListAsync();

        var shifts = await LoadScheduleShifts();
        var sessions = await LoadSessions(rangeStart, rangeEnd);

        var counted = new List<ReportCountedResourceReserve>();
        foreach (var row in rows)
        {
            // Vinculada: a presença é a da sessão da sala.
            var key = row.SpaceReserveId is { } spaceReserveId ? (spaceReserveId, (long?)null) : (0L, row.Id);
            var (slotDays, absentSlotDays) = Measure(
                key, row.Status, row.DateFrom, row.DateTo, row.ScheduleIds,
                rangeStart, rangeEnd, today, shifts, sessions);
            if (slotDays == 0)
            {
                continue;
            }

            // unidades x horários x dias dentro do período
            counted.Add(new ReportCountedResourceReserve(
                row, StatusOf(slotDays, absentSlotDays), row.Quantity * slotDays, row.Quantity * absentSlotDays));
        }

        var slotsPerDay = enabledScheduleIds
            .Concat(counted.SelectMany(c => c.Row.ScheduleIds))
            .Distinct()
            .Count();

        // Recurso desativado só aparece se teve reserva contabilizada (como o espaço bloqueado no relatório de salas).
        var countedResourceIds = counted.Select(c => c.Row.ResourceId).ToHashSet();
        var byResource = counted.ToLookup(c => c.Row.ResourceId);

        var resourceStats = resources
            .Where(r => r.Enabled || countedResourceIds.Contains(r.Id))
            .Select(resource =>
            {
                var items = byResource[resource.Id].ToList();
                var absent = items.Count(i => i.Status == EReportReserveStatus.Absent);
                var unitSlotDays = items.Sum(i => i.UnitSlotDays);
                var absentUnitSlotDays = items.Sum(i => i.AbsentUnitSlotDays);
                double capacity = (double)resource.Quantity * slotsPerDay * daysInRange;

                return new ReportResourceStatDTO(
                    resource.Id,
                    resource.Name,
                    resource.Quantity,
                    Percent(unitSlotDays, capacity),
                    items.Count,
                    items.Count - absent,
                    absent,
                    items.Sum(i => i.Row.Quantity),
                    // Ausência = unidades x horários ausentes / unidades x horários reservados.
                    Percent(absentUnitSlotDays, unitSlotDays));
            })
            .ToList();

        var highestDemand = TopTied(
            resourceStats.Where(r => r.UtilizationRate > 0),
            r => r.UtilizationRate,
            r => r.Name);

        var lowestDemand = resourceStats.Count < 2
            ? new List<ReportResourceStatDTO>()
            : TopTied(resourceStats, r => r.UtilizationRate, r => r.Name, descending: false);
        if (highestDemand.Count > 0 && lowestDemand.Count > 0
            && lowestDemand[0].UtilizationRate >= highestDemand[0].UtilizationRate)
        {
            lowestDemand = [];
        }

        var mostIdleResources = TopTied(
            resourceStats.Where(r => r.AbsentReserves > 0),
            r => (r.AbsentReserves, r.IdleRate),
            r => r.Name);

        var userStats = counted
            .GroupBy(c => c.Row.AccountId)
            .Select(group =>
            {
                var first = group.First().Row;
                var total = group.Count();
                var absent = group.Count(i => i.Status == EReportReserveStatus.Absent);
                var slots = group.Sum(i => i.UnitSlotDays);
                var absentSlots = group.Sum(i => i.AbsentUnitSlotDays);

                return new ReportUserStatDTO(
                    group.Key,
                    first.UserName ?? "Sem nome",
                    first.Registration,
                    total,
                    total - absent,
                    absent,
                    Percent(absentSlots, slots));
            })
            .ToList();

        var mostReservesUsers = TopTied(userStats, u => u.TotalReserves, u => u.Name);
        var mostIdleUsers = TopTied(
            userStats.Where(u => u.AbsentReserves > 0),
            u => (u.AbsentReserves, u.IdleRate),
            u => u.Name);

        var totalAbsent = counted.Count(c => c.Status == EReportReserveStatus.Absent);
        var totalUnitSlotDays = counted.Sum(c => c.UnitSlotDays);
        var absentUnitSlotDaysTotal = counted.Sum(c => c.AbsentUnitSlotDays);
        var linked = counted.Count(c => c.Row.SpaceName != null);
        var averageUtilization = resourceStats.Count == 0
            ? 0
            : Math.Round(resourceStats.Average(r => r.UtilizationRate), 1, MidpointRounding.AwayFromZero);

        var reserves = counted
            .OrderBy(c => c.Row.DateFrom)
            .ThenBy(c => c.Row.ResourceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Row.Id)
            .Select(c => new ResourceReportReserveDTO(
                c.Row.Id,
                c.Row.ResourceName,
                c.Row.UserName ?? "Sem nome",
                c.Row.Quantity,
                c.Row.SpaceName,
                c.Row.DateFrom,
                c.Row.DateTo,
                c.Status))
            .ToList();

        return new ResourceReportResponseDTO(
            rangeStart.Year,
            rangeStart.Month,
            lastMonth.Year,
            lastMonth.Month,
            DateTimeOffset.UtcNow,
            new ResourceReportSummaryDTO(
                averageUtilization,
                Percent(absentUnitSlotDaysTotal, totalUnitSlotDays),
                counted.Count,
                counted.Count - totalAbsent,
                totalAbsent,
                linked,
                counted.Count - linked,
                highestDemand),
            new ResourceReportRankingDTO(highestDemand, lowestDemand, mostIdleResources),
            new ReportUsersDTO(mostReservesUsers, mostIdleUsers),
            reserves);
    }

    /// <summary>Turno de cada horário (inclusive desativados).</summary>
    private Task<Dictionary<long, EShift>> LoadScheduleShifts() =>
        dbCtx.Schedules.AsNoTracking().ToDictionaryAsync(s => s.Id, s => s.Shift);

    /// <summary>
    /// Sessões de uso do período por (reserva de sala, reserva de recurso avulsa, dia, turno). A chave usa 0 / null
    /// no lado que não se aplica.
    /// </summary>
    private async Task<Dictionary<(long SpaceReserveId, long? ResourceReserveId, DateOnly Day, EShift Shift), EUsageStatus>>
        LoadSessions(DateOnly start, DateOnly end)
    {
        var rows = await dbCtx.ReserveUsages.AsNoTracking()
            .Where(u => u.Date >= start && u.Date <= end)
            .Select(u => new { u.FkSpaceReserveId, u.FkResourceReserveId, u.Date, u.Shift, u.Status })
            .ToListAsync();

        return rows.ToDictionary(
            u => (u.FkSpaceReserveId ?? 0L, u.FkSpaceReserveId == null ? u.FkResourceReserveId : null, u.Date, u.Shift),
            u => u.Status);
    }

    /// <summary>
    /// Horários x dias de uma reserva dentro do período e quantos deles foram ausências.
    /// Com sessão de uso (check-in) naquele dia e turno: check-in feito = presente; ausente ou nunca iniciada = ausência.
    /// Sem sessão (dias anteriores ao check-in existir): regra antiga por reserva, ver <see cref="Classify"/>.
    /// </summary>
    private static (int SlotDays, int AbsentSlotDays) Measure(
        (long SpaceReserveId, long? ResourceReserveId) key,
        EReserveStatus status,
        DateOnly dateFrom,
        DateOnly dateTo,
        List<long> scheduleIds,
        DateOnly rangeStart,
        DateOnly rangeEnd,
        DateOnly today,
        IReadOnlyDictionary<long, EShift> shifts,
        IReadOnlyDictionary<(long, long?, DateOnly, EShift), EUsageStatus> sessions)
    {
        var fallback = Classify(status, dateFrom, today);
        var from = dateFrom > rangeStart ? dateFrom : rangeStart;
        var to = dateTo < rangeEnd ? dateTo : rangeEnd;
        int slotDays = 0, absentSlotDays = 0;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            foreach (var scheduleId in scheduleIds)
            {
                bool? present = null;
                if (shifts.TryGetValue(scheduleId, out var shift)
                    && sessions.TryGetValue((key.SpaceReserveId, key.ResourceReserveId, day, shift), out var usage))
                {
                    present = UsageRules.IsPresent(usage);
                }
                else if (fallback != null)
                {
                    present = fallback == EReportReserveStatus.Confirmed;
                }

                if (present == null)
                {
                    continue;
                }

                slotDays++;
                if (present == false)
                {
                    absentSlotDays++;
                }
            }
        }

        return (slotDays, absentSlotDays);
    }

    /// <summary>A reserva só aparece como "Ausente" na lista quando não houve presença em nenhum horário.</summary>
    private static EReportReserveStatus StatusOf(int slotDays, int absentSlotDays) =>
        absentSlotDays == slotDays ? EReportReserveStatus.Absent : EReportReserveStatus.Confirmed;

    /// <summary>
    /// Regra antiga (dias sem sessão de uso):
    /// confirmada = Confirmada;
    /// em "Booking" cuja data de início já passou = Ausente (a confirmação deveria ter chegado antes da reserva);
    /// em "Booking" que ainda não chegou ao dia = fora do relatório (retorna null).
    /// </summary>
    private static EReportReserveStatus? Classify(EReserveStatus status, DateOnly dateFrom, DateOnly today)
    {
        if (status == EReserveStatus.Confirmed)
        {
            return EReportReserveStatus.Confirmed;
        }

        return dateFrom < today ? EReportReserveStatus.Absent : null;
    }

    /// <summary>
    /// Todos os itens que empatam no melhor valor de <paramref name="key"/> (o maior, ou o menor com
    /// <paramref name="descending"/> falso), em ordem alfabética. Lista vazia quando não há itens.
    /// </summary>
    private static List<T> TopTied<T, TKey>(
        IEnumerable<T> items,
        Func<T, TKey> key,
        Func<T, string> name,
        bool descending = true) where TKey : IComparable<TKey>
    {
        var list = items.ToList();
        if (list.Count == 0)
        {
            return [];
        }

        var best = descending ? list.Max(key)! : list.Min(key)!;

        return list
            .Where(item => key(item).CompareTo(best) == 0)
            .OrderBy(name, StringComparer.OrdinalIgnoreCase)
            .ToList();
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

/// <param name="SlotDays">Horários ocupados no período: dias da reserva dentro do período x horários da reserva.</param>
/// <param name="AbsentSlotDays">Desses, os que ficaram sem check-in (ausência).</param>
internal sealed record ReportCountedReserve(
    ReportReserveRow Row, EReportReserveStatus Status, int SlotDays, int AbsentSlotDays);

internal sealed record ReportResourceReserveRow(
    long Id,
    long ResourceId,
    string ResourceName,
    string AccountId,
    string? UserName,
    string Registration,
    int Quantity,
    string? SpaceName,
    long? SpaceReserveId,
    DateOnly DateFrom,
    DateOnly DateTo,
    EReserveStatus Status,
    List<long> ScheduleIds);

internal sealed record ReportResourceRow(long Id, string Name, int Quantity, bool Enabled);

/// <summary>
/// <c>UnitSlotDays</c>: unidades x horários x dias da reserva dentro do período;
/// <c>AbsentUnitSlotDays</c>: a parte deles sem check-in.
/// </summary>
internal sealed record ReportCountedResourceReserve(
    ReportResourceReserveRow Row, EReportReserveStatus Status, int UnitSlotDays, int AbsentUnitSlotDays);
