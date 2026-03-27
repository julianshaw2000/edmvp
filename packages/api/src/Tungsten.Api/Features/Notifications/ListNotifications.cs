using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Notifications;

public static class ListNotifications
{
    public record Query : IRequest<Result<Response>>;

    public record NotificationItem(
        Guid Id, string Type, string Title, string Message,
        Guid? ReferenceId, bool IsRead, DateTime CreatedAt);

    public record Response(IReadOnlyList<NotificationItem> Items, int UnreadCount);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == currentUser.IdentityUserId && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var items = await db.Notifications.AsNoTracking()
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .Select(n => new NotificationItem(
                    n.Id, n.Type, n.Title, n.Message,
                    n.ReferenceId, n.IsRead, n.CreatedAt))
                .ToListAsync(ct);

            var unreadCount = items.Count(n => !n.IsRead);

            return Result<Response>.Success(new Response(items, unreadCount));
        }
    }
}
