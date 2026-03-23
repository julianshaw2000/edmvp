using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Pagination;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Platform;

public static class ListTenants
{
    public record Query(int Page, int PageSize) : IRequest<Result<PagedResponse<TenantDto>>>;

    public record TenantDto(Guid Id, string Name, string Status, int UserCount, int BatchCount, DateTime CreatedAt);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Result<PagedResponse<TenantDto>>>
    {
        public async Task<Result<PagedResponse<TenantDto>>> Handle(Query query, CancellationToken ct)
        {
            var totalCount = await db.Tenants.CountAsync(ct);

            var items = await db.Tenants.AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(t => new TenantDto(
                    t.Id, t.Name, t.Status,
                    db.Users.Count(u => u.TenantId == t.Id),
                    db.Batches.Count(b => b.TenantId == t.Id),
                    t.CreatedAt))
                .ToListAsync(ct);

            return Result<PagedResponse<TenantDto>>.Success(
                new PagedResponse<TenantDto>(items, totalCount, query.Page, query.PageSize));
        }
    }
}
