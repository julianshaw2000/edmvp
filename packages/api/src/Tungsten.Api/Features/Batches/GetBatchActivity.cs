using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Batches;

public static class GetBatchActivity
{
    public record Query(Guid BatchId) : IRequest<Result<List<ActivityDto>>>;

    public record ActivityDto(
        Guid Id,
        string UserDisplayName,
        string Action,
        string EntityType,
        string Result,
        string? FailureReason,
        DateTime Timestamp);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<List<ActivityDto>>>
    {
        public async Task<Result<List<ActivityDto>>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var items = await db.AuditLogs.AsNoTracking()
                .Where(a => a.TenantId == tenantId && a.BatchId == query.BatchId)
                .OrderBy(a => a.Timestamp)
                .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new ActivityDto(
                    a.Id, u.DisplayName, a.Action, a.EntityType,
                    a.Result, a.FailureReason, a.Timestamp))
                .ToListAsync(ct);

            return Result<List<ActivityDto>>.Success(items);
        }
    }
}
