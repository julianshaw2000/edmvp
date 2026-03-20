# Phase 3: Compliance Engine — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement automated RMAP and OECD DDG compliance checking that runs after each custody event, with batch compliance rollup and notification creation.

**Architecture:** Compliance checks are triggered via MediatR `INotification` published after a custody event is saved. Two independent notification handlers (RmapChecker, OecdDdgChecker) run the checks and persist results. A batch rollup service recalculates the batch compliance status after each check. Notification records are created for FAIL/FLAG results.

**Tech Stack:** ASP.NET Core 10, MediatR, EF Core 10, xUnit, NSubstitute, FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-20-tungsten-pilot-mvp-design.md` — Section 7

**Existing code:**
- Entities exist: `ComplianceCheckEntity`, `NotificationEntity`, `RmapSmelterEntity`, `RiskCountryEntity`, `SanctionedEntityEntity`
- `SeedData.cs` seeds reference data (smelters, risk countries, sanctions)
- `CreateCustodyEvent.cs` creates events — needs to publish a notification after save
- `Common/Result.cs`, `Common/Auth/ICurrentUserService`, `Common/Auth/Roles`

---

## File Structure

```
packages/api/src/Tungsten.Api/
  Features/
    Compliance/
      Events/
        CustodyEventCreated.cs        ← MediatR INotification
      Checkers/
        RmapChecker.cs                ← INotificationHandler — RMAP conformance check
        OecdDdgChecker.cs             ← INotificationHandler — OECD DDG checks
      Services/
        BatchComplianceRollup.cs      ← recalculate batch compliance_status
        ComplianceNotificationService.cs ← create notification records on FAIL/FLAG
      GetBatchCompliance.cs           ← GET /api/batches/{batchId}/compliance
      GetEventCompliance.cs           ← GET /api/events/{eventId}/compliance
      ComplianceEndpoints.cs          ← endpoint mapping

packages/api/tests/Tungsten.Api.Tests/
  Features/
    Compliance/
      RmapCheckerTests.cs
      OecdDdgCheckerTests.cs
      BatchComplianceRollupTests.cs
      ComplianceNotificationServiceTests.cs
```

---

## Task 1: CustodyEventCreated Notification and Publish Hook

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Compliance/Events/CustodyEventCreated.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/CustodyEvents/CreateCustodyEvent.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/CustodyEvents/CreateCorrection.cs`

- [ ] **Step 1: Create CustodyEventCreated notification**

```csharp
using MediatR;

namespace Tungsten.Api.Features.Compliance.Events;

public record CustodyEventCreated(
    Guid EventId,
    Guid BatchId,
    Guid TenantId,
    string EventType,
    string ActorName,
    string? SmelterId) : INotification;
```

- [ ] **Step 2: Modify CreateCustodyEvent to publish notification**

Add `IPublisher publisher` to the Handler constructor. After `await db.SaveChangesAsync(ct);` add:

```csharp
await publisher.Publish(new CustodyEventCreated(
    entity.Id, entity.BatchId, entity.TenantId,
    entity.EventType, entity.ActorName, entity.SmelterId), ct);
```

Update constructor: `public class Handler(AppDbContext db, ICurrentUserService currentUser, IPublisher publisher)`

Add using: `using Tungsten.Api.Features.Compliance.Events;`

- [ ] **Step 3: Same change for CreateCorrection**

Add `IPublisher publisher` and publish after save.

- [ ] **Step 4: Build and verify existing tests still pass**

