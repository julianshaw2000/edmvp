# Phase D: CMRT v6.x Import — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Enable buyers to upload CMRT v6.x Excel workbooks, preview parsed smelter data matched against RMAP records, and confirm imports to create tenant-smelter associations and an audit trail.

**Architecture:** ClosedXML parses the CMRT workbook extracting declaration, smelter list, and product data. A two-phase MediatR handler provides preview (dry run) then confirm (persist). New entities track import history and tenant-smelter associations. Angular frontend provides upload dropzone, preview table, and import history.

**Tech Stack:** ClosedXML (.NET Excel parsing), EF Core migration, MediatR CQRS, Angular 21+ standalone components

---

## File Structure

### Backend — New files
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/CmrtImportEntity.cs`
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/TenantSmelterAssociationEntity.cs`
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/CmrtImportConfiguration.cs`
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/TenantSmelterAssociationConfiguration.cs`
- `packages/api/src/Tungsten.Api/Common/Services/CmrtParserService.cs`
- `packages/api/src/Tungsten.Api/Features/Buyer/ImportCmrt.cs`
- `packages/api/src/Tungsten.Api/Features/Buyer/ListCmrtImports.cs`

### Backend — Modified files
- `packages/api/src/Tungsten.Api/Tungsten.Api.csproj` — Add ClosedXML
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/AppDbContext.cs` — Add DbSets
- `packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs` — Register endpoints

### Frontend — New files
- `packages/web/src/app/features/buyer/cmrt-import.component.ts`

### Frontend — Modified files
- `packages/web/src/app/features/buyer/buyer.routes.ts` — Add route
- `packages/web/src/app/features/buyer/data/buyer-api.service.ts` — Add API methods
- `packages/web/src/app/core/layout/sidebar.component.ts` — Add sidebar link

---

## Chunk 1: Database Entities + Migration

### Task 1: Add ClosedXML dependency and create entities

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Tungsten.Api.csproj`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/CmrtImportEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/TenantSmelterAssociationEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/CmrtImportConfiguration.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/TenantSmelterAssociationConfiguration.cs`
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/AppDbContext.cs`

- [ ] **Step 1: Add ClosedXML NuGet package**

Run:
```bash
cd packages/api/src/Tungsten.Api && dotnet add package ClosedXML
```

- [ ] **Step 2: Create CmrtImportEntity**

Create `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/CmrtImportEntity.cs`:

```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class CmrtImportEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string FileName { get; set; }
    public required string DeclarationCompany { get; set; }
    public int? ReportingYear { get; set; }
    public int RowsParsed { get; set; }
    public int RowsMatched { get; set; }
    public int RowsUnmatched { get; set; }
    public int Errors { get; set; }
    public Guid ImportedBy { get; set; }
    public DateTime ImportedAt { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity Importer { get; set; } = null!;
}
```

- [ ] **Step 3: Create TenantSmelterAssociationEntity**

Create `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/TenantSmelterAssociationEntity.cs`:

```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class TenantSmelterAssociationEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string SmelterId { get; set; }
    public required string Source { get; set; }
    public Guid? CmrtImportId { get; set; }
    public required string Status { get; set; }
    public required string MetalType { get; set; }
    public DateTime CreatedAt { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
    public RmapSmelterEntity? Smelter { get; set; }
    public CmrtImportEntity? CmrtImport { get; set; }
}
```

- [ ] **Step 4: Create CmrtImportConfiguration**

Create `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/CmrtImportConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class CmrtImportConfiguration : IEntityTypeConfiguration<CmrtImportEntity>
{
    public void Configure(EntityTypeBuilder<CmrtImportEntity> builder)
    {
        builder.ToTable("cmrt_imports");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DeclarationCompany).IsRequired().HasMaxLength(300);
        builder.Property(e => e.ImportedAt).HasDefaultValueSql("now()");
        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Importer).WithMany().HasForeignKey(e => e.ImportedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
```

