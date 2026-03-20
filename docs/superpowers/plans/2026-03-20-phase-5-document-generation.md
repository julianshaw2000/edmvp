# Phase 5: Document Generation — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement Material Passport and Audit Dossier PDF generation via QuestPDF, share link generation (30-day expiry), and public verification/shared document endpoints.

**Architecture:** QuestPDF generates PDFs server-side. Generated documents are stored via `IFileStorageService` and tracked in the `GeneratedDocuments` table. Share tokens are random URL-safe strings with expiry. Two public (unauthenticated) endpoints serve verification and shared document access.

**Tech Stack:** ASP.NET Core 10, QuestPDF, EF Core 10, xUnit, FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-20-tungsten-pilot-mvp-design.md` — Section 8, Section 6.1 (Document Generation + Public endpoints)

---

## File Structure

```
packages/api/src/Tungsten.Api/
  Features/
    DocumentGeneration/
      GeneratePassport.cs             ← POST /api/batches/{batchId}/passport
      GenerateDossier.cs              ← POST /api/batches/{batchId}/dossier
      GetGeneratedDocument.cs         ← GET /api/generated-documents/{id}
      ShareDocument.cs                ← POST /api/generated-documents/{id}/share
      Templates/
        PassportTemplate.cs           ← QuestPDF Material Passport layout
        DossierTemplate.cs            ← QuestPDF Audit Dossier layout
      DocumentGenerationEndpoints.cs
    Public/
      VerifyBatch.cs                  ← GET /api/verify/{batchId}
      GetSharedDocument.cs            ← GET /api/shared/{token}
      PublicEndpoints.cs
