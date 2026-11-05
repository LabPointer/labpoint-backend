using Backend.Handler;
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
    /// Edita a conta do usuário autenticado: nome de usuário e/ou matéria associada (campos nulos são ignorados).
    /// Regrava o cookie auth-info com os dados atualizados e devolve a conta.
    /// </remarks>
    [HttpPatch("edit")]
    [ProducesResponseType(typeof(AccountResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchEdit([FromBody] AccountEditRequestDTO data)
    {
        var account = await accountService.EditAccount(User, data);

        // Mantém o nome exibido no front em dia sem exigir novo login.
        AuthInfoCookie.Write(HttpContext, account.Username, account.Registration, [account.Role.ToString()]);

        return Ok(account);
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
    /// Admin: editar usuário
    /// </summary>
    /// <remarks>
    /// Somente o Owner altera cargos. Ativar/desativar a conta exige cargo superior ao do usuário alvo
    /// (ordem crescente: User, Admin, Owner).
    /// </remarks>
    [HttpPatch("admin/edit")]
    [Authorize(Roles = "Admin,Owner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AdminPatchUsers([FromBody] AdminAccountEditRequestDTO data)
    {
        await accountService.AdminEditAccount(User, data);
        
        return NoContent();
    }
}

