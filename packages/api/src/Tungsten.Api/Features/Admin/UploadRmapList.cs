using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Admin;

public static class UploadRmapList
{
    public record Command(Stream CsvStream) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "UploadRmapList";
        public string EntityType => "RmapSmelter";
    }

    public record Response(int Imported, int Updated, int Total);

    public class Handler(AppDbContext db) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            using var reader = new StreamReader(cmd.CsvStream);
            var header = await reader.ReadLineAsync(ct);
            if (header is null)
                return Result<Response>.Failure("Empty CSV file");

            // Expect: SmelterId,SmelterName,Country,ConformanceStatus,LastAuditDate
            var imported = 0;
            var updated = 0;

            while (await reader.ReadLineAsync(ct) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 4) continue;

                var smelterId = parts[0].Trim();
                var smelterName = parts[1].Trim();
                var country = parts[2].Trim();
                var conformanceStatus = parts[3].Trim();
                DateOnly? lastAuditDate = parts.Length > 4 && !string.IsNullOrWhiteSpace(parts[4])
                    ? DateOnly.Parse(parts[4].Trim(), CultureInfo.InvariantCulture)
                    : null;

                var existing = await db.RmapSmelters.FirstOrDefaultAsync(s => s.SmelterId == smelterId, ct);
                if (existing is not null)
                {
                    existing.SmelterName = smelterName;
                    existing.Country = country;
                    existing.ConformanceStatus = conformanceStatus;
                    existing.LastAuditDate = lastAuditDate;
                    existing.LoadedAt = DateTime.UtcNow;
                    updated++;
                }
                else
                {
                    db.RmapSmelters.Add(new RmapSmelterEntity
                    {
                        SmelterId = smelterId,
                        SmelterName = smelterName,
                        Country = country,
                        ConformanceStatus = conformanceStatus,
                        LastAuditDate = lastAuditDate,
                        LoadedAt = DateTime.UtcNow,
                    });
                    imported++;
                }
            }

            await db.SaveChangesAsync(ct);

            var total = await db.RmapSmelters.CountAsync(ct);
            return Result<Response>.Success(new Response(imported, updated, total));
        }
    }
}
