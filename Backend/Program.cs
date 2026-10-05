using Backend.Data;
using Backend.Handler;
using Backend.Services;
using Data;
using Services;
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

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "auth-session";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

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
    }
    else
    {
        Console.WriteLine("PostgreSQL connection failed!");
    }
}

app.Run();