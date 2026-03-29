# Post-Stripe Password Setup — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After a successful Stripe checkout, take the user directly to a password creation screen, auto-log them in, and redirect to `/admin` — with two abandonment recovery paths (setup email + login-page detection).

**Architecture:** The Stripe webhook no longer creates an `AppIdentityUser`; it leaves `UserEntity.IdentityUserId = "pending|{guid}"` and stores the Stripe session ID. Three new anonymous API endpoints handle status polling, password creation, and resending the setup email. A new `SetPasswordComponent` polls until the user is provisioned, then collects the password and auto-logs in. Auth handlers follow the existing static-method pattern used by `Login.cs` and `Register.cs` (not MediatR), since they require `HttpContext` for cookie setting and match the established auth code style.

**Tech Stack:** .NET 10 / ASP.NET Core, EF Core + PostgreSQL, Stripe.net SDK, ASP.NET Core Identity, Angular 21+ standalone signals, Tailwind CSS.

**Spec:** `docs/superpowers/specs/2026-03-29-post-stripe-password-setup-design.md`

---

## Chunk 1: Data Model + Migration

### Task 1: Add `StripeSessionId` to `UserEntity` and create EF migration

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs`
- Create: `packages/api/src/Tungsten.Api/Migrations/<timestamp>_AddStripeSessionId.cs` (generated)

- [ ] **Step 1.1: Add property to `UserEntity`**

Open `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs` and add the new property after `UpdatedAt`:

```csharp
public string? StripeSessionId { get; set; }
```

Full file after change:
```csharp
namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public required string IdentityUserId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string Role { get; set; }
    public Guid TenantId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? StripeSessionId { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
}
```

- [ ] **Step 1.2: Generate EF Core migration**

From `packages/api/src/Tungsten.Api/`:
```bash
dotnet ef migrations add AddStripeSessionId
```

Expected output: `Build succeeded. ... Done.` with a new migration file in `Migrations/`.

- [ ] **Step 1.3: Verify migration file looks correct**

Open the generated `Migrations/<timestamp>_AddStripeSessionId.cs`. Confirm it adds a nullable `stripe_session_id` (or `StripeSessionId`) column to the `users` table. It should look like:

```csharp
migrationBuilder.AddColumn<string>(
    name: "StripeSessionId",
    table: "users",
    type: "text",
    nullable: true);
```

- [ ] **Step 1.4: Build to confirm compilation**

```bash
dotnet build packages/api/src/Tungsten.Api/Tungsten.Api.csproj
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

- [ ] **Step 1.5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs
git add packages/api/src/Tungsten.Api/Migrations/
git commit -m "feat: add StripeSessionId to UserEntity for post-stripe password setup"
```

---

## Chunk 2: Backend — Modify Existing Files

> **TDD note for this chunk:** `StripeWebhookHandler.cs` signature changes in Task 3 will immediately break the test project. Task 3 therefore begins by updating `StripeWebhookTests.cs` **before** touching production code, so the project is always in a compilable red/green state.

### Task 2: Update `EmailTemplates.cs` — rename `Welcome` to `AccountSetup` with corrected copy

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Services/EmailTemplates.cs`

The only call site for `Welcome` is `StripeWebhookHandler.cs`, which we modify in Task 3. Rename the method and update the copy to remove "Sign in with Google".

- [ ] **Step 2.1: Replace `Welcome` with `AccountSetup` in `EmailTemplates.cs`**

Replace the entire `Welcome` method with:

```csharp
public static (string subject, string htmlBody, string textBody) AccountSetup(string adminName, string companyName, string setupUrl)
{
    var subject = $"Complete your auditraks account setup";
    var htmlBody = $"""
        <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
            <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">Welcome to auditraks</h1>
            <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                Hi {adminName},
            </p>
            <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                Your organization <strong>{companyName}</strong> has been set up on auditraks.
                You have a <strong>60-day free trial</strong> to explore the platform.
            </p>
            <p style="color: #334155; font-size: 16px; line-height: 1.6;">
                Set up your password to get started:
            </p>
            <a href="{setupUrl}" style="display: inline-block; background: #4f46e5; color: white; padding: 12px 28px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 15px; margin: 16px 0;">
                Set up your password
            </a>
            <p style="color: #64748b; font-size: 14px; margin-top: 32px;">
                As your organization's admin, you can invite team members from the Admin Dashboard.
            </p>
            <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 32px 0;" />
            <p style="color: #94a3b8; font-size: 12px;">
                &copy; 2026 auditraks. Tungsten supply chain compliance, automated.
            </p>
        </div>
        """;
    var textBody = $"Welcome to auditraks, {adminName}!\n\nYour organization {companyName} has been set up on auditraks. You have a 60-day free trial to explore the platform.\n\nSet up your password to get started: {setupUrl}\n\nAs your organization's admin, you can invite team members from the Admin Dashboard.\n\n© 2026 auditraks.";
    return (subject, htmlBody, textBody);
}
```

- [ ] **Step 2.2: Build**

```bash
dotnet build packages/api/src/Tungsten.Api/Tungsten.Api.csproj
```

