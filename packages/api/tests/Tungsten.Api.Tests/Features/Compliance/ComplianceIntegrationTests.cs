using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Checkers;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Features.Compliance.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Compliance;

/// <summary>
/// Full-flow integration tests: seed reference data, dispatch both RmapChecker and OecdDdgChecker,
/// then verify batch ComplianceStatus via BatchComplianceRollup.
/// </summary>
public class ComplianceIntegrationTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    // ─── scenario 1 ──────────────────────────────────────────────────────────
    // PRIMARY_PROCESSING, conformant smelter, low-risk country, all docs
    // → RMAP: PASS + OECD: PASS → batch COMPLIANT

    [Fact]
    public async Task Scenario1_ConformantSmelterLowRiskAllDocs_BothPassBatchCompliant()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });

        var (batchId, tenantId, eventId, userId) = await SeedPrimaryProcessingEvent(db, "AT", "Clean Corp", "CID001100");
        AddRequiredDocs(db, tenantId, batchId, eventId, "PRIMARY_PROCESSING", userId);
        await db.SaveChangesAsync();

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Clean Corp", "CID001100");
        await new RmapChecker(db).Handle(notification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification, CancellationToken.None);

        var checks = await db.ComplianceChecks.ToListAsync();
        checks.Should().HaveCount(2);
        checks.First(c => c.Framework == "RMAP").Status.Should().Be("PASS");
        checks.First(c => c.Framework == "OECD_DDG").Status.Should().Be("PASS");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("COMPLIANT");
    }

    // ─── scenario 2 ──────────────────────────────────────────────────────────
    // PRIMARY_PROCESSING, non-conformant smelter, high-risk country
    // → RMAP: FAIL + OECD: FLAG → batch FLAGGED

    [Fact]
    public async Task Scenario2_NonConformantSmelterHighRiskCountry_FailAndFlagBatchFlagged()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID000999", SmelterName = "Bad Smelter",
            Country = "CD", ConformanceStatus = "NON_CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "CD", CountryName = "DRC", RiskLevel = "HIGH", Source = "OECD"
        });

        var (batchId, tenantId, eventId, userId) = await SeedPrimaryProcessingEvent(db, "CD", "Mining Corp", "CID000999");
        AddRequiredDocs(db, tenantId, batchId, eventId, "PRIMARY_PROCESSING", userId);
        await db.SaveChangesAsync();

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Mining Corp", "CID000999");
        await new RmapChecker(db).Handle(notification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification, CancellationToken.None);

        var checks = await db.ComplianceChecks.ToListAsync();
        checks.First(c => c.Framework == "RMAP").Status.Should().Be("FAIL");
        checks.First(c => c.Framework == "OECD_DDG").Status.Should().Be("FLAG");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── scenario 3 ──────────────────────────────────────────────────────────
    // PRIMARY_PROCESSING, unknown smelter + sanctioned actor
    // → RMAP: FLAG + OECD: FAIL → batch FLAGGED

    [Fact]
    public async Task Scenario3_UnknownSmelterAndSanctionedActor_FlagAndFailBatchFlagged()
    {
        var db = CreateDb();

        // No RmapSmelter in DB → FLAG
        db.SanctionedEntities.Add(new SanctionedEntityEntity
        {
            Id = Guid.NewGuid(), EntityName = "Sanctioned Corp",
            EntityType = "ORGANIZATION", Source = "UN", LoadedAt = DateTime.UtcNow
        });
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });

        var (batchId, tenantId, eventId, userId) = await SeedPrimaryProcessingEvent(db, "AT", "Sanctioned Corp", "GHOST_SMELTER");
        AddRequiredDocs(db, tenantId, batchId, eventId, "PRIMARY_PROCESSING", userId);
        await db.SaveChangesAsync();

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Sanctioned Corp", "GHOST_SMELTER");
        await new RmapChecker(db).Handle(notification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification, CancellationToken.None);

        var checks = await db.ComplianceChecks.ToListAsync();
        checks.First(c => c.Framework == "RMAP").Status.Should().Be("FLAG");
        checks.First(c => c.Framework == "OECD_DDG").Status.Should().Be("FAIL");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── scenario 4 ──────────────────────────────────────────────────────────
    // MINE_EXTRACTION, high-risk country, no documents
    // → no RMAP check + OECD: FLAG (FLAG > INSUFFICIENT_DATA) → batch FLAGGED

    [Fact]
    public async Task Scenario4_MineExtractionHighRiskNoDocuments_NoRmapOecdFlagBatchFlagged()
    {
        var db = CreateDb();

        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "CD", CountryName = "DRC", RiskLevel = "HIGH", Source = "OECD"
        });

        var (batchId, tenantId, eventId, _) = await SeedMineExtractionEvent(db, "CD", "Mining Corp");
        // No documents attached

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "MINE_EXTRACTION", "Mining Corp", null);
        await new RmapChecker(db).Handle(notification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification, CancellationToken.None);

        var checks = await db.ComplianceChecks.ToListAsync();
        checks.Should().NotContain(c => c.Framework == "RMAP", "MINE_EXTRACTION never triggers RMAP check");
        checks.Should().ContainSingle(c => c.Framework == "OECD_DDG");
        checks.First(c => c.Framework == "OECD_DDG").Status.Should().Be("FLAG");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── scenario 5 ──────────────────────────────────────────────────────────
    // MINE_EXTRACTION, low-risk country, all docs, clean actor
    // → no RMAP check + OECD: PASS → batch COMPLIANT

    [Fact]
    public async Task Scenario5_MineExtractionLowRiskAllDocsCleanActor_NoRmapOecdPassBatchCompliant()
    {
        var db = CreateDb();

        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AU", CountryName = "Australia", RiskLevel = "LOW", Source = "OECD"
        });

        var (batchId, tenantId, eventId, userId) = await SeedMineExtractionEvent(db, "AU", "Clean Miner");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION", userId);
        await db.SaveChangesAsync();

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "MINE_EXTRACTION", "Clean Miner", null);
        await new RmapChecker(db).Handle(notification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification, CancellationToken.None);

        var checks = await db.ComplianceChecks.ToListAsync();
        checks.Should().NotContain(c => c.Framework == "RMAP");
        checks.Should().ContainSingle(c => c.Framework == "OECD_DDG");
        checks.First(c => c.Framework == "OECD_DDG").Status.Should().Be("PASS");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("COMPLIANT");
    }

    // ─── scenario 6 ──────────────────────────────────────────────────────────
    // Multiple events: first event PASS, second event FAIL
    // → batch FLAGGED

    [Fact]
    public async Task Scenario6_MultipleEvents_FirstPassSecondFail_BatchFlagged()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID000999", SmelterName = "Bad Smelter",
            Country = "XX", ConformanceStatus = "NON_CONFORMANT", LoadedAt = DateTime.UtcNow
        });

        var (batchId, tenantId, event1Id, _) = await SeedPrimaryProcessingEvent(db, "AT", "Corp A", "CID001100");
        var event2Id = await SeedAdditionalEvent(db, batchId, tenantId, "PRIMARY_PROCESSING", "Corp A", "CID000999");

        // First event: PASS
        var notification1 = new CustodyEventCreated(event1Id, batchId, tenantId, "PRIMARY_PROCESSING", "Corp A", "CID001100");
        await new RmapChecker(db).Handle(notification1, CancellationToken.None);

        // Second event: FAIL
        var notification2 = new CustodyEventCreated(event2Id, batchId, tenantId, "PRIMARY_PROCESSING", "Corp A", "CID000999");
        await new RmapChecker(db).Handle(notification2, CancellationToken.None);

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── scenario 7 ──────────────────────────────────────────────────────────
    // Multiple events: all PASS → batch COMPLIANT

    [Fact]
    public async Task Scenario7_MultipleEventsAllPass_BatchCompliant()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001101", SmelterName = "Another Good Smelter",
            Country = "DE", ConformanceStatus = "ACTIVE_PARTICIPATING", LoadedAt = DateTime.UtcNow
        });

        var (batchId, tenantId, event1Id, _) = await SeedPrimaryProcessingEvent(db, "AT", "Corp A", "CID001100");
        var event2Id = await SeedAdditionalEvent(db, batchId, tenantId, "PRIMARY_PROCESSING", "Corp A", "CID001101");

        var notification1 = new CustodyEventCreated(event1Id, batchId, tenantId, "PRIMARY_PROCESSING", "Corp A", "CID001100");
        await new RmapChecker(db).Handle(notification1, CancellationToken.None);

        var notification2 = new CustodyEventCreated(event2Id, batchId, tenantId, "PRIMARY_PROCESSING", "Corp A", "CID001101");
        await new RmapChecker(db).Handle(notification2, CancellationToken.None);

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("COMPLIANT");
    }

    // ─── scenario 8 ──────────────────────────────────────────────────────────
    // Correction event also triggers compliance checks (IsCorrection = true)

    [Fact]
    public async Task Scenario8_CorrectionEvent_AlsoTriggersComplianceChecks()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });

        var (batchId, tenantId, originalEventId, userId) = await SeedPrimaryProcessingEvent(db, "AT", "Corp A", "CID001100");

        // Create a correction event pointing at the original
        var correctionEventId = Guid.NewGuid();
        db.CustodyEvents.Add(new CustodyEventEntity
        {
            Id = correctionEventId, BatchId = batchId, TenantId = tenantId,
            EventType = "PRIMARY_PROCESSING", IdempotencyKey = Guid.NewGuid().ToString(),
            EventDate = DateTime.UtcNow, Location = "Loc", ActorName = "Corp A",
            SmelterId = "CID001100", Description = "Correction",
            Sha256Hash = new string('c', 64),
            IsCorrection = true, CorrectsEventId = originalEventId,
            CreatedBy = userId, CreatedAt = DateTime.UtcNow
        });
        AddRequiredDocs(db, tenantId, batchId, correctionEventId, "PRIMARY_PROCESSING", userId);
        await db.SaveChangesAsync();

        // Handlers treat correction events the same as regular ones
        var correctionNotification = new CustodyEventCreated(
            correctionEventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp A", "CID001100");
        await new RmapChecker(db).Handle(correctionNotification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(correctionNotification, CancellationToken.None);

        var correctionChecks = await db.ComplianceChecks
            .Where(c => c.CustodyEventId == correctionEventId)
            .ToListAsync();

        correctionChecks.Should().HaveCount(2, "correction event should generate both RMAP and OECD_DDG checks");
        correctionChecks.First(c => c.Framework == "RMAP").Status.Should().Be("PASS");
        correctionChecks.First(c => c.Framework == "OECD_DDG").Status.Should().Be("PASS");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("COMPLIANT");
    }

    // ─── additional edge cases ────────────────────────────────────────────────

    [Fact]
    public async Task BothCheckers_PrimaryProcessing_UnknownSmelterAndHighRiskCountry_BothNonPass()
    {
        var db = CreateDb();

        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "CD", CountryName = "DRC", RiskLevel = "HIGH", Source = "OECD"
        });

        var (batchId, tenantId, eventId, userId) = await SeedPrimaryProcessingEvent(db, "CD", "Miner", "GHOST_SMELTER");
        AddRequiredDocs(db, tenantId, batchId, eventId, "PRIMARY_PROCESSING", userId);
        await db.SaveChangesAsync();

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Miner", "GHOST_SMELTER");
        await new RmapChecker(db).Handle(notification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification, CancellationToken.None);

        var rmapCheck = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        var oecdCheck = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        rmapCheck.Status.Should().Be("FLAG");
        oecdCheck.Status.Should().Be("FLAG");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    [Fact]
    public async Task BothCheckers_RunTwice_CreatesChecksForEachEvent()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });

        var (batchId, tenantId, event1Id, userId) = await SeedPrimaryProcessingEvent(db, "AT", "Corp", "CID001100");
        AddRequiredDocs(db, tenantId, batchId, event1Id, "PRIMARY_PROCESSING", userId);
        var event2Id = await SeedAdditionalEvent(db, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID001100");
        AddRequiredDocs(db, tenantId, batchId, event2Id, "PRIMARY_PROCESSING", userId);
        await db.SaveChangesAsync();

        var notification1 = new CustodyEventCreated(event1Id, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID001100");
        var notification2 = new CustodyEventCreated(event2Id, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID001100");

        await new RmapChecker(db).Handle(notification1, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification1, CancellationToken.None);
        await new RmapChecker(db).Handle(notification2, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification2, CancellationToken.None);

        var totalChecks = await db.ComplianceChecks.CountAsync();
        totalChecks.Should().Be(4); // 2 events × 2 frameworks

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("COMPLIANT");
    }

    [Fact]
    public async Task BothCheckers_FailFromRmapPassFromOecd_BatchFlagged()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID000999", SmelterName = "Bad Smelter",
            Country = "AT", ConformanceStatus = "NON_CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });

        var (batchId, tenantId, eventId, userId) = await SeedPrimaryProcessingEvent(db, "AT", "Clean Corp", "CID000999");
        AddRequiredDocs(db, tenantId, batchId, eventId, "PRIMARY_PROCESSING", userId);
        await db.SaveChangesAsync();

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Clean Corp", "CID000999");
        await new RmapChecker(db).Handle(notification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification, CancellationToken.None);

        var rmapCheck = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        var oecdCheck = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        rmapCheck.Status.Should().Be("FAIL");
        oecdCheck.Status.Should().Be("PASS");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    [Fact]
    public async Task BothCheckers_PassFromRmapInsufficientDataFromOecd_BatchInsufficientData()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });

        // No documents for PRIMARY_PROCESSING → INSUFFICIENT_DATA from OECD
        var (batchId, tenantId, eventId, _) = await SeedPrimaryProcessingEvent(db, "AT", "Good Corp", "CID001100");

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Good Corp", "CID001100");
        await new RmapChecker(db).Handle(notification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification, CancellationToken.None);

        var rmapCheck = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        var oecdCheck = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        rmapCheck.Status.Should().Be("PASS");
        oecdCheck.Status.Should().Be("INSUFFICIENT_DATA");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("INSUFFICIENT_DATA");
    }

    [Fact]
    public async Task BothCheckers_FlagFromRmapAndInsufficientDataFromOecd_BatchFlagged()
    {
        var db = CreateDb();

        // Unknown smelter → FLAG from RMAP
        // Low-risk country, no docs → INSUFFICIENT_DATA from OECD
        // Combined → FLAGGED (FLAG > INSUFFICIENT_DATA)
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });

        var (batchId, tenantId, eventId, _) = await SeedPrimaryProcessingEvent(db, "AT", "Corp", "GHOST_SMELTER");
        // No documents

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "GHOST_SMELTER");
        await new RmapChecker(db).Handle(notification, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(notification, CancellationToken.None);

        var rmapCheck = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        var oecdCheck = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        rmapCheck.Status.Should().Be("FLAG");
        oecdCheck.Status.Should().Be("INSUFFICIENT_DATA");

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    [Fact]
    public async Task MineExtractionAndPrimaryProcessing_MixedResults_FinalRollupCorrect()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "CD", CountryName = "DRC", RiskLevel = "HIGH", Source = "OECD"
        });

        // Mine extraction event in high-risk country, no docs → OECD: FLAG
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

        var mineEvent = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = Guid.NewGuid().ToString(),
            EventDate = DateTime.UtcNow, Location = "Loc", ActorName = "Miner",
            Description = "Mine", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(mineEvent);

        var processEvent = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "PRIMARY_PROCESSING", IdempotencyKey = Guid.NewGuid().ToString(),
            EventDate = DateTime.UtcNow, Location = "Loc", ActorName = "Smelter Corp",
            SmelterId = "CID001100", Description = "Process",
            Sha256Hash = new string('b', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(processEvent);
        AddRequiredDocs(db, tenant.Id, batch.Id, processEvent.Id, "PRIMARY_PROCESSING", user.Id);
        await db.SaveChangesAsync();

        // MINE_EXTRACTION: no RMAP check; OECD → FLAG (high-risk, no docs)
        var mineNotif = new CustodyEventCreated(mineEvent.Id, batch.Id, tenant.Id, "MINE_EXTRACTION", "Miner", null);
        await new RmapChecker(db).Handle(mineNotif, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(mineNotif, CancellationToken.None);

        // PRIMARY_PROCESSING: RMAP → PASS; OECD → PASS (low risk country via default, conformant smelter)
        // Note: batch OriginCountry is "CD" which is HIGH risk — OECD will also FLAG here
        var processNotif = new CustodyEventCreated(processEvent.Id, batch.Id, tenant.Id, "PRIMARY_PROCESSING", "Smelter Corp", "CID001100");
        await new RmapChecker(db).Handle(processNotif, CancellationToken.None);
        await new OecdDdgChecker(db).Handle(processNotif, CancellationToken.None);

        // Any FLAG → FLAGGED overall
        var finalBatch = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        finalBatch.ComplianceStatus.Should().Be("FLAGGED");
    }

    // ─── notification integration ─────────────────────────────────────────────

    [Fact]
    public async Task BothCheckers_FailResult_NotificationsCreatedForBuyersAndAdmin()
    {
        var db = CreateDb();

        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID000999", SmelterName = "Bad Smelter",
            Country = "XX", ConformanceStatus = "NON_CONFORMANT", LoadedAt = DateTime.UtcNow
        });

        var (batchId, tenantId, eventId, _) = await SeedPrimaryProcessingEvent(db, "AT", "Corp", "CID000999");
        var buyer = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "b1", Email = "b@t.com",
            DisplayName = "B", Role = "BUYER", TenantId = tenantId, IsActive = true
        };
        db.Users.Add(buyer);
        await db.SaveChangesAsync();

        var notification = new CustodyEventCreated(eventId, batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID000999");
        await new RmapChecker(db).Handle(notification, CancellationToken.None);

        // Notifications should be created for the FAIL result
        var notifCount = await db.Notifications.CountAsync();
        notifCount.Should().BeGreaterThan(0);

        var notifTypes = await db.Notifications.Select(n => n.Type).Distinct().ToListAsync();
        notifTypes.Should().Contain("COMPLIANCE_FLAG");
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static async Task<(Guid batchId, Guid tenantId, Guid eventId, Guid userId)>
        SeedPrimaryProcessingEvent(AppDbContext db, string originCountry, string actorName, string smelterId)
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
            MineralType = "tungsten", OriginCountry = originCountry, OriginMine = "M",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);
        var evt = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "PRIMARY_PROCESSING", IdempotencyKey = Guid.NewGuid().ToString(),
            EventDate = DateTime.UtcNow, Location = "Loc", ActorName = actorName,
            SmelterId = smelterId, Description = "Processing",
            Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        await db.SaveChangesAsync();
        return (batch.Id, tenant.Id, evt.Id, user.Id);
    }

    private static async Task<(Guid batchId, Guid tenantId, Guid eventId, Guid userId)>
        SeedMineExtractionEvent(AppDbContext db, string originCountry, string actorName)
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
            MineralType = "tungsten", OriginCountry = originCountry, OriginMine = "M",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);
        var evt = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = Guid.NewGuid().ToString(),
            EventDate = DateTime.UtcNow, Location = "Loc", ActorName = actorName,
            Description = "Extraction", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        await db.SaveChangesAsync();
        return (batch.Id, tenant.Id, evt.Id, user.Id);
    }

    private static async Task<Guid> SeedAdditionalEvent(
        AppDbContext db, Guid batchId, Guid tenantId, string eventType, string actorName, string? smelterId)
    {
        var userId = await db.Users.Select(u => u.Id).FirstAsync();
        var evt = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batchId, TenantId = tenantId,
            EventType = eventType, IdempotencyKey = Guid.NewGuid().ToString(),
            EventDate = DateTime.UtcNow, Location = "Loc", ActorName = actorName,
            SmelterId = smelterId, Description = "Additional event",
            Sha256Hash = new string('e', 64),
            CreatedBy = userId, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        await db.SaveChangesAsync();
        return evt.Id;
    }

    /// <summary>Adds all required documents for the given event type.</summary>
    private static void AddRequiredDocs(
        AppDbContext db, Guid tenantId, Guid batchId, Guid eventId, string eventType, Guid userId)
    {
        var docTypes = eventType switch
        {
            "MINE_EXTRACTION" => new[] { "CERTIFICATE_OF_ORIGIN", "MINERALOGICAL_CERTIFICATE" },
            "CONCENTRATION" => new[] { "ASSAY_REPORT" },
            "TRADING_TRANSFER" => new[] { "TRANSPORT_DOCUMENT" },
            "LABORATORY_ASSAY" => new[] { "ASSAY_REPORT" },
            "PRIMARY_PROCESSING" => new[] { "SMELTER_CERTIFICATE" },
            "EXPORT_SHIPMENT" => new[] { "EXPORT_PERMIT", "TRANSPORT_DOCUMENT" },
            _ => Array.Empty<string>()
        };

        var idx = 0;
        foreach (var dt in docTypes)
        {
            idx++;
            db.Documents.Add(new DocumentEntity
            {
                Id = Guid.NewGuid(), TenantId = tenantId, CustodyEventId = eventId, BatchId = batchId,
                FileName = $"doc{idx}_{eventId}.pdf",
                StorageKey = $"key{idx}_{eventId}",
                FileSizeBytes = 1000, ContentType = "application/pdf",
                Sha256Hash = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64],
                DocumentType = dt, UploadedBy = userId
            });
        }
    }
}
