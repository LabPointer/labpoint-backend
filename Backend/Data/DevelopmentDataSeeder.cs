using System.Text.Json;
using System.Text.Json.Serialization;
using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Resource;
using Models.Schedule;
using Models.Subject;

namespace Backend.Data;

public static class DevelopmentDataSeeder
{
    private const string SeedFileName = "Data/development-seed.json";

    public static async Task SeedAsync(AppDbContext context)
    {
        var seedPath = Path.Combine(AppContext.BaseDirectory, SeedFileName);
        if (!File.Exists(seedPath))
            throw new FileNotFoundException("Arquivo de seed de desenvolvimento não encontrado.", seedPath);

        await using var stream = File.OpenRead(seedPath);
        var data = await JsonSerializer.DeserializeAsync<DevelopmentSeedData>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        });

        if (data is null)
            throw new InvalidOperationException("O arquivo de seed de desenvolvimento está vazio ou inválido.");

        var existingSpaceNames = await context.Spaces
            .Select(space => space.Name)
            .ToHashSetAsync();
        var existingResourceNames = await context.Resources
            .Select(resource => resource.Name)
            .ToHashSetAsync();
        var existingSubjectNames = await context.Subjects
            .Select(subject => subject.Name)
            .ToHashSetAsync();
        var existingSchedules = await context.Schedules
            .Select(schedule => new { schedule.StartAt, schedule.EndAt, schedule.Shift })
            .ToHashSetAsync();

        context.Spaces.AddRange(data.Spaces
            .Where(space => existingSpaceNames.Add(space.Name))
            .Select(space => new SpaceModel
            {
                Name = space.Name,
                Description = space.Description,
                Capacity = space.Capacity,
                Locked = space.Locked
            }));

        context.Resources.AddRange(data.Resources
            .Where(resource => existingResourceNames.Add(resource.Name))
            .Select(resource => new ResourceModel
            {
                Name = resource.Name,
                Enabled = resource.Enabled
            }));

        context.Subjects.AddRange(data.Subjects
            .Where(subject => existingSubjectNames.Add(subject.Name))
            .Select(subject => new SubjectModel
            {
                Name = subject.Name,
                Enabled = subject.Enabled
            }));

        context.Schedules.AddRange(data.Schedules
            .Where(schedule => existingSchedules.Add(new
            {
                schedule.StartAt,
                schedule.EndAt,
                schedule.Shift
            }))
            .Select(schedule => new ScheduleModel
            {
                StartAt = schedule.StartAt,
                EndAt = schedule.EndAt,
                Shift = schedule.Shift,
                Enabled = schedule.Enabled
            }));

        await context.SaveChangesAsync();
    }

    private sealed class DevelopmentSeedData
    {
        public List<SpaceSeed> Spaces { get; init; } = [];
        public List<ResourceSeed> Resources { get; init; } = [];
        public List<ScheduleSeed> Schedules { get; init; } = [];
        public List<SubjectSeed> Subjects { get; init; } = [];
    }

    private sealed class SpaceSeed
    {
        public required string Name { get; init; }
        public required string Description { get; init; }
        public int Capacity { get; init; }
        public bool Locked { get; init; }
    }

    private sealed class ResourceSeed
    {
        public required string Name { get; init; }
        public bool Enabled { get; init; }
    }

    private sealed class ScheduleSeed
    {
        public TimeOnly StartAt { get; init; }
        public TimeOnly EndAt { get; init; }
        public EShift Shift { get; init; }
        public bool Enabled { get; init; }
    }

    private sealed class SubjectSeed
    {
        public required string Name { get; init; }
        public bool Enabled { get; init; }
    }
}