Expected: build fails because `StripeWebhookHandler.cs` still calls `EmailTemplates.Welcome`. This confirms the rename was needed — proceed to Task 3 immediately.

---

### Task 3: Update `StripeWebhookHandler.cs` — remove Identity user creation, store session ID, send setup email

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Signup/StripeWebhookHandler.cs`
- Modify: `packages/api/tests/Tungsten.Api.Tests/Features/Signup/StripeWebhookTests.cs` ← update tests first (TDD red)

- [ ] **Step 3.0 (TDD red): Update `StripeWebhookTests.cs` call signatures before changing production code**

Open `packages/api/tests/Tungsten.Api.Tests/Features/Signup/StripeWebhookTests.cs`. Find all **3** calls to `handler.HandleCheckoutCompleted(...)` (at lines 34, 67, 131) and add `"cs_test_session_123"` as the last argument:

```csharp
// Before (all 3 calls)
await handler.HandleCheckoutCompleted("cus_test123", "sub_test123", "Acme Mining", "John Smith", "john@acme.com", "PRO");
// After
await handler.HandleCheckoutCompleted("cus_test123", "sub_test123", "Acme Mining", "John Smith", "john@acme.com", "PRO", "cs_test_session_123");
```

Run to confirm red (compile error — handler still has 6-arg signature):
```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~StripeWebhook" -v 2>&1 | head -20
```
Expected: **compilation error** — correct, this is the TDD red phase.

- [ ] **Step 3.1: Update `HandleCheckoutCompleted` signature and body**

Replace the entire method with:

```csharp
public async Task HandleCheckoutCompleted(
    string customerId, string subscriptionId,
    string companyName, string adminName, string adminEmail, string plan, string sessionId)
{
    if (await db.Users.AnyAsync(u => u.Email == adminEmail))
    {
        logger.LogWarning("Checkout completed but email {Email} already exists, skipping", adminEmail);
        return;
    }

    var basePrefix = GenerateSchemaPrefix(companyName);
    var prefix = basePrefix;
    var suffix = 2;
    while (await db.Tenants.AnyAsync(t => t.SchemaPrefix == prefix))
    {
        prefix = $"{basePrefix}_{suffix}";
        suffix++;
    }

    var (maxBatches, maxUsers) = PlanConfiguration.GetLimits(plan);
    var tenant = new TenantEntity
    {
        Id = Guid.NewGuid(),
        Name = companyName,
        SchemaPrefix = prefix,
        Status = "TRIAL",
        StripeCustomerId = customerId,
        StripeSubscriptionId = subscriptionId,
        PlanName = plan,
        MaxBatches = maxBatches,
        MaxUsers = maxUsers,
        TrialEndsAt = DateTime.UtcNow.AddDays(60),
        CreatedAt = DateTime.UtcNow,
    };

    var adminUser = new UserEntity
    {
        Id = Guid.NewGuid(),
        IdentityUserId = $"pending|{Guid.NewGuid()}",
        Email = adminEmail,
        DisplayName = adminName,
        Role = "TENANT_ADMIN",
        TenantId = tenant.Id,
        IsActive = true,
        StripeSessionId = sessionId,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    db.Tenants.Add(tenant);
    db.Users.Add(adminUser);
    await db.SaveChangesAsync();

    logger.LogInformation("Tenant '{Name}' provisioned via Stripe checkout for {Email}", companyName, adminEmail);

    // Send setup email with link to create password
    var baseUrl = config["BaseUrl"] ?? "https://auditraks.com";
    var setupUrl = $"{baseUrl}/signup/set-password?session={Uri.EscapeDataString(sessionId)}";
    var (subject, htmlBody, textBody) = EmailTemplates.AccountSetup(adminName, companyName, setupUrl);
    try { await emailService.SendAsync(adminEmail, subject, htmlBody, textBody, CancellationToken.None); }
    catch (Exception ex) { logger.LogWarning(ex, "Failed to send setup email to {Email}", adminEmail); }
}
```

Note: The `UserManager` parameter is no longer used by this method. Keep it in the constructor signature for now — it may be used by future handlers. Remove unused `using` statements if the compiler warns.

- [ ] **Step 3.2: Build**

```bash
dotnet build packages/api/src/Tungsten.Api/Tungsten.Api.csproj
```

Expected: fails because `SignupEndpoints.cs` still calls `HandleCheckoutCompleted` without `sessionId`. Proceed to Task 4.

---

### Task 4: Update `SignupEndpoints.cs` — pass `session.Id` and update success URL call site

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Signup/SignupEndpoints.cs`

- [ ] **Step 4.1: Pass `session.Id` to `HandleCheckoutCompleted`**

In the `CheckoutSessionCompleted` case, add `session.Id` as the last argument:

```csharp
case Stripe.EventTypes.CheckoutSessionCompleted:
    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
    if (session?.Metadata != null)
    {
        await handler.HandleCheckoutCompleted(
            session.CustomerId ?? session.Customer?.Id ?? "",
            session.SubscriptionId ?? session.Subscription?.Id ?? "",
            session.Metadata.GetValueOrDefault("companyName", ""),
            session.Metadata.GetValueOrDefault("adminName", ""),
            session.Metadata.GetValueOrDefault("adminEmail", ""),
            session.Metadata.GetValueOrDefault("plan", "PRO"),
            session.Id);
    }
    break;
```

- [ ] **Step 4.2: Build**

```bash
dotnet build packages/api/src/Tungsten.Api/Tungsten.Api.csproj
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

### Task 5: Update `CreateCheckoutSession.cs` — change success URL

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Signup/CreateCheckoutSession.cs`

- [ ] **Step 5.1: Change the success URL**

Find line 62:
```csharp
SuccessUrl = $"{baseUrl}/signup/success",
```

Replace with:
```csharp
SuccessUrl = $"{baseUrl}/signup/set-password?session={{CHECKOUT_SESSION_ID}}",
```

Important: `{CHECKOUT_SESSION_ID}` is a Stripe-supplied literal placeholder. In a C# string it must be escaped as `{{CHECKOUT_SESSION_ID}}` inside an interpolated string, or written as a verbatim string. The double braces `{{` and `}}` produce literal `{` and `}` in the output, which is exactly what Stripe expects.

- [ ] **Step 5.2: Build**

```bash
dotnet build packages/api/src/Tungsten.Api/Tungsten.Api.csproj
```

Expected: `Build succeeded.`

---

### Task 6: Update `Login.cs` — detect pending users

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Auth/Login.cs`

- [ ] **Step 6.1: Add pending-user check before Identity lookup**

Add the DB check as the very first thing in the `Handle` method, before `userManager.FindByEmailAsync`:

```csharp
public static async Task<IResult> Handle(
    Request request,
    SignInManager<AppIdentityUser> signInManager,
    UserManager<AppIdentityUser> userManager,
    IJwtTokenService jwtTokenService,
    AppDbContext db,
    HttpContext httpContext,
    CancellationToken ct)
{
    // Check for incomplete account setup before Identity lookup
    var pendingUser = await db.Users.AsNoTracking()
        .AnyAsync(u => u.Email == request.Email && u.IdentityUserId.StartsWith("pending|"), ct);
    if (pendingUser)
        return TypedResults.Json(new { error = "ACCOUNT_SETUP_INCOMPLETE" }, statusCode: 400);

    var identityUser = await userManager.FindByEmailAsync(request.Email);
    // ... rest of method unchanged
```

- [ ] **Step 6.2: Build and run existing auth tests**

```bash
dotnet build packages/api/src/Tungsten.Api/Tungsten.Api.csproj
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~Auth" --no-build
```

Expected: all auth tests pass.

- [ ] **Step 6.3: Commit all backend changes to existing files**

```bash
git add packages/api/src/Tungsten.Api/Common/Services/EmailTemplates.cs
git add packages/api/src/Tungsten.Api/Features/Signup/StripeWebhookHandler.cs
git add packages/api/src/Tungsten.Api/Features/Signup/SignupEndpoints.cs
git add packages/api/src/Tungsten.Api/Features/Signup/CreateCheckoutSession.cs
git add packages/api/src/Tungsten.Api/Features/Auth/Login.cs
git commit -m "feat: update signup flow — remove temp Identity user, store session ID, detect pending login"
```

---

## Chunk 3: New Backend Endpoints

### Task 7: Add new webhook tests for `StripeSessionId` storage and no Identity user creation

**Files:**
- Modify: `packages/api/tests/Tungsten.Api.Tests/Features/Signup/StripeWebhookTests.cs`

The call-signature update (adding `sessionId` argument) was already done in Step 3.0. This task adds two new tests for the new behaviours.

- [ ] **Step 7.1: Add test asserting `StripeSessionId` is stored**

Add a new test after `HandleCheckoutCompleted_CreatesTenantAndUser`:

```csharp
[Fact]
public async Task HandleCheckoutCompleted_StoresStripeSessionId()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    var db = new AppDbContext(options);

    var handler = CreateHandler(db);
    await handler.HandleCheckoutCompleted("cus_123", "sub_123", "Acme Mining", "John Smith", "john@acme.com", "PRO", "cs_test_abc123");

    var user = await db.Users.FirstOrDefaultAsync();
    Assert.NotNull(user);
    Assert.Equal("cs_test_abc123", user.StripeSessionId);
}
```

- [ ] **Step 7.3: Add test asserting no AppIdentityUser is created**

```csharp
[Fact]
public async Task HandleCheckoutCompleted_DoesNotCreateIdentityUser()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
    var db = new AppDbContext(options);

    var userManager = Substitute.For<UserManager<AppIdentityUser>>(
        Substitute.For<IUserStore<AppIdentityUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);

    var handler = new StripeWebhookHandler(db, userManager,
        Substitute.For<ILogger<StripeWebhookHandler>>(),
        Substitute.For<IEmailService>(),
        new ConfigurationBuilder().Build());

    await handler.HandleCheckoutCompleted("cus_123", "sub_123", "Acme Mining", "John Smith", "john@acme.com", "PRO", "cs_test_abc");

    await userManager.DidNotReceive().CreateAsync(Arg.Any<AppIdentityUser>(), Arg.Any<string>());
}
```

- [ ] **Step 7.4: Run webhook tests**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~StripeWebhook" -v
```

