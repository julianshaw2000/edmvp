using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Services;

public static class ComplianceNotificationService
{
    public static async Task CreateNotificationsAsync(
        AppDbContext db,
        Guid tenantId,
        Guid submitterId,
        Guid referenceId,
        string checkStatus,
        string detail,
        CancellationToken ct)
    {
        // Only notify on FAIL or FLAG
        if (checkStatus is not ("FAIL" or "FLAG"))
            return;

        // Get recipients: the supplier + all buyers + all admins in the tenant
        var recipients = await db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive &&
                (u.Id == submitterId || u.Role == "BUYER" || u.Role == "PLATFORM_ADMIN"))
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in recipients)
        {
            db.Notifications.Add(new NotificationEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Type = "COMPLIANCE_FLAG",
                Title = $"Compliance {checkStatus}: attention required",
                Message = detail,
                ReferenceId = referenceId,
                IsRead = false,
                EmailSent = false,
                EmailRetryCount = 0,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
