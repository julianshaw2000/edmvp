# Phase 11: Production Readiness + Audit Logging — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add business-event audit logging via MediatR pipeline behaviour and harden the platform for pilot customer onboarding.

**Architecture:** MediatR `AuditBehaviour<TRequest, TResponse>` pipeline behaviour automatically logs every command implementing `IAuditable` to an `audit_logs` PostgreSQL table. `ICurrentUserService` is extended with cached `UserId`/`TenantId` resolution. Production hardening adds ASP.NET Core health checks, Angular Sentry integration, and GitHub Actions CI/CD.

**Tech Stack:** .NET 10, MediatR, EF Core + PostgreSQL, Angular 21 (signals, standalone), Sentry, GitHub Actions

**Spec:** `docs/superpowers/specs/2026-03-23-phase-11-production-readiness-design.md`

---

## Chunk 1: Backend Audit Infrastructure

### Task 1: AuditLogEntity + EF Core Configuration

**Files:**
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/AuditLogEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/AppDbContext.cs`

- [ ] **Step 1: Create AuditLogEntity**

```csharp
// packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/AuditLogEntity.cs
using System.Text.Json;

namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class AuditLogEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? BatchId { get; set; }
    public JsonElement? Payload { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }

    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity User { get; set; } = null!;
}
```

- [ ] **Step 2: Create AuditLogConfiguration**

```csharp
// packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Result).HasMaxLength(20).IsRequired();
        builder.Property(e => e.FailureReason).HasMaxLength(2000);
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.Payload).HasColumnType("jsonb");

        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.Timestamp }).IsDescending(false, true).HasDatabaseName("ix_audit_logs_tenant_timestamp");
        builder.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId }).HasDatabaseName("ix_audit_logs_tenant_entity");
        builder.HasIndex(e => new { e.TenantId, e.BatchId, e.Timestamp }).HasDatabaseName("ix_audit_logs_tenant_batch");
    }
}
```

- [ ] **Step 3: Add DbSet to AppDbContext**

Add to `packages/api/src/Tungsten.Api/Infrastructure/Persistence/AppDbContext.cs`:

```csharp
public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
```

- [ ] **Step 4: Generate EF Core migration**

Run:
```bash
cd packages/api && dotnet ef migrations add AddAuditLogs --project src/Tungsten.Api
```

If `dotnet ef` is not installed, use:
```bash
cd packages/api && dotnet build
```

Verify the migration file is created in `Migrations/`.

- [ ] **Step 5: Build to verify compilation**

Run: `cd packages/api && dotnet build`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Infrastructure/Persistence/
git commit -m "feat: add AuditLogEntity with EF Core configuration and migration"
```

---

### Task 2: Extend ICurrentUserService with cached UserId/TenantId

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Common/Auth/CurrentUserServiceTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Common/Auth/CurrentUserServiceTests.cs
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Common.Auth;