Expected: all 7 tests pass (5 existing + 2 new).

---

### Task 8: Create `GetSignupSessionStatus.cs`

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Signup/GetSignupSessionStatus.cs`
- Modify: `packages/api/tests/Tungsten.Api.Tests/Features/Signup/GetSignupSessionStatusTests.cs` (create)

This is a static handler (same pattern as `Login.cs`) — no MediatR needed since there is no complex pipeline behaviour required.

- [ ] **Step 8.1: Write the failing test first**

Create `packages/api/tests/Tungsten.Api.Tests/Features/Signup/GetSignupSessionStatusTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class GetSignupSessionStatusTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Returns_Provisioned_False_WhenUserHasPendingPrefix()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "TRIAL", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "pending|abc", Email = "jane@acme.com",
            DisplayName = "Jane", Role = "TENANT_ADMIN", TenantId = tenantId,
            StripeSessionId = "cs_test_123", IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await GetSignupSessionStatus.CheckProvisionedAsync(db, "jane@acme.com", CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Returns_Provisioned_True_WhenIdentityUserIdIsReal()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "TRIAL", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "real-identity-user-id", Email = "jane@acme.com",
            DisplayName = "Jane", Role = "TENANT_ADMIN", TenantId = tenantId,
            StripeSessionId = "cs_test_123", IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await GetSignupSessionStatus.CheckProvisionedAsync(db, "jane@acme.com", CancellationToken.None);

        Assert.True(result);
    }
}
```

- [ ] **Step 8.2: Run test to confirm it fails**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~GetSignupSessionStatus" -v
```

