# Phase 13: Self-Service Signup + Stripe Billing — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add self-service customer onboarding with Stripe Checkout (60-day trial, $249/month) and webhook-driven tenant provisioning.

**Architecture:** Public `/api/signup/checkout` endpoint creates a Stripe Checkout Session. Stripe webhook at `/api/stripe/webhook` provisions tenants on `checkout.session.completed` and manages lifecycle via `invoice.paid`, `invoice.payment_failed`, `customer.subscription.deleted`. TenantStatusBehaviour updated for TRIAL/CANCELLED states.

**Tech Stack:** .NET 10, Stripe.net SDK, MediatR, EF Core + PostgreSQL, Angular 21

**Spec:** `docs/superpowers/specs/2026-03-23-phase-13-stripe-billing-design.md`

---

## Chunk 1: Database + Stripe Infrastructure

### Task 1: Add Stripe Fields to TenantEntity + Migration

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/TenantEntity.cs`
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/TenantConfiguration.cs`

- [ ] **Step 1: Add Stripe fields to TenantEntity**

Read `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/TenantEntity.cs`, then add:

```csharp
public string? StripeCustomerId { get; set; }
public string? StripeSubscriptionId { get; set; }
public string? PlanName { get; set; }
public DateTime? TrialEndsAt { get; set; }
```

- [ ] **Step 2: Add configuration for new fields**

Read `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Configurations/TenantConfiguration.cs`, then add:

```csharp
builder.Property(e => e.StripeCustomerId).HasMaxLength(100);
builder.Property(e => e.StripeSubscriptionId).HasMaxLength(100);
builder.Property(e => e.PlanName).HasMaxLength(50);

builder.HasIndex(e => e.StripeCustomerId).IsUnique().HasFilter("\"StripeCustomerId\" IS NOT NULL").HasDatabaseName("ix_tenants_stripe_customer");
builder.HasIndex(e => e.StripeSubscriptionId).IsUnique().HasFilter("\"StripeSubscriptionId\" IS NOT NULL").HasDatabaseName("ix_tenants_stripe_subscription");
```

- [ ] **Step 3: Generate migration**

Run: `cd /c/__edMVP && dotnet ef migrations add AddStripeFields --project packages/api/src/Tungsten.Api`

- [ ] **Step 4: Build to verify**

Run: `cd /c/__edMVP/packages/api && dotnet build`

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Infrastructure/Persistence/ packages/api/src/Tungsten.Api/Migrations/
git commit -m "feat: add Stripe fields to TenantEntity with unique indexes and migration"
```

---

### Task 2: Install Stripe.net NuGet Package

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Tungsten.Api.csproj`

- [ ] **Step 1: Install package**

Run: `cd /c/__edMVP/packages/api/src/Tungsten.Api && dotnet add package Stripe.net`

- [ ] **Step 2: Build to verify**

Run: `cd /c/__edMVP/packages/api && dotnet build`

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Tungsten.Api.csproj
git commit -m "chore: add Stripe.net NuGet package"
```

---

### Task 3: Update TenantStatusBehaviour for TRIAL + CANCELLED

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Behaviours/TenantStatusBehaviour.cs`
- Modify: `packages/api/tests/Tungsten.Api.Tests/Common/Behaviours/TenantStatusBehaviourTests.cs`

- [ ] **Step 1: Add tests for TRIAL and CANCELLED**

Add to the existing `TenantStatusBehaviourTests.cs`:

```csharp
[Fact]
public async Task Handle_TrialTenant_ProceedsToHandler()
{
    var currentUser = Substitute.For<ICurrentUserService>();
    currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("TRIAL");
    currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("SUPPLIER");
    var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

    var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);
    var response = Result<string>.Success("trial ok");

    var result = await behaviour.Handle(
        new TestCommand("test"), _ => Task.FromResult(response), CancellationToken.None);

    Assert.True(result.IsSuccess);
}

[Fact]
public async Task Handle_CancelledTenant_ReturnsFailureWithDistinctMessage()
{
    var currentUser = Substitute.For<ICurrentUserService>();
    currentUser.GetTenantStatusAsync(Arg.Any<CancellationToken>()).Returns("CANCELLED");
    currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("SUPPLIER");
    var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };

    var behaviour = new TenantStatusBehaviour<TestCommand, Result<string>>(currentUser, accessor);

    var result = await behaviour.Handle(
        new TestCommand("test"), _ => Task.FromResult(Result<string>.Success("nope")), CancellationToken.None);

    Assert.False(result.IsSuccess);
    Assert.Contains("cancelled", result.Error!, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~TenantStatusBehaviourTests"`

