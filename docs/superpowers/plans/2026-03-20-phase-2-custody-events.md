# Phase 2: Custody Events — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement batch CRUD and custody event creation with SHA-256 hash chains, idempotency, corrections, metadata validation, and integrity verification — all with full unit and integration tests.

**Architecture:** Vertical Slice pattern with MediatR. Each endpoint is a self-contained file containing command/query, handler, validator, and response DTO. Handlers use the Result pattern for error flow. FluentValidation validators run via MediatR pipeline behaviour. Metadata validation uses per-event-type schemas.

**Tech Stack:** ASP.NET Core 10, MediatR, FluentValidation, EF Core 10, xUnit, NSubstitute, FluentAssertions

**Spec:** `docs/superpowers/specs/2026-03-20-tungsten-pilot-mvp-design.md` — Sections 4.2, 4.3, 6.1, 10, Phase 2

**Existing code context:**
- Entities and DbContext already exist (Phase 1)
- `Common/Result.cs` provides `Result<T>` / `Result` types
- `Common/Auth/` has `ICurrentUserService`, `Roles`, authorization policies
- `Features/Auth/GetMe.cs` is the only existing feature slice
- Program.cs is fully wired with MediatR, FluentValidation, auth, CORS, etc.

---

## File Structure

```
packages/api/src/Tungsten.Api/
  Common/
    Pagination/
      PagedRequest.cs              ← shared pagination query params
      PagedResponse.cs             ← shared pagination envelope
    Services/
      HashService.cs               ← SHA-256 hashing for events
      IdempotencyKeyService.cs     ← deterministic key generation
  Features/
    Batches/
      CreateBatch.cs               ← POST /api/batches
      GetBatch.cs                  ← GET /api/batches/{id}
      ListBatches.cs               ← GET /api/batches
      BatchEndpoints.cs            ← endpoint mapping
    CustodyEvents/
      CreateCustodyEvent.cs        ← POST /api/batches/{batchId}/events
      CreateCorrection.cs          ← POST /api/events/{eventId}/corrections
      GetCustodyEvent.cs           ← GET /api/events/{id}
      ListCustodyEvents.cs         ← GET /api/batches/{batchId}/events
      VerifyIntegrity.cs           ← GET /api/batches/{batchId}/verify-integrity
      CustodyEventEndpoints.cs     ← endpoint mapping
      MetadataValidator.cs         ← per-event-type metadata validation

packages/api/tests/Tungsten.Api.Tests/
  Features/
    Batches/
      CreateBatchTests.cs
      GetBatchTests.cs
      ListBatchesTests.cs
    CustodyEvents/
      CreateCustodyEventTests.cs
      CreateCorrectionTests.cs
      HashServiceTests.cs
      IdempotencyKeyServiceTests.cs
      VerifyIntegrityTests.cs
      MetadataValidatorTests.cs
```

---

## Chunk 1: Shared Infrastructure and Batch CRUD

### Task 0: FluentValidation MediatR Pipeline Behaviour

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Behaviours/ValidationBehaviour.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Create ValidationBehaviour**

```csharp
using FluentValidation;
using MediatR;
using Tungsten.Api.Common;

namespace Tungsten.Api.Common.Behaviours;

public class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count == 0)
            return await next();

        var errors = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // If TResponse is Result<T>, return a failure Result
        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(string)])!;
            return (TResponse)failureMethod.Invoke(null, [$"Validation failed: {errors}"])!;
        }

        throw new ValidationException(failures);
    }
}
```

- [ ] **Step 2: Register in Program.cs**

Add after the `AddMediatR` line:
```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
```

Add using:
```csharp
using Tungsten.Api.Common.Behaviours;
using MediatR;
```

- [ ] **Step 3: Build and test**

```bash
cd /c/__edMVP/packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Behaviours/ packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: add FluentValidation MediatR pipeline behaviour for automatic request validation"
```

---

### Task 1: Pagination Types

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Pagination/PagedRequest.cs`
- Create: `packages/api/src/Tungsten.Api/Common/Pagination/PagedResponse.cs`

- [ ] **Step 1: Create PagedRequest**

```csharp
namespace Tungsten.Api.Common.Pagination;

public record PagedRequest(int Page = 1, int PageSize = 20)
{
    public int Skip => (Page - 1) * PageSize;
}
```

- [ ] **Step 2: Create PagedResponse**

```csharp
namespace Tungsten.Api.Common.Pagination;

public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize);
```

- [ ] **Step 3: Verify build**

```bash
cd /c/__edMVP/packages/api && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Pagination/
git commit -m "feat: add shared pagination types (PagedRequest, PagedResponse)"
```

---

### Task 2: Batch CRUD — CreateBatch

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Batches/CreateBatch.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Batches/CreateBatchTests.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Batches;

public class CreateBatchTests
{
    private static (AppDbContext db, TenantEntity tenant, UserEntity user) SetupDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "Test", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|1", Email = "s@test.com",
            DisplayName = "Supplier", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        db.SaveChanges();

        return (db, tenant, user);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesBatch()
    {
        var (db, tenant, user) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new CreateBatch.Handler(db, currentUser);
        var command = new CreateBatch.Command(
            "BATCH-001", "tungsten", "CD", "Bisie Mine", 500.0m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchNumber.Should().Be("BATCH-001");
        result.Value.Status.Should().Be("CREATED");
        result.Value.ComplianceStatus.Should().Be("PENDING");
    }

    [Fact]
    public async Task Handle_DuplicateBatchNumber_ReturnsFailure()
    {
        var (db, tenant, user) = SetupDb();
        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "BATCH-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new CreateBatch.Handler(db, currentUser);
        var command = new CreateBatch.Command(
            "BATCH-001", "tungsten", "CD", "Bisie Mine", 500.0m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }
}
```

- [ ] **Step 2: Run tests — verify they fail**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "CreateBatchTests"
```

Expected: FAIL — `CreateBatch` type doesn't exist.

- [ ] **Step 3: Implement CreateBatch handler**

```csharp
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Batches;