Expected: compilation error — `GetSignupSessionStatus` does not exist yet.

- [ ] **Step 8.3: Create `GetSignupSessionStatus.cs`**

Create `packages/api/src/Tungsten.Api/Features/Signup/GetSignupSessionStatus.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Signup;

public static class GetSignupSessionStatus
{
    public record Response(bool Provisioned);

    public static async Task<IResult> Handle(
        string sessionId,
        AppDbContext db,
        CancellationToken ct)
    {
        // Fetch session from Stripe to get the customer email
        Stripe.Checkout.Session session;
        try
        {
            var service = new Stripe.Checkout.SessionService();
            session = await service.GetAsync(sessionId, cancellationToken: ct);
        }
        catch (Stripe.StripeException)
        {
            return TypedResults.NotFound(new { error = "Session not found." });
        }

        if (session.Status != "complete")
            return TypedResults.BadRequest(new { error = "Checkout session is not complete." });

        var email = session.CustomerDetails?.Email ?? session.CustomerEmail;
        if (string.IsNullOrEmpty(email))
            return TypedResults.BadRequest(new { error = "No email found on session." });

        var provisioned = await CheckProvisionedAsync(db, email, ct);
        return TypedResults.Ok(new Response(provisioned));
    }

    // Extracted for unit testing (bypasses Stripe API call)
    public static async Task<bool> CheckProvisionedAsync(AppDbContext db, string email, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return false;
        return !user.IdentityUserId.StartsWith("pending|");
    }
}
```

- [ ] **Step 8.4: Run tests**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~GetSignupSessionStatus" -v
```

Expected: 2 tests pass.

---

### Task 9: Create `SetInitialPassword.cs`

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Signup/SetInitialPassword.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Signup/SetInitialPasswordTests.cs`

- [ ] **Step 9.1: Write failing tests**

Create `packages/api/tests/Tungsten.Api.Tests/Features/Signup/SetInitialPasswordTests.cs`:

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Identity;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class SetInitialPasswordTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static UserManager<AppIdentityUser> CreateUserManager() =>
        Substitute.For<UserManager<AppIdentityUser>>(
            Substitute.For<IUserStore<AppIdentityUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

    [Fact]
    public async Task Returns400_WhenNoPendingUserFound()
    {
        var db = CreateDb();
        var userManager = CreateUserManager();
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((AppIdentityUser?)null);
        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContext = new DefaultHttpContext();

        // No user seeded — should get 400
        var result = await SetInitialPassword.HandleCoreAsync(
            "no-one@acme.com", "Password123!", db, userManager, jwtService, httpContext, CancellationToken.None);

        // Cast to IStatusCodeHttpResult (generic-invariant-safe) to check status code
        var statusResult = result as IStatusCodeHttpResult;
        Assert.NotNull(statusResult);
        Assert.Equal(400, statusResult.StatusCode);
    }

    [Fact]
    public async Task Returns409_WhenIdentityUserAlreadyExists()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "TRIAL", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "pending|abc", Email = "jane@acme.com",
            DisplayName = "Jane", Role = "TENANT_ADMIN", TenantId = tenantId,
            StripeSessionId = "cs_test_123", IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var userManager = CreateUserManager();
        userManager.FindByEmailAsync("jane@acme.com")
            .Returns(new AppIdentityUser { Email = "jane@acme.com" });

        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContext = new DefaultHttpContext();

        var result = await SetInitialPassword.HandleCoreAsync(
            "jane@acme.com", "Password123!", db, userManager, jwtService, httpContext, CancellationToken.None);

        // Cast to IStatusCodeHttpResult (generic-invariant-safe) to check status code
        var statusResult = result as IStatusCodeHttpResult;
        Assert.NotNull(statusResult);
        Assert.Equal(409, statusResult.StatusCode);
    }
}
```

- [ ] **Step 9.2: Run test to confirm compile error**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~SetInitialPassword" -v
```

