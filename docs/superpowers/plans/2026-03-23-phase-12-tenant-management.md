# Phase 12: Tenant Isolation + Management — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Transform the platform from single-tenant to multi-tenant with tenant provisioning, TENANT_ADMIN role, tenant-scoped user management, and tenant suspension.

**Architecture:** Add TENANT_ADMIN role and RequirePlatformAdmin policy. Platform admins create tenants via new API endpoints. Tenant suspension enforced via MediatR TenantStatusBehaviour. Handler-level role enforcement prevents privilege escalation. Frontend dashboard adapts to role.

**Tech Stack:** .NET 10, MediatR, EF Core + PostgreSQL, Angular 21 (signals, standalone), Tailwind CSS

**Spec:** `docs/superpowers/specs/2026-03-23-phase-12-tenant-management-design.md`

---

## Chunk 1: Role & Authorization Infrastructure

### Task 1: Add TENANT_ADMIN Role Constant

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/Roles.cs`

- [ ] **Step 1: Add TenantAdmin constant**

In `packages/api/src/Tungsten.Api/Common/Auth/Roles.cs`, add:

```csharp
public const string TenantAdmin = "TENANT_ADMIN";
```

After the existing `Admin` constant.

- [ ] **Step 2: Build to verify**

Run: `cd /c/__edMVP/packages/api && dotnet build`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Auth/Roles.cs
git commit -m "feat: add TENANT_ADMIN role constant"
```

---

### Task 2: Add RequirePlatformAdmin Policy + Update RequireAdmin

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/AuthorizationPolicies.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Common/Auth/AuthorizationPolicyTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Common/Auth/AuthorizationPolicyTests.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Tests.Common.Auth;

public class AuthorizationPolicyTests
{
    [Fact]
    public void RequireAdmin_AllowsBothPlatformAdminAndTenantAdmin()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options => options.AddTungstenPolicies());
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = authOptions.GetPolicyAsync(AuthorizationPolicies.RequireAdmin).Result!;
        var requirement = policy.Requirements.OfType<RoleRequirement>().First();

        Assert.Contains(Roles.Admin, requirement.AllowedRoles);
        Assert.Contains(Roles.TenantAdmin, requirement.AllowedRoles);
    }

    [Fact]
    public void RequirePlatformAdmin_AllowsOnlyPlatformAdmin()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options => options.AddTungstenPolicies());
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = authOptions.GetPolicyAsync(AuthorizationPolicies.RequirePlatformAdmin).Result!;
        var requirement = policy.Requirements.OfType<RoleRequirement>().First();

        Assert.Contains(Roles.Admin, requirement.AllowedRoles);
        Assert.DoesNotContain(Roles.TenantAdmin, requirement.AllowedRoles);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~AuthorizationPolicyTests"`
Expected: FAIL — `RequirePlatformAdmin` does not exist

- [ ] **Step 3: Update AuthorizationPolicies.cs**

```csharp
// packages/api/src/Tungsten.Api/Common/Auth/AuthorizationPolicies.cs
namespace Tungsten.Api.Common.Auth;

public static class AuthorizationPolicies
{
    public const string RequireSupplier = "RequireSupplier";
    public const string RequireBuyer = "RequireBuyer";
    public const string RequireAdmin = "RequireAdmin";
    public const string RequirePlatformAdmin = "RequirePlatformAdmin";
    public const string RequireTenantAccess = "RequireTenantAccess";

    public static void AddTungstenPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(RequireSupplier, policy =>
            policy.AddRequirements(new RoleRequirement(Roles.Supplier)));

        options.AddPolicy(RequireBuyer, policy =>
            policy.AddRequirements(new RoleRequirement(Roles.Buyer)));

        options.AddPolicy(RequireAdmin, policy =>
            policy.AddRequirements(new RoleRequirement(Roles.Admin, Roles.TenantAdmin)));

        options.AddPolicy(RequirePlatformAdmin, policy =>
            policy.AddRequirements(new RoleRequirement(Roles.Admin)));

        options.AddPolicy(RequireTenantAccess, policy =>
            policy.AddRequirements(new TenantAccessRequirement()));
    }
}
```

