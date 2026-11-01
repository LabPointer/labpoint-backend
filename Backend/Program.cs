using System.Text.Json;
using Backend.Data;
using Backend.Handler;
using Backend.Services;
using Data;
using Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Account;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Settings
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("Frontend"));

// Services
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState.Values
                .SelectMany(entry => entry.Errors)
                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "Valor inválido."
                    : error.ErrorMessage);

            throw new BadRequestException(string.Join(" ", errors));
        };
    });
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DbConnection"),
        npgsql =>
        {
            npgsql.MapEnum<EShift>("shift_enum");
            npgsql.MapEnum<EReserveStatus>("reserve_status_enum");
        }
    ).UseLazyLoadingProxies();
    // Map enun type

});

builder.Services
    .AddIdentity<AccountModel, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ ";
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// O Render termina o HTTPS no proxy e repassa a requisição ao app por HTTP, avisando o esquema original em
// X-Forwarded-Proto. Sem isso Request.IsHttps seria sempre false e os cookies não sairiam como Secure.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // O IP do proxy do Render não é fixo, então não dá para listá-lo em KnownProxies.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Front (Vercel) e API (Render) ficam em sites diferentes: para o navegador aceitar e reenviar o cookie nas
// requisições cross-site (fetch com credentials: "include") ele precisa ser SameSite=None e Secure.
// Em Development (http://localhost) continua Lax/SameAsRequest, porque SameSite=None exige HTTPS.
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "auth-session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None;
    options.Cookie.SecurePolicy = isDevelopment ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Padrão do Identity: 30 min. Com 1 min, bloquear uma conta ou trocar o cargo (que renovam o SecurityStamp)
// derruba/atualiza a sessão já aberta quase imediatamente.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddSingleton<IBackgroundTaskService>(new BackgroundTaskService(capacity: 200));
builder.Services.AddHostedService<QueueService>();
builder.Services.AddScoped<IEmailSender<AccountModel>, SmtpEmailService>();
builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IResourceService, ResourceService>();
builder.Services.AddScoped<ISpaceService, SpaceService>();
builder.Services.AddScoped<ISpaceReserveService, SpaceReserveService>();
builder.Services.AddScoped<IResourceReserveService, ResourceReserveService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IReportService, ReportService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (allowedOrigins.Contains("*"))
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }
    else
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    }
}));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// Precisa vir antes de todo o resto para o esquema/IP originais valerem nos middlewares seguintes.
app.UseForwardedHeaders();

app.UseExceptionHandler();

/** Usar apenas em caso da chave for comprometida
var keyService = app.Services.GetRequiredService<IKeyManager>();
keyService.CreateNewKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));
keyService.RevokeAllKeys(DateTimeOffset.UtcNow, reason: "Chave comprometida");
*/

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/openapi/{documentName}.json");
    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint("/openapi/v1.json", "OpenAPI V1");
    });
    app.UseReDoc(options => { options.SpecUrl("/openapi/v1.json"); });
    app.MapScalarApiReference("/scalar");
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (await context.Database.CanConnectAsync())
    {
        Console.WriteLine("PostgreSQL connected!!");
        // Enum roles
        await IdentitySeeder.SeedRolesAsync(scope.ServiceProvider);

        if (app.Environment.IsDevelopment())
        {
            await DevelopmentDataSeeder.SeedAsync(context);
        }
    }
    else
    {
        Console.WriteLine("PostgreSQL connection failed!");
    }
}

app.Run();