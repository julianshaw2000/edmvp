using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class GenerateDueDiligenceSummary
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;
    public record RiskFlag(string Framework, string Status, string Detail, DateTime CheckedAt);
    public record SmelterStatus(string SmelterId, string SmelterName, string ConformanceStatus, DateOnly? LastAuditDate);
    public record Response(IReadOnlyList<RiskFlag> RiskFlags, IReadOnlyList<SmelterStatus> Smelters, string OecdDdgVersion, string SummaryText);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.TenantId == tenantId, ct);
            if (batch is null) return Result<Response>.Failure("Batch not found");

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == query.BatchId).OrderByDescending(c => c.CheckedAt).ToListAsync(ct);

            var riskFlags = checks.Where(c => c.Status is "FLAG" or "FAIL" or "INSUFFICIENT_DATA")
                .Select(c =>
                {
                    var detail = "";
                    if (c.Details.HasValue)
                        try { detail = c.Details.Value.GetProperty("detail").GetString() ?? ""; } catch { detail = c.Details.Value.ToString(); }
                    return new RiskFlag(c.Framework, c.Status, detail, c.CheckedAt);
                }).ToList();

            var smelterIds = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId && e.SmelterId != null)
                .Select(e => e.SmelterId!).Distinct().ToListAsync(ct);
            var smelters = await db.RmapSmelters.AsNoTracking()
                .Where(s => smelterIds.Contains(s.SmelterId))
                .Select(s => new SmelterStatus(s.SmelterId, s.SmelterName, s.ConformanceStatus, s.LastAuditDate))
                .ToListAsync(ct);

            var oecdVersion = checks.Where(c => c.Framework == "OECD_DDG").Select(c => c.RuleVersion).FirstOrDefault() ?? "1.0.0-pilot";
            var summary = $"Batch {batch.BatchNumber}: {checks.Count} checks, {riskFlags.Count} flags, {smelters.Count} smelter(s), OECD DDG {oecdVersion}.";
            return Result<Response>.Success(new Response(riskFlags, smelters, oecdVersion, summary));
        }
    }
}