public static class CreateBatch
{
    public record Command(
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string OriginMine,
        decimal WeightKg) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string OriginMine,
        decimal WeightKg,
        string Status,
        string ComplianceStatus,
        DateTime CreatedAt);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BatchNumber).NotEmpty().MaximumLength(100);
            RuleFor(x => x.MineralType).NotEmpty().MaximumLength(50);
            RuleFor(x => x.OriginCountry).NotEmpty().Length(2)
                .Matches("^[A-Z]{2}$").WithMessage("Must be ISO 3166-1 alpha-2");
            RuleFor(x => x.OriginMine).NotEmpty().MaximumLength(200);
            RuleFor(x => x.WeightKg).GreaterThan(0);
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var exists = await db.Batches.AnyAsync(
                b => b.TenantId == user.TenantId && b.BatchNumber == cmd.BatchNumber, ct);
            if (exists)
                return Result<Response>.Failure($"Batch '{cmd.BatchNumber}' already exists in this tenant");

            var batch = new BatchEntity
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                BatchNumber = cmd.BatchNumber,
                MineralType = cmd.MineralType,
                OriginCountry = cmd.OriginCountry,
                OriginMine = cmd.OriginMine,
                WeightKg = cmd.WeightKg,
                Status = "CREATED",
                ComplianceStatus = "PENDING",
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            db.Batches.Add(batch);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                batch.Id, batch.BatchNumber, batch.MineralType,
                batch.OriginCountry, batch.OriginMine, batch.WeightKg,
                batch.Status, batch.ComplianceStatus, batch.CreatedAt));
        }
    }
}
```

- [ ] **Step 4: Run tests — verify they pass**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "CreateBatchTests"
```

Expected: 2 tests pass.

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Batches/CreateBatch.cs packages/api/tests/Tungsten.Api.Tests/Features/Batches/CreateBatchTests.cs
git commit -m "feat: add CreateBatch command with validation and duplicate check"
```

---

### Task 3: Batch CRUD — GetBatch and ListBatches

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Batches/GetBatch.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Batches/ListBatches.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Batches/GetBatchTests.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Batches/ListBatchesTests.cs`

- [ ] **Step 1: Write GetBatch tests**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Batches;

public class GetBatchTests
{
    [Fact]
    public async Task Handle_ExistingBatch_ReturnsBatchWithEventCount()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|1", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);

        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new GetBatch.Handler(db, currentUser);
        var result = await handler.Handle(new GetBatch.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchNumber.Should().Be("B-001");
        result.Value.EventCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NonExistentBatch_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|1", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new GetBatch.Handler(db, currentUser);
        var result = await handler.Handle(new GetBatch.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Implement GetBatch**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Batches;

public static class GetBatch
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string OriginMine,
        decimal WeightKg,
        string Status,
        string ComplianceStatus,
        DateTime CreatedAt,
        int EventCount);

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
                .Where(b => b.Id == query.Id && b.TenantId == user.TenantId)
                .Select(b => new Response(
                    b.Id, b.BatchNumber, b.MineralType,
                    b.OriginCountry, b.OriginMine, b.WeightKg,
                    b.Status, b.ComplianceStatus, b.CreatedAt,
                    b.CustodyEvents.Count))
                .FirstOrDefaultAsync(ct);

            return batch is null
                ? Result<Response>.Failure("Batch not found")
                : Result<Response>.Success(batch);
        }
    }
}
```

- [ ] **Step 3: Write ListBatches tests**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Batches;

public class ListBatchesTests
{
    [Fact]
    public async Task Handle_SupplierRole_ReturnsOnlyOwnBatches()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var supplier1 = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s1", Email = "s1@test.com",
            DisplayName = "S1", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        var supplier2 = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s2", Email = "s2@test.com",
            DisplayName = "S2", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(supplier1, supplier2);

        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "M1",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = supplier1.Id
        });
        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-002",
            MineralType = "tungsten", OriginCountry = "RW", OriginMine = "M2",
            WeightKg = 200, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = supplier2.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(supplier1.Auth0Sub);

        var handler = new ListBatches.Handler(db, currentUser);
        var result = await handler.Handle(new ListBatches.Query(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].BatchNumber.Should().Be("B-001");
    }

    [Fact]
    public async Task Handle_BuyerRole_ReturnsAllTenantBatches()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        var buyer = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|b", Email = "b@test.com",
            DisplayName = "B", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(supplier, buyer);

        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "M1",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = supplier.Id
        });
        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-002",
            MineralType = "tungsten", OriginCountry = "RW", OriginMine = "M2",
            WeightKg = 200, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = supplier.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(buyer.Auth0Sub);

        var handler = new ListBatches.Handler(db, currentUser);
        var result = await handler.Handle(new ListBatches.Query(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }
}
```

- [ ] **Step 4: Implement ListBatches**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Pagination;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Batches;

public static class ListBatches
{
    public record Query(int Page = 1, int PageSize = 20) : IRequest<Result<PagedResponse<BatchItem>>>;

    public record BatchItem(
        Guid Id,
        string BatchNumber,
        string MineralType,
        string OriginCountry,
        string OriginMine,
        decimal WeightKg,
        string Status,
        string ComplianceStatus,
        DateTime CreatedAt,
        int EventCount);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<PagedResponse<BatchItem>>>
    {
        public async Task<Result<PagedResponse<BatchItem>>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<PagedResponse<BatchItem>>.Failure("User not found");

            var baseQuery = db.Batches.AsNoTracking()
                .Where(b => b.TenantId == user.TenantId);

            // Suppliers see only their own batches; buyers and admins see all tenant batches
            if (user.Role == Roles.Supplier)
                baseQuery = baseQuery.Where(b => b.CreatedBy == user.Id);

            var totalCount = await baseQuery.CountAsync(ct);

            var paged = new PagedRequest(query.Page, query.PageSize);
            var items = await baseQuery
                .OrderByDescending(b => b.CreatedAt)
                .Skip(paged.Skip)
                .Take(paged.PageSize)
                .Select(b => new BatchItem(
                    b.Id, b.BatchNumber, b.MineralType,
                    b.OriginCountry, b.OriginMine, b.WeightKg,
                    b.Status, b.ComplianceStatus, b.CreatedAt,
                    b.CustodyEvents.Count))
                .ToListAsync(ct);

            return Result<PagedResponse<BatchItem>>.Success(
                new PagedResponse<BatchItem>(items, totalCount, query.Page, query.PageSize));
        }
    }
}
```

- [ ] **Step 5: Run all batch tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "Batches"
```

