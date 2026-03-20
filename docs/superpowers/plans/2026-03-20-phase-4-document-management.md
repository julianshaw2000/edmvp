# Phase 4: Document Management — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement document upload/download with SHA-256 hashing, storage abstraction (local filesystem for dev, Cloudflare R2 for production), and document linking to custody events and batches.

**Architecture:** An `IFileStorageService` abstraction handles file persistence. For development/pilot, a `LocalFileStorageService` stores files on disk. The upload endpoint validates file type/size, computes SHA-256, stores via the abstraction, and persists metadata. Downloads return pre-signed URLs (or direct file paths for local storage).

**Tech Stack:** ASP.NET Core 10, EF Core 10, xUnit, NSubstitute, FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-20-tungsten-pilot-mvp-design.md` — Section 4.2 (Document entity), Section 6.1 (Document endpoints), Phase 4

---

## File Structure

```
packages/api/src/Tungsten.Api/
  Common/
    Services/
      IFileStorageService.cs         ← storage abstraction
      LocalFileStorageService.cs     ← local filesystem implementation
  Features/
    Documents/
      UploadDocument.cs              ← POST /api/events/{eventId}/documents
      GetDocument.cs                 ← GET /api/documents/{id}
      ListBatchDocuments.cs          ← GET /api/batches/{batchId}/documents
      DocumentEndpoints.cs           ← endpoint mapping

packages/api/tests/Tungsten.Api.Tests/
  Features/
    Documents/
      UploadDocumentTests.cs
      ListBatchDocumentsTests.cs
```

---

## Task 1: File Storage Abstraction

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Services/IFileStorageService.cs`
- Create: `packages/api/src/Tungsten.Api/Common/Services/LocalFileStorageService.cs`

- [ ] **Step 1: Create IFileStorageService**

```csharp
namespace Tungsten.Api.Common.Services;

public interface IFileStorageService
{
    Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct);
    Task<Stream> DownloadAsync(string key, CancellationToken ct);
    string GetDownloadUrl(string key);
    Task DeleteAsync(string key, CancellationToken ct);
}
```

- [ ] **Step 2: Create LocalFileStorageService**

```csharp
namespace Tungsten.Api.Common.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _basePath = configuration["Storage:LocalPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage");
        Directory.CreateDirectory(_basePath);
    }

    public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        var filePath = GetFilePath(key);
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);

        await using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, ct);
        return key;
    }

    public Task<Stream> DownloadAsync(string key, CancellationToken ct)
    {
        var filePath = GetFilePath(key);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {key}");
        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    public string GetDownloadUrl(string key) => $"/api/documents/file/{key}";

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        var filePath = GetFilePath(key);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    private string GetFilePath(string key) => Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
}
```

- [ ] **Step 3: Register in Program.cs**

Add after the services section:
```csharp
builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();
```

Add using:
```csharp
using Tungsten.Api.Common.Services;
```

- [ ] **Step 4: Build**

```bash
cd /c/__edMVP/packages/api && dotnet build
```

- [ ] **Step 5: Commit**

---

## Task 2: UploadDocument Command

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Documents/UploadDocument.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Documents/UploadDocumentTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Documents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Documents;

public class UploadDocumentTests
{
    private static (AppDbContext db, UserEntity user, CustodyEventEntity evt) SetupDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

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
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.UtcNow, Location = "L", ActorName = "A",
            Description = "D", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        db.SaveChanges();

        return (db, user, evt);
    }

    [Fact]
    public async Task Handle_ValidUpload_CreatesDocumentWithHash()
    {
        var (db, user, evt) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);
        var storage = Substitute.For<IFileStorageService>();
        storage.UploadAsync(Arg.Any<string>(), Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("documents/test.pdf");
        storage.GetDownloadUrl(Arg.Any<string>()).Returns("/api/documents/file/test.pdf");

        var handler = new UploadDocument.Handler(db, currentUser, storage);

        using var stream = new MemoryStream("test content"u8.ToArray());
        var command = new UploadDocument.Command(
            evt.Id, "cert.pdf", "application/pdf",
            stream, stream.Length, "CERTIFICATE_OF_ORIGIN");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FileName.Should().Be("cert.pdf");
        result.Value.Sha256Hash.Should().HaveLength(64);
        result.Value.DocumentType.Should().Be("CERTIFICATE_OF_ORIGIN");

        var doc = await db.Documents.FirstAsync();
        doc.Sha256Hash.Should().HaveLength(64);
    }

    [Fact]
    public async Task Handle_FileTooLarge_ReturnsFailure()
    {
        var (db, user, evt) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);
        var storage = Substitute.For<IFileStorageService>();

        var handler = new UploadDocument.Handler(db, currentUser, storage);

        using var stream = new MemoryStream();
        var command = new UploadDocument.Command(
            evt.Id, "huge.pdf", "application/pdf",
            stream, 26 * 1024 * 1024, // 26MB > 25MB limit
            "CERTIFICATE_OF_ORIGIN");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("25MB");
    }

    [Fact]
    public async Task Handle_InvalidContentType_ReturnsFailure()
    {
        var (db, user, evt) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);
        var storage = Substitute.For<IFileStorageService>();

        var handler = new UploadDocument.Handler(db, currentUser, storage);

        using var stream = new MemoryStream();
        var command = new UploadDocument.Command(
            evt.Id, "script.exe", "application/x-msdownload",
            stream, 1000, "OTHER");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("type");
    }
}
```

- [ ] **Step 2: Implement UploadDocument**

```csharp
using System.Security.Cryptography;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Documents;

