using System.Net;
using System.Text.Json;

namespace Backend.Handler;

/// <summary>
/// Cookie auth-info: dados de exibição da sessão (nome, matrícula, cargos) lidos pelo front, por isso HttpOnly = false.
/// Não serve para autenticação; isso fica no cookie de sessão do Identity.
/// </summary>
public static class AuthInfoCookie
{
    public const string Name = "auth-info";

    public static void Write(HttpContext ctx, string? username, string registration, IEnumerable<string> roles)
    {
        var cookieData = new { username = username ?? "Não definido", registration, roles };
        var cookieValue = WebUtility.UrlEncode(JsonSerializer.Serialize(cookieData));

        var options = BuildOptions(ctx.Request);
        options.Expires = DateTimeOffset.UtcNow.AddDays(1);

        ctx.Response.Cookies.Append(Name, cookieValue, options);
    }

    /// <summary>
    /// Para apagar, o Set-Cookie precisa repetir os atributos do cookie original; um SameSite diferente
    /// seria ignorado pelo navegador em requisições cross-site.
    /// </summary>
    public static void Delete(HttpContext ctx) => ctx.Response.Cookies.Delete(Name, BuildOptions(ctx.Request));

    /// <summary>
    /// Sob HTTPS (Render, via X-Forwarded-Proto) sai SameSite=None; Secure para valer entre o front e a API em
    /// sites diferentes; em http://localhost continua Lax.
    /// </summary>
    private static CookieOptions BuildOptions(HttpRequest request) => new()
    {
        HttpOnly = false,
        Secure = request.IsHttps,
        SameSite = request.IsHttps ? SameSiteMode.None : SameSiteMode.Lax
    };
}
