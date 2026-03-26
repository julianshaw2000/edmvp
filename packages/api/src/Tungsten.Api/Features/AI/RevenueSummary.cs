using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services.AI;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.AI;

public static class RevenueSummary
{
    public record Query : IRequest<Result<Response>>;
    public record Response(string Summary, decimal Mrr, int ActiveCount, int TrialCount, int SuspendedCount, int CancelledCount);

    private static class PlanConfiguration
    {
        public static decimal GetPrice(string? planName) => planName?.ToUpperInvariant() switch
        {
            "PRO" => 249m,
            "STARTER" => 99m,
            _ => 99m,
        };
    }

    public class Handler(AppDbContext db, IAiService ai) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var weekAgo = now.AddDays(-7);
            var weekAhead = now.AddDays(7);

            var tenants = await db.Tenants.AsNoTracking()
                .Select(t => new { t.Id, t.Status, t.PlanName, t.TrialEndsAt, t.CreatedAt })
                .ToListAsync(ct);

            var active = tenants.Where(t => t.Status == "ACTIVE").ToList();
            var trial = tenants.Where(t => t.Status == "TRIAL").ToList();
            var suspended = tenants.Where(t => t.Status == "SUSPENDED").ToList();
            var cancelled = tenants.Where(t => t.Status == "CANCELLED").ToList();

            var mrr = active.Sum(t => PlanConfiguration.GetPrice(t.PlanName));
            var trialsExpiringThisWeek = trial.Count(t => t.TrialEndsAt.HasValue && t.TrialEndsAt.Value <= weekAhead);
            var recentSignups = tenants.Count(t => t.CreatedAt >= weekAgo);

            var dataContext = $"""
                Platform Revenue & Business Summary — {now:yyyy-MM-dd}

                Tenant Status Breakdown:
                - ACTIVE: {active.Count} tenants
                - TRIAL: {trial.Count} tenants
                - SUSPENDED: {suspended.Count} tenants
                - CANCELLED: {cancelled.Count} tenants
                - Total: {tenants.Count} tenants

                Revenue:
                - Estimated MRR: ${mrr:F0} USD
                - Plan breakdown (active): {string.Join(", ", active.GroupBy(t => t.PlanName ?? "STARTER").Select(g => $"{g.Key}: {g.Count()}"))}

                Trial Activity:
                - Trials expiring this week: {trialsExpiringThisWeek}
                - New signups in last 7 days: {recentSignups}

                Suspended/Cancelled Risk:
                - Suspended (possible failed payment): {suspended.Count}
                - Churned this period: {cancelled.Count}
                """;

            var systemPrompt = """
                You are a business intelligence assistant for auditraks, a B2B SaaS compliance platform.
                Generate a brief executive business summary (3-5 bullet points) based on the data provided.
                Focus on: revenue health, growth signals, churn risk, and one actionable recommendation.
                Be concise, professional, and data-driven. Use markdown bullet points.
                """;

            var summary = await ai.GenerateAsync(systemPrompt, dataContext, ct);

            return Result<Response>.Success(new Response(summary, mrr, active.Count, trial.Count, suspended.Count, cancelled.Count));
        }
    }
}
