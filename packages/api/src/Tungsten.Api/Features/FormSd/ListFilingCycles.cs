using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class ListFilingCycles
{
    public record Query : IRequest<Result<Response>>;
    public record CycleItem(Guid Id, int ReportingYear, DateTime DueDate, string Status, DateTime? SubmittedAt, string? Notes);
    public record Response(IReadOnlyList<CycleItem> Cycles);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var cycles = await db.FormSdFilingCycles.AsNoTracking()
                .Where(c => c.TenantId == tenantId).OrderByDescending(c => c.ReportingYear)
                .Select(c => new CycleItem(c.Id, c.ReportingYear, c.DueDate, c.Status, c.SubmittedAt, c.Notes))
                .ToListAsync(ct);
            return Result<Response>.Success(new Response(cycles));
        }
    }
}
