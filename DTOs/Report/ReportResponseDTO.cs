namespace DTOs.Report;

/// <summary>Situação de uma reserva dentro do relatório (só entram reservas confirmadas ou ausentes).</summary>
public enum EReportReserveStatus
{
    Confirmed,
    Absent
}

/// <summary>
/// Indicadores de um espaço no mês. Taxas em porcentagem (0 a 100), com uma casa decimal.
/// </summary>
/// <param name="OccupancyRate">Períodos reservados (confirmados + ausentes) sobre a capacidade do espaço no mês.</param>
/// <param name="IdleRate">Reservas ausentes sobre o total de reservas contabilizadas do espaço.</param>
public record ReportSpaceStatDTO(
    long SpaceId,
    string Name,
    double OccupancyRate,
    int TotalReserves,
    int ConfirmedReserves,
    int AbsentReserves,
    double IdleRate);

/// <param name="IdleRate">Reservas ausentes sobre o total de reservas contabilizadas do usuário.</param>
public record ReportUserStatDTO(
    string AccountId,
    string Name,
    string Registration,
    int TotalReserves,
    int ConfirmedReserves,
    int AbsentReserves,
    double IdleRate);

/// <param name="AverageOccupancyRate">Média da taxa de ocupação de todos os espaços considerados.</param>
/// <param name="IdleRate">Ausências sobre o total de reservas contabilizadas.</param>
/// <param name="MostDisputedSpace">Espaço com maior taxa de ocupação (nulo se nenhum teve reservas).</param>
public record ReportSummaryDTO(
    double AverageOccupancyRate,
    double IdleRate,
    int TotalReserves,
    int ConfirmedReserves,
    int AbsentReserves,
    ReportSpaceStatDTO? MostDisputedSpace);

/// <param name="HighestDemand">Espaço com maior taxa de ocupação.</param>
/// <param name="LowestDemand">Espaço com menor taxa de ocupação (nulo quando há menos de dois espaços).</param>
/// <param name="MostIdle">Espaço com mais reservas ausentes (nulo se não houve ausências).</param>
public record ReportRankingDTO(
    ReportSpaceStatDTO? HighestDemand,
    ReportSpaceStatDTO? LowestDemand,
    ReportSpaceStatDTO? MostIdle);

/// <param name="MostReserves">Usuário com mais reservas no mês.</param>
/// <param name="MostIdle">Usuário com mais reservas ausentes (nulo se não houve ausências).</param>
public record ReportUsersDTO(
    ReportUserStatDTO? MostReserves,
    ReportUserStatDTO? MostIdle);

public record ReportReserveDTO(
    long Id,
    string SpaceName,
    string UserName,
    DateOnly DateFrom,
    DateOnly DateTo,
    EReportReserveStatus Status);

public record ReportResponseDTO(
    int Year,
    int Month,
    DateTimeOffset GeneratedAt,
    ReportSummaryDTO Summary,
    ReportRankingDTO Ranking,
    ReportUsersDTO Users,
    List<ReportReserveDTO> Reserves);