Expected: compilation error — `SetInitialPassword` does not exist.

- [ ] **Step 9.3: Create `SetInitialPassword.cs`**

Create `packages/api/src/Tungsten.Api/Features/Signup/SetInitialPassword.cs`:

```csharp
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Identity;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Signup;

public static class SetInitialPassword
{
    public record Request(string SessionId, string Password);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SessionId).NotEmpty();
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        }
    }

    public static async Task<IResult> Handle(
        Request request,
        UserManager<AppIdentityUser> userManager,
        IJwtTokenService jwtTokenService,
        AppDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        // Validate input
        var validator = new Validator();
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.Json(new { error = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) }, statusCode: 400);

        // Get email from Stripe (source of truth — never trust client-supplied email)
        Stripe.Checkout.Session session;
        try
        {
            var service = new Stripe.Checkout.SessionService();
            session = await service.GetAsync(request.SessionId, cancellationToken: ct);
        }
        catch (Stripe.StripeException)
        {
            return TypedResults.Json(new { error = "Invalid session." }, statusCode: 400);
        }

        var email = session.CustomerDetails?.Email ?? session.CustomerEmail;
        if (string.IsNullOrEmpty(email))
            return TypedResults.Json(new { error = "No email found on session." }, statusCode: 400);

        return await HandleCoreAsync(email, request.Password, db, userManager, jwtTokenService, httpContext, ct);
    }

    // Extracted for unit testing (bypasses Stripe API call)
    public static async Task<IResult> HandleCoreAsync(
        string email,
        string password,
        AppDbContext db,
        UserManager<AppIdentityUser> userManager,
        IJwtTokenService jwtTokenService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var appUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IdentityUserId.StartsWith("pending|"), ct);

        if (appUser is null)
            return TypedResults.Json(new { error = "No pending account found for this email." }, statusCode: 400);

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return TypedResults.Json(new { error = "Account already set up. Please sign in." }, statusCode: 409);

        var identityUser = new AppIdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,  // Stripe verified the address
            AppUserId = appUser.Id,
        };

        var createResult = await userManager.CreateAsync(identityUser, password);
        if (!createResult.Succeeded)
            return TypedResults.Json(
                new { error = string.Join(" ", createResult.Errors.Select(e => e.Description)) },
                statusCode: 400);

        appUser.IdentityUserId = identityUser.Id;
        appUser.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var accessToken = jwtTokenService.GenerateAccessToken(identityUser.Id, email);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        await jwtTokenService.SaveRefreshTokenAsync(identityUser.Id, refreshToken, ct);

        httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth/refresh",
            MaxAge = TimeSpan.FromDays(14),
        });

        return TypedResults.Ok(new { accessToken });
    }
}
```

- [ ] **Step 9.4: Run tests**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~SetInitialPassword" -v
```

Expected: 2 tests pass.

---

### Task 10: Create `ResendSetupEmail.cs`

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Signup/ResendSetupEmail.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Signup/ResendSetupEmailTests.cs`

- [ ] **Step 10.1: Write failing test**

Create `packages/api/tests/Tungsten.Api.Tests/Features/Signup/ResendSetupEmailTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class ResendSetupEmailTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Returns200_WhenEmailNotFound_DoesNotSendEmail()
    {
        var db = CreateDb();
        var emailService = Substitute.For<IEmailService>();
        var config = new ConfigurationBuilder().Build();

        var request = new ResendSetupEmail.Request("nobody@nowhere.com");
        var result = await ResendSetupEmail.Handle(request, db, emailService, config, CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok>(result);
        await emailService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendsSetupEmail_WhenPendingUserFound()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "TRIAL", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "pending|abc", Email = "jane@acme.com",
            DisplayName = "Jane", Role = "TENANT_ADMIN", TenantId = tenantId,
            StripeSessionId = "cs_test_xyz", IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var emailService = Substitute.For<IEmailService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BaseUrl"] = "https://test.example.com" })
            .Build();

        var request = new ResendSetupEmail.Request("jane@acme.com");
        var result = await ResendSetupEmail.Handle(request, db, emailService, config, CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok>(result);
        await emailService.Received(1).SendAsync(
            "jane@acme.com",
            Arg.Any<string>(),
            Arg.Is<string>(h => h.Contains("cs_test_xyz")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 10.2: Run test to confirm compile error**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~ResendSetupEmail" -v
```

Expected: compilation error.

- [ ] **Step 10.3: Create `ResendSetupEmail.cs`**

