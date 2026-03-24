using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Services;

public class TrialWarningService(AppDbContext db, IEmailService emailService, ILogger<TrialWarningService> logger)
{
    public async Task SendTrialWarningsAsync(CancellationToken ct)
    {
        var warningDate = DateTime.UtcNow.AddDays(7);

        // Find TRIAL tenants whose trial ends within 7 days and haven't been warned
        var tenants = await db.Tenants.AsNoTracking()
            .Where(t => t.Status == "TRIAL" && t.TrialEndsAt != null && t.TrialEndsAt <= warningDate && t.TrialEndsAt > DateTime.UtcNow)
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            var admin = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Role == "TENANT_ADMIN" && u.IsActive, ct);

            if (admin is null) continue;

            var daysRemaining = (int)(tenant.TrialEndsAt!.Value - DateTime.UtcNow).TotalDays;
            if (daysRemaining < 1) daysRemaining = 1;

            var (subject, htmlBody, textBody) = EmailTemplates.TrialEndingSoon(admin.DisplayName, tenant.Name, daysRemaining);
            try
            {
                await emailService.SendAsync(admin.Email, subject, htmlBody, textBody, ct);
                logger.LogInformation("Sent trial warning to {Email} for tenant {Tenant} ({Days} days remaining)", admin.Email, tenant.Name, daysRemaining);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send trial warning to {Email}", admin.Email);
            }
        }
    }
}