- [ ] **Step 5: Create TenantSmelterAssociationConfiguration**

Create `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/TenantSmelterAssociationConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class TenantSmelterAssociationConfiguration : IEntityTypeConfiguration<TenantSmelterAssociationEntity>
{
    public void Configure(EntityTypeBuilder<TenantSmelterAssociationEntity> builder)
    {
        builder.ToTable("tenant_smelter_associations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SmelterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Source).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.MetalType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(e => new { e.TenantId, e.SmelterId }).IsUnique();
        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Smelter).WithMany().HasForeignKey(e => e.SmelterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.CmrtImport).WithMany().HasForeignKey(e => e.CmrtImportId).OnDelete(DeleteBehavior.SetNull);
    }
}
```

- [ ] **Step 6: Register DbSets in AppDbContext**

In `packages/api/src/Tungsten.Api/Infrastructure/Persistence/AppDbContext.cs`, add with the other DbSets:

```csharp
public DbSet<CmrtImportEntity> CmrtImports => Set<CmrtImportEntity>();
public DbSet<TenantSmelterAssociationEntity> TenantSmelterAssociations => Set<TenantSmelterAssociationEntity>();
```

- [ ] **Step 7: Create migration**

```bash
cd packages/api/src/Tungsten.Api && dotnet ef migrations add AddCmrtImportTables --context AppDbContext
```

- [ ] **Step 8: Build**

Run: `cd packages/api && dotnet build`

- [ ] **Step 9: Commit**

```bash
git add packages/api/src/Tungsten.Api/Tungsten.Api.csproj packages/api/src/Tungsten.Api/Infrastructure/Persistence/ packages/api/src/Tungsten.Api/Migrations/
git commit -m "feat: add CmrtImport and TenantSmelterAssociation entities with migration (GAP-1)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 2: CMRT Parser Service + Import Handler

### Task 2: Create CmrtParserService

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Services/CmrtParserService.cs`

- [ ] **Step 1: Create the parser**

```csharp
using ClosedXML.Excel;

namespace Tungsten.Api.Common.Services;

public record CmrtDeclaration(string CompanyName, int? ReportingYear, string? DeclarationScope);

public record CmrtSmelterRow(
    string MetalType,
    string? SmelterName,
    string? SmelterId,
    string? Country,
    string? SourceCountry,
    string? SourcingStatus,
    int RowNumber);

public record CmrtParseResult(
    CmrtDeclaration Declaration,
    List<CmrtSmelterRow> Smelters,
    List<string> Errors);

public static class CmrtParserService
{
    public static CmrtParseResult Parse(Stream fileStream)
    {
        var errors = new List<string>();
        var smelters = new List<CmrtSmelterRow>();
        string companyName = "Unknown";
        int? reportingYear = null;
        string? declarationScope = null;

        using var workbook = new XLWorkbook(fileStream);

        // Parse Declaration tab
        var declSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Contains("Declaration", StringComparison.OrdinalIgnoreCase));

        if (declSheet is not null)
        {
            // CMRT v6.x: Company name typically at B8, reporting year at B18
            companyName = declSheet.Cell("B8").GetString()?.Trim() ?? "Unknown";
            if (string.IsNullOrWhiteSpace(companyName)) companyName = "Unknown";

            var yearCell = declSheet.Cell("B18").GetString()?.Trim();
            if (int.TryParse(yearCell, out var year)) reportingYear = year;

            var scopeCell = declSheet.Cell("B14").GetString()?.Trim();
            if (!string.IsNullOrWhiteSpace(scopeCell)) declarationScope = scopeCell;
        }
        else
        {
            errors.Add("Declaration worksheet not found");
        }

        // Parse Smelter List tab
        var smelterSheet = workbook.Worksheets.FirstOrDefault(ws =>
            ws.Name.Contains("Smelter", StringComparison.OrdinalIgnoreCase)
            && ws.Name.Contains("List", StringComparison.OrdinalIgnoreCase));

        if (smelterSheet is not null)
        {
            // CMRT v6.x smelter list: data starts at row 4
            // Column A: Metal, B: Smelter Name, C: Smelter ID, D: Country, E: Source Country, F: Status
            var lastRow = smelterSheet.LastRowUsed()?.RowNumber() ?? 3;

            for (var row = 4; row <= lastRow; row++)
            {
                var metalType = smelterSheet.Cell(row, 1).GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(metalType)) continue;

                var smelterName = smelterSheet.Cell(row, 2).GetString()?.Trim();
                var smelterId = smelterSheet.Cell(row, 3).GetString()?.Trim();
                var country = smelterSheet.Cell(row, 4).GetString()?.Trim();
                var sourceCountry = smelterSheet.Cell(row, 5).GetString()?.Trim();
                var sourcingStatus = smelterSheet.Cell(row, 6).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(smelterName) && string.IsNullOrWhiteSpace(smelterId))
                {
                    errors.Add($"Row {row}: Missing both smelter name and ID");
                    continue;
                }

                smelters.Add(new CmrtSmelterRow(
                    metalType, smelterName, smelterId, country,
                    sourceCountry, sourcingStatus, row));
            }
        }
        else
        {
            errors.Add("Smelter List worksheet not found");
        }

        return new CmrtParseResult(
            new CmrtDeclaration(companyName, reportingYear, declarationScope),
            smelters,
            errors);
    }
}
```

