using System.Security.Claims;
using Backend.Services;
using DTOs.Error;
using DTOs.Space;
using DTOs.SpaceReserve;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("reserves")]
public class ReserveController(ClaimsPrincipal principal, IReserveService reserveService) : ControllerBase
{
    /// <summary>
    /// Listar reservas
    /// </summary>
    /// <remarks>
    /// Lista todos os espaços cadastrados
    /// </remarks>
    [HttpGet("space")]
    [ProducesResponseType(typeof(IEnumerable<SpaceResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpaceReserves([FromQuery] SpaceReserveRequestDTO query)
    {
        var spaceReserve = await reserveService.GetSpaceReserve(principal, query);

        return Ok(spaceReserve);
    }
}