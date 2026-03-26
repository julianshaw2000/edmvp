using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Pagination;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Batches;

public static class ListBatches
{
    public record Query(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResponse<BatchItem>>>;

    public record BatchItem(
        Guid Id,
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string OriginMine,
        decimal WeightKg,
        string Status,
        string ComplianceStatus,
        DateTime CreatedAt,
        int EventCount);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<PagedResponse<BatchItem>>>
    {
        public async Task<Result<PagedResponse<BatchItem>>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.EntraOid == currentUser.EntraOid && u.IsActive, ct);
            if (user is null)
                return Result<PagedResponse<BatchItem>>.Failure("User not found");

            var baseQuery = db.Batches.AsNoTracking()
                .Where(b => b.TenantId == user.TenantId);

            // Suppliers see only their own batches; buyers and admins see all tenant batches
            if (user.Role == Roles.Supplier)
                baseQuery = baseQuery.Where(b => b.CreatedBy == user.Id);

            var totalCount = await baseQuery.CountAsync(ct);

            var paged = new PagedRequest(query.Page, query.PageSize);
            var items = await baseQuery
                .OrderByDescending(b => b.CreatedAt)
                .Skip(paged.Skip)
                .Take(paged.PageSize)
                .Select(b => new BatchItem(
                    b.Id, b.BatchNumber, b.MineralType,
                    b.OriginCountry, b.OriginMine, b.WeightKg,
                    b.Status, b.ComplianceStatus, b.CreatedAt,
                    b.CustodyEvents.Count))
                .ToListAsync(ct);

            return Result<PagedResponse<BatchItem>>.Success(
                new PagedResponse<BatchItem>(items, totalCount, query.Page, query.PageSize));
        }
    }
}
