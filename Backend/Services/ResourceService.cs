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
        if (string.IsNullOrEmpty(query.Name))
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
}