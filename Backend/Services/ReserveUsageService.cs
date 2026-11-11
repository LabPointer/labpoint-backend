using System.Security.Claims;
using Backend.Handler;
using Data;
using DTOs.Schedule;
using DTOs.Usage;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Usage;

namespace Backend.Services;

public interface IReserveUsageService
{
    public Task<UsageTokenResponseDTO> GetByToken(string token);

    public Task TokenCheckIn(string token, UsageEntriesRequestDTO data);

    public Task TokenCheckOut(string token, UsageEntriesRequestDTO data);

    public Task<List<UsageSessionResponseDTO>> GetMine(ClaimsPrincipal user, UsageRequestDTO query);

    public Task CheckIn(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data);

    public Task SaveEntries(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data);

    public Task CheckOut(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data);

    public Task<List<UsageSessionResponseDTO>> GetBySpaceReserve(ClaimsPrincipal user, long spaceReserveId);

    public Task<List<UsageSessionResponseDTO>> GetByResourceReserve(ClaimsPrincipal user, long resourceReserveId);

    public Task<List<UsageSessionResponseDTO>> AdminGet(ClaimsPrincipal user, AdminUsageRequestDTO query);

    public Task AdminSaveEntries(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data);

    public Task AdminCheckOut(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data);
}

/// <summary>
/// Check-in, relatório de uso e check-out das sessões (reserva x dia x turno). O usuário só mexe nas próprias sessões
/// e dentro das janelas; depois do check-out o relatório fica só leitura para ele. Admin/Root edita e fecha sessões
/// dos outros usuários a qualquer momento depois do check-in.
/// </summary>
public class ReserveUsageService(AppDbContext dbCtx) : IReserveUsageService
{
    private const string NotFound = "Sessão de uso não encontrada.";

    public async Task<UsageTokenResponseDTO> GetByToken(string token)
    {
        var (session, step) = await FindByToken(token);
        var row = await LoadRow(session.Id);
        return new UsageTokenResponseDTO(step, row.ToDto());
    }

    public async Task TokenCheckIn(string token, UsageEntriesRequestDTO data)
    {
        var (session, step) = await FindByToken(token);
        if (step != EUsageStep.CheckIn)
        {
            throw new BadRequestException("Este link é de check-out.");
        }

        await DoCheckIn(session, data);
    }

    public async Task TokenCheckOut(string token, UsageEntriesRequestDTO data)
    {
        var (session, step) = await FindByToken(token);
        if (step != EUsageStep.CheckOut)
        {
            throw new BadRequestException("Este link é de check-in.");
        }

        await DoCheckOut(session, data, adminId: null);
    }

    public async Task<List<UsageSessionResponseDTO>> GetMine(ClaimsPrincipal user, UsageRequestDTO query)
    {
        var userId = UserId(user);
        var queryable = dbCtx.ReserveUsages.AsNoTracking().Where(u => u.FkAccountId == userId);

        if (query.StartAt.HasValue || query.EndAt.HasValue)
        {
            if (query.StartAt == null || query.EndAt == null)
            {
                throw new BadRequestException("Informe as datas de início e término juntas.");
            }

            ReserveRules.EnsureValidPeriod(query.StartAt.Value, query.EndAt.Value);
            queryable = queryable.Where(u => u.Date >= query.StartAt && u.Date <= query.EndAt);
        }

        if (query.Statuses is { Count: > 0 })
        {
            var statuses = query.Statuses.ToList();
            queryable = queryable.Where(u => statuses.Contains(u.Status));
        }

        return await List(queryable, query.Limit, query.Offset);
    }

    public async Task CheckIn(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data) =>
        await DoCheckIn(await LoadOwn(user, usageId), data);

    public async Task SaveEntries(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data)
    {
        var session = await LoadOwn(user, usageId);
        if (session.Status != EUsageStatus.CheckedIn)
        {
            throw new BadRequestException(session.Status is EUsageStatus.Completed or EUsageStatus.CheckoutPending
                ? "O relatório não pode mais ser alterado depois do check-out."
                : "Faça o check-in antes de preencher o relatório.");
        }

        await ApplyEntries(session, data, requireExit: false);
        await dbCtx.SaveChangesAsync();
    }

    public async Task CheckOut(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data) =>
        await DoCheckOut(await LoadOwn(user, usageId), data, adminId: null);