Expected: 4 tests pass (2 CreateBatch + 2 GetBatch + 2 ListBatches... actually the GetBatch tests haven't been created yet in the test step. Let me include them properly).

Wait — GetBatch tests were written in Step 1 above. So: 2 CreateBatch + 2 GetBatch + 2 ListBatches = 6 tests pass.

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Batches/ packages/api/tests/Tungsten.Api.Tests/Features/Batches/
git commit -m "feat: add GetBatch and ListBatches queries with role-based filtering"
```

---

### Task 4: Batch Endpoints Wiring

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Batches/BatchEndpoints.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Create BatchEndpoints**

```csharp
using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Batches;

public static class BatchEndpoints
{
    public static IEndpointRouteBuilder MapBatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/batches").RequireAuthorization();

        group.MapPost("/", async (CreateBatch.Command command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/batches/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireSupplier);

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetBatch.Query(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        });

        group.MapGet("/", async (int? page, int? pageSize, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListBatches.Query(page ?? 1, pageSize ?? 20));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
```

- [ ] **Step 2: Register endpoints in Program.cs**

Add before `app.Run();`:
```csharp
app.MapBatchEndpoints();
```

Add using at top:
```csharp
using Tungsten.Api.Features.Batches;
```

- [ ] **Step 3: Build and test**

```bash
cd /c/__edMVP/packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

Expected: Build succeeds, all unit tests pass.

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Batches/BatchEndpoints.cs packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: wire batch endpoints (POST, GET, GET list) in Program.cs"
```

---

## Chunk 2: Hash Service, Idempotency, and Custody Event Creation

### Task 5: HashService and IdempotencyKeyService

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Services/HashService.cs`
- Create: `packages/api/src/Tungsten.Api/Common/Services/IdempotencyKeyService.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/HashServiceTests.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/IdempotencyKeyServiceTests.cs`

- [ ] **Step 1: Write HashService tests**

```csharp
using FluentAssertions;
using Tungsten.Api.Common.Services;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class HashServiceTests
{
    [Fact]
    public void ComputeEventHash_SameInput_SameHash()
    {
        var date = HashService.NormalizeDate("2026-01-15T10:00:00Z");
        var hash1 = HashService.ComputeEventHash("MINE_EXTRACTION", date,
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Bisie Mine",
            "Mining Corp", null, "First extraction", "{}", null);

        var hash2 = HashService.ComputeEventHash("MINE_EXTRACTION", date,
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Bisie Mine",
            "Mining Corp", null, "First extraction", "{}", null);

        hash1.Should().Be(hash2);
    }

    [Fact]
    public void ComputeEventHash_DifferentInput_DifferentHash()
    {
        var hash1 = HashService.ComputeEventHash("MINE_EXTRACTION",
            HashService.NormalizeDate("2026-01-15T10:00:00Z"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Bisie Mine",
            "Mining Corp", null, "First extraction", "{}", null);

        var hash2 = HashService.ComputeEventHash("MINE_EXTRACTION",
            HashService.NormalizeDate("2026-01-15T11:00:00Z"),
            Guid.Parse("11111111-1111-1111-1111-111111111111"), "Bisie Mine",
            "Mining Corp", null, "First extraction", "{}", null);

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ComputeEventHash_Returns64CharHexString()
    {
        var hash = HashService.ComputeEventHash("MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            Guid.NewGuid(), "loc", "actor", null, "desc", "{}", null);

        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");
    }

    [Fact]
    public void ComputeEventHash_IncludesPreviousHash_ChangesResult()
    {
        var batchId = Guid.NewGuid();
        var hashWithout = HashService.ComputeEventHash("MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            batchId, "loc", "actor", null, "desc", "{}", null);

        var hashWith = HashService.ComputeEventHash("MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            batchId, "loc", "actor", null, "desc", "{}", "abc123");

        hashWithout.Should().NotBe(hashWith);
    }
}
```

- [ ] **Step 2: Implement HashService**

```csharp
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tungsten.Api.Common.Services;

public static class HashService
{
    /// <summary>
    /// Normalizes a date string to UTC ISO 8601 format for consistent hashing.
    /// Both creation and verification paths MUST use this to avoid format mismatches.
    /// </summary>
    public static string NormalizeDate(string dateString) =>
        DateTime.Parse(dateString).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

    public static string NormalizeDate(DateTime date) =>
        date.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

    public static string ComputeEventHash(
        string eventType,
        string eventDate,
        Guid batchId,
        string location,
        string actorName,
        string? smelterId,
        string description,
        string metadata,
        string? previousEventHash)
    {
        // Use SortedDictionary for guaranteed stable key ordering
        var fields = new SortedDictionary<string, string>
        {
            ["actor_name"] = actorName,
            ["batch_id"] = batchId.ToString(),
            ["description"] = description,
            ["event_date"] = eventDate, // caller must pre-normalize via NormalizeDate
            ["event_type"] = eventType,
            ["location"] = location,
            ["metadata"] = metadata,
            ["previous_event_hash"] = previousEventHash ?? "",
            ["smelter_id"] = smelterId ?? "",
        };

        var canonical = JsonSerializer.Serialize(fields, new JsonSerializerOptions { WriteIndented = false });
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexStringLower(hashBytes);
    }
}
```

- [ ] **Step 3: Write IdempotencyKeyService tests**

```csharp
using FluentAssertions;
using Tungsten.Api.Common.Services;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class IdempotencyKeyServiceTests
{
    [Fact]
    public void GenerateKey_SameInput_SameKey()
    {
        var batchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var key1 = IdempotencyKeyService.GenerateKey(batchId, "MINE_EXTRACTION", "2026-01-15T10:00:00Z", "Bisie Mine", "Mining Corp");
        var key2 = IdempotencyKeyService.GenerateKey(batchId, "MINE_EXTRACTION", "2026-01-15T10:00:00Z", "Bisie Mine", "Mining Corp");

        key1.Should().Be(key2);
    }

    [Fact]
    public void GenerateKey_DifferentInput_DifferentKey()
    {
        var batchId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var key1 = IdempotencyKeyService.GenerateKey(batchId, "MINE_EXTRACTION", "2026-01-15T10:00:00Z", "Bisie Mine", "Mining Corp");
        var key2 = IdempotencyKeyService.GenerateKey(batchId, "CONCENTRATION", "2026-01-15T10:00:00Z", "Bisie Mine", "Mining Corp");

        key1.Should().NotBe(key2);
    }
}
```

- [ ] **Step 4: Implement IdempotencyKeyService**

```csharp
using System.Security.Cryptography;
using System.Text;

namespace Tungsten.Api.Common.Services;

public static class IdempotencyKeyService
{
    public static string GenerateKey(Guid batchId, string eventType, string eventDate, string location, string actorName)
    {
        var input = $"{batchId}|{eventType}|{eventDate}|{location}|{actorName}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(hashBytes);
    }
}
```

- [ ] **Step 5: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "HashServiceTests|IdempotencyKeyServiceTests"
```

Expected: 6 tests pass (4 hash + 2 idempotency).

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Services/ packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/
git commit -m "feat: add HashService (SHA-256 event hashing) and IdempotencyKeyService"
```

---

### Task 6: Metadata Validator

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/CustodyEvents/MetadataValidator.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/MetadataValidatorTests.cs`

- [ ] **Step 1: Write MetadataValidator tests**

```csharp
using System.Text.Json;
using FluentAssertions;
using Tungsten.Api.Features.CustodyEvents;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class MetadataValidatorTests
{
    [Fact]
    public void Validate_MineExtraction_ValidMetadata_ReturnsSuccess()
    {
        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0",
            mineOperatorIdentity = "Mining Corp",
            mineralogicalCertificateRef = "CERT-001"
        });

        var result = MetadataValidator.Validate("MINE_EXTRACTION", metadata);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MineExtraction_MissingField_ReturnsErrors()
    {
        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0"
            // missing mineOperatorIdentity and mineralogicalCertificateRef
        });

        var result = MetadataValidator.Validate("MINE_EXTRACTION", metadata);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_PrimaryProcessing_ValidMetadata_ReturnsSuccess()
    {
        var metadata = JsonSerializer.SerializeToElement(new
        {
            smelterId = "CID001100",
            processType = "Carbothermic reduction",
            inputWeightKg = 500.0,
            outputWeightKg = 450.0
        });

        var result = MetadataValidator.Validate("PRIMARY_PROCESSING", metadata);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnknownEventType_ReturnsError()
    {
        var metadata = JsonSerializer.SerializeToElement(new { });

        var result = MetadataValidator.Validate("UNKNOWN_TYPE", metadata);
        result.IsValid.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Implement MetadataValidator**

```csharp
using System.Text.Json;

namespace Tungsten.Api.Features.CustodyEvents;

public record MetadataValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static MetadataValidationResult Success() => new(true, []);
    public static MetadataValidationResult Failure(params string[] errors) => new(false, errors);
}

public static class MetadataValidator
{
    private static readonly Dictionary<string, string[]> RequiredFields = new()
    {
        ["MINE_EXTRACTION"] = ["gpsCoordinates", "mineOperatorIdentity", "mineralogicalCertificateRef"],
        ["CONCENTRATION"] = ["facilityName", "processDescription", "inputWeightKg", "outputWeightKg", "concentrationRatio"],
        ["TRADING_TRANSFER"] = ["sellerIdentity", "buyerIdentity", "transferDate", "contractReference"],
        ["LABORATORY_ASSAY"] = ["laboratoryName", "assayMethod", "tungstenContentPct", "assayCertificateRef"],
        ["PRIMARY_PROCESSING"] = ["smelterId", "processType", "inputWeightKg", "outputWeightKg"],
        ["EXPORT_SHIPMENT"] = ["originCountry", "destinationCountry", "transportMode", "exportPermitRef"],
    };

    public static MetadataValidationResult Validate(string eventType, JsonElement metadata)
    {
        if (!RequiredFields.TryGetValue(eventType, out var required))
            return MetadataValidationResult.Failure($"Unknown event type: {eventType}");

        var errors = new List<string>();
        foreach (var field in required)
        {
            if (!metadata.TryGetProperty(field, out var prop) ||
                prop.ValueKind == JsonValueKind.Null ||
                (prop.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(prop.GetString())))
            {
                errors.Add($"Missing required metadata field: {field}");
            }
        }

        return errors.Count == 0
            ? MetadataValidationResult.Success()
            : MetadataValidationResult.Failure([.. errors]);
    }
}
```

- [ ] **Step 3: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "MetadataValidatorTests"
```

Expected: 4 tests pass.

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/CustodyEvents/MetadataValidator.cs packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/MetadataValidatorTests.cs
git commit -m "feat: add per-event-type metadata validator for custody events"
```

---

### Task 7: CreateCustodyEvent Command

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/CustodyEvents/CreateCustodyEvent.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/CreateCustodyEventTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.CustodyEvents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class CreateCustodyEventTests
{
    private static (AppDbContext db, TenantEntity tenant, UserEntity user, BatchEntity batch) SetupDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);

        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 500, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);
        db.SaveChanges();

        return (db, tenant, user, batch);
    }

    [Fact]
    public async Task Handle_ValidEvent_CreatesWithHashAndIdempotencyKey()
    {
        var (db, tenant, user, batch) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0",
            mineOperatorIdentity = "Mining Corp",
            mineralogicalCertificateRef = "CERT-001"
        });

        var handler = new CreateCustodyEvent.Handler(db, currentUser);
        var command = new CreateCustodyEvent.Command(
            batch.Id, "MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            "Bisie Mine", null, "Mining Corp", null,
            "First extraction", metadata);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sha256Hash.Should().HaveLength(64);
        result.Value.PreviousEventHash.Should().BeNull();

        var saved = await db.CustodyEvents.FirstAsync();
        saved.IdempotencyKey.Should().NotBeNullOrEmpty();
        saved.Sha256Hash.Should().Be(result.Value.Sha256Hash);
    }

    [Fact]
    public async Task Handle_SecondEvent_LinksPreviousHash()
    {
        var (db, tenant, user, batch) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        // Create first event directly in DB
        var firstEvent = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "key1",
            EventDate = DateTime.UtcNow, Location = "Bisie", ActorName = "Corp",
            Description = "First", Sha256Hash = "aabbccdd" + new string('0', 56),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(firstEvent);
        db.SaveChanges();

        var metadata = JsonSerializer.SerializeToElement(new
        {
            facilityName = "Processor",
            processDescription = "Concentration",
            inputWeightKg = 500.0,
            outputWeightKg = 400.0,
            concentrationRatio = 1.25
        });

        var handler = new CreateCustodyEvent.Handler(db, currentUser);
        var command = new CreateCustodyEvent.Command(
            batch.Id, "CONCENTRATION", "2026-01-16T10:00:00Z",
            "Facility", null, "Processor Inc", null,
            "Concentration step", metadata);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviousEventHash.Should().Be(firstEvent.Sha256Hash);
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ReturnsFailure()
    {
        var (db, tenant, user, batch) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0",
            mineOperatorIdentity = "Mining Corp",
            mineralogicalCertificateRef = "CERT-001"
        });

        var handler = new CreateCustodyEvent.Handler(db, currentUser);
        var command = new CreateCustodyEvent.Command(
            batch.Id, "MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            "Bisie Mine", null, "Mining Corp", null,
            "First extraction", metadata);

        // First call succeeds
        var result1 = await handler.Handle(command, CancellationToken.None);
        result1.IsSuccess.Should().BeTrue();

        // Second call with same key fields fails
        var result2 = await handler.Handle(command, CancellationToken.None);
        result2.IsSuccess.Should().BeFalse();
        result2.Error.Should().Contain("Duplicate");
    }

    [Fact]
    public async Task Handle_InvalidMetadata_ReturnsFailure()
    {
        var (db, tenant, user, batch) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var metadata = JsonSerializer.SerializeToElement(new { /* missing required fields */ });

        var handler = new CreateCustodyEvent.Handler(db, currentUser);
        var command = new CreateCustodyEvent.Command(
            batch.Id, "MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            "Bisie Mine", null, "Mining Corp", null,
            "First extraction", metadata);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("metadata");
    }
}
```

- [ ] **Step 2: Implement CreateCustodyEvent**

```csharp
using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.CustodyEvents;