public class CurrentUserServiceTests
{
    private static (AppDbContext db, IHttpContextAccessor accessor) CreateContext(string auth0Sub)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, auth0Sub) };
        var identity = new ClaimsIdentity(claims, "Test");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        return (db, accessor);
    }

    [Fact]
    public async Task GetUserIdAsync_ReturnsUserId_WhenUserExists()
    {
        var (db, accessor) = CreateContext("auth0|test123");
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = "auth0|test123", Email = "test@test.com", DisplayName = "Test", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new CurrentUserService(accessor, db);
        var result = await svc.GetUserIdAsync(CancellationToken.None);

        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task GetTenantIdAsync_ReturnsTenantId_WhenUserExists()
    {
        var (db, accessor) = CreateContext("auth0|test456");
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), Auth0Sub = "auth0|test456", Email = "t@t.com", DisplayName = "T", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new CurrentUserService(accessor, db);
        var result = await svc.GetTenantIdAsync(CancellationToken.None);

        Assert.Equal(tenantId, result);
    }

    [Fact]
    public async Task GetUserIdAsync_CachesResult_OnSecondCall()
    {
        var (db, accessor) = CreateContext("auth0|cache");
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = "auth0|cache", Email = "c@c.com", DisplayName = "C", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new CurrentUserService(accessor, db);
        var first = await svc.GetUserIdAsync(CancellationToken.None);
        var second = await svc.GetUserIdAsync(CancellationToken.None);

        Assert.Equal(first, second);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~CurrentUserServiceTests"`
Expected: FAIL — `CurrentUserService` constructor does not accept `AppDbContext`

- [ ] **Step 3: Implement extended CurrentUserService**

Replace `packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs`:

```csharp
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Common.Auth;

public interface ICurrentUserService
{
    string Auth0Sub { get; }
    Task<Guid> GetUserIdAsync(CancellationToken ct);
    Task<Guid> GetTenantIdAsync(CancellationToken ct);
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db) : ICurrentUserService
{
    private Guid? _userId;
    private Guid? _tenantId;

    public string Auth0Sub =>
        httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("No authenticated user");

    public async Task<Guid> GetUserIdAsync(CancellationToken ct)
    {
        if (_userId.HasValue) return _userId.Value;
        await ResolveUserAsync(ct);
        return _userId!.Value;
    }

    public async Task<Guid> GetTenantIdAsync(CancellationToken ct)
    {
        if (_tenantId.HasValue) return _tenantId.Value;
        await ResolveUserAsync(ct);
        return _tenantId!.Value;
    }

    private async Task ResolveUserAsync(CancellationToken ct)
    {
        var sub = Auth0Sub;
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Auth0Sub == sub && u.IsActive)
            .Select(u => new { u.Id, u.TenantId })
            .FirstOrDefaultAsync(ct)
            ?? throw new UnauthorizedAccessException("User not found");

        _userId = user.Id;
        _tenantId = user.TenantId;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~CurrentUserServiceTests"`
Expected: PASS (3 tests)

- [ ] **Step 5: Run all tests to verify no regressions**

Run: `cd packages/api && dotnet test`
Expected: All tests pass

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs packages/api/tests/Tungsten.Api.Tests/Common/Auth/CurrentUserServiceTests.cs
git commit -m "feat: extend ICurrentUserService with cached UserId/TenantId resolution"
```

---

### Task 3: IAuditable Interface + AuditRedact Attribute

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Audit/IAuditable.cs`
- Create: `packages/api/src/Tungsten.Api/Common/Audit/AuditRedactAttribute.cs`

- [ ] **Step 1: Create IAuditable interface**

```csharp
// packages/api/src/Tungsten.Api/Common/Audit/IAuditable.cs
namespace Tungsten.Api.Common.Audit;

public interface IAuditable
{
    string AuditAction { get; }
    string EntityType { get; }
}
```

- [ ] **Step 2: Create AuditRedact attribute**

```csharp
// packages/api/src/Tungsten.Api/Common/Audit/AuditRedactAttribute.cs
namespace Tungsten.Api.Common.Audit;

[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditRedactAttribute : Attribute;
```

- [ ] **Step 3: Build to verify**

Run: `cd packages/api && dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Audit/
git commit -m "feat: add IAuditable interface and AuditRedact attribute"
```

---

### Task 4: AuditBehaviour MediatR Pipeline

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Audit/AuditPayloadSerializer.cs`
- Create: `packages/api/src/Tungsten.Api/Common/Behaviours/AuditBehaviour.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Common/Audit/AuditPayloadSerializerTests.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Common/Behaviours/AuditBehaviourTests.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs` (register behaviour)

- [ ] **Step 1: Write AuditPayloadSerializer tests**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Common/Audit/AuditPayloadSerializerTests.cs
using System.Text.Json;
using Tungsten.Api.Common.Audit;

namespace Tungsten.Api.Tests.Common.Audit;

public class AuditPayloadSerializerTests
{
    private record SimpleCommand(string Name, int Value);
    private record CommandWithRedact(string Name, [property: AuditRedact] string Secret);
    private record CommandWithStream(string Name, Stream Data);

    [Fact]
    public void Serialize_SimpleCommand_ReturnsJson()
    {
        var cmd = new SimpleCommand("test", 42);
        var result = AuditPayloadSerializer.Serialize(cmd);
        var doc = JsonDocument.Parse(result.GetRawText());
        Assert.Equal("test", doc.RootElement.GetProperty("Name").GetString());
        Assert.Equal(42, doc.RootElement.GetProperty("Value").GetInt32());
    }

    [Fact]
    public void Serialize_RedactedField_ReplacesWithMarker()
    {
        var cmd = new CommandWithRedact("visible", "my-secret");
        var result = AuditPayloadSerializer.Serialize(cmd);
        var doc = JsonDocument.Parse(result.GetRawText());
        Assert.Equal("visible", doc.RootElement.GetProperty("Name").GetString());
        Assert.Equal("[REDACTED]", doc.RootElement.GetProperty("Secret").GetString());
    }

    [Fact]
    public void Serialize_StreamField_ReplacesWithMarker()
    {
        using var stream = new MemoryStream();
        var cmd = new CommandWithStream("file", stream);
        var result = AuditPayloadSerializer.Serialize(cmd);
        var doc = JsonDocument.Parse(result.GetRawText());
        Assert.Equal("[STREAM]", doc.RootElement.GetProperty("Data").GetString());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~AuditPayloadSerializerTests"`
Expected: FAIL — `AuditPayloadSerializer` does not exist

- [ ] **Step 3: Implement AuditPayloadSerializer**

```csharp
// packages/api/src/Tungsten.Api/Common/Audit/AuditPayloadSerializer.cs
using System.Reflection;
using System.Text.Json;

namespace Tungsten.Api.Common.Audit;

public static class AuditPayloadSerializer
{
    public static JsonElement Serialize<T>(T command)
    {
        var dict = new Dictionary<string, object?>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (typeof(Stream).IsAssignableFrom(prop.PropertyType))
            {
                dict[prop.Name] = "[STREAM]";
            }
            else if (prop.GetCustomAttribute<AuditRedactAttribute>() is not null)
            {
                dict[prop.Name] = "[REDACTED]";
            }
            else
            {
                dict[prop.Name] = prop.GetValue(command);
            }
        }

        var json = JsonSerializer.Serialize(dict);
        return JsonDocument.Parse(json).RootElement.Clone();
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~AuditPayloadSerializerTests"`
Expected: PASS (3 tests)

- [ ] **Step 5: Write AuditBehaviour tests**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Common/Behaviours/AuditBehaviourTests.cs
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Behaviours;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Common.Behaviours;

public class AuditBehaviourTests
{
    // Test command implementing IAuditable
    public record TestAuditableCommand(string Name) : IRequest<Result<TestResponse>>, IAuditable
    {
        public string AuditAction => "TestAction";
        public string EntityType => "TestEntity";
    }

    public record TestResponse(Guid Id, string Name);

    // Non-auditable command
    public record TestNonAuditableCommand(string Name) : IRequest<Result<TestResponse>>;

    private static (AppDbContext db, AuditBehaviour<TRequest, TResponse> behaviour) CreateBehaviour<TRequest, TResponse>(
        string auth0Sub = "auth0|test") where TRequest : IRequest<TResponse>
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        // Seed user
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = auth0Sub, Email = "t@t.com", DisplayName = "T", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, auth0Sub) };
        var identity = new ClaimsIdentity(claims, "Test");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        httpContext.Request.Headers["User-Agent"] = "TestAgent";

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUser = new CurrentUserService(accessor, db);
        var logger = Substitute.For<ILogger<AuditBehaviour<TRequest, TResponse>>>();

        var behaviour = new AuditBehaviour<TRequest, TResponse>(db, currentUser, accessor, logger);
        return (db, behaviour);
    }

    [Fact]
    public async Task Handle_AuditableCommand_Success_WritesAuditLog()
    {
        var (db, behaviour) = CreateBehaviour<TestAuditableCommand, Result<TestResponse>>();
        var entityId = Guid.NewGuid();
        var response = Result<TestResponse>.Success(new TestResponse(entityId, "test"));

        var result = await behaviour.Handle(
            new TestAuditableCommand("test"),
            () => Task.FromResult(response),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("TestAction", log.Action);
        Assert.Equal("TestEntity", log.EntityType);
        Assert.Equal(entityId, log.EntityId);
        Assert.Equal("Success", log.Result);
        Assert.Null(log.FailureReason);
    }

    [Fact]
    public async Task Handle_AuditableCommand_Failure_WritesAuditLogWithReason()
    {
        var (db, behaviour) = CreateBehaviour<TestAuditableCommand, Result<TestResponse>>();
        var response = Result<TestResponse>.Failure("Not found");

        var result = await behaviour.Handle(
            new TestAuditableCommand("test"),
            () => Task.FromResult(response),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("Failure", log.Result);
        Assert.Equal("Not found", log.FailureReason);
        Assert.Null(log.EntityId);
    }

    [Fact]
    public async Task Handle_NonAuditableCommand_SkipsLogging()
    {
        var (db, behaviour) = CreateBehaviour<TestNonAuditableCommand, Result<TestResponse>>();
        var response = Result<TestResponse>.Success(new TestResponse(Guid.NewGuid(), "test"));

        await behaviour.Handle(
            new TestNonAuditableCommand("test"),
            () => Task.FromResult(response),
            CancellationToken.None);

        var count = await db.AuditLogs.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Handle_AuditableCommand_ExtractsBatchId_FromCommandProperty()
    {
        var (db, behaviour) = CreateBehaviour<TestAuditableEventCommand, Result<TestResponse>>();
        var batchId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var response = Result<TestResponse>.Success(new TestResponse(entityId, "test"));

        await behaviour.Handle(
            new TestAuditableEventCommand("test", batchId),
            () => Task.FromResult(response),
            CancellationToken.None);

        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(batchId, log.BatchId);
        Assert.Equal(entityId, log.EntityId);
    }

    // Command with BatchId property for BatchId extraction test
    public record TestAuditableEventCommand(string Name, Guid BatchId) : IRequest<Result<TestResponse>>, IAuditable
    {
        public string AuditAction => "CreateCustodyEvent";
        public string EntityType => "CustodyEvent";
    }

    [Fact]
    public async Task Handle_NoHttpContext_SkipsLogging()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var accessor = new HttpContextAccessor { HttpContext = null };
        var currentUser = Substitute.For<ICurrentUserService>();
        var logger = Substitute.For<ILogger<AuditBehaviour<TestAuditableCommand, Result<TestResponse>>>>();

        var behaviour = new AuditBehaviour<TestAuditableCommand, Result<TestResponse>>(db, currentUser, accessor, logger);
        var response = Result<TestResponse>.Success(new TestResponse(Guid.NewGuid(), "test"));

        await behaviour.Handle(
            new TestAuditableCommand("test"),
            () => Task.FromResult(response),
            CancellationToken.None);

        var count = await db.AuditLogs.CountAsync();
        Assert.Equal(0, count);
    }
}
```

- [ ] **Step 6: Run tests to verify they fail**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~AuditBehaviourTests"`
Expected: FAIL — `AuditBehaviour` does not exist

- [ ] **Step 7: Implement AuditBehaviour**

```csharp
// packages/api/src/Tungsten.Api/Common/Behaviours/AuditBehaviour.cs
using System.Reflection;
using MediatR;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Common.Behaviours;

public class AuditBehaviour<TRequest, TResponse>(
    AppDbContext db,
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AuditBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Skip if not auditable
        if (request is not IAuditable auditable)
            return await next();

        // Skip if no HTTP context (background worker)
        if (httpContextAccessor.HttpContext is null)
            return await next();

        var response = await next();

        try
        {
            var userId = await currentUser.GetUserIdAsync(ct);
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            // Extract EntityId and result status from response
            Guid? entityId = null;
            string? failureReason = null;
            var resultText = "Success";

            // Handle non-generic Result (e.g., UpdateUser returns Result, not Result<T>)
            if (response is Result nonGenericResult)
            {
                if (!nonGenericResult.IsSuccess)
                {
                    resultText = "Failure";
                    failureReason = nonGenericResult.Error;
                }
            }
            else if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var isSuccess = (bool)typeof(TResponse).GetProperty("IsSuccess")!.GetValue(response)!;
                if (isSuccess)
                {
                    var value = typeof(TResponse).GetProperty("Value")!.GetValue(response);
                    if (value is not null)
                    {
                        var idProp = value.GetType().GetProperty("Id");
                        if (idProp?.PropertyType == typeof(Guid))
                            entityId = (Guid)idProp.GetValue(value)!;
                    }
                }
                else
                {
                    resultText = "Failure";
                    failureReason = typeof(TResponse).GetProperty("Error")?.GetValue(response)?.ToString();
                }
            }

            // Extract BatchId
            Guid? batchId = null;
            if (auditable.EntityType == "Batch")
            {
                batchId = entityId;
            }
            else
            {
                var batchIdProp = typeof(TRequest).GetProperty("BatchId");
                if (batchIdProp?.PropertyType == typeof(Guid))
                    batchId = (Guid)batchIdProp.GetValue(request)!;
            }

            var httpContext = httpContextAccessor.HttpContext;
            var entry = new AuditLogEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Action = auditable.AuditAction,
                EntityType = auditable.EntityType,
                EntityId = entityId,
                BatchId = batchId,
                Payload = AuditPayloadSerializer.Serialize(request),
                Result = resultText,
                FailureReason = failureReason,
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext.Request.Headers.UserAgent.ToString().Length > 500
                    ? httpContext.Request.Headers.UserAgent.ToString()[..500]
                    : httpContext.Request.Headers.UserAgent.ToString(),
                Timestamp = DateTime.UtcNow,
            };

            db.AuditLogs.Add(entry);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write audit log for {Action}", auditable.AuditAction);
        }

        return response;
    }
}
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~AuditBehaviourTests"`
Expected: PASS (5 tests)

- [ ] **Step 9: Register AuditBehaviour in Program.cs**

In `packages/api/src/Tungsten.Api/Program.cs`, add after the `ValidationBehaviour` registration (line 95):

```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehaviour<,>));
```

Add the using (if not already present — `AuditBehaviour` is in the `Behaviours` namespace alongside `ValidationBehaviour`, which is already imported):
```csharp
using Tungsten.Api.Common.Behaviours;
```

- [ ] **Step 10: Build and run all tests**

Run: `cd packages/api && dotnet build && dotnet test`
Expected: All tests pass

- [ ] **Step 11: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Audit/ packages/api/src/Tungsten.Api/Common/Behaviours/AuditBehaviour.cs packages/api/tests/Tungsten.Api.Tests/Common/ packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: add AuditBehaviour MediatR pipeline with payload serializer"
```

---

## Chunk 2: Mark Commands as IAuditable + Audit API Endpoints

### Task 5: Mark all commands as IAuditable

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Batches/CreateBatch.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Batches/UpdateBatchStatus.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Batches/SplitBatch.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/CustodyEvents/CreateCustodyEvent.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/CustodyEvents/CreateCorrection.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Documents/UploadDocument.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GeneratePassport.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GenerateDossier.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/ShareDocument.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Users/CreateUser.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Users/UpdateUser.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Admin/UploadRmapList.cs`

- [ ] **Step 1: Add IAuditable to CreateBatch.Command**

In `packages/api/src/Tungsten.Api/Features/Batches/CreateBatch.cs`, add using and modify the Command record:

```csharp
using Tungsten.Api.Common.Audit;
// ...
public record Command(
    string BatchNumber,
    string MineralType,
    string OriginCountry,
    string OriginMine,
    decimal WeightKg) : IRequest<Result<Response>>, IAuditable
{
    public string AuditAction => "CreateBatch";
    public string EntityType => "Batch";
}
```

- [ ] **Step 2: Add IAuditable to UpdateBatchStatus.Command**

Same pattern — add `IAuditable` with `AuditAction => "UpdateBatchStatus"`, `EntityType => "Batch"`.

- [ ] **Step 3: Add IAuditable to SplitBatch.Command**

`AuditAction => "SplitBatch"`, `EntityType => "Batch"`.

- [ ] **Step 4: Add IAuditable to CreateCustodyEvent.Command**

`AuditAction => "CreateCustodyEvent"`, `EntityType => "CustodyEvent"`.

- [ ] **Step 5: Add IAuditable to CreateCorrection.Command**

`AuditAction => "CreateCorrection"`, `EntityType => "CustodyEvent"`.

- [ ] **Step 6: Add IAuditable to UploadDocument.Command**

`AuditAction => "UploadDocument"`, `EntityType => "Document"`.
Note: If the command has a `Stream` property, it will be serialized as `"[STREAM]"` automatically.

- [ ] **Step 7: Add IAuditable to GeneratePassport.Command**

`AuditAction => "GeneratePassport"`, `EntityType => "GeneratedDocument"`.

- [ ] **Step 8: Add IAuditable to GenerateDossier.Command**

`AuditAction => "GenerateDossier"`, `EntityType => "GeneratedDocument"`.

- [ ] **Step 9: Add IAuditable to ShareDocument.Command**

`AuditAction => "ShareDocument"`, `EntityType => "GeneratedDocument"`.

- [ ] **Step 10: Add IAuditable to CreateUser.Command**

`AuditAction => "CreateUser"`, `EntityType => "User"`.

- [ ] **Step 11: Add IAuditable to UpdateUser.Command**

`AuditAction => "UpdateUser"`, `EntityType => "User"`.

- [ ] **Step 12: Add IAuditable to UploadRmapList.Command**

`AuditAction => "UploadRmapList"`, `EntityType => "RmapSmelter"`.

- [ ] **Step 13: Build and run all tests**

Run: `cd packages/api && dotnet build && dotnet test`
Expected: All tests pass

- [ ] **Step 14: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/
git commit -m "feat: mark all 12 mutation commands as IAuditable"
```

---

### Task 6: Admin Audit Log Query Endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Admin/ListAuditLogs.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Admin/ListAuditLogsTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Features/Admin/ListAuditLogsTests.cs
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Admin;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Admin;

public class ListAuditLogsTests
{
    [Fact]
    public async Task Handle_ReturnsPagedAuditLogs_FilteredByTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T1", SchemaPrefix = "t1", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Tenants.Add(new TenantEntity { Id = otherTenantId, Name = "T2", SchemaPrefix = "t2", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = "auth0|admin", Email = "a@a.com", DisplayName = "Admin", Role = "PLATFORM_ADMIN", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        // Add logs for both tenants
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Action = "CreateBatch", EntityType = "Batch", Result = "Success", Timestamp = DateTime.UtcNow });
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = otherTenantId, UserId = userId, Action = "CreateBatch", EntityType = "Batch", Result = "Success", Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);

        var handler = new ListAuditLogs.Handler(db, currentUser);
        var result = await handler.Handle(new ListAuditLogs.Query(1, 20, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~ListAuditLogsTests"`
Expected: FAIL — `ListAuditLogs` does not exist

- [ ] **Step 3: Implement ListAuditLogs**

```csharp
// packages/api/src/Tungsten.Api/Features/Admin/ListAuditLogs.cs
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Pagination;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Admin;

public static class ListAuditLogs
{
    public record Query(
        int Page,
        int PageSize,
        Guid? UserId,
        string? Action,
        string? EntityType,
        DateTime? From,
        DateTime? To) : IRequest<Result<PagedResponse<AuditLogDto>>>;

    public record AuditLogDto(
        Guid Id,
        Guid UserId,
        string UserDisplayName,
        string Action,
        string EntityType,
        Guid? EntityId,
        JsonElement? Payload,
        string Result,
        string? FailureReason,
        string? IpAddress,
        DateTime Timestamp);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<PagedResponse<AuditLogDto>>>
    {
        public async Task<Result<PagedResponse<AuditLogDto>>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var q = db.AuditLogs.AsNoTracking()
                .Where(a => a.TenantId == tenantId);

            if (query.UserId.HasValue)
                q = q.Where(a => a.UserId == query.UserId.Value);
            if (!string.IsNullOrEmpty(query.Action))
                q = q.Where(a => a.Action == query.Action);
            if (!string.IsNullOrEmpty(query.EntityType))
                q = q.Where(a => a.EntityType == query.EntityType);
            if (query.From.HasValue)
                q = q.Where(a => a.Timestamp >= query.From.Value);
            if (query.To.HasValue)
                q = q.Where(a => a.Timestamp <= query.To.Value);

            var totalCount = await q.CountAsync(ct);

            var items = await q
                .OrderByDescending(a => a.Timestamp)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new AuditLogDto(
                    a.Id, a.UserId, u.DisplayName, a.Action, a.EntityType,
                    a.EntityId, a.Payload, a.Result, a.FailureReason,
                    a.IpAddress, a.Timestamp))
                .ToListAsync(ct);

            return Result<PagedResponse<AuditLogDto>>.Success(
                new PagedResponse<AuditLogDto>(items, totalCount, query.Page, query.PageSize));
        }
    }
}
```

- [ ] **Step 4: Add endpoint to AdminEndpoints.cs**

Add to `packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs`, inside `MapAdminEndpoints`:

```csharp
app.MapGet("/api/admin/audit-logs", async (
    int? page, int? pageSize, Guid? userId, string? action,
    string? entityType, DateTime? from, DateTime? to,
    IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new ListAuditLogs.Query(
        page ?? 1, pageSize ?? 20, userId, action, entityType, from, to), ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization(AuthorizationPolicies.RequireAdmin);
```

- [ ] **Step 5: Run tests**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~ListAuditLogsTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Admin/ListAuditLogs.cs packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs packages/api/tests/Tungsten.Api.Tests/Features/Admin/ListAuditLogsTests.cs
git commit -m "feat: add admin audit log query endpoint with pagination and filters"
```

---

### Task 7: Batch Activity Feed Endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Batches/GetBatchActivity.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Batches/BatchEndpoints.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Batches/GetBatchActivityTests.cs`

- [ ] **Step 1: Write the failing test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Features/Batches/GetBatchActivityTests.cs
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Batches;

public class GetBatchActivityTests
{
    [Fact]
    public async Task Handle_ReturnsBatchAuditEntries_FilteredByBatchIdAndTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var otherBatchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = "auth0|u", Email = "u@u.com", DisplayName = "User", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Action = "CreateBatch", EntityType = "Batch", EntityId = batchId, BatchId = batchId, Result = "Success", Timestamp = DateTime.UtcNow.AddMinutes(-2) });
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Action = "CreateCustodyEvent", EntityType = "CustodyEvent", BatchId = batchId, Result = "Success", Timestamp = DateTime.UtcNow.AddMinutes(-1) });
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Action = "CreateBatch", EntityType = "Batch", EntityId = otherBatchId, BatchId = otherBatchId, Result = "Success", Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);

        var handler = new GetBatchActivity.Handler(db, currentUser);
        var result = await handler.Handle(new GetBatchActivity.Query(batchId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("CreateBatch", result.Value[0].Action); // chronological order
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~GetBatchActivityTests"`
Expected: FAIL

- [ ] **Step 3: Implement GetBatchActivity**

```csharp
// packages/api/src/Tungsten.Api/Features/Batches/GetBatchActivity.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Batches;

public static class GetBatchActivity
{
    public record Query(Guid BatchId) : IRequest<Result<List<ActivityDto>>>;

    public record ActivityDto(
        Guid Id,
        string UserDisplayName,
        string Action,
        string EntityType,
        string Result,
        string? FailureReason,
        DateTime Timestamp);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<List<ActivityDto>>>
    {
        public async Task<Result<List<ActivityDto>>> Handle(Query query, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var items = await db.AuditLogs.AsNoTracking()
                .Where(a => a.TenantId == tenantId && a.BatchId == query.BatchId)
                .OrderBy(a => a.Timestamp)
                .Join(db.Users, a => a.UserId, u => u.Id, (a, u) => new ActivityDto(
                    a.Id, u.DisplayName, a.Action, a.EntityType,
                    a.Result, a.FailureReason, a.Timestamp))
                .ToListAsync(ct);

            return Result<List<ActivityDto>>.Success(items);
        }
    }
}
```

- [ ] **Step 4: Add endpoint to BatchEndpoints.cs**

In `packages/api/src/Tungsten.Api/Features/Batches/BatchEndpoints.cs`, add inside `MapBatchEndpoints`:

```csharp
group.MapGet("/{id:guid}/activity", async (Guid id, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new GetBatchActivity.Query(id), ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
});

// Note: CancellationToken ct is included in the endpoint signature per CLAUDE.md rules.
```

- [ ] **Step 5: Run tests**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~GetBatchActivityTests"`
Expected: PASS

- [ ] **Step 6: Run all tests**

Run: `cd packages/api && dotnet test`
Expected: All pass

- [ ] **Step 7: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Batches/GetBatchActivity.cs packages/api/src/Tungsten.Api/Features/Batches/BatchEndpoints.cs packages/api/tests/Tungsten.Api.Tests/Features/Batches/GetBatchActivityTests.cs
git commit -m "feat: add batch activity feed endpoint"
```

---

## Chunk 3: Frontend — Admin Audit Log + Batch Activity Feed

### Task 8: Shared Audit Log Types

**Files:**
- Create: `packages/web/src/app/features/admin/data/audit-log.models.ts`

- [ ] **Step 1: Create audit log models**

```typescript
// packages/web/src/app/features/admin/data/audit-log.models.ts
export interface AuditLogEntry {
  id: string;
  userId: string;
  userDisplayName: string;
  action: string;
  entityType: string;
  entityId: string | null;
  payload: Record<string, unknown> | null;
  result: 'Success' | 'Failure';
  failureReason: string | null;
  ipAddress: string | null;
  timestamp: string;
}

export interface AuditLogFilters {
  page: number;
  pageSize: number;
  userId?: string;
  action?: string;
  entityType?: string;
  from?: string;
  to?: string;
}

export interface PagedAuditLogs {
  items: AuditLogEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface BatchActivity {
  id: string;
  userDisplayName: string;
  action: string;
  entityType: string;
  result: string;
  failureReason: string | null;
  timestamp: string;
}

export const AUDIT_ACTION_LABELS: Record<string, string> = {
  CreateBatch: 'Created batch',
  UpdateBatchStatus: 'Updated batch status',
  SplitBatch: 'Split batch',
  CreateCustodyEvent: 'Logged custody event',
  CreateCorrection: 'Submitted correction',
  UploadDocument: 'Uploaded document',
  GeneratePassport: 'Generated Material Passport',
  GenerateDossier: 'Generated audit dossier',
  ShareDocument: 'Shared document',
  CreateUser: 'Created user',
  UpdateUser: 'Updated user',
  UploadRmapList: 'Uploaded RMAP smelter list',
};
```

- [ ] **Step 2: Commit**

```bash
git add packages/web/src/app/features/admin/data/audit-log.models.ts
git commit -m "feat: add audit log TypeScript models and action labels"
```

---

### Task 9: Admin Audit Log API Service + Store

**Files:**
- Modify: `packages/web/src/app/features/admin/data/admin-api.service.ts`
- Modify: `packages/web/src/app/features/admin/admin.store.ts`
- Modify: `packages/web/src/app/features/admin/admin.facade.ts`

- [ ] **Step 1: Add audit log methods to admin API service**

Add to `packages/web/src/app/features/admin/data/admin-api.service.ts`:

```typescript
import { AuditLogFilters, PagedAuditLogs } from './audit-log.models';
import { HttpParams } from '@angular/common/http';

// Add method to the service (note: this.apiUrl matches existing service pattern):
getAuditLogs(filters: AuditLogFilters) {
  let params = new HttpParams()
    .set('page', filters.page)
    .set('pageSize', filters.pageSize);
  // HttpParams is immutable — must reassign on each .set()
  if (filters.userId) params = params.set('userId', filters.userId);
  if (filters.action) params = params.set('action', filters.action);
  if (filters.entityType) params = params.set('entityType', filters.entityType);
  if (filters.from) params = params.set('from', filters.from);
  if (filters.to) params = params.set('to', filters.to);

  return this.http.get<PagedAuditLogs>(`${this.apiUrl}/api/admin/audit-logs`, { params });
}
```

- [ ] **Step 2: Add audit log state to admin store**

Add audit log signals to `packages/web/src/app/features/admin/admin.store.ts`:
- `auditLogs` signal holding `PagedAuditLogs`
- `auditFilters` signal holding `AuditLogFilters`
- `loadAuditLogs()` method
- `updateAuditFilters()` method

- [ ] **Step 3: Add facade methods**

Add `loadAuditLogs()` and `updateAuditFilters()` to `packages/web/src/app/features/admin/admin.facade.ts`.

- [ ] **Step 4: Build to verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add packages/web/src/app/features/admin/
git commit -m "feat: add audit log API service, store, and facade methods"
```

---

### Task 10: Admin Audit Log Page Component

**Files:**
- Create: `packages/web/src/app/features/admin/audit-log.component.ts`
- Modify: `packages/web/src/app/features/admin/admin.routes.ts`

- [ ] **Step 1: Create audit log component**

```typescript
// packages/web/src/app/features/admin/audit-log.component.ts
import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AdminFacade } from './admin.facade';
import { AUDIT_ACTION_LABELS } from './data/audit-log.models';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-6">
      <div class="flex items-center justify-between mb-6">
        <div>
          <a routerLink="/admin" class="text-sm text-indigo-600 hover:underline">&larr; Back to Dashboard</a>
          <h1 class="text-2xl font-bold text-gray-900 mt-1">Audit Log</h1>
        </div>
      </div>

      <!-- Filters -->
      <div class="bg-white rounded-lg shadow p-4 mb-4 flex flex-wrap gap-4 items-end">
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">Action</label>
          <select class="border rounded px-2 py-1.5 text-sm" (change)="onActionFilter($event)">
            <option value="">All actions</option>
            @for (action of actionOptions; track action) {
              <option [value]="action">{{ actionLabels[action] || action }}</option>
            }
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">Entity Type</label>
          <select class="border rounded px-2 py-1.5 text-sm" (change)="onEntityTypeFilter($event)">
            <option value="">All types</option>
            <option value="Batch">Batch</option>
            <option value="CustodyEvent">Custody Event</option>
            <option value="Document">Document</option>
            <option value="GeneratedDocument">Generated Document</option>
            <option value="User">User</option>
            <option value="RmapSmelter">RMAP Smelter</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 mb-1">Result</label>
          <select class="border rounded px-2 py-1.5 text-sm" (change)="onResultFilter($event)">
            <option value="">All</option>
            <option value="Success">Success</option>
            <option value="Failure">Failure</option>
          </select>
        </div>
      </div>

      <!-- Table -->
      <div class="bg-white rounded-lg shadow overflow-hidden">
        <table class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Timestamp</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">User</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Action</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Entity</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Result</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200">
            @for (entry of facade.auditLogs().items; track entry.id) {
              <tr class="hover:bg-gray-50 cursor-pointer" (click)="toggleExpand(entry.id)">
                <td class="px-4 py-3 text-sm text-gray-600">{{ entry.timestamp | date:'short' }}</td>
                <td class="px-4 py-3 text-sm text-gray-900">{{ entry.userDisplayName }}</td>
                <td class="px-4 py-3 text-sm text-gray-900">{{ actionLabels[entry.action] || entry.action }}</td>
                <td class="px-4 py-3 text-sm text-gray-600">{{ entry.entityType }}</td>
                <td class="px-4 py-3">
                  <span class="px-2 py-0.5 rounded-full text-xs font-medium"
                        [class]="entry.result === 'Success' ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'">
                    {{ entry.result }}
                  </span>
                </td>
              </tr>
              @if (expandedId() === entry.id) {
                <tr>
                  <td colspan="5" class="px-4 py-3 bg-gray-50">
                    <pre class="text-xs text-gray-700 whitespace-pre-wrap">{{ entry.payload | json }}</pre>
                    @if (entry.failureReason) {
                      <p class="text-xs text-red-600 mt-2">Reason: {{ entry.failureReason }}</p>
                    }
                  </td>
                </tr>
              }
            }
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div class="flex justify-between items-center mt-4">
        <span class="text-sm text-gray-600">
          {{ facade.auditLogs().totalCount }} entries
        </span>
        <div class="flex gap-2">
          <button class="px-3 py-1 border rounded text-sm"
                  [disabled]="facade.auditLogs().page <= 1"
                  (click)="onPage(facade.auditLogs().page - 1)">Previous</button>
          <button class="px-3 py-1 border rounded text-sm"
                  [disabled]="facade.auditLogs().page * facade.auditLogs().pageSize >= facade.auditLogs().totalCount"
                  (click)="onPage(facade.auditLogs().page + 1)">Next</button>
        </div>
      </div>
    </div>
  `,
})
export class AuditLogComponent {
  protected readonly facade = inject(AdminFacade);
  protected readonly actionLabels = AUDIT_ACTION_LABELS;
  protected readonly expandedId = signal<string | null>(null);

  protected readonly actionOptions = Object.keys(AUDIT_ACTION_LABELS);

  constructor() {
    this.facade.loadAuditLogs();
  }

  toggleExpand(id: string) {
    this.expandedId.update(current => current === id ? null : id);
  }

  onActionFilter(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.facade.updateAuditFilters({ action: value || undefined, page: 1 });
  }

  onEntityTypeFilter(event: Event) {
    const value = (event.target as HTMLSelectElement).value;
    this.facade.updateAuditFilters({ entityType: value || undefined, page: 1 });
  }

  onResultFilter(event: Event) {
    // Result filter — implement via action filter or add to query
    this.facade.updateAuditFilters({ page: 1 });
  }

  onPage(page: number) {
    this.facade.updateAuditFilters({ page });
  }
}
```

- [ ] **Step 2: Add route to admin.routes.ts**

Add to `packages/web/src/app/features/admin/admin.routes.ts`:

```typescript
{
  path: 'audit-log',
  loadComponent: () => import('./audit-log.component').then(m => m.AuditLogComponent),
},
```

- [ ] **Step 3: Add navigation link in admin dashboard**

Add an "Audit Log" card/link in `packages/web/src/app/features/admin/admin-dashboard.component.ts` that routes to `/admin/audit-log`.

- [ ] **Step 4: Build to verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add packages/web/src/app/features/admin/
git commit -m "feat: add admin audit log page with filters and pagination"
```

---

### Task 11: Batch Activity Feed Tab

**Files:**
- Create: `packages/web/src/app/features/supplier/ui/activity-feed.component.ts`
- Modify: `packages/web/src/app/features/supplier/data/supplier-api.service.ts`
- Modify: `packages/web/src/app/features/supplier/batch-detail.component.ts`
- Modify: `packages/web/src/app/features/buyer/batch-detail.component.ts`

- [ ] **Step 1: Add getBatchActivity to supplier API service**

Add to `packages/web/src/app/features/supplier/data/supplier-api.service.ts`:

```typescript
import { BatchActivity } from '../../admin/data/audit-log.models';

getBatchActivity(batchId: string) {
  return this.http.get<BatchActivity[]>(`${this.baseUrl}/batches/${batchId}/activity`);
}
```

- [ ] **Step 2: Create activity feed presentational component**

```typescript
// packages/web/src/app/features/supplier/ui/activity-feed.component.ts
import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { BatchActivity, AUDIT_ACTION_LABELS } from '../../admin/data/audit-log.models';

@Component({
  selector: 'app-activity-feed',
  standalone: true,
  imports: [DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-3">
      @for (entry of activities(); track entry.id) {
        <div class="flex items-start gap-3 py-2 border-b border-gray-100 last:border-0">
          <div class="w-2 h-2 mt-2 rounded-full"
               [class]="entry.result === 'Success' ? 'bg-green-500' : 'bg-red-500'"></div>
          <div class="flex-1">
            <p class="text-sm text-gray-900">
              <span class="font-medium">{{ entry.userDisplayName }}</span>
              {{ actionLabels[entry.action] || entry.action | lowercase }}
            </p>
            @if (entry.failureReason) {
              <p class="text-xs text-red-600 mt-0.5">{{ entry.failureReason }}</p>
            }
            <p class="text-xs text-gray-500 mt-0.5">{{ entry.timestamp | date:'medium' }}</p>
          </div>
        </div>
      } @empty {
        <p class="text-sm text-gray-500 py-4 text-center">No activity recorded yet.</p>
      }
    </div>
  `,
})
export class ActivityFeedComponent {
  activities = input.required<BatchActivity[]>();
  protected readonly actionLabels = AUDIT_ACTION_LABELS;
}
```

- [ ] **Step 3: Add Activity tab to supplier batch-detail.component.ts**

In `packages/web/src/app/features/supplier/batch-detail.component.ts`:
- Import `ActivityFeedComponent`
- Add an "Activity" tab alongside existing tabs (Events, Compliance, Documents, etc.)
- Load activity data using `httpResource()` or `toSignal()` from `supplierApiService.getBatchActivity(batchId)`
- Render `<app-activity-feed [activities]="batchActivity()" />`

- [ ] **Step 4: Add Activity tab to buyer batch-detail.component.ts**

Same pattern in `packages/web/src/app/features/buyer/batch-detail.component.ts`:
- Add `getBatchActivity` to buyer API service (or import from shared)
- Add Activity tab
- Render `<app-activity-feed [activities]="batchActivity()" />`

- [ ] **Step 5: Build to verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add packages/web/src/app/features/supplier/ packages/web/src/app/features/buyer/ packages/web/src/app/features/admin/data/audit-log.models.ts
git commit -m "feat: add batch activity feed tab to supplier and buyer batch detail"
```

