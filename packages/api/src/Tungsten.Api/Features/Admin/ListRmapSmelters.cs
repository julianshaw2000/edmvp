using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Admin;

public static class ListRmapSmelters
{
    public record Query : IRequest<Result<Response>>;

    public record SmelterItem(
        string SmelterId,
        string SmelterName,
        string Country,
        string ConformanceStatus,
        DateOnly? LastAuditDate,
        DateTime LoadedAt,
        string? MineralType,
        string? FacilityLocation,
        string[]? SourcingCountries);

    public record Response(
        IReadOnlyList<SmelterItem> Smelters,
        int TotalCount,
        DateTime? LastLoadedAt);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var smelters = await db.RmapSmelters.AsNoTracking()
                .OrderBy(s => s.SmelterName)
                .Select(s => new SmelterItem(
                    s.SmelterId, s.SmelterName, s.Country,
                    s.ConformanceStatus, s.LastAuditDate, s.LoadedAt,
                    s.MineralType, s.FacilityLocation, s.SourcingCountries))
                .ToListAsync(ct);

            var lastLoadedAt = smelters.Count > 0
                ? smelters.Max(s => s.LoadedAt)
                : (DateTime?)null;

            return Result<Response>.Success(new Response(smelters, smelters.Count, lastLoadedAt));
        }
    }
}
