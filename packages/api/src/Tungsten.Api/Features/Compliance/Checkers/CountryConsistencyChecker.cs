using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Features.Compliance.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Checkers;

/// <summary>
/// Validates geographic consistency across the custody event chain.
/// Rules:
///   1. MINE_EXTRACTION metadata originCountry must match batch originCountry
///   2. EXPORT_SHIPMENT metadata originCountry must match batch originCountry
///   3. EXPORT_SHIPMENT destinationCountry should match PRIMARY_PROCESSING country (if both exist)
///   4. CONCENTRATION/TRADING_TRANSFER in a sanctioned country that differs from batch origin → FLAG
///   5. Smelter sourcing countries must include batch origin (handled by SmelterOriginCoherenceChecker)
/// </summary>
public class CountryConsistencyChecker(AppDbContext db, HybridCache cache) : INotificationHandler<CustodyEventCreated>
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(1),
        LocalCacheExpiration = TimeSpan.FromHours(1),
    };

    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        var batch = await db.Batches.AsNoTracking()
            .Where(b => b.Id == notification.BatchId)
            .Select(b => new { b.OriginCountry })
            .FirstOrDefaultAsync(ct);

        if (batch is null) return;

        var violations = new List<(string rule, string status, string detail)>();

        // Rule 1: MINE_EXTRACTION metadata originCountry must match batch
        if (notification.EventType == "MINE_EXTRACTION" && notification.Metadata.HasValue)
        {
            var metaOrigin = GetMetadataString(notification.Metadata.Value, "originCountry");
            if (metaOrigin is not null &&
                !metaOrigin.Equals(batch.OriginCountry, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(("MINE_ORIGIN_MISMATCH", "FAIL",
                    $"Mine extraction declares origin country '{metaOrigin}' but batch origin is '{batch.OriginCountry}'"));
            }
        }

        // Rule 2: EXPORT_SHIPMENT metadata originCountry must match batch
        if (notification.EventType == "EXPORT_SHIPMENT" && notification.Metadata.HasValue)
        {
            var exportOrigin = GetMetadataString(notification.Metadata.Value, "originCountry");
            if (exportOrigin is not null &&
                !exportOrigin.Equals(batch.OriginCountry, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add(("EXPORT_ORIGIN_MISMATCH", "FAIL",
                    $"Export shipment declares origin '{exportOrigin}' but batch origin is '{batch.OriginCountry}'"));
            }

            // Rule 3: Export destination should match smelter country (if smelter event exists)
            var exportDest = GetMetadataString(notification.Metadata.Value, "destinationCountry");
            if (exportDest is not null)
            {
                var smelterEvent = await db.CustodyEvents.AsNoTracking()
                    .Where(e => e.BatchId == notification.BatchId
                        && e.EventType == "PRIMARY_PROCESSING"
                        && e.SmelterId != null)
                    .Select(e => new { e.SmelterId })
                    .FirstOrDefaultAsync(ct);

                if (smelterEvent is not null)
                {
                    var smelter = await db.RmapSmelters.AsNoTracking()
                        .Where(s => s.SmelterId == smelterEvent.SmelterId)
                        .Select(s => new { s.Country })
                        .FirstOrDefaultAsync(ct);

                    if (smelter is not null &&
                        !exportDest.Equals(smelter.Country, StringComparison.OrdinalIgnoreCase))
                    {
                        violations.Add(("EXPORT_DEST_SMELTER_MISMATCH", "FLAG",
                            $"Export destination '{exportDest}' does not match smelter country '{smelter.Country}'"));
                    }
                }
            }
        }

        // Rule 4: Events in sanctioned countries different from batch origin
        if (notification.EventType is "CONCENTRATION" or "TRADING_TRANSFER")
        {
            var sanctionedCountries = await cache.GetOrCreateAsync(
                "risk-countries-high",
                async cancel => await db.RiskCountries.AsNoTracking()
                    .Where(r => r.RiskLevel == "HIGH")
                    .Select(r => r.CountryCode)
                    .ToListAsync(cancel),
                CacheOptions,
                cancellationToken: ct);

            // Check if event metadata contains a country that is sanctioned and differs from batch origin
            if (notification.Metadata.HasValue)
            {
                var eventCountry = GetMetadataString(notification.Metadata.Value, "facilityCountry")
                    ?? GetMetadataString(notification.Metadata.Value, "country");

                if (eventCountry is not null &&
                    !eventCountry.Equals(batch.OriginCountry, StringComparison.OrdinalIgnoreCase) &&
                    sanctionedCountries.Contains(eventCountry, StringComparer.OrdinalIgnoreCase))
                {
                    violations.Add(("SANCTIONED_TRANSIT", "FLAG",
                        $"Event occurs in sanctioned country '{eventCountry}' which differs from batch origin '{batch.OriginCountry}'"));
                }
            }
        }

        // If no violations, record a PASS
        if (violations.Count == 0)
        {
            var passCheck = new ComplianceCheckEntity
            {
                Id = Guid.NewGuid(),
                CustodyEventId = notification.EventId,
                BatchId = notification.BatchId,
                TenantId = notification.TenantId,
                Framework = "COUNTRY_CONSISTENCY",
                Status = "PASS",
                Details = JsonSerializer.SerializeToElement(new
                {
                    detail = "All country consistency checks passed",
                    batchOriginCountry = batch.OriginCountry,
                    eventType = notification.EventType,
                }),
                CheckedAt = DateTime.UtcNow,
                RuleVersion = "1.0.0-pilot",
            };
            db.ComplianceChecks.Add(passCheck);
            await db.SaveChangesAsync(ct);
            await BatchComplianceRollup.RecalculateAsync(db, notification.BatchId, ct);
            return;
        }

        // Record each violation as a separate check
        foreach (var (rule, status, detail) in violations)
        {
            var check = new ComplianceCheckEntity
            {
                Id = Guid.NewGuid(),
                CustodyEventId = notification.EventId,
                BatchId = notification.BatchId,
                TenantId = notification.TenantId,
                Framework = "COUNTRY_CONSISTENCY",
                Status = status,
                Details = JsonSerializer.SerializeToElement(new
                {
                    rule,
                    detail,
                    batchOriginCountry = batch.OriginCountry,
                    eventType = notification.EventType,
                }),
                CheckedAt = DateTime.UtcNow,
                RuleVersion = "1.0.0-pilot",
            };
            db.ComplianceChecks.Add(check);
        }

        await db.SaveChangesAsync(ct);
        await BatchComplianceRollup.RecalculateAsync(db, notification.BatchId, ct);

        // Notify on violations
        var worstStatus = violations.Any(v => v.status == "FAIL") ? "FAIL" : "FLAG";
        var summaryDetail = string.Join("; ", violations.Select(v => v.detail));

        var evt = await db.CustodyEvents.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == notification.EventId, ct);
        if (evt is not null)
        {
            await ComplianceNotificationService.CreateNotificationsAsync(
                db, notification.TenantId, evt.CreatedBy, notification.EventId,
                worstStatus, $"Country consistency: {summaryDetail}", ct);
        }
    }

    private static string? GetMetadataString(JsonElement metadata, string key)
    {
        if (metadata.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString();
        return null;
    }
}