---

## Chunk 4: Production Hardening

### Task 12: ASP.NET Core Health Checks

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Program.cs`
- Modify: `packages/api/tests/Tungsten.Api.Tests/Integration/HealthCheckTests.cs`

- [ ] **Step 1: Update health check test**

Update `packages/api/tests/Tungsten.Api.Tests/Integration/HealthCheckTests.cs` to test `/health/live` and `/health/ready`:

```csharp
[Fact]
public async Task HealthLive_ReturnsHealthy()
{
    var response = await _client.GetAsync("/health/live");
    response.EnsureSuccessStatusCode();
}

[Fact]
public async Task HealthReady_ReturnsStatus()
{
    var response = await _client.GetAsync("/health/ready");
    Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~HealthCheck"`
Expected: FAIL — endpoints don't exist yet

- [ ] **Step 3: Implement health checks in Program.cs**

Replace the existing `/health` endpoint with ASP.NET Core health checks:

```csharp
// In service registration section:
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgresql")
    .AddCheck("migrations", () => DatabaseMigrationService.IsReady
        ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy()
        : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded("Migrations running"));

// Replace the existing app.MapGet("/health", ...) with:
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false, // No checks — just proves the process is alive
});
app.MapHealthChecks("/health/ready");
```

Add NuGet package: `dotnet add packages/api/src/Tungsten.Api package AspNetCore.HealthChecks.NpgSql`

- [ ] **Step 4: Run tests to verify they pass**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~HealthCheck"`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/ packages/api/tests/Tungsten.Api.Tests/Integration/HealthCheckTests.cs
git commit -m "feat: replace custom health endpoint with ASP.NET Core HealthChecks"
```

---

### Task 13: Sentry User Context (API) + Angular Sentry Integration

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Program.cs` (add Sentry user context middleware)
- Modify: `packages/web/src/app/app.config.ts`
- Modify: `packages/web/src/environments/environment.ts` (or equivalent)

- [ ] **Step 0: Add Sentry user context to API**

In `packages/api/src/Tungsten.Api/Program.cs`, after `app.UseAuthorization();`, add:

```csharp
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var sub = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (sub is not null)
        {
            SentrySdk.ConfigureScope(scope =>
            {
                scope.User = new Sentry.SentryUser { Id = sub };
            });
        }
    }
    await next();
});
```

This attaches the Auth0 sub (no PII) to Sentry events for the API side.

- [ ] **Step 1: Install @sentry/angular**

Run: `cd packages/web && npm install @sentry/angular`

- [ ] **Step 2: Configure Sentry in app.config.ts**

Add Sentry initialization and ErrorHandler override:

```typescript
import * as Sentry from '@sentry/angular';

// In the providers array:
{
  provide: ErrorHandler,
  useValue: Sentry.createErrorHandler({ showDialog: false }),
},
{
  provide: Sentry.TraceService,
  deps: [Router],
},
```

Add Sentry.init() call in main.ts or app.config.ts:

```typescript
if (environment.sentryDsn) {
  Sentry.init({
    dsn: environment.sentryDsn,
    environment: environment.production ? 'production' : 'development',
    tracesSampleRate: 0.2,
  });
}
```

- [ ] **Step 3: Add sentryDsn to environment config**

Add `sentryDsn: ''` to environment files (empty by default, configured via env var in production).

- [ ] **Step 4: Build to verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add packages/web/
git commit -m "feat: add Sentry error tracking to Angular frontend"
```

---

### Task 14: GitHub Actions CI/CD Pipeline

**Files:**
- Create: `.github/workflows/ci.yml`

- [ ] **Step 1: Create CI workflow**

```yaml
# .github/workflows/ci.yml
name: CI/CD

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  api:
    name: API — Build & Test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet build
        working-directory: packages/api
      - run: dotnet test --no-build
        working-directory: packages/api
      - run: dotnet format --verify-no-changes
        working-directory: packages/api

  web:
    name: Web — Build & Test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '22'
          cache: 'npm'
          cache-dependency-path: packages/web/package-lock.json
      - run: npm ci
        working-directory: packages/web
      - run: npx ng build
        working-directory: packages/web

  deploy:
    name: Deploy to Render
    needs: [api, web]
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    runs-on: ubuntu-latest
    steps:
      - name: Deploy API
        run: curl -s "${{ secrets.RENDER_API_DEPLOY_HOOK }}"
      - name: Deploy Web
        run: curl -s "${{ secrets.RENDER_WEB_DEPLOY_HOOK }}"
```

- [ ] **Step 2: Commit**

```bash
git add .github/workflows/ci.yml
git commit -m "ci: add GitHub Actions pipeline for build, test, and Render deploy"
```

---

### Task 15: Tenant Isolation Verification Test

**Files:**
- Create: `packages/api/tests/Tungsten.Api.Tests/Integration/TenantIsolationTests.cs`

- [ ] **Step 1: Write the tenant isolation test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Integration/TenantIsolationTests.cs
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Integration;

public class TenantIsolationTests
{
    [Fact]
    public async Task ListBatches_OnlyReturnsBatchesFromUserTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var user1Id = Guid.NewGuid();

        db.Tenants.Add(new TenantEntity { Id = tenant1, Name = "T1", SchemaPrefix = "t1", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Tenants.Add(new TenantEntity { Id = tenant2, Name = "T2", SchemaPrefix = "t2", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = user1Id, Auth0Sub = "auth0|u1", Email = "u1@t.com", DisplayName = "U1", Role = "SUPPLIER", TenantId = tenant1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        db.Batches.Add(new BatchEntity { Id = Guid.NewGuid(), TenantId = tenant1, BatchNumber = "T1-001", MineralType = "Tungsten", OriginCountry = "RW", OriginMine = "Mine1", WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING", CreatedBy = user1Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Batches.Add(new BatchEntity { Id = Guid.NewGuid(), TenantId = tenant2, BatchNumber = "T2-001", MineralType = "Tungsten", OriginCountry = "RW", OriginMine = "Mine2", WeightKg = 200, Status = "CREATED", ComplianceStatus = "PENDING", CreatedBy = user1Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|u1");
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenant1);
        currentUser.GetUserIdAsync(Arg.Any<CancellationToken>()).Returns(user1Id);

        var handler = new ListBatches.Handler(db, currentUser);
        var result = await handler.Handle(new ListBatches.Query(1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("T1-001", result.Value.Items[0].BatchNumber);
    }

    [Fact]
    public async Task ListAuditLogs_OnlyReturnsLogsFromUserTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Tenants.Add(new TenantEntity { Id = tenant1, Name = "T1", SchemaPrefix = "t1", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Tenants.Add(new TenantEntity { Id = tenant2, Name = "T2", SchemaPrefix = "t2", Status = "ACTIVE", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = "auth0|a", Email = "a@t.com", DisplayName = "A", Role = "PLATFORM_ADMIN", TenantId = tenant1, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenant1, UserId = userId, Action = "CreateBatch", EntityType = "Batch", Result = "Success", Timestamp = DateTime.UtcNow });
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenant2, UserId = userId, Action = "CreateBatch", EntityType = "Batch", Result = "Success", Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenant1);

        var handler = new Features.Admin.ListAuditLogs.Handler(db, currentUser);
        var result = await handler.Handle(
            new Features.Admin.ListAuditLogs.Query(1, 20, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
    }
}
```

- [ ] **Step 2: Run tests**

Run: `cd packages/api && dotnet test --filter "FullyQualifiedName~TenantIsolationTests"`
Expected: PASS (both tests — verifying existing behaviour)

- [ ] **Step 3: Run full test suite**

Run: `cd packages/api && dotnet test`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add packages/api/tests/Tungsten.Api.Tests/Integration/TenantIsolationTests.cs
git commit -m "test: add tenant isolation verification tests for batches and audit logs"
```

---

### Task 16: Final Build Verification

- [ ] **Step 1: Run full API build and tests**

Run: `cd packages/api && dotnet build && dotnet test`
Expected: All pass

- [ ] **Step 2: Run full frontend build**

Run: `cd packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 3: Run format check**

Run: `cd packages/api && dotnet format --verify-no-changes`
Expected: No formatting issues

- [ ] **Step 4: Final commit if any formatting fixes needed**

```bash
git add -A && git commit -m "chore: formatting fixes from Phase 11 implementation"
```
