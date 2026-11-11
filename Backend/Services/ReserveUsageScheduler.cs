using System.Globalization;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Models.Usage;
using Services;

namespace Backend.Services;

/// <summary>
/// Um ciclo do check-in/check-out (roda a cada minuto pelo <see cref="ReserveUsageWorker"/>):
/// cria as sessões do dia, envia os e-mails de check-in, marca ausências, envia os e-mails de check-out e marca
/// check-outs vencidos. Cada passo é idempotente: rodar de novo no mesmo minuto não duplica nada.
/// </summary>
public class ReserveUsageScheduler(
    AppDbContext dbCtx,
    ICalendarService calendar,
    IMailSender mailSender,
    IOptions<FrontendSettings> frontend,
    ILogger<ReserveUsageScheduler> logger)
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public async Task RunAsync(CancellationToken ct)
    {
        var now = BrazilClock.UtcNow;
        var today = BrazilClock.Today;

        await MaterializeSessions(today, now, ct);
        await SendCheckInEmails(now, ct);
        await MarkAbsent(now, ct);
        await SendCheckOutEmails(now, ct);
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

    private async Task SendCheckInEmails(DateTimeOffset now, CancellationToken ct)
    {
        var emailFrom = now + (UsageRules.CheckInEmailBefore - UsageRules.CheckInOpensBefore);
        var sessions = await dbCtx.ReserveUsages
            .Where(u => u.Status == EUsageStatus.Scheduled
                        && u.CheckInEmailSentAt == null
                        && u.CheckInOpensAt <= emailFrom
                        && u.CheckInClosesAt > now)
            .ToListAsync(ct);

        await SendLinks(sessions, now, isCheckIn: true, ct);
    }

    private async Task SendCheckOutEmails(DateTimeOffset now, CancellationToken ct)
    {
        var sessions = await dbCtx.ReserveUsages
            .Where(u => u.Status == EUsageStatus.CheckedIn
                        && u.CheckOutEmailSentAt == null
                        && u.CheckOutOpensAt <= now
                        && u.CheckOutClosesAt > now)
            .ToListAsync(ct);

        await SendLinks(sessions, now, isCheckIn: false, ct);
    }

    /// <summary>
    /// Gera o token, grava o hash e marca o envio ANTES de mandar o e-mail: se o envio falhar o usuário ainda pode usar
    /// as páginas do sistema, e um reenvio nunca duplica o aviso.
    /// </summary>
    private async Task SendLinks(List<ReserveUsageModel> sessions, DateTimeOffset now, bool isCheckIn, CancellationToken ct)
    {
        if (sessions.Count == 0)
        {
            return;
        }

        var tokens = new Dictionary<long, string>();
        foreach (var session in sessions)
        {
            var (token, hash) = UsageRules.NewToken();
            tokens[session.Id] = token;

            if (isCheckIn)
            {
                session.CheckInTokenHash = hash;
                session.CheckInEmailSentAt = now;
            }
            else
            {
                session.CheckOutTokenHash = hash;
                session.CheckOutEmailSentAt = now;
            }
        }

        await dbCtx.SaveChangesAsync(ct);

        var ids = tokens.Keys.ToList();
        var rows = await UsageQueries.Project(dbCtx.ReserveUsages.AsNoTracking().Where(u => ids.Contains(u.Id)))
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Email))
            {
                continue;
            }

            try
            {
                await SendEmail(row, tokens[row.Id], isCheckIn);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao enviar o e-mail de {Step} da sessão {SessionId}",
                    isCheckIn ? "check-in" : "check-out", row.Id);
            }
        }
    }

    private async Task SendEmail(UsageRow row, string token, bool isCheckIn)
    {
        var link = $"{frontend.Value.Url.TrimEnd('/')}/usage?token={Uri.EscapeDataString(token)}";
        var deadline = BrazilClock.ToLocal(isCheckIn ? row.CheckInClosesAt : row.CheckOutClosesAt);
        var schedules = string.Join(", ", row.Schedules.Select(s => $"{s.StartAt:HH\\:mm}–{s.EndAt:HH\\:mm}"));

        var values = new Dictionary<string, string>
        {
            ["user"] = row.Account.UserName ?? "",
            ["place"] = row.Place,
            ["date"] = row.Date.ToString("dd/MM/yyyy", PtBr),
            ["shift"] = ShiftLabel(row.Shift),
            ["schedules"] = schedules,
            ["deadline"] = deadline.ToString("HH:mm", PtBr),
            ["link"] = link
        };

        var template = isCheckIn ? "check-in.html" : "check-out.html";
        var html = await mailSender.RenderTemplateAsync(template, values)
                   ?? $"<p>Olá, {System.Net.WebUtility.HtmlEncode(values["user"])}!</p>" +
                      $"<p>{(isCheckIn ? "Faça o check-in" : "Faça o check-out")} de {System.Net.WebUtility.HtmlEncode(row.Place)} " +
                      $"({values["date"]}, {values["shift"]}) até {values["deadline"]}: " +
                      $"<a href=\"{System.Net.WebUtility.HtmlEncode(link)}\">abrir</a>.</p>";

        var subject = isCheckIn
            ? $"Check-in: {row.Place} — {values["shift"]} {values["date"]}"
            : $"Check-out: {row.Place} — {values["shift"]} {values["date"]}";

        await mailSender.SendAsync(row.Email!, subject, html);
    }

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
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException
                                           {
                                               SqlState: Npgsql.PostgresErrorCodes.UniqueViolation
                                           })
        {
            // Outra instância criou a mesma sessão ao mesmo tempo: o próximo ciclo segue normalmente.
            dbCtx.ChangeTracker.Clear();
        }
    }

    public static string ShiftLabel(EShift shift) => shift switch
    {
        EShift.Morning => "Manhã",
        EShift.Afternoon => "Tarde",
        _ => "Noite"
    };
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