public static class UploadDocument
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25MB

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "application/pdf",
        "image/jpeg",
        "image/png",
        "image/tiff",
        "image/gif",
    ];

    private static readonly HashSet<string> ValidDocumentTypes =
    [
        "CERTIFICATE_OF_ORIGIN", "ASSAY_REPORT", "TRANSPORT_DOCUMENT",
        "SMELTER_CERTIFICATE", "MINERALOGICAL_CERTIFICATE", "EXPORT_PERMIT", "OTHER"
    ];

    public record Command(
        Guid EventId,
        string FileName,
        string ContentType,
        Stream FileStream,
        long FileSizeBytes,
        string DocumentType) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string FileName,
        long FileSizeBytes,
        string ContentType,
        string Sha256Hash,
        string DocumentType,
        string DownloadUrl,
        DateTime CreatedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            // Validate file size
            if (cmd.FileSizeBytes > MaxFileSizeBytes)
                return Result<Response>.Failure($"File exceeds maximum size of 25MB");

            // Validate content type
            if (!AllowedContentTypes.Contains(cmd.ContentType))
                return Result<Response>.Failure($"Unsupported content type: {cmd.ContentType}. Allowed: PDF, JPEG, PNG, TIFF, GIF");

            // Validate document type
            if (!ValidDocumentTypes.Contains(cmd.DocumentType))
                return Result<Response>.Failure($"Invalid document type: {cmd.DocumentType}");

            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var evt = await db.CustodyEvents.AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == cmd.EventId && e.TenantId == user.TenantId, ct);
            if (evt is null)
                return Result<Response>.Failure("Event not found");

            // Compute SHA-256 hash of file content (FR-P031)
            cmd.FileStream.Position = 0;
            var hashBytes = await SHA256.HashDataAsync(cmd.FileStream, ct);
            var sha256Hash = Convert.ToHexStringLower(hashBytes);

            // Upload to storage
            cmd.FileStream.Position = 0;
            var storageKey = $"documents/{user.TenantId}/{evt.BatchId}/{Guid.NewGuid()}/{cmd.FileName}";
            await storage.UploadAsync(storageKey, cmd.FileStream, cmd.ContentType, ct);

            var doc = new DocumentEntity
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                CustodyEventId = cmd.EventId,
                BatchId = evt.BatchId,
                FileName = cmd.FileName,
                StorageKey = storageKey,
                FileSizeBytes = cmd.FileSizeBytes,
                ContentType = cmd.ContentType,
                Sha256Hash = sha256Hash,
                DocumentType = cmd.DocumentType,
                UploadedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            db.Documents.Add(doc);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                doc.Id, doc.FileName, doc.FileSizeBytes, doc.ContentType,
                doc.Sha256Hash, doc.DocumentType,
                storage.GetDownloadUrl(storageKey), doc.CreatedAt));
        }
    }
}
```

- [ ] **Step 3: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "UploadDocumentTests"
```

Expected: 3 tests pass.

- [ ] **Step 4: Commit**

---

## Task 3: GetDocument and ListBatchDocuments Queries

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Documents/GetDocument.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Documents/ListBatchDocuments.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Documents/ListBatchDocumentsTests.cs`

- [ ] **Step 1: Implement GetDocument**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Documents;

public static class GetDocument
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string FileName,
        long FileSizeBytes,
        string ContentType,
        string Sha256Hash,
        string DocumentType,
        string DownloadUrl,
        DateTime CreatedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var doc = await db.Documents.AsNoTracking()
                .Where(d => d.Id == query.Id && d.TenantId == user.TenantId)
                .FirstOrDefaultAsync(ct);

            if (doc is null)
                return Result<Response>.Failure("Document not found");

            return Result<Response>.Success(new Response(
                doc.Id, doc.FileName, doc.FileSizeBytes, doc.ContentType,
                doc.Sha256Hash, doc.DocumentType,
                storage.GetDownloadUrl(doc.StorageKey), doc.CreatedAt));
        }
    }
}
```

- [ ] **Step 2: Implement ListBatchDocuments**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Documents;

