using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Buyer;

public static class GetSupplierEngagement
{
    public record Query : IRequest<Result<Response>>;

    public record SupplierItem(
        Guid Id,
        string DisplayName,
        DateTime? LastEventDate,
        int BatchCount,
        int FlaggedBatchCount,
        string Status);

    public record Response(
        int TotalSuppliers,
        int ActiveSuppliers,
        int StaleSuppliers,
        int FlaggedSuppliers,
        List<SupplierItem> Suppliers);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

            var suppliers = await db.Users.AsNoTracking()
                .Where(u => u.TenantId == tenantId && u.Role == Roles.Supplier && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.DisplayName,
                    Batches = db.Batches.AsNoTracking()
                        .Where(b => b.CreatedBy == u.Id && b.TenantId == tenantId)
                        .Select(b => new
                        {
                            b.ComplianceStatus,
                            LatestEventDate = b.CustodyEvents
                                .OrderByDescending(e => e.EventDate)
                                .Select(e => (DateTime?)e.EventDate)
                                .FirstOrDefault()
                        }).ToList()
                })
                .ToListAsync(ct);

            var items = suppliers.Select(s =>
            {
                var batchCount = s.Batches.Count;
                var flaggedCount = s.Batches.Count(b => b.ComplianceStatus == "FLAGGED");
                var lastEvent = s.Batches
                    .Where(b => b.LatestEventDate.HasValue)
                    .Select(b => b.LatestEventDate!.Value)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

                var status = flaggedCount > 0 ? "flagged"
                    : batchCount == 0 ? "new"
                    : lastEvent == default ? "stale"
                    : lastEvent >= ninetyDaysAgo ? "active"
                    : "stale";

                return new SupplierItem(
                    s.Id,
                    s.DisplayName,
                    lastEvent == default ? null : lastEvent,
                    batchCount,
                    flaggedCount,
                    status);
            }).OrderBy(s => s.Status == "flagged" ? 0 : s.Status == "stale" ? 1 : s.Status == "new" ? 2 : 3)
              .ThenBy(s => s.DisplayName)
              .ToList();

            return Result<Response>.Success(new Response(
                TotalSuppliers: items.Count,
                ActiveSuppliers: items.Count(s => s.Status == "active"),
                StaleSuppliers: items.Count(s => s.Status == "stale"),
                FlaggedSuppliers: items.Count(s => s.Status == "flagged"),
                Suppliers: items));
        }
    }
}
