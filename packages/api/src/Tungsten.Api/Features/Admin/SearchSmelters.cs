using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Admin;

public static class SearchSmelters
{
    public record Query(string? Search, string? MineralType, int Page = 1, int PageSize = 20)
        : IRequest<Result<Response>>;

    public record SmelterResult(
        string SmelterId,
        string SmelterName,
        string Country,
        string ConformanceStatus,
        string? MineralType,
        string[]? SourcingCountries);

    public record Response(IReadOnlyList<SmelterResult> Items, int TotalCount);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var q = db.RmapSmelters.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var search = query.Search.ToLower();
                q = q.Where(s => s.SmelterName.ToLower().Contains(search)
                    || s.SmelterId.ToLower().Contains(search)
                    || s.Country.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(query.MineralType))
                q = q.Where(s => s.MineralType != null && s.MineralType.ToLower() == query.MineralType.ToLower());

            var total = await q.CountAsync(ct);

            var items = await q
                .OrderBy(s => s.SmelterName)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(s => new SmelterResult(
                    s.SmelterId, s.SmelterName, s.Country,
                    s.ConformanceStatus, s.MineralType, s.SourcingCountries))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(items, total));
        }
    }
}
