using Backend.Services;
using DTOs.Error;
using DTOs.Resource;
using DTOs.Space;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("space")]
public class SpaceController(ISpaceService spaceService) : ControllerBase
{
    /// <summary>
    /// Listar todos os espaços
    /// </summary>
    /// <remarks>
    /// Lista todos os espaços cadastrados
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SpaceResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSpaces([FromQuery] SpaceRequestDTO query)
    {
        var resources = await spaceService.GetSpaces(query);

        return Ok(resources);
    }

    /// <summary>
    /// Criar espaço
    /// </summary>
    /// <remarks>
    /// Cria um novo espaço com base nos parâmetros fornecidos. Apenas usuários com permissões adequadas podem criar espaços.
    /// </remarks>
    [HttpPost("manage/create")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostCreate([FromBody] SpaceCreateRequestDTO data)
    {
        await spaceService.CreateSpace(data);

        return Created();
    }

    /// <summary>
    /// Editar espaço
    /// </summary>
    /// <remarks>
    /// Edita os dados de um espaço existente. Apenas usuários com permissões adequadas podem editar espaços.
    /// </remarks>
    [HttpPatch("manage/edit")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchEdit([FromBody] SpaceEditRequestDTO data)
    {
        await spaceService.EditSpace(data);

        return NoContent();
    }
}