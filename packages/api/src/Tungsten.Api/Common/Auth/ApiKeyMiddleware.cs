using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public class ApiKeyMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        // Only check API key if not already authenticated via JWT
        if (context.User.Identity?.IsAuthenticated != true &&
            context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            var apiKey = apiKeyHeader.ToString();
            var keyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));

            var key = await db.ApiKeys.AsNoTracking()
                .Include(k => k.CreatedBy)
                .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

            if (key is not null)
            {
                // Update last used (fire-and-forget — non-critical)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await db.ApiKeys
                            .Where(k => k.Id == key.Id)
                            .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, DateTime.UtcNow));
                    }
                    catch
                    {
                        // Non-critical — swallow
                    }
                });

                // Set identity claims so ICurrentUserService works
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, key.CreatedBy.Auth0Sub),
                    new Claim("api_key_id", key.Id.ToString()),
                };
                context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
            }
        }

        await next(context);
    }
}
