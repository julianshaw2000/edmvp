using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Features.Compliance.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Checkers;

public class MassBalanceChecker(AppDbContext db) : INotificationHandler<CustodyEventCreated>
{
    // FR-P007: For CONCENTRATION and PRIMARY_PROCESSING, output weight must not exceed input weight + 5%
    private static readonly HashSet<string> ApplicableEventTypes =
        ["CONCENTRATION", "PRIMARY_PROCESSING"];

    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        if (!ApplicableEventTypes.Contains(notification.EventType))
            return;

        if (notification.Metadata is null)
            return;

        var metadata = notification.Metadata.Value;

        if (!metadata.TryGetProperty("inputWeightKg", out var inputProp) ||
            !metadata.TryGetProperty("outputWeightKg", out var outputProp))
            return;

        if (!TryGetDecimal(inputProp, out var inputWeight) ||
            !TryGetDecimal(outputProp, out var outputWeight))
            return;

        var threshold = inputWeight * 1.05m;
        string status;
        string detail;

        if (outputWeight > threshold)
        {
            status = "FLAG";
            detail = $"Mass balance violation: output {outputWeight:N3} kg exceeds input {inputWeight:N3} kg by more than 5% (threshold {threshold:N3} kg)";
        }
        else
        {
            status = "PASS";
            detail = $"Mass balance check passed: output {outputWeight:N3} kg within 5% tolerance of input {inputWeight:N3} kg";
        }

        var check = new ComplianceCheckEntity
        {
            Id = Guid.NewGuid(),
            CustodyEventId = notification.EventId,
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            Framework = "MASS_BALANCE",
            Status = status,
            Details = JsonSerializer.SerializeToElement(new
            {
                detail,
                inputWeightKg = inputWeight,
                outputWeightKg = outputWeight,
                thresholdKg = threshold,
            }),
            CheckedAt = DateTime.UtcNow,
            RuleVersion = "1.0.0-pilot",
        };

        db.ComplianceChecks.Add(check);
        await db.SaveChangesAsync(ct);

        await BatchComplianceRollup.RecalculateAsync(db, notification.BatchId, ct);

        if (status == "FLAG")
        {
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

    private static bool TryGetDecimal(JsonElement element, out decimal value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetDecimal(out value),
            JsonValueKind.String => decimal.TryParse(
                element.GetString(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out value),
            _ => false,
        };
    }
}
