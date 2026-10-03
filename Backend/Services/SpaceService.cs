using Backend.Handler;
using Data;
using DTOs.Space;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Models;

namespace Backend.Services;

public interface ISpaceService
{
    public Task<List<SpaceResponseDTO>> GetSpaces(SpaceRequestDTO query);

    public Task CreateSpace(SpaceCreateRequestDTO data);

    public Task EditSpace(SpaceEditRequestDTO data);
}

public class SpaceService(AppDbContext dbCtx) : ISpaceService
{
    public async Task<List<SpaceResponseDTO>> GetSpaces(SpaceRequestDTO query)
    {
        var queryable = dbCtx.Spaces.AsNoTracking();

        // Name filter
        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            queryable = queryable.Where(s => s.Name.Contains(query.Name));
        }

        // Capacity filter
        if (query.Capacity > 0)
        {
            queryable = queryable.Where(s => s.Capacity >= query.Capacity);
        }

        // Locked filter
        queryable = queryable.Where(s => s.Locked == query.Locked);

        // Subjects filter
        if (query.Subjects != null && query.Subjects.Count > 0)
        {
            queryable = queryable.Where(s => s.SpaceSubjects
                .Select(ss => ss.FkSubjectId)
                .Any(id => query.Subjects.Contains(id)));
        }

        // Resources filter
        if (query.Resources != null && query.Resources.Count > 0)
        {
            queryable = queryable.Where(s => s.SpaceResources
                .Select(sr => sr.FkResourceId)
                .Any(id => query.Resources.Contains(id)));
        }

        // Date and schedule filter
        if (query.StartAt != null && query.EndAt != null)
        {
            if (query.StartAt > query.EndAt)
            {
                throw new BadRequestException("A data de início não pode ser maior que a data de término.");
            }
            queryable = queryable.Where(s => !s.SpaceReserves.Any(r =>
                // Checa interseção de períodos de data
                r.DateFrom <= query.EndAt && r.DateTo >= query.StartAt
            ));

            if (query.Schedules != null && query.Schedules.Count > 0)
            {
                queryable = queryable.Where(s => !s.SpaceReserves.Any(r =>
                    // Checa se algum dos horários pesquisados já está ocupado nessa reserva
                    r.SpaceReserveSchedules.Any(rss => query.Schedules.Contains(rss.FkScheduleId))
                ));
            }
        }

        var spaces = await queryable
            .Take(query.Limit + 1)
            .Skip(query.Offset)
            .Select(s => new SpaceResponseDTO(
                s.Id,
                s.Name,
                s.Capacity,
                s.Description,
                s.Locked,
                s.SpaceSubjects.Select(ss => new SpaceHashDataDTO(ss.FkSubjectId, ss.Subject.Name)).ToHashSet(),
                s.SpaceResources.Select(sr => new SpaceHashDataDTO(sr.FkResourceId, sr.Resource.Name)).ToHashSet()
            ))
            .ToListAsync();

        if (spaces.Count == 0)
        {
            throw new ResourceNotFoundException("Nenhum espaço encontrado com os filtros informados.");
        }