- [ ] **Step 3: Update TenantStatusBehaviour**

Read `packages/api/src/Tungsten.Api/Common/Behaviours/TenantStatusBehaviour.cs`. Change the `if (tenantStatus == "SUSPENDED")` check to:

```csharp
if (tenantStatus is "SUSPENDED" or "CANCELLED")
{
    var role = await currentUser.GetRoleAsync(ct);
    if (role != Roles.Admin)
    {
        var message = tenantStatus == "SUSPENDED"
            ? "Your organization's account has been suspended. Contact support."
            : "Your subscription has been cancelled.";

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(string)])!;
            return (TResponse)failureMethod.Invoke(null, [message])!;
        }

        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(message);
        }
    }
}
```

- [ ] **Step 4: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet test --filter "FullyQualifiedName~TenantStatusBehaviourTests"`
Expected: PASS (6 tests)

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Behaviours/TenantStatusBehaviour.cs packages/api/tests/Tungsten.Api.Tests/Common/Behaviours/TenantStatusBehaviourTests.cs
git commit -m "feat: update TenantStatusBehaviour for TRIAL and CANCELLED statuses"
```

---

### Task 4: Update UpdateTenantStatus Validator

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Platform/UpdateTenantStatus.cs`

- [ ] **Step 1: Add CANCELLED to allowed values**

Read `packages/api/src/Tungsten.Api/Features/Platform/UpdateTenantStatus.cs`. Change the validator from:
```csharp
RuleFor(x => x.Status).Must(s => s is "ACTIVE" or "SUSPENDED")
```
to:
```csharp
RuleFor(x => x.Status).Must(s => s is "ACTIVE" or "SUSPENDED" or "CANCELLED")
    .WithMessage("Status must be ACTIVE, SUSPENDED, or CANCELLED");
```

Note: TRIAL is not manually settable — reserved for webhook provisioning.

- [ ] **Step 2: Build and test**

Run: `cd /c/__edMVP/packages/api && dotnet build && dotnet test`

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Platform/UpdateTenantStatus.cs
git commit -m "feat: add CANCELLED to UpdateTenantStatus validator"
```

---

## Chunk 2: Stripe Checkout + Webhook Endpoints

### Task 5: Create Checkout Session Endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Signup/CreateCheckoutSession.cs`
- Create: `packages/api/src/Tungsten.Api/Features/Signup/SignupEndpoints.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Signup/CreateCheckoutSessionTests.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Write the test**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Features/Signup/CreateCheckoutSessionTests.cs
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class CreateCheckoutSessionTests
{
    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Existing", SchemaPrefix = "existing", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), Auth0Sub = "auth0|x", Email = "taken@acme.com", DisplayName = "X", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new CreateCheckoutSession.Handler(db, null!); // Stripe service not needed for this test
        var result = await handler.Handle(
            new CreateCheckoutSession.Command("Acme", "John", "taken@acme.com"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already in use", result.Error!);
    }
}
```

- [ ] **Step 2: Implement CreateCheckoutSession**

```csharp
// packages/api/src/Tungsten.Api/Features/Signup/CreateCheckoutSession.cs
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Tungsten.Api.Common;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Signup;

public static class CreateCheckoutSession
{
    public record Command(string CompanyName, string Name, string Email) : IRequest<Result<Response>>;

    public record Response(string CheckoutUrl);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    public class Handler(AppDbContext db, IConfiguration config)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var emailExists = await db.Users.AnyAsync(u => u.Email == cmd.Email, ct);
            if (emailExists)
                return Result<Response>.Failure($"Email '{cmd.Email}' is already in use");

            var priceId = config["Stripe:PriceId"];
            var baseUrl = config["BaseUrl"] ?? "https://accutrac-web.onrender.com";

            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                CustomerEmail = cmd.Email,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    }
                ],
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = 60,
                    Metadata = new Dictionary<string, string>
                    {
                        ["companyName"] = cmd.CompanyName,
                        ["adminName"] = cmd.Name,
                        ["adminEmail"] = cmd.Email,
                    }
                },
                SuccessUrl = $"{baseUrl}/signup/success",
                CancelUrl = $"{baseUrl}/signup",
                Metadata = new Dictionary<string, string>
                {
                    ["companyName"] = cmd.CompanyName,
                    ["adminName"] = cmd.Name,
                    ["adminEmail"] = cmd.Email,
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: ct);

            return Result<Response>.Success(new Response(session.Url));
        }
    }
}
```

- [ ] **Step 3: Create SignupEndpoints**

