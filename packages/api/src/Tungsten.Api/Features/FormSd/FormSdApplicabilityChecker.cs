using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.FormSd;

public class FormSdApplicabilityChecker(AppDbContext db) : INotificationHandler<CustodyEventCreated>
{
    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        var result = await ApplicabilityEngine.EvaluateAsync(db, notification.BatchId, notification.TenantId, ct);

        var existing = await db.FormSdAssessments.AsNoTracking()
            .Where(a => a.BatchId == notification.BatchId && a.SupersedesId == null)
            .OrderByDescending(a => a.AssessedAt)
            .FirstOrDefaultAsync(ct);

        if (existing?.ApplicabilityStatus == result.Status)
            return;

        db.FormSdAssessments.Add(new FormSdAssessmentEntity
        {
            Id = Guid.NewGuid(),
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            ApplicabilityStatus = result.Status,
            RuleSetVersion = result.RuleSetVersion,
            EngineVersion = result.EngineVersion,
            Reasoning = result.Reasoning?.ToString(),
            SupersedesId = existing?.Id,
            AssessedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);
    }
}