- [ ] **Step 2: Build**

Run: `cd packages/api && dotnet build`

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Services/CmrtParserService.cs
git commit -m "feat: add CmrtParserService for CMRT v6.x Excel parsing (GAP-1)

Parses Declaration tab (company, year, scope) and Smelter List tab
(metal, name, ID, country, sourcing status) using ClosedXML.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Create ImportCmrt handler and ListCmrtImports query

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Buyer/ImportCmrt.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Buyer/ListCmrtImports.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs`

- [ ] **Step 1: Create ImportCmrt handler**

Create `packages/api/src/Tungsten.Api/Features/Buyer/ImportCmrt.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Buyer;

public static class ImportCmrt
{
    public record PreviewCommand(Stream FileStream, string FileName) : IRequest<Result<PreviewResponse>>;

    public record ConfirmCommand(
        string FileName,
        string DeclarationCompany,
        int? ReportingYear,
        List<SmelterMatchItem> Smelters) : IRequest<Result<ConfirmResponse>>;

    public record SmelterMatchItem(
        string MetalType,
        string? SmelterName,
        string? SmelterId,
        string? Country,
        string MatchStatus,
        string? MatchedSmelterId);

    public record PreviewResponse(
        string DeclarationCompany,
        int? ReportingYear,
        string? DeclarationScope,
        int TotalSmelters,
        int Matched,
        int Unmatched,
        int ErrorCount,
        List<SmelterPreviewItem> Smelters,
        List<string> Errors);

    public record SmelterPreviewItem(
        string MetalType,
        string? SmelterName,
        string? SmelterId,
        string? Country,
        string MatchStatus,
        string? MatchedSmelterId,
        string? MatchedSmelterName,
        string? ConformanceStatus,
        int RowNumber);

    public record ConfirmResponse(Guid ImportId, int Created, int Skipped);

