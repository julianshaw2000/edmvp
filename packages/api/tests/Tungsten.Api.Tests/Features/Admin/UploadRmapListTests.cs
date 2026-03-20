using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Admin;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Admin;

public class UploadRmapListTests
{
    [Fact]
    public async Task Handle_ValidCsv_ImportsSmelters()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var csv = "SmelterId,SmelterName,Country,ConformanceStatus,LastAuditDate\nCID001,Test Smelter,US,CONFORMANT,2025-01-01\nCID002,Another Smelter,DE,NON_CONFORMANT,";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var handler = new UploadRmapList.Handler(db);
        var result = await handler.Handle(new UploadRmapList.Command(stream), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Imported.Should().Be(2);
        result.Value.Total.Should().Be(2);

        var smelters = await db.RmapSmelters.ToListAsync();
        smelters.Should().HaveCount(2);
        smelters.First(s => s.SmelterId == "CID001").ConformanceStatus.Should().Be("CONFORMANT");
    }

    [Fact]
    public async Task Handle_ExistingSmelters_UpdatesThem()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001", SmelterName = "Old Name", Country = "US",
            ConformanceStatus = "NON_CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var csv = "SmelterId,SmelterName,Country,ConformanceStatus,LastAuditDate\nCID001,New Name,US,CONFORMANT,2025-06-01";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var handler = new UploadRmapList.Handler(db);
        var result = await handler.Handle(new UploadRmapList.Command(stream), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Updated.Should().Be(1);
        result.Value.Imported.Should().Be(0);

        var smelter = await db.RmapSmelters.FirstAsync(s => s.SmelterId == "CID001");
        smelter.SmelterName.Should().Be("New Name");
        smelter.ConformanceStatus.Should().Be("CONFORMANT");
    }
}
