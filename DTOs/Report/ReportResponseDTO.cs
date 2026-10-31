namespace DTOs.Report;

/// <summary>Situação de uma reserva dentro do relatório (só entram reservas confirmadas ou ausentes).</summary>
public enum EReportReserveStatus
{
    Confirmed,
    Absent
}

/// <summary>
/// Indicadores de um espaço no período. Taxas em porcentagem (0 a 100), com uma casa decimal.
/// </summary>
/// <param name="OccupancyRate">Períodos reservados (confirmados + ausentes) sobre a capacidade do espaço no período.</param>
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
/// <param name="MostDisputedSpaces">
/// Espaços com a maior taxa de ocupação. Tem mais de um item quando há empate na taxa; vazio se nenhum teve reservas.
/// </param>
public record ReportSummaryDTO(
    double AverageOccupancyRate,
    double IdleRate,
    int TotalReserves,
    int ConfirmedReserves,
    int AbsentReserves,
    List<ReportSpaceStatDTO> MostDisputedSpaces);

/// <remarks>
/// Cada ranking é uma lista porque pode haver empate: todos os espaços (ou usuários) com o mesmo valor
/// no critério do ranking aparecem, ordenados pelo nome. Lista vazia = ninguém se enquadra.
/// </remarks>
/// <param name="HighestDemand">Espaços com a maior taxa de ocupação (taxa maior que zero).</param>
/// <param name="LowestDemand">
/// Espaços com a menor taxa de ocupação. Vazio quando há menos de dois espaços ou quando todos empatam
/// na mesma taxa (não existe um "menos disputado" diferente do "mais disputado").
/// </param>
/// <param name="MostIdle">
/// Espaços com mais reservas ausentes; o empate só vale quando a quantidade de ausências e a taxa de ociosidade
/// são iguais. Vazio se não houve ausências.
/// </param>
public record ReportRankingDTO(
    List<ReportSpaceStatDTO> HighestDemand,
    List<ReportSpaceStatDTO> LowestDemand,
    List<ReportSpaceStatDTO> MostIdle);

/// <remarks>Mesma regra de empate de <see cref="ReportRankingDTO"/>.</remarks>
/// <param name="MostReserves">Usuários com mais reservas no período (todos os que empatam na quantidade).</param>
/// <param name="MostIdle">
/// Usuários com mais reservas ausentes; o empate só vale quando a quantidade de ausências e a taxa de ociosidade
/// são iguais. Vazio se não houve ausências.
/// </param>
public record ReportUsersDTO(
    List<ReportUserStatDTO> MostReserves,
    List<ReportUserStatDTO> MostIdle);

public record ReportReserveDTO(
    long Id,
    string SpaceName,
    string UserName,
    DateOnly DateFrom,
    DateOnly DateTo,
    EReportReserveStatus Status);

/// <summary>Período coberto: do mês inicial ao final, ambos inclusos (iguais quando é um mês só).</summary>
public record ReportResponseDTO(
    int FromYear,
    int FromMonth,
    int ToYear,
    int ToMonth,
    DateTimeOffset GeneratedAt,
    ReportSummaryDTO Summary,
    ReportRankingDTO Ranking,
    ReportUsersDTO Users,
    List<ReportReserveDTO> Reserves);
