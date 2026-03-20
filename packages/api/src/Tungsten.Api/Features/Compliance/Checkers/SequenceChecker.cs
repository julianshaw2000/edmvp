using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Checkers;

/// <summary>
/// FR-P008: Flags when a custody event's timestamp is earlier than the most recent event for that batch.
/// </summary>
public class SequenceChecker(AppDbContext db) : INotificationHandler<CustodyEventCreated>
{
    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        // Find the most recent event for this batch, excluding the current event
        var mostRecentDate = await db.CustodyEvents.AsNoTracking()
            .Where(e => e.BatchId == notification.BatchId && e.Id != notification.EventId)
            .OrderByDescending(e => e.EventDate)
            .Select(e => (DateTime?)e.EventDate)
            .FirstOrDefaultAsync(ct);

        if (mostRecentDate is null)
            return; // First event for this batch — no sequence to check

        string status;
        string detail;

        if (notification.EventDate < mostRecentDate.Value)
        {
            status = "FLAG";
            detail = $"Out-of-order event: event date {notification.EventDate:yyyy-MM-dd HH:mm:ss UTC} is earlier than the most recent batch event at {mostRecentDate.Value:yyyy-MM-dd HH:mm:ss UTC}";
        }
        else
        {
            return; // In-order — no compliance check needed
        }

        var check = new ComplianceCheckEntity
        {
            Id = Guid.NewGuid(),
            CustodyEventId = notification.EventId,
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            Framework = "SEQUENCE_CHECK",
            Status = status,
            Details = JsonSerializer.SerializeToElement(new
            {
                detail,
                eventDate = notification.EventDate,
                mostRecentEventDate = mostRecentDate.Value,
            }),
            CheckedAt = DateTime.UtcNow,
            RuleVersion = "1.0.0-pilot",
        };

        db.ComplianceChecks.Add(check);
        await db.SaveChangesAsync(ct);
    }
}
