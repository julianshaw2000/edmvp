using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Features.Compliance.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Checkers;

public class OecdDdgChecker(AppDbContext db) : INotificationHandler<CustodyEventCreated>
{
    // Required documents per event type (from spec Section 4.3)
    private static readonly Dictionary<string, string[]> RequiredDocTypes = new()
    {
        ["MINE_EXTRACTION"] = ["CERTIFICATE_OF_ORIGIN", "MINERALOGICAL_CERTIFICATE"],
        ["CONCENTRATION"] = ["ASSAY_REPORT"],
        ["TRADING_TRANSFER"] = ["TRANSPORT_DOCUMENT"],
        ["LABORATORY_ASSAY"] = ["ASSAY_REPORT"],
        ["PRIMARY_PROCESSING"] = ["SMELTER_CERTIFICATE"],
        ["EXPORT_SHIPMENT"] = ["EXPORT_PERMIT", "TRANSPORT_DOCUMENT"],
    };

    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        var subChecks = new List<(string name, string status, string detail)>();

        // 1. Origin country risk
        var batch = await db.Batches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == notification.BatchId, ct);
        if (batch is not null)
        {
            var riskCountry = await db.RiskCountries.AsNoTracking()
                .FirstOrDefaultAsync(r => r.CountryCode == batch.OriginCountry, ct);

            if (riskCountry?.RiskLevel == "HIGH")
                subChecks.Add(("origin_risk", "FLAG", $"Origin country {batch.OriginCountry} is HIGH risk"));
            else
                subChecks.Add(("origin_risk", "PASS", "Origin country not high risk"));
        }

        // 2. Sanctions check
        var isSanctioned = await db.SanctionedEntities.AsNoTracking()
            .AnyAsync(s => s.EntityName == notification.ActorName, ct);
        if (isSanctioned)
            subChecks.Add(("sanctions", "FAIL", $"Actor '{notification.ActorName}' is on sanctions list"));
        else
            subChecks.Add(("sanctions", "PASS", "Actor not sanctioned"));

        // 3. Document completeness
        if (RequiredDocTypes.TryGetValue(notification.EventType, out var required))
        {
            var attachedTypes = await db.Documents.AsNoTracking()
                .Where(d => d.CustodyEventId == notification.EventId)
                .Select(d => d.DocumentType)
                .ToListAsync(ct);

            var missing = required.Except(attachedTypes).ToList();
            if (missing.Count > 0)
                subChecks.Add(("doc_completeness", "INSUFFICIENT_DATA",
                    $"Missing required documents: {string.Join(", ", missing)}"));
            else
                subChecks.Add(("doc_completeness", "PASS", "All required documents attached"));
        }

        // Overall: worst-case of sub-checks (FAIL > FLAG > INSUFFICIENT_DATA > PASS)
        var overallStatus = DetermineOverallStatus(subChecks.Select(s => s.status));

        var check = new ComplianceCheckEntity
        {
            Id = Guid.NewGuid(),
            CustodyEventId = notification.EventId,
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            Framework = "OECD_DDG",
            Status = overallStatus,
            Details = JsonSerializer.SerializeToElement(new
            {
                checks = subChecks.Select(s => new { s.name, s.status, s.detail })
            }),
            CheckedAt = DateTime.UtcNow,
        };

        db.ComplianceChecks.Add(check);
        await db.SaveChangesAsync(ct);

        await BatchComplianceRollup.RecalculateAsync(db, notification.BatchId, ct);

        // Get the event's creator for notification
        var evt = await db.CustodyEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == notification.EventId, ct);
        if (evt is not null)
        {
            var summaryDetail = $"OECD DDG check result: {overallStatus}. Sub-checks: {string.Join("; ", subChecks.Select(s => $"{s.name}={s.status}"))}";
            await ComplianceNotificationService.CreateNotificationsAsync(
                db, notification.TenantId, evt.CreatedBy, notification.EventId,
                overallStatus, summaryDetail, ct);
        }
    }

    private static string DetermineOverallStatus(IEnumerable<string> statuses)
    {
        var list = statuses.ToList();
        if (list.Contains("FAIL")) return "FAIL";
        if (list.Contains("FLAG")) return "FLAG";
        if (list.Contains("INSUFFICIENT_DATA")) return "INSUFFICIENT_DATA";
        return "PASS";
    }
}
