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
/// Validates that a smelter's sourcing countries include the batch's origin country.
/// Runs on PRIMARY_PROCESSING events with a SmelterId.
/// </summary>
public class SmelterOriginCoherenceChecker(AppDbContext db, HybridCache cache) : INotificationHandler<CustodyEventCreated>
{
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(1),
        LocalCacheExpiration = TimeSpan.FromHours(1),
    };

    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        if (notification.EventType != "PRIMARY_PROCESSING" ||
            string.IsNullOrEmpty(notification.SmelterId))
            return;

        var smelters = await cache.GetOrCreateAsync(
            "rmap-smelters-sourcing",
            async cancel => await db.RmapSmelters.AsNoTracking()
                .Where(s => s.SourcingCountries != null)
                .Select(s => new CachedSmelterSourcing(s.SmelterId, s.SmelterName, s.SourcingCountries!))
                .ToListAsync(cancel),
            CacheOptions,
            cancellationToken: ct);

        // Get batch origin country
        var batch = await db.Batches.AsNoTracking()
            .Where(b => b.Id == notification.BatchId)
            .Select(b => new { b.OriginCountry })
            .FirstOrDefaultAsync(ct);

        if (batch is null) return;

        var smelter = smelters.FirstOrDefault(s => s.SmelterId == notification.SmelterId);

        string status;
        string detail;

        if (smelter is null)
        {
            // Smelter has no sourcing data — can't verify
            status = "FLAG";
            detail = $"Smelter {notification.SmelterId} has no sourcing country data. Cannot verify origin coherence.";
        }
        else if (smelter.SourcingCountries.Contains(batch.OriginCountry, StringComparer.OrdinalIgnoreCase))
        {
            status = "PASS";
            detail = $"Smelter {smelter.SmelterName} sources from {batch.OriginCountry}. Origin coherence verified.";
        }
        else
        {
            status = "FAIL";
            detail = $"Smelter {smelter.SmelterName} does not source from {batch.OriginCountry}. " +
                     $"Sourcing countries: {string.Join(", ", smelter.SourcingCountries)}.";
        }

        var check = new ComplianceCheckEntity
        {
            Id = Guid.NewGuid(),
            CustodyEventId = notification.EventId,
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            Framework = "SMELTER_ORIGIN",
            Status = status,
            Details = JsonSerializer.SerializeToElement(new
            {
                detail,
                smelterId = notification.SmelterId,
                batchOriginCountry = batch.OriginCountry,
                smelterSourcingCountries = smelter?.SourcingCountries,
            }),
            CheckedAt = DateTime.UtcNow,
            RuleVersion = "1.0.0-pilot",
        };

        db.ComplianceChecks.Add(check);
        await db.SaveChangesAsync(ct);

        await BatchComplianceRollup.RecalculateAsync(db, notification.BatchId, ct);

        if (status is "FAIL" or "FLAG")
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

    private record CachedSmelterSourcing(string SmelterId, string SmelterName, string[] SourcingCountries);
}
