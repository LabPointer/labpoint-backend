using System.ComponentModel.DataAnnotations;

namespace DTOs.Space;

public record SpaceRequestDTO(
    string? Name,
    [Range(10, 200, ErrorMessage = "O valor deve estar entre 10 e 200.")]
    int? Capacity,
    HashSet<long> Resources,
    HashSet<long> Subjects,
    DateOnly? StartAt,
    DateOnly? EndAt,
    HashSet<long> Schedules,
    bool Locked = false,
    int Limit = 10,
    int Offset = 0);