```csharp
// packages/api/src/Tungsten.Api/Features/Signup/SignupEndpoints.cs
using MediatR;

namespace Tungsten.Api.Features.Signup;

public static class SignupEndpoints
{
    public static IEndpointRouteBuilder MapSignupEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/signup/checkout", async (CreateCheckoutSession.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireRateLimiting("public");

        return app;
    }
}
```

- [ ] **Step 4: Register in Program.cs**

Add to `packages/api/src/Tungsten.Api/Program.cs`:

1. Add Stripe API key configuration after the Sentry section:
```csharp
// Stripe
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"];
if (!string.IsNullOrEmpty(stripeSecretKey))
    Stripe.StripeConfiguration.ApiKey = stripeSecretKey;
```

2. Add endpoint registration after `app.MapPlatformEndpoints();`:
```csharp
app.MapSignupEndpoints();
```

3. Add using:
```csharp
using Tungsten.Api.Features.Signup;
```

- [ ] **Step 5: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet build && dotnet test`

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Signup/ packages/api/tests/Tungsten.Api.Tests/Features/Signup/ packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: add Stripe checkout session endpoint for self-service signup"
```

---

### Task 6: Stripe Webhook Endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Signup/StripeWebhook.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Signup/SignupEndpoints.cs`
- Create: `packages/api/tests/Tungsten.Api.Tests/Features/Signup/StripeWebhookTests.cs`

- [ ] **Step 1: Write tests for webhook tenant provisioning**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Features/Signup/StripeWebhookTests.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class StripeWebhookTests
{
    [Fact]
    public async Task HandleCheckoutCompleted_CreatesTenantAndUser()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();

        var handler = new StripeWebhookHandler(db, logger);
        await handler.HandleCheckoutCompleted(
            "cus_test123", "sub_test123",
            "Acme Mining", "John Smith", "john@acme.com");

        var tenant = await db.Tenants.FirstOrDefaultAsync();
        Assert.NotNull(tenant);
        Assert.Equal("Acme Mining", tenant.Name);
        Assert.Equal("TRIAL", tenant.Status);
        Assert.Equal("cus_test123", tenant.StripeCustomerId);
        Assert.Equal("sub_test123", tenant.StripeSubscriptionId);
        Assert.Equal("PRO", tenant.PlanName);
        Assert.NotNull(tenant.TrialEndsAt);

        var user = await db.Users.FirstOrDefaultAsync();
        Assert.NotNull(user);
        Assert.Equal("john@acme.com", user.Email);
        Assert.Equal("TENANT_ADMIN", user.Role);
        Assert.StartsWith("pending|", user.Auth0Sub);
    }

    [Fact]
    public async Task HandleCheckoutCompleted_DuplicateEmail_SkipsCreation()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Existing", SchemaPrefix = "existing", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), Auth0Sub = "auth0|x", Email = "john@acme.com", DisplayName = "X", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();
        var handler = new StripeWebhookHandler(db, logger);
        await handler.HandleCheckoutCompleted(
            "cus_test456", "sub_test456",
            "Acme Mining", "John Smith", "john@acme.com");

        // Should not create a second tenant
        Assert.Equal(1, await db.Tenants.CountAsync());
    }

    [Fact]
    public async Task HandleInvoicePaid_TransitionsTrialToActive()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "TRIAL", StripeSubscriptionId = "sub_test", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();
        var handler = new StripeWebhookHandler(db, logger);
        await handler.HandleInvoicePaid("sub_test");

        var tenant = await db.Tenants.FindAsync(tenantId);
        Assert.Equal("ACTIVE", tenant!.Status);
    }

    [Fact]
    public async Task HandlePaymentFailed_SuspendsTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", StripeSubscriptionId = "sub_test", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();
        var handler = new StripeWebhookHandler(db, logger);
        await handler.HandlePaymentFailed("sub_test");

        var tenant = await db.Tenants.FindAsync(tenantId);
        Assert.Equal("SUSPENDED", tenant!.Status);
    }

    [Fact]
    public async Task HandleSubscriptionDeleted_CancelsTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", StripeSubscriptionId = "sub_test", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();
        var handler = new StripeWebhookHandler(db, logger);
        await handler.HandleSubscriptionDeleted("sub_test");

        var tenant = await db.Tenants.FindAsync(tenantId);
        Assert.Equal("CANCELLED", tenant!.Status);
    }
}
```

- [ ] **Step 2: Implement StripeWebhookHandler**

```csharp
// packages/api/src/Tungsten.Api/Features/Signup/StripeWebhook.cs
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Features.Signup;

