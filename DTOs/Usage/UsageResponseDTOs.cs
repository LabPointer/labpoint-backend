using DTOs.Schedule;
using Models;

namespace DTOs.Usage;

public enum EUsageStep
{
    CheckIn,
    CheckOut
}

public record UsageAccountDTO(string Id, string? UserName, string Registration);

public record UsageEntryResponseDTO(
    long ScheduleId,
    string ClassName,
    EUsageModality Modality,
    int NotebooksIn,
    int ChargersIn,
    int? NotebooksOut,
    int? ChargersOut);

/// <summary>Recurso usado na sessão: os vinculados à sala ou o próprio recurso da reserva avulsa.</summary>
public record UsageResourceDTO(long ResourceId, string Name, int Quantity);

/// <summary>Sessão de uso (reserva x dia x turno) com o relatório preenchido até agora.</summary>
/// <param name="SpaceName">Nulo em reserva de recurso avulsa.</param>
/// <param name="Schedules">Horários da reserva naquele turno, em ordem.</param>
/// <param name="CheckedOutBy">Admin que fechou o check-out; nulo quando foi o próprio usuário.</param>
public record UsageSessionResponseDTO(
    long Id,
    long? SpaceReserveId,
    long? ResourceReserveId,
    string? SpaceName,
    List<UsageResourceDTO> Resources,
    UsageAccountDTO Account,
    DateOnly Date,
    EShift Shift,
    EUsageStatus Status,
    List<ScheduleResponseDTO> Schedules,
    DateTimeOffset CheckInOpensAt,
    DateTimeOffset CheckInClosesAt,
    DateTimeOffset CheckOutOpensAt,
    DateTimeOffset CheckOutClosesAt,
    DateTimeOffset? CheckedInAt,
    DateTimeOffset? CheckedOutAt,
    UsageAccountDTO? CheckedOutBy,
    List<UsageEntryResponseDTO> Entries);

/// <summary>Sessão aberta por um link do e-mail e a etapa que o link permite.</summary>
public record UsageTokenResponseDTO(EUsageStep Step, UsageSessionResponseDTO Session);

/// <summary>Contagem das sessões de uso de uma reserva, por situação.</summary>
public record UsageSummaryDTO(int Total, int Scheduled, int CheckedIn, int CheckoutPending, int Completed, int Absent)
{
    public static readonly UsageSummaryDTO Empty = new(0, 0, 0, 0, 0, 0);
}
