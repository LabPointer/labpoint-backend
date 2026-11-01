using System.Net;
using System.Text.Json;
using Backend.Handler;
using Backend.Services;
using DTOs.Auth;
using DTOs.Error;
using MailKit;
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
    IBackgroundTaskService queueService,
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
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostSignUp([FromBody] SignUpRequestDTO body)
    {
        if (await userManager.Users.AnyAsync(u => u.Registration == body.Registration))
            throw new BadRequestException("Matrícula já cadastrada.");

        var user = new AccountModel
        {
            UserName = body.Username,
            Registration = body.Registration,
            Email = body.Email
        };

        var result = await userManager.CreateAsync(user, body.Password);
        if (!result.Succeeded)
            throw new BadRequestException($"Erro ao criar usuário. {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, "User");

        await SendEmailConfirmAsync(user);

        return Ok();
    }

    /// <summary>
    /// Confirmar e-mail
    /// </summary>
    /// <remarks>Rota aberta pelo link enviado por e-mail após o cadastro.</remarks>
    [HttpGet("confirm-email", Name = "ConfirmEmail")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetConfirmEmail([FromQuery] ConfirmEmailRequestDTO query)
    {
        var user = await userManager.FindByIdAsync(query.UserId);
        if (user is null)
            throw new BadRequestException("Token inválido ou expirado.");

        var result = await userManager.ConfirmEmailAsync(user, query.Token);
        if (!result.Succeeded)
            throw new BadRequestException($"Token inválido ou expirado. {string.Join(", ", result.Errors.Select(e => e.Description))}");

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

        if (user is not null && !user.EmailConfirmed)
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
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> PostSignIn([FromBody] SignInRequestDTO body)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Registration == body.Registration);
        if (user is null)
            throw new UnauthorizedException("Matrícula ou senha incorretos.");

        var result = await signInManager.PasswordSignInAsync(
            user, body.Password, body.RememberMe, lockoutOnFailure: true);

        if (result.IsLockedOut)
            throw new ForbiddenException("Conta temporariamente bloqueada por excesso de tentativas. Tente novamente mais tarde.");

        if (result.IsNotAllowed)
            throw new ForbiddenException("Confirme seu e-mail antes de entrar.");

        if (!result.Succeeded)
            throw new UnauthorizedException("Matrícula ou senha incorretos.");

        var roles = await userManager.GetRolesAsync(user);
        
        var cookieData = new { username = user.UserName ?? "Não definido", registration = user.Registration, roles };
        string jsonString = JsonSerializer.Serialize(cookieData);
        string cookieValue = WebUtility.UrlEncode(jsonString);
        var cookieOptions = BuildInfoCookieOptions();
        cookieOptions.Expires = DateTimeOffset.UtcNow.AddDays(1);
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
        // SignOutAsync já apaga o auth-session com os mesmos atributos (SameSite/Secure) com que ele foi criado.
        await signInManager.SignOutAsync();
        // Para apagar, o Set-Cookie precisa repetir os atributos do cookie original; um SameSite diferente
        // seria ignorado pelo navegador em requisições cross-site.
        HttpContext.Response.Cookies.Delete("auth-info", BuildInfoCookieOptions());
        return Ok();
    }

    /// <summary>
    /// Opções do cookie auth-info (dados de exibição lidos pelo front, por isso HttpOnly = false).
    /// Sob HTTPS (Render, via X-Forwarded-Proto) sai SameSite=None; Secure para valer entre o front e a API em
    /// sites diferentes; em http://localhost continua Lax.
    /// </summary>
    private CookieOptions BuildInfoCookieOptions() => new()
    {
        HttpOnly = false,
        Secure = Request.IsHttps,
        SameSite = Request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax
    };

    /// <summary>
    /// Solicitar redefinição de senha
    /// </summary>
    /// <remarks>Envia um e-mail com o link para o front-end. Responde sempre 202, exista a conta ou não.</remarks>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostForgotPassword([FromBody] EmailRequestDTO body)
    {
        var user = await userManager.FindByEmailAsync(body.Email);

        if (user is not null && user.EmailConfirmed)
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
    [ProducesResponseType(typeof(ErroResponseDTO), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PostResetPassword([FromBody] ResetPasswordRequestDTO body)
    {
        var user = await userManager.FindByEmailAsync(body.Email);
        if (user is null)
            throw new BadRequestException("Token inválido ou expirado.");

        var result = await userManager.ResetPasswordAsync(user, body.Token, body.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException($"Token inválido ou expirado. {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return NoContent();
    }

    private async Task SendEmailConfirmAsync(AccountModel user)
    {
        await queueService.EnqueueAsync(async (sp, ct) =>
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
        await queueService.EnqueueAsync(async (sp, ct) =>
        {
            var emailService = sp.GetRequiredService<IEmailSender<AccountModel>>();
            var userService = sp.GetService<UserManager<AccountModel>>();
            var token = await userService.GeneratePasswordResetTokenAsync(user);
            var frontendUrl = frontend.Value.Url;
            var link = $"{frontendUrl.TrimEnd('/')}/reset-password" +
                       $"?email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}";

            await emailService.SendPasswordResetLinkAsync(user, user.Email!, link);
        });
    }

    private static IdentityError Error(string code, string description) =>
        new() { Code = code, Description = description };

    private static IdentityError InvalidToken() =>
        Error("InvalidToken", "Token inválido ou expirado.");
}