```bash
cd /c/__edMVP/packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

Note: Existing tests use `new Handler(db, currentUser)` — they need to be updated to pass a mock `IPublisher`. Update `CreateCustodyEventTests` and `CreateCorrectionTests` to add `var publisher = Substitute.For<IPublisher>();` and pass it.

- [ ] **Step 5: Commit**

---

## Task 2: RMAP Checker

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Compliance/Checkers/RmapChecker.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Compliance/RmapCheckerTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task Handle_ConformantSmelter_CreatesPassCheck()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Good Smelter",
            Country = "AT", ConformanceStatus = "CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID001100");

        var handler = new RmapChecker(db);
        await handler.Handle(new CustodyEventCreated(
            Guid.NewGuid(), batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID001100"),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstOrDefaultAsync(c => c.Framework == "RMAP");
        check.Should().NotBeNull();
        check!.Status.Should().Be("PASS");
    }

    [Fact]
    public async Task Handle_ActiveParticipating_CreatesPassCheck()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID002082", SmelterName = "Participating",
            Country = "CN", ConformanceStatus = "ACTIVE_PARTICIPATING", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID002082");

        var handler = new RmapChecker(db);
        await handler.Handle(new CustodyEventCreated(
            Guid.NewGuid(), batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID002082"),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        check.Status.Should().Be("PASS");
    }

    [Fact]
    public async Task Handle_NonConformantSmelter_CreatesFailCheck()
    {
        var db = CreateDb();
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID000999", SmelterName = "Bad Smelter",
            Country = "XX", ConformanceStatus = "NON_CONFORMANT", LoadedAt = DateTime.UtcNow
        });
        var (batchId, tenantId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "CID000999");

        var handler = new RmapChecker(db);
        await handler.Handle(new CustodyEventCreated(
            Guid.NewGuid(), batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "CID000999"),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        check.Status.Should().Be("FAIL");
    }

    [Fact]
    public async Task Handle_UnknownSmelter_CreatesFlagCheck()
    {
        var db = CreateDb();
        // No smelter in DB
        var (batchId, tenantId) = await SeedBatchAndEvent(db, "PRIMARY_PROCESSING", "UNKNOWN");

        var handler = new RmapChecker(db);
        await handler.Handle(new CustodyEventCreated(
            Guid.NewGuid(), batchId, tenantId, "PRIMARY_PROCESSING", "Corp", "UNKNOWN"),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "RMAP");
        check.Status.Should().Be("FLAG");
    }

    [Fact]
    public async Task Handle_NonSmelterEvent_DoesNotCreateCheck()
    {
        var db = CreateDb();
        var (batchId, tenantId) = await SeedBatchAndEvent(db, "MINE_EXTRACTION", null);

        var handler = new RmapChecker(db);
        await handler.Handle(new CustodyEventCreated(
            Guid.NewGuid(), batchId, tenantId, "MINE_EXTRACTION", "Corp", null),
            CancellationToken.None);

        var checks = await db.ComplianceChecks.CountAsync();
        checks.Should().Be(0);
    }

    private static async Task<(Guid batchId, Guid tenantId)> SeedBatchAndEvent(
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
        await db.SaveChangesAsync();
        return (batch.Id, tenant.Id);
    }
}
```

- [ ] **Step 2: Implement RmapChecker**

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Checkers;

public class RmapChecker(AppDbContext db) : INotificationHandler<CustodyEventCreated>
{
    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        // Only check PRIMARY_PROCESSING events with a smelter ID
        if (notification.EventType != "PRIMARY_PROCESSING" ||
            string.IsNullOrEmpty(notification.SmelterId))
            return;

        var smelter = await db.RmapSmelters.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SmelterId == notification.SmelterId, ct);

        string status;
        string detail;

        if (smelter is null)
        {
            status = "FLAG";
            detail = "Smelter not in RMAP list";
        }
        else if (smelter.ConformanceStatus is "CONFORMANT" or "ACTIVE_PARTICIPATING")
        {
            status = "PASS";
            detail = $"Smelter {smelter.SmelterName} is {smelter.ConformanceStatus}";
        }
        else
        {
            status = "FAIL";
            detail = $"Smelter {smelter.SmelterName} is non-conformant per RMAP";
        }

        var check = new ComplianceCheckEntity
        {
            Id = Guid.NewGuid(),
            CustodyEventId = notification.EventId,
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            Framework = "RMAP",
            Status = status,
            Details = JsonSerializer.SerializeToElement(new { detail }),
            CheckedAt = DateTime.UtcNow,
        };

        db.ComplianceChecks.Add(check);
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 3: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "RmapCheckerTests"
```

Expected: 5 tests pass.

- [ ] **Step 4: Commit**

---

## Task 3: OECD DDG Checker

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Compliance/Checkers/OecdDdgChecker.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Compliance/OecdDdgCheckerTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task Handle_HighRiskCountry_CreatesFlagCheck()
    {
        var db = CreateDb();
        db.RiskCountries.Add(new RiskCountryEntity
        {
            CountryCode = "CD", CountryName = "DRC", RiskLevel = "HIGH", Source = "OECD"
        });
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "CD", "Mining Corp");

        var handler = new OecdDdgChecker(db);
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Mining Corp", null),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("FLAG");
    }

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

        var handler = new OecdDdgChecker(db);
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Bad Corp", null),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("FAIL");
    }

    [Fact]
    public async Task Handle_NoDocuments_CreatesInsufficientDataCheck()
    {
        var db = CreateDb();
        // No risk country, no sanctions — but no documents attached
        var (batchId, tenantId, eventId) = await SeedBatchAndEvent(db, "AT", "Good Corp");

        var handler = new OecdDdgChecker(db);
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("INSUFFICIENT_DATA");
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

        // Add required documents for MINE_EXTRACTION: CERTIFICATE_OF_ORIGIN, MINERALOGICAL_CERTIFICATE
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

        var handler = new OecdDdgChecker(db);
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Good Corp", null),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("PASS");
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

        var handler = new OecdDdgChecker(db);
        await handler.Handle(new CustodyEventCreated(
            eventId, batchId, tenantId, "MINE_EXTRACTION", "Bad Corp", null),
            CancellationToken.None);

        var check = await db.ComplianceChecks.FirstAsync(c => c.Framework == "OECD_DDG");
        check.Status.Should().Be("FAIL"); // FAIL trumps everything
    }

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
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.UtcNow, Location = "Loc", ActorName = actorName,
            Description = "Desc", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        await db.SaveChangesAsync();
        return (batch.Id, tenant.Id, evt.Id);
    }
}
```

