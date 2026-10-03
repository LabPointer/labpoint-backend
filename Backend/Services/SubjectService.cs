using Backend.Handler;
using Data;
using DTOs.Subject;
using Microsoft.EntityFrameworkCore;
using Models.Subject;

namespace Backend.Services;

public interface ISubjectService
{
    public Task<List<SubjectResponseDTO>> GetSubjects(SubjectRequestDTO query);
    public Task CreateSubject(SubjectCreateRequestDTO data);
    public Task EditSubject(SubjectEditRequestDTO data);
}

public class SubjectService(AppDbContext dbCtx) : ISubjectService
{
    public async Task<List<SubjectResponseDTO>> GetSubjects(SubjectRequestDTO query)
    {
        var subjectQuery = dbCtx.Subjects.Where(p => p.Enabled == query.IsActive);
        if (!string.IsNullOrEmpty(query.Name))
        {
            subjectQuery = subjectQuery.Where(p => p.Name.Contains(query.Name));
        }

        var subjects = await subjectQuery
            .Take(query.Limit + 1)
            .Skip(query.Offset)
            .Select(s => new SubjectResponseDTO(s.Id, s.Name, s.Enabled))
            .ToListAsync();
        
        if (subjects.Count == 0)
            throw new ResourceNotFoundException("Nenhuma disciplina encontrada");

        return subjects;
    }
    
    public async Task CreateSubject(SubjectCreateRequestDTO data)
    {
        var subject = new SubjectModel{ Name = data.Name, Enabled = data.Enabled };
        await dbCtx.Subjects.AddAsync(subject);
        await dbCtx.SaveChangesAsync();
    }
    
    public async Task EditSubject(SubjectEditRequestDTO data)
    {
        var subject = await dbCtx.Subjects.FindAsync(data.Id);
        if (subject is null)
            throw new ResourceNotFoundException("Disciplina não encontrada");

        subject.Name = data.Name;
        subject.Enabled = data.Enabled;
        await dbCtx.SaveChangesAsync();
    }
}