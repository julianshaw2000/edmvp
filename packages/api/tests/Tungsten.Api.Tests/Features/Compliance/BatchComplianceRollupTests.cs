using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Compliance;

public class BatchComplianceRollupTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    // ─── scenario 1: All PASS → COMPLIANT ────────────────────────────────────

    [Fact]
    public async Task Rollup_AllPass_SetsBatchCompliant()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("COMPLIANT");
    }

    [Fact]
    public async Task Rollup_SinglePass_SetsBatchCompliant()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("COMPLIANT");
    }

    [Fact]
    public async Task Rollup_ManyPassChecks_SetsBatchCompliant()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        for (var i = 0; i < 5; i++)
            db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("COMPLIANT");
    }

    // ─── scenario 2: Any FAIL → FLAGGED ──────────────────────────────────────

    [Fact]
    public async Task Rollup_AnyFail_SetsBatchFlagged()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "FAIL", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("FLAGGED");
    }

    [Fact]
    public async Task Rollup_OnlyFail_SetsBatchFlagged()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "FAIL", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── scenario 3: Any FLAG (no FAIL) → FLAGGED ────────────────────────────

    [Fact]
    public async Task Rollup_AnyFlag_SetsBatchFlagged()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "FLAG", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("FLAGGED");
    }

    [Fact]
    public async Task Rollup_OnlyFlag_SetsBatchFlagged()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "FLAG", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── scenario 4: Only INSUFFICIENT_DATA → INSUFFICIENT_DATA ──────────────

    [Fact]
    public async Task Rollup_InsufficientData_SetsBatchInsufficientData()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "INSUFFICIENT_DATA", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("INSUFFICIENT_DATA");
    }

    [Fact]
    public async Task Rollup_OnlyInsufficientData_SetsBatchInsufficientData()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "INSUFFICIENT_DATA", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("INSUFFICIENT_DATA");
    }

    // ─── scenario 5: No checks → PENDING ─────────────────────────────────────

    [Fact]
    public async Task Rollup_NoChecks_SetsBatchPending()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("PENDING");
    }

    // ─── scenario 6: Mixed PASS + FAIL + FLAG → FLAGGED (FAIL wins) ──────────

    [Fact]
    public async Task Rollup_PassFailFlag_FailWins_SetsFlagged()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "FAIL", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "FLAG", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("FLAGGED");
    }

    [Fact]
    public async Task Rollup_AllStatuses_FailStillWins()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "FAIL", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "FLAG", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "INSUFFICIENT_DATA", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── scenario 7: Mixed PASS + INSUFFICIENT_DATA → INSUFFICIENT_DATA ───────

    [Fact]
    public async Task Rollup_PassAndInsufficientData_SetsInsufficientData()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "INSUFFICIENT_DATA", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("INSUFFICIENT_DATA");
    }

    // ─── scenario 8: Mixed PASS + FLAG + INSUFFICIENT_DATA → FLAGGED ──────────

    [Fact]
    public async Task Rollup_PassFlagInsufficientData_FlagWinsOverInsufficientData()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "FLAG", Guid.NewGuid()));
        db.ComplianceChecks.Add(MakeCheck(batch.Id, "INSUFFICIENT_DATA", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("FLAGGED"); // FLAG > INSUFFICIENT_DATA
    }

    // ─── UpdatedAt is refreshed ───────────────────────────────────────────────

    [Fact]
    public async Task Rollup_UpdatesBatchUpdatedAt()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);
        var originalUpdatedAt = batch.UpdatedAt;

        // Ensure enough clock difference
        await Task.Delay(5);

        db.ComplianceChecks.Add(MakeCheck(batch.Id, "PASS", Guid.NewGuid()));
        await db.SaveChangesAsync();

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.UpdatedAt.Should().BeOnOrAfter(originalUpdatedAt);
    }

    // ─── nonexistent batch is gracefully ignored ──────────────────────────────

    [Fact]
    public async Task Rollup_NonExistentBatch_DoesNotThrow()
    {
        var db = CreateDb();
        var nonExistentId = Guid.NewGuid();

        var act = () => BatchComplianceRollup.RecalculateAsync(db, nonExistentId, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static async Task<(BatchEntity batch, Guid tenantId)> SeedBatch(AppDbContext db)
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
        await db.SaveChangesAsync();
        return (batch, tenant.Id);
    }

    private static ComplianceCheckEntity MakeCheck(Guid batchId, string status, Guid eventId) => new()
    {
        Id = Guid.NewGuid(), CustodyEventId = eventId, BatchId = batchId,
        TenantId = Guid.NewGuid(), Framework = "RMAP", Status = status,
        CheckedAt = DateTime.UtcNow
    };
}
