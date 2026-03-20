using System.Text.Json;
using System.Threading.RateLimiting;
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
using Tungsten.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
builder.Services.AddSingleton<IEmailService, LogEmailService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<GetMe.Handler>());
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<GetMe.Handler>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:4200"];
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

// Sentry
if (!string.IsNullOrEmpty(builder.Configuration["Sentry:Dsn"]))
{
    builder.WebHost.UseSentry(o =>
    {
        o.Dsn = builder.Configuration["Sentry:Dsn"];
        o.TracesSampleRate = 0.2;
    });
}

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("tungsten-api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddConsoleExporter());

// JSON
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply migrations and seed data on startup
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.SeedAsync(db);
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseMiddleware<AuditLoggingMiddleware>();

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Auth endpoints
app.MapGet("/api/me", async (HttpContext httpContext, IMediator mediator, AppDbContext db, ICurrentUserService currentUser) =>
{
    var result = await mediator.Send(new GetMe.Query());
    if (result.IsSuccess)
        return Results.Ok(result.Value);

    // Auto-provision: link Auth0 identity to platform user
    {
        var auth0Sub = currentUser.Auth0Sub;
        var email = httpContext.User.FindFirst("email")?.Value
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? "user@accutrac.org";
        var name = httpContext.User.FindFirst("name")?.Value
            ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
            ?? "Pilot User";

        // Check if this user was invited (exists by email with pending Auth0Sub)
        var invited = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.Auth0Sub.StartsWith("pending|"));
        if (invited is not null)
        {
            // Link the invited user to their Auth0 identity
            invited.Auth0Sub = auth0Sub;
            invited.DisplayName = name;
            invited.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var retry = await mediator.Send(new GetMe.Query());
            if (retry.IsSuccess)
                return Results.Ok(retry.Value);
        }

        // Otherwise create a new user as PLATFORM_ADMIN
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Status == "ACTIVE");
        if (tenant is not null)
        {
            var newUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                Auth0Sub = auth0Sub,
                Email = email,
                DisplayName = name,
                Role = "PLATFORM_ADMIN",
                TenantId = tenant.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            var retry = await mediator.Send(new GetMe.Query());
            if (retry.IsSuccess)
                return Results.Ok(retry.Value);
        }
    }

    return Results.NotFound(new { error = result.Error });
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

app.Run();

// Make Program accessible for WebApplicationFactory
public partial class Program { }
