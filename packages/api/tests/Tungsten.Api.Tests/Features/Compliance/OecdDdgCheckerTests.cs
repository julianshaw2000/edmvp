using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Tungsten.Api.Features.Compliance.Checkers;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Compliance;

public class OecdDdgCheckerTests
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

    // ─── scenario 1: HIGH risk origin country → FLAG ─────────────────────────

    [Fact]
    public async Task Handle_HighRiskCountry_CreatesFlagCheck()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "CD", CountryName = "DRC", RiskLevel = "HIGH", Source = "OECD"
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "CD", "Mining Corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Mining Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("FLAG");
    }

    // ─── scenario 2: MEDIUM risk origin country → PASS (only HIGH triggers) ──

    [Fact]
    public async Task Handle_MediumRiskCountry_CreatesPassCheck()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "ZM", CountryName = "Zambia", RiskLevel = "MEDIUM", Source = "OECD"
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "ZM", "Clean Corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Clean Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS");
    }

    // ─── scenario 3: LOW risk origin country → PASS ──────────────────────────

    [Fact]
    public async Task Handle_LowRiskCountry_CreatesPassCheck()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Clean Corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Clean Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS");
    }

    // ─── scenario 4: Unknown origin country (not in DB) → PASS ──────────────

    [Fact]
    public async Task Handle_UnknownOriginCountry_CreatesPassCheck()
    {
        var db = CreateDb();
        // No risk country record in DB — assumed low risk
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "XX", "Clean Corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Clean Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS");
    }

    // ─── scenario 5: Sanctioned actor (exact name match) → FAIL ──────────────

    [Fact]
    public async Task Handle_SanctionedActor_CreatesFailCheck()
    {
        var db = CreateDb();
        db.SanctionedEntities.Add(new SanctionedEntityEntity
        {
            Id = Guid.NewGuid(), EntityName = "Bad Corp",
            EntityType = "ORGANIZATION", Source = "UN", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Bad Corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Bad Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("FAIL");
    }

    [Fact]
    public async Task Handle_SanctionedActor_CaseSensitiveMatch_OnlyExactNameFails()
    {
        var db = CreateDb();
        db.SanctionedEntities.Add(new SanctionedEntityEntity
        {
            Id = Guid.NewGuid(), EntityName = "Bad Corp",
            EntityType = "ORGANIZATION", Source = "UN", LoadedAt = DateTime.UtcNow
        });
        // Actor name with different capitalisation should NOT match
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "bad corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "bad corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS"); // exact-name check only
    }

    // ─── scenario 6: Non-sanctioned actor → PASS ─────────────────────────────

    [Fact]
    public async Task Handle_NonSanctionedActor_PassesSanctionsCheck()
    {
        var db = CreateDb();
        db.SanctionedEntities.Add(new SanctionedEntityEntity
        {
            Id = Guid.NewGuid(), EntityName = "Different Bad Corp",
            EntityType = "ORGANIZATION", Source = "UN", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS");
    }

    // ─── scenario 7: Missing required documents → INSUFFICIENT_DATA ──────────

    [Fact]
    public async Task Handle_NoDocuments_CreatesInsufficientDataCheck()
    {
        var db = CreateDb();
        // No risk country, no sanctions — but no documents attached
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("INSUFFICIENT_DATA");
    }

    [Fact]
    public async Task Handle_MissingAllDocsForConcentration_CreatesInsufficientDataCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "CONCENTRATION", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("INSUFFICIENT_DATA");
    }

    [Fact]
    public async Task Handle_MissingAllDocsForExportShipment_CreatesInsufficientDataCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "EXPORT_SHIPMENT", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("INSUFFICIENT_DATA");
    }

    // ─── scenario 8: All required documents present → PASS ───────────────────

    [Fact]
    public async Task Handle_AllRequiredDocsPresent_CreatesPassCheck()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS");
    }

    [Fact]
    public async Task Handle_LowRiskWithDocs_CreatesPassCheck()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");

        var user = await db.Users.FirstAsync();
        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CustodyEventId = eventId, BatchId = batchId,
            FileName = "cert.pdf", StorageKey = "k1", FileSizeBytes = 1000,
            ContentType = "application/pdf", Sha256Hash = new string('a', 64),
            DocumentType = "CERTIFICATE_OF_ORIGIN", UploadedBy = user.Id
        });
        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CustodyEventId = eventId, BatchId = batchId,
            FileName = "mineral.pdf", StorageKey = "k2", FileSizeBytes = 1000,
            ContentType = "application/pdf", Sha256Hash = new string('b', 64),
            DocumentType = "MINERALOGICAL_CERTIFICATE", UploadedBy = user.Id
        });
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS");
    }

    // ─── scenario 9: Partial documents → INSUFFICIENT_DATA ───────────────────

    [Fact]
    public async Task Handle_PartialDocuments_CreatesInsufficientDataCheck()
    {
        var db = CreateDb();
        // Low-risk country, clean actor, but only 1 of 2 required docs for MINE_EXTRACTION
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");

        var user = await db.Users.FirstAsync();
        // Only CERTIFICATE_OF_ORIGIN present; MINERALOGICAL_CERTIFICATE is missing
        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CustodyEventId = eventId, BatchId = batchId,
            FileName = "cert.pdf", StorageKey = "k1", FileSizeBytes = 1000,
            ContentType = "application/pdf", Sha256Hash = new string('a', 64),
            DocumentType = "CERTIFICATE_OF_ORIGIN", UploadedBy = user.Id
        });
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("INSUFFICIENT_DATA");
    }

    [Fact]
    public async Task Handle_PartialDocsForExportShipment_CreatesInsufficientDataCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");

        var user = await db.Users.FirstAsync();
        // Only EXPORT_PERMIT present; TRANSPORT_DOCUMENT is missing
        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CustodyEventId = eventId, BatchId = batchId,
            FileName = "permit.pdf", StorageKey = "k1", FileSizeBytes = 1000,
            ContentType = "application/pdf", Sha256Hash = new string('c', 64),
            DocumentType = "EXPORT_PERMIT", UploadedBy = user.Id
        });
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "EXPORT_SHIPMENT", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("INSUFFICIENT_DATA");
    }

    // ─── scenario 10: FAIL trumps FLAG ────────────────────────────────────────

    [Fact]
    public async Task Handle_SanctionedActorAndHighRiskCountry_FailTrumpsFlag()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "CD", CountryName = "DRC", RiskLevel = "HIGH", Source = "OECD"
        });
        db.SanctionedEntities.Add(new SanctionedEntityEntity
        {
            Id = Guid.NewGuid(), EntityName = "Sanctioned Miner",
            EntityType = "ORGANIZATION", Source = "UN", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "CD", "Sanctioned Miner");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Sanctioned Miner", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("FAIL");
    }

    [Fact]
    public async Task Handle_SanctionedActorOverridesAll_FailWins()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });
        db.SanctionedEntities.Add(new SanctionedEntityEntity
        {
            Id = Guid.NewGuid(), EntityName = "Bad Corp",
            EntityType = "ORGANIZATION", Source = "UN", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Bad Corp");

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Bad Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("FAIL"); // FAIL trumps everything
    }

    // ─── scenario 11: FLAG trumps INSUFFICIENT_DATA ───────────────────────────

    [Fact]
    public async Task Handle_HighRiskCountryAndMissingDocs_FlagTrumpsInsufficientData()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "CD", CountryName = "DRC", RiskLevel = "HIGH", Source = "OECD"
        });
        // No documents attached
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "CD", "Mining Corp");

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Mining Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("FLAG"); // FLAG > INSUFFICIENT_DATA
    }

    // ─── scenario 12: Multiple sub-checks all PASS → overall PASS ────────────

    [Fact]
    public async Task Handle_AllSubCheckPass_OverallPass()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "DE", CountryName = "Germany", RiskLevel = "LOW", Source = "OECD"
        });
        // No sanctions entry for this actor
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "DE", "Clean Miner GmbH");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Clean Miner GmbH", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS");
    }

    // ─── rollup & notification side-effects ──────────────────────────────────

    [Fact]
    public async Task Handle_FailResult_UpdatesBatchToFlagged()
    {
        var db = CreateDb();
        db.SanctionedEntities.Add(new SanctionedEntityEntity
        {
            Id = Guid.NewGuid(), EntityName = "Bad Corp",
            EntityType = "ORGANIZATION", Source = "UN", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Bad Corp");

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Bad Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("FLAGGED");
    }

    [Fact]
    public async Task Handle_PassResult_UpdatesBatchToCompliant()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var batch = await db.Batches.FirstAsync(b => b.Id == batchId);
        batch.ComplianceStatus.Should().Be("COMPLIANT");
    }

    [Fact]
    public async Task Handle_FailStatus_CreatesNotification()
    {
        var db = CreateDb();
        db.SanctionedEntities.Add(new SanctionedEntityEntity
        {
            Id = Guid.NewGuid(), EntityName = "Bad Corp",
            EntityType = "ORGANIZATION", Source = "UN", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Bad Corp");
        var buyer = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "b1", Email = "b@t.com",
            DisplayName = "B", Role = "BUYER", TenantId = tenantId, IsActive = true
        };
        db.Users.Add(buyer);
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Bad Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var notifCount = await db.Notifications.CountAsync();
        notifCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_PassStatus_DoesNotCreateNotification()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");
        AddRequiredDocs(db, tenantId, batchId, eventId, "MINE_EXTRACTION");
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var notifCount = await db.Notifications.CountAsync();
        notifCount.Should().Be(0);
    }

    // ─── check metadata ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AnyResult_CheckFrameworkIsOecdDdg()
    {
        var db = CreateDb();
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync();
        check.Framework.Should().Be("OECD_DDG");
    }

    // ─── event types without required doc mapping → only origin/sanctions ─────

    [Fact]
    public async Task Handle_EventTypeWithNoDocRequirement_LowRiskCleanActor_Pass()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "AT", CountryName = "Austria", RiskLevel = "LOW", Source = "OECD"
        });
        // Use a LABORATORY_ASSAY event type; its required doc is ASSAY_REPORT
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");
        var user = await db.Users.FirstAsync();
        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, CustodyEventId = eventId, BatchId = batchId,
            FileName = "assay.pdf", StorageKey = "ka", FileSizeBytes = 1000,
            ContentType = "application/pdf", Sha256Hash = new string('d', 64),
            DocumentType = "ASSAY_REPORT", UploadedBy = user.Id
        });
        await db.SaveChangesAsync();

        var handler = new OecdDdgChecker(db, CreateCache());
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "LABORATORY_ASSAY", "Good Corp", null, null, DateTime.UtcNow),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS");
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static async Task<(Guid batchId, Guid tenantId, Guid eventId)> SeedBatchAndEvent(
        AppDbContext db, string originCountry, string actorName)
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
            Description = "Desc", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        await db.SaveChangesAsync();
        return (batch.Id, tenant.Id, evt.Id);
    }

    /// <summary>Adds all required documents for the given event type inline (caller must SaveChangesAsync).</summary>
    private static void AddRequiredDocs(AppDbContext db, Guid tenantId, Guid batchId, Guid eventId, string eventType)
    {
        var userId = db.Users.First().Id;
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
                FileName = $"doc{idx}.pdf", StorageKey = $"key{idx}", FileSizeBytes = 1000,
                ContentType = "application/pdf",
                Sha256Hash = new string((char)('a' + idx), 64),
                DocumentType = dt, UploadedBy = userId
            });
        }
    }
}