        return spaces;
    }

    public async Task CreateSpace(SpaceCreateRequestDTO data)
    {
        if (data.Subjects != null && data.Subjects.Count > 0)
        {
            var distinctSubjectIds = data.Subjects.Distinct().ToList();

            var existingSubjectCount = await dbCtx.Subjects
                .CountAsync(s => distinctSubjectIds.Contains(s.Id));

            if (existingSubjectCount != distinctSubjectIds.Count)
            {
                throw new BadRequestException("Uma ou mais matérias informadas não existem no sistema.");
            }
        }

        if (data.Resources != null && data.Resources.Count > 0)
        {
            var distinctResourceIds = data.Resources.Distinct().ToList();

            var existingResourceCount = await dbCtx.Resources
                .CountAsync(r => distinctResourceIds.Contains(r.Id));

            if (existingResourceCount != distinctResourceIds.Count)
            {
                throw new BadRequestException("Uma ou mais recursos informados não existem no sistema.");
            }
        }

        var space = new SpaceModel
        {
            Name = data.Name,
            Description = data.Description,
            Capacity = data.Capacity,
            Locked = data.Locked,

            SpaceSubjects = data.Subjects?
                .Select(id => new SpaceSubjectModel { FkSubjectId = id })
                .ToList() ?? new List<SpaceSubjectModel>(),

            SpaceResources = data.Resources?
                .Select(id => new SpaceResourceModel { FkResourceId = id })
                .ToList() ?? new List<SpaceResourceModel>()
        };

        dbCtx.Spaces.Add(space);
        await dbCtx.SaveChangesAsync();
    }

    public async Task EditSpace(SpaceEditRequestDTO data)
    {
        var space = await dbCtx.Spaces
            .Include(s => s.SpaceSubjects)
            .Include(s => s.SpaceResources)
            .FirstOrDefaultAsync(s => s.Id == data.Id);

        if (space == null)
        {
            throw new ResourceNotFoundException("Espaço não encontrado.");
        }

        space.Name = data.Name ?? space.Name;
        space.Description = data.Description ?? space.Description;
        space.Capacity = data.Capacity ?? space.Capacity;
        space.Locked = data.Locked ?? space.Locked;

        var newSubjectIds = data.Subjects?.Distinct().ToList();
        var newResourceIds = data.Resources?.Distinct().ToList();

        var validationTasks = new List<Task>();

        List<long> subjectsToValidate = null;
        if (newSubjectIds != null)
        {
            var currentSubjectIds = space.SpaceSubjects.Select(ss => ss.FkSubjectId).ToHashSet();
            subjectsToValidate = newSubjectIds.Where(id => !currentSubjectIds.Contains(id)).ToList();

            if (subjectsToValidate.Count > 0)
            {
                validationTasks.Add(ValidateSubjectsExist(subjectsToValidate));
            }
        }

        List<long> resourcesToValidate = null;
        if (newResourceIds != null)
        {
            var currentResourceIds = space.SpaceResources.Select(sr => sr.FkResourceId).ToHashSet();
            resourcesToValidate = newResourceIds.Where(id => !currentResourceIds.Contains(id)).ToList();

            if (resourcesToValidate.Count > 0)
            {
                validationTasks.Add(ValidateResourcesExist(resourcesToValidate));
            }
        }

        if (validationTasks.Count > 0)
        {
            await Task.WhenAll(validationTasks);
        }

        if (newSubjectIds != null)
        {
            var requestedSubjectsSet = newSubjectIds.ToHashSet();

            var subjectsToRemove = space.SpaceSubjects
                .Where(ss => !requestedSubjectsSet.Contains(ss.FkSubjectId))
                .ToList();

            foreach (var item in subjectsToRemove)
            {
                space.SpaceSubjects.Remove(item);
            }

            var currentSubjectsSet = space.SpaceSubjects.Select(ss => ss.FkSubjectId).ToHashSet();
            foreach (var subjectId in requestedSubjectsSet)
            {
                if (!currentSubjectsSet.Contains(subjectId))
                {
                    space.SpaceSubjects.Add(new SpaceSubjectModel { FkSubjectId = subjectId });
                }
            }
        }

        if (newResourceIds != null)
        {
            var requestedResourcesSet = newResourceIds.ToHashSet();

            var resourcesToRemove = space.SpaceResources
                .Where(sr => !requestedResourcesSet.Contains(sr.FkResourceId))
                .ToList();

            foreach (var item in resourcesToRemove)
            {
                space.SpaceResources.Remove(item);
            }

            var currentResourcesSet = space.SpaceResources.Select(sr => sr.FkResourceId).ToHashSet();
            foreach (var resourceId in requestedResourcesSet)
            {
                if (!currentResourcesSet.Contains(resourceId))
                {
                    space.SpaceResources.Add(new SpaceResourceModel { FkResourceId = resourceId });
                }
            }
        }

        await dbCtx.SaveChangesAsync();
    }
    private async Task ValidateSubjectsExist(List<long> subjectIds)
    {
        var count = await dbCtx.Subjects.CountAsync(s => subjectIds.Contains(s.Id));
        if (count != subjectIds.Count)
        {
            throw new BadRequestException("Uma ou mais matérias informadas não existem no sistema.");
        }
    }

    private async Task ValidateResourcesExist(List<long> resourceIds)
    {
        var count = await dbCtx.Resources.CountAsync(r => resourceIds.Contains(r.Id));
        if (count != resourceIds.Count)
        {
            throw new BadRequestException("Uma ou mais recursos informados não existem no sistema.");
        }
    }
}