- [ ] **Step 2: Implement OecdDdgChecker**

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Checkers;

public class OecdDdgChecker(AppDbContext db) : INotificationHandler<CustodyEventCreated>
{
    // Required documents per event type (from spec Section 4.3)
    private static readonly Dictionary<string, string[]> RequiredDocTypes = new()
    {
        ["MINE_EXTRACTION"] = ["CERTIFICATE_OF_ORIGIN", "MINERALOGICAL_CERTIFICATE"],
        ["CONCENTRATION"] = ["ASSAY_REPORT"],
        ["TRADING_TRANSFER"] = ["TRANSPORT_DOCUMENT"],
        ["LABORATORY_ASSAY"] = ["ASSAY_REPORT"],
        ["PRIMARY_PROCESSING"] = ["SMELTER_CERTIFICATE"],
        ["EXPORT_SHIPMENT"] = ["EXPORT_PERMIT", "TRANSPORT_DOCUMENT"],
    };

    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        var subChecks = new List<(string name, string status, string detail)>();

        // 1. Origin country risk
        var batch = await db.Batches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == notification.BatchId, ct);
        if (batch is not null)
        {
            var riskCountry = await db.RiskCountries.AsNoTracking()
                .FirstOrDefaultAsync(r => r.CountryCode == batch.OriginCountry, ct);

            if (riskCountry?.RiskLevel == "HIGH")
                subChecks.Add(("origin_risk", "FLAG", $"Origin country {batch.OriginCountry} is HIGH risk"));
            else
                subChecks.Add(("origin_risk", "PASS", "Origin country not high risk"));
        }

        // 2. Sanctions check
        var isSanctioned = await db.SanctionedEntities.AsNoTracking()
            .AnyAsync(s => s.EntityName == notification.ActorName, ct);
        if (isSanctioned)
            subChecks.Add(("sanctions", "FAIL", $"Actor '{notification.ActorName}' is on sanctions list"));
        else
            subChecks.Add(("sanctions", "PASS", "Actor not sanctioned"));

        // 3. Document completeness
        if (RequiredDocTypes.TryGetValue(notification.EventType, out var required))
        {
            var attachedTypes = await db.Documents.AsNoTracking()
                .Where(d => d.CustodyEventId == notification.EventId)
                .Select(d => d.DocumentType)
                .ToListAsync(ct);

            var missing = required.Except(attachedTypes).ToList();
            if (missing.Count > 0)
                subChecks.Add(("doc_completeness", "INSUFFICIENT_DATA",
                    $"Missing required documents: {string.Join(", ", missing)}"));
            else
                subChecks.Add(("doc_completeness", "PASS", "All required documents attached"));
        }

        // Overall: worst-case of sub-checks (FAIL > FLAG > INSUFFICIENT_DATA > PASS)
        var overallStatus = DetermineOverallStatus(subChecks.Select(s => s.status));

        var check = new ComplianceCheckEntity
        {
            Id = Guid.NewGuid(),
            CustodyEventId = notification.EventId,
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            Framework = "OECD_DDG",
            Status = overallStatus,
            Details = JsonSerializer.SerializeToElement(new
            {
                checks = subChecks.Select(s => new { s.name, s.status, s.detail })
            }),
            CheckedAt = DateTime.UtcNow,
        };

        db.ComplianceChecks.Add(check);
        await db.SaveChangesAsync(ct);
    }

    private static string DetermineOverallStatus(IEnumerable<string> statuses)
    {
        var list = statuses.ToList();
        if (list.Contains("FAIL")) return "FAIL";
        if (list.Contains("FLAG")) return "FLAG";
        if (list.Contains("INSUFFICIENT_DATA")) return "INSUFFICIENT_DATA";
        return "PASS";
    }
}
```

- [ ] **Step 3: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "OecdDdgCheckerTests"
```

