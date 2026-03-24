using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Services;

public interface IPlanEnforcementService
{
    Task<string?> CheckBatchLimitAsync(Guid tenantId, CancellationToken ct);
    Task<string?> CheckUserLimitAsync(Guid tenantId, CancellationToken ct);
}

public class PlanEnforcementService(AppDbContext db) : IPlanEnforcementService
{
    public async Task<string?> CheckBatchLimitAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant?.MaxBatches is null)
            return null; // Unlimited

        var currentCount = await db.Batches.CountAsync(b => b.TenantId == tenantId, ct);
        if (currentCount >= tenant.MaxBatches.Value)
            return $"Batch limit reached ({tenant.MaxBatches.Value}). Upgrade your plan to create more batches.";

        return null; // Within limit
    }

    public async Task<string?> CheckUserLimitAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        if (tenant?.MaxUsers is null)
            return null; // Unlimited

        var currentCount = await db.Users.CountAsync(u => u.TenantId == tenantId && u.IsActive, ct);
        if (currentCount >= tenant.MaxUsers.Value)
            return $"User limit reached ({tenant.MaxUsers.Value}). Upgrade your plan to add more users.";

        return null;
    }
}