    public class PreviewHandler(AppDbContext db)
        : IRequestHandler<PreviewCommand, Result<PreviewResponse>>
    {
        public async Task<Result<PreviewResponse>> Handle(PreviewCommand request, CancellationToken ct)
        {
            CmrtParseResult parseResult;
            try
            {
                parseResult = CmrtParserService.Parse(request.FileStream);
            }
            catch (Exception ex)
            {
                return Result<PreviewResponse>.Failure($"Failed to parse CMRT file: {ex.Message}");
            }

            var allRmapSmelters = await db.RmapSmelters.AsNoTracking().ToListAsync(ct);

            var previewItems = new List<SmelterPreviewItem>();
            var matched = 0;
            var unmatched = 0;

            foreach (var row in parseResult.Smelters)
            {
                RmapSmelterEntity? match = null;

                // Primary match: by SmelterId (RMAP CID)
                if (!string.IsNullOrWhiteSpace(row.SmelterId))
                    match = allRmapSmelters.FirstOrDefault(s =>
                        s.SmelterId.Equals(row.SmelterId, StringComparison.OrdinalIgnoreCase));

                // Fallback: by name + country
                if (match is null && !string.IsNullOrWhiteSpace(row.SmelterName) && !string.IsNullOrWhiteSpace(row.Country))
                    match = allRmapSmelters.FirstOrDefault(s =>
                        s.SmelterName.Equals(row.SmelterName, StringComparison.OrdinalIgnoreCase)
                        && s.Country.Equals(row.Country, StringComparison.OrdinalIgnoreCase));

                if (match is not null)
                {
                    matched++;
                    previewItems.Add(new SmelterPreviewItem(
                        row.MetalType, row.SmelterName, row.SmelterId, row.Country,
                        "matched", match.SmelterId, match.SmelterName, match.ConformanceStatus, row.RowNumber));
                }
                else
                {
                    unmatched++;
                    previewItems.Add(new SmelterPreviewItem(
                        row.MetalType, row.SmelterName, row.SmelterId, row.Country,
                        "unmatched", null, null, null, row.RowNumber));
                }
            }

            return Result<PreviewResponse>.Success(new PreviewResponse(
                parseResult.Declaration.CompanyName,
                parseResult.Declaration.ReportingYear,
                parseResult.Declaration.DeclarationScope,
                parseResult.Smelters.Count,
                matched,
                unmatched,
                parseResult.Errors.Count,
                previewItems,
                parseResult.Errors));
        }
    }

    public class ConfirmHandler(AppDbContext db, ICurrentUserService currentUser, ILogger<ConfirmHandler> logger)
        : IRequestHandler<ConfirmCommand, Result<ConfirmResponse>>
    {
        public async Task<Result<ConfirmResponse>> Handle(ConfirmCommand request, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var userId = await currentUser.GetUserIdAsync(ct);

            var import = new CmrtImportEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FileName = request.FileName,
                DeclarationCompany = request.DeclarationCompany,
                ReportingYear = request.ReportingYear,
                RowsParsed = request.Smelters.Count,
                RowsMatched = request.Smelters.Count(s => s.MatchStatus == "matched"),
                RowsUnmatched = request.Smelters.Count(s => s.MatchStatus == "unmatched"),
                Errors = 0,
                ImportedBy = userId,
                ImportedAt = DateTime.UtcNow,
            };
            db.CmrtImports.Add(import);

            var created = 0;
            var skipped = 0;

            foreach (var smelter in request.Smelters.Where(s => s.MatchedSmelterId is not null))
            {
                var exists = await db.TenantSmelterAssociations.AnyAsync(a =>
                    a.TenantId == tenantId && a.SmelterId == smelter.MatchedSmelterId, ct);

                if (exists)
                {
                    skipped++;
                    continue;
                }

                db.TenantSmelterAssociations.Add(new TenantSmelterAssociationEntity
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    SmelterId = smelter.MatchedSmelterId!,
                    Source = "CMRT_IMPORT",
                    CmrtImportId = import.Id,
                    Status = "verified",
                    MetalType = smelter.MetalType,
                    CreatedAt = DateTime.UtcNow,
                });
                created++;
            }

