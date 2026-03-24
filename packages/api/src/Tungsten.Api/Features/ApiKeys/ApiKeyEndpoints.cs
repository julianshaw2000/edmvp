using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.ApiKeys;

public static class ApiKeyEndpointsMap
{
    public static IEndpointRouteBuilder MapApiKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/api-keys")
            .RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        // List (returns prefix + name, never the full key)
        group.MapGet("/", async (AppDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var keys = await db.ApiKeys.AsNoTracking()
                .Where(k => k.TenantId == tenantId)
                .OrderByDescending(k => k.CreatedAt)
                .Select(k => new
                {
                    k.Id,
                    k.Name,
                    k.KeyPrefix,
                    k.IsActive,
                    k.CreatedAt,
                    k.LastUsedAt,
                })
                .ToListAsync(ct);
            return TypedResults.Ok(keys);
        });

        // Create (returns the full key ONCE — never stored or shown again)
        group.MapPost("/", async (CreateApiKeyRequest request, AppDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.Problem("Name is required.", statusCode: 400);

            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var userId = await currentUser.GetUserIdAsync(ct);

            // Generate key: "at_" prefix + 32 random bytes hex
            var rawKey = "at_" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
            var keyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

            var entity = new ApiKeyEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                CreatedByUserId = userId,
                Name = request.Name,
                KeyHash = keyHash,
                KeyPrefix = rawKey[..11], // "at_" + first 8 hex chars
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            db.ApiKeys.Add(entity);
            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/api-keys/{entity.Id}",
                new { entity.Id, entity.Name, entity.KeyPrefix, Key = rawKey });
        });

        // Revoke
        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, ICurrentUserService currentUser, CancellationToken ct) =>
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var key = await db.ApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.TenantId == tenantId, ct);
            if (key is null) return Results.NotFound();
            key.IsActive = false;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        return app;
    }
}

public record CreateApiKeyRequest(string Name);
