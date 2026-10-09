using Backend.Handler;
using Data;
using DTOs.Resource;
using DTOs.Space;
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
        var resourceQuery = dbCtx.Resources.AsQueryable();
        
        if (query.CanReserve.HasValue)
            resourceQuery = resourceQuery.Where(r => r.CanReserve == query.CanReserve);

        if (query.Enabled.HasValue)
            resourceQuery = resourceQuery.Where(r => r.Enabled == query.Enabled);

        if (!string.IsNullOrEmpty(query.Name))
        {
            resourceQuery = resourceQuery.Where(r => r.Name.Contains(query.Name));
        }

        var resources = await resourceQuery
            .Take(query.Limit + 1)
            .Skip(query.Offset * query.Limit)
            .Select(r => new ResourceResponseDTO(r.Id, r.Name, r.CanReserve, r.Enabled))
            .ToListAsync();
            
        if (resources.Count == 0)
            throw new ResourceNotFoundException("Nenhum recurso encontrado");

        return resources;
    }

    public Task AdminCreateResource(ResourceCreateRequestDTO data)
    {
        var resource = new ResourceModel
        {
            Name = data.Name,
            CanReserve = data.CanReserve,
            Enabled = data.Enabled
        };

        dbCtx.Resources.Add(resource);
        return dbCtx.SaveChangesAsync();
    }

    public Task AdminEditResource(ResourceEditRequestDTO data)
    {
        var resource = dbCtx.Resources.Find(data.Id);
        if (resource is null)
            throw new ResourceNotFoundException("Resource not found");
        
        if (string.IsNullOrWhiteSpace(data.Name) && data.CanReserve is null && data.Enabled is null)
            throw new BadRequestException("No fields to update were provided");

        resource.Name = data.Name ?? resource.Name;
        resource.CanReserve = data.CanReserve ?? resource.CanReserve;
        resource.Enabled = data.Enabled ?? resource.Enabled;

        dbCtx.Resources.Update(resource);
        return dbCtx.SaveChangesAsync();
    }
}