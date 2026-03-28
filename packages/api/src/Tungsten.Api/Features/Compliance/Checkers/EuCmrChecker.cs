using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Features.Compliance.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Checkers;

/// <summary>
/// EU Conflict Minerals Regulation (EU 2017/821) checker.
/// Only runs for tenants with "EU_CMR" in their Regulations list.
/// Checks:
///   Rule 1: Due diligence system — are OECD DDG steps documented in the chain?
///   Rule 2: Volume threshold — cumulative tungsten imports exceed 100kg/year
/// </summary>
public class EuCmrChecker(AppDbContext db) : INotificationHandler<CustodyEventCreated>
{
    private const decimal TungstenThresholdKg = 100m;

    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        // Check if tenant has EU_CMR enabled
        var tenant = await db.Tenants.AsNoTracking()
            .Where(t => t.Id == notification.TenantId)
            .Select(t => new { t.Regulations })
            .FirstOrDefaultAsync(ct);

        if (tenant?.Regulations is null || !tenant.Regulations.Contains("EU_CMR"))
            return;

        var subChecks = new List<(string name, string status, string detail)>();

        // Rule 1: Due diligence system — check that the batch has events covering key OECD DDG steps
        var batch = await db.Batches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == notification.BatchId, ct);
        if (batch is null) return;

        var eventTypes = await db.CustodyEvents.AsNoTracking()
            .Where(e => e.BatchId == notification.BatchId)
            .Select(e => e.EventType)
            .Distinct()
            .ToListAsync(ct);

        // OECD DDG Steps 1-3 mapped to event types:
        // Step 1 (traceability): MINE_EXTRACTION
        // Step 2 (risk identification): any event triggers compliance checks
        // Step 3 (risk mitigation): TRADING_TRANSFER or PRIMARY_PROCESSING
        var hasStep1 = eventTypes.Contains("MINE_EXTRACTION");
        var hasStep3 = eventTypes.Contains("TRADING_TRANSFER") || eventTypes.Contains("PRIMARY_PROCESSING");

        if (hasStep1 && hasStep3)
        {
            subChecks.Add(("due_diligence", "PASS", "OECD DDG Steps 1-3 documented in custody chain"));
        }
        else
        {
            var missing = new List<string>();
            if (!hasStep1) missing.Add("Step 1 (traceability/origin)");
            if (!hasStep3) missing.Add("Step 3 (risk mitigation)");
            subChecks.Add(("due_diligence", "INSUFFICIENT_DATA",
                $"Incomplete due diligence documentation. Missing: {string.Join(", ", missing)}"));
        }

        // Rule 2: Volume threshold — cumulative tungsten imports this year
        var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var totalWeightKg = await db.Batches.AsNoTracking()
            .Where(b => b.TenantId == notification.TenantId
                && b.CreatedAt >= yearStart
                && b.MineralType.ToLower().Contains("tungsten"))
            .SumAsync(b => b.WeightKg, ct);

        if (totalWeightKg >= TungstenThresholdKg)
        {
            subChecks.Add(("volume_threshold", "FLAG",
                $"Annual tungsten import volume ({totalWeightKg:F0} kg) exceeds EU CMR threshold ({TungstenThresholdKg:F0} kg). " +
                "Full EU CMR obligations apply."));
        }
        else
        {
            subChecks.Add(("volume_threshold", "PASS",
                $"Annual tungsten import volume ({totalWeightKg:F0} kg) below EU CMR threshold ({TungstenThresholdKg:F0} kg)."));
        }

        // Determine overall status
        var overallStatus = "PASS";
        if (subChecks.Any(c => c.status == "FAIL")) overallStatus = "FAIL";
        else if (subChecks.Any(c => c.status == "FLAG")) overallStatus = "FLAG";
        else if (subChecks.Any(c => c.status == "INSUFFICIENT_DATA")) overallStatus = "INSUFFICIENT_DATA";

        var check = new ComplianceCheckEntity
        {
            Id = Guid.NewGuid(),
            CustodyEventId = notification.EventId,
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            Framework = "EU_CMR",
            Status = overallStatus,
            Details = JsonSerializer.SerializeToElement(new
            {
                checks = subChecks.Select(c => new { c.name, c.status, c.detail }),
            }),
            CheckedAt = DateTime.UtcNow,
            RuleVersion = "1.0.0-pilot",
        };

        db.ComplianceChecks.Add(check);
        await db.SaveChangesAsync(ct);

        await BatchComplianceRollup.RecalculateAsync(db, notification.BatchId, ct);

        if (overallStatus is "FAIL" or "FLAG" or "INSUFFICIENT_DATA")
        {
            var evt = await db.CustodyEvents.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == notification.EventId, ct);
            if (evt is not null)
            {
                var summaryDetail = string.Join("; ", subChecks.Select(c => $"{c.name}: {c.detail}"));
                await ComplianceNotificationService.CreateNotificationsAsync(
                    db, notification.TenantId, evt.CreatedBy, notification.EventId,
                    overallStatus, $"EU CMR: {summaryDetail}", ct);
            }
        }
    }
}
