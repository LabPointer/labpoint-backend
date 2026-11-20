using System.Net;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Services;

namespace Email;

/// <summary>
/// Monta o HTML a partir de <c>Templates/{nome}</c> e envia por SMTP (MailKit).
/// Com <c>Smtp:Host</c> vazio, em Development o e-mail só é escrito no log (para testes); fora de Development o envio
/// falha, para nunca vazar links com token em log de produção.
/// </summary>
public sealed class SmtpMailer(IOptions<SmtpSettings> options, IHostEnvironment environment, ILogger<SmtpMailer> logger)
{
    private readonly SmtpSettings _settings = options.Value;

    public async Task<string> RenderAsync(string template, IReadOnlyDictionary<string, string> values, CancellationToken ct)
    {
        // Só o nome do arquivo: o template vem do banco e não pode apontar para fora da pasta.
        var path = Path.Combine(environment.ContentRootPath, "Templates", Path.GetFileName(template));
        if (!File.Exists(path))
            throw new FileNotFoundException($"Template de e-mail não encontrado: {template}");

        var html = await File.ReadAllTextAsync(path, ct);
        foreach (var (key, value) in values)
            html = html.Replace("{" + key + "}", WebUtility.HtmlEncode(value));

        return html;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
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
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrEmpty(_settings.User))
            await client.AuthenticateAsync(_settings.User, _settings.Password, ct);

        await client.SendAsync(new SendRequest(message), ct);
        await client.DisconnectAsync(true, ct);
    }
}