            // Save unmatched as "unverified" associations with the CMRT-provided ID
            foreach (var smelter in request.Smelters.Where(s =>
                s.MatchStatus == "unmatched" && !string.IsNullOrWhiteSpace(s.SmelterId)))
            {
                var exists = await db.TenantSmelterAssociations.AnyAsync(a =>
                    a.TenantId == tenantId && a.SmelterId == smelter.SmelterId!, ct);

                if (!exists)
                {
                    db.TenantSmelterAssociations.Add(new TenantSmelterAssociationEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        SmelterId = smelter.SmelterId!,
                        Source = "CMRT_IMPORT",
                        CmrtImportId = import.Id,
                        Status = "unverified",
                        MetalType = smelter.MetalType,
                        CreatedAt = DateTime.UtcNow,
                    });
                    created++;
                }
            }

            await db.SaveChangesAsync(ct);

            logger.LogInformation("CMRT import {ImportId}: {Created} associations created, {Skipped} skipped",
                import.Id, created, skipped);

            return Result<ConfirmResponse>.Success(new ConfirmResponse(import.Id, created, skipped));
        }
    }
}
```

- [ ] **Step 2: Create ListCmrtImports query**

Create `packages/api/src/Tungsten.Api/Features/Buyer/ListCmrtImports.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Buyer;

public static class ListCmrtImports
{
    public record Query : IRequest<Result<List<ImportItem>>>;

    public record ImportItem(
        Guid Id,
        string FileName,
        string DeclarationCompany,
        int? ReportingYear,
        int RowsParsed,
        int RowsMatched,
        int RowsUnmatched,
        string ImportedBy,
        DateTime ImportedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<List<ImportItem>>>
    {
        public async Task<Result<List<ImportItem>>> Handle(Query request, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var imports = await db.CmrtImports.AsNoTracking()
                .Where(i => i.TenantId == tenantId)
                .OrderByDescending(i => i.ImportedAt)
                .Select(i => new ImportItem(
                    i.Id,
                    i.FileName,
                    i.DeclarationCompany,
                    i.ReportingYear,
                    i.RowsParsed,
                    i.RowsMatched,
                    i.RowsUnmatched,
                    i.Importer.DisplayName,
                    i.ImportedAt))
                .ToListAsync(ct);

            return Result<List<ImportItem>>.Success(imports);
        }
    }
}
```

- [ ] **Step 3: Register endpoints**

In `packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs`, add after the existing endpoints:

```csharp
group.MapPost("/cmrt-import/preview", async (HttpRequest httpRequest, IMediator mediator, CancellationToken ct) =>
{
    var form = await httpRequest.ReadFormAsync(ct);
    var file = form.Files.FirstOrDefault();
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "No file uploaded" });

    if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { error = "Only .xlsx files are supported" });

    using var stream = file.OpenReadStream();
    var result = await mediator.Send(new ImportCmrt.PreviewCommand(stream, file.FileName), ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
}).DisableAntiforgery();