```

---

## Task 1: QuestPDF Setup and Passport Template

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/Templates/PassportTemplate.cs`
- Create: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/Templates/DossierTemplate.cs`

- [ ] **Step 1: Add QuestPDF NuGet package**

```bash
cd /c/__edMVP/packages/api/src/Tungsten.Api
dotnet add package QuestPDF
```

- [ ] **Step 2: Configure QuestPDF license in Program.cs**

Add near the top of Program.cs (after `var builder = ...`):
```csharp
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
```

Add using:
```csharp
using QuestPDF.Infrastructure;
```

- [ ] **Step 3: Create PassportTemplate**

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tungsten.Api.Features.DocumentGeneration.Templates;

public record PassportData(
    string BatchNumber,
    string TenantName,
    string MineralType,
    string OriginCountry,
    string OriginMine,
    decimal WeightKg,
    string Status,
    string ComplianceStatus,
    string VerificationUrl,
    IReadOnlyList<PassportEventData> Events,
    IReadOnlyList<PassportComplianceData> ComplianceChecks,
    IReadOnlyList<PassportDocumentData> Documents,
    bool HashChainIntact,
    DateTime GeneratedAt);

public record PassportEventData(
    string EventType, DateTime EventDate, string Location,
    string ActorName, bool IsCorrection, string Sha256Hash);

public record PassportComplianceData(
    string EventType, string Framework, string Status, DateTime CheckedAt);

public record PassportDocumentData(
    string FileName, string DocumentType, DateTime CreatedAt);

public class PassportTemplate(PassportData data) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(10));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text("MATERIAL PASSPORT").Bold().FontSize(18);
                    row.ConstantItem(150).AlignRight().Text(data.TenantName).FontSize(10);
                });
                col.Item().PaddingTop(5).Text($"Batch: {data.BatchNumber}").FontSize(12).Bold();
                col.Item().Text($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}").FontSize(8).FontColor(Colors.Grey.Medium);
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });

            page.Content().PaddingTop(15).Column(col =>
            {
                // Batch Summary
                col.Item().Text("Batch Summary").Bold().FontSize(12);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                    table.Cell().Text("Mineral Type:"); table.Cell().Text(data.MineralType);
                    table.Cell().Text("Origin Country:"); table.Cell().Text(data.OriginCountry);
                    table.Cell().Text("Origin Mine:"); table.Cell().Text(data.OriginMine);
                    table.Cell().Text("Weight (kg):"); table.Cell().Text(data.WeightKg.ToString("N2"));
                    table.Cell().Text("Status:"); table.Cell().Text(data.Status);
                    table.Cell().Text("Compliance:"); table.Cell().Text(data.ComplianceStatus);
                });

                // Custody Chain
                col.Item().PaddingTop(15).Text("Custody Chain").Bold().FontSize(12);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2);
                        c.RelativeColumn(2); c.RelativeColumn(1);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Event Type").Bold();
                        h.Cell().Text("Date").Bold();
                        h.Cell().Text("Location").Bold();
                        h.Cell().Text("Actor").Bold();
                        h.Cell().Text("Correction").Bold();
                    });
                    foreach (var evt in data.Events)
                    {
                        table.Cell().Text(evt.EventType);
                        table.Cell().Text(evt.EventDate.ToString("yyyy-MM-dd"));
                        table.Cell().Text(evt.Location);
                        table.Cell().Text(evt.ActorName);
                        table.Cell().Text(evt.IsCorrection ? "Yes" : "");
                    }
                });

                // Compliance Summary
                col.Item().PaddingTop(15).Text("Compliance Summary").Bold().FontSize(12);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(2);
                        c.RelativeColumn(1); c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Event Type").Bold();
                        h.Cell().Text("Framework").Bold();
                        h.Cell().Text("Status").Bold();
                        h.Cell().Text("Checked").Bold();
                    });
                    foreach (var check in data.ComplianceChecks)
                    {
                        table.Cell().Text(check.EventType);
                        table.Cell().Text(check.Framework);
                        table.Cell().Text(check.Status);
                        table.Cell().Text(check.CheckedAt.ToString("yyyy-MM-dd"));
                    }
                });

                // Document Registry
                col.Item().PaddingTop(15).Text("Document Registry").Bold().FontSize(12);
                col.Item().PaddingTop(5).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); c.RelativeColumn(2); c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("File Name").Bold();
                        h.Cell().Text("Type").Bold();
                        h.Cell().Text("Uploaded").Bold();
                    });
                    foreach (var doc in data.Documents)
                    {
                        table.Cell().Text(doc.FileName);
                        table.Cell().Text(doc.DocumentType);
                        table.Cell().Text(doc.CreatedAt.ToString("yyyy-MM-dd"));
                    }
                });

                // Tamper Evidence
                col.Item().PaddingTop(15).Text("Tamper Evidence").Bold().FontSize(12);
                col.Item().PaddingTop(5).Text(data.HashChainIntact
                    ? "Hash chain verification: INTACT - No tampering detected"
                    : "Hash chain verification: BROKEN - Potential tampering detected")
                    .FontColor(data.HashChainIntact ? Colors.Green.Darken2 : Colors.Red.Darken2);

                // Verification URL
                col.Item().PaddingTop(10).Text($"Verify online: {data.VerificationUrl}").FontSize(8);
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("Generated by Tungsten Supply Chain Compliance Platform | ");
                text.Span(data.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss UTC"));
            });
        });
    }
}
```

- [ ] **Step 4: Create DossierTemplate**

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Tungsten.Api.Features.DocumentGeneration.Templates;

public record DossierData(
    string BatchNumber,
    string TenantName,
    string MineralType,
    string OriginCountry,
    string OriginMine,
    decimal WeightKg,
    string Status,
    string ComplianceStatus,
    IReadOnlyList<DossierEventData> Events,
    IReadOnlyList<DossierComplianceData> ComplianceChecks,
    IReadOnlyList<DossierDocumentData> Documents,
    DateTime GeneratedAt);

public record DossierEventData(
    string EventType, DateTime EventDate, string Location,
    string ActorName, string? SmelterId, string Description,
    bool IsCorrection, Guid? CorrectsEventId,
    string Sha256Hash, string? PreviousEventHash);

public record DossierComplianceData(
    string EventType, string Framework, string Status,
    string Details, DateTime CheckedAt);

public record DossierDocumentData(
    string FileName, string DocumentType, long FileSizeBytes,
    string UploadedBy, DateTime CreatedAt);