public static class CreateCustodyEvent
{
    public record Command(
        Guid BatchId,
        string EventType,
        string EventDate,
        string Location,
        string? GpsCoordinates,
        string ActorName,
        string? SmelterId,
        string Description,
        JsonElement Metadata) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        Guid BatchId,
        string EventType,
        string EventDate,
        string Location,
        string ActorName,
        string Sha256Hash,
        string? PreviousEventHash,
        DateTime CreatedAt);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BatchId).NotEmpty();
            RuleFor(x => x.EventType).NotEmpty().MaximumLength(30);
            RuleFor(x => x.EventDate).NotEmpty();
            RuleFor(x => x.Location).NotEmpty().MaximumLength(500);
            RuleFor(x => x.ActorName).NotEmpty().MaximumLength(300);
            RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches
                .FirstOrDefaultAsync(b => b.Id == cmd.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            // Validate metadata for event type
            var metadataValidation = MetadataValidator.Validate(cmd.EventType, cmd.Metadata);
            if (!metadataValidation.IsValid)
                return Result<Response>.Failure($"Invalid metadata: {string.Join("; ", metadataValidation.Errors)}");

            // Generate idempotency key
            var idempotencyKey = IdempotencyKeyService.GenerateKey(
                cmd.BatchId, cmd.EventType, cmd.EventDate, cmd.Location, cmd.ActorName);

            // Check for duplicate
            var exists = await db.CustodyEvents.AnyAsync(
                e => e.BatchId == cmd.BatchId && e.IdempotencyKey == idempotencyKey, ct);
            if (exists)
                return Result<Response>.Failure("Duplicate event: an event with these key fields already exists for this batch");

            // Get previous event hash for chain
            var previousHash = await db.CustodyEvents
                .Where(e => e.BatchId == cmd.BatchId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => e.Sha256Hash)
                .FirstOrDefaultAsync(ct);

            // Normalize date and compute SHA-256 hash
            var normalizedDate = HashService.NormalizeDate(cmd.EventDate);
            var metadataString = cmd.Metadata.GetRawText();
            var sha256Hash = HashService.ComputeEventHash(
                cmd.EventType, normalizedDate, cmd.BatchId, cmd.Location,
                cmd.ActorName, cmd.SmelterId, cmd.Description,
                metadataString, previousHash);

            var entity = new CustodyEventEntity
            {
                Id = Guid.NewGuid(),
                BatchId = cmd.BatchId,
                TenantId = user.TenantId,
                EventType = cmd.EventType,
                IdempotencyKey = idempotencyKey,
                EventDate = DateTime.Parse(cmd.EventDate).ToUniversalTime(),
                Location = cmd.Location,
                GpsCoordinates = cmd.GpsCoordinates,
                ActorName = cmd.ActorName,
                SmelterId = cmd.SmelterId,
                Description = cmd.Description,
                Metadata = cmd.Metadata,
                SchemaVersion = 1,
                IsCorrection = false,
                Sha256Hash = sha256Hash,
                PreviousEventHash = previousHash,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            db.CustodyEvents.Add(entity);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                entity.Id, entity.BatchId, entity.EventType,
                cmd.EventDate, entity.Location, entity.ActorName,
                entity.Sha256Hash, entity.PreviousEventHash,
                entity.CreatedAt));
        }
    }
}
```

- [ ] **Step 3: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "CreateCustodyEventTests"
```

