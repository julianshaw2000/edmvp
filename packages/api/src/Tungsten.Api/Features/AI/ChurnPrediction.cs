using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.AI;

public static class ChurnPrediction
{
    public record Query : IRequest<Result<Response>>;

    public record TenantChurnRisk(
        Guid TenantId,
        string TenantName,
        string Status,
        string RiskLevel,
        List<string> Reasons,
        DateTime? LastActivity);

    public record Response(List<TenantChurnRisk> Tenants);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var cutoff30 = DateTime.UtcNow.AddDays(-30);

            var tenants = await db.Tenants.AsNoTracking()
                .Where(t => t.Status != "CANCELLED")
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Status,
                    UserCount = t.Users.Count,
                })
                .ToListAsync(ct);

            var tenantIds = tenants.Select(t => t.Id).ToList();

            // Last audit log timestamp per tenant (approximates last login)
            var lastActivityRows = await db.AuditLogs.AsNoTracking()
                .Where(a => tenantIds.Contains(a.TenantId))
                .GroupBy(a => a.TenantId)
                .Select(g => new { TenantId = g.Key, LastActivity = g.Max(a => a.Timestamp) })
                .ToListAsync(ct);

            // Batch count last 30 days per tenant
            var recentBatchRows = await db.Batches.AsNoTracking()
                .Where(b => tenantIds.Contains(b.TenantId) && b.CreatedAt >= cutoff30)
                .GroupBy(b => b.TenantId)
                .Select(g => new { TenantId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            // Most recent batch creation date per tenant
            var lastBatchRows = await db.Batches.AsNoTracking()
                .Where(b => tenantIds.Contains(b.TenantId))
                .GroupBy(b => b.TenantId)
                .Select(g => new { TenantId = g.Key, LastCreated = g.Max(b => b.CreatedAt) })
                .ToListAsync(ct);

            // Convert to dictionaries — keys may be absent for tenants with no data
            var activityMap = lastActivityRows.ToDictionary(x => x.TenantId, x => x.LastActivity);
            var batchCountMap = recentBatchRows.ToDictionary(x => x.TenantId, x => x.Count);
            var lastBatchMap = lastBatchRows.ToDictionary(x => x.TenantId, x => x.LastCreated);

            var now = DateTime.UtcNow;

            var results = tenants.Select(t =>
            {
                var hasActivity = activityMap.TryGetValue(t.Id, out var lastActivity);
                batchCountMap.TryGetValue(t.Id, out var recentBatches);
                var hasBatch = lastBatchMap.TryGetValue(t.Id, out var lastBatch);

                var reasons = new List<string>();

                var daysSinceActivity = hasActivity ? (now - lastActivity).TotalDays : double.MaxValue;
                var daysSinceLastBatch = hasBatch ? (now - lastBatch).TotalDays : double.MaxValue;

                if (!hasActivity)
                    reasons.Add("No recorded activity");
                else if (daysSinceActivity >= 14)
                    reasons.Add($"No activity for {(int)daysSinceActivity} days");

                if (!hasBatch)
                    reasons.Add("No batches ever created");
                else if (daysSinceLastBatch >= 14)
                    reasons.Add($"No new batch in {(int)daysSinceLastBatch} days");

                if (recentBatches == 0)
                    reasons.Add("Zero batch activity in last 30 days");
                else if (recentBatches < 3)
                    reasons.Add($"Only {recentBatches} batch(es) created in last 30 days");

                if (t.UserCount <= 1)
                    reasons.Add("Only 1 user — low team adoption");

                var riskLevel = "LOW";
                if (daysSinceActivity >= 14 || !hasBatch || recentBatches == 0)
                    riskLevel = "HIGH";
                else if (recentBatches < 3 || daysSinceLastBatch >= 7)
                    riskLevel = "MEDIUM";

                if (reasons.Count == 0)
                    reasons.Add("Active and engaged");

                DateTime? lastActivityNullable = hasActivity ? lastActivity : null;

                return new TenantChurnRisk(t.Id, t.Name, t.Status, riskLevel, reasons, lastActivityNullable);
            })
            .OrderByDescending(t => t.RiskLevel switch { "HIGH" => 2, "MEDIUM" => 1, _ => 0 })
            .ThenBy(t => t.LastActivity)
            .ToList();

            return Result<Response>.Success(new Response(results));
        }
    }
}
