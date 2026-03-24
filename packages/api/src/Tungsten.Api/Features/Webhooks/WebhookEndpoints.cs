using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Webhooks;

public static class WebhookEndpointsMap
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks")
            .RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        // List
        group.MapGet("/", async (AppDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var endpoints = await db.WebhookEndpoints.AsNoTracking()
                .Where(w => w.TenantId == tenantId)
                .OrderByDescending(w => w.CreatedAt)
                .Select(w => new { w.Id, w.Url, w.Events, w.IsActive, w.CreatedAt })
                .ToListAsync(ct);
            return Results.Ok(endpoints);
        });

        // Create
        group.MapPost("/", async (CreateWebhookRequest request, AppDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var secret = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
            var entity = new WebhookEndpointEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Url = request.Url,
                Secret = secret,
                Events = request.Events,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };
            db.WebhookEndpoints.Add(entity);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/webhooks/{entity.Id}", new { entity.Id, entity.Url, entity.Events, entity.Secret, entity.IsActive });
        });

        // Delete
        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var endpoint = await db.WebhookEndpoints.FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId, ct);
            if (endpoint is null) return Results.NotFound();
            db.WebhookEndpoints.Remove(endpoint);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }
}

public record CreateWebhookRequest(string Url, string Events);
