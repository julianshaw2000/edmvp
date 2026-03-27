using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Compliance;

public static class GetBatchCompliance
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record CheckItem(string Framework, string Status, DateTime CheckedAt);

    public record Response(
        Guid BatchId,
        string OverallStatus,
        IReadOnlyList<CheckItem> Checks);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.IdentityUserId == currentUser.IdentityUserId && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == query.BatchId)
                .OrderByDescending(c => c.CheckedAt)
                .Select(c => new CheckItem(c.Framework, c.Status, c.CheckedAt))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(
                batch.Id, batch.ComplianceStatus, checks));
        }
    }
}
