using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Public;

public static class VerifyBatch
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record Response(
        Guid BatchId,
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string ComplianceStatus,
        int EventCount,
        bool HashChainIntact);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);

            // Verify hash chain
            var hashChainIntact = true;
            string? previousHash = null;
            foreach (var evt in events)
            {
                if (evt.PreviousEventHash != previousHash)
                {
                    hashChainIntact = false;
                    break;
                }
                previousHash = evt.Sha256Hash;
            }

            return Result<Response>.Success(new Response(
                batch.Id, batch.BatchNumber, batch.MineralType,
                batch.OriginCountry, batch.ComplianceStatus,
                events.Count, hashChainIntact));
        }
    }
}