public static class ListBatchDocuments
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record DocumentItem(
        Guid Id,
        string FileName,
        long FileSizeBytes,
        string ContentType,
        string DocumentType,
        string DownloadUrl,
        DateTime CreatedAt);

    public record Response(IReadOnlyList<DocumentItem> Documents, int TotalCount);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IFileStorageService storage)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var docs = await db.Documents.AsNoTracking()
                .Where(d => d.BatchId == query.BatchId && d.TenantId == user.TenantId)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => new DocumentItem(
                    d.Id, d.FileName, d.FileSizeBytes, d.ContentType,
                    d.DocumentType, storage.GetDownloadUrl(d.StorageKey), d.CreatedAt))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(docs, docs.Count));
        }
    }
}
```

- [ ] **Step 3: Write ListBatchDocuments tests**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Documents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Documents;

public class ListBatchDocumentsTests
{
    [Fact]
    public async Task Handle_BatchWithDocuments_ReturnsAll()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

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

        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchId = batch.Id,
            FileName = "cert.pdf", StorageKey = "k1", FileSizeBytes = 1000,
            ContentType = "application/pdf", Sha256Hash = new string('a', 64),
            DocumentType = "CERTIFICATE_OF_ORIGIN", UploadedBy = user.Id
        });
        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchId = batch.Id,
            FileName = "assay.pdf", StorageKey = "k2", FileSizeBytes = 2000,
            ContentType = "application/pdf", Sha256Hash = new string('b', 64),
            DocumentType = "ASSAY_REPORT", UploadedBy = user.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);
        var storage = Substitute.For<IFileStorageService>();
        storage.GetDownloadUrl(Arg.Any<string>()).Returns(x => $"/download/{x.Arg<string>()}");

        var handler = new ListBatchDocuments.Handler(db, currentUser, storage);
        var result = await handler.Handle(new ListBatchDocuments.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Documents.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_CrossTenant_ReturnsEmpty()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantA = new TenantEntity { Id = Guid.NewGuid(), Name = "A", SchemaPrefix = "a", Status = "ACTIVE" };
        var tenantB = new TenantEntity { Id = Guid.NewGuid(), Name = "B", SchemaPrefix = "b", Status = "ACTIVE" };
        db.Tenants.AddRange(tenantA, tenantB);

        var userA = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|a", Email = "a@t.com",
            DisplayName = "A", Role = "SUPPLIER", TenantId = tenantA.Id, IsActive = true
        };
        var userB = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|b", Email = "b@t.com",
            DisplayName = "B", Role = "SUPPLIER", TenantId = tenantB.Id, IsActive = true
        };
        db.Users.AddRange(userA, userB);

        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantA.Id, BatchNumber = "B1",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "M",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = userA.Id
        };
        db.Batches.Add(batch);
        db.Documents.Add(new DocumentEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantA.Id, BatchId = batch.Id,
            FileName = "cert.pdf", StorageKey = "k1", FileSizeBytes = 1000,
            ContentType = "application/pdf", Sha256Hash = new string('a', 64),
            DocumentType = "CERTIFICATE_OF_ORIGIN", UploadedBy = userA.Id
        });
        db.SaveChanges();

        // User B tries to access tenant A's documents
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(userB.Auth0Sub);
        var storage = Substitute.For<IFileStorageService>();

        var handler = new ListBatchDocuments.Handler(db, currentUser, storage);
        var result = await handler.Handle(new ListBatchDocuments.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Documents.Should().BeEmpty();
    }
}
```

- [ ] **Step 4: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "Documents"
```

Expected: 5 tests pass (3 Upload + 2 List).

- [ ] **Step 5: Commit**

---

## Task 4: Document Endpoints Wiring

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Documents/DocumentEndpoints.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Create DocumentEndpoints**

```csharp
using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Documents;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        // Upload document to an event
        app.MapPost("/api/events/{eventId:guid}/documents", async (
            Guid eventId,
            IFormFile file,
            string documentType,
            IMediator mediator) =>
        {
            using var stream = file.OpenReadStream();
            var command = new UploadDocument.Command(
                eventId, file.FileName, file.ContentType,
                stream, file.Length, documentType);

            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/documents/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(AuthorizationPolicies.RequireSupplier)
        .DisableAntiforgery();

        // Get document (returns download URL)
        app.MapGet("/api/documents/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetDocument.Query(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        // List all documents for a batch
        app.MapGet("/api/batches/{batchId:guid}/documents", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListBatchDocuments.Query(batchId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization();

        return app;
    }
}
```

- [ ] **Step 2: Register in Program.cs**

Add `using Tungsten.Api.Features.Documents;` and `app.MapDocumentEndpoints();` before `app.Run();`.

- [ ] **Step 3: Build and run all tests**

```bash
cd /c/__edMVP/packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

Expected: Build succeeds, all tests pass (~54 total).

- [ ] **Step 4: Commit**

---

## Summary

**Phase 4 delivers:**
1. `IFileStorageService` abstraction with `LocalFileStorageService` for dev
2. Document upload: `POST /api/events/{eventId}/documents` — validates type/size, computes SHA-256 (FR-P031), stores file, persists metadata
3. Document download: `GET /api/documents/{id}` — returns download URL
4. Document listing: `GET /api/batches/{batchId}/documents` — all documents for a batch with cross-tenant isolation
5. File size limit: 25MB (NFR-P03)
6. Content type validation: PDF, JPEG, PNG, TIFF, GIF only

**Tests:** 5 new tests (3 Upload + 2 ListBatch)

**Next:** Phase 5 — Document Generation (QuestPDF, Material Passport, Audit Dossier)
