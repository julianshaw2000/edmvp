using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Users;

public static class ListUsers
{
    public record Query(Guid? TenantId = null) : IRequest<Result<Response>>;

    public record UserItem(Guid Id, string Email, string DisplayName, string Role, bool IsActive, string? TenantName);

    public record Response(IReadOnlyList<UserItem> Users, int TotalCount);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var callerRole = await currentUser.GetRoleAsync(ct);
            var dbQuery = db.Users.AsNoTracking().AsQueryable();

            if (callerRole == Roles.Admin)
            {
                // PLATFORM_ADMIN: all tenants, optionally filter by one
                if (query.TenantId.HasValue)
                    dbQuery = dbQuery.Where(u => u.TenantId == query.TenantId.Value);
            }
            else
            {
                // TENANT_ADMIN: scoped to own tenant, hide PLATFORM_ADMIN users
                var tenantId = await currentUser.GetTenantIdAsync(ct);
                dbQuery = dbQuery.Where(u => u.TenantId == tenantId);
                dbQuery = dbQuery.Where(u => u.Role != Roles.Admin);
            }

            var users = await dbQuery
                .OrderBy(u => u.DisplayName)
                .Join(db.Tenants, u => u.TenantId, t => t.Id, (u, t) => new UserItem(u.Id, u.Email, u.DisplayName, u.Role, u.IsActive, t.Name))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(users, users.Count));
        }
    }
}
