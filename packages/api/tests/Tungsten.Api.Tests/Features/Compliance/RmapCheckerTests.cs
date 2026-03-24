using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Tungsten.Api.Features.Compliance.Checkers;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Compliance;

public class RmapCheckerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    private static HybridCache CreateCache()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    // ─── scenario 1: CONFORMANT smelter → PASS ───────────────────────────────

    [Fact]
    public async Task Handle_ConformantSmelter_CreatesPassCheck()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID001100");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID001100", null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstOrDefaultAsync(c => c.Framework == "RMAP");
        check.Should().NotBeNull();
        check!.Status.Should().Be("PASS");
    }

    [Fact]
    public async Task Handle_ConformantSmelter_SetsCorrectBatchId()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID001100");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID001100", null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        check.BatchId.Should().Be(batchId);
        check.CustodyEventId.Should().Be(eventId);
        check.TenantId.Should().Be(tenantId);
    }

    // ─── scenario 2: ACTIVE_PARTICIPATING smelter → PASS ─────────────────────

    [Fact]
    public async Task Handle_ActiveParticipating_CreatesPassCheck()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID002082", SmelterName = "Participating",
            Country = "CN", ConformanceStatus = "ACTIVE_PARTICIPATING", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID002082");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID002082", null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        check.Status.Should().Be("PASS");
    }

    // ─── scenario 3: NON_CONFORMANT smelter → FAIL ───────────────────────────

    [Fact]
    public async Task Handle_NonConformantSmelter_CreatesFailCheck()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID000999", SmelterName = "Bad Smelter",
            Country = "XX", ConformanceStatus = "NON_CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID000999");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID000999", null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        check.Status.Should().Be("FAIL");
    }

    [Fact]
    public async Task Handle_NonConformantSmelter_UpdatesBatchToFlagged()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID000999", SmelterName = "Bad Smelter",
            Country = "XX", ConformanceStatus = "NON_CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID000999");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID000999", null, DateTime.UtcNow),
            CancellationToken.None);

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── scenario 4: Unknown smelter (not in DB) → FLAG ──────────────────────

    [Fact]
    public async Task Handle_UnknownSmelter_CreatesFlagCheck()
    {
        var db = CreateDb();
        // No smelter in DB
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "UNKNOWN");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "UNKNOWN", null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        check.Status.Should().Be("FLAG");
    }

    [Fact]
    public async Task Handle_UnknownSmelter_UpdatesBatchToFlagged()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "GHOST_ID");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "GHOST_ID", null, DateTime.UtcNow),
            CancellationToken.None);

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── scenario 5: Non-smelter event type → no check created ───────────────

    [Fact]
    public async Task Handle_MineExtractionEvent_DoesNotCreateCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "MINE_EXTRACTION", null);

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var checks = await db.ComplianceChecks.CountAsync();
        checks.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ConcentrationEvent_DoesNotCreateCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "CONCENTRATION", null);

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "CONCENTRATION", "Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var checks = await db.ComplianceChecks.CountAsync();
        checks.Should().Be(0);
    }

    [Fact]
    public async Task Handle_TradingTransferEvent_DoesNotCreateCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "TRADING_TRANSFER", null);

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "TRADING_TRANSFER", "Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var checks = await db.ComplianceChecks.CountAsync();
        checks.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ExportShipmentEvent_DoesNotCreateCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "EXPORT_SHIPMENT", null);

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "EXPORT_SHIPMENT", "Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var checks = await db.ComplianceChecks.CountAsync();
        checks.Should().Be(0);
    }

    // ─── scenario 6: PRIMARY_PROCESSING with null smelterId → no check ────────

    [Fact]
    public async Task Handle_PrimaryProcessingWithNullSmelterId_DoesNotCreateCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", null);

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var checks = await db.ComplianceChecks.CountAsync();
        checks.Should().Be(0);
    }

    // ─── scenario 7: PRIMARY_PROCESSING with empty string smelterId → no check ─

    [Fact]
    public async Task Handle_PrimaryProcessingWithEmptySmelterId_DoesNotCreateCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "", null, DateTime.UtcNow),
            CancellationToken.None);

        var checks = await db.ComplianceChecks.CountAsync();
        checks.Should().Be(0);
    }

    // ─── rollup side-effects ──────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ConformantSmelter_UpdatesBatchToCompliant()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID001100");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID001100", null, DateTime.UtcNow),
            CancellationToken.None);

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("COMPLIANT");
    }

    // ─── notification side-effects ────────────────────────────────────────────

    [Fact]
    public async Task Handle_NonConformantSmelter_CreatesNotification()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID000999", SmelterName = "Bad Smelter",
            Country = "XX", ConformanceStatus = "NON_CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        // Add a buyer so notifications are generated
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID000999");
        var buyer = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "b1", Email = "b@t.com",
            DisplayName = "B", Role = "BUYER", TenantId = tenantId, IsActive = true
        };
        db.Users.Add(buyer);
        await db.SaveChangesAsync();

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID000999", null, DateTime.UtcNow),
            CancellationToken.None);

        var notifCount = await db.Notifications.CountAsync();
        notifCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_ConformantSmelter_DoesNotCreateNotification()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID001100");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID001100", null, DateTime.UtcNow),
            CancellationToken.None);

        var notifCount = await db.Notifications.CountAsync();
        notifCount.Should().Be(0);
    }

    // ─── check contains correct framework label ───────────────────────────────

    [Fact]
    public async Task Handle_AnyResult_CheckFrameworkIsRmap()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID001100");

        var handler = new RmapChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID001100", null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync();
        check.Framework.Should().Be("RMAP");
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static async Task<(Guid batchId, Guid tenantId, Guid eventId)> SeedBatchAndEvent(
        AppDbContext db, string eventType, string? smelterId)
    {
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B1",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "M",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);
        var evt = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = eventType, IdempotencyKey = Guid.NewGuid().ToString(),
            EventDate = DateTime.UtcNow, Location = "Loc", ActorName = "Corp",
            SmelterId = smelterId, Description = "Desc",
            Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        await db.SaveChangesAsync();
        return (batch.Id, tenant.Id, evt.Id);
    }
}