Expected: 5 tests pass.

- [ ] **Step 4: Commit**

---

## Task 4: Batch Compliance Rollup

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Compliance/Services/BatchComplianceRollup.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Compliance/BatchComplianceRollupTests.cs`

- [ ] **Step 1: Write tests**

```csharp
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
    public async Task Rollup_NoChecks_SetsBatchPending()
    {
        var db = CreateDb();
        var (batch, _) = await SeedBatch(db);

        await BatchComplianceRollup.RecalculateAsync(db, batch.Id, CancellationToken.None);

        var updated = await db.Batches.FirstAsync(b => b.Id == batch.Id);
        updated.ComplianceStatus.Should().Be("PENDING");
    }

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
```

- [ ] **Step 2: Implement BatchComplianceRollup**

```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Compliance.Services;

public static class BatchComplianceRollup
{
    public static async Task RecalculateAsync(AppDbContext db, Guid batchId, CancellationToken ct)
    {
        var statuses = await db.ComplianceChecks.AsNoTracking()
            .Where(c => c.BatchId == batchId)
            .Select(c => c.Status)
            .ToListAsync(ct);

        var newStatus = statuses.Count == 0 ? "PENDING" : DetermineStatus(statuses);

        var batch = await db.Batches.FirstOrDefaultAsync(b => b.Id == batchId, ct);
        if (batch is null) return;

        batch.ComplianceStatus = newStatus;
        batch.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static string DetermineStatus(List<string> statuses)
    {
        if (statuses.Contains("FAIL")) return "FLAGGED"; // spec: displayed as FLAGGED in UI
        if (statuses.Contains("FLAG")) return "FLAGGED";
        if (statuses.Contains("INSUFFICIENT_DATA")) return "INSUFFICIENT_DATA";
        return "COMPLIANT";
    }
}
```

- [ ] **Step 3: Wire rollup into both checkers**

After `await db.SaveChangesAsync(ct);` in both `RmapChecker` and `OecdDdgChecker`, add:

```csharp
await Services.BatchComplianceRollup.RecalculateAsync(db, notification.BatchId, ct);
```

- [ ] **Step 4: Run all compliance tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "Compliance"
```

Expected: 15 tests pass (5 RMAP + 5 OECD + 5 Rollup).

- [ ] **Step 5: Commit**

---

## Task 5: Compliance Notification Service

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Compliance/Services/ComplianceNotificationService.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Compliance/ComplianceNotificationServiceTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Compliance;

public class ComplianceNotificationServiceTests
{
    [Fact]
    public async Task CreateNotifications_FailStatus_NotifiesSupplierBuyersAndAdmin()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "s", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        var buyer = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "b", Email = "b@t.com",
            DisplayName = "B", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        };
        var admin = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "a", Email = "a@t.com",
            DisplayName = "A", Role = "PLATFORM_ADMIN", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(supplier, buyer, admin);
        await db.SaveChangesAsync();

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FAIL",
            "Smelter non-conformant", CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().HaveCount(3); // supplier + buyer + admin
        notifications.Should().OnlyContain(n => n.Type == "COMPLIANCE_FLAG");
    }

    [Fact]
    public async Task CreateNotifications_PassStatus_NoNotifications()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "s", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, user.Id, Guid.NewGuid(), "PASS",
            "All good", CancellationToken.None);

        var count = await db.Notifications.CountAsync();
        count.Should().Be(0);
    }
}
```

- [ ] **Step 2: Implement ComplianceNotificationService**

```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Compliance.Services;