Expected: 4 tests pass.

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/CustodyEvents/CreateCustodyEvent.cs packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/CreateCustodyEventTests.cs
git commit -m "feat: add CreateCustodyEvent with SHA-256 hashing, hash chain, idempotency, and metadata validation"
```

---

## Chunk 3: Corrections, Queries, Integrity Verification, and Endpoint Wiring

### Task 8: CreateCorrection Command

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/CustodyEvents/CreateCorrection.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/CreateCorrectionTests.cs`

- [ ] **Step 1: Write tests**

```csharp
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.CustodyEvents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class CreateCorrectionTests
{
    [Fact]
    public async Task Handle_ValidCorrection_LinksToOriginalEvent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 500, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);

        var originalEvent = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "key1",
            EventDate = DateTime.UtcNow, Location = "Bisie", ActorName = "Corp",
            Description = "Original", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(originalEvent);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0",
            mineOperatorIdentity = "Corrected Mining Corp",
            mineralogicalCertificateRef = "CERT-002"
        });

        var handler = new CreateCorrection.Handler(db, currentUser);
        var command = new CreateCorrection.Command(
            originalEvent.Id, "2026-01-15T10:00:00Z",
            "Bisie Mine", null, "Corrected Mining Corp", null,
            "Corrected extraction details", metadata);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrection.Should().BeTrue();
        result.Value.CorrectsEventId.Should().Be(originalEvent.Id);
    }
}
```