public class StripeWebhookHandler(AppDbContext db, ILogger<StripeWebhookHandler> logger)
{
    public async Task HandleCheckoutCompleted(
        string customerId, string subscriptionId,
        string companyName, string adminName, string adminEmail)
    {
        // Dedup: skip if email already exists
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

        var tenant = new TenantEntity
        {
            Id = Guid.NewGuid(),
            Name = companyName,
            SchemaPrefix = prefix,
            Status = "TRIAL",
            StripeCustomerId = customerId,
            StripeSubscriptionId = subscriptionId,
            PlanName = "PRO",
            TrialEndsAt = DateTime.UtcNow.AddDays(60),
            CreatedAt = DateTime.UtcNow,
        };

        var adminUser = new UserEntity
        {
            Id = Guid.NewGuid(),
            Auth0Sub = $"pending|{Guid.NewGuid()}",
            Email = adminEmail,
            DisplayName = adminName,
            Role = "TENANT_ADMIN",
            TenantId = tenant.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        db.Tenants.Add(tenant);
        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        logger.LogInformation("Tenant '{Name}' provisioned via Stripe checkout for {Email}", companyName, adminEmail);
    }

    public async Task HandleInvoicePaid(string subscriptionId)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.StripeSubscriptionId == subscriptionId);
        if (tenant is null)
        {
            logger.LogWarning("invoice.paid: no tenant found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        if (tenant.Status is "TRIAL" or "SUSPENDED")
        {
            tenant.Status = "ACTIVE";
            await db.SaveChangesAsync();
            logger.LogInformation("Tenant '{Name}' activated via invoice.paid", tenant.Name);
        }
    }

    public async Task HandlePaymentFailed(string subscriptionId)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.StripeSubscriptionId == subscriptionId);
        if (tenant is null)
        {
            logger.LogWarning("invoice.payment_failed: no tenant found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        tenant.Status = "SUSPENDED";
        await db.SaveChangesAsync();
        logger.LogWarning("Tenant '{Name}' suspended due to payment failure", tenant.Name);
    }

    public async Task HandleSubscriptionDeleted(string subscriptionId)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.StripeSubscriptionId == subscriptionId);
        if (tenant is null)
        {
            logger.LogWarning("customer.subscription.deleted: no tenant found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        tenant.Status = "CANCELLED";
        await db.SaveChangesAsync();
        logger.LogWarning("Tenant '{Name}' cancelled via subscription deletion", tenant.Name);
    }

    private static string GenerateSchemaPrefix(string name)
    {
        var prefix = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "_").Trim('_');
        return prefix.Length > 50 ? prefix[..50] : prefix;
    }
}
```

- [ ] **Step 3: Add webhook endpoint to SignupEndpoints**

Add to `packages/api/src/Tungsten.Api/Features/Signup/SignupEndpoints.cs`:

```csharp
app.MapPost("/api/stripe/webhook", async (HttpContext httpContext, AppDbContext db, IConfiguration config, ILogger<StripeWebhookHandler> logger) =>
{
    var json = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
    var webhookSecret = config["Stripe:WebhookSecret"];

    Stripe.Event stripeEvent;
    try
    {
        stripeEvent = Stripe.EventUtility.ConstructEvent(
            json, httpContext.Request.Headers["Stripe-Signature"], webhookSecret);
    }
    catch (Stripe.StripeException)
    {
        return Results.BadRequest("Invalid signature");
    }

    var handler = new StripeWebhookHandler(db, logger);

    switch (stripeEvent.Type)
    {
        case "checkout.session.completed":
            var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
            if (session?.Metadata != null)
            {
                await handler.HandleCheckoutCompleted(
                    session.CustomerId ?? session.Customer?.Id ?? "",
                    session.SubscriptionId ?? session.Subscription?.Id ?? "",
                    session.Metadata.GetValueOrDefault("companyName", ""),
                    session.Metadata.GetValueOrDefault("adminName", ""),
                    session.Metadata.GetValueOrDefault("adminEmail", ""));
            }
            break;

        case "invoice.paid":
            var paidInvoice = stripeEvent.Data.Object as Stripe.Invoice;
            if (paidInvoice?.SubscriptionId != null)
                await handler.HandleInvoicePaid(paidInvoice.SubscriptionId);
            break;

        case "invoice.payment_failed":
            var failedInvoice = stripeEvent.Data.Object as Stripe.Invoice;
            if (failedInvoice?.SubscriptionId != null)
                await handler.HandlePaymentFailed(failedInvoice.SubscriptionId);
            break;

        case "customer.subscription.deleted":
            var subscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (subscription?.Id != null)
                await handler.HandleSubscriptionDeleted(subscription.Id);
            break;
    }

    return Results.Ok();
}).DisableAntiforgery();
```

Add usings at the top of SignupEndpoints.cs:
```csharp
using Tungsten.Api.Infrastructure.Persistence;
```

- [ ] **Step 4: Register StripeWebhookHandler as scoped service**

In `Program.cs`, add:
```csharp
builder.Services.AddScoped<StripeWebhookHandler>();
```

Or keep it as direct instantiation in the endpoint (simpler — the handler takes `AppDbContext` and `ILogger` which are already available).

- [ ] **Step 5: Run tests**

Run: `cd /c/__edMVP/packages/api && dotnet build && dotnet test`

- [ ] **Step 6: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Signup/ packages/api/tests/Tungsten.Api.Tests/Features/Signup/ packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: add Stripe webhook handler for tenant lifecycle management"
```

