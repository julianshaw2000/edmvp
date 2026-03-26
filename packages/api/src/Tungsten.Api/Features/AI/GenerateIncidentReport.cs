using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services.AI;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.AI;

public static class GenerateIncidentReport
{
    public record Command(Guid BatchId) : IRequest<Result<Response>>;
    public record Response(string Report, string BatchNumber);

    public class Handler(AppDbContext db, IAiService ai) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var batch = await db.Batches.AsNoTracking()
                .Where(b => b.Id == cmd.BatchId)
                .Select(b => new
                {
                    b.Id,
                    b.BatchNumber,
                    b.MineralType,
                    b.OriginCountry,
                    b.OriginMine,
                    b.WeightKg,
                    b.Status,
                    b.ComplianceStatus,
                    b.CreatedAt,
                    TenantName = b.Tenant.Name,
                })
                .FirstOrDefaultAsync(ct);

            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.EventDate)
                .Select(e => new
                {
                    e.EventType,
                    e.EventDate,
                    e.Location,
                    e.ActorName,
                    e.SmelterId,
                    e.Description,
                    e.IsCorrection,
                })
                .ToListAsync(ct);

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == cmd.BatchId)
                .OrderBy(c => c.CheckedAt)
                .Select(c => new
                {
                    c.Framework,
                    c.Status,
                    c.CheckedAt,
                    c.RuleVersion,
                    Details = c.Details.HasValue ? c.Details.Value.ToString() : null,
                })
                .ToListAsync(ct);

            var documents = await db.Documents.AsNoTracking()
                .Where(d => d.BatchId == cmd.BatchId)
                .Select(d => new { d.FileName, d.DocumentType, d.CreatedAt })
                .ToListAsync(ct);

            var eventLines = events.Select(e =>
                $"  - [{e.EventDate:yyyy-MM-dd}] {e.EventType} at {e.Location} by {e.ActorName}" +
                (e.SmelterId != null ? $" (Smelter: {e.SmelterId})" : "") +
                (e.IsCorrection ? " [CORRECTION]" : "") +
                $": {e.Description}");

            var checkLines = checks.Select(c =>
                $"  - [{c.Framework}] {c.Status} (checked {c.CheckedAt:yyyy-MM-dd}, rule v{c.RuleVersion})" +
                (c.Details != null ? $" — {c.Details}" : ""));

            var docLines = documents.Select(d => $"  - {d.DocumentType}: {d.FileName} (uploaded {d.CreatedAt:yyyy-MM-dd})");

            var dataContext = $"""
                COMPLIANCE INCIDENT DATA

                Batch Information:
                - Batch Number: {batch.BatchNumber}
                - Tenant: {batch.TenantName}
                - Mineral Type: {batch.MineralType}
                - Origin Country: {batch.OriginCountry}
                - Origin Mine: {batch.OriginMine}
                - Weight: {batch.WeightKg} kg
                - Status: {batch.Status}
                - Compliance Status: {batch.ComplianceStatus}
                - Created: {batch.CreatedAt:yyyy-MM-dd}

                Custody Events ({events.Count} total):
                {string.Join("\n", eventLines.DefaultIfEmpty("  None recorded"))}

                Compliance Checks ({checks.Count} total):
                {string.Join("\n", checkLines.DefaultIfEmpty("  None recorded"))}

                Supporting Documents ({documents.Count} total):
                {string.Join("\n", docLines.DefaultIfEmpty("  None uploaded"))}
                """;

            var systemPrompt = """
                You are a compliance documentation specialist for a 3TG (tungsten, tin, tantalum, gold) mineral supply chain platform.
                Generate a formal compliance incident report suitable for submission to an external auditor.

                Structure the report as:
                1. Executive Summary
                2. Batch Identification & Chain of Custody
                3. Compliance Check Results (detail each framework check)
                4. Identified Issues & Risk Assessment
                5. Supporting Evidence
                6. Recommended Remediation Steps
                7. Conclusion

                Use formal language. Be precise and factual. Format in markdown.
                Reference specific frameworks: RMAP, OECD DDG, Dodd-Frank Section 1502, EU Regulation 2017/821 where applicable.
                """;

            var report = await ai.GenerateAsync(systemPrompt, dataContext, ct);

            return Result<Response>.Success(new Response(report, batch.BatchNumber));
        }
    }
}