- [ ] **Step 2: Implement CreateCorrection**

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.CustodyEvents;

public static class CreateCorrection
{
    public record Command(
        Guid OriginalEventId,
        string EventDate,
        string Location,
        string? GpsCoordinates,
        string ActorName,
        string? SmelterId,
        string Description,
        JsonElement Metadata) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        Guid BatchId,
        string EventType,
        bool IsCorrection,
        Guid? CorrectsEventId,
        string Sha256Hash,
        DateTime CreatedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var originalEvent = await db.CustodyEvents
                .FirstOrDefaultAsync(e => e.Id == cmd.OriginalEventId && e.TenantId == user.TenantId, ct);
            if (originalEvent is null)
                return Result<Response>.Failure("Original event not found");

            // Validate metadata against original event type
            var metadataValidation = MetadataValidator.Validate(originalEvent.EventType, cmd.Metadata);
            if (!metadataValidation.IsValid)
                return Result<Response>.Failure($"Invalid metadata: {string.Join("; ", metadataValidation.Errors)}");

            // Correction gets its own idempotency key (original event id + correction timestamp)
            var idempotencyKey = IdempotencyKeyService.GenerateKey(
                originalEvent.BatchId, originalEvent.EventType, cmd.EventDate, cmd.Location, cmd.ActorName);

            var exists = await db.CustodyEvents.AnyAsync(
                e => e.BatchId == originalEvent.BatchId && e.IdempotencyKey == idempotencyKey, ct);
            if (exists)
                return Result<Response>.Failure("Duplicate correction event");

            var previousHash = await db.CustodyEvents
                .Where(e => e.BatchId == originalEvent.BatchId)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => e.Sha256Hash)
                .FirstOrDefaultAsync(ct);

            var metadataString = cmd.Metadata.GetRawText();
            var normalizedDate = HashService.NormalizeDate(cmd.EventDate);
            var sha256Hash = HashService.ComputeEventHash(
                originalEvent.EventType, normalizedDate, originalEvent.BatchId,
                cmd.Location, cmd.ActorName, cmd.SmelterId,
                cmd.Description, metadataString, previousHash);

            var entity = new CustodyEventEntity
            {
                Id = Guid.NewGuid(),
                BatchId = originalEvent.BatchId,
                TenantId = user.TenantId,
                EventType = originalEvent.EventType,
                IdempotencyKey = idempotencyKey,
                EventDate = DateTime.Parse(cmd.EventDate).ToUniversalTime(),
                Location = cmd.Location,
                GpsCoordinates = cmd.GpsCoordinates,
                ActorName = cmd.ActorName,
                SmelterId = cmd.SmelterId,
                Description = cmd.Description,
                Metadata = cmd.Metadata,
                SchemaVersion = 1,
                IsCorrection = true,
                CorrectsEventId = originalEvent.Id,
                Sha256Hash = sha256Hash,
                PreviousEventHash = previousHash,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow,
            };

            db.CustodyEvents.Add(entity);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(
                entity.Id, entity.BatchId, entity.EventType,
                true, entity.CorrectsEventId,
                entity.Sha256Hash, entity.CreatedAt));
        }
    }
}
```

- [ ] **Step 3: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "CreateCorrectionTests"
```

Expected: 1 test passes.

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/CustodyEvents/CreateCorrection.cs packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/CreateCorrectionTests.cs
git commit -m "feat: add CreateCorrection command (FR-P003 event correction/amendment)"
```

---

### Task 9: CustodyEvent Queries (Get, List) and Integrity Verification

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/CustodyEvents/GetCustodyEvent.cs`
- Create: `packages/api/src/Tungsten.Api/Features/CustodyEvents/ListCustodyEvents.cs`
- Create: `packages/api/src/Tungsten.Api/Features/CustodyEvents/VerifyIntegrity.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/VerifyIntegrityTests.cs`

- [ ] **Step 1: Implement GetCustodyEvent**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.CustodyEvents;

