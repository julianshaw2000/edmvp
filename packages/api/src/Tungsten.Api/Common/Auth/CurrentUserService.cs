using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public interface ICurrentUserService
{
    string Auth0Sub { get; }
    Task<Guid> GetUserIdAsync(CancellationToken ct);
    Task<Guid> GetTenantIdAsync(CancellationToken ct);
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db) : ICurrentUserService
{
    private Guid? _userId;
    private Guid? _tenantId;

    public string Auth0Sub =>
        httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("No authenticated user");

    public async Task<Guid> GetUserIdAsync(CancellationToken ct)
    {
        if (_userId.HasValue) return _userId.Value;
        await ResolveUserAsync(ct);
        return _userId!.Value;
    }

    public async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        if (_tenantId.HasValue) return _tenantId.Value;
        await ResolveUserAsync(ct);
        return _tenantId!.Value;
    }

    private async Task ResolveUserAsync(CancellationToken ct)
    {
        var sub = Auth0Sub;
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Auth0Sub == sub && u.IsActive)
            .Select(u => new { u.Id, u.TenantId })
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("User not found");

        _userId = user.Id;
        _tenantId = user.TenantId;
    }
}
