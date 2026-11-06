using Backend.Services;
using DTOs.Error;
using DTOs.Report;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Owner")]
[Route("report")]
public class ReportController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Relatório de um período de meses
    /// </summary>
    /// <remarks>
    /// Retorna o relatório de ocupação e ociosidade de um mês ou de uma sequência de meses já encerrados
    /// (do mês anterior ao atual para trás). Informe <c>FromYear</c>/<c>FromMonth</c> e, para mais de um mês,
    /// também <c>ToYear</c>/<c>ToMonth</c>; sem o fim, o período é só o mês inicial.
    /// Reservas canceladas, recusadas e bloqueadas não entram. Reservas pendentes cuja data de início já passou
    /// contam como ausentes; pendentes que ainda não chegaram ao dia da reserva ficam de fora.
    /// Quando há empate nos rankings (espaços ou usuários), todos os empatados são retornados.
    /// O resultado é gravado na primeira consulta de cada período; use <c>Refresh=true</c> para recalcular.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ReportResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReport([FromQuery] ReportRequestDTO query)
    {
        var report = await reportService.GetReport(query);

        return Ok(report);
    }

    /// <summary>
    /// Relatório de recursos de um período de meses
    /// </summary>
    /// <remarks>
    /// Utilização e ociosidade dos recursos reserváveis, com as mesmas regras do relatório de salas (meses encerrados,
    /// pendentes já iniciadas contam como ausentes, empates retornam todos, cache por período com <c>Refresh</c>).
    /// A taxa de utilização considera a quantidade: unidades reservadas x horários x dias sobre
    /// unidades x horários por dia x dias do período.
    /// </remarks>
    [HttpGet("resources")]
    [ProducesResponseType(typeof(ResourceReportResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetResourceReport([FromQuery] ReportRequestDTO query)
    {
        var report = await reportService.GetResourceReport(query);

        return Ok(report);
    }
}