public static class GetCustodyEvent
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        Guid BatchId,
        string EventType,
        DateTime EventDate,
        string Location,
        string? GpsCoordinates,
        string ActorName,
        string? SmelterId,
        string Description,
        bool IsCorrection,
        Guid? CorrectsEventId,
        string Sha256Hash,
        string? PreviousEventHash,
        DateTime CreatedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var evt = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.Id == query.Id && e.TenantId == user.TenantId)
                .Select(e => new Response(
                    e.Id, e.BatchId, e.EventType, e.EventDate,
                    e.Location, e.GpsCoordinates, e.ActorName, e.SmelterId,
                    e.Description, e.IsCorrection, e.CorrectsEventId,
                    e.Sha256Hash, e.PreviousEventHash, e.CreatedAt))
                .FirstOrDefaultAsync(ct);

            return evt is null
                ? Result<Response>.Failure("Event not found")
                : Result<Response>.Success(evt);
        }
    }
}
```

- [ ] **Step 2: Implement ListCustodyEvents**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Pagination;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.CustodyEvents;

public static class ListCustodyEvents
{
    public record Query(Guid BatchId, int Page = 1, int PageSize = 20) : IRequest<Result<PagedResponse<EventItem>>>;

    public record EventItem(
        Guid Id,
        string EventType,
        DateTime EventDate,
        string Location,
        string ActorName,
        bool IsCorrection,
        string Sha256Hash,
        DateTime CreatedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<PagedResponse<EventItem>>>
    {
        public async Task<Result<PagedResponse<EventItem>>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<PagedResponse<EventItem>>.Failure("User not found");

            var baseQuery = db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId && e.TenantId == user.TenantId);

            var totalCount = await baseQuery.CountAsync(ct);

            var paged = new PagedRequest(query.Page, query.PageSize);
            var items = await baseQuery
                .OrderBy(e => e.CreatedAt)
                .Skip(paged.Skip)
                .Take(paged.PageSize)
                .Select(e => new EventItem(
                    e.Id, e.EventType, e.EventDate, e.Location,
                    e.ActorName, e.IsCorrection, e.Sha256Hash, e.CreatedAt))
                .ToListAsync(ct);

            return Result<PagedResponse<EventItem>>.Success(
                new PagedResponse<EventItem>(items, totalCount, query.Page, query.PageSize));
        }
    }
}
```

- [ ] **Step 2b: Write GetCustodyEvent and ListCustodyEvents tests**

Create `packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/GetCustodyEventTests.cs`:
```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.CustodyEvents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class GetCustodyEventTests
{
    [Fact]
    public async Task Handle_ExistingEvent_ReturnsEvent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 500, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);
        var evt = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.UtcNow, Location = "Bisie", ActorName = "Corp",
            Description = "Test", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new GetCustodyEvent.Handler(db, currentUser);
        var result = await handler.Handle(new GetCustodyEvent.Query(evt.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be("MINE_EXTRACTION");
    }

    [Fact]
    public async Task Handle_CrossTenantAccess_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantA = new TenantEntity { Id = Guid.NewGuid(), Name = "A", SchemaPrefix = "a", Status = "ACTIVE" };
        var tenantB = new TenantEntity { Id = Guid.NewGuid(), Name = "B", SchemaPrefix = "b", Status = "ACTIVE" };
        db.Tenants.AddRange(tenantA, tenantB);

        var userA = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|a", Email = "a@test.com",
            DisplayName = "A", Role = "SUPPLIER", TenantId = tenantA.Id, IsActive = true
        };
        var userB = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|b", Email = "b@test.com",
            DisplayName = "B", Role = "SUPPLIER", TenantId = tenantB.Id, IsActive = true
        };
        db.Users.AddRange(userA, userB);

        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantA.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 500, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = userA.Id
        };
        db.Batches.Add(batch);
        var evt = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenantA.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.UtcNow, Location = "Bisie", ActorName = "Corp",
            Description = "Test", Sha256Hash = new string('a', 64),
            CreatedBy = userA.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        db.SaveChanges();

        // User B tries to access tenant A's event
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(userB.Auth0Sub);

        var handler = new GetCustodyEvent.Handler(db, currentUser);
        var result = await handler.Handle(new GetCustodyEvent.Query(evt.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
```

- [ ] **Step 3: Write VerifyIntegrity tests**

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.CustodyEvents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class VerifyIntegrityTests
{
    [Fact]
    public async Task Handle_ValidChain_ReturnsIntact()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 500, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);

        // Build a valid chain manually
        var hash1 = HashService.ComputeEventHash("MINE_EXTRACTION",
            HashService.NormalizeDate("2026-01-15T10:00:00Z"),
            batch.Id, "Bisie", "Corp", null, "First", "{}", null);

        db.CustodyEvents.Add(new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.Parse("2026-01-15T10:00:00Z").ToUniversalTime(),
            Location = "Bisie", ActorName = "Corp", Description = "First",
            Sha256Hash = hash1, PreviousEventHash = null,
            CreatedBy = user.Id, CreatedAt = DateTime.Parse("2026-01-15T10:00:00Z").ToUniversalTime()
        });

        var hash2 = HashService.ComputeEventHash("CONCENTRATION",
            HashService.NormalizeDate("2026-01-16T10:00:00Z"),
            batch.Id, "Facility", "Processor", null, "Second", "{}", hash1);

        db.CustodyEvents.Add(new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "CONCENTRATION", IdempotencyKey = "k2",
            EventDate = DateTime.Parse("2026-01-16T10:00:00Z").ToUniversalTime(),
            Location = "Facility", ActorName = "Processor", Description = "Second",
            Sha256Hash = hash2, PreviousEventHash = hash1,
            CreatedBy = user.Id, CreatedAt = DateTime.Parse("2026-01-16T10:00:00Z").ToUniversalTime()
        });

        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new VerifyIntegrity.Handler(db, currentUser);
        var result = await handler.Handle(new VerifyIntegrity.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeTrue();
        result.Value.EventCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_TamperedHash_ReturnsNotIntact()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 500, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);

        // Create event with a WRONG hash (simulating tampering)
        db.CustodyEvents.Add(new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.Parse("2026-01-15T10:00:00Z").ToUniversalTime(),
            Location = "Bisie", ActorName = "Corp", Description = "First",
            Sha256Hash = "tampered_hash_" + new string('0', 50),  // 64 chars total
            PreviousEventHash = null,
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new VerifyIntegrity.Handler(db, currentUser);
        var result = await handler.Handle(new VerifyIntegrity.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeFalse();
        result.Value.FirstTamperedEventId.Should().NotBeNull();
    }
}
```

- [ ] **Step 4: Implement VerifyIntegrity**

```csharp
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.CustodyEvents;

