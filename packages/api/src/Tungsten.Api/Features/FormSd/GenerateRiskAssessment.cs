using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class GenerateRiskAssessment
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;
    public record RiskCategory(string Category, string Rating, string Detail);
    public record Response(string OverallRating, IReadOnlyList<RiskCategory> Categories, string SummaryText);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.TenantId == tenantId, ct);
            if (batch is null) return Result<Response>.Failure("Batch not found");

            var categories = new List<RiskCategory>();

            // 1. Source country
            var highRisk = await db.RiskCountries.AsNoTracking()
                .Where(r => r.RiskLevel == "HIGH").Select(r => r.CountryCode).ToListAsync(ct);
            categories.Add(highRisk.Contains(batch.OriginCountry)
                ? new RiskCategory("Source Country", "HIGH", $"{batch.OriginCountry} is high-risk (CAHRA)")
                : new RiskCategory("Source Country", "LOW", $"{batch.OriginCountry} is not high-risk"));

            // 2. Smelter conformance
            var smelterIds = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId && e.SmelterId != null)
                .Select(e => e.SmelterId!).Distinct().ToListAsync(ct);
            if (smelterIds.Count == 0)
                categories.Add(new RiskCategory("Smelter Conformance", "HIGH", "No smelter identified"));
            else
            {
                var smelters = await db.RmapSmelters.AsNoTracking().Where(s => smelterIds.Contains(s.SmelterId)).ToListAsync(ct);
                categories.Add(smelters.All(s => s.ConformanceStatus is "CONFORMANT" or "ACTIVE_PARTICIPATING")
                    ? new RiskCategory("Smelter Conformance", "LOW", "All smelters RMAP conformant")
                    : new RiskCategory("Smelter Conformance", "HIGH", "Non-conformant smelter(s)"));
            }

            // 3. Chain completeness
            var eventCount = await db.CustodyEvents.AsNoTracking().CountAsync(e => e.BatchId == query.BatchId, ct);
            categories.Add(eventCount >= 4
                ? new RiskCategory("Chain Completeness", "LOW", $"{eventCount} events — substantially complete")
                : new RiskCategory("Chain Completeness", eventCount >= 2 ? "MEDIUM" : "HIGH", $"{eventCount} event(s) — incomplete"));

            // 4. Compliance flags
            var flagCount = await db.ComplianceChecks.AsNoTracking()
                .CountAsync(c => c.BatchId == query.BatchId && (c.Status == "FLAG" || c.Status == "FAIL"), ct);
            categories.Add(flagCount == 0
                ? new RiskCategory("Compliance Flags", "LOW", "No flags")
                : new RiskCategory("Compliance Flags", flagCount > 2 ? "HIGH" : "MEDIUM", $"{flagCount} flag(s)"));

            var overall = categories.Any(c => c.Rating == "HIGH") ? "HIGH"
                : categories.Any(c => c.Rating == "MEDIUM") ? "MEDIUM" : "LOW";
            var summary = $"Batch {batch.BatchNumber}: Overall {overall}. " +
                string.Join("; ", categories.Select(c => $"{c.Category}: {c.Rating}"));

            return Result<Response>.Success(new Response(overall, categories, summary));
        }
    }
}
