using System.Security.Cryptography;
using System.Text;

namespace Core;

/// <summary>
/// Tokens dos links de check-in/check-out: o token (base64url) vai só no e-mail; o banco guarda o SHA-256 dele.
/// Usado pelo backend (validação) e pelo serviço de e-mail (geração), que precisam do mesmo formato.
/// </summary>
public static class UsageTokens
{
    public static (string Token, string Hash) NewToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return (token, Hash(token));
    }

    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