group.MapPost("/cmrt-import/confirm", async (ImportCmrt.ConfirmCommand command, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(command, ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
});

group.MapGet("/cmrt-imports", async (IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new ListCmrtImports.Query(), ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
});
```

- [ ] **Step 4: Build and test**

Run: `cd packages/api && dotnet build && dotnet test`

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Buyer/ImportCmrt.cs packages/api/src/Tungsten.Api/Features/Buyer/ListCmrtImports.cs packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs
git commit -m "feat: add CMRT import preview/confirm endpoints and import history (GAP-1)

Two-phase import: preview parses CMRT and matches smelters against RMAP
data, confirm persists associations and audit trail. List endpoint shows
import history.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 3: Frontend — CMRT Import Page

### Task 4: Create CMRT import component

**Files:**
- Create: `packages/web/src/app/features/buyer/cmrt-import.component.ts`

- [ ] **Step 1: Create the component**

```typescript
import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { API_URL } from '../../core/http/api-url.token';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

interface SmelterPreviewItem {
  metalType: string;
  smelterName: string | null;
  smelterId: string | null;
  country: string | null;
  matchStatus: 'matched' | 'unmatched';
  matchedSmelterId: string | null;
  matchedSmelterName: string | null;
  conformanceStatus: string | null;
  rowNumber: number;
}

interface PreviewResponse {
  declarationCompany: string;
  reportingYear: number | null;
  declarationScope: string | null;
  totalSmelters: number;
  matched: number;
  unmatched: number;
  errorCount: number;
  smelters: SmelterPreviewItem[];
  errors: string[];
}

interface ConfirmResponse {
  importId: string;
  created: number;
  skipped: number;
}

interface ImportHistoryItem {
  id: string;
  fileName: string;
  declarationCompany: string;
  reportingYear: number | null;
  rowsParsed: number;
  rowsMatched: number;
  rowsUnmatched: number;
  importedBy: string;
  importedAt: string;
}

@Component({
  selector: 'app-cmrt-import',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, DatePipe, PageHeaderComponent],
  template: `
    <a routerLink="/buyer" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>
    <app-page-header title="CMRT Import" subtitle="Import smelter data from a Conflict Minerals Reporting Template" />

    <div class="max-w-4xl">
      <!-- Upload Section (shown when no preview) -->
      @if (!preview()) {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-8 mb-6">
          <div class="border-2 border-dashed border-slate-300 rounded-xl p-10 text-center hover:border-indigo-400 transition-colors cursor-pointer"
            (click)="fileInput.click()"
            (dragover)="$event.preventDefault(); $event.stopPropagation()"
            (drop)="onFileDrop($event)">
            <svg class="w-12 h-12 text-slate-300 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
            </svg>
            <p class="text-sm font-medium text-slate-700 mb-1">Drop your CMRT file here or click to browse</p>
            <p class="text-xs text-slate-400">Accepts .xlsx files (CMRT v6.x format)</p>
          </div>
          <input #fileInput type="file" accept=".xlsx" class="hidden" (change)="onFileSelected($event)" />

          @if (uploading()) {
            <div class="mt-4 flex items-center gap-2 text-sm text-indigo-600">
              <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              Parsing CMRT file...
            </div>
          }
          @if (error()) {
            <p class="mt-4 text-sm text-rose-600">{{ error() }}</p>
          }
        </div>
      }

      <!-- Preview Section -->
      @if (preview(); as data) {
        <div class="space-y-6 mb-6">
          <!-- Declaration Summary -->
          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6">
            <h3 class="text-sm font-semibold text-slate-900 mb-3">Declaration Summary</h3>
            <div class="grid grid-cols-3 gap-4 text-sm">
              <div>
                <span class="text-slate-500">Company:</span>
                <span class="ml-1 font-medium text-slate-900">{{ data.declarationCompany }}</span>
              </div>
              <div>
                <span class="text-slate-500">Reporting Year:</span>
                <span class="ml-1 font-medium text-slate-900">{{ data.reportingYear ?? 'N/A' }}</span>
              </div>
              <div>
                <span class="text-slate-500">Scope:</span>
                <span class="ml-1 font-medium text-slate-900">{{ data.declarationScope ?? 'N/A' }}</span>
              </div>
            </div>
          </div>

          <!-- Match Summary -->
          <div class="grid grid-cols-3 gap-4">
            <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5 text-center">
              <p class="text-2xl font-bold text-slate-900">{{ data.totalSmelters }}</p>
              <p class="text-xs text-slate-500">Total Smelters</p>
            </div>
            <div class="bg-emerald-50 rounded-xl border border-emerald-200 shadow-sm p-5 text-center">
              <p class="text-2xl font-bold text-emerald-700">{{ data.matched }}</p>
              <p class="text-xs text-emerald-600">Matched in RMAP</p>
            </div>
            <div class="bg-amber-50 rounded-xl border border-amber-200 shadow-sm p-5 text-center">
              <p class="text-2xl font-bold text-amber-700">{{ data.unmatched }}</p>
              <p class="text-xs text-amber-600">Unmatched</p>
            </div>
          </div>

          <!-- Errors -->
          @if (data.errors.length > 0) {
            <div class="bg-rose-50 border border-rose-200 rounded-xl p-4">
              <h4 class="text-sm font-semibold text-rose-700 mb-2">Parsing Errors</h4>
              @for (err of data.errors; track err) {
                <p class="text-xs text-rose-600">{{ err }}</p>
              }
            </div>
          }

          <!-- Smelter Preview Table -->
          <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-slate-50 border-b border-slate-200">
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Row</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Metal</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Smelter</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">ID</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Country</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Match</th>
                </tr>
              </thead>
              <tbody>
                @for (s of data.smelters; track s.rowNumber) {
                  <tr class="border-b border-slate-100"
                    [class]="s.matchStatus === 'matched' ? '' : 'bg-amber-50/50'">
                    <td class="px-4 py-2.5 text-slate-400">{{ s.rowNumber }}</td>
                    <td class="px-4 py-2.5 text-slate-700">{{ s.metalType }}</td>
                    <td class="px-4 py-2.5 text-slate-900 font-medium">{{ s.smelterName ?? '—' }}</td>
                    <td class="px-4 py-2.5 text-slate-500 font-mono text-xs">{{ s.smelterId ?? '—' }}</td>
                    <td class="px-4 py-2.5 text-slate-500">{{ s.country ?? '—' }}</td>
                    <td class="px-4 py-2.5">
                      @if (s.matchStatus === 'matched') {
                        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-emerald-50 text-emerald-700">
                          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                          </svg>
                          {{ s.conformanceStatus }}
                        </span>
                      } @else {
                        <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-amber-50 text-amber-700">
                          Unmatched
                        </span>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-3">
            <button (click)="confirmImport()"
              [disabled]="confirming()"
              class="bg-indigo-600 text-white py-2.5 px-6 rounded-xl text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 shadow-sm transition-all">
              {{ confirming() ? 'Importing...' : 'Confirm Import' }}
            </button>
            <button (click)="cancelPreview()"
              class="text-sm font-medium text-slate-500 hover:text-slate-700 px-4 py-2.5 rounded-xl hover:bg-slate-100 transition-all">
              Cancel
            </button>
          </div>

          @if (confirmResult()) {
            <div class="bg-emerald-50 border border-emerald-200 rounded-xl p-4 text-sm text-emerald-700">
              Import complete: {{ confirmResult()!.created }} associations created, {{ confirmResult()!.skipped }} skipped.
            </div>
          }
        </div>
      }

      <!-- Import History -->
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-200">
          <h3 class="text-sm font-semibold text-slate-900">Import History</h3>
        </div>
        @if (history().length > 0) {
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-slate-50 border-b border-slate-200">
                <th class="text-left px-4 py-3 font-semibold text-slate-600">File</th>
                <th class="text-left px-4 py-3 font-semibold text-slate-600">Company</th>
                <th class="text-center px-4 py-3 font-semibold text-slate-600">Matched</th>
                <th class="text-center px-4 py-3 font-semibold text-slate-600">Unmatched</th>
                <th class="text-left px-4 py-3 font-semibold text-slate-600">Imported</th>
              </tr>
            </thead>
            <tbody>
              @for (h of history(); track h.id) {
                <tr class="border-b border-slate-100 last:border-0">
                  <td class="px-4 py-3 font-medium text-slate-900">{{ h.fileName }}</td>
                  <td class="px-4 py-3 text-slate-500">{{ h.declarationCompany }}</td>
                  <td class="px-4 py-3 text-center text-emerald-600 font-medium">{{ h.rowsMatched }}</td>
                  <td class="px-4 py-3 text-center text-amber-600 font-medium">{{ h.rowsUnmatched }}</td>
                  <td class="px-4 py-3 text-slate-500">{{ h.importedAt | date:'medium' }}</td>
                </tr>
              }
            </tbody>
          </table>
        } @else {
          <div class="px-6 py-8 text-center text-slate-400 text-sm">No imports yet</div>
        }
      </div>
    </div>
  `,
})
export class CmrtImportComponent {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  uploading = signal(false);
  error = signal<string | null>(null);
  preview = signal<PreviewResponse | null>(null);
  confirming = signal(false);
  confirmResult = signal<ConfirmResponse | null>(null);
  history = signal<ImportHistoryItem[]>([]);