- [ ] **Step 4: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~AuthorizationPolicyTests"`
Expected: PASS (2 tests)

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Auth/AuthorizationPolicies.cs packages/api/tests/Tungsten.Api.Tests/Common/Auth/AuthorizationPolicyTests.cs
git commit -m "feat: add RequirePlatformAdmin policy, update RequireAdmin to include TENANT_ADMIN"
```

---

### Task 3: Update Admin Endpoint Policies to RequirePlatformAdmin

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs`

- [ ] **Step 1: Change RMAP and Jobs endpoints to RequirePlatformAdmin**

In `packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs`, change:
- RMAP upload endpoint: `RequireAuthorization(AuthorizationPolicies.RequireAdmin)` → `RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin)`
- RMAP list endpoint: same change
- Jobs endpoint: same change
- Audit logs endpoint: keep as `RequireAdmin` (both roles can access)

- [ ] **Step 2: Build and run all tests**

Run: `cd /c/__edMVP/packages/api && dotnet build && dotnet test`
Expected: All pass

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs
git commit -m "feat: restrict RMAP and Jobs endpoints to RequirePlatformAdmin"
```

---

### Task 4: TenantStatusBehaviour — Suspension Enforcement

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs` (add tenant status to cache)
- Create: `packages/api/src/Tungsten.Api/Common/Behaviours/TenantStatusBehaviour.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Common/Behaviours/TenantStatusBehaviourTests.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs` (register behaviour)

- [ ] **Step 1: Extend ICurrentUserService with GetTenantStatusAsync**

Add to `ICurrentUserService` interface:
```csharp
Task<string> GetTenantStatusAsync(CancellationToken ct);
```

Update `CurrentUserService.ResolveUserAsync` to also select `TenantStatus`:
```csharp
private string? _tenantStatus;

public async Task<string> GetTenantStatusAsync(CancellationToken ct)
{
    if (_tenantStatus is not null) return _tenantStatus;
    await ResolveUserAsync(ct);
    return _tenantStatus!;
}

private async Task ResolveUserAsync(CancellationToken ct)
{
    var sub = Auth0Sub;
    var user = await db.Users.AsNoTracking()
        .Where(u => u.Auth0Sub == sub && u.IsActive)
        .Join(db.Tenants, u => u.TenantId, t => t.Id, (u, t) => new { u.Id, u.TenantId, TenantStatus = t.Status })
        .FirstOrDefaultAsync(ct)
        ?? throw new UnauthorizedAccessException("User not found");

    _userId = user.Id;
    _tenantId = user.TenantId;
    _tenantStatus = user.TenantStatus;
}
```

Also add `Task<string> GetRoleAsync(CancellationToken ct)` for the behaviour to check if user is PLATFORM_ADMIN:
```csharp
private string? _role;

public async Task<string> GetRoleAsync(CancellationToken ct)
{
    if (_role is not null) return _role;
    await ResolveUserAsync(ct);
    return _role!;
}
```

Update `ResolveUserAsync` to also select `u.Role` and cache it in `_role`.

- [ ] **Step 2: Write TenantStatusBehaviour tests**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Common/Behaviours/TenantStatusBehaviourTests.cs
using MediatR;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Behaviours;

namespace Tungsten.Api.Tests.Common.Behaviours;

public class TenantStatusBehaviourTests
{
    public record TestCommand(string Name) : IRequest<Result<string>>;

