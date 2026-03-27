using System.Text.Json;
using System.Threading.RateLimiting;
using Resend;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Tungsten.Api.Infrastructure.Identity;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence.Entities;
using Tungsten.Api.Common.Behaviours;
using Tungsten.Api.Common.Middleware;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Common.Services.AI;
using Tungsten.Api.Features.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Features.Compliance;
using Tungsten.Api.Features.CustodyEvents;
using Tungsten.Api.Features.Documents;
using Tungsten.Api.Features.DocumentGeneration;
using Tungsten.Api.Features.Notifications;
using Tungsten.Api.Features.Public;
using Tungsten.Api.Features.Users;
using Tungsten.Api.Features.Admin;
using Tungsten.Api.Features.Platform;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Features.Billing;
using Tungsten.Api.Features.Webhooks;
using Tungsten.Api.Features.Analytics;
using Tungsten.Api.Features.ApiKeys;
using Tungsten.Api.Features.AI;
using Tungsten.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Database — with Neon serverless resilience
// Neon's pooler drops idle connections after ~5 min. Keepalive + short pool lifetime
// ensures we don't hold stale TCP connections that cause "Exception while reading from stream".
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    + ";Pooling=true;Minimum Pool Size=0;Maximum Pool Size=20;Connection Idle Lifetime=60;Connection Pruning Interval=10;Keepalive=30;Timeout=15";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
        npgsql.CommandTimeout(30);
    }));

// Run migrations in background so Kestrel starts accepting requests immediately
builder.Services.AddHostedService<DatabaseMigrationService>();

// Identity
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<AppIdentityUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
.AddEntityFrameworkStores<IdentityDbContext>()
.AddDefaultTokenProviders();

// JWT bearer (self-issued tokens)
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30),
    };
});

builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

builder.Services.AddAuthorization(options => options.AddTungstenPolicies());
builder.Services.AddScoped<IAuthorizationHandler, RoleAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, TenantAccessHandler>();

// Services
if (!string.IsNullOrEmpty(builder.Configuration["R2:AccountId"]))
    builder.Services.AddSingleton<IFileStorageService, R2FileStorageService>();
else
    builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
if (!string.IsNullOrEmpty(builder.Configuration["Resend:ApiKey"]))
{
    builder.Services.AddOptions();
    builder.Services.AddHttpClient<ResendClient>();
    builder.Services.Configure<ResendClientOptions>(o =>
    {
        o.ApiToken = builder.Configuration["Resend:ApiKey"]!;
    });
    builder.Services.AddTransient<IResend, ResendClient>();
    builder.Services.AddSingleton<IEmailService, ResendEmailService>();
}
else
    builder.Services.AddSingleton<IEmailService, LogEmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IPlanEnforcementService, PlanEnforcementService>();
builder.Services.AddScoped<IWebhookDispatchService, WebhookDispatchService>();
builder.Services.AddSingleton<IAiService, OpenAiService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetMe.Handler>());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantStatusBehaviour<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehaviour<,>));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<GetMe.Handler>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200", "https://accutrac-web.onrender.com", "https://auditraks.com"];
        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public", opt =>
    {
        opt.PermitLimit = 30;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// Stripe
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrEmpty(stripeSecretKey))
    Stripe.StripeConfiguration.ApiKey = stripeSecretKey;

// Sentry
if (!string.IsNullOrEmpty(builder.Configuration["Sentry:Dsn"]))
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = builder.Configuration["Sentry:Dsn"];
        o.TracesSampleRate = 0.2;
    });
}

// OpenTelemetry (console exporter removed — noisy and adds I/O latency)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("tungsten-api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// JSON
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddHybridCache();

builder.Services.AddProblemDetails();

builder.Services.AddHealthChecks()
    .AddCheck("migrations", () => DatabaseMigrationService.IsReady
        ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
        : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Migrations running"));

var app = builder.Build();

// Migrations now run in DatabaseMigrationService (background)

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<AuditLoggingMiddleware>();

// Sentry user context
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var userId = context.User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userId is not null)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new Sentry.SentryUser { Id = userId };
            });
        }
    }
    await next();
});

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false,
});
app.MapHealthChecks("/health/ready");

// Auth endpoints
var auth = app.MapGroup("/api/auth").WithTags("Auth");
auth.MapPost("/login", Login.Handle).AllowAnonymous();
auth.MapPost("/refresh", RefreshToken.Handle).AllowAnonymous();
auth.MapPost("/logout", Logout.Handle).AllowAnonymous();
auth.MapPost("/register", Register.Handle).AllowAnonymous();
auth.MapPost("/forgot-password", ForgotPassword.Handle).AllowAnonymous().RequireRateLimiting("public");
auth.MapPost("/reset-password", ResetPassword.Handle).AllowAnonymous().RequireRateLimiting("public");
auth.MapGet("/confirm-email", ConfirmEmail.Handle).AllowAnonymous();
auth.MapPost("/resend-confirmation", ResendConfirmation.Handle).AllowAnonymous().RequireRateLimiting("public");

app.MapGet("/api/me", async (
    IMediator mediator,
    ILogger<Program> logger) =>
{
    try
    {
        var meResult = await mediator.Send(new GetMe.Query());
        if (meResult.IsSuccess)
            return Results.Ok(meResult.Value);

        return Results.Json(new { error = "No account found." }, statusCode: 403);
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Json(new { error = "Not authenticated." }, statusCode: 401);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "/api/me failed");
        return Results.Json(new { error = $"Login failed: {ex.Message}" }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapBatchEndpoints();
app.MapCustodyEventEndpoints();
app.MapComplianceEndpoints();
app.MapDocumentEndpoints();
app.MapDocumentGenerationEndpoints();
app.MapPublicEndpoints();
app.MapNotificationEndpoints();
app.MapUserEndpoints();
app.MapAdminEndpoints();
app.MapPlatformEndpoints();
app.MapSignupEndpoints();
app.MapBillingEndpoints();
app.MapWebhookEndpoints();
app.MapAnalyticsEndpoints();
app.MapApiKeyEndpoints();
app.MapAiEndpoints();

app.Run();

// Make Program accessible for WebApplicationFactory
public partial class Program { }
