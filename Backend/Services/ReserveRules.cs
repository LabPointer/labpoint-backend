using System.Linq.Expressions;
using Backend.Handler;
using Data;
using DTOs.SpaceReserve;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Reserve;

namespace Backend.Services;

/// <summary>
/// Regras compartilhadas entre reservas de sala e de recurso: horários válidos, conflito de sala e a sincronização
/// das reservas de recurso vinculadas a uma reserva de sala (mesmo período, mesmos horários, status acompanhando a sala).
/// </summary>
public static class ReserveRules
{
    /// <summary>Só reservas de sala pendentes ou confirmadas ocupam horários.</summary>
    public static readonly Expression<Func<SpaceReserveModel, bool>> IsSpaceReserveActive =
        sr => sr.Status == EReserveStatus.Booking || sr.Status == EReserveStatus.Confirmed;

    public static void EnsureValidPeriod(DateOnly start, DateOnly end)
    {
        if (start > end)
        {
            throw new BadRequestException("Data de início precisa ser igual ou anterior à data de término.");
        }
    }

    /// <summary>Os horários precisam existir e estar ativos (uma consulta só).</summary>
    public static async Task EnsureSchedulesAvailable(AppDbContext dbCtx, IReadOnlySet<long> scheduleIds)
    {
        if (scheduleIds.Count == 0)
        {
            return;
        }

        var ids = scheduleIds.ToList();
        var validCount = await dbCtx.Schedules.CountAsync(s => ids.Contains(s.Id) && s.Enabled);
        if (validCount != scheduleIds.Count)
        {
            throw new BadRequestException("Um ou mais horários informados não existem ou estão desativados.");
        }
    }

    /// <summary>A sala não pode ter outra reserva ativa no mesmo período e horário.</summary>
    public static async Task EnsureSpaceNoConflict(
        AppDbContext dbCtx, long reserveId, long spaceId, DateOnly start, DateOnly end, IReadOnlySet<long> scheduleIds)
    {
        var ids = scheduleIds.ToList();
        bool hasConflict = await dbCtx.SpaceReserves
            .Where(sr => sr.Id != reserveId && sr.FkSpaceId == spaceId && sr.DateFrom <= end && sr.DateTo >= start)
            .Where(IsSpaceReserveActive)
            .AnyAsync(sr => sr.SpaceReserveSchedules.Any(s => ids.Contains(s.FkScheduleId)));

        if (hasConflict)
        {
            throw new BadRequestException("A reserva conflita com uma reserva existente no mesmo período e horário.");
        }
    }

    public static void ReplaceSpaceSchedules(SpaceReserveModel spaceReserve, IEnumerable<long> scheduleIds)
    {
        spaceReserve.SpaceReserveSchedules.Clear();
        foreach (var scheduleId in scheduleIds)
        {
            spaceReserve.SpaceReserveSchedules.Add(new SpaceReserveScheduleModel
            {
                FkSpaceReserveId = spaceReserve.Id,
                FkScheduleId = scheduleId
            });
        }
    }

    public static void ReplaceResourceSchedules(ResourceReserveModel resourceReserve, IEnumerable<long> scheduleIds)
    {
        resourceReserve.ResourceReserveSchedules.Clear();
        foreach (var scheduleId in scheduleIds)
        {
            resourceReserve.ResourceReserveSchedules.Add(new ResourceReserveScheduleModel
            {
                FkResourceReserveId = resourceReserve.Id,
                FkScheduleId = scheduleId
            });
        }
    }

    /// <summary>
    /// Status de uma reserva de recurso criada junto com a sala. Reserva de sala bloqueada (Locked) não ocupa
    /// horários, mas o recurso pedido deve ficar separado: entra como pendente.
    /// </summary>
    public static EReserveStatus LinkedStatusFor(EReserveStatus spaceStatus) =>
        ResourceAvailability.IsActiveStatus(spaceStatus) ? spaceStatus : EReserveStatus.Booking;