public static class ComplianceNotificationService
{
    public static async Task CreateNotificationsAsync(
        AppDbContext db,
        Guid tenantId,
        Guid submitterId,
        Guid referenceId,
        string checkStatus,
        string detail,
        CancellationToken ct)
    {
        // Only notify on FAIL or FLAG
        if (checkStatus is not ("FAIL" or "FLAG"))
            return;

        // Get recipients: the supplier + all buyers + all admins in the tenant
        var recipients = await db.Users.AsNoTracking()
            .Where(u => u.TenantId == tenantId && u.IsActive &&
                (u.Id == submitterId || u.Role == "BUYER" || u.Role == "PLATFORM_ADMIN"))
            .Select(u => u.Id)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in recipients)
        {
            db.Notifications.Add(new NotificationEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Type = "COMPLIANCE_FLAG",
                Title = $"Compliance {checkStatus}: attention required",
                Message = detail,
                ReferenceId = referenceId,
                IsRead = false,
                EmailSent = false,
                EmailRetryCount = 0,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 3: Wire notifications into checkers**

After the rollup call in both `RmapChecker` and `OecdDdgChecker`, add:

```csharp
// Get the event's creator for notification
var evt = await db.CustodyEvents.AsNoTracking()
    .FirstOrDefaultAsync(e => e.Id == notification.EventId, ct);
if (evt is not null)
{
    await Services.ComplianceNotificationService.CreateNotificationsAsync(
        db, notification.TenantId, evt.CreatedBy, notification.EventId,
        status, detail, ct);
}
```

(For OecdDdgChecker, use `overallStatus` and a summary detail string.)

- [ ] **Step 4: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "ComplianceNotificationServiceTests"
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit**

---

## Task 6: Compliance Query Endpoints

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Compliance/GetBatchCompliance.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Compliance/GetEventCompliance.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Compliance/ComplianceEndpoints.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Create GetBatchCompliance**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Compliance;

public static class GetBatchCompliance
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record CheckItem(string Framework, string Status, DateTime CheckedAt);

    public record Response(
        Guid BatchId,
        string OverallStatus,
        IReadOnlyList<CheckItem> Checks);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == query.BatchId)
                .OrderByDescending(c => c.CheckedAt)
                .Select(c => new CheckItem(c.Framework, c.Status, c.CheckedAt))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(
                batch.Id, batch.ComplianceStatus, checks));
        }
    }
}
```

- [ ] **Step 2: Create GetEventCompliance**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Compliance;

public static class GetEventCompliance
{
    public record Query(Guid EventId) : IRequest<Result<Response>>;

    public record CheckItem(Guid Id, string Framework, string Status, object? Details, DateTime CheckedAt);

    public record Response(Guid EventId, IReadOnlyList<CheckItem> Checks);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.CustodyEventId == query.EventId && c.TenantId == user.TenantId)
                .Select(c => new CheckItem(c.Id, c.Framework, c.Status, c.Details, c.CheckedAt))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(query.EventId, checks));
        }
    }
}
```

- [ ] **Step 3: Create ComplianceEndpoints**

```csharp
using MediatR;

namespace Tungsten.Api.Features.Compliance;

public static class ComplianceEndpoints
{
    public static IEndpointRouteBuilder MapComplianceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/batches/{batchId:guid}/compliance", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetBatchCompliance.Query(batchId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        app.MapGet("/api/events/{eventId:guid}/compliance", async (Guid eventId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetEventCompliance.Query(eventId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        return app;
    }
}
```

- [ ] **Step 4: Register in Program.cs**

Add `using Tungsten.Api.Features.Compliance;` and `app.MapComplianceEndpoints();` before `app.Run();`.

- [ ] **Step 5: Build and run all tests**

```bash
cd /c/__edMVP/packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

Expected: Build succeeds, all tests pass (~49 total).

- [ ] **Step 6: Commit**

---

## Summary

**Phase 3 delivers:**
1. `CustodyEventCreated` MediatR notification published after event/correction creation
2. RMAP checker: 5 rules (conformant, active_participating, non-conformant, unknown, non-smelter-event skip)
3. OECD DDG checker: 3 sub-checks (origin country risk, sanctions, document completeness), worst-case rollup
4. Batch compliance rollup: recalculates batch status after each check
5. Compliance notification service: notifies supplier + buyers + admin on FAIL/FLAG
6. Query endpoints: GET /api/batches/{id}/compliance, GET /api/events/{id}/compliance

**Tests:** ~17 new tests (5 RMAP + 5 OECD + 5 Rollup + 2 Notifications)

**Next:** Phase 4 — Document Management (Cloudflare R2, upload/download, SHA-256)
