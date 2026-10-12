using System.Net;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MimeKit;
using Models;
using Models.Account;
using Services;

namespace Backend.Services;

/// <summary>
/// Implementação do IEmailSender&lt;TUser&gt; do Identity usando MailKit.
/// Se Smtp:Host estiver vazio, em Development o e-mail (com o link) é só escrito no log, para facilitar testes.
/// Fora de Development, lança exceção, para nunca vazar tokens em log de produção.
/// </summary>
public class SmtpEmailService(
    IOptions<SmtpSettings> options,
    IHostEnvironment environment,
    ILogger<SmtpEmailService> logger) : IEmailSender<AccountModel>
{
    private readonly SmtpSettings _settings = options.Value;

    public async Task SendConfirmationLinkAsync(AccountModel user, string email, string confirmationLink)
    {
        var templatePath = Path.Combine(environment.ContentRootPath, "Templates", "Email", "confirm-email.html");
        
        string htmlBody;

        if (File.Exists(templatePath))
        {
            htmlBody = await File.ReadAllTextAsync(templatePath);

            htmlBody = htmlBody
                .Replace("{user}", WebUtility.HtmlEncode(user.UserName))
                .Replace("{confirm-link}", WebUtility.HtmlEncode(confirmationLink));
        }
        else
        {
            htmlBody = $"<p>Olá, {WebUtility.HtmlEncode(user.UserName)}!</p>" +
                       $"<p>Para ativar sua conta, <a href=\"{WebUtility.HtmlEncode(confirmationLink)}\">confirme seu e-mail</a>.</p>";
        }
        
        await SendAsync(email, "Confirme seu e-mail", htmlBody);
    }

    public async Task SendPasswordResetLinkAsync(AccountModel user, string email, string resetLink)
    {
        var templatePath = Path.Combine(environment.ContentRootPath, "Templates", "Email", "reset-password.html");
        
        string htmlBody;

        if (File.Exists(templatePath))
        {
            htmlBody = await File.ReadAllTextAsync(templatePath);

            htmlBody = htmlBody
                .Replace("{user}", WebUtility.HtmlEncode(user.UserName))
                .Replace("{reset-link}", WebUtility.HtmlEncode(resetLink));
        }
        else
        {
            htmlBody = $"<p>Olá, {WebUtility.HtmlEncode(user.UserName)}!</p>" +
                       $"<p>Para criar uma nova senha, <a href=\"{WebUtility.HtmlEncode(resetLink)}\">clique aqui</a>.</p>" +
                       "<p>Se você não pediu isso, ignore este e-mail.</p>";
        }
        
        await SendAsync(email, "Redefinição de senha", htmlBody);
    }


    public async Task SendPasswordResetCodeAsync(AccountModel user, string email, string resetCode)
    {
        await SendAsync(email, "Código de redefinição de senha",
            $"<p>Seu código de redefinição: <strong>{WebUtility.HtmlEncode(resetCode)}</strong></p>");
    }
        

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host))
        {
            if (!environment.IsDevelopment())
                throw new InvalidOperationException("SMTP não configurado (seção 'Smtp').");

            logger.LogWarning("SMTP não configurado. E-mail NÃO enviado. Para: {To} | Assunto: {Subject} | Corpo: {Body}",
                to, subject, htmlBody);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.Auto);

        if (!string.IsNullOrEmpty(_settings.User))
            await client.AuthenticateAsync(_settings.User, _settings.Password);

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}