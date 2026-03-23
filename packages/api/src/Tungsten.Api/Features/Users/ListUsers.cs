using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Users;

public static class ListUsers
{
    public record Query : IRequest<Result<Response>>;

    public record UserItem(Guid Id, string Email, string DisplayName, string Role, bool IsActive);

    public record Response(IReadOnlyList<UserItem> Users, int TotalCount);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var callerRole = await currentUser.GetRoleAsync(ct);
            var dbQuery = db.Users.AsNoTracking()
                .Where(u => u.TenantId == user.TenantId);

            if (callerRole == Roles.TenantAdmin)
                dbQuery = dbQuery.Where(u => u.Role != Roles.Admin);

            var users = await dbQuery
                .OrderBy(u => u.DisplayName)
                .Select(u => new UserItem(u.Id, u.Email, u.DisplayName, u.Role, u.IsActive))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(users, users.Count));
        }
    }
}
