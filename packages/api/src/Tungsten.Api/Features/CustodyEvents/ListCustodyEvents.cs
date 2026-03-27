using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Pagination;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.CustodyEvents;

public static class ListCustodyEvents
{
    public record Query(Guid BatchId, int Page = 1, int PageSize = 20) : IRequest<Result<PagedResponse<EventItem>>>;

    public record EventItem(
        Guid Id,
        string EventType,
        DateTime EventDate,
        string Location,
        string ActorName,
        bool IsCorrection,
        string Sha256Hash,
        DateTime CreatedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<PagedResponse<EventItem>>>
    {
        public async Task<Result<PagedResponse<EventItem>>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == currentUser.IdentityUserId && u.IsActive, ct);
            if (user is null)
                return Result<PagedResponse<EventItem>>.Failure("User not found");

            var baseQuery = db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId && e.TenantId == user.TenantId);

            var totalCount = await baseQuery.CountAsync(ct);

            var paged = new PagedRequest(query.Page, query.PageSize);
            var items = await baseQuery
                .OrderBy(e => e.CreatedAt)
                .Skip(paged.Skip)
                .Take(paged.PageSize)
                .Select(e => new EventItem(
                    e.Id, e.EventType, e.EventDate, e.Location,
                    e.ActorName, e.IsCorrection, e.Sha256Hash, e.CreatedAt))
                .ToListAsync(ct);

            return Result<PagedResponse<EventItem>>.Success(
                new PagedResponse<EventItem>(items, totalCount, query.Page, query.PageSize));
        }
    }
}
