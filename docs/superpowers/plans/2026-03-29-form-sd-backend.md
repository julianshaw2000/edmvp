# Form SD Compliance Module — Backend Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Form SD (Dodd-Frank §1502) compliance engine: 3TG applicability determination, supply chain description generation, due diligence documentation, risk assessment, filing cycle tracking, and notifications — all as backend services with API endpoints.

**Architecture:** Extends the existing compliance checker pattern (MediatR `INotificationHandler<CustodyEventCreated>`). New entities for applicability assessments, filing cycles, and Form SD packages. Applicability runs automatically on custody event creation. Supply chain description, due diligence, and risk assessment are generated on-demand via API endpoints. Filing cycles are managed per-tenant per-year with a daily background check for overdue status.

**Tech Stack:** .NET 10, EF Core, PostgreSQL (Neon), MediatR, QuestPDF, Resend email, BackgroundService.

**Depends on:** Existing compliance checkers (RMAP, OECD DDG, Mass Balance, Sequence, Country Consistency, Smelter Origin).

---

## Codebase Context

**Existing patterns to follow:**
- Compliance checkers: `packages/api/src/Tungsten.Api/Features/Compliance/Checkers/*.cs` — each implements `INotificationHandler<CustodyEventCreated>`
- Entities: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/*.cs`
- Configurations: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/*.cs`
- Vertical slice endpoints: `packages/api/src/Tungsten.Api/Features/{Feature}/{Feature}Endpoints.cs`
- MediatR commands/queries: `public record Command(...) : IRequest<Result<Response>>`
- Background services: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/DatabaseMigrationService.cs`
- Email: `IEmailService.SendAsync(to, subject, html, text, ct)`
- Result pattern: `Result<T>.Success(value)` / `Result<T>.Failure(error)`

---

## File Map

### New Entities
- `Infrastructure/Persistence/Entities/FormSdAssessmentEntity.cs` — per-batch applicability result
- `Infrastructure/Persistence/Entities/FormSdFilingCycleEntity.cs` — per-tenant per-year filing tracker
- `Infrastructure/Persistence/Entities/FormSdPackageEntity.cs` — generated support package metadata

### New Configurations
- `Infrastructure/Persistence/Configurations/FormSdAssessmentConfiguration.cs`
- `Infrastructure/Persistence/Configurations/FormSdFilingCycleConfiguration.cs`
- `Infrastructure/Persistence/Configurations/FormSdPackageConfiguration.cs`

### New Feature Files
- `Features/FormSd/FormSdEndpoints.cs` — API route mapping
- `Features/FormSd/ApplicabilityEngine.cs` — deterministic 3TG applicability evaluation
- `Features/FormSd/GenerateSupplyChainDescription.cs` — custody chain narrative
- `Features/FormSd/GenerateDueDiligenceSummary.cs` — OECD DDG flag aggregation
- `Features/FormSd/GenerateRiskAssessment.cs` — per-batch risk summary
- `Features/FormSd/GetBatchFormSdStatus.cs` — query batch applicability
- `Features/FormSd/ListFilingCycles.cs` — query filing cycles for tenant
- `Features/FormSd/UpdateFilingCycleStatus.cs` — update cycle status
- `Features/FormSd/FormSdFilingCycleService.cs` — background service for overdue detection + auto-create

### Modified Files
- `Infrastructure/Persistence/AppDbContext.cs` — add 3 new DbSets
- `Program.cs` — register FormSd endpoints + background service

### Tests
- `tests/Features/FormSd/ApplicabilityEngineTests.cs`
- `tests/Features/FormSd/SupplyChainDescriptionTests.cs`
- `tests/Features/FormSd/RiskAssessmentTests.cs`
- `tests/Features/FormSd/FilingCycleTests.cs`

---

## Chunk 1: Data Model + Migration

### Task 1: Create FormSdAssessmentEntity

**Files:**
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/FormSdAssessmentEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/FormSdAssessmentConfiguration.cs`

- [ ] **Step 1: Create the entity**

```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class FormSdAssessmentEntity
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public required string ApplicabilityStatus { get; set; } // IN_SCOPE, OUT_OF_SCOPE, INDETERMINATE
    public required string RuleSetVersion { get; set; }
    public required string EngineVersion { get; set; }
    public string? Reasoning { get; set; } // JSON: rule-by-rule evaluation
    public Guid? SupersedesId { get; set; } // Points to prior assessment if corrected
    public DateTime AssessedAt { get; set; }
}
```

- [ ] **Step 2: Create the configuration**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class FormSdAssessmentConfiguration : IEntityTypeConfiguration<FormSdAssessmentEntity>
{
    public void Configure(EntityTypeBuilder<FormSdAssessmentEntity> builder)
    {
        builder.ToTable("form_sd_assessments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ApplicabilityStatus).IsRequired().HasMaxLength(20);
        builder.Property(a => a.RuleSetVersion).IsRequired().HasMaxLength(20);
        builder.Property(a => a.EngineVersion).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Reasoning).HasColumnType("jsonb");
        builder.Property(a => a.AssessedAt).HasDefaultValueSql("now()");
        builder.HasIndex(a => new { a.BatchId, a.TenantId });
        builder.HasIndex(a => a.SupersedesId);
    }
}
```

---

### Task 2: Create FormSdFilingCycleEntity

**Files:**
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/FormSdFilingCycleEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/FormSdFilingCycleConfiguration.cs`

- [ ] **Step 1: Create the entity**

```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class FormSdFilingCycleEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int ReportingYear { get; set; }
    public DateTime DueDate { get; set; }
    public required string Status { get; set; } // NOT_STARTED, IN_PROGRESS, PACKAGE_READY, FILED, OVERDUE
    public DateTime? SubmittedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

- [ ] **Step 2: Create the configuration**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class FormSdFilingCycleConfiguration : IEntityTypeConfiguration<FormSdFilingCycleEntity>
{
    public void Configure(EntityTypeBuilder<FormSdFilingCycleEntity> builder)
    {
        builder.ToTable("form_sd_filing_cycles");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Status).IsRequired().HasMaxLength(20);
        builder.HasIndex(c => new { c.TenantId, c.ReportingYear }).IsUnique();
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("now()");
    }
}
```

---

### Task 3: Create FormSdPackageEntity

**Files:**
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/FormSdPackageEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/FormSdPackageConfiguration.cs`

- [ ] **Step 1: Create the entity**

```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class FormSdPackageEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int ReportingYear { get; set; }
    public required string StorageKey { get; set; }
    public required string Sha256Hash { get; set; }
    public required string RuleSetVersion { get; set; }
    public required string PlatformVersion { get; set; }
    public Guid GeneratedBy { get; set; }
    public string? SourceJson { get; set; } // Full JSON source data
    public DateTime GeneratedAt { get; set; }
}
```

- [ ] **Step 2: Create the configuration**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class FormSdPackageConfiguration : IEntityTypeConfiguration<FormSdPackageEntity>
{
    public void Configure(EntityTypeBuilder<FormSdPackageEntity> builder)
    {
        builder.ToTable("form_sd_packages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Sha256Hash).IsRequired().HasMaxLength(64);
        builder.Property(p => p.RuleSetVersion).IsRequired().HasMaxLength(20);
        builder.Property(p => p.PlatformVersion).IsRequired().HasMaxLength(20);
        builder.Property(p => p.SourceJson).HasColumnType("jsonb");
        builder.Property(p => p.GeneratedAt).HasDefaultValueSql("now()");
        builder.HasIndex(p => new { p.TenantId, p.ReportingYear });
    }
}
```

---

### Task 4: Add DbSets + Generate Migration

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/AppDbContext.cs`

- [ ] **Step 1: Add DbSets**

```csharp
public DbSet<FormSdAssessmentEntity> FormSdAssessments => Set<FormSdAssessmentEntity>();
public DbSet<FormSdFilingCycleEntity> FormSdFilingCycles => Set<FormSdFilingCycleEntity>();
public DbSet<FormSdPackageEntity> FormSdPackages => Set<FormSdPackageEntity>();
```

- [ ] **Step 2: Generate migration**

Run: `cd packages/api/src/Tungsten.Api && dotnet ef migrations add AddFormSdTables --context AppDbContext`

- [ ] **Step 3: Build and verify**

Run: `cd packages/api && dotnet build`

- [ ] **Step 4: Commit**

```bash
git add packages/api/
git commit -m "feat: add Form SD data model — assessments, filing cycles, packages"
```

---

## Chunk 2: 3TG Applicability Engine

### Task 5: Create ApplicabilityEngine

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/ApplicabilityEngine.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/FormSd/ApplicabilityEngineTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.FormSd;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.FormSd;

public class ApplicabilityEngineTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Evaluate_TungstenBatch_WithConformantSmelter_ReturnsInScope()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Batches.Add(new BatchEntity
        {
            Id = batchId, TenantId = tenantId, BatchNumber = "TEST-001",
            MineralType = "Tungsten (Wolframite)", OriginCountry = "RW",
            OriginMine = "Test Mine", WeightKg = 100, Status = "ACTIVE",
            ComplianceStatus = "PENDING", CreatedBy = userId,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        db.RmapSmelters.Add(new RmapSmelterEntity
        {
            SmelterId = "CID001100", SmelterName = "Wolfram Bergbau",
            Country = "AT", ConformanceStatus = "CONFORMANT",
            LoadedAt = DateTime.UtcNow,
        });
        db.CustodyEvents.Add(new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batchId, TenantId = tenantId,
            EventType = "PRIMARY_PROCESSING", EventDate = DateTime.UtcNow,
            Location = "Austria", ActorName = "Test", Description = "Smelting",
            SmelterId = "CID001100", Sha256Hash = "abc123",
            IdempotencyKey = Guid.NewGuid().ToString(),
            CreatedBy = userId, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await ApplicabilityEngine.EvaluateAsync(db, batchId, tenantId, default);

        Assert.Equal("IN_SCOPE", result.Status);
        Assert.Equal("1.0.0", result.RuleSetVersion);
    }

    [Fact]
    public async Task Evaluate_NonTungstenBatch_ReturnsOutOfScope()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        db.Batches.Add(new BatchEntity
        {
            Id = batchId, TenantId = tenantId, BatchNumber = "TEST-002",
            MineralType = "Copper", OriginCountry = "CL",
            OriginMine = "Test Mine", WeightKg = 100, Status = "ACTIVE",
            ComplianceStatus = "PENDING", CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await ApplicabilityEngine.EvaluateAsync(db, batchId, tenantId, default);

        Assert.Equal("OUT_OF_SCOPE", result.Status);
    }

    [Fact]
    public async Task Evaluate_TungstenBatch_NoSmelterEvent_ReturnsIndeterminate()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();

        db.Batches.Add(new BatchEntity
        {
            Id = batchId, TenantId = tenantId, BatchNumber = "TEST-003",
            MineralType = "Tungsten (Wolframite)", OriginCountry = "RW",
            OriginMine = "Test Mine", WeightKg = 100, Status = "ACTIVE",
            ComplianceStatus = "PENDING", CreatedBy = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var result = await ApplicabilityEngine.EvaluateAsync(db, batchId, tenantId, default);

        Assert.Equal("INDETERMINATE", result.Status);
    }
}
```

- [ ] **Step 2: Run tests — should fail (class doesn't exist)**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~ApplicabilityEngine"`

- [ ] **Step 3: Implement ApplicabilityEngine**

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.FormSd;

public record ApplicabilityResult(string Status, string RuleSetVersion, string EngineVersion, JsonElement? Reasoning);

public static class ApplicabilityEngine
{
    private const string RuleSetVersion = "1.0.0";
    private const string EngineVersion = "1.0.0";

    // 3TG minerals covered by Dodd-Frank §1502
    private static readonly string[] CoveredMinerals = ["tungsten", "tin", "tantalum", "gold"];

    public static async Task<ApplicabilityResult> EvaluateAsync(
        AppDbContext db, Guid batchId, Guid tenantId, CancellationToken ct)
    {
        var batch = await db.Batches.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == batchId && b.TenantId == tenantId, ct);

        if (batch is null)
            return new ApplicabilityResult("OUT_OF_SCOPE", RuleSetVersion, EngineVersion, null);

        var rules = new List<(string rule, string status, string detail)>();

        // Rule 1: Mineral type must be a covered 3TG mineral
        var mineralLower = batch.MineralType.ToLower();
        var isCoveredMineral = CoveredMinerals.Any(m => mineralLower.Contains(m));

        if (!isCoveredMineral)
        {
            rules.Add(("mineral_type", "OUT_OF_SCOPE",
                $"Mineral type '{batch.MineralType}' is not a covered 3TG mineral"));
            return BuildResult("OUT_OF_SCOPE", rules);
        }
        rules.Add(("mineral_type", "IN_SCOPE", $"Mineral type '{batch.MineralType}' is a covered 3TG mineral"));

        // Rule 2: Check if smelter event exists with RMAP-listed smelter
        var smelterEvents = await db.CustodyEvents.AsNoTracking()
            .Where(e => e.BatchId == batchId && e.EventType == "PRIMARY_PROCESSING" && e.SmelterId != null)
            .Select(e => e.SmelterId!)
            .ToListAsync(ct);

        if (smelterEvents.Count == 0)
        {
            rules.Add(("smelter_identity", "INDETERMINATE",
                "No smelter/processing event recorded — cannot determine full supply chain origin"));
            return BuildResult("INDETERMINATE", rules);
        }

        // Rule 3: Verify smelter is in RMAP list and check country of origin
        var smelterIds = smelterEvents.Distinct().ToList();
        var smelters = await db.RmapSmelters.AsNoTracking()
            .Where(s => smelterIds.Contains(s.SmelterId))
            .ToListAsync(ct);

        var allSmeltersListed = smelterIds.All(id => smelters.Any(s => s.SmelterId == id));
        if (!allSmeltersListed)
        {
            var unlisted = smelterIds.Where(id => !smelters.Any(s => s.SmelterId == id));
            rules.Add(("smelter_rmap_status", "INDETERMINATE",
                $"Smelter(s) not in RMAP list: {string.Join(", ", unlisted)}"));
            return BuildResult("INDETERMINATE", rules);
        }

        rules.Add(("smelter_rmap_status", "IN_SCOPE",
            $"All smelters in RMAP list: {string.Join(", ", smelters.Select(s => $"{s.SmelterName} ({s.ConformanceStatus})"))}"));

        // Rule 4: Origin country — DRC or adjoining countries trigger In Scope
        var drcAdjoining = new[] { "CD", "RW", "BI", "UG", "TZ", "ZM", "AO", "CG", "SS", "CF" };
        var isConflictOrigin = drcAdjoining.Contains(batch.OriginCountry, StringComparer.OrdinalIgnoreCase);

        if (isConflictOrigin)
        {
            rules.Add(("origin_country", "IN_SCOPE",
                $"Origin country '{batch.OriginCountry}' is DRC or adjoining country — covered under §1502"));
        }
        else
        {
            rules.Add(("origin_country", "IN_SCOPE",
                $"Origin country '{batch.OriginCountry}' — mineral is covered 3TG regardless of origin"));
        }

        return BuildResult("IN_SCOPE", rules);
    }

    private static ApplicabilityResult BuildResult(string status, List<(string rule, string status, string detail)> rules)
    {
        var reasoning = JsonSerializer.SerializeToElement(new
        {
            rules = rules.Select(r => new { r.rule, r.status, r.detail }),
        });
        return new ApplicabilityResult(status, RuleSetVersion, EngineVersion, reasoning);
    }
}
```

- [ ] **Step 4: Run tests — should pass**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~ApplicabilityEngine"`
Expected: 3/3 pass

- [ ] **Step 5: Commit**

```bash
git commit -m "feat: 3TG applicability engine with deterministic rule evaluation"
```

---

### Task 6: Wire ApplicabilityEngine into compliance pipeline

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/FormSdApplicabilityChecker.cs`

- [ ] **Step 1: Create the MediatR notification handler**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Events;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.FormSd;

/// <summary>
/// Runs 3TG applicability assessment after each custody event.
/// Only creates a new assessment if the result differs from the most recent one.
/// </summary>
public class FormSdApplicabilityChecker(AppDbContext db) : INotificationHandler<CustodyEventCreated>
{
    public async Task Handle(CustodyEventCreated notification, CancellationToken ct)
    {
        var result = await ApplicabilityEngine.EvaluateAsync(
            db, notification.BatchId, notification.TenantId, ct);

        // Check if assessment already exists with same status
        var existing = await db.FormSdAssessments.AsNoTracking()
            .Where(a => a.BatchId == notification.BatchId && a.SupersedesId == null)
            .OrderByDescending(a => a.AssessedAt)
            .FirstOrDefaultAsync(ct);

        if (existing?.ApplicabilityStatus == result.Status)
            return; // No change

        var assessment = new FormSdAssessmentEntity
        {
            Id = Guid.NewGuid(),
            BatchId = notification.BatchId,
            TenantId = notification.TenantId,
            ApplicabilityStatus = result.Status,
            RuleSetVersion = result.RuleSetVersion,
            EngineVersion = result.EngineVersion,
            Reasoning = result.Reasoning?.ToString(),
            SupersedesId = existing?.Id,
            AssessedAt = DateTime.UtcNow,
        };

        db.FormSdAssessments.Add(assessment);
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Build and verify**

Run: `cd packages/api && dotnet build`

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: wire Form SD applicability into compliance pipeline"
```

---

## Chunk 3: Supply Chain Description + Risk Assessment

### Task 7: Supply Chain Description Generator

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/GenerateSupplyChainDescription.cs`

- [ ] **Step 1: Create the handler**

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class GenerateSupplyChainDescription
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record ChainLink(string EventType, DateTime EventDate, string Location, string ActorName, string? SmelterId);
    public record ChainGap(string Description, string Severity);

    public record Response(
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string OriginMine,
        IReadOnlyList<ChainLink> Chain,
        IReadOnlyList<ChainGap> Gaps,
        string NarrativeText,
        string SourceJson);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.TenantId == tenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId)
                .OrderBy(e => e.EventDate)
                .Select(e => new ChainLink(e.EventType, e.EventDate, e.Location, e.ActorName, e.SmelterId))
                .ToListAsync(ct);

            // Detect gaps in the custody chain
            var gaps = new List<ChainGap>();
            var expectedSequence = new[] { "MINE_EXTRACTION", "LABORATORY_ASSAY", "CONCENTRATION", "TRADING_TRANSFER", "PRIMARY_PROCESSING", "EXPORT_SHIPMENT" };
            var presentTypes = events.Select(e => e.EventType).Distinct().ToHashSet();

            foreach (var expected in expectedSequence)
            {
                if (!presentTypes.Contains(expected))
                {
                    var severity = expected is "MINE_EXTRACTION" or "PRIMARY_PROCESSING" ? "HIGH" : "MEDIUM";
                    gaps.Add(new ChainGap($"Missing {expected} event in custody chain", severity));
                }
            }

            // Check for smelter identification
            var hasSmelter = events.Any(e => e.SmelterId is not null);
            if (!hasSmelter)
                gaps.Add(new ChainGap("No smelter identification in supply chain", "HIGH"));

            // Generate narrative
            var narrative = GenerateNarrative(batch, events, gaps);

            var sourceJson = JsonSerializer.Serialize(new { batch = new { batch.BatchNumber, batch.MineralType, batch.OriginCountry, batch.OriginMine }, events, gaps });

            return Result<Response>.Success(new Response(
                batch.BatchNumber, batch.MineralType, batch.OriginCountry,
                batch.OriginMine, events, gaps, narrative, sourceJson));
        }

        private static string GenerateNarrative(dynamic batch, List<ChainLink> events, List<ChainGap> gaps)
        {
            var lines = new List<string>
            {
                $"Batch {batch.BatchNumber} tracks {batch.MineralType} originating from {batch.OriginMine}, {batch.OriginCountry}.",
                $"The supply chain comprises {events.Count} documented custody events.",
            };

            foreach (var e in events)
            {
                var smelterNote = e.SmelterId is not null ? $" (Smelter: {e.SmelterId})" : "";
                lines.Add($"  - {e.EventType}: {e.Location}, {e.EventDate:yyyy-MM-dd} — {e.ActorName}{smelterNote}");
            }

            if (gaps.Count > 0)
            {
                lines.Add($"\n{gaps.Count} gap(s) identified in the custody chain:");
                foreach (var g in gaps)
                    lines.Add($"  - [{g.Severity}] {g.Description}");
            }
            else
            {
                lines.Add("\nNo gaps identified. Custody chain is complete.");
            }

            return string.Join("\n", lines);
        }
    }
}
```

- [ ] **Step 2: Build and verify**

Run: `cd packages/api && dotnet build`

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: supply chain description generator with gap detection"
```

---

### Task 8: Due Diligence Summary Generator

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/GenerateDueDiligenceSummary.cs`

- [ ] **Step 1: Create the handler**

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class GenerateDueDiligenceSummary
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record RiskFlag(string Framework, string Status, string Detail, DateTime CheckedAt);
    public record SmelterStatus(string SmelterId, string SmelterName, string ConformanceStatus, DateOnly? LastAuditDate);

    public record Response(
        IReadOnlyList<RiskFlag> RiskFlags,
        IReadOnlyList<SmelterStatus> Smelters,
        string OecdDdgVersion,
        string SummaryText);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.TenantId == tenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            // Aggregate all compliance check results for this batch
            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == query.BatchId)
                .OrderByDescending(c => c.CheckedAt)
                .ToListAsync(ct);

            var riskFlags = checks
                .Where(c => c.Status is "FLAG" or "FAIL" or "INSUFFICIENT_DATA")
                .Select(c =>
                {
                    var detail = "";
                    if (c.Details.HasValue)
                    {
                        try { detail = c.Details.Value.GetProperty("detail").GetString() ?? ""; }
                        catch { detail = c.Details.Value.ToString(); }
                    }
                    return new RiskFlag(c.Framework, c.Status, detail, c.CheckedAt);
                })
                .ToList();

            // Get smelter statuses for smelters in the chain
            var smelterIds = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId && e.SmelterId != null)
                .Select(e => e.SmelterId!)
                .Distinct()
                .ToListAsync(ct);

            var smelters = await db.RmapSmelters.AsNoTracking()
                .Where(s => smelterIds.Contains(s.SmelterId))
                .Select(s => new SmelterStatus(s.SmelterId, s.SmelterName, s.ConformanceStatus, s.LastAuditDate))
                .ToListAsync(ct);

            var oecdVersion = checks
                .Where(c => c.Framework == "OECD_DDG")
                .Select(c => c.RuleVersion)
                .FirstOrDefault() ?? "1.0.0-pilot";

            var summary = $"Due diligence assessment for batch {batch.BatchNumber}. " +
                $"{checks.Count} compliance checks performed across {checks.Select(c => c.Framework).Distinct().Count()} frameworks. " +
                $"{riskFlags.Count} risk flag(s) identified. " +
                $"{smelters.Count} smelter(s) in chain, OECD DDG version: {oecdVersion}.";

            return Result<Response>.Success(new Response(riskFlags, smelters, oecdVersion, summary));
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git commit -m "feat: due diligence summary generator aggregating OECD DDG flags"
```

---

### Task 9: Risk Assessment Generator

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/GenerateRiskAssessment.cs`

- [ ] **Step 1: Create the handler**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class GenerateRiskAssessment
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record RiskCategory(string Category, string Rating, string Detail);

    public record Response(
        string OverallRating,
        IReadOnlyList<RiskCategory> Categories,
        string SummaryText);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId && b.TenantId == tenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var categories = new List<RiskCategory>();

            // 1. Source country risk
            var highRiskCountries = await db.RiskCountries.AsNoTracking()
                .Where(r => r.RiskLevel == "HIGH")
                .Select(r => r.CountryCode)
                .ToListAsync(ct);

            var countryRisk = highRiskCountries.Contains(batch.OriginCountry)
                ? new RiskCategory("Source Country", "HIGH", $"{batch.OriginCountry} is a high-risk origin (CAHRA)")
                : new RiskCategory("Source Country", "LOW", $"{batch.OriginCountry} is not a high-risk origin");
            categories.Add(countryRisk);

            // 2. Smelter conformance
            var smelterIds = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId && e.SmelterId != null)
                .Select(e => e.SmelterId!).Distinct().ToListAsync(ct);

            if (smelterIds.Count == 0)
            {
                categories.Add(new RiskCategory("Smelter Conformance", "HIGH", "No smelter identified in supply chain"));
            }
            else
            {
                var smelters = await db.RmapSmelters.AsNoTracking()
                    .Where(s => smelterIds.Contains(s.SmelterId)).ToListAsync(ct);
                var allConformant = smelters.All(s => s.ConformanceStatus is "CONFORMANT" or "ACTIVE_PARTICIPATING");
                categories.Add(allConformant
                    ? new RiskCategory("Smelter Conformance", "LOW", "All smelters RMAP conformant")
                    : new RiskCategory("Smelter Conformance", "HIGH", "One or more smelters not RMAP conformant"));
            }

            // 3. Chain of custody completeness
            var eventCount = await db.CustodyEvents.AsNoTracking()
                .CountAsync(e => e.BatchId == query.BatchId, ct);
            var chainRisk = eventCount >= 4
                ? new RiskCategory("Chain Completeness", "LOW", $"{eventCount} events — chain substantially complete")
                : new RiskCategory("Chain Completeness", eventCount >= 2 ? "MEDIUM" : "HIGH",
                    $"Only {eventCount} event(s) — chain incomplete");
            categories.Add(chainRisk);

            // 4. OECD DDG flags
            var flagCount = await db.ComplianceChecks.AsNoTracking()
                .CountAsync(c => c.BatchId == query.BatchId && (c.Status == "FLAG" || c.Status == "FAIL"), ct);
            categories.Add(flagCount == 0
                ? new RiskCategory("OECD DDG Flags", "LOW", "No compliance flags")
                : new RiskCategory("OECD DDG Flags", flagCount > 2 ? "HIGH" : "MEDIUM",
                    $"{flagCount} compliance flag(s) raised"));

            // Overall rating: worst category wins
            var overall = categories.Any(c => c.Rating == "HIGH") ? "HIGH"
                : categories.Any(c => c.Rating == "MEDIUM") ? "MEDIUM" : "LOW";

            var summary = $"Risk assessment for batch {batch.BatchNumber}: Overall {overall}. " +
                string.Join("; ", categories.Select(c => $"{c.Category}: {c.Rating}"));

            return Result<Response>.Success(new Response(overall, categories, summary));
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git commit -m "feat: risk assessment generator with 4-category evaluation"
```

---

## Chunk 4: Filing Cycles + API Endpoints

### Task 10: Filing Cycle Management

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/ListFilingCycles.cs`
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/UpdateFilingCycleStatus.cs`
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/FormSdFilingCycleService.cs`

- [ ] **Step 1: Create ListFilingCycles query**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class ListFilingCycles
{
    public record Query : IRequest<Result<Response>>;

    public record CycleItem(Guid Id, int ReportingYear, DateTime DueDate, string Status, DateTime? SubmittedAt, string? Notes);

    public record Response(IReadOnlyList<CycleItem> Cycles);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var cycles = await db.FormSdFilingCycles.AsNoTracking()
                .Where(c => c.TenantId == tenantId)
                .OrderByDescending(c => c.ReportingYear)
                .Select(c => new CycleItem(c.Id, c.ReportingYear, c.DueDate, c.Status, c.SubmittedAt, c.Notes))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(cycles));
        }
    }
}
```

- [ ] **Step 2: Create UpdateFilingCycleStatus command**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class UpdateFilingCycleStatus
{
    public record Command(Guid CycleId, string Status, string? Notes = null) : IRequest<Result<Response>>;

    public record Response(Guid Id, string Status);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Command, Result<Response>>
    {
        private static readonly string[] ValidStatuses = ["NOT_STARTED", "IN_PROGRESS", "PACKAGE_READY", "FILED"];

        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            if (!ValidStatuses.Contains(cmd.Status))
                return Result<Response>.Failure($"Invalid status. Valid: {string.Join(", ", ValidStatuses)}");

            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var cycle = await db.FormSdFilingCycles
                .FirstOrDefaultAsync(c => c.Id == cmd.CycleId && c.TenantId == tenantId, ct);
            if (cycle is null)
                return Result<Response>.Failure("Filing cycle not found");

            cycle.Status = cmd.Status;
            cycle.UpdatedAt = DateTime.UtcNow;
            if (cmd.Notes is not null) cycle.Notes = cmd.Notes;
            if (cmd.Status == "FILED") cycle.SubmittedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(cycle.Id, cycle.Status));
        }
    }
}
```

- [ ] **Step 3: Create background service for overdue detection + auto-create**

```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.FormSd;

/// <summary>
/// Daily sweep: flags overdue filing cycles and auto-creates next year's cycle on Jan 1.
/// </summary>
public class FormSdFilingCycleService(IServiceProvider services, ILogger<FormSdFilingCycleService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Flag overdue cycles
                var now = DateTime.UtcNow;
                var overdue = await db.FormSdFilingCycles
                    .Where(c => c.DueDate < now && c.Status != "FILED" && c.Status != "OVERDUE")
                    .ToListAsync(stoppingToken);

                foreach (var cycle in overdue)
                {
                    cycle.Status = "OVERDUE";
                    cycle.UpdatedAt = now;
                    logger.LogWarning("Form SD filing cycle {Year} for tenant {TenantId} is OVERDUE",
                        cycle.ReportingYear, cycle.TenantId);
                }

                // Auto-create next year's cycle for tenants that have Form SD enabled
                var currentYear = now.Year;
                var tenantsWithCycles = await db.FormSdFilingCycles
                    .Select(c => c.TenantId).Distinct().ToListAsync(stoppingToken);

                foreach (var tenantId in tenantsWithCycles)
                {
                    var hasCurrentYear = await db.FormSdFilingCycles
                        .AnyAsync(c => c.TenantId == tenantId && c.ReportingYear == currentYear, stoppingToken);

                    if (!hasCurrentYear)
                    {
                        db.FormSdFilingCycles.Add(new FormSdFilingCycleEntity
                        {
                            Id = Guid.NewGuid(),
                            TenantId = tenantId,
                            ReportingYear = currentYear,
                            DueDate = new DateTime(currentYear, 6, 30, 23, 59, 59, DateTimeKind.Utc), // Form SD due June 30
                            Status = "NOT_STARTED",
                            CreatedAt = now,
                            UpdatedAt = now,
                        });
                        logger.LogInformation("Created Form SD filing cycle for {Year}, tenant {TenantId}", currentYear, tenantId);
                    }
                }

                if (overdue.Count > 0 || tenantsWithCycles.Count > 0)
                    await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Form SD filing cycle sweep failed");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

- [ ] **Step 4: Commit**

```bash
git commit -m "feat: filing cycle management — list, update status, background overdue sweep"
```

---

### Task 11: API Endpoints + Program.cs Registration

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/FormSdEndpoints.cs`
- Create: `packages/api/src/Tungsten.Api/Features/FormSd/GetBatchFormSdStatus.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Create GetBatchFormSdStatus query**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class GetBatchFormSdStatus
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record Response(string ApplicabilityStatus, string? RuleSetVersion, string? Reasoning, DateTime? AssessedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var assessment = await db.FormSdAssessments.AsNoTracking()
                .Where(a => a.BatchId == query.BatchId && a.TenantId == tenantId && a.SupersedesId == null)
                .OrderByDescending(a => a.AssessedAt)
                .FirstOrDefaultAsync(ct);

            if (assessment is null)
                return Result<Response>.Success(new Response("NOT_ASSESSED", null, null, null));

            return Result<Response>.Success(new Response(
                assessment.ApplicabilityStatus,
                assessment.RuleSetVersion,
                assessment.Reasoning,
                assessment.AssessedAt));
        }
    }
}
```

- [ ] **Step 2: Create FormSdEndpoints**

```csharp
using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.FormSd;

public static class FormSdEndpoints
{
    public static IEndpointRouteBuilder MapFormSdEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/form-sd").RequireAuthorization();

        // Batch-level Form SD status
        group.MapGet("/batches/{batchId:guid}/status", async (Guid batchId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBatchFormSdStatus.Query(batchId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        // Supply chain description
        group.MapGet("/batches/{batchId:guid}/supply-chain", async (Guid batchId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GenerateSupplyChainDescription.Query(batchId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        // Due diligence summary
        group.MapGet("/batches/{batchId:guid}/due-diligence", async (Guid batchId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GenerateDueDiligenceSummary.Query(batchId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        // Risk assessment
        group.MapGet("/batches/{batchId:guid}/risk-assessment", async (Guid batchId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GenerateRiskAssessment.Query(batchId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        // Filing cycles
        group.MapGet("/filing-cycles", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListFilingCycles.Query(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        group.MapPatch("/filing-cycles/{cycleId:guid}", async (Guid cycleId, UpdateFilingCycleStatus.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = command with { CycleId = cycleId };
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        return app;
    }
}
```

- [ ] **Step 3: Register in Program.cs**

Add after `app.MapAiEndpoints();`:
```csharp
app.MapFormSdEndpoints();
```

Add in services section:
```csharp
builder.Services.AddHostedService<FormSdFilingCycleService>();
```

Add using:
```csharp
using Tungsten.Api.Features.FormSd;
```

- [ ] **Step 4: Build and run all tests**

Run: `cd packages/api && dotnet build && dotnet test --filter "FullyQualifiedName~FormSd|FullyQualifiedName~Batches|FullyQualifiedName~Compliance"`

- [ ] **Step 5: Generate EF migration**

Run: `cd packages/api/src/Tungsten.Api && dotnet ef migrations add AddFormSdTables --context AppDbContext`

- [ ] **Step 6: Commit**

```bash
git add packages/api/
git commit -m "feat: Form SD API endpoints — status, supply chain, due diligence, risk, filing cycles"
```

---

## Summary

| Feature | Implementation | Endpoint |
|---------|---------------|----------|
| 3TG Applicability | `ApplicabilityEngine` + `FormSdApplicabilityChecker` (auto on event) | `GET /api/form-sd/batches/{id}/status` |
| Supply Chain Description | `GenerateSupplyChainDescription` (on-demand) | `GET /api/form-sd/batches/{id}/supply-chain` |
| Due Diligence Summary | `GenerateDueDiligenceSummary` (on-demand) | `GET /api/form-sd/batches/{id}/due-diligence` |
| Risk Assessment | `GenerateRiskAssessment` (on-demand) | `GET /api/form-sd/batches/{id}/risk-assessment` |
| Filing Cycles | `ListFilingCycles` + `UpdateFilingCycleStatus` | `GET/PATCH /api/form-sd/filing-cycles` |
| Background Service | `FormSdFilingCycleService` — daily overdue sweep + auto-create | N/A (background) |

**Next plans:**
- **Plan B:** Form SD Support Package PDF generation (Feature 5)
- **Plan C:** Frontend — buyer dashboard, batch status column, supplier prompts (Features 7-9)
