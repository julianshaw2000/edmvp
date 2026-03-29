using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class GenerateSupplyChainDescription
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;
    public record ChainLink(string EventType, DateTime EventDate, string Location, string ActorName, string? SmelterId);
    public record ChainGap(string Description, string Severity);
    public record Response(string BatchNumber, string MineralType, string OriginCountry, string OriginMine,
        IReadOnlyList<ChainLink> Chain, IReadOnlyList<ChainGap> Gaps, string NarrativeText, string SourceJson);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        private static readonly string[] ExpectedSequence =
            ["MINE_EXTRACTION", "LABORATORY_ASSAY", "CONCENTRATION", "TRADING_TRANSFER", "PRIMARY_PROCESSING", "EXPORT_SHIPMENT"];

        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.TenantId == tenantId, ct);
            if (batch is null) return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId).OrderBy(e => e.EventDate)
                .Select(e => new ChainLink(e.EventType, e.EventDate, e.Location, e.ActorName, e.SmelterId))
                .ToListAsync(ct);

            var presentTypes = events.Select(e => e.EventType).Distinct().ToHashSet();
            var gaps = new List<ChainGap>();
            foreach (var expected in ExpectedSequence)
            {
                if (!presentTypes.Contains(expected))
                {
                    var severity = expected is "MINE_EXTRACTION" or "PRIMARY_PROCESSING" ? "HIGH" : "MEDIUM";
                    gaps.Add(new ChainGap($"Missing {expected} event", severity));
                }
            }
            if (!events.Any(e => e.SmelterId is not null))
                gaps.Add(new ChainGap("No smelter identification in supply chain", "HIGH"));

            var lines = new List<string>
            {
                $"Batch {batch.BatchNumber}: {batch.MineralType} from {batch.OriginMine}, {batch.OriginCountry}.",
                $"{events.Count} custody events documented.",
            };
            foreach (var e in events)
            {
                var smelter = e.SmelterId is not null ? $" (Smelter: {e.SmelterId})" : "";
                lines.Add($"  - {e.EventType}: {e.Location}, {e.EventDate:yyyy-MM-dd} — {e.ActorName}{smelter}");
            }
            if (gaps.Count > 0)
            {
                lines.Add($"\n{gaps.Count} gap(s):");
                foreach (var g in gaps) lines.Add($"  - [{g.Severity}] {g.Description}");
            }
            else lines.Add("\nCustody chain complete.");

            var narrative = string.Join("\n", lines);
            var sourceJson = JsonSerializer.Serialize(new { batch = new { batch.BatchNumber, batch.MineralType, batch.OriginCountry, batch.OriginMine }, events, gaps });
            return Result<Response>.Success(new Response(batch.BatchNumber, batch.MineralType, batch.OriginCountry, batch.OriginMine, events, gaps, narrative, sourceJson));
        }
    }
}
