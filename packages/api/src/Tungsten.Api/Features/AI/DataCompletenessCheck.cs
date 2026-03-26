using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.AI;

public static class DataCompletenessCheck
{
    public record Query : IRequest<Result<Response>>;

    public record BatchCompleteness(Guid Id, string BatchNumber, string MineralType, int Score, List<string> MissingFields);

    public record Response(List<BatchCompleteness> Batches, double AverageScore);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var batches = await db.Batches.AsNoTracking()
                .Where(b => b.TenantId == tenantId)
                .Select(b => new
                {
                    b.Id, b.BatchNumber, b.MineralType, b.OriginCountry, b.OriginMine, b.WeightKg,
                    b.Status, b.ComplianceStatus,
                    EventCount = db.CustodyEvents.Count(e => e.BatchId == b.Id),
                    HasSmelterEvent = db.CustodyEvents.Any(e => e.BatchId == b.Id && e.SmelterId != null),
                    DocumentCount = db.Documents.Count(d => d.BatchId == b.Id),
                })
                .ToListAsync(ct);

            var results = batches.Select(b =>
            {
                var missing = new List<string>();
                var score = 100;

                if (b.EventCount == 0) { missing.Add("No custody events logged"); score -= 40; }
                else if (b.EventCount < 3) { missing.Add($"Only {b.EventCount} events (recommend 3+)"); score -= 15; }

                if (!b.HasSmelterEvent) { missing.Add("No smelter/processing event with RMAP ID"); score -= 20; }
                if (b.DocumentCount == 0) { missing.Add("No supporting documents uploaded"); score -= 10; }
                if (b.ComplianceStatus == "PENDING") { missing.Add("Compliance checks not yet run"); score -= 10; }
                if (string.IsNullOrEmpty(b.OriginMine) || b.OriginMine == "Unknown") { missing.Add("Origin mine not specified"); score -= 5; }

                return new BatchCompleteness(b.Id, b.BatchNumber, b.MineralType, Math.Max(0, score), missing);
            })
            .OrderBy(b => b.Score)
            .ToList();

            var avgScore = results.Count > 0 ? results.Average(b => b.Score) : 100;

            return Result<Response>.Success(new Response(results, Math.Round(avgScore, 1)));
        }
    }
}
