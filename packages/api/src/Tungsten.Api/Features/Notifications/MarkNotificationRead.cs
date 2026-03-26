using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Notifications;

public static class MarkNotificationRead
{
    public record Command(Guid Id) : IRequest<Result>;

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.EntraOid == currentUser.EntraOid && u.IsActive, ct);
            if (user is null)
                return Result.Failure("User not found");

            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == cmd.Id && n.UserId == user.Id, ct);
            if (notification is null)
                return Result.Failure("Notification not found");

            notification.IsRead = true;
            await db.SaveChangesAsync(ct);
            return Result.Success();
        }
    }
}
