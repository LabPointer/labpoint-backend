using Backend.Services;
using DTOs.Error;
using DTOs.Subject;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("subject")]
public class SubjectController(
    ISubjectService subjectService) : ControllerBase
{
    /// <summary>
    /// Listar todas as matérias
    /// </summary>
    /// <remarks>
    /// Lista todas as matérias cadastradas
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubjectResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubjects([FromQuery] SubjectRequestDTO query)
    {
        var subjects = await subjectService.GetSubjects(query);
        
        return Ok(subjects);
    }

    /// <summary>
    /// Criar matéria
    /// </summary>
    /// <remarks>
    /// Cria uma nova matéria. Apenas usuários com permissões adequadas podem criar matérias.
    /// </remarks>
    [HttpPost("manage/create")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostCreate([FromBody] SubjectCreateRequestDTO data)
    {
        await subjectService.CreateSubject(data);
        
        return Created();
    }

    /// <summary>
    /// Editar matéria
    /// </summary>
    /// <remarks>
    /// Edita os dados de uma matéria. Apenas usuários com permissões adequadas podem editar matérias.
    /// </remarks>
    [HttpPatch("manage/edit")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(typeof(object), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PatchEdit([FromBody] SubjectEditRequestDTO data)
    {
        await subjectService.EditSubject(data);
        
        return NoContent();
    }
}