public static class VerifyIntegrity
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record Response(
        bool IsIntact,
        int EventCount,
        Guid? FirstTamperedEventId,
        string? TamperDetail);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId && e.TenantId == user.TenantId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);

            if (events.Count == 0)
                return Result<Response>.Success(new Response(true, 0, null, null));

            string? previousHash = null;
            foreach (var evt in events)
            {
                var metadataString = evt.Metadata.HasValue
                    ? evt.Metadata.Value.GetRawText()
                    : "{}";

                var expectedHash = HashService.ComputeEventHash(
                    evt.EventType,
                    HashService.NormalizeDate(evt.EventDate),
                    evt.BatchId,
                    evt.Location,
                    evt.ActorName,
                    evt.SmelterId,
                    evt.Description,
                    metadataString,
                    previousHash);

                if (evt.Sha256Hash != expectedHash)
                {
                    return Result<Response>.Success(new Response(
                        false, events.Count, evt.Id,
                        $"Hash mismatch at event {evt.Id}: expected {expectedHash[..16]}..., got {evt.Sha256Hash[..16]}..."));
                }

                if (evt.PreviousEventHash != previousHash)
                {
                    return Result<Response>.Success(new Response(
                        false, events.Count, evt.Id,
                        $"Chain break at event {evt.Id}: previous hash mismatch"));
                }

                previousHash = evt.Sha256Hash;
            }

            return Result<Response>.Success(new Response(true, events.Count, null, null));
        }
    }
}
```

- [ ] **Step 5: Run tests**

```bash
cd /c/__edMVP/packages/api && dotnet test --filter "VerifyIntegrityTests"
```

Expected: 2 tests pass.

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/CustodyEvents/ packages/api/tests/Tungsten.Api.Tests/Features/CustodyEvents/
git commit -m "feat: add GetCustodyEvent, ListCustodyEvents, and VerifyIntegrity (hash chain verification)"
```

---

### Task 10: CustodyEvent Endpoints Wiring

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/CustodyEvents/CustodyEventEndpoints.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Create CustodyEventEndpoints**

```csharp
using System.Text.Json;
using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.CustodyEvents;

public static class CustodyEventEndpoints
{
    public static IEndpointRouteBuilder MapCustodyEventEndpoints(this IEndpointRouteBuilder app)
    {
        // Events nested under batches
        var batchEvents = app.MapGroup("/api/batches/{batchId:guid}/events").RequireAuthorization();

        batchEvents.MapGet("/", async (Guid batchId, int? page, int? pageSize, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListCustodyEvents.Query(batchId, page ?? 1, pageSize ?? 20));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        batchEvents.MapPost("/", async (Guid batchId, CreateCustodyEvent.Command command, IMediator mediator) =>
        {
            // Override batchId from route
            var cmd = command with { BatchId = batchId };
            var result = await mediator.Send(cmd);
            if (result.IsSuccess)
                return Results.Created($"/api/events/{result.Value.Id}", result.Value);
            if (result.Error?.Contains("Duplicate") == true)
                return Results.Conflict(new { error = result.Error });
            return Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireSupplier);

        // Event detail
        var events = app.MapGroup("/api/events").RequireAuthorization();

        events.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCustodyEvent.Query(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        });

        // Corrections
        events.MapPost("/{eventId:guid}/corrections", async (Guid eventId, CreateCorrection.Command command, IMediator mediator) =>
        {
            var cmd = command with { OriginalEventId = eventId };
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/events/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireSupplier);

        // Integrity verification
        app.MapGet("/api/batches/{batchId:guid}/verify-integrity", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new VerifyIntegrity.Query(batchId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        return app;
    }
}
```

- [ ] **Step 2: Register in Program.cs**

Add before `app.Run();`:
```csharp
app.MapCustodyEventEndpoints();
```

Add using:
```csharp
using Tungsten.Api.Features.CustodyEvents;
```

- [ ] **Step 3: Build and run all unit tests**

```bash
cd /c/__edMVP/packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

Expected: Build succeeds. All unit tests pass (7 from Phase 1 + new ones from Phase 2).

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/CustodyEvents/CustodyEventEndpoints.cs packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: wire custody event endpoints (create, get, list, corrections, verify-integrity)"
```

---

## Summary

**Phase 2 delivers:**
1. Pagination infrastructure (PagedRequest, PagedResponse)
2. Batch CRUD: POST /api/batches, GET /api/batches/{id}, GET /api/batches (role-based filtering)
3. HashService: SHA-256 canonical event hashing
4. IdempotencyKeyService: deterministic key from event fields (FR-P002)
5. MetadataValidator: per-event-type mandatory field validation
6. CustodyEvent creation with hash chain, idempotency, and metadata validation
7. Event corrections (FR-P003): linked correction records
8. GetCustodyEvent and ListCustodyEvents queries
9. VerifyIntegrity: hash chain recomputation and tamper detection
10. All endpoints wired in Program.cs with proper authorization

**Tests:** ~20+ unit tests covering all handlers, validators, hash computation, chain verification, idempotency, and corrections.

**Next plan:** Phase 3 — Compliance Engine (RMAP + OECD DDG checkers)
