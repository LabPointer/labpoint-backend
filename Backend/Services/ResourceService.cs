using Backend.Handler;
using Data;
using DTOs.Resource;
using DTOs.Space;
using Microsoft.EntityFrameworkCore;
using Models.Resource;

namespace Backend.Services;

public interface IResourceService
{
    public Task<List<ResourceResponseDTO>> GetResources(ResourceRequestDTO query);
    
    public Task EditResource(ResourceEditRequestDTO data);

    public Task CreateResource(ResourceCreateRequestDTO data);
}

public class ResourceService(AppDbContext dbCtx) : IResourceService
{
    public async Task<List<ResourceResponseDTO>> GetResources(ResourceRequestDTO query)
    {
        var resourceQuery = dbCtx.Resources.Where(r => r.CanReserve == query.CanReserve);
        if (!string.IsNullOrEmpty(query.Name))
        {
            resourceQuery = resourceQuery.Where(r => r.Name.Contains(query.Name));
        }
        
        var resources = await resourceQuery
            .Take(query.Limit + 1)
            .Skip(query.Offset)
            .Select(r => new ResourceResponseDTO(r.Id, r.Name, r.Description, r.CanReserve, r.Enabled))
            .ToListAsync();
        
        if (resources.Count == 0)
            throw new ResourceNotFoundException("No resources found");

        return resources;
    }

    public Task CreateResource(ResourceCreateRequestDTO data)
    {
        var resource = new ResourceModel
        {
            Name = data.Name,
            Description = data.Description,
            CanReserve = data.CanReserve,
            Enabled = data.Enabled
        };

        dbCtx.Resources.Add(resource);
        return dbCtx.SaveChangesAsync();
    }

    public Task EditResource(ResourceEditRequestDTO data)
    {
        var resource = dbCtx.Resources.Find(data.Id);
        if (resource is null)
            throw new ResourceNotFoundException("Resource not found");
        
        if (string.IsNullOrWhiteSpace(data.Name) && string.IsNullOrWhiteSpace(data.Description) && data.CanReserve is null && data.Enabled is null)
            throw new BadRequestException("No fields to update were provided");

        resource.Name = data.Name ?? resource.Name;
        resource.Description = data.Description ?? resource.Description;
        resource.CanReserve = data.CanReserve ?? resource.CanReserve;
        resource.Enabled = data.Enabled ?? resource.Enabled;

        dbCtx.Resources.Update(resource);
        return dbCtx.SaveChangesAsync();
    }
}