  private selectedFileName = '';

  constructor() {
    this.loadHistory();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) this.uploadFile(input.files[0]);
  }

  onFileDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    const file = event.dataTransfer?.files[0];
    if (file) this.uploadFile(file);
  }

  private uploadFile(file: File) {
    if (!file.name.endsWith('.xlsx')) {
      this.error.set('Only .xlsx files are supported');
      return;
    }
    this.uploading.set(true);
    this.error.set(null);
    this.selectedFileName = file.name;

    const formData = new FormData();
    formData.append('file', file);

    this.http.post<PreviewResponse>(
      `${this.apiUrl}/api/buyer/cmrt-import/preview`, formData
    ).subscribe({
      next: (res) => {
        this.preview.set(res);
        this.uploading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.error ?? 'Failed to parse CMRT file');
        this.uploading.set(false);
      },
    });
  }

  confirmImport() {
    const data = this.preview();
    if (!data) return;
    this.confirming.set(true);
    this.confirmResult.set(null);

    this.http.post<ConfirmResponse>(
      `${this.apiUrl}/api/buyer/cmrt-import/confirm`,
      {
        fileName: this.selectedFileName,
        declarationCompany: data.declarationCompany,
        reportingYear: data.reportingYear,
        smelters: data.smelters.map(s => ({
          metalType: s.metalType,
          smelterName: s.smelterName,
          smelterId: s.smelterId,
          country: s.country,
          matchStatus: s.matchStatus,
          matchedSmelterId: s.matchedSmelterId,
        })),
      }
    ).subscribe({
      next: (res) => {
        this.confirmResult.set(res);
        this.confirming.set(false);
        this.loadHistory();
      },
      error: (err) => {
        this.error.set(err.error?.error ?? 'Failed to confirm import');
        this.confirming.set(false);
      },
    });
  }

  cancelPreview() {
    this.preview.set(null);
    this.confirmResult.set(null);
    this.error.set(null);
  }

  private loadHistory() {
    this.http.get<ImportHistoryItem[]>(
      `${this.apiUrl}/api/buyer/cmrt-imports`
    ).subscribe({
      next: (res) => this.history.set(res),
    });
  }
}
```

- [ ] **Step 2: Build**

Run: `cd packages/web && npx ng build`

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/features/buyer/cmrt-import.component.ts
git commit -m "feat: create CMRT import page with upload, preview, and history (GAP-1)

Two-step flow: upload CMRT xlsx, preview matched/unmatched smelters,
confirm to create associations. Shows import history below.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Register route and sidebar link

**Files:**
- Modify: `packages/web/src/app/features/buyer/buyer.routes.ts`
- Modify: `packages/web/src/app/core/layout/sidebar.component.ts`

- [ ] **Step 1: Add route**

In `packages/web/src/app/features/buyer/buyer.routes.ts`, add a new route entry:

```typescript
{
  path: 'cmrt-import',
  loadComponent: () => import('./cmrt-import.component').then(m => m.CmrtImportComponent),
},
```

- [ ] **Step 2: Add sidebar link**

In `packages/web/src/app/core/layout/sidebar.component.ts`, find the BUYER case and add a new item after the Form SD entry:

```typescript
{ label: 'CMRT Import', route: '/buyer/cmrt-import', icon: '<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12"/></svg>' },
```

- [ ] **Step 3: Build and verify**

Run: `cd packages/web && npx ng build`

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/features/buyer/buyer.routes.ts packages/web/src/app/core/layout/sidebar.component.ts
git commit -m "feat: add CMRT Import route and sidebar link for buyers (GAP-1)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Full build, push, and verify

- [ ] **Step 1: Full build check**

Run: `cd packages/api && dotnet build && dotnet test`
Run: `cd packages/web && npx ng build`
Expected: Both pass

- [ ] **Step 2: Push**

```bash
git push origin main
```
