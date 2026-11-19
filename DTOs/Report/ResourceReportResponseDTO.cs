namespace DTOs.Report;

/// <summary>
/// Indicadores de um recurso reservável no período. Taxas em porcentagem (0 a 100), com uma casa decimal.
/// </summary>
/// <param name="Quantity">Unidades do recurso.</param>
/// <param name="UtilizationRate">
/// Unidades reservadas (confirmadas + ausentes) x horários x dias, sobre a capacidade do recurso no período
/// (unidades x horários por dia x dias).
/// </param>
/// <param name="UnitsReserved">Soma das unidades pedidas nas reservas contabilizadas.</param>
/// <param name="IdleRate">Reservas ausentes sobre o total de reservas contabilizadas do recurso.</param>
public record ReportResourceStatDTO(
    long ResourceId,
    string Name,
    int Quantity,
    double UtilizationRate,
    int TotalReserves,
    int ConfirmedReserves,
    int AbsentReserves,
    int UnitsReserved,
    double IdleRate);

/// <param name="AverageUtilizationRate">Média da taxa de utilização de todos os recursos considerados.</param>
/// <param name="LinkedReserves">Reservas feitas junto com uma sala.</param>
/// <param name="StandaloneReserves">Reservas avulsas (sem sala).</param>
/// <param name="MostDemandedResources">Recursos com a maior taxa de utilização (todos os empatados).</param>
public record ResourceReportSummaryDTO(
    double AverageUtilizationRate,
    double IdleRate,
    int TotalReserves,
    int ConfirmedReserves,
    int AbsentReserves,
    int LinkedReserves,
    int StandaloneReserves,
    List<ReportResourceStatDTO> MostDemandedResources);

/// <remarks>Mesma regra de empate de <see cref="ReportRankingDTO"/>.</remarks>
public record ResourceReportRankingDTO(
    List<ReportResourceStatDTO> HighestDemand,
    List<ReportResourceStatDTO> LowestDemand,
    List<ReportResourceStatDTO> MostIdle);

/// <param name="SpaceName">Sala da reserva vinculada; nulo quando é avulsa.</param>
public record ResourceReportReserveDTO(
    long Id,
    string ResourceName,
    string UserName,
    int Quantity,
    string? SpaceName,
    DateOnly DateFrom,
    DateOnly DateTo,
    EReportReserveStatus Status);

/// <summary>Relatório de reservas de recursos. Mesmo período e regras do relatório de salas.</summary>
public record ResourceReportResponseDTO(
    int FromYear,
    int FromMonth,
    int ToYear,
    int ToMonth,
    DateTimeOffset GeneratedAt,
    ResourceReportSummaryDTO Summary,
    ResourceReportRankingDTO Ranking,
    ReportUsersDTO Users,
    List<ResourceReportReserveDTO> Reserves);
