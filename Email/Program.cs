using Data;
using Email;
using Services;

// Serviço de e-mail do LabPoint: envia o que o backend coloca na fila email_outbox e agenda os avisos de
// check-in/check-out. Não expõe HTTP: o único ponto de contato com o backend é o banco (PostgreSQL ou SQLite,
// conforme Database:Provider, igual ao backend).
var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("Frontend"));
builder.Services.Configure<EmailServiceSettings>(builder.Configuration.GetSection("EmailService"));

if (string.IsNullOrWhiteSpace(builder.Configuration["Frontend:Url"]))
    throw new InvalidOperationException("Configure Frontend:Url (usado nos links de check-in/check-out).");

builder.Services.AddLabpointDatabase(builder.Configuration);
builder.Services.AddSingleton<SmtpMailer>();
builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddHostedService<UsageEmailScheduler>();

builder.Build().Run();
