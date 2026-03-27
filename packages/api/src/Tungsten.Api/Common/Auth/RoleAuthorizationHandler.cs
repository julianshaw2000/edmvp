using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public class RoleRequirement(params string[] allowedRoles) : IAuthorizationRequirement
{
    public string[] AllowedRoles { get; } = allowedRoles;
}

public class TenantAccessRequirement : IAuthorizationRequirement;

public class RoleAuthorizationHandler(AppDbContext db, ICurrentUserService currentUser)
    : AuthorizationHandler<RoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, RoleRequirement requirement)
    {
        try
        {
            var sub = currentUser.IdentityUserId;
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == sub && u.IsActive);

            if (user is null) return;

            if (user.Role == Roles.Admin || requirement.AllowedRoles.Contains(user.Role))
                context.Succeed(requirement);
        }
        catch (UnauthorizedAccessException)
        {
            // No authenticated user — requirement not met
        }
    }
}

public class TenantAccessHandler(AppDbContext db, ICurrentUserService currentUser)
    : AuthorizationHandler<TenantAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, TenantAccessRequirement requirement)
    {
        try
        {
            var sub = currentUser.IdentityUserId;
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == sub && u.IsActive);

            if (user is null) return;
            context.Succeed(requirement);
        }
        catch (UnauthorizedAccessException)
        {
            // No authenticated user — requirement not met
        }
    }
}