Create `packages/api/src/Tungsten.Api/Features/Signup/ResendSetupEmail.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Signup;

public static class ResendSetupEmail
{
    public record Request(string Email);

    public static async Task<IResult> Handle(
        Request request,
        AppDbContext db,
        IEmailService emailService,
        IConfiguration config,
        CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IdentityUserId.StartsWith("pending|"), ct);

        // Always return 200 — no information leak about whether email exists
        if (user is null || string.IsNullOrEmpty(user.StripeSessionId))
            return TypedResults.Ok();

        var baseUrl = config["BaseUrl"] ?? "https://auditraks.com";
        var setupUrl = $"{baseUrl}/signup/set-password?session={Uri.EscapeDataString(user.StripeSessionId)}";
        var (subject, htmlBody, textBody) = EmailTemplates.AccountSetup(user.DisplayName, user.Tenant.Name, setupUrl);

        try { await emailService.SendAsync(user.Email, subject, htmlBody, textBody, ct); }
        catch { /* swallow — don't leak send failures */ }

        return TypedResults.Ok();
    }
}
```

- [ ] **Step 10.4: Run tests**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~ResendSetupEmail" -v
```

Expected: 2 tests pass.

---

### Task 11: Register new endpoints in `SignupEndpoints.cs`

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Signup/SignupEndpoints.cs`

- [ ] **Step 11.1: Add three new endpoints to `MapSignupEndpoints`**

Add after the existing `MapPost("/api/stripe/webhook", ...)` registration, before `return app;`:

```csharp
app.MapGet("/api/signup/session/{sessionId}", async (
    string sessionId,
    AppDbContext db,
    CancellationToken ct) =>
    await GetSignupSessionStatus.Handle(sessionId, db, ct));

app.MapPost("/api/signup/set-password", async (
    SetInitialPassword.Request request,
    UserManager<AppIdentityUser> userManager,
    IJwtTokenService jwtTokenService,
    AppDbContext db,
    HttpContext httpContext,
    CancellationToken ct) =>
    await SetInitialPassword.Handle(request, userManager, jwtTokenService, db, httpContext, ct));

app.MapPost("/api/signup/resend-setup", async (
    ResendSetupEmail.Request request,
    AppDbContext db,
    IEmailService emailService,
    IConfiguration config,
    CancellationToken ct) =>
    await ResendSetupEmail.Handle(request, db, emailService, config, ct));
```

Add the required `using` at the top of the file:
```csharp
using Tungsten.Api.Common.Auth;
```

- [ ] **Step 11.2: Build**

```bash
dotnet build packages/api/src/Tungsten.Api/Tungsten.Api.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 11.3: Run all signup tests**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/ --filter "FullyQualifiedName~Signup" -v
```

Expected: all tests pass (webhook + new handlers).