    public async Task<List<UsageSessionResponseDTO>> GetBySpaceReserve(ClaimsPrincipal user, long spaceReserveId)
    {
        var owner = await dbCtx.SpaceReserves.AsNoTracking()
            .Where(sr => sr.Id == spaceReserveId)
            .Select(sr => sr.FkAccountId)
            .FirstOrDefaultAsync();

        EnsureCanView(user, owner);
        return await ListAll(dbCtx.ReserveUsages.AsNoTracking().Where(u => u.FkSpaceReserveId == spaceReserveId));
    }

    public async Task<List<UsageSessionResponseDTO>> GetByResourceReserve(ClaimsPrincipal user, long resourceReserveId)
    {
        var reserve = await dbCtx.ResourceReserves.AsNoTracking()
            .Where(rr => rr.Id == resourceReserveId)
            .Select(rr => new { rr.FkAccountId, rr.FkSpaceReserveId })
            .FirstOrDefaultAsync();

        EnsureCanView(user, reserve?.FkAccountId);

        // Reserva vinculada: o uso é registrado nas sessões da sala.
        var queryable = reserve!.FkSpaceReserveId is { } spaceReserveId
            ? dbCtx.ReserveUsages.AsNoTracking().Where(u => u.FkSpaceReserveId == spaceReserveId)
            : dbCtx.ReserveUsages.AsNoTracking().Where(u => u.FkResourceReserveId == resourceReserveId);

        return await ListAll(queryable);
    }

    public async Task<List<UsageSessionResponseDTO>> AdminGet(ClaimsPrincipal user, AdminUsageRequestDTO query)
    {
        ReserveRules.EnsureValidPeriod(query.StartAt, query.EndAt);
        var userId = UserId(user);

        // Admin não gerencia as próprias sessões (mesma regra de "Gerenciar reservas").
        var queryable = dbCtx.ReserveUsages.AsNoTracking()
            .Where(u => u.FkAccountId != userId && u.Date >= query.StartAt && u.Date <= query.EndAt);

        if (query.Statuses is { Count: > 0 })
        {
            var statuses = query.Statuses.ToList();
            queryable = queryable.Where(u => statuses.Contains(u.Status));
        }

        if (query.SpaceIds is { Count: > 0 })
        {
            var spaceIds = query.SpaceIds.ToList();
            queryable = queryable.Where(u => u.SpaceReserve != null && spaceIds.Contains(u.SpaceReserve.FkSpaceId));
        }

        if (query.AccountIds is { Count: > 0 })
        {
            var accountIds = query.AccountIds.ToList();
            queryable = queryable.Where(u => accountIds.Contains(u.FkAccountId));
        }

        return await List(queryable, query.Limit, query.Offset);
    }

    public async Task AdminSaveEntries(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data)
    {
        var session = await LoadOthers(user, usageId);
        if (!UsageRules.IsPresent(session.Status))
        {
            throw new BadRequestException("A sessão não tem check-in: não há relatório para editar.");
        }

        // Sessão encerrada continua precisando das quantidades de saída.
        await ApplyEntries(session, data, requireExit: session.Status == EUsageStatus.Completed);
        await dbCtx.SaveChangesAsync();
    }

    public async Task AdminCheckOut(ClaimsPrincipal user, long usageId, UsageEntriesRequestDTO data)
    {
        var session = await LoadOthers(user, usageId);
        if (session.Status is not (EUsageStatus.CheckedIn or EUsageStatus.CheckoutPending))
        {
            throw new BadRequestException(session.Status == EUsageStatus.Completed
                ? "O check-out desta sessão já foi feito."
                : "A sessão não tem check-in para encerrar.");
        }

        await DoCheckOut(session, data, adminId: UserId(user));
    }

    // ---------------------------------------------------------------- regras

    private async Task DoCheckIn(ReserveUsageModel session, UsageEntriesRequestDTO data)
    {
        var now = BrazilClock.UtcNow;

        if (session.Status != EUsageStatus.Scheduled)
        {
            throw new BadRequestException(session.Status == EUsageStatus.Absent
                ? "O prazo do check-in terminou e a reserva deste turno foi liberada."
                : "O check-in desta sessão já foi feito.");
        }

        if (now < session.CheckInOpensAt)
        {
            throw new BadRequestException(
                $"O check-in abre às {BrazilClock.ToLocal(session.CheckInOpensAt):HH:mm}.");
        }

        if (now > session.CheckInClosesAt)
        {
            throw new BadRequestException("O prazo do check-in terminou.");
        }

        var slots = await ApplyEntries(session, data, requireExit: false);

        // No check-in ao menos o primeiro horário do turno precisa estar preenchido.
        var first = slots.MinBy(s => s.StartAt)!;
        if (session.Entries.All(e => e.FkScheduleId != first.Id))
        {
            throw new BadRequestException("Preencha o relatório do primeiro horário do turno.");
        }

        session.Status = EUsageStatus.CheckedIn;
        session.CheckedInAt = now;
        session.CheckInTokenHash = null;
        await dbCtx.SaveChangesAsync();
    }

    private async Task DoCheckOut(ReserveUsageModel session, UsageEntriesRequestDTO data, string? adminId)
    {
        var now = BrazilClock.UtcNow;

        if (adminId == null)
        {
            if (session.Status != EUsageStatus.CheckedIn)
            {
                throw new BadRequestException(session.Status switch
                {
                    EUsageStatus.Completed => "O check-out desta sessão já foi feito.",
                    EUsageStatus.CheckoutPending => "O prazo do check-out terminou: procure a coordenação.",
                    _ => "Faça o check-in antes do check-out."
                });
            }

            if (now < session.CheckOutOpensAt)
            {
                throw new BadRequestException(
                    $"O check-out abre às {BrazilClock.ToLocal(session.CheckOutOpensAt):HH:mm}.");
            }

            if (now > session.CheckOutClosesAt)
            {
                throw new BadRequestException("O prazo do check-out terminou: procure a coordenação.");
            }
        }

        await ApplyEntries(session, data, requireExit: true);

        session.Status = EUsageStatus.Completed;
        session.CheckedOutAt = now;
        session.FkCheckedOutById = adminId;
        session.CheckInTokenHash = null;
        session.CheckOutTokenHash = null;
        await dbCtx.SaveChangesAsync();
    }

    /// <summary>
    /// Grava as linhas do relatório (só os horários enviados; com <c>RepeatForAll</c>, a primeira linha vale para todos).
    /// Com <paramref name="requireExit"/>, todos os horários do turno precisam terminar com entrada e saída.
    /// Devolve os horários do turno. Não salva.
    /// </summary>
    private async Task<List<ScheduleResponseDTO>> ApplyEntries(
        ReserveUsageModel session, UsageEntriesRequestDTO data, bool requireExit)
    {
        var slots = await LoadSlots(session);
        var slotIds = slots.Select(s => s.Id).ToHashSet();

        var inputs = data.RepeatForAll
            ? slotIds.Select(id => data.Entries[0] with { ScheduleId = id }).ToList()
            : data.Entries;

        if (inputs.Select(e => e.ScheduleId).Distinct().Count() != inputs.Count)
        {
            throw new BadRequestException("Há mais de uma linha do relatório para o mesmo horário.");
        }

        if (inputs.Any(e => !slotIds.Contains(e.ScheduleId)))
        {
            throw new BadRequestException("Um ou mais horários não pertencem a este turno da reserva.");
        }

        foreach (var input in inputs)
        {
            if ((input.NotebooksOut == null) != (input.ChargersOut == null))
            {
                throw new BadRequestException("Informe notebooks e fontes da saída juntos.");
            }

            var entry = session.Entries.FirstOrDefault(e => e.FkScheduleId == input.ScheduleId);
            if (entry == null)
            {
                entry = new ReserveUsageEntryModel { FkScheduleId = input.ScheduleId };
                session.Entries.Add(entry);
            }

            entry.ClassName = input.ClassName.Trim();
            entry.Modality = input.Modality;
            entry.NotebooksIn = input.NotebooksIn;
            entry.ChargersIn = input.ChargersIn;
            entry.NotebooksOut = input.NotebooksOut;
            entry.ChargersOut = input.ChargersOut;
        }

        if (requireExit)
        {
            var complete = slotIds.All(id => session.Entries.Any(e =>
                e.FkScheduleId == id && e.NotebooksOut != null && e.ChargersOut != null));

            if (!complete)
            {
                throw new BadRequestException(
                    "Preencha a entrada e a saída (notebooks e fontes) de todos os horários do turno.");
            }
        }

        return slots;
    }

    /// <summary>Horários da reserva no turno da sessão, em ordem.</summary>
    private async Task<List<ScheduleResponseDTO>> LoadSlots(ReserveUsageModel session)
    {
        var query = session.FkSpaceReserveId is { } spaceReserveId
            ? dbCtx.SpaceReserveSchedules.AsNoTracking()
                .Where(s => s.FkSpaceReserveId == spaceReserveId && s.Schedule.Shift == session.Shift)
                .Select(s => s.Schedule)
            : dbCtx.ResourceReserveSchedules.AsNoTracking()
                .Where(s => s.FkResourceReserveId == session.FkResourceReserveId && s.Schedule.Shift == session.Shift)
                .Select(s => s.Schedule);

        var slots = await query
            .OrderBy(s => s.StartAt)
            .Select(s => new ScheduleResponseDTO(s.Id, s.StartAt, s.EndAt, s.Shift, s.Enabled))
            .ToListAsync();

        if (slots.Count == 0)
        {
            throw new BadRequestException("A reserva não tem mais horários neste turno.");
        }

        return slots;
    }

    // ---------------------------------------------------------------- carregamento

    private async Task<(ReserveUsageModel Session, EUsageStep Step)> FindByToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
        {
            throw new ResourceNotFoundException("Link inválido.");
        }

        var hash = UsageRules.HashToken(token);
        var session = await dbCtx.ReserveUsages
            .Include(u => u.Entries)
            .FirstOrDefaultAsync(u => u.CheckInTokenHash == hash || u.CheckOutTokenHash == hash);

        if (session == null)
        {
            throw new ResourceNotFoundException("Link inválido ou já utilizado.");
        }

        var now = BrazilClock.UtcNow;
        if (session.CheckInTokenHash == hash)
        {
            if (now > session.CheckInClosesAt)
            {
                throw new BadRequestException("O prazo deste check-in terminou.");
            }

            return (session, EUsageStep.CheckIn);
        }

        if (now > session.CheckOutClosesAt)
        {
            throw new BadRequestException("O prazo deste check-out terminou: procure a coordenação.");
        }

        return (session, EUsageStep.CheckOut);
    }

    private async Task<ReserveUsageModel> LoadOwn(ClaimsPrincipal user, long usageId)
    {
        var userId = UserId(user);
        return await dbCtx.ReserveUsages
                   .Include(u => u.Entries)
                   .FirstOrDefaultAsync(u => u.Id == usageId && u.FkAccountId == userId)
               ?? throw new ResourceNotFoundException(NotFound);
    }

    private async Task<ReserveUsageModel> LoadOthers(ClaimsPrincipal user, long usageId)
    {
        var userId = UserId(user);
        return await dbCtx.ReserveUsages
                   .Include(u => u.Entries)
                   .FirstOrDefaultAsync(u => u.Id == usageId && u.FkAccountId != userId)
               ?? throw new ResourceNotFoundException(NotFound);
    }

    private async Task<UsageRow> LoadRow(long usageId) =>
        await UsageQueries.Project(dbCtx.ReserveUsages.AsNoTracking().Where(u => u.Id == usageId)).FirstAsync();

    /// <summary>Paginado (Limit + 1: o extra indica próxima página); vazio = 404, como as outras listagens.</summary>
    private static async Task<List<UsageSessionResponseDTO>> List(IQueryable<ReserveUsageModel> queryable, int limit, int offset)
    {
        var rows = await UsageQueries.Project(queryable
                .OrderByDescending(u => u.Date)
                .ThenBy(u => u.Shift)
                .ThenBy(u => u.Id)
                .Skip(offset)
                .Take(limit + 1))
            .ToListAsync();

        if (rows.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhuma sessão de uso encontrada com os filtros fornecidos.");
        }

        return rows.Select(r => r.ToDto()).ToList();
    }

    /// <summary>Todas as sessões de uma reserva (lista vazia quando ainda não há nenhuma).</summary>
    private static async Task<List<UsageSessionResponseDTO>> ListAll(IQueryable<ReserveUsageModel> queryable)
    {
        var rows = await UsageQueries.Project(queryable.OrderBy(u => u.Date).ThenBy(u => u.Shift)).ToListAsync();
        return rows.Select(r => r.ToDto()).ToList();
    }

    /// <summary>Dono da reserva ou Admin/Root. Reserva inexistente = 404.</summary>
    private static void EnsureCanView(ClaimsPrincipal user, string? ownerId)
    {
        if (ownerId == null)
        {
            throw new ResourceNotFoundException("Reserva não encontrada.");
        }

        if (ownerId != UserId(user) && !user.IsInRole("Admin") && !user.IsInRole("Root"))
        {
            throw new ResourceNotFoundException("Reserva não encontrada.");
        }
    }

    private static string UserId(ClaimsPrincipal user) => user.FindFirst(ClaimTypes.NameIdentifier)!.Value;
}
