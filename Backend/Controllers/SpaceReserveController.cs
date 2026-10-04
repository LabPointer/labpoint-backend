using Backend.Services;
using DTOs.Error;
using DTOs.Schedule;
using DTOs.Space;
using DTOs.SpaceReserve;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("space-reserve")]
public class SpaceReserveController(ISpaceReserveService reserveService) : ControllerBase
{
    /// <summary>
    /// Listar reservas
    /// </summary>
    /// <remarks>
    /// Lista todos os espaços cadastrados
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SpaceResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReserves([FromQuery] SpaceReserveRequestDTO query)
    {
        var spaceReserve = await reserveService.GetSpaceReserve(User, query);

        return Ok(spaceReserve);
    }

    /// <summary>
    /// Pegar horarios
    /// </summary>
    /// <remarks>
    /// Retorna uma lista de horarios ja reservados para um espaco especifico de acordo com data range fornecido
    /// </remarks>
    [HttpGet("existing-schedules/{spaceId}")]
    [ProducesResponseType(typeof(List<ScheduleResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExistingSchedules([FromRoute] long spaceId, [FromQuery] ExistingScheduleRequestDTO query)
    {
        var existingSchedules = await reserveService.GetExistingSchedules(User, spaceId, query);

        return Ok(existingSchedules);
    }

    /// <summary>
    /// Criar reserva
    /// </summary>
    /// <remarks>
    /// Cria uma nova reserva para o espaço especificado, com base nas datas fornecidas e no usuário autenticado
    /// </remarks>
    [HttpPost("create/{spaceId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostCreateReserve([FromRoute] long spaceId, [FromBody] SpaceReserveCreateRequestDTO data)
    {
        await reserveService.CreateSpaceReserve(User, spaceId, data);

        return Ok();
    }

    /// <summary>
    /// Editar reserva
    /// </summary>
    /// <remarks>
    /// Edita a data e horário de uma reserva de espaço existente.
    /// </remarks>
    [HttpPatch("edit/{reserveId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchEditReserve([FromRoute] long reserveId, [FromBody] SpaceReserveEditRequestDTO data)
    {
        await reserveService.EditSpaceReserve(User, reserveId, data);

        return Ok();
    }

    /// <summary>
    /// Cancelar reserva
    /// </summary>
    /// <remarks>
    /// Cancela uma reserva de espaço existente.
    /// </remarks>
    [HttpDelete("cancel/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCancelReserve([FromRoute] long id)
    {
        await reserveService.CancelSpaceReserve(User, id);

        return Ok();
    }
}