---

## Chunk 3: Frontend — Signup Pages

### Task 7: Signup Page + Success Page

**Files:**
- Create: `packages/web/src/app/features/signup/signup.component.ts`
- Create: `packages/web/src/app/features/signup/signup-success.component.ts`
- Modify: `packages/web/src/app/app.routes.ts`
- Modify: `packages/web/src/app/features/auth/login.component.ts`
- Modify: `packages/web/src/environments/environment.ts`
- Modify: `packages/web/src/environments/environment.production.ts`

- [ ] **Step 1: Create signup component**

Create `packages/web/src/app/features/signup/signup.component.ts`:
- Standalone component, OnPush
- Form with: company name, your name, email, confirm email
- Client-side validation: all required, emails must match
- "Start 60-day free trial" button
- Subtitle: "$249/month after trial. Cancel anytime."
- On submit: POST to `${apiUrl}/api/signup/checkout`, redirect to `checkoutUrl`
- Error handling for duplicate email
- Styled like the login page (centered card, indigo accents)
- Link back to login: "Already have an account? Sign in"

- [ ] **Step 2: Create success component**

Create `packages/web/src/app/features/signup/signup-success.component.ts`:
- Standalone component, OnPush
- "Your account is being set up!" heading
- "Sign in with Google to get started" button linking to `/login`
- Note: "Account setup may take a few seconds. If you see an error on first sign-in, wait a moment and try again."
- Styled like login page

- [ ] **Step 3: Add routes**

In `packages/web/src/app/app.routes.ts`, add before the catch-all route:

```typescript
{
  path: 'signup',
  loadComponent: () => import('./features/signup/signup.component').then(m => m.SignupComponent),
},
{
  path: 'signup/success',
  loadComponent: () => import('./features/signup/signup-success.component').then(m => m.SignupSuccessComponent),
},
```

- [ ] **Step 4: Add signup link to login page**

In `packages/web/src/app/features/auth/login.component.ts`, add a link below the sign-in button:
```html
<a routerLink="/signup" class="text-sm text-indigo-600 hover:underline">
  Don't have an account? Start a free trial
</a>
```

- [ ] **Step 5: Build**

Run: `cd /c/__edMVP/packages/web && npx ng build`

- [ ] **Step 6: Commit**

```bash
git add packages/web/src/app/features/signup/ packages/web/src/app/app.routes.ts packages/web/src/app/features/auth/login.component.ts
git commit -m "feat: add signup and success pages with Stripe checkout integration"
```

---

### Task 8: Tenant Status Display on Dashboard

**Files:**
- Modify: `packages/web/src/app/features/admin/admin-dashboard.component.ts`

- [ ] **Step 1: Show trial/plan status**

Read `packages/web/src/app/features/admin/admin-dashboard.component.ts`.

Add a status banner at the top of the dashboard for TENANT_ADMIN:
- If tenant status is TRIAL: show "Trial — X days remaining" with a progress bar
- If ACTIVE: show "Pro Plan — Active"
- Use the `auth.profile()` signal to access `tenantName`

This requires the `/api/me` response to include trial info. Check if `GetMe.Response` includes tenant status and trial end date. If not, add `tenantStatus` and `trialEndsAt` to the response.

- [ ] **Step 2: Build**

Run: `cd /c/__edMVP/packages/web && npx ng build`

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/features/admin/ packages/api/src/Tungsten.Api/Features/Auth/
git commit -m "feat: show tenant plan status on admin dashboard"
```

---

### Task 9: Final Build Verification

- [ ] **Step 1: Run full API build and tests**

Run: `cd /c/__edMVP/packages/api && dotnet build && dotnet test`

- [ ] **Step 2: Run full frontend build**

Run: `cd /c/__edMVP/packages/web && npx ng build`

- [ ] **Step 3: Commit any formatting fixes**

```bash
git add -A && git commit -m "chore: formatting fixes from Phase 13 implementation"
```
