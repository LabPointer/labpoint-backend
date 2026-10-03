using Backend.Services;
using DTOs.Error;
using DTOs.Resource;
using DTOs.Space;
using DTOs.Subject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("resource")]
public class ResourceController(IResourceService resourceService) : ControllerBase
{
    /// <summary>
    /// Listar todos os recursos
    /// </summary>
    /// <remarks>
    /// Lista todas os recursos cadastradas
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ResourceResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetResources([FromQuery] ResourceRequestDTO query)
    {
        var resources = await resourceService.GetResources(query);
        
        return Ok(resources);
    }

    /// <summary>
    /// Criar recurso
    /// </summary>
    /// <remarks>
    /// Cria um novo recurso. Apenas usuários com permissões adequadas podem criar recursos.
    /// </remarks>
    [HttpPost("manage/create")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostCreate([FromBody] ResourceCreateRequestDTO data)
    {
        await resourceService.CreateResource(data);

        return Created();
    }

    /// <summary>
    /// Editar recurso
    /// </summary>
    /// <remarks>
    /// Edita um recurso existente. Apenas usuários com permissões adequadas podem editar recursos.
    /// </remarks>
    [HttpPatch("manage/edit")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchEdit([FromBody] ResourceEditRequestDTO data)
    {
        await resourceService.EditResource(data);

        return NoContent();
    }
}