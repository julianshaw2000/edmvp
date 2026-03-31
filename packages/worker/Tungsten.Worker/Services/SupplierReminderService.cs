using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Worker.Services;

public class SupplierReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<SupplierReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await SendInactivityReminders(db, emailService, stoppingToken);
                await SendStaleWarnings(db, emailService, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SupplierReminderService failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task SendInactivityReminders(AppDbContext db, IEmailService emailService, CancellationToken ct)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var staleBatches = await db.Batches
            .Include(b => b.Creator)
            .Include(b => b.CustodyEvents)
            .Where(b => b.ComplianceStatus != "COMPLIANT"
                && b.Creator.Role == "SUPPLIER"
                && b.Creator.IsActive
                && (b.LastReminderSentAt == null || b.LastReminderSentAt < thirtyDaysAgo))
            .ToListAsync(ct);

        foreach (var batch in staleBatches)
        {
            var lastEvent = batch.CustodyEvents
                .OrderByDescending(e => e.EventDate)
                .FirstOrDefault();

            if (lastEvent is null || lastEvent.EventDate < thirtyDaysAgo)
            {
                var daysSince = lastEvent is null
                    ? (DateTime.UtcNow - batch.CreatedAt).Days
                    : (DateTime.UtcNow - lastEvent.EventDate).Days;

                try
                {
                    var (subject, htmlBody, textBody) = EmailTemplates.BatchInactivityReminder(
                        batch.Creator.DisplayName, batch.BatchNumber, daysSince);

                    await emailService.SendAsync(batch.Creator.Email, subject, htmlBody, textBody, ct);

                    batch.LastReminderSentAt = DateTime.UtcNow;

                    db.Notifications.Add(new NotificationEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = batch.TenantId,
                        UserId = batch.CreatedBy,
                        Type = "INACTIVITY_REMINDER",
                        Title = "Batch needs attention",
                        Message = $"Your batch {batch.BatchNumber} has had no events for {daysSince} days.",
                        ReferenceId = batch.Id,
                        CreatedAt = DateTime.UtcNow,
                    });

                    logger.LogInformation("Inactivity reminder sent for batch {BatchId} to {Email}",
                        batch.Id, batch.Creator.Email);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send inactivity reminder for batch {BatchId}", batch.Id);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SendStaleWarnings(AppDbContext db, IEmailService emailService, CancellationToken ct)
    {
        var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);

        var staleSuppliers = await db.Users.AsNoTracking()
            .Where(u => u.Role == "SUPPLIER" && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                u.TenantId,
                LatestEvent = db.CustodyEvents
                    .Where(e => e.CreatedBy == u.Id)
                    .OrderByDescending(e => e.EventDate)
                    .Select(e => (DateTime?)e.EventDate)
                    .FirstOrDefault(),
                HasBatches = db.Batches.Any(b => b.CreatedBy == u.Id),
                AlreadyNotified = db.Notifications
                    .Any(n => n.UserId == u.Id && n.Type == "STALE_WARNING"
                        && n.CreatedAt > sixtyDaysAgo)
            })
            .Where(u => u.HasBatches
                && (u.LatestEvent == null || u.LatestEvent < sixtyDaysAgo)
                && !u.AlreadyNotified)
            .ToListAsync(ct);

        foreach (var supplier in staleSuppliers)
        {
            try
            {
                var tenantAdmins = await db.Users.AsNoTracking()
                    .Where(u => u.TenantId == supplier.TenantId
                        && (u.Role == "TENANT_ADMIN" || u.Role == "PLATFORM_ADMIN")
                        && u.IsActive)
                    .ToListAsync(ct);

                foreach (var admin in tenantAdmins)
                {
                    db.Notifications.Add(new NotificationEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = supplier.TenantId,
                        UserId = admin.Id,
                        Type = "STALE_WARNING",
                        Title = "Supplier going stale",
                        Message = $"{supplier.DisplayName} has had no activity for 60+ days.",
                        CreatedAt = DateTime.UtcNow,
                    });
                }

                logger.LogInformation("Stale warning created for supplier {SupplierId}", supplier.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create stale warning for supplier {SupplierId}", supplier.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
