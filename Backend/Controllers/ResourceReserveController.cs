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
    /// Listar minhas reservas de recurso
    /// </summary>
    /// <remarks>
    /// Reservas de recurso do usuário autenticado (avulsas e vinculadas a uma sala). As datas filtram por
    /// sobreposição com o período. Retorna até <c>Limit + 1</c> itens: o extra indica que há próxima página.
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
    /// Disponibilidade de um recurso por horário
    /// </summary>
    /// <remarks>
    /// Para cada horário ativo, a menor quantidade de unidades livres do recurso no período.
    /// Na edição, informe a própria reserva em <c>ExcludeSpaceReserveId</c> ou <c>ExcludeResourceReserveId</c>
    /// para que as unidades dela não contem como ocupadas.
    /// </remarks>
    [HttpGet("availability/{resourceId}")]
    [ProducesResponseType(typeof(IEnumerable<ResourceScheduleAvailabilityDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailability([FromRoute] long resourceId, [FromQuery] ResourceAvailabilityRequestDTO query)
    {
        var availability = await resourceReserveService.GetAvailability(resourceId, query);
        return Ok(availability);
    }

    /// <summary>
    /// Reservar recurso
    /// </summary>
    /// <remarks>
    /// Reserva avulsa (sem sala) de N unidades de um recurso reservável. Recusa se, em algum dia e horário do
    /// período, não houver unidades livres suficientes.
    /// </remarks>
    [HttpPost("create/{resourceId}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PostCreateResourceReserve(
        [FromRoute] long resourceId,
        [FromBody] ResourceReserveCreateRequestDTO data)
    {
        await resourceReserveService.CreateResourceReserve(User, resourceId, data);
        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>
    /// Editar reserva de recurso
    /// </summary>
    /// <remarks>
    /// Altera período, horários e/ou quantidade. Se a reserva estiver vinculada a uma sala, o novo período e os
    /// novos horários valem também para a reserva da sala e para os outros recursos dela (todos são conferidos).
    /// </remarks>
    [HttpPatch("edit/{resourceReserveId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchEditResourceReserve(
        [FromRoute] long resourceReserveId,
        [FromBody] ResourceReserveEditRequestDTO data)
    {
        await resourceReserveService.EditResourceReserve(User, resourceReserveId, data);
        return NoContent();
    }

    /// <summary>
    /// Cancelar reserva de recurso
    /// </summary>
    /// <remarks>Marca a reserva como cancelada. Não cancela a reserva de sala a que ela está vinculada.</remarks>
    [HttpDelete("cancel/{resourceReserveId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCancelResourceReserve([FromRoute] long resourceReserveId)
    {
        await resourceReserveService.CancelResourceReserve(User, resourceReserveId);
        return NoContent();
    }

    /// <summary>
    /// Admin: listar reservas de recurso
    /// </summary>
    /// <remarks>
    /// Reservas de recurso dos outros usuários (exceto o autenticado) que se sobrepõem ao período. Retorna até <c>Limit + 1</c> itens.
    /// </remarks>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(typeof(IEnumerable<AdminResourceReserveResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminGetResourceReserves([FromQuery] AdminResourceReserveRequestDTO query)
    {
        var reserves = await resourceReserveService.AdminGetResourceReserves(User, query);
        return Ok(reserves);
    }

    /// <summary>
    /// Admin: editar reserva de recurso
    /// </summary>
    /// <remarks>
    /// Altera status, período, horários e/ou quantidade. O status do recurso não muda o da sala vinculada.
    /// </remarks>
    [HttpPatch("admin/edit/{resourceReserveId}")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminPatchEditResourceReserve(
        [FromRoute] long resourceReserveId,
        [FromBody] AdminResourceReserveEditRequestDTO data)
    {
        await resourceReserveService.AdminEditResourceReserve(User, resourceReserveId, data);
        return NoContent();
    }
}
