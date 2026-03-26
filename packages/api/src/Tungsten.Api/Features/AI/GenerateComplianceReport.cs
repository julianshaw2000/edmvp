using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services.AI;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.AI;

public static class GenerateComplianceReport
{
    public record Command(string? Period) : IRequest<Result<Response>>;
    public record Response(string Report);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IAiService ai)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var callerRole = await currentUser.GetRoleAsync(ct);
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var batches = db.Batches.AsNoTracking();
            if (callerRole != Roles.Admin)
                batches = batches.Where(b => b.TenantId == tenantId);

            var totalBatches = await batches.CountAsync(ct);
            var compliant = await batches.CountAsync(b => b.ComplianceStatus == "COMPLIANT", ct);
            var flagged = await batches.CountAsync(b => b.ComplianceStatus == "FLAGGED", ct);
            var pending = await batches.CountAsync(b => b.ComplianceStatus == "PENDING", ct);

            var mineralBreakdown = await batches
                .Select(b => new { b.MineralType, b.ComplianceStatus })
                .ToListAsync(ct);

            var countryBreakdown = await batches
                .Select(b => new { b.OriginCountry, b.ComplianceStatus })
                .ToListAsync(ct);

            var totalEvents = await db.CustodyEvents.AsNoTracking()
                .Where(e => callerRole == Roles.Admin || e.TenantId == tenantId)
                .CountAsync(ct);

            var dataContext = $"""
                Period: {cmd.Period ?? "All time"}
                Total Batches: {totalBatches}
                Compliant: {compliant}
                Flagged: {flagged}
                Pending: {pending}
                Total Custody Events: {totalEvents}
                Minerals: {string.Join(", ", mineralBreakdown.GroupBy(b => b.MineralType).Select(g => $"{g.Key}: {g.Count()}"))}
                Countries: {string.Join(", ", countryBreakdown.GroupBy(b => b.OriginCountry).Select(g => $"{g.Key}: {g.Count()}"))}
                Flagged by Country: {string.Join(", ", countryBreakdown.Where(b => b.ComplianceStatus == "FLAGGED").GroupBy(b => b.OriginCountry).Select(g => $"{g.Key}: {g.Count()}"))}
                """;

            var systemPrompt = """
                You are a compliance reporting assistant for auditraks, a 3TG mineral supply chain compliance platform.
                Generate a professional, concise compliance summary report in markdown format.
                Include: executive summary, key metrics, compliance status breakdown, risk areas, and recommendations.
                Be factual — only reference data provided. Do not invent numbers.
                """;

            var report = await ai.GenerateAsync(systemPrompt, dataContext, ct);
            return Result<Response>.Success(new Response(report));
        }
    }
}
