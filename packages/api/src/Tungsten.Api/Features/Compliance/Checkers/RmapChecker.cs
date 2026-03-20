using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Features.Compliance.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Checkers;

public class RmapChecker(AppDbContext db) : INotificationHandler<CustodyEventCreated>
{
    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        // Only check PRIMARY_PROCESSING events with a smelter ID
        if (notification.EventType != "PRIMARY_PROCESSING" ||
            string.IsNullOrEmpty(notification.SmelterId))
            return;

        var smelter = await db.RmapSmelters.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SmelterId == notification.SmelterId, ct);

        string status;
        string detail;

        if (smelter is null)
        {
            status = "FLAG";
            detail = "Smelter not in RMAP list";
        }
        else if (smelter.ConformanceStatus is "CONFORMANT" or "ACTIVE_PARTICIPATING")
        {
            status = "PASS";
            detail = $"Smelter {smelter.SmelterName} is {smelter.ConformanceStatus}";
        }
        else
        {
            status = "FAIL";
            detail = $"Smelter {smelter.SmelterName} is non-conformant per RMAP";
        }

        var check = new ComplianceCheckEntity
        {
            Id = Guid.NewGuid(),
            CustodyEventId = notification.EventId,
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            Framework = "RMAP",
            Status = status,
            Details = JsonSerializer.SerializeToElement(new { detail }),
            CheckedAt = DateTime.UtcNow,
            RuleVersion = "1.0.0-pilot",
        };

        db.ComplianceChecks.Add(check);
        await db.SaveChangesAsync(ct);

        await BatchComplianceRollup.RecalculateAsync(db, notification.BatchId, ct);

        // Get the event's creator for notification
        var evt = await db.CustodyEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == notification.EventId, ct);
        if (evt is not null)
        {
            await ComplianceNotificationService.CreateNotificationsAsync(
                db, notification.TenantId, evt.CreatedBy, notification.EventId,
                status, detail, ct);
        }
    }
}
