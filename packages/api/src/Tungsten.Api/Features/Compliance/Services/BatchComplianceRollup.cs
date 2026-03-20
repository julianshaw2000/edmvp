using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Compliance.Services;

public static class BatchComplianceRollup
{
    public static async Task RecalculateAsync(AppDbContext db, Guid batchId, CancellationToken ct)
    {
        var statuses = await db.ComplianceChecks.AsNoTracking()
            .Where(c => c.BatchId == batchId)
            .Select(c => c.Status)
            .ToListAsync(ct);

        var newStatus = statuses.Count == 0 ? "PENDING" : DetermineStatus(statuses);

        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null) return;

        batch.ComplianceStatus = newStatus;
        batch.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static string DetermineStatus(List<string> statuses)
    {
        if (statuses.Contains("FAIL")) return "FLAGGED"; // spec: displayed as FLAGGED in UI
        if (statuses.Contains("FLAG")) return "FLAGGED";
        if (statuses.Contains("INSUFFICIENT_DATA")) return "INSUFFICIENT_DATA";
        return "COMPLIANT";
    }
}