public class DossierTemplate(DossierData data) : IDocument
{
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Text("AUDIT DOSSIER").Bold().FontSize(18);
                col.Item().PaddingTop(5).Text($"Batch: {data.BatchNumber} | {data.TenantName}").FontSize(12).Bold();
                col.Item().Text($"Generated: {data.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}").FontSize(8).FontColor(Colors.Grey.Medium);
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            });

            page.Content().PaddingTop(10).Column(col =>
            {
                // Batch Summary
                col.Item().Text("1. Batch Summary").Bold().FontSize(11);
                col.Item().PaddingTop(3).Text($"Mineral: {data.MineralType} | Origin: {data.OriginCountry}, {data.OriginMine} | Weight: {data.WeightKg:N2} kg");
                col.Item().Text($"Status: {data.Status} | Compliance: {data.ComplianceStatus}");

                // Full Event Log
                col.Item().PaddingTop(12).Text("2. Full Event Log").Bold().FontSize(11);
                foreach (var evt in data.Events)
                {
                    col.Item().PaddingTop(5).Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(evtCol =>
                    {
                        evtCol.Item().Text($"{evt.EventType} — {evt.EventDate:yyyy-MM-dd HH:mm}").Bold();
                        evtCol.Item().Text($"Location: {evt.Location} | Actor: {evt.ActorName}");
                        if (evt.SmelterId is not null)
                            evtCol.Item().Text($"Smelter ID: {evt.SmelterId}");
                        evtCol.Item().Text($"Description: {evt.Description}");
                        if (evt.IsCorrection)
                            evtCol.Item().Text($"CORRECTION of event {evt.CorrectsEventId}").FontColor(Colors.Orange.Darken2);
                        evtCol.Item().Text($"Hash: {evt.Sha256Hash}").FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                }

                // Compliance Details
                col.Item().PaddingTop(12).Text("3. Compliance Check Details").Bold().FontSize(11);
                col.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2); c.RelativeColumn(1);
                        c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("Event").Bold();
                        h.Cell().Text("Framework").Bold();
                        h.Cell().Text("Status").Bold();
                        h.Cell().Text("Details").Bold();
                        h.Cell().Text("Checked").Bold();
                    });
                    foreach (var check in data.ComplianceChecks)
                    {
                        table.Cell().Text(check.EventType);
                        table.Cell().Text(check.Framework);
                        var statusColor = check.Status switch
                        {
                            "FAIL" => Colors.Red.Darken2,
                            "FLAG" => Colors.Orange.Darken2,
                            _ => Colors.Black
                        };
                        table.Cell().Text(check.Status).FontColor(statusColor);
                        table.Cell().Text(check.Details).FontSize(8);
                        table.Cell().Text(check.CheckedAt.ToString("yyyy-MM-dd"));
                    }
                });

                // Document List
                col.Item().PaddingTop(12).Text("4. Document List").Bold().FontSize(11);
                col.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3); c.RelativeColumn(2);
                        c.RelativeColumn(1); c.RelativeColumn(2); c.RelativeColumn(2);
                    });
                    table.Header(h =>
                    {
                        h.Cell().Text("File").Bold();
                        h.Cell().Text("Type").Bold();
                        h.Cell().Text("Size").Bold();
                        h.Cell().Text("Uploaded By").Bold();
                        h.Cell().Text("Date").Bold();
                    });
                    foreach (var doc in data.Documents)
                    {
                        table.Cell().Text(doc.FileName);
                        table.Cell().Text(doc.DocumentType);
                        table.Cell().Text(FormatSize(doc.FileSizeBytes));
                        table.Cell().Text(doc.UploadedBy);
                        table.Cell().Text(doc.CreatedAt.ToString("yyyy-MM-dd"));
                    }
                });
            });

            page.Footer().AlignCenter().Text("Generated by Tungsten Supply Chain Compliance Platform");
        });
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F1} MB"
    };
}
```

- [ ] **Step 5: Build**

```bash
cd /c/__edMVP/packages/api && dotnet build
```

- [ ] **Step 6: Commit**

---

## Task 2: GeneratePassport and GenerateDossier Commands

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GeneratePassport.cs`
- Create: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GenerateDossier.cs`

- [ ] **Step 1: Implement GeneratePassport**

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.DocumentGeneration.Templates;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class GeneratePassport
{
    public record Command(Guid BatchId) : IRequest<Result<Response>>;

    public record Response(Guid Id, string DownloadUrl, DateTime GeneratedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage, IConfiguration config)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == cmd.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.CreatedAt)
                .Select(e => new PassportEventData(
                    e.EventType, e.EventDate, e.Location,
                    e.ActorName, e.IsCorrection, e.Sha256Hash))
                .ToListAsync(ct);

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == cmd.BatchId)
                .Join(db.CustodyEvents, c => c.CustodyEventId, e => e.Id, (c, e) => new { c, e })
                .Select(x => new PassportComplianceData(
                    x.e.EventType, x.c.Framework, x.c.Status, x.c.CheckedAt))
                .ToListAsync(ct);

            var docs = await db.Documents.AsNoTracking()
                .Where(d => d.BatchId == cmd.BatchId)
                .Select(d => new PassportDocumentData(d.FileName, d.DocumentType, d.CreatedAt))
                .ToListAsync(ct);

            // Verify hash chain integrity
            var allEvents = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);

            var hashChainIntact = VerifyChain(allEvents);

            var baseUrl = config["App:BaseUrl"] ?? "https://tungsten.example.com";
            var verificationUrl = $"{baseUrl}/api/verify/{cmd.BatchId}";

            var passportData = new PassportData(
                batch.BatchNumber, user.Tenant.Name, batch.MineralType,
                batch.OriginCountry, batch.OriginMine, batch.WeightKg,
                batch.Status, batch.ComplianceStatus, verificationUrl,
                events, checks, docs, hashChainIntact, DateTime.UtcNow);

            // Generate PDF
            var template = new PassportTemplate(passportData);
            using var pdfStream = new MemoryStream();
            template.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            // Store
            var storageKey = $"passports/{user.TenantId}/{cmd.BatchId}/{Guid.NewGuid()}.pdf";
            await storage.UploadAsync(storageKey, pdfStream, "application/pdf", ct);

            var genDoc = new GeneratedDocumentEntity
            {
                Id = Guid.NewGuid(),
                BatchId = cmd.BatchId,
                TenantId = user.TenantId,
                DocumentType = "MATERIAL_PASSPORT",
                StorageKey = storageKey,
                GeneratedBy = user.Id,
                GeneratedAt = DateTime.UtcNow,
            };

            db.GeneratedDocuments.Add(genDoc);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                genDoc.Id, storage.GetDownloadUrl(storageKey), genDoc.GeneratedAt));
        }

        private static bool VerifyChain(List<CustodyEventEntity> events)
        {
            string? previousHash = null;
            foreach (var evt in events)
            {
                if (evt.PreviousEventHash != previousHash)
                    return false;
                previousHash = evt.Sha256Hash;
            }
            return true;
        }
    }
}
```

