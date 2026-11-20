using System.Text.Json;
using Data;
using Microsoft.EntityFrameworkCore;
using Models.Email;

namespace Core.Email;

/// <summary>
/// Fila de e-mails (<c>email_outbox</c>), compartilhada pelo backend (pede e-mails de conta) e pelo serviço de e-mail
/// (avisos de check-in/check-out). Gravar no banco garante que nada se perde se o serviço estiver fora do ar.
/// </summary>
public static class EmailOutbox
{
    /// <summary>Canal do LISTEN/NOTIFY (só PostgreSQL). No SQLite o serviço de e-mail encontra os pedidos pela varredura.</summary>
    public const string NotifyChannel = "email_outbox";

    public const string CheckInTemplate = "check-in.html";
    public const string CheckOutTemplate = "check-out.html";

    /// <summary>Adiciona o e-mail ao contexto (salve com <c>SaveChanges</c> e depois chame <see cref="NotifyAsync"/>).</summary>
    public static EmailOutboxModel Add(AppDbContext dbCtx, string to, string subject, string template,
        IReadOnlyDictionary<string, string> values, long? usageId = null, DateTimeOffset? expiresAt = null)
    {
        var email = new EmailOutboxModel
        {
            ToAddress = to,
            Subject = subject,
            Template = template,
            Payload = JsonSerializer.Serialize(values),
            FkUsageId = usageId,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        dbCtx.EmailOutbox.Add(email);
        return email;
    }

    /// <summary>Acorda o serviço de e-mail na hora (PostgreSQL). Dentro de uma transação, o aviso sai no commit.</summary>
    public static async Task NotifyAsync(AppDbContext dbCtx, CancellationToken ct = default)
    {
        if (dbCtx.Database.SupportsNotify())
            await dbCtx.Database.ExecuteSqlRawAsync($"SELECT pg_notify('{NotifyChannel}', '')", ct);
    }
}
