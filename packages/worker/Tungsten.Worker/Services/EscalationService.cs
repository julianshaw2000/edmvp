namespace Tungsten.Worker.Services;

using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

public sealed class EscalationService(
    IServiceScopeFactory scopeFactory,
    ILogger<EscalationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EscalationService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cutoff = DateTime.UtcNow.AddHours(-48);
                var flaggedChecks = await db.ComplianceChecks
                    .Where(c => c.Status == "FLAG" && c.CheckedAt < cutoff)
                    .Select(c => new { c.Id, c.BatchId, c.TenantId, c.Framework })
                    .ToListAsync(stoppingToken);

                foreach (var check in flaggedChecks)
                {
                    // Check if escalation notification already exists
                    var alreadyEscalated = await db.Notifications
                        .AnyAsync(n => n.ReferenceId == check.Id
                            && n.Type == "ESCALATION", stoppingToken);

                    if (alreadyEscalated) continue;

                    // Find all admins for this tenant
                    var admins = await db.Users
                        .Where(u => u.TenantId == check.TenantId && u.Role == "PLATFORM_ADMIN" && u.IsActive)
                        .ToListAsync(stoppingToken);

                    foreach (var admin in admins)
                    {
                        db.Notifications.Add(new NotificationEntity
                        {
                            Id = Guid.NewGuid(),
                            TenantId = check.TenantId,
                            UserId = admin.Id,
                            Type = "ESCALATION",
                            Title = $"Compliance flag unresolved >48h: {check.Framework}",
                            Message = $"Batch {check.BatchId} has an unresolved {check.Framework} flag older than 48 hours.",
                            ReferenceId = check.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Escalated compliance check {CheckId} for batch {BatchId}", check.Id, check.BatchId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EscalationService error");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