- [ ] **Step 11.4: Run full test suite**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/
```

Expected: all tests pass.

- [ ] **Step 11.5: Commit new endpoints**

```bash
git add packages/api/src/Tungsten.Api/Features/Signup/GetSignupSessionStatus.cs
git add packages/api/src/Tungsten.Api/Features/Signup/SetInitialPassword.cs
git add packages/api/src/Tungsten.Api/Features/Signup/ResendSetupEmail.cs
git add packages/api/src/Tungsten.Api/Features/Signup/SignupEndpoints.cs
git add packages/api/tests/Tungsten.Api.Tests/Features/Signup/
git commit -m "feat: add session status, set-password, and resend-setup endpoints"
```

---

## Chunk 4: Frontend

### Task 12: Create `SetPasswordComponent`

**Files:**
- Create: `packages/web/src/app/features/signup/set-password.component.ts`

- [ ] **Step 12.1: Create the component**

Create `packages/web/src/app/features/signup/set-password.component.ts`:

```typescript
import {
  Component, inject, signal, computed, OnInit,
  ChangeDetectionStrategy, DestroyRef
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { timer, EMPTY } from 'rxjs';
import { switchMap, takeWhile, catchError } from 'rxjs/operators';
import { AuthService } from '../../core/auth/auth.service';
import { environment } from '../../../environments/environment';

const POLL_INTERVAL_MS = 2000;
const POLL_TIMEOUT_MS = 30_000;

type State = 'provisioning' | 'ready' | 'submitting' | 'timeout' | 'error';

@Component({
  selector: 'app-set-password',
  standalone: true,
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="min-h-screen flex items-center justify-center bg-slate-50 px-6">
      <div class="w-full max-w-sm">
        <div class="flex items-center gap-2 justify-center mb-8">
          <img src="assets/auditraks-logo.png" alt="auditraks" class="h-10" />
        </div>

        <div class="bg-white rounded-2xl shadow-sm border border-slate-200 p-8">

          @if (state() === 'provisioning') {
            <div class="text-center py-4">
              <div class="w-8 h-8 border-2 border-indigo-600 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
              <h1 class="text-xl font-bold text-slate-900 mb-2">Setting up your account</h1>
              <p class="text-sm text-slate-500">This usually takes just a few seconds…</p>
            </div>
          }

          @if (state() === 'ready' || state() === 'submitting') {
            <h1 class="text-xl font-bold text-slate-900 text-center mb-2">Create your password</h1>
            <p class="text-sm text-slate-500 text-center mb-6">You'll use this to sign in to auditraks.</p>

            @if (errorMessage()) {
              <div class="mb-5 bg-rose-50 border border-rose-200 rounded-xl p-4">
                <p class="text-sm text-rose-700">{{ errorMessage() }}</p>
              </div>
            }

            <form (ngSubmit)="onSubmit()" class="space-y-4">
              <div>
                <label for="password" class="block text-sm font-medium text-slate-700 mb-1">Password</label>
                <input
                  id="password" name="password" type="password"
                  [(ngModel)]="password" required
                  [disabled]="state() === 'submitting'"
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent disabled:opacity-50"
                  placeholder="8+ characters"
                />
              </div>

              <div>
                <label for="confirm" class="block text-sm font-medium text-slate-700 mb-1">Confirm password</label>
                <input
                  id="confirm" name="confirm" type="password"
                  [(ngModel)]="confirm" required
                  [disabled]="state() === 'submitting'"
                  class="w-full border border-slate-300 rounded-xl px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:border-transparent disabled:opacity-50"
                />
              </div>

              <p class="text-xs text-slate-400">8+ characters, uppercase, lowercase, and a digit.</p>

              <button
                type="submit"
                [disabled]="state() === 'submitting'"
                class="w-full bg-indigo-600 text-white py-3 px-4 rounded-xl font-medium hover:bg-indigo-700 transition-all disabled:opacity-50"
              >
                @if (state() === 'submitting') {
                  <div class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin inline-block mr-2"></div>
                  Creating account…
                } @else {
                  Create account
                }
              </button>
            </form>
          }

          @if (state() === 'timeout' || state() === 'error') {
            <div class="text-center py-4">
              <svg class="w-12 h-12 text-rose-400 mx-auto mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
                  d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
              </svg>
              <h1 class="text-xl font-bold text-slate-900 mb-2">Something went wrong</h1>
              <p class="text-sm text-slate-500 mb-6">
                We couldn't complete your account setup.
                Please contact support and we'll get you sorted.
              </p>
              <a
                href="mailto:support@auditraks.com"
                class="inline-block bg-indigo-600 text-white py-2.5 px-6 rounded-xl font-medium hover:bg-indigo-700 transition-all text-sm"
              >
                Contact support
              </a>
            </div>
          }

        </div>
      </div>
    </div>
  `,
})
export class SetPasswordComponent implements OnInit {
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private destroyRef = inject(DestroyRef);

  readonly state = signal<State>('provisioning');
  readonly errorMessage = signal('');
  password = '';
  confirm = '';

  private sessionId = '';
  private pollStart = 0;

  ngOnInit(): void {
    this.sessionId = this.route.snapshot.queryParamMap.get('session') ?? '';
    if (!this.sessionId) {
      this.router.navigate(['/signup']);
      return;
    }
    this.startPolling();
  }

  private startPolling(): void {
    this.pollStart = Date.now();

    // timer(0, interval) fires immediately then every interval — no initial delay.
    // catchError inside switchMap absorbs per-request errors so the timer keeps running.
    timer(0, POLL_INTERVAL_MS)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        takeWhile(() => this.state() === 'provisioning'),
        switchMap(() =>
          this.http
            .get<{ provisioned: boolean }>(
              `${environment.apiUrl}/api/signup/session/${this.sessionId}`
            )
            .pipe(catchError(() => EMPTY)) // absorb HTTP errors — keep the timer running
        )
      )
      .subscribe((res) => {
        if (res.provisioned) {
          this.state.set('ready');
        } else if (Date.now() - this.pollStart > POLL_TIMEOUT_MS) {
          this.state.set('timeout');
        }
      });
  }

  onSubmit(): void {
    if (this.state() !== 'ready') return;
    if (!this.password || !this.confirm) return;

    if (this.password !== this.confirm) {
      this.errorMessage.set('Passwords do not match.');
      return;
    }

    this.errorMessage.set('');
    this.state.set('submitting');

    this.http
      .post<{ accessToken: string }>(`${environment.apiUrl}/api/signup/set-password`, {
        sessionId: this.sessionId,
        password: this.password,
      })
      .subscribe({
        next: async (res) => {
          this.auth.setAccessToken(res.accessToken);
          await this.auth.loadProfile();
          this.router.navigate(['/admin']);
        },
        error: (err) => {
          const status = err?.status;
          if (status === 409) {
            this.router.navigate(['/login'], { queryParams: { hint: 'already-setup' } });
          } else {
            this.state.set('ready');
            this.errorMessage.set(
              err?.error?.error ?? 'Something went wrong. Please try again.'
            );
          }
        },
      });
  }
}
```

- [ ] **Step 12.2: Build frontend**

```bash
cd packages/web && ng build --configuration=development 2>&1 | tail -20
```

Expected: no errors.

---

### Task 13: Update `app.routes.ts` — add new route, remove old one

**Files:**
- Modify: `packages/web/src/app/app.routes.ts`

- [ ] **Step 13.1: Replace the `signup/success` route with `signup/set-password`**

Find:
```typescript
{
  path: 'signup/success',
  loadComponent: () => import('./features/signup/signup-success.component').then(m => m.SignupSuccessComponent),
},
```

Replace with:
```typescript
{
  path: 'signup/set-password',
  loadComponent: () => import('./features/signup/set-password.component').then(m => m.SetPasswordComponent),
},
```

- [ ] **Step 13.2: Build to verify no broken imports**

```bash
cd packages/web && ng build --configuration=development 2>&1 | tail -20
```

Expected: `Build at: ... - Hash: ...` with no errors.

---

### Task 14: Update `LoginComponent` — handle `ACCOUNT_SETUP_INCOMPLETE` and `hint=already-setup`

**Files:**
- Modify: `packages/web/src/app/features/auth/login.component.ts`

- [ ] **Step 14.1: Add signals and constructor changes**

Add a new readonly signal:
```typescript
readonly setupIncomplete = signal(false);
readonly alreadySetup = signal(false);
```

Update the `constructor` to also handle `hint`:
```typescript
constructor() {
  const params = this.route.snapshot.queryParamMap;
  if (params.get('emailConfirmed') === 'true') {
    this.emailConfirmed.set(true);
  }
  if (params.get('hint') === 'already-setup') {
    this.alreadySetup.set(true);
  }
}
```

- [ ] **Step 14.2: Update the error handler in `onSubmit`**

Replace the `error:` callback:
```typescript
error: (err) => {
  this.submitting.set(false);
  const errorCode = err?.error?.error;
  if (errorCode === 'ACCOUNT_SETUP_INCOMPLETE') {
    this.setupIncomplete.set(true);
    this.errorMessage.set('');
  } else {
    this.errorMessage.set(errorCode || 'Sign in failed. Please try again.');
  }
},
```

- [ ] **Step 14.3: Add banners to template**

Add these two `@if` blocks immediately after the existing `emailConfirmed` banner (after line ~25):

```html
@if (alreadySetup()) {
  <div class="mb-5 bg-emerald-50 border border-emerald-200 rounded-xl p-4">
    <p class="text-sm text-emerald-700">Your account is already set up. Please sign in.</p>
  </div>
}

@if (setupIncomplete()) {
  <div class="mb-5 bg-amber-50 border border-amber-200 rounded-xl p-4">
    <p class="text-sm text-amber-700 mb-2">
      Your account setup is incomplete. Check your email for a setup link, or resend it below.
    </p>
    <button
      type="button"
      (click)="resendSetupEmail()"
      [disabled]="resendSent()"
      class="text-xs font-medium text-amber-700 underline disabled:no-underline disabled:opacity-60"
    >
      {{ resendSent() ? 'Setup email sent. Check your inbox.' : 'Resend setup email' }}
    </button>
  </div>
}
```

- [ ] **Step 14.4: Add `resendSent` signal and `resendSetupEmail` method to the class**

Add signals:
```typescript
readonly resendSent = signal(false);
```

Add method:
```typescript
resendSetupEmail(): void {
  if (!this.email || this.resendSent()) return;
  this.http.post(`${environment.apiUrl}/api/signup/resend-setup`, { email: this.email })
    .subscribe({ next: () => this.resendSent.set(true), error: () => this.resendSent.set(true) });
}
```

Add `HttpClient` injection. `provideHttpClient()` is already registered at the application level in `app.config.ts` — do **not** add `HttpClientModule` to the component's imports array. Just inject it:

```typescript
private http = inject(HttpClient);
```

Add the import at the top of the file:
```typescript
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
```

- [ ] **Step 14.5: Build**

```bash
cd packages/web && ng build --configuration=development 2>&1 | tail -20
```

Expected: no errors.

- [ ] **Step 14.6: Delete `signup-success.component.ts`**

```bash
rm packages/web/src/app/features/signup/signup-success.component.ts
```

Rebuild to confirm no dangling references:
```bash
cd packages/web && ng build --configuration=development 2>&1 | tail -20
```

Expected: no errors.

- [ ] **Step 14.7: Run full backend test suite one final time**

```bash
dotnet test packages/api/tests/Tungsten.Api.Tests/
```

Expected: all tests pass.

- [ ] **Step 14.8: Final commit**

```bash
git add packages/web/src/app/features/signup/set-password.component.ts
git add packages/web/src/app/app.routes.ts
git add packages/web/src/app/features/auth/login.component.ts
git rm packages/web/src/app/features/signup/signup-success.component.ts
git commit -m "feat: add SetPasswordComponent, update routes and login for post-stripe setup flow"
```

---

## Verification Checklist

After all tasks complete:

- [ ] `dotnet build` — no errors
- [ ] `dotnet test` — all tests pass
- [ ] `ng build` — no errors
- [ ] Stripe success URL in `CreateCheckoutSession.cs` contains `{CHECKOUT_SESSION_ID}` literal (escaped as `{{CHECKOUT_SESSION_ID}}`)
- [ ] `signup-success.component.ts` deleted, `signup/success` route removed
- [ ] `signup/set-password` route added
- [ ] `EmailTemplates.Welcome` renamed to `AccountSetup` — no "Sign in with Google" in HTML
- [ ] `StripeWebhookHandler` no longer calls `userManager.CreateAsync`
- [ ] `Login.cs` returns `{ error: "ACCOUNT_SETUP_INCOMPLETE" }` for pending users
- [ ] `SetPasswordComponent` uses `DestroyRef` + `takeUntilDestroyed` for polling teardown
