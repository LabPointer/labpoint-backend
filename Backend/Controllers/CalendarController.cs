using Backend.Services;
using DTOs.Calendar;
using DTOs.Error;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("calendar")]
public class CalendarController(ICalendarService calendarService) : ControllerBase
{
    /// <summary>
    /// Dias do calendário
    /// </summary>
    /// <remarks>
    /// Situação de cada dia do período (no máximo 400 dias): fim de semana, feriados, liberado pela coordenação e
    /// bloqueado para reservas. Fins de semana e feriados ficam bloqueados até um admin liberar a data.
    /// </remarks>
    [HttpGet("days")]
    [ProducesResponseType(typeof(IEnumerable<CalendarDayResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDays([FromQuery] CalendarDaysRequestDTO query)
    {
        return Ok(await calendarService.GetDays(query));
    }

    /// <summary>
    /// Liberar datas
    /// </summary>
    /// <remarks>
    /// Libera fins de semana e feriados para reservas. Datas já liberadas são ignoradas.
    /// </remarks>
    [HttpPost("admin/release")]
    [Authorize(Roles = "Admin,Root")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdminPostRelease([FromBody] ReleaseDatesRequestDTO data)
    {
        await calendarService.AdminRelease(User, data);
        return NoContent();
    }

    /// <summary>
    /// Reservas afetadas
    /// </summary>
    /// <remarks>
    /// Quantas reservas ativas (de sala e de recurso avulsa) usam a data, para decidir entre manter e cancelar
    /// antes de bloquear o dia de novo.
    /// </remarks>
    [HttpGet("admin/affected")]
    [Authorize(Roles = "Admin,Root")]
    [ProducesResponseType(typeof(AffectedReservesResponseDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdminGetAffected([FromQuery] DateOnly date)
    {
        return Ok(await calendarService.AdminGetAffected(date));
    }

    /// <summary>
    /// Bloquear data de novo
    /// </summary>
    /// <remarks>
    /// Remove a liberação. Com "keepReserves", as reservas já feitas continuam usando o dia; sem, o dia sai delas e
    /// as que ficam sem nenhum outro dia são canceladas.
    /// </remarks>
    [HttpPost("admin/revoke")]
    [Authorize(Roles = "Admin,Root")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminPostRevoke([FromBody] RevokeDateRequestDTO data)
    {
        await calendarService.AdminRevoke(data);
        return NoContent();
    }

    /// <summary>
    /// Cadastrar feriado extra
    /// </summary>
    /// <remarks>
    /// Feriado que não vem da lista nacional, estadual ou municipal (recesso, ponte, feriado decretado).
    /// </remarks>
    [HttpPost("admin/holiday")]
    [Authorize(Roles = "Admin,Root")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdminPostHoliday([FromBody] HolidayCreateRequestDTO data)
    {
        await calendarService.AdminCreateHoliday(data);
        return Created();
    }

    /// <summary>
    /// Remover feriado extra
    /// </summary>
    /// <remarks>
    /// Só feriados cadastrados pela coordenação podem ser removidos.
    /// </remarks>
    [HttpDelete("admin/holiday/{holidayId:long}")]
    [Authorize(Roles = "Admin,Root")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminDeleteHoliday(long holidayId)
    {
        await calendarService.AdminDeleteHoliday(holidayId);
        return NoContent();
    }
}
