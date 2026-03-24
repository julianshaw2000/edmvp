using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Analytics;

public static class GetAnalytics
{
    public record Query : IRequest<Result<Response>>;

    public record Response(
        int TotalBatches,
        int CompletedBatches,
        int FlaggedBatches,
        int PendingBatches,
        int TotalEvents,
        int TotalUsers,
        ComplianceBreakdown Compliance,
        List<MineralBreakdown> MineralDistribution,
        List<CountryBreakdown> OriginCountries,
        List<MonthlyActivity> MonthlyBatches);

    public record ComplianceBreakdown(int Compliant, int Flagged, int Pending);
    public record MineralBreakdown(string Mineral, int Count);
    public record CountryBreakdown(string Country, int Count);
    public record MonthlyActivity(string Month, int Count);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var batches = db.Batches.AsNoTracking().Where(b => b.TenantId == tenantId);

            var totalBatches = await batches.CountAsync(ct);
            var completedBatches = await batches.CountAsync(b => b.Status == "COMPLETED", ct);
            var flaggedBatches = await batches.CountAsync(b => b.ComplianceStatus == "FLAGGED", ct);
            var pendingBatches = await batches.CountAsync(b => b.ComplianceStatus == "PENDING", ct);
            var compliantBatches = await batches.CountAsync(b => b.ComplianceStatus == "COMPLIANT", ct);

            var totalEvents = await db.CustodyEvents.CountAsync(e => e.TenantId == tenantId, ct);
            var totalUsers = await db.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive, ct);

            var mineralDist = await batches
                .GroupBy(b => b.MineralType)
                .Select(g => new MineralBreakdown(g.Key, g.Count()))
                .ToListAsync(ct);

            var countryDist = await batches
                .GroupBy(b => b.OriginCountry)
                .Select(g => new CountryBreakdown(g.Key, g.Count()))
                .OrderByDescending(c => c.Count)
                .Take(10)
                .ToListAsync(ct);

            // Monthly activity for last 6 months
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
            var monthlyRaw = await batches
                .Where(b => b.CreatedAt >= sixMonthsAgo)
                .ToListAsync(ct);

            var monthlyBatches = monthlyRaw
                .GroupBy(b => b.CreatedAt.ToString("yyyy-MM"))
                .Select(g => new MonthlyActivity(g.Key, g.Count()))
                .OrderBy(m => m.Month)
                .ToList();

            return Result<Response>.Success(new Response(
                totalBatches, completedBatches, flaggedBatches, pendingBatches,
                totalEvents, totalUsers,
                new ComplianceBreakdown(compliantBatches, flaggedBatches, pendingBatches),
                mineralDist, countryDist, monthlyBatches));
        }
    }
}
