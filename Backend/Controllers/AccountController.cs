using Backend.Services;
using DTOs.Account;
using DTOs.Error;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Authorize]
[Route("account")]
public class AccountController(IAccountService accountService) : ControllerBase
{
    /// <summary>
    /// Informação da conta
    /// </summary>
    /// <remarks>
    /// Retorna informações do usuário autenticado, incluindo ID, matrícula, nome de usuário, e-mail e cargo.
    /// </remarks>
    [HttpGet("@me")]
    [ProducesResponseType(typeof(AccountResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMe()
    {
        var me = await accountService.GetAccount(User);
        
        return Ok(me);
    }

    /// <summary>
    /// Editar conta
    /// </summary>
    /// <remarks>
    /// Edita as informações da conta do usuário autenticado, incluindo nome de usuário.
    /// </remarks>
    [HttpPatch("edit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PatchEdit([FromBody] AccountEditRequestDTO data)
    {
        await accountService.EditAccount(User, data);
        
        return NoContent();
    }

    /// <summary>
    /// Admin: listar usuarios
    /// </summary>
    /// <remarks>
    /// Lista todos os usuários cadastrados no sistema.
    /// </remarks>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(typeof(IEnumerable<AccountResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminGetUsers([FromQuery] AdminAccountRequestDTO query)
    {
        var users = await accountService.AdminGetUsers(User, query);
        
        return Ok(users);
    }

    /// <summary>
    /// Admin: listar usuarios
    /// </summary>
    /// <remarks>
    /// Lista todos os usuários cadastrados no sistema.
    /// </remarks>
    [HttpPatch("admin/edit")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminPatchUsers([FromBody] AdminAccountEditRequestDTO data)
    {
        await accountService.AdminEditAccount(User, data);
        
        return NoContent();
    }
}

