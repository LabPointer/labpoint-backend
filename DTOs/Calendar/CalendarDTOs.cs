using System.ComponentModel.DataAnnotations;
using Models;

namespace DTOs.Calendar;

/// <summary>Período consultado no calendário (no máximo 400 dias).</summary>
public record CalendarDaysRequestDTO(
    [Required(ErrorMessage = "A data de início é obrigatória.")]
    DateOnly StartAt,
    [Required(ErrorMessage = "A data de término é obrigatória.")]
    DateOnly EndAt);

public record HolidayResponseDTO(long Id, DateOnly Date, string Name, EHolidaySource Source);

/// <summary>
/// Situação de um dia para reservas. <c>Blocked</c>: fim de semana ou feriado sem liberação ativa.
/// <c>Released</c>: liberado por um admin (só faz sentido em fim de semana ou feriado).
/// </summary>
public record CalendarDayResponseDTO(
    DateOnly Date,
    bool IsWeekend,
    List<HolidayResponseDTO> Holidays,
    bool Released,
    bool Blocked);

public record ReleaseDatesRequestDTO(
    [Required(ErrorMessage = "Informe as datas a liberar.")]
    [MinLength(1, ErrorMessage = "Informe ao menos uma data.")]
    [MaxLength(62, ErrorMessage = "Libere no máximo 62 datas por vez.")]
    List<DateOnly> Dates);

/// <summary><c>KeepReserves</c>: true mantém as reservas já feitas para o dia; false cancela.</summary>
public record RevokeDateRequestDTO(
    [Required(ErrorMessage = "A data é obrigatória.")]
    DateOnly Date,
    bool KeepReserves);

/// <summary>Reservas ativas (pendentes ou confirmadas) que usam o dia.</summary>
public record AffectedReservesResponseDTO(DateOnly Date, int SpaceReserves, int ResourceReserves);

public record HolidayCreateRequestDTO(
    [Required(ErrorMessage = "A data é obrigatória.")]
    DateOnly Date,
    [Required(ErrorMessage = "O nome é obrigatório.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
    string Name);
