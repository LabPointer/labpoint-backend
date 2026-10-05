using System.Net;
using System.Text.Json;
using Backend.Services;
using DTOs.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Models.Account;
using Services;

namespace Backend.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    UserManager<AccountModel> userManager,
    SignInManager<AccountModel> signInManager,
    IBackgroundTaskService service,
    IConfiguration configuration,
    IOptions<FrontendSettings> frontend,
    ILogger<AuthController> logger) : ControllerBase
{
    /// <summary>
    /// Registrar
    /// </summary>
    /// <remarks>
    /// Registra um novo usuário com a role "User" e envia um e-mail de confirmação.
    /// Apenas um Admin/Owner autenticado pode definir outra role através do campo "role".
    /// </remarks>
    [HttpPost("sign-up")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<IdentityError>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostSignUp([FromBody] SignUpRequestDTO body)
    {
        if (await userManager.Users.AnyAsync(u => u.Registration == body.Registration))
            return BadRequest(new[] { Error("DuplicateRegistration", "Matrícula já cadastrada.") });

        // Cadastro público SEMPRE vira "User": aceitar a role do body de qualquer um permitiria
        // que alguém se cadastrasse como Owner.
        var role = CallerCanAssignRoles() ? body.Role : EAccountRole.User;

        var user = new AccountModel
        {
            UserName = body.Username,
            Registration = body.Registration,
            Email = body.Email
        };

        var result = await userManager.CreateAsync(user, body.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        var roleResult = await userManager.AddToRoleAsync(user, role.ToString());
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return BadRequest(roleResult.Errors);
        }

        await SendEmailConfirmAsync(user);

        return Ok();
    }

    /// <summary>
    /// Confirmar e-mail
    /// </summary>
    /// <remarks>Rota aberta pelo link enviado por e-mail após o cadastro.</remarks>
    [HttpGet("confirm-email", Name = "ConfirmEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IEnumerable<IdentityError>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConfirmEmail([FromQuery] ConfirmEmailRequestDTO query)
    {
        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null)
            return BadRequest(new[] { InvalidToken() });

        var result = await userManager.ConfirmEmailAsync(user, query.Token);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return Ok();
    }

    /// <summary>
    /// Reenviar confirmação de e-mail
    /// </summary>
    /// <remarks>Responde sempre 202, exista a conta ou não, para não revelar quais e-mails estão cadastrados.</remarks>
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> PostResendConfirmation([FromBody] EmailRequestDTO body)
    {
        var user = await userManager.FindByEmailAsync(body.Email);

        if (user is not null && !await userManager.IsEmailConfirmedAsync(user))
        {
            await SendEmailConfirmAsync(user);
        }

        return Accepted();
    }

    /// <summary>
    /// Entrar
    /// </summary>
    /// <remarks>Autentica por matrícula e senha e grava o cookie de sessão.</remarks>
    [HttpPost("sign-in")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PostSignIn([FromBody] SignInRequestDTO body)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Registration == body.Registration);
        if (user is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Matrícula ou senha incorretos.");

        var result = await signInManager.PasswordSignInAsync(
            user, body.Password, body.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return Problem(statusCode: StatusCodes.Status403Forbidden,
                title: "Conta temporariamente bloqueada por excesso de tentativas. Tente novamente mais tarde.");

        if (result.IsNotAllowed)
            return Problem(statusCode: StatusCodes.Status403Forbidden,
                title: "Confirme seu e-mail antes de entrar.");

        if (!result.Succeeded)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Matrícula ou senha incorretos.");

        var roles = await userManager.GetRolesAsync(user);
        
        var cookieData = new { username = user.UserName ?? "Não definido", registration = user.Registration, roles };
        string jsonString = JsonSerializer.Serialize(cookieData);
        string cookieValue = WebUtility.UrlEncode(jsonString);
        var cookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(1)
        };
        HttpContext.Response.Cookies.Append("auth-info", cookieValue, cookieOptions);
        
        return Ok();
    }

    /// <summary>
    /// Sair
    /// </summary>
    /// <remarks>Remove o cookie de sessão.</remarks>
    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PostSignOut()
    {
        await signInManager.SignOutAsync();
        HttpContext.Response.Cookies.Delete("auth-session");
        HttpContext.Response.Cookies.Delete("auth-info");
        return Ok();
    }

    /// <summary>
    /// Solicitar redefinição de senha
    /// </summary>
    /// <remarks>Envia um e-mail com o link para o front-end. Responde sempre 202, exista a conta ou não.</remarks>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> PostForgotPassword([FromBody] EmailRequestDTO body)
    {
        var user = await userManager.FindByEmailAsync(body.Email);

        if (user is not null && !await userManager.IsEmailConfirmedAsync(user))
        {
            await SendPasswordResetAsync(user);
        }

        return Accepted();
    }

    /// <summary>
    /// Redefinir senha
    /// </summary>
    /// <remarks>Troca a senha usando o token recebido por e-mail.</remarks>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(IEnumerable<IdentityError>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostResetPassword([FromBody] ResetPasswordRequestDTO body)
    {
        var user = await userManager.FindByEmailAsync(body.Email);
        if (user is null)
            return BadRequest(new[] { InvalidToken() });

        var result = await userManager.ResetPasswordAsync(user, body.Token, body.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        return NoContent();
    }

    private async Task SendEmailConfirmAsync(AccountModel user)
    {
        await service.EnqueueAsync(async (sp, ct) =>
        {
            var emailService = sp.GetRequiredService<IEmailSender<AccountModel>>();
            var userService = sp.GetService<UserManager<AccountModel>>();
            var token = await userService.GenerateEmailConfirmationTokenAsync(user);
            var link = $"{frontend.Value.Url}/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(token)}";

            await emailService.SendConfirmationLinkAsync(user, user.Email!, link);
        });
    }
    
    private async Task SendPasswordResetAsync(AccountModel user)
    {
        await service.EnqueueAsync(async (sp, ct) =>
        {
            var emailService = sp.GetRequiredService<IEmailSender<AccountModel>>();
            var userService = sp.GetService<UserManager<AccountModel>>();
            var token = await userService.GenerateEmailConfirmationTokenAsync(user);
            var frontendUrl = frontend.Value.Url;
            var link = $"{frontendUrl.TrimEnd('/')}/reset-password" +
                       $"?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            await emailService.SendConfirmationLinkAsync(user, user.Email!, link);
        });
    }

    private bool CallerCanAssignRoles() =>
        User.IsInRole(nameof(EAccountRole.Admin)) || User.IsInRole(nameof(EAccountRole.Owner));

    private static IdentityError Error(string code, string description) =>
        new() { Code = code, Description = description };

    private static IdentityError InvalidToken() =>
        Error("InvalidToken", "Token inválido ou expirado.");
}