- [ ] **Step 2: Implement GenerateDossier**

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.DocumentGeneration.Templates;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class GenerateDossier
{
    public record Command(Guid BatchId) : IRequest<Result<Response>>;

    public record Response(Guid Id, string DownloadUrl, DateTime GeneratedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == cmd.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderBy(e => e.CreatedAt)
                .Select(e => new DossierEventData(
                    e.EventType, e.EventDate, e.Location,
                    e.ActorName, e.SmelterId, e.Description,
                    e.IsCorrection, e.CorrectsEventId,
                    e.Sha256Hash, e.PreviousEventHash))
                .ToListAsync(ct);

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.BatchId == cmd.BatchId)
                .Join(db.CustodyEvents, c => c.CustodyEventId, e => e.Id, (c, e) => new { c, e })
                .Select(x => new DossierComplianceData(
                    x.e.EventType, x.c.Framework, x.c.Status,
                    x.c.Details.HasValue ? x.c.Details.Value.GetRawText() : "{}",
                    x.c.CheckedAt))
                .ToListAsync(ct);

            var docs = await db.Documents.AsNoTracking()
                .Where(d => d.BatchId == cmd.BatchId)
                .Join(db.Users, d => d.UploadedBy, u => u.Id, (d, u) => new { d, u })
                .Select(x => new DossierDocumentData(
                    x.d.FileName, x.d.DocumentType, x.d.FileSizeBytes,
                    x.u.DisplayName, x.d.CreatedAt))
                .ToListAsync(ct);

            var dossierData = new DossierData(
                batch.BatchNumber, user.Tenant.Name, batch.MineralType,
                batch.OriginCountry, batch.OriginMine, batch.WeightKg,
                batch.Status, batch.ComplianceStatus,
                events, checks, docs, DateTime.UtcNow);

            var template = new DossierTemplate(dossierData);
            using var pdfStream = new MemoryStream();
            template.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            var storageKey = $"dossiers/{user.TenantId}/{cmd.BatchId}/{Guid.NewGuid()}.pdf";
            await storage.UploadAsync(storageKey, pdfStream, "application/pdf", ct);

            var genDoc = new GeneratedDocumentEntity
            {
                Id = Guid.NewGuid(),
                BatchId = cmd.BatchId,
                TenantId = user.TenantId,
                DocumentType = "AUDIT_DOSSIER",
                StorageKey = storageKey,
                GeneratedBy = user.Id,
                GeneratedAt = DateTime.UtcNow,
            };

            db.GeneratedDocuments.Add(genDoc);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                genDoc.Id, storage.GetDownloadUrl(storageKey), genDoc.GeneratedAt));
        }
    }
}
```

- [ ] **Step 3: Build**

```bash
cd /c/__edMVP/packages/api && dotnet build
```

- [ ] **Step 4: Commit**

---

## Task 3: Share Document and Get Generated Document

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GetGeneratedDocument.cs`
- Create: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/ShareDocument.cs`

- [ ] **Step 1: Implement GetGeneratedDocument**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class GetGeneratedDocument
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        Guid BatchId,
        string DocumentType,
        string DownloadUrl,
        string? ShareToken,
        DateTime? ShareExpiresAt,
        DateTime GeneratedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var doc = await db.GeneratedDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == query.Id && d.TenantId == user.TenantId, ct);
            if (doc is null)
                return Result<Response>.Failure("Generated document not found");

            return Result<Response>.Success(new Response(
                doc.Id, doc.BatchId, doc.DocumentType,
                storage.GetDownloadUrl(doc.StorageKey),
                doc.ShareToken, doc.ShareExpiresAt, doc.GeneratedAt));
        }
    }
}
```

- [ ] **Step 2: Implement ShareDocument**

```csharp
using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class ShareDocument
{
    public record Command(Guid DocumentId) : IRequest<Result<Response>>;

    public record Response(string ShareUrl, DateTime ExpiresAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IConfiguration config)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var doc = await db.GeneratedDocuments
                .FirstOrDefaultAsync(d => d.Id == cmd.DocumentId && d.TenantId == user.TenantId, ct);
            if (doc is null)
                return Result<Response>.Failure("Generated document not found");

            // Generate URL-safe token
            var tokenBytes = RandomNumberGenerator.GetBytes(32);
            var token = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-").Replace("/", "_").TrimEnd('=');

            var expiresAt = DateTime.UtcNow.AddDays(30);

            doc.ShareToken = token;
            doc.ShareExpiresAt = expiresAt;
            await db.SaveChangesAsync(ct);

            var baseUrl = config["App:BaseUrl"] ?? "https://tungsten.example.com";
            var shareUrl = $"{baseUrl}/api/shared/{token}";

            return Result<Response>.Success(new Response(shareUrl, expiresAt));
        }
    }
}
```

- [ ] **Step 3: Build**

```bash
cd /c/__edMVP/packages/api && dotnet build
```

- [ ] **Step 4: Commit**

---

## Task 4: Public Endpoints (VerifyBatch + GetSharedDocument)

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Public/VerifyBatch.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Public/GetSharedDocument.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Public/PublicEndpoints.cs`

- [ ] **Step 1: Implement VerifyBatch**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Public;

public static class VerifyBatch
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record Response(
        Guid BatchId,
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string ComplianceStatus,
        int EventCount,
        bool HashChainIntact);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var batch = await db.Batches.AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == query.BatchId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);

            // Verify hash chain
            var hashChainIntact = true;
            string? previousHash = null;
            foreach (var evt in events)
            {
                if (evt.PreviousEventHash != previousHash)
                {
                    hashChainIntact = false;
                    break;
                }
                previousHash = evt.Sha256Hash;
            }

            return Result<Response>.Success(new Response(
                batch.Id, batch.BatchNumber, batch.MineralType,
                batch.OriginCountry, batch.ComplianceStatus,
                events.Count, hashChainIntact));
        }
    }
}
```

