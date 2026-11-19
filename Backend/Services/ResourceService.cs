using Backend.Handler;
using Data;
using DTOs.Resource;
using Microsoft.EntityFrameworkCore;
using Models.Resource;

namespace Backend.Services;

public interface IResourceService
{
    public Task<IEnumerable<ResourceResponseDTO>> GetResources(ResourceRequestDTO query);

    public Task AdminEditResource(ResourceEditRequestDTO data);

    public Task AdminCreateResource(ResourceCreateRequestDTO data);
}

public class ResourceService(AppDbContext dbCtx) : IResourceService
{
    public async Task<IEnumerable<ResourceResponseDTO>> GetResources(ResourceRequestDTO query)
    {
        var resourceQuery = dbCtx.Resources.AsNoTracking();

        if (query.Enabled.HasValue)
            resourceQuery = resourceQuery.Where(r => r.Enabled == query.Enabled);

        if (query.Reservable.HasValue)
            resourceQuery = resourceQuery.Where(r => r.Reservable == query.Reservable);

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var pattern = QueryHelpers.ContainsPattern(query.Name);
            resourceQuery = resourceQuery.Where(r => EF.Functions.ILike(r.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            resourceQuery = resourceQuery.Where(ResourceAvailability.MatchesText(query.SearchQuery));
        }

        var ordered = resourceQuery.OrderBy(r => r.Name).ThenBy(r => r.Id);
        var checksAvailability = query.StartAt.HasValue || query.EndAt.HasValue;

        List<ResourceResponseDTO> resources;
        if (!checksAvailability)
        {
            // Limit + 1: o front usa o item extra para saber se há próxima página.
            resources = await ordered
                .Skip(query.Offset)
                .Take(query.Limit + 1)
                .Select(r => new ResourceResponseDTO(r.Id, r.Name, r.Enabled, r.Description, r.Reservable, r.Quantity, null))
                .ToListAsync();
        }
        else
        {
            resources = await GetWithAvailability(ordered, query);
        }

        if (resources.Count == 0)
            throw new ResourceNotFoundException("Nenhum recurso encontrado");

        return resources;
    }

    /// <summary>
    /// Com período: calcula as unidades livres de cada recurso reservável e descarta os que não atendem
    /// <c>MinQuantity</c> (padrão 1). O filtro depende da ocupação, então a paginação é feita depois, em memória
    /// (são poucos recursos reserváveis).
    /// <c>Available</c>: com horários informados, o menor valor entre eles (vale para todos); sem horários,
    /// o maior valor entre os horários ativos (existe ao menos um horário com essa folga no período todo).
    /// </summary>
    private async Task<List<ResourceResponseDTO>> GetWithAvailability(IQueryable<ResourceModel> ordered, ResourceRequestDTO query)
    {
        if (query.StartAt == null || query.EndAt == null)
            throw new BadRequestException("Informe as datas de início e término para consultar a disponibilidade.");

        var start = query.StartAt.Value;
        var end = query.EndAt.Value;
        if (start > end)
            throw new BadRequestException("Data de início precisa ser igual ou anterior à data de término.");

        if (end.DayNumber - start.DayNumber > 366)
            throw new BadRequestException("O período de disponibilidade deve ter no máximo um ano.");

        var rows = await ordered
            .Where(r => r.Reservable && r.Quantity != null)
            .Select(r => new { r.Id, r.Name, r.Enabled, r.Description, r.Reservable, r.Quantity })
            .ToListAsync();

        var scheduleIds = query.ScheduleIds is { Count: > 0 }
            ? query.ScheduleIds
            : (await dbCtx.Schedules.AsNoTracking().Where(s => s.Enabled).Select(s => s.Id).ToListAsync()).ToHashSet();

        var exclusion = new ReserveExclusion(query.ExcludeSpaceReserveId, query.ExcludeResourceReserveId);
        var usage = await ResourceAvailability.LoadUsage(
            dbCtx, rows.Select(r => r.Id).ToList(), start, end, scheduleIds, exclusion);

        var allSchedulesRequired = query.ScheduleIds is { Count: > 0 };
        var minQuantity = query.MinQuantity ?? 1;

        return rows
            .Select(r =>
            {
                // pico de ocupação de cada horário dentro do período
                var freeBySchedule = scheduleIds.Select(scheduleId =>
                {
                    var peak = usage
                        .Where(entry => entry.Key.ResourceId == r.Id && entry.Key.ScheduleId == scheduleId)
                        .Select(entry => entry.Value)
                        .DefaultIfEmpty(0)
                        .Max();
                    return r.Quantity!.Value - peak;
                }).DefaultIfEmpty(0).ToList();

                var available = Math.Max(allSchedulesRequired ? freeBySchedule.Min() : freeBySchedule.Max(), 0);
                return new ResourceResponseDTO(r.Id, r.Name, r.Enabled, r.Description, r.Reservable, r.Quantity, available);
            })
            .Where(r => r.Available >= minQuantity)
            .Skip(query.Offset)
            .Take(query.Limit + 1)
            .ToList();
    }

    public async Task AdminCreateResource(ResourceCreateRequestDTO data)
    {
        var name = data.Name.Trim();
        await EnsureNameAvailable(name, null);

        if (data.Reservable && data.Quantity is not > 0)
            throw new BadRequestException("Informe a quantidade de unidades do recurso reservável.");

        var resource = new ResourceModel
        {
            Name = name,
            Description = NormalizeDescription(data.Description),
            Enabled = data.Enabled,
            Reservable = data.Reservable,
            Quantity = data.Reservable ? data.Quantity : null
        };

        dbCtx.Resources.Add(resource);
        await dbCtx.SaveChangesAsync();
    }

    public async Task AdminEditResource(ResourceEditRequestDTO data)
    {
        var resource = await dbCtx.Resources.FindAsync(data.Id);
        if (resource is null)
            throw new ResourceNotFoundException("Recurso não encontrado.");

        if (string.IsNullOrWhiteSpace(data.Name) && data.Enabled is null && data.Description is null
            && data.Reservable is null && data.Quantity is null)
            throw new BadRequestException("Informe ao menos um campo para alterar.");

        if (!string.IsNullOrWhiteSpace(data.Name) && data.Name.Trim() != resource.Name)
        {
            var name = data.Name.Trim();
            await EnsureNameAvailable(name, resource.Id);
            resource.Name = name;
        }

        if (data.Description != null)
            resource.Description = NormalizeDescription(data.Description);

        resource.Enabled = data.Enabled ?? resource.Enabled;

        var targetReservable = data.Reservable ?? resource.Reservable;
        var targetQuantity = targetReservable ? data.Quantity ?? resource.Quantity : null;

        if (targetReservable && targetQuantity is not > 0)
            throw new BadRequestException("Informe a quantidade de unidades do recurso reservável.");

        if (targetReservable && !resource.Reservable)
        {
            var linkedToSpaces = await dbCtx.SpaceResources.AnyAsync(sr => sr.FkResourceId == resource.Id);
            if (linkedToSpaces)
                throw new ConflictException(
                    "O recurso está associado a um ou mais espaços. Remova-o dos espaços antes de torná-lo reservável.");
        }

        if (resource.Reservable)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            if (!targetReservable)
            {
                var hasFutureReserves = await dbCtx.ResourceReserves
                    .Where(ResourceAvailability.IsActive)
                    .AnyAsync(rr => rr.FkResourceId == resource.Id && rr.DateTo >= today);
                if (hasFutureReserves)
                    throw new ConflictException(
                        "O recurso tem reservas ativas futuras. Cancele-as antes de torná-lo não reservável.");
            }
            else if (targetQuantity < resource.Quantity)
            {
                var lastDay = await dbCtx.ResourceReserves
                    .Where(ResourceAvailability.IsActive)
                    .Where(rr => rr.FkResourceId == resource.Id && rr.DateTo >= today)
                    .MaxAsync(rr => (DateOnly?)rr.DateTo);

                if (lastDay != null)
                {
                    var usage = await ResourceAvailability.LoadUsage(
                        dbCtx, [resource.Id], today, lastDay.Value, null, ReserveExclusion.None);
                    var peak = ResourceAvailability.PeakUsage(usage, resource.Id);
                    if (peak > targetQuantity)
                        throw new ConflictException(
                            $"Já existem reservas futuras usando {peak} unidade(s) ao mesmo tempo. " +
                            "A quantidade não pode ficar abaixo disso.");
                }
            }
        }

        resource.Reservable = targetReservable;
        resource.Quantity = targetQuantity;

        await dbCtx.SaveChangesAsync();
    }

    private async Task EnsureNameAvailable(string name, long? currentId)
    {
        var inUse = await dbCtx.Resources.AnyAsync(r => r.Id != currentId && r.Name == name);
        if (inUse)
            throw new ConflictException("Já existe um recurso com este nome.");
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
