using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public record ApplicabilityResult(string Status, string RuleSetVersion, string EngineVersion, JsonElement? Reasoning);

public static class ApplicabilityEngine
{
    private const string RuleSetVersion = "1.0.0";
    private const string EngineVersion = "1.0.0";
    private static readonly string[] CoveredMinerals = ["tungsten", "tin", "tantalum", "gold"];
    private static readonly string[] DrcAdjoining = ["CD", "RW", "BI", "UG", "TZ", "ZM", "AO", "CG", "SS", "CF"];

    public static async Task<ApplicabilityResult> EvaluateAsync(
        AppDbContext db, Guid batchId, Guid tenantId, CancellationToken ct)
    {
        var batch = await db.Batches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId && b.TenantId == tenantId, ct);

        if (batch is null)
            return new ApplicabilityResult("OUT_OF_SCOPE", RuleSetVersion, EngineVersion, null);

        var rules = new List<(string rule, string status, string detail)>();

        // Rule 1: Mineral type must be a covered 3TG mineral
        var isCovered = CoveredMinerals.Any(m => batch.MineralType.ToLower().Contains(m));
        if (!isCovered)
        {
            rules.Add(("mineral_type", "OUT_OF_SCOPE", $"'{batch.MineralType}' is not a covered 3TG mineral"));
            return Build("OUT_OF_SCOPE", rules);
        }
        rules.Add(("mineral_type", "IN_SCOPE", $"'{batch.MineralType}' is a covered 3TG mineral"));

        // Rule 2: Smelter event exists
        var smelterIds = await db.CustodyEvents.AsNoTracking()
            .Where(e => e.BatchId == batchId && e.EventType == "PRIMARY_PROCESSING" && e.SmelterId != null)
            .Select(e => e.SmelterId!).Distinct().ToListAsync(ct);

        if (smelterIds.Count == 0)
        {
            rules.Add(("smelter_identity", "INDETERMINATE", "No smelter event recorded"));
            return Build("INDETERMINATE", rules);
        }

        // Rule 3: Smelters in RMAP list
        var smelters = await db.RmapSmelters.AsNoTracking()
            .Where(s => smelterIds.Contains(s.SmelterId)).ToListAsync(ct);
        var unlisted = smelterIds.Where(id => !smelters.Any(s => s.SmelterId == id)).ToList();
        if (unlisted.Count > 0)
        {
            rules.Add(("smelter_rmap_status", "INDETERMINATE", $"Smelter(s) not in RMAP list: {string.Join(", ", unlisted)}"));
            return Build("INDETERMINATE", rules);
        }
        rules.Add(("smelter_rmap_status", "IN_SCOPE",
            $"All smelters listed: {string.Join(", ", smelters.Select(s => $"{s.SmelterName} ({s.ConformanceStatus})"))}"));

        // Rule 4: Origin country
        var isConflict = DrcAdjoining.Contains(batch.OriginCountry, StringComparer.OrdinalIgnoreCase);
        rules.Add(("origin_country", "IN_SCOPE",
            isConflict ? $"'{batch.OriginCountry}' is DRC/adjoining — covered under §1502"
                       : $"'{batch.OriginCountry}' — covered 3TG mineral regardless of origin"));

        return Build("IN_SCOPE", rules);
    }

    private static ApplicabilityResult Build(string status, List<(string rule, string status, string detail)> rules)
    {
        var reasoning = JsonSerializer.SerializeToElement(new { rules = rules.Select(r => new { r.rule, r.status, r.detail }) });
        return new ApplicabilityResult(status, RuleSetVersion, EngineVersion, reasoning);
    }
}
