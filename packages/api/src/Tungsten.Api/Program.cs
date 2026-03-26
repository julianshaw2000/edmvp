using System.Text.Json;
using System.Threading.RateLimiting;
using Resend;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
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

// Entra External ID
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");

// CIAM tokens lack 'tid' and use {tenantId}.ciamlogin.com as the issuer subdomain.
// Microsoft.Identity.Web registers a PostConfigureOptions that overwrites IssuerValidator,
// so we use PostConfigure (which runs after all PostConfigureOptions) to set ours last.
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var tenantId = builder.Configuration["AzureAd:TenantId"]!;
    var clientId = builder.Configuration["AzureAd:ClientId"]!;
    var expectedIssuer = $"https://{tenantId}.ciamlogin.com/{tenantId}/v2.0";
    options.TokenValidationParameters.ValidIssuers = [expectedIssuer];
    options.TokenValidationParameters.ValidAudiences = [clientId, $"api://{clientId}"];
    options.TokenValidationParameters.IssuerValidator = (issuer, _, _) =>
    {
        if (string.Equals(issuer, expectedIssuer, StringComparison.OrdinalIgnoreCase))
            return issuer;
        throw new SecurityTokenInvalidIssuerException($"Invalid issuer '{issuer}'. Expected '{expectedIssuer}'.");
    };
    // Log auth failures so we can see the exact reason in Render logs
    options.Events ??= new JwtBearerEvents();
    var prev = options.Events.OnAuthenticationFailed;
    options.Events.OnAuthenticationFailed = async ctx =>
    {
        var log = ctx.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        log.LogError(ctx.Exception, "JWT auth failed: {Type}", ctx.Exception.GetType().Name);
        if (prev != null) await prev(ctx);
    };
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

// Sentry user context (Entra OID only — no PII)
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var oid = context.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? context.User.FindFirst("oid")?.Value;
        if (oid is not null)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new Sentry.SentryUser { Id = oid };
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
app.MapGet("/api/me", async (
    HttpContext httpContext,
    IMediator mediator,
    AppDbContext db,
    ICurrentUserService currentUser,
    ILogger<Program> logger) =>
{
    try
    {
        var oid = currentUser.EntraOid;

        // Extract email and name from token claims
        var email = httpContext.User.FindFirst("email")?.Value
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var name = httpContext.User.FindFirst("name")?.Value
            ?? httpContext.User.FindFirst("preferred_username")?.Value
            ?? "User";

        // Check if invited user with this email is waiting to be linked (pending| prefix)
        if (!string.IsNullOrEmpty(email))
        {
            var invited = await db.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.EntraOid.StartsWith("pending|"));
            if (invited is not null)
            {
                invited.EntraOid = oid;
                invited.DisplayName = name;
                invited.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }
        }

        var meResult = await mediator.Send(new GetMe.Query());
        if (meResult.IsSuccess)
            return Results.Ok(meResult.Value);

        // Not found — check if this is a Google social login (first time)
        var idp = httpContext.User.FindFirst("idp")?.Value
            ?? httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/identityprovider")?.Value;
        var isGoogleLogin = idp?.Contains("google", StringComparison.OrdinalIgnoreCase) == true;

        if (isGoogleLogin && !string.IsNullOrEmpty(email))
        {
            // First-time Google user — require admin activation before access
            var existingByEmail = await db.Users.AnyAsync(u => u.Email == email);
            if (!existingByEmail)
            {
                return Results.Json(new { status = "pending_activation" }, statusCode: 403);
            }
        }

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
