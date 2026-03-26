using System.Text.Json;
using System.Threading.RateLimiting;
using Resend;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsql =>
    {
        npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);
        npgsql.CommandTimeout(30);
    }));

// Run migrations in background so Kestrel starts accepting requests immediately
builder.Services.AddHostedService<DatabaseMigrationService>();

// Auth0
var auth0Domain = builder.Configuration["Auth0:Domain"];
var auth0Audience = builder.Configuration["Auth0:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrEmpty(auth0Domain))
        {
            options.Authority = $"https://{auth0Domain}/";
            options.Audience = auth0Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://{auth0Domain}/",
                ValidateAudience = true,
                ValidAudience = auth0Audience,
                ValidateLifetime = true,
            };
        }
        else
        {
            // Dev mode: no Auth0 configured — allow anonymous for testing
            options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // Skip token validation when no Auth0 is configured
                    context.NoResult();
                    return Task.CompletedTask;
                }
            };
        }
    });

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
builder.Services.AddSingleton<IAiService, ClaudeAiService>();
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
            .AllowAnyMethod();
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

// Sentry user context (Auth0 sub only — no PII)
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var sub = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (sub is not null)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new Sentry.SentryUser { Id = sub };
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
app.MapGet("/api/me", async (HttpContext httpContext, IMediator mediator, AppDbContext db, ICurrentUserService currentUser, ILogger<Program> logger) =>
{
    try
    {
    var auth0Sub = currentUser.Auth0Sub;

    // Extract email and name from token claims
    var email = httpContext.User.FindFirst("email")?.Value
        ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        ?? httpContext.User.FindFirst("https://auditraks.com/email")?.Value;
    var name = httpContext.User.FindFirst("name")?.Value
        ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? httpContext.User.FindFirst("https://auditraks.com/name")?.Value
        ?? "User";

    // Check if a user with this Auth0Sub exists but has the wrong email (mis-linked)
    if (!string.IsNullOrEmpty(email))
    {
        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Auth0Sub == auth0Sub);
        if (existingUser is not null && existingUser.Email != email)
        {
            // This Auth0Sub was linked to the wrong user (e.g. fallback "user@auditraks.com").
            // Unlink it so the correct user can be found/created below.
            existingUser.Auth0Sub = $"unlinked|{existingUser.Auth0Sub}";
            existingUser.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        else if (existingUser is not null)
        {
            // Correct match — return the user
            var result = await mediator.Send(new GetMe.Query());
            if (result.IsSuccess)
                return Results.Ok(result.Value);
        }

        // Check if there's an invited user with this email waiting to be linked
        var invited = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Auth0Sub.StartsWith("pending|"));
        if (invited is not null)
        {
            invited.Auth0Sub = auth0Sub;
            invited.DisplayName = name;
            invited.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var retryResult = await mediator.Send(new GetMe.Query());
            if (retryResult.IsSuccess)
                return Results.Ok(retryResult.Value);
        }

        // Check if there's already a user with this email (linked to another identity)
        var existingByEmail = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (existingByEmail is not null)
        {
            existingByEmail.Auth0Sub = auth0Sub;
            existingByEmail.DisplayName = name;
            existingByEmail.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var retryResult = await mediator.Send(new GetMe.Query());
            if (retryResult.IsSuccess)
                return Results.Ok(retryResult.Value);
        }
    }

    var meResult = await mediator.Send(new GetMe.Query());
    if (meResult.IsSuccess)
        return Results.Ok(meResult.Value);

    return Results.Json(new { error = "No account found. Contact your administrator to get access." }, statusCode: 403);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "/api/me failed for user");
        return Results.Json(new { error = $"Login failed: {ex.GetType().Name}: {ex.Message}" }, statusCode: 500);
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
