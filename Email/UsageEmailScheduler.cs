using System.Globalization;
using Core;
using Core.Email;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Services;

namespace Email;

/// <summary>
/// Agenda dos avisos de check-in e check-out. A cada <c>UsageScanSeconds</c> procura as sessões de uso
/// (<c>reserve_usage</c>, criadas pelo backend) que chegaram na hora do aviso. Para cada uma, numa transação: gera o
/// token do link, grava o hash e a data na sessão e coloca o e-mail na fila ligado à sessão (<c>fk_usage_id</c>), com
/// prazo igual ao fim da janela (<c>expires_at</c>). Assim um aviso nunca sai duas vezes, nunca sai com o link já
/// vencido, e a coordenação vê no Gerenciar check-out se ele foi enviado.
/// </summary>
public sealed class UsageEmailScheduler(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailServiceSettings> options,
    IOptions<FrontendSettings> frontend,
    ILogger<UsageEmailScheduler> logger) : BackgroundService
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly EmailServiceSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(_settings.UsageScanSeconds, 10)));

        do
        {
            try
            {
                var checkIns = await Run(isCheckIn: true, stoppingToken);
                var checkOuts = await Run(isCheckIn: false, stoppingToken);
                if (checkIns + checkOuts > 0)
                    logger.LogInformation("Avisos agendados: {CheckIns} de check-in, {CheckOuts} de check-out",
                        checkIns, checkOuts);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro ao agendar os avisos de check-in/check-out");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private sealed record DueSession(
        long Id,
        DateOnly Date,
        EShift Shift,
        DateTimeOffset ClosesAt,
        string? UserName,
        string? Email,
        string Place,
        List<SlotTime> SpaceSlots,
        List<SlotTime> ResourceSlots);

    private sealed record SlotTime(TimeOnly StartAt, TimeOnly EndAt);

    private async Task<int> Run(bool isCheckIn, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var lead = TimeSpan.FromMinutes(isCheckIn
            ? _settings.CheckInEmailMinutesBeforeWindow
            : _settings.CheckOutEmailMinutesBeforeWindow);
        var opensUntil = now + lead;

        var sessions = db.ReserveUsages.AsNoTracking();
        sessions = isCheckIn
            ? sessions.Where(u => u.Status == EUsageStatus.Scheduled && u.CheckInEmailSentAt == null
                                 && u.CheckInOpensAt <= opensUntil && u.CheckInClosesAt > now)
            : sessions.Where(u => u.Status == EUsageStatus.CheckedIn && u.CheckOutEmailSentAt == null
                                  && u.CheckOutOpensAt <= opensUntil && u.CheckOutClosesAt > now);

        // Lugar: sala da reserva ou, para reserva de recurso avulsa, o recurso. Horários: os da reserva naquele turno.
        var due = await sessions
            .Select(u => new DueSession(
                u.Id,
                u.Date,
                u.Shift,
                isCheckIn ? u.CheckInClosesAt : u.CheckOutClosesAt,
                u.Account.UserName,
                u.Account.Email,
                u.SpaceReserve != null
                    ? u.SpaceReserve.Space.Name
                    : u.ResourceReserve != null ? u.ResourceReserve.Resource.Name : "Reserva",
                u.SpaceReserve == null
                    ? new List<SlotTime>()
                    : u.SpaceReserve.SpaceReserveSchedules.Where(s => s.Schedule.Shift == u.Shift)
                        .Select(s => new SlotTime(s.Schedule.StartAt, s.Schedule.EndAt)).ToList(),
                u.ResourceReserve == null
                    ? new List<SlotTime>()
                    : u.ResourceReserve.ResourceReserveSchedules.Where(s => s.Schedule.Shift == u.Shift)
                        .Select(s => new SlotTime(s.Schedule.StartAt, s.Schedule.EndAt)).ToList()))
            .AsSplitQuery()
            .ToListAsync(ct);

        var scheduled = 0;
        foreach (var session in due)
        {
            var (token, hash) = UsageTokens.NewToken();

            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            // Reserva a sessão: se outra instância já gerou o aviso, nada muda e este ciclo pula a sessão.
            var claimed = isCheckIn
                ? await db.ReserveUsages
                    .Where(u => u.Id == session.Id && u.CheckInEmailSentAt == null)
                    .ExecuteUpdateAsync(set => set
                        .SetProperty(u => u.CheckInTokenHash, hash)
                        .SetProperty(u => u.CheckInEmailSentAt, now), ct)
                : await db.ReserveUsages
                    .Where(u => u.Id == session.Id && u.CheckOutEmailSentAt == null)
                    .ExecuteUpdateAsync(set => set
                        .SetProperty(u => u.CheckOutTokenHash, hash)
                        .SetProperty(u => u.CheckOutEmailSentAt, now), ct);
            if (claimed == 0)
                continue;

            if (!string.IsNullOrWhiteSpace(session.Email))
            {
                Enqueue(db, session, token, isCheckIn);
                await db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
            scheduled++;
        }

        if (scheduled > 0)
            await EmailOutbox.NotifyAsync(db, ct);

        return scheduled;
    }

    private void Enqueue(AppDbContext db, DueSession session, string token, bool isCheckIn)
    {
        var link = $"{frontend.Value.Url.TrimEnd('/')}/usage?token={Uri.EscapeDataString(token)}";
        var date = session.Date.ToString("dd/MM/yyyy", PtBr);
        var shift = session.Shift switch
        {
            EShift.Morning => "Manhã",
            EShift.Afternoon => "Tarde",
            _ => "Noite"
        };
        var schedules = string.Join(", ", session.SpaceSlots.Concat(session.ResourceSlots)
            .OrderBy(s => s.StartAt)
            .Select(s => $"{s.StartAt.ToString("HH:mm", PtBr)}–{s.EndAt.ToString("HH:mm", PtBr)}"));

        var values = new Dictionary<string, string>
        {
            ["user"] = session.UserName ?? "",
            ["place"] = session.Place,
            ["date"] = date,
            ["shift"] = shift,
            ["schedules"] = schedules,
            ["deadline"] = BrazilClock.ToLocal(session.ClosesAt).ToString("HH:mm", PtBr),
            ["link"] = link
        };

        EmailOutbox.Add(db, session.Email!,
            $"{(isCheckIn ? "Check-in" : "Check-out")}: {session.Place} — {shift} {date}",
            isCheckIn ? EmailOutbox.CheckInTemplate : EmailOutbox.CheckOutTemplate,
            values, usageId: session.Id, expiresAt: session.ClosesAt);
    }
}
