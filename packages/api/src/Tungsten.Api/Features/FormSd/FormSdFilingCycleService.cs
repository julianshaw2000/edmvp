using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.FormSd;

public class FormSdFilingCycleService(IServiceProvider services, ILogger<FormSdFilingCycleService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow;

                // Flag overdue cycles
                var overdue = await db.FormSdFilingCycles
                    .Where(c => c.DueDate < now && c.Status != "FILED" && c.Status != "OVERDUE")
                    .ToListAsync(stoppingToken);
                foreach (var cycle in overdue)
                {
                    cycle.Status = "OVERDUE";
                    cycle.UpdatedAt = now;
                    logger.LogWarning("Form SD cycle {Year} for tenant {TenantId} is OVERDUE", cycle.ReportingYear, cycle.TenantId);
                }

                // Auto-create current year cycle for tenants that have Form SD
                var currentYear = now.Year;
                var tenantsWithCycles = await db.FormSdFilingCycles
                    .Select(c => c.TenantId).Distinct().ToListAsync(stoppingToken);
                foreach (var tenantId in tenantsWithCycles)
                {
                    if (!await db.FormSdFilingCycles.AnyAsync(c => c.TenantId == tenantId && c.ReportingYear == currentYear, stoppingToken))
                    {
                        db.FormSdFilingCycles.Add(new FormSdFilingCycleEntity
                        {
                            Id = Guid.NewGuid(), TenantId = tenantId, ReportingYear = currentYear,
                            DueDate = new DateTime(currentYear, 6, 30, 23, 59, 59, DateTimeKind.Utc),
                            Status = "NOT_STARTED", CreatedAt = now, UpdatedAt = now,
                        });
                    }
                }

                if (overdue.Count > 0 || tenantsWithCycles.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex) { logger.LogError(ex, "Form SD filing cycle sweep failed"); }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