    [Fact]
    public async Task Handle_ActiveTenant_ProceedsToHandler()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("ACTIVE");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("SUPPLIER");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);
        var response = Result<string>.Success("ok");

        var result = await behaviour.Handle(
            new TestCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Value);
    }

    [Fact]
    public async Task Handle_SuspendedTenant_NonAdmin_ReturnsFailure()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("SUSPENDED");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("SUPPLIER");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);

        var result = await behaviour.Handle(
            new TestCommand("test"),
            _ => Task.FromResult(Result<string>.Success("should not reach")),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("suspended", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_SuspendedTenant_PlatformAdmin_ProceedsToHandler()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("SUSPENDED");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("PLATFORM_ADMIN");
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);
        var response = Result<string>.Success("admin ok");

        var result = await behaviour.Handle(
            new TestCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_NoHttpContext_SkipsCheck()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        var accessor = new HttpContextAccessor { HttpContext = null };

        var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);
        var response = Result<string>.Success("worker ok");

        var result = await behaviour.Handle(
            new TestCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~TenantStatusBehaviourTests"`
Expected: FAIL

- [ ] **Step 4: Implement TenantStatusBehaviour**

```csharp
// packages/api/src/Tungsten.Api/Common/Behaviours/TenantStatusBehaviour.cs
using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Common.Behaviours;

public class TenantStatusBehaviour<TRequest, TResponse>(
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        // Skip for background workers
        if (httpContextAccessor.HttpContext is null)
            return await next();

        var tenantStatus = await currentUser.GetTenantStatusAsync(ct);

        if (tenantStatus == "SUSPENDED")
        {
            var role = await currentUser.GetRoleAsync(ct);
            if (role != Roles.Admin)
            {
                // Return failure using Result pattern
                if (typeof(TResponse).IsGenericType &&
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(string)])!;
                    return (TResponse)failureMethod.Invoke(null, ["Your organization's account has been suspended. Contact support."])!;
                }

                if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure("Your organization's account has been suspended. Contact support.");
                }
            }
        }

        return await next();
    }
}
```

- [ ] **Step 5: Register in Program.cs**

In `packages/api/src/Tungsten.Api/Program.cs`, add AFTER `ValidationBehaviour` and BEFORE `AuditBehaviour`:

```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantStatusBehaviour<,>));
```

Pipeline order: ValidationBehaviour → TenantStatusBehaviour → AuditBehaviour → Handler

- [ ] **Step 6: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~TenantStatusBehaviourTests"`
Expected: PASS (4 tests)

- [ ] **Step 7: Run all tests**

Run: `cd /c/__edMVP/packages/api && dotnet test`
Expected: All pass

- [ ] **Step 8: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs packages/api/src/Tungsten.Api/Common/Behaviours/TenantStatusBehaviour.cs packages/api/tests/Tungsten.Api.Tests/Common/Behaviours/TenantStatusBehaviourTests.cs packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: add TenantStatusBehaviour for suspension enforcement via Result pattern"
```

---

## Chunk 2: Tenant CRUD Endpoints

### Task 5: CreateTenant Endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Platform/CreateTenant.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Platform/PlatformEndpoints.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Platform/CreateTenantTests.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs` (register endpoints)

- [ ] **Step 1: Write the test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Features/Platform/CreateTenantTests.cs
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Platform;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Platform;

public class CreateTenantTests
{
    [Fact]
    public async Task Handle_ValidInput_CreatesTenantAndAdmin()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var handler = new CreateTenant.Handler(db);
        var result = await handler.Handle(
            new CreateTenant.Command("Acme Mining", "admin@acme.com"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Mining", result.Value.Name);
        Assert.Equal("ACTIVE", result.Value.Status);

        // Verify tenant created
        var tenant = await db.Tenants.FirstOrDefaultAsync();
        Assert.NotNull(tenant);
        Assert.Equal("acme_mining", tenant.SchemaPrefix);

        // Verify TENANT_ADMIN user created
        var user = await db.Users.FirstOrDefaultAsync();
        Assert.NotNull(user);
        Assert.Equal("admin@acme.com", user.Email);
        Assert.Equal("TENANT_ADMIN", user.Role);
        Assert.StartsWith("pending|", user.Auth0Sub);
        Assert.Equal(tenant.Id, user.TenantId);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Existing", SchemaPrefix = "existing", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), Auth0Sub = "auth0|x", Email = "admin@acme.com", DisplayName = "X", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new CreateTenant.Handler(db);
        var result = await handler.Handle(
            new CreateTenant.Command("Acme Mining", "admin@acme.com"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already in use", result.Error!);
    }

    [Fact]
    public async Task Handle_SchemaPrefixCollision_AppendsNumericSuffix()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        db.Tenants.Add(new TenantEntity { Id = Guid.NewGuid(), Name = "Existing", SchemaPrefix = "acme_mining", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new CreateTenant.Handler(db);
        var result = await handler.Handle(
            new CreateTenant.Command("Acme Mining", "new@acme.com"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tenant = await db.Tenants.OrderByDescending(t => t.CreatedAt).FirstAsync();
        Assert.Equal("acme_mining_2", tenant.SchemaPrefix);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~CreateTenantTests"`
Expected: FAIL

- [ ] **Step 3: Implement CreateTenant**

```csharp
// packages/api/src/Tungsten.Api/Features/Platform/CreateTenant.cs
using System.Text.RegularExpressions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Platform;

public static class CreateTenant
{
    public record Command(string Name, string AdminEmail) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "CreateTenant";
        public string EntityType => "Tenant";
    }

    public record Response(Guid Id, string Name, string Status, string AdminEmail, DateTime CreatedAt);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
        }
    }

    public class Handler(AppDbContext db) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            // Global email uniqueness
            var emailExists = await db.Users.AnyAsync(u => u.Email == cmd.AdminEmail, ct);
            if (emailExists)
                return Result<Response>.Failure($"Email '{cmd.AdminEmail}' is already in use");

            // Generate schema prefix
            var basePrefix = GenerateSchemaPrefix(cmd.Name);
            var prefix = basePrefix;
            var suffix = 2;
            while (await db.Tenants.AnyAsync(t => t.SchemaPrefix == prefix, ct))
            {
                prefix = $"{basePrefix}_{suffix}";
                suffix++;
            }

            var tenant = new TenantEntity
            {
                Id = Guid.NewGuid(),
                Name = cmd.Name,
                SchemaPrefix = prefix,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            };

            var adminUser = new UserEntity
            {
                Id = Guid.NewGuid(),
                Auth0Sub = $"pending|{Guid.NewGuid()}",
                Email = cmd.AdminEmail,
                DisplayName = cmd.AdminEmail.Split('@')[0],
                Role = "TENANT_ADMIN",
                TenantId = tenant.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            db.Tenants.Add(tenant);
            db.Users.Add(adminUser);
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(
                new Response(tenant.Id, tenant.Name, tenant.Status, cmd.AdminEmail, tenant.CreatedAt));
        }

        private static string GenerateSchemaPrefix(string name)
        {
            var prefix = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
            return prefix.Length > 50 ? prefix[..50] : prefix;
        }
    }
}
```

- [ ] **Step 4: Create PlatformEndpoints.cs**

```csharp
// packages/api/src/Tungsten.Api/Features/Platform/PlatformEndpoints.cs
using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Platform;

public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/platform/tenants")
            .RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin);

        group.MapPost("/", async (CreateTenant.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/platform/tenants/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
```

- [ ] **Step 5: Register endpoints in Program.cs**

Add to `packages/api/src/Tungsten.Api/Program.cs` after `app.MapAdminEndpoints();`:

```csharp
app.MapPlatformEndpoints();
```

Add the using:
```csharp
using Tungsten.Api.Features.Platform;
```

- [ ] **Step 6: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~CreateTenantTests"`
Expected: PASS (3 tests)

- [ ] **Step 7: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Platform/ packages/api/tests/Tungsten.Api.Tests/Features/Platform/ packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: add CreateTenant endpoint with schema prefix generation and email uniqueness"
```

---

### Task 6: ListTenants Endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Platform/ListTenants.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Platform/PlatformEndpoints.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Platform/ListTenantsTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Features/Platform/ListTenantsTests.cs
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Platform;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Platform;

public class ListTenantsTests
{
    [Fact]
    public async Task Handle_ReturnsTenants_WithUserAndBatchCounts()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        var userId = Guid.NewGuid();
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = "auth0|1", Email = "a@a.com", DisplayName = "A", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Batches.Add(new BatchEntity { Id = Guid.NewGuid(), TenantId = tenantId, BatchNumber = "B-001", MineralType = "Tungsten", OriginCountry = "RW", OriginMine = "Mine", WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING", CreatedBy = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new ListTenants.Handler(db);
        var result = await handler.Handle(new ListTenants.Query(1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Acme", result.Value.Items[0].Name);
        Assert.Equal(1, result.Value.Items[0].UserCount);
        Assert.Equal(1, result.Value.Items[0].BatchCount);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~ListTenantsTests"`

- [ ] **Step 3: Implement ListTenants**

```csharp
// packages/api/src/Tungsten.Api/Features/Platform/ListTenants.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Pagination;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Platform;

public static class ListTenants
{
    public record Query(int Page, int PageSize) : IRequest<Result<PagedResponse<TenantDto>>>;

    public record TenantDto(Guid Id, string Name, string Status, int UserCount, int BatchCount, DateTime CreatedAt);

    public class Handler(AppDbContext db) : IRequestHandler<Query, Result<PagedResponse<TenantDto>>>
    {
        public async Task<Result<PagedResponse<TenantDto>>> Handle(Query query, CancellationToken ct)
        {
            var totalCount = await db.Tenants.CountAsync(ct);

            var items = await db.Tenants.AsNoTracking()
                .OrderByDescending(t => t.CreatedAt)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(t => new TenantDto(
                    t.Id, t.Name, t.Status,
                    db.Users.Count(u => u.TenantId == t.Id),
                    db.Batches.Count(b => b.TenantId == t.Id),
                    t.CreatedAt))
                .ToListAsync(ct);

            return Result<PagedResponse<TenantDto>>.Success(
                new PagedResponse<TenantDto>(items, totalCount, query.Page, query.PageSize));
        }
    }
}
```

- [ ] **Step 4: Add endpoint to PlatformEndpoints.cs**

Add inside `MapPlatformEndpoints`:
```csharp
group.MapGet("/", async (int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(new ListTenants.Query(page ?? 1, pageSize ?? 20), ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
});
```

- [ ] **Step 5: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~ListTenantsTests"`
Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Platform/
git commit -m "feat: add ListTenants endpoint with user and batch counts"
```

---

### Task 7: UpdateTenantStatus Endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Platform/UpdateTenantStatus.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Platform/PlatformEndpoints.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Platform/UpdateTenantStatusTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Features/Platform/UpdateTenantStatusTests.cs
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Platform;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Platform;

public class UpdateTenantStatusTests
{
    [Fact]
    public async Task Handle_SuspendsTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid()); // Different tenant

        var handler = new UpdateTenantStatus.Handler(db, currentUser);
        var result = await handler.Handle(
            new UpdateTenantStatus.Command(tenantId, "SUSPENDED"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("SUSPENDED", result.Value.Status);
    }

    [Fact]
    public async Task Handle_CannotSuspendOwnTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId); // Same tenant

        var handler = new UpdateTenantStatus.Handler(db, currentUser);
        var result = await handler.Handle(
            new UpdateTenantStatus.Command(tenantId, "SUSPENDED"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("own tenant", result.Error!);
    }
}
```

- [ ] **Step 2: Implement UpdateTenantStatus**

```csharp
// packages/api/src/Tungsten.Api/Features/Platform/UpdateTenantStatus.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Platform;

public static class UpdateTenantStatus
{
    public record Command(Guid TenantId, string Status) : IRequest<Result<Response>>, IAuditable
    {
        public string AuditAction => "UpdateTenantStatus";
        public string EntityType => "Tenant";
    }

    public record Response(Guid Id, string Name, string Status);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.TenantId).NotEmpty();
            RuleFor(x => x.Status).Must(s => s is "ACTIVE" or "SUSPENDED")
                .WithMessage("Status must be ACTIVE or SUSPENDED");
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == cmd.TenantId, ct);
            if (tenant is null)
                return Result<Response>.Failure("Tenant not found");

            if (cmd.Status == "SUSPENDED")
            {
                var callerTenantId = await currentUser.GetTenantIdAsync(ct);
                if (callerTenantId == cmd.TenantId)
                    return Result<Response>.Failure("Cannot suspend your own tenant");
            }

            tenant.Status = cmd.Status;
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(tenant.Id, tenant.Name, tenant.Status));
        }
    }
}
```

- [ ] **Step 3: Add endpoint to PlatformEndpoints.cs**

```csharp
group.MapPatch("/{id:guid}/status", async (Guid id, UpdateTenantStatus.Command command, IMediator mediator, CancellationToken ct) =>
{
    var cmd = command with { TenantId = id };
    var result = await mediator.Send(cmd, ct);
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
});
```

- [ ] **Step 4: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~UpdateTenantStatusTests"`
Expected: PASS (2 tests)

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Platform/ packages/api/tests/Tungsten.Api.Tests/Features/Platform/
git commit -m "feat: add UpdateTenantStatus endpoint with self-suspension protection"
```

---

## Chunk 3: User Management Modifications

### Task 8: Update CreateUser — TENANT_ADMIN Can Invite Users

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Users/CreateUser.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Users/CreateUserRoleEnforcementTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Features/Users/CreateUserRoleEnforcementTests.cs
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Users;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Configuration;

namespace Tungsten.Api.Tests.Features.Users;

public class CreateUserRoleEnforcementTests
{
    private static (AppDbContext db, ICurrentUserService currentUser) SetupWithRole(string callerRole)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = "auth0|caller", Email = "caller@test.com", DisplayName = "Caller", Role = callerRole, TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|caller");
        currentUser.GetUserIdAsync(Arg.Any<CancellationToken>()).Returns(userId);
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns(callerRole);

        return (db, currentUser);
    }

    [Fact]
    public async Task TenantAdmin_CanInviteSupplier()
    {
        var (db, currentUser) = SetupWithRole("TENANT_ADMIN");
        var emailService = Substitute.For<IEmailService>();
        var config = Substitute.For<IConfiguration>();
        var handler = new CreateUser.Handler(db, currentUser, emailService, config);

        var result = await handler.Handle(
            new CreateUser.Command("new@test.com", "New User", "SUPPLIER"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TenantAdmin_CannotAssignTenantAdmin()
    {
        var (db, currentUser) = SetupWithRole("TENANT_ADMIN");
        var emailService = Substitute.For<IEmailService>();
        var config = Substitute.For<IConfiguration>();
        var handler = new CreateUser.Handler(db, currentUser, emailService, config);

        var result = await handler.Handle(
            new CreateUser.Command("new@test.com", "New User", "TENANT_ADMIN"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Supplier or Buyer", result.Error!);
    }

    [Fact]
    public async Task PlatformAdmin_CanAssignTenantAdmin()
    {
        var (db, currentUser) = SetupWithRole("PLATFORM_ADMIN");
        var emailService = Substitute.For<IEmailService>();
        var config = Substitute.For<IConfiguration>();
        var handler = new CreateUser.Handler(db, currentUser, emailService, config);

        var result = await handler.Handle(
            new CreateUser.Command("new@test.com", "New User", "TENANT_ADMIN"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~CreateUserRoleEnforcementTests"`

- [ ] **Step 3: Update CreateUser.cs**

In the `Validator`, add TENANT_ADMIN to allowed roles:
```csharp
RuleFor(x => x.Role).Must(r => r is "SUPPLIER" or "BUYER" or "PLATFORM_ADMIN" or "TENANT_ADMIN")
    .WithMessage("Invalid role");
```

In the `Handler`, add at the start of `Handle` method (after resolving admin user):
```csharp
// Role assignment enforcement
var callerRole = await currentUser.GetRoleAsync(ct);
if (callerRole == Roles.TenantAdmin && cmd.Role is not ("SUPPLIER" or "BUYER"))
    return Result<Response>.Failure("You can only assign Supplier or Buyer roles");
```

- [ ] **Step 4: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~CreateUserRoleEnforcementTests"`
Expected: PASS (3 tests)

- [ ] **Step 5: Run all tests**

Run: `cd /c/__edMVP/packages/api && dotnet test`
Expected: All pass

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Users/CreateUser.cs packages/api/tests/Tungsten.Api.Tests/Features/Users/
git commit -m "feat: allow TENANT_ADMIN to invite users with role enforcement"
```

---

### Task 9: Update UpdateUser — TENANT_ADMIN Role Restrictions

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Users/UpdateUser.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Users/UpdateUserRoleEnforcementTests.cs`

- [ ] **Step 1: Write the test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Features/Users/UpdateUserRoleEnforcementTests.cs
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Users;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Users;

public class UpdateUserRoleEnforcementTests
{
    [Fact]
    public async Task TenantAdmin_CannotEscalateToAdmin()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = callerId, Auth0Sub = "auth0|caller", Email = "c@t.com", DisplayName = "C", Role = "TENANT_ADMIN", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = targetId, Auth0Sub = "auth0|target", Email = "t@t.com", DisplayName = "T", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|caller");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("TENANT_ADMIN");

        var handler = new UpdateUser.Handler(db, currentUser);
        var result = await handler.Handle(
            new UpdateUser.Command(targetId, "PLATFORM_ADMIN", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot assign", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TenantAdmin_CannotModifyOtherTenantAdmin()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = callerId, Auth0Sub = "auth0|caller", Email = "c@t.com", DisplayName = "C", Role = "TENANT_ADMIN", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = targetId, Auth0Sub = "auth0|target", Email = "t@t.com", DisplayName = "T", Role = "TENANT_ADMIN", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|caller");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("TENANT_ADMIN");

        var handler = new UpdateUser.Handler(db, currentUser);
        var result = await handler.Handle(
            new UpdateUser.Command(targetId, null, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("cannot modify", result.Error!, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Update UpdateUser handler**

Add at the start of Handle, after resolving the admin user and target user:

```csharp
var callerRole = await currentUser.GetRoleAsync(ct);
if (callerRole == Roles.TenantAdmin)
{
    if (target.Role is Roles.Admin or Roles.TenantAdmin)
        return Result.Failure("You cannot modify this user");
    if (cmd.Role is Roles.Admin or Roles.TenantAdmin)
        return Result.Failure("You cannot assign this role");
}
```

- [ ] **Step 3: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~UpdateUserRoleEnforcementTests"`
Expected: PASS (2 tests)

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Users/UpdateUser.cs packages/api/tests/Tungsten.Api.Tests/Features/Users/
git commit -m "feat: add TENANT_ADMIN role restrictions in UpdateUser handler"
```

---

### Task 10: Update ListUsers — Hide PLATFORM_ADMIN from TENANT_ADMIN

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Users/ListUsers.cs`

- [ ] **Step 1: Update ListUsers handler**

After resolving the current user, add:

```csharp
var callerRole = await currentUser.GetRoleAsync(ct);
var query = db.Users.AsNoTracking()
    .Where(u => u.TenantId == user.TenantId);

if (callerRole == Roles.TenantAdmin)
    query = query.Where(u => u.Role != Roles.Admin);
```

- [ ] **Step 2: Build and run all tests**

Run: `cd /c/__edMVP/packages/api && dotnet build && dotnet test`
Expected: All pass

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Users/ListUsers.cs
git commit -m "feat: hide PLATFORM_ADMIN users from TENANT_ADMIN in ListUsers"
```

---

### Task 11: Update `/api/me` — Remove Auto-Provisioning Fallback

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Read the current `/api/me` endpoint in Program.cs**

Read lines 188-292 of `packages/api/src/Tungsten.Api/Program.cs`.

- [ ] **Step 2: Remove the auto-provisioning block**

Remove the block that creates a new user as PLATFORM_ADMIN on the first active tenant (the `// Auto-provision: no matching user found` section). Replace it with:

```csharp
return Results.Json(new { error = "No account found. Contact your administrator to get access." }, statusCode: 403);
```

Keep the existing logic for:
- Auth0Sub match → return user
- Email match with `pending|` prefix → link and return
- Email match with different Auth0Sub → relink and return

- [ ] **Step 3: Build and run all tests**

Run: `cd /c/__edMVP/packages/api && dotnet build && dotnet test`
Expected: All pass

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: remove auto-provisioning, return 403 for unknown users"
```

---

## Chunk 4: Frontend Changes

### Task 12: Update TypeScript Types + Route Guards

**Files:**
- Modify: `packages/web/src/app/core/auth/auth.service.ts`
- Modify: `packages/web/src/app/app.routes.ts`
- Modify: `packages/web/src/app/features/auth/login.component.ts`

- [ ] **Step 1: Update UserProfile role type**

In `packages/web/src/app/core/auth/auth.service.ts`, change:
```typescript
role: 'SUPPLIER' | 'BUYER' | 'PLATFORM_ADMIN' | 'TENANT_ADMIN';
```

- [ ] **Step 2: Update admin route guard**

In `packages/web/src/app/app.routes.ts`, change the admin route guard from:
```typescript
canActivate: [roleGuard('PLATFORM_ADMIN')],
```
to:
```typescript
canActivate: [roleGuard('PLATFORM_ADMIN', 'TENANT_ADMIN')],
```

- [ ] **Step 3: Update login component for 403 handling**

In `packages/web/src/app/features/auth/login.component.ts`, ensure that when `/api/me` returns 403, the UI shows: "No account found. Contact your administrator to get access." instead of a generic error.

- [ ] **Step 4: Build**

Run: `cd /c/__edMVP/packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 5: Commit**

```bash
git add packages/web/src/app/core/auth/ packages/web/src/app/app.routes.ts packages/web/src/app/features/auth/login.component.ts
git commit -m "feat: add TENANT_ADMIN type, update route guards, handle 403 on login"
```

---

### Task 13: Admin Dashboard — Role-Based Visibility

**Files:**
- Modify: `packages/web/src/app/features/admin/admin-dashboard.component.ts`

- [ ] **Step 1: Inject AuthService and conditionally show cards**

Read the current admin-dashboard.component.ts. Then:

- Inject `AuthService` to access `role()` signal
- Wrap the PLATFORM_ADMIN-only cards (RMAP, Jobs, Tenants) in `@if (isPlatformAdmin())` blocks
- Add a "Tenants" quick action card linking to `/admin/tenants`
- TENANT_ADMIN sees only: Users, Audit Log

- [ ] **Step 2: Build**

Run: `cd /c/__edMVP/packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/features/admin/admin-dashboard.component.ts
git commit -m "feat: show admin dashboard cards based on role (PLATFORM_ADMIN vs TENANT_ADMIN)"
```

---

### Task 14: Tenant Management Page + Admin API Service

**Files:**
- Create: `packages/web/src/app/features/admin/data/tenant.models.ts`
- Modify: `packages/web/src/app/features/admin/data/admin-api.service.ts`
- Create: `packages/web/src/app/features/admin/tenant-management.component.ts`
- Modify: `packages/web/src/app/features/admin/admin.routes.ts`

- [ ] **Step 1: Create tenant models**

```typescript
// packages/web/src/app/features/admin/data/tenant.models.ts
export interface TenantDto {
  id: string;
  name: string;
  status: 'ACTIVE' | 'SUSPENDED';
  userCount: number;
  batchCount: number;
  createdAt: string;
}

export interface CreateTenantRequest {
  name: string;
  adminEmail: string;
}

export interface PagedTenants {
  items: TenantDto[];
  totalCount: number;
  page: number;
  pageSize: number;
}
```

- [ ] **Step 2: Add tenant methods to admin-api.service.ts**

```typescript
import { TenantDto, CreateTenantRequest, PagedTenants } from './tenant.models';

listTenants(page = 1, pageSize = 20) {
  return this.http.get<PagedTenants>(`${this.apiUrl}/api/platform/tenants?page=${page}&pageSize=${pageSize}`);
}

createTenant(request: CreateTenantRequest) {
  return this.http.post<TenantDto>(`${this.apiUrl}/api/platform/tenants`, request);
}

updateTenantStatus(id: string, status: 'ACTIVE' | 'SUSPENDED') {
  return this.http.patch<TenantDto>(`${this.apiUrl}/api/platform/tenants/${id}/status`, { status });
}
```

- [ ] **Step 3: Create tenant management component**

Create `packages/web/src/app/features/admin/tenant-management.component.ts`:
- Standalone component with `ChangeDetectionStrategy.OnPush`
- Table showing tenants: name, status badge, user count, batch count, created date
- "Create Tenant" button → inline form or modal with name + email fields
- Suspend/Reactivate toggle button per row
- Uses `toSignal()` + `AdminApiService` for data loading
- Back link to `/admin`

- [ ] **Step 4: Add route**

In `packages/web/src/app/features/admin/admin.routes.ts`, add:
```typescript
{
  path: 'tenants',
  loadComponent: () => import('./tenant-management.component').then(m => m.TenantManagementComponent),
},
```

- [ ] **Step 5: Build**

Run: `cd /c/__edMVP/packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add packages/web/src/app/features/admin/
git commit -m "feat: add tenant management page with create, list, and suspend/reactivate"
```

---

### Task 15: Update User Management — Role Picker Based on Caller Role

**Files:**
- Modify: `packages/web/src/app/features/admin/user-management.component.ts`

- [ ] **Step 1: Read the current user-management.component.ts**

- [ ] **Step 2: Update role picker**

- Inject `AuthService` to access `role()` signal
- If caller is TENANT_ADMIN: show only SUPPLIER and BUYER in the role dropdown
- If caller is PLATFORM_ADMIN: show SUPPLIER, BUYER, and TENANT_ADMIN in the role dropdown
- Never show PLATFORM_ADMIN in the dropdown (assigned manually only)

- [ ] **Step 3: Build**

Run: `cd /c/__edMVP/packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/features/admin/user-management.component.ts
git commit -m "feat: show role picker options based on caller role"
```

---

### Task 16: Final Build Verification

- [ ] **Step 1: Run full API build and tests**

Run: `cd /c/__edMVP/packages/api && dotnet build && dotnet test`
Expected: All pass

- [ ] **Step 2: Run full frontend build**

Run: `cd /c/__edMVP/packages/web && npx ng build`
Expected: Build succeeded

- [ ] **Step 3: Run format check**

Run: `cd /c/__edMVP/packages/api && dotnet format --verify-no-changes`

- [ ] **Step 4: Final commit if formatting fixes needed**

```bash
git add -A && git commit -m "chore: formatting fixes from Phase 12 implementation"
```
