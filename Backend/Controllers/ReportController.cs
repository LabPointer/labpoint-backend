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
    /// Relatório mensal
    /// </summary>
    /// <remarks>
    /// Retorna o relatório de ocupação e ociosidade de um mês já encerrado (do mês anterior para trás).
    /// Reservas canceladas, recusadas e bloqueadas não entram. Reservas pendentes cuja data de início já passou
    /// contam como ausentes; pendentes que ainda não chegaram ao dia da reserva ficam de fora.
    /// O resultado é gravado na primeira consulta de cada mês; use <c>Refresh=true</c> para recalcular.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ReportResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReport([FromQuery] ReportRequestDTO query)
    {
        var report = await reportService.GetMonthlyReport(query);

        return Ok(report);
    }
}
