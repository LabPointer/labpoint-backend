using Backend.Services;
using DTOs.Error;
using DTOs.ResourceReserve;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("resource-reserve")]
public class ResourceReserveController(IResourceReserveService resourceReserveService) : ControllerBase
{
    /// <summary>
    /// Listar reservas
    /// </summary>
    /// <remarks>
    /// Lista todas as reservas de recursos cadastradas de acordo com os filtros fornecidos
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResourceReserveResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResourceReserves([FromQuery] ResourceReserveRequestDTO query)
    {
        var reserves = await resourceReserveService.GetResourceReserves(User, query);

        return Ok(reserves);
    }

    /// <summary>
    /// Criar reserva
    /// </summary>
    /// <remarks>
    /// Cria uma nova reserva para o recurso especificado para um espaço especifico
    /// </remarks>
    [HttpPost("create/{spaceReserveId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostCreateResourceReserve(
        [FromRoute] long spaceReserveId,
        [FromBody] ResourceReserveCreateRequestDTO data)
    {
        await resourceReserveService.CreateResourceReserve(User, spaceReserveId, data);

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Editar reserva
    /// </summary>
    /// <remarks>
    /// Edita uma reserva existente para o recurso especificado para um espaço especifico
    /// </remarks>
    [HttpPatch("edit/{resourceReserveId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PatchEditResourceReserve(
        [FromRoute] long resourceReserveId,
        [FromBody] ResourceReserveEditRequestDTO data)
    {
        await resourceReserveService.EditResourceReserve(User, resourceReserveId, data);

        return NoContent();
    }

    /// <summary>
    /// Calcelar reserva
    /// </summary>
    /// <remarks>
    /// Cancela uma reserva existente para o recurso especificado para um espaço especifico
    /// </remarks>
    [HttpDelete("cancel/{resourceReserveId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteCancelResourceReserve([FromRoute] long resourceReserveId)
    {
        await resourceReserveService.CancelResourceReserve(User, resourceReserveId);

        return NoContent();
    }
}