    /// <summary>
    /// Leva o período e os horários de <paramref name="spaceReserve"/> para as reservas de recurso vinculadas ativas e,
    /// quando <paramref name="requested"/> vem preenchido, ajusta o conjunto (cria, altera quantidade, cancela as que
    /// saíram). Antes, confere a disponibilidade de todos os recursos que ficam ativos, desconsiderando as unidades da
    /// própria reserva de sala. Não salva; quem chama faz o SaveChanges.
    /// <c>requested</c>: conjunto final de recursos (nulo = manter os atuais).
    /// <c>quantityOverrides</c>: novas quantidades para reservas vinculadas existentes (edição pelo lado do recurso).
    /// </summary>
    public static async Task SyncLinkedResources(
        AppDbContext dbCtx,
        SpaceReserveModel spaceReserve,
        DateOnly start,
        DateOnly end,
        IReadOnlySet<long> scheduleIds,
        IReadOnlyCollection<ReserveResourceItemDTO>? requested,
        IReadOnlyDictionary<long, int>? quantityOverrides = null)
    {
        var linked = spaceReserve.Id == 0
            ? new List<ResourceReserveModel>()
            : await dbCtx.ResourceReserves
                .Include(rr => rr.ResourceReserveSchedules)
                .Where(rr => rr.FkSpaceReserveId == spaceReserve.Id)
                .ToListAsync();

        var active = linked.Where(rr => ResourceAvailability.IsActiveStatus(rr.Status)).ToList();

        // Quantidade final por recurso.
        Dictionary<long, int> target;
        if (requested != null)
        {
            target = ResourceAvailability.MergeDemands(
                requested.Select(item => new ResourceDemand(item.ResourceId, item.Quantity)));
        }
        else
        {
            target = new Dictionary<long, int>();
            foreach (var reserve in active)
            {
                var quantity = quantityOverrides != null && quantityOverrides.TryGetValue(reserve.Id, out var newQuantity)
                    ? newQuantity
                    : reserve.Quantity;
                target[reserve.FkResourceId] = target.GetValueOrDefault(reserve.FkResourceId) + quantity;
            }
        }

        // Reserva de sala inativa (cancelada, recusada, bloqueada) não segura unidades: não há o que conferir.
        if (ResourceAvailability.IsActiveStatus(spaceReserve.Status) || spaceReserve.Id == 0)
        {
            await ResourceAvailability.EnsureAvailable(
                dbCtx,
                target.Select(entry => new ResourceDemand(entry.Key, entry.Value)),
                start,
                end,
                scheduleIds,
                new ReserveExclusion(SpaceReserveId: spaceReserve.Id == 0 ? null : spaceReserve.Id));
        }

        var kept = new HashSet<long>();
        foreach (var reserve in active)
        {
            // recurso que saiu do conjunto, ou cópia duplicada do mesmo recurso: cancela
            if (!target.ContainsKey(reserve.FkResourceId) || !kept.Add(reserve.FkResourceId))
            {
                reserve.Status = EReserveStatus.Canceled;
                continue;
            }

            reserve.Quantity = target[reserve.FkResourceId];
            reserve.DateFrom = start;
            reserve.DateTo = end;
            ReplaceResourceSchedules(reserve, scheduleIds);
        }

        foreach (var (resourceId, quantity) in target.Where(entry => !kept.Contains(entry.Key)))
        {
            var reserve = new ResourceReserveModel
            {
                FkResourceId = resourceId,
                FkAccountId = spaceReserve.FkAccountId,
                Purpose = spaceReserve.Purpose,
                Quantity = quantity,
                DateFrom = start,
                DateTo = end,
                Status = LinkedStatusFor(spaceReserve.Status),
                ResourceReserveSchedules = scheduleIds
                    .Select(scheduleId => new ResourceReserveScheduleModel { FkScheduleId = scheduleId })
                    .ToList()
            };

            spaceReserve.ResourceReserves.Add(reserve);
        }
    }

    /// <summary>
    /// A reserva de sala mudou de status: as reservas de recurso vinculadas que ainda estão ativas acompanham
    /// (cancelar a sala cancela os recursos; confirmar a sala confirma os recursos). As que o admin já tinha
    /// cancelado ou recusado individualmente ficam como estão. Bloquear a sala (Locked) não mexe nos recursos.
    /// </summary>
    public static Task<int> CascadeStatus(AppDbContext dbCtx, long spaceReserveId, EReserveStatus newStatus)
    {
        if (newStatus == EReserveStatus.Locked)
        {
            return Task.FromResult(0);
        }

        return dbCtx.ResourceReserves
            .Where(rr => rr.FkSpaceReserveId == spaceReserveId)
            .Where(ResourceAvailability.IsActive)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rr => rr.Status, newStatus));
    }

    /// <summary>Projeção dos recursos vinculados de uma reserva de sala.</summary>
    public static readonly Expression<Func<ResourceReserveModel, LinkedResourceDTO>> ToLinkedDto =
        rr => new LinkedResourceDTO(rr.Id, rr.FkResourceId, rr.Resource.Name, rr.Resource.Description, rr.Quantity, rr.Status);
}
