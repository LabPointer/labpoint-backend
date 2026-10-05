using System.ComponentModel.DataAnnotations;

namespace DTOs.Space;

public record SpaceRequestDTO(
    string? Name,
    [Range(10, 200, ErrorMessage = "O valor deve estar entre 10 e 200.")]
    int? Capacity,
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos um recurso para o filtro")]
    HashSet<long>? Resources,
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos um assunto para o filtro")]
    HashSet<long>? Subjects,
    DateOnly? StartAt,
    DateOnly? EndAt,
    [MinLength(1, ErrorMessage = "É necessário informar pelo menos um horário para o filtro")]
    HashSet<long>? Schedules,
    bool Locked = false,
    int Limit = 10,
    int Offset = 0);