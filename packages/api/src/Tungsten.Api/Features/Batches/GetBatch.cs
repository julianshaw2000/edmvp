using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Batches;

public static class GetBatch
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string OriginMine,
        decimal WeightKg,
        string Status,
        string ComplianceStatus,
        DateTime CreatedAt,
        int EventCount);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.EntraOid == currentUser.EntraOid && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches.AsNoTracking()
                .Where(b => b.Id == query.Id && b.TenantId == user.TenantId)
                .Select(b => new Response(
                    b.Id, b.BatchNumber, b.MineralType,
                    b.OriginCountry, b.OriginMine, b.WeightKg,
                    b.Status, b.ComplianceStatus, b.CreatedAt,
                    b.CustodyEvents.Count))
                .FirstOrDefaultAsync(ct);

            return batch is null
                ? Result<Response>.Failure("Batch not found")
                : Result<Response>.Success(batch);
        }
    }
}
