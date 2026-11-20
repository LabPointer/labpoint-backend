using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Usage;

namespace Backend.Services;

/// <summary>
/// Um ciclo do check-in/check-out (roda a cada minuto pelo <see cref="ReserveUsageWorker"/>):
/// cria as sessões do dia, marca ausências e marca check-outs vencidos. Cada passo é idempotente: rodar de novo no
/// mesmo minuto não duplica nada. Os e-mails com os links de check-in/check-out são do serviço Email
/// (projeto <c>Email</c>), que lê as sessões criadas aqui.
/// </summary>
public class ReserveUsageScheduler(AppDbContext dbCtx, ICalendarService calendar)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = BrazilClock.UtcNow;
        var today = BrazilClock.Today;

        await MaterializeSessions(today, now, ct);
        await MarkAbsent(now, ct);
        await MarkCheckoutPending(now, ct);
    }

    private sealed record ExpectedSession(
        long? SpaceReserveId, long? ResourceReserveId, string AccountId, EShift Shift, List<ShiftSlot> Slots);

    /// <summary>
    /// Uma sessão por (reserva ativa que cobre hoje, turno com horários). Reservas de recurso vinculadas não têm sessão
    /// própria. Sessões ainda não iniciadas cuja reserva foi cancelada ou mudou de turno/dia são removidas; janelas são
    /// recalculadas se os horários mudaram.
    /// </summary>
    private async Task MaterializeSessions(DateOnly today, DateTimeOffset now, CancellationToken ct)
    {
        // Fim de semana ou feriado sem liberação: a reserva pula o dia (sem sessão, sem e-mail, sem ausência).
        // Gera também os feriados do ano seguinte com antecedência (sem custo depois da primeira vez: fica em cache).
        var blocked = await calendar.Load(today, today, ct);
        await calendar.Load(today.AddYears(1), today.AddYears(1), ct);

        var spaceRows = await dbCtx.SpaceReserves.AsNoTracking()
            .Where(sr => sr.DateFrom <= today && sr.DateTo >= today)
            .Where(ReserveRules.IsSpaceReserveActive)
            .Select(sr => new
            {
                sr.Id,
                sr.FkAccountId,
                sr.CreatedAt,
                Slots = sr.SpaceReserveSchedules
                    .Select(s => new ShiftSlot(s.FkScheduleId, s.Schedule.StartAt, s.Schedule.EndAt, s.Schedule.Shift))
                    .ToList()
            })
            .AsSplitQuery()
            .ToListAsync(ct);

        var resourceRows = await dbCtx.ResourceReserves.AsNoTracking()
            .Where(rr => rr.FkSpaceReserveId == null && rr.DateFrom <= today && rr.DateTo >= today)
            .Where(ResourceAvailability.IsActive)
            .Select(rr => new
            {
                rr.Id,
                rr.FkAccountId,
                rr.CreatedAt,
                Slots = rr.ResourceReserveSchedules
                    .Select(s => new ShiftSlot(s.FkScheduleId, s.Schedule.StartAt, s.Schedule.EndAt, s.Schedule.Shift))
                    .ToList()
            })
            .AsSplitQuery()
            .ToListAsync(ct);

        var expected = spaceRows
            .Where(r => blocked.IsUsable(today, r.CreatedAt))
            .SelectMany(r => r.Slots.GroupBy(s => s.Shift)
                .Select(g => new ExpectedSession(r.Id, null, r.FkAccountId, g.Key, g.ToList())))
            .Concat(resourceRows.Where(r => blocked.IsUsable(today, r.CreatedAt)).SelectMany(r => r.Slots.GroupBy(s => s.Shift)
                .Select(g => new ExpectedSession(null, r.Id, r.FkAccountId, g.Key, g.ToList()))))
            .ToList();

        var existing = await dbCtx.ReserveUsages
            .Where(u => u.Date == today)
            .ToListAsync(ct);

        var byKey = existing.ToDictionary(u => (u.FkSpaceReserveId, u.FkResourceReserveId, u.Shift));
        var expectedKeys = new HashSet<(long?, long?, EShift)>();

        foreach (var item in expected)
        {
            var key = (item.SpaceReserveId, item.ResourceReserveId, item.Shift);
            expectedKeys.Add(key);
            var windows = UsageRules.ComputeWindows(today, item.Slots);

            if (!byKey.TryGetValue(key, out var session))
            {
                // O turno já acabou (ex.: serviço fora do ar): não cria sessão; o relatório usa a regra antiga.
                if (now > windows.CheckOutClosesAt)
                {
                    continue;
                }

                dbCtx.ReserveUsages.Add(new ReserveUsageModel
                {
                    FkSpaceReserveId = item.SpaceReserveId,
                    FkResourceReserveId = item.ResourceReserveId,
                    FkAccountId = item.AccountId,
                    Date = today,
                    Shift = item.Shift,
                    Status = EUsageStatus.Scheduled,
                    CheckInOpensAt = windows.CheckInOpensAt,
                    CheckInClosesAt = LateClose(windows.CheckInClosesAt, now),
                    CheckOutOpensAt = windows.CheckOutOpensAt,
                    CheckOutClosesAt = windows.CheckOutClosesAt
                });
                continue;
            }

            // Horários da reserva mudaram no mesmo turno: as janelas acompanham.
            if (session.Status is EUsageStatus.Scheduled or EUsageStatus.CheckedIn
                && (session.CheckInOpensAt != windows.CheckInOpensAt
                    || session.CheckOutOpensAt != windows.CheckOutOpensAt
                    || session.CheckOutClosesAt != windows.CheckOutClosesAt))
            {
                if (session.Status == EUsageStatus.Scheduled)
                {
                    session.CheckInOpensAt = windows.CheckInOpensAt;
                    session.CheckInClosesAt = LateClose(windows.CheckInClosesAt, now);
                }

                session.CheckOutOpensAt = windows.CheckOutOpensAt;
                session.CheckOutClosesAt = windows.CheckOutClosesAt;
            }
        }

        // Só sessões ainda não iniciadas somem; as com check-in ou ausência ficam como registro.
        var stale = existing
            .Where(u => u.Status == EUsageStatus.Scheduled
                        && !expectedKeys.Contains((u.FkSpaceReserveId, u.FkResourceReserveId, u.Shift)))
            .ToList();
        dbCtx.ReserveUsages.RemoveRange(stale);

        await SaveIgnoringDuplicates(ct);
    }

    /// <summary>Reserva feita depois do prazo de check-in ainda ganha uma folga para fazer o check-in.</summary>
    private static DateTimeOffset LateClose(DateTimeOffset closesAt, DateTimeOffset now) =>
        now > closesAt ? now + UsageRules.LateCheckInGrace : closesAt;

    /// <summary>Sem check-in até o fim da janela: ausente (os horários do turno deixam de ocupar a sala/recursos).</summary>
    private Task MarkAbsent(DateTimeOffset now, CancellationToken ct) =>
        dbCtx.ReserveUsages
            .Where(u => u.Status == EUsageStatus.Scheduled && u.CheckInClosesAt <= now)
            .ExecuteUpdateAsync(set => set
                .SetProperty(u => u.Status, EUsageStatus.Absent)
                .SetProperty(u => u.CheckInTokenHash, (string?)null), ct);

    /// <summary>Check-out não feito no prazo: só um admin fecha.</summary>
    private Task MarkCheckoutPending(DateTimeOffset now, CancellationToken ct) =>
        dbCtx.ReserveUsages
            .Where(u => u.Status == EUsageStatus.CheckedIn && u.CheckOutClosesAt <= now)
            .ExecuteUpdateAsync(set => set
                .SetProperty(u => u.Status, EUsageStatus.CheckoutPending)
                .SetProperty(u => u.CheckOutTokenHash, (string?)null), ct);

    private async Task SaveIgnoringDuplicates(CancellationToken ct)
    {
        try
        {
            await dbCtx.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            // Outra instância criou a mesma sessão ao mesmo tempo: o próximo ciclo segue normalmente.
            dbCtx.ChangeTracker.Clear();
        }
    }
}

/// <summary>Roda o <see cref="ReserveUsageScheduler"/> a cada minuto, em um escopo de DI novo por ciclo.</summary>
public sealed class ReserveUsageWorker(IServiceScopeFactory scopeFactory, ILogger<ReserveUsageWorker> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<ReserveUsageScheduler>();
                await scheduler.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no ciclo de check-in/check-out");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
