using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.AI;

public static class TenantHealthCheck
{
    public record Query : IRequest<Result<Response>>;

    public record TenantHealth(
        Guid TenantId,
        string TenantName,
        string Status,
        int HealthScore,
        string Color,
        List<string> Insights);

    public record Response(List<TenantHealth> Tenants, double AverageHealth);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var cutoff30 = now.AddDays(-30);

            var tenants = await db.Tenants.AsNoTracking()
                .Where(t => t.Status != "CANCELLED")
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.Status,
                    ActiveUserCount = t.Users.Count(u => u.IsActive),
                    TotalBatches = t.Batches.Count,
                    RecentBatches = t.Batches.Count(b => b.CreatedAt >= cutoff30),
                    CompliantBatches = t.Batches.Count(b => b.ComplianceStatus == "COMPLIANT"),
                    FlaggedBatches = t.Batches.Count(b => b.ComplianceStatus == "FLAGGED"),
                    HasPassport = db.GeneratedDocuments.Any(d => d.TenantId == t.Id && d.DocumentType == "MaterialPassport"),
                    DocumentCount = db.Documents.Count(d => d.TenantId == t.Id),
                    LastEvent = db.CustodyEvents.Where(e => e.TenantId == t.Id).Select(e => (DateTime?)e.CreatedAt).Max(),
                })
                .ToListAsync(ct);

            var results = tenants.Select(t =>
            {
                var score = 0;
                var insights = new List<string>();

                // Active users (max 20 pts)
                if (t.ActiveUserCount >= 3) { score += 20; insights.Add($"{t.ActiveUserCount} active users"); }
                else if (t.ActiveUserCount == 2) { score += 12; insights.Add("2 active users"); }
                else if (t.ActiveUserCount == 1) { score += 6; insights.Add("Only 1 active user"); }
                else { insights.Add("No active users"); }

                // Batch creation frequency (max 25 pts)
                if (t.RecentBatches >= 5) { score += 25; insights.Add($"{t.RecentBatches} batches created this month"); }
                else if (t.RecentBatches >= 2) { score += 15; insights.Add($"{t.RecentBatches} batches this month"); }
                else if (t.RecentBatches == 1) { score += 8; insights.Add("Only 1 batch this month"); }
                else if (t.TotalBatches > 0) { score += 3; insights.Add("No batches created in 30 days"); }
                else { insights.Add("No batches ever created"); }

                // Feature adoption (max 25 pts)
                if (t.HasPassport) { score += 15; insights.Add("Material Passports generated"); }
                else if (t.TotalBatches > 0) { insights.Add("No Material Passports generated yet"); }

                if (t.DocumentCount > 0) { score += 10; insights.Add($"{t.DocumentCount} supporting documents uploaded"); }
                else { insights.Add("No documents uploaded"); }

                // Compliance health (max 20 pts)
                if (t.TotalBatches > 0)
                {
                    var compliantRate = (double)t.CompliantBatches / t.TotalBatches;
                    if (compliantRate >= 0.9) { score += 20; insights.Add($"{t.CompliantBatches}/{t.TotalBatches} batches compliant"); }
                    else if (compliantRate >= 0.6) { score += 12; insights.Add($"{t.CompliantBatches}/{t.TotalBatches} batches compliant"); }
                    else if (compliantRate > 0) { score += 5; insights.Add($"Only {t.CompliantBatches}/{t.TotalBatches} batches compliant"); }
                    if (t.FlaggedBatches > 0) { score -= 5; insights.Add($"{t.FlaggedBatches} flagged batch(es)"); }
                }

                // Recency (max 10 pts)
                if (t.LastEvent.HasValue)
                {
                    var days = (now - t.LastEvent.Value).TotalDays;
                    if (days <= 7) { score += 10; }
                    else if (days <= 14) { score += 6; }
                    else if (days <= 30) { score += 3; }
                    else { insights.Add($"No activity in {(int)days} days"); }
                }
                else
                {
                    insights.Add("No custody events recorded");
                }

                score = Math.Max(0, Math.Min(100, score));

                var color = score > 70 ? "green" : score >= 40 ? "amber" : "red";

                return new TenantHealth(t.Id, t.Name, t.Status, score, color, insights);
            })
            .OrderBy(t => t.HealthScore)
            .ToList();

            var avg = results.Count > 0 ? results.Average(t => t.HealthScore) : 0;
            return Result<Response>.Success(new Response(results, Math.Round(avg, 1)));
        }
    }
}
