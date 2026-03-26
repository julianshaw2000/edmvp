using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public interface ICurrentUserService
{
    string EntraOid { get; }
    Task<Guid> GetUserIdAsync(CancellationToken ct);
    Task<Guid> GetTenantIdAsync(CancellationToken ct);
    Task<string> GetTenantStatusAsync(CancellationToken ct);
    Task<string> GetRoleAsync(CancellationToken ct);
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db) : ICurrentUserService
{
    private Guid? _userId;
    private Guid? _tenantId;
    private string? _role;
    private string? _tenantStatus;

    public string EntraOid =>
        httpContextAccessor.HttpContext?.User
            .FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
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

    public async Task<string> GetTenantStatusAsync(CancellationToken ct)
    {
        if (_tenantStatus is not null) return _tenantStatus;
        await ResolveUserAsync(ct);
        return _tenantStatus!;
    }

    public async Task<string> GetRoleAsync(CancellationToken ct)
    {
        if (_role is not null) return _role;
        await ResolveUserAsync(ct);
        return _role!;
    }

    private async Task ResolveUserAsync(CancellationToken ct)
    {
        var oid = EntraOid;
        var user = await db.Users.AsNoTracking()
            .Where(u => u.EntraOid == oid && u.IsActive)
            .Join(db.Tenants, u => u.TenantId, t => t.Id,
                (u, t) => new { u.Id, u.TenantId, u.Role, TenantStatus = t.Status })
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("User not found");

        _userId = user.Id;
        _tenantId = user.TenantId;
        _role = user.Role;
        _tenantStatus = user.TenantStatus;
    }
}
