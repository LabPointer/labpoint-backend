using System.Text.Json;
using Core.Email;
using Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Npgsql;

namespace Email;

/// <summary>
/// Consome a fila <c>email_outbox</c>. No PostgreSQL fica em <c>LISTEN email_outbox</c> para acordar assim que um e-mail
/// é gravado; nos dois bancos varre a fila periodicamente (pega o que ficou para trás e as novas tentativas).
/// Falhas são tentadas de novo com espera crescente até <c>MaxAttempts</c>. Avisos cujo prazo (<c>expires_at</c>) passou
/// não são enviados: ficam com status Expirado.
/// Cada e-mail é "reservado" antes do envio (UPDATE condicional em <c>next_attempt_at</c>), então mais de uma
/// instância pode rodar sem mandar o mesmo e-mail duas vezes.
/// </summary>
public sealed class OutboxDispatcher(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    SmtpMailer mailer,
    IOptions<EmailServiceSettings> options,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    /// <summary>Tempo que um e-mail fica reservado para esta instância enquanto é enviado.</summary>
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(5);

    private static readonly TimeSpan[] Backoff =
    [
        TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1)
    ];

    private readonly EmailServiceSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var poll = TimeSpan.FromSeconds(Math.Max(_settings.OutboxPollSeconds, 1));

        while (!stoppingToken.IsCancellationRequested)
        {
            NpgsqlConnection? listener = null;
            try
            {
                listener = await OpenListener(stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    while (await ProcessBatch(stoppingToken) > 0)
                    {
                        // Esvazia o que estiver pronto antes de voltar a esperar.
                    }

                    // Volta quando chega um NOTIFY (PostgreSQL) ou quando o intervalo de varredura passa.
                    if (listener != null)
                        await listener.WaitAsync(poll, stoppingToken);
                    else
                        await Task.Delay(poll, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Banco fora do ar ou conexão caiu: espera um pouco e reconecta.
                logger.LogError(ex, "Erro na fila de e-mails; tentando de novo em {Delay}", poll);
                await Task.Delay(poll, stoppingToken);
            }
            finally
            {
                if (listener != null)
                    await listener.DisposeAsync();
            }
        }
    }

    /// <summary>Conexão dedicada ao LISTEN (só PostgreSQL). Nulo no SQLite.</summary>
    private async Task<NpgsqlConnection?> OpenListener(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (!db.Database.SupportsNotify())
        {
            logger.LogInformation("Banco sem LISTEN/NOTIFY: verificando a fila a cada {Seconds} s",
                _settings.OutboxPollSeconds);
            return null;
        }

        // A string do contexto perde a senha depois de aberta; usa a da configuração.
        var connection = new NpgsqlConnection(configuration.GetConnectionString("DbConnection"));
        await connection.OpenAsync(ct);
        await using (var listen = new NpgsqlCommand($"LISTEN {EmailOutbox.NotifyChannel}", connection))
            await listen.ExecuteNonQueryAsync(ct);

        logger.LogInformation("Escutando o canal {Channel}", EmailOutbox.NotifyChannel);
        return connection;
    }

    /// <summary>Marca os vencidos e envia um lote de e-mails prontos. Devolve quantos foram processados.</summary>
    private async Task<int> ProcessBatch(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTimeOffset.UtcNow;

        var expired = await db.EmailOutbox
            .Where(e => e.Status == EEmailStatus.Pending && e.ExpiresAt != null && e.ExpiresAt <= now)
            .ExecuteUpdateAsync(set => set
                .SetProperty(e => e.Status, EEmailStatus.Expired)
                .SetProperty(e => e.LastError, "O prazo do aviso passou antes do envio; o link já não valeria."), ct);
        if (expired > 0)
            logger.LogWarning("{Count} aviso(s) expirado(s) sem envio", expired);

        var batch = await db.EmailOutbox.AsNoTracking()
            .Where(e => e.Status == EEmailStatus.Pending && e.NextAttemptAt <= now)
            .OrderBy(e => e.Id)
            .Take(Math.Max(_settings.BatchSize, 1))
            .ToListAsync(ct);

        foreach (var email in batch)
        {
            // Reserva: só quem trocar o next_attempt_at lido envia este e-mail.
            var claimed = await db.EmailOutbox
                .Where(e => e.Id == email.Id && e.Status == EEmailStatus.Pending && e.NextAttemptAt == email.NextAttemptAt)
                .ExecuteUpdateAsync(set => set.SetProperty(e => e.NextAttemptAt, now + Lease), ct);
            if (claimed == 0)
                continue;

            var attempts = email.Attempts + 1;
            try
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(email.Payload) ?? [];
                var html = await mailer.RenderAsync(email.Template, values, ct);
                await mailer.SendAsync(email.ToAddress, email.Subject, html, ct);

                var sentAt = DateTimeOffset.UtcNow;
                await db.EmailOutbox.Where(e => e.Id == email.Id).ExecuteUpdateAsync(set => set
                    .SetProperty(e => e.Status, EEmailStatus.Sent)
                    .SetProperty(e => e.Attempts, attempts)
                    .SetProperty(e => e.SentAt, sentAt)
                    .SetProperty(e => e.LastError, (string?)null), ct);
                logger.LogInformation("E-mail {EmailId} ({Template}) enviado", email.Id, email.Template);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var giveUp = attempts >= _settings.MaxAttempts;
                var retryAt = DateTimeOffset.UtcNow + Backoff[Math.Min(attempts - 1, Backoff.Length - 1)];
                var error = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                await db.EmailOutbox.Where(e => e.Id == email.Id).ExecuteUpdateAsync(set => set
                    .SetProperty(e => e.Status, giveUp ? EEmailStatus.Failed : EEmailStatus.Pending)
                    .SetProperty(e => e.Attempts, attempts)
                    .SetProperty(e => e.NextAttemptAt, retryAt)
                    .SetProperty(e => e.LastError, error), ct);
                logger.LogError(ex, "Falha ao enviar o e-mail {EmailId} (tentativa {Attempt}{Final})",
                    email.Id, attempts, giveUp ? ", desistindo" : "");
            }
        }

        return batch.Count + expired;
    }
}
