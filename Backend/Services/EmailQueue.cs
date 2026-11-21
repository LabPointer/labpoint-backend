using Core.Email;
using Data;
using Microsoft.AspNetCore.Identity;
using Models.Account;

namespace Backend.Services;

/// <summary>
/// Pede o envio de um e-mail ao serviço de e-mail (projeto Email): grava na fila <c>email_outbox</c> e, no PostgreSQL,
/// avisa com <c>NOTIFY email_outbox</c>. O backend não fala com o SMTP; o HTML é montado pelo serviço a partir do template.
/// </summary>
public interface IEmailQueue
{
    /// <param name="to">Destinatário.</param>
    /// <param name="subject">Assunto.</param>
    /// <param name="template">Arquivo em <c>Email/Templates</c> (ex.: <c>reset-password.html</c>).</param>
    /// <param name="values">Valores de cada <c>{chave}</c> do template, em texto puro.</param>
    /// <param name="ct">Cancelamento.</param>
    Task EnqueueAsync(string to, string subject, string template, IReadOnlyDictionary<string, string> values,
        CancellationToken ct = default);
}

/// <summary>Também é o <see cref="IEmailSender{TUser}"/> do Identity (confirmação de e-mail e redefinição de senha).</summary>
public sealed class EmailQueue(AppDbContext dbCtx) : IEmailQueue, IEmailSender<AccountModel>
{
    public async Task EnqueueAsync(string to, string subject, string template,
        IReadOnlyDictionary<string, string> values, CancellationToken ct = default)
    {
        EmailOutbox.Add(dbCtx, to, subject, template, values);
        await dbCtx.SaveChangesAsync(ct);

        // Só acorda o serviço; se ele estiver fora do ar, a varredura periódica pega o e-mail depois.
        await EmailOutbox.NotifyAsync(dbCtx, ct);
    }

    public Task SendConfirmationLinkAsync(AccountModel user, string email, string confirmationLink) =>
        EnqueueAsync(email, "Confirme seu e-mail", "confirm-email.html", new Dictionary<string, string>
        {
            ["user"] = user.UserName ?? "",
            ["confirm-link"] = confirmationLink
        });

    public Task SendPasswordResetLinkAsync(AccountModel user, string email, string resetLink) =>
        EnqueueAsync(email, "Redefinição de senha", "reset-password.html", new Dictionary<string, string>
        {
            ["user"] = user.UserName ?? "",
            ["reset-link"] = resetLink
        });

    public Task SendPasswordResetCodeAsync(AccountModel user, string email, string resetCode) =>
        EnqueueAsync(email, "Código de redefinição de senha", "reset-code.html", new Dictionary<string, string>
        {
            ["user"] = user.UserName ?? "",
            ["code"] = resetCode
        });
}