- [ ] **Step 2: Implement GetSharedDocument**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Public;

public static class GetSharedDocument
{
    public record Query(string Token) : IRequest<Result<Response>>;

    public record Response(string DownloadUrl, string DocumentType, DateTime GeneratedAt);

    public class Handler(AppDbContext db, IFileStorageService storage) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var doc = await db.GeneratedDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.ShareToken == query.Token, ct);

            if (doc is null)
                return Result<Response>.Failure("Shared document not found");

            if (doc.ShareExpiresAt.HasValue && doc.ShareExpiresAt.Value < DateTime.UtcNow)
                return Result<Response>.Failure("Share link has expired");

            return Result<Response>.Success(new Response(
                storage.GetDownloadUrl(doc.StorageKey),
                doc.DocumentType, doc.GeneratedAt));
        }
    }
}
```

- [ ] **Step 3: Create PublicEndpoints**

```csharp
using MediatR;

namespace Tungsten.Api.Features.Public;

public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        // Public verification endpoint (FR-P060) — unauthenticated
        app.MapGet("/api/verify/{batchId:guid}", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new VerifyBatch.Query(batchId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireRateLimiting("public");

        // Shared document access (FR-P053) — unauthenticated
        app.MapGet("/api/shared/{token}", async (string token, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetSharedDocument.Query(token));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireRateLimiting("public");

        return app;
    }
}
```

- [ ] **Step 4: Commit**

---

## Task 5: Document Generation Endpoints and Wiring

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/DocumentGenerationEndpoints.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Create DocumentGenerationEndpoints**

```csharp
using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class DocumentGenerationEndpoints
{
    public static IEndpointRouteBuilder MapDocumentGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/batches/{batchId:guid}/passport", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GeneratePassport.Command(batchId));
            return result.IsSuccess
                ? Results.Created($"/api/generated-documents/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireBuyer);

        app.MapPost("/api/batches/{batchId:guid}/dossier", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GenerateDossier.Command(batchId));
            return result.IsSuccess
                ? Results.Created($"/api/generated-documents/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireBuyer);

        app.MapGet("/api/generated-documents/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetGeneratedDocument.Query(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        app.MapPost("/api/generated-documents/{id:guid}/share", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new ShareDocument.Command(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireBuyer);

        return app;
    }
}
```

- [ ] **Step 2: Register all new endpoints in Program.cs**

Add usings:
```csharp
using Tungsten.Api.Features.DocumentGeneration;
using Tungsten.Api.Features.Public;
```

Add before `app.Run();`:
```csharp
app.MapDocumentGenerationEndpoints();
app.MapPublicEndpoints();
```

- [ ] **Step 3: Build and run all tests**

```bash
cd /c/__edMVP/packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

Expected: Build succeeds, all 54 existing tests pass. (No new unit tests for PDF generation — tested via integration in E2E phase.)

- [ ] **Step 4: Commit**

---

## Summary

**Phase 5 delivers:**
1. QuestPDF integrated — Material Passport + Audit Dossier PDF templates
2. `POST /api/batches/{batchId}/passport` — generates Material Passport PDF (buyer only)
3. `POST /api/batches/{batchId}/dossier` — generates Audit Dossier PDF (buyer only)
4. `GET /api/generated-documents/{id}` — download generated document
5. `POST /api/generated-documents/{id}/share` — 30-day share link (FR-P053)
6. `GET /api/verify/{batchId}` — public verification endpoint (FR-P060, unauthenticated, rate-limited)
7. `GET /api/shared/{token}` — public shared document access (unauthenticated, rate-limited)

**New endpoints:** 7

**Next:** Phase 6 — Supplier Portal (Angular)
