using Backend.Services;
using DTOs.Error;
using DTOs.Resource;
using DTOs.Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("schedule")]
public class ScheduleController(IScheduleService scheduleService) : ControllerBase
{
    /// <summary>
    /// Listar todos os horarios
    /// </summary>
    /// <remarks>
    /// Lista todas os horarios cadastrados
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ScheduleResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedules([FromQuery] ScheduleRequestDTO query)
    {
        var schedules = await scheduleService.GetSchedules(query);
        
        return Ok(schedules);
    }

    /// <summary>
    /// Criar horario
    /// </summary>
    /// <remarks>
    /// Cria um novo horario. Apenas usuários com permissões adequadas podem criar horarios.
    /// </remarks>
    [HttpPost("admin/create")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostCreate([FromBody] ScheduleCreateRequestDTO data)
    {
        await scheduleService.AdminCreateSchedule(data);

        return Created();
    }

    /// <summary>
    /// Editar horario
    /// </summary>
    /// <remarks>
    /// Edita um horario existente. Apenas usuários com permissões adequadas podem editar horarios.
    /// </remarks>
    [HttpPatch("admin/edit")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchEdit([FromBody] ScheduleEditRequestDTO data)
    {
        await scheduleService.AdminEditSchedule(data);

        return NoContent();
    }
}