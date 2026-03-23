using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Users;

public static class UpdateUser
{
    public record Command(Guid Id, string? Role, bool? IsActive) : IRequest<Result>, IAuditable
    {
        public string AuditAction => "UpdateUser";
        public string EntityType => "User";
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command cmd, CancellationToken ct)
        {
            var admin = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (admin is null)
                return Result.Failure("User not found");

            var target = await db.Users
                .FirstOrDefaultAsync(u => u.Id == cmd.Id && u.TenantId == admin.TenantId, ct);
            if (target is null)
                return Result.Failure("Target user not found");

            var callerRole = await currentUser.GetRoleAsync(ct);
            if (callerRole == Roles.TenantAdmin)
            {
                if (target.Role is Roles.Admin or Roles.TenantAdmin)
                    return Result.Failure("You cannot modify this user");
                if (cmd.Role is Roles.Admin or Roles.TenantAdmin)
                    return Result.Failure("You cannot assign this role");
            }

            if (cmd.Role is not null) target.Role = cmd.Role;
            if (cmd.IsActive.HasValue) target.IsActive = cmd.IsActive.Value;
            target.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
