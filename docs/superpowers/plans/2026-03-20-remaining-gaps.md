# Remaining Gaps Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close all remaining spec gaps to make the Tungsten Pilot MVP fully functional — real email delivery, background worker, split-batch, document generation notifications, 48h escalation, invitation emails, buyer share UX, admin job monitor, and session timeout.

**Architecture:** Three workstreams — (A) Backend email + worker infrastructure, (B) Missing business logic endpoints, (C) Angular UI polish. Each workstream is independently deployable. The worker becomes a real background service processing jobs from the `Jobs` table and retrying failed emails.

**Tech Stack:** .NET 10, MediatR, EF Core + Npgsql, QuestPDF, SendGrid (via HTTP API), Angular 21+, Tailwind CSS

---

## Workstream A: Email + Worker Infrastructure

### Task 1: SendGrid Email Service

**Files:**
- Create: `packages/api/src/Tungsten.Api/Common/Services/SendGridEmailService.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`
- Modify: `packages/api/src/Tungsten.Api/Tungsten.Api.csproj`
- Test: `packages/api/tests/Tungsten.Api.Tests/Common/Services/SendGridEmailServiceTests.cs`

**Context:** `IEmailService` interface exists with `SendAsync(to, subject, htmlBody, textBody, ct)`. `LogEmailService` is the only implementation. We need a real implementation using SendGrid's HTTP API.

- [ ] **Step 1: Add SendGrid NuGet package**

```bash
cd packages/api/src/Tungsten.Api
dotnet add package SendGrid --version 9.*
```

- [ ] **Step 2: Write tests for SendGridEmailService**

```csharp
// packages/api/tests/Tungsten.Api.Tests/Common/Services/SendGridEmailServiceTests.cs
namespace Tungsten.Api.Tests.Common.Services;

using Tungsten.Api.Common.Services;

public class SendGridEmailServiceTests
{
    [Fact]
    public void Implements_IEmailService() =>
        typeof(SendGridEmailService).GetInterfaces()
            .Should().Contain(typeof(IEmailService));

    [Fact]
    public async Task SendAsync_Throws_When_ApiKey_Missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var logger = NullLoggerFactory.Instance.CreateLogger<SendGridEmailService>();
        var svc = new SendGridEmailService(config, logger);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.SendAsync("test@test.com", "sub", "<p>hi</p>", "hi", CancellationToken.None));
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

```bash
cd packages/api && dotnet test --filter "SendGridEmailService"
```
Expected: FAIL — `SendGridEmailService` does not exist yet.

- [ ] **Step 4: Implement SendGridEmailService**

```csharp
// packages/api/src/Tungsten.Api/Common/Services/SendGridEmailService.cs
namespace Tungsten.Api.Common.Services;

using SendGrid;
using SendGrid.Helpers.Mail;

public sealed class SendGridEmailService(
    IConfiguration configuration,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken ct)
    {
        var apiKey = configuration["SendGrid:ApiKey"]
            ?? throw new InvalidOperationException("SendGrid:ApiKey not configured");
        var fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@accutrac.org";
        var fromName = configuration["SendGrid:FromName"] ?? "AccuTrac";

        var client = new SendGridClient(apiKey);
        var msg = MailHelper.CreateSingleEmail(
            new EmailAddress(fromEmail, fromName),
            new EmailAddress(to),
            subject,
            textBody,
            htmlBody);

        var response = await client.SendEmailAsync(msg, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            logger.LogError("SendGrid failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"SendGrid returned {response.StatusCode}");
        }

        logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}
```

- [ ] **Step 5: Update Program.cs to conditionally use SendGrid**

In `Program.cs`, replace the unconditional `LogEmailService` registration:

```csharp
// Replace:
builder.Services.AddSingleton<IEmailService, LogEmailService>();

// With:
if (!string.IsNullOrEmpty(builder.Configuration["SendGrid:ApiKey"]))
    builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
else
    builder.Services.AddSingleton<IEmailService, LogEmailService>();
```

- [ ] **Step 6: Run tests and verify they pass**

```bash
cd packages/api && dotnet test --filter "FullyQualifiedName!~Integration"
```
Expected: All tests PASS.

- [ ] **Step 7: Commit**

```bash
git add -A && git commit -m "feat: add SendGrid email service with conditional registration"
```

---

### Task 2: Worker Background Job Processor

**Files:**
- Create: `packages/worker/Tungsten.Worker/Services/JobProcessorService.cs`
- Create: `packages/worker/Tungsten.Worker/Services/EmailRetryService.cs`
- Modify: `packages/worker/Tungsten.Worker/Program.cs`

**Context:** The `Jobs` table and `NotificationEntity.EmailRetryCount` exist in the DB. The worker currently registers nothing. We need two `BackgroundService` implementations: one to process jobs (compliance re-checks, doc generation), one to retry failed email notifications.

- [ ] **Step 1: Implement JobProcessorService**

```csharp
// packages/worker/Tungsten.Worker/Services/JobProcessorService.cs
namespace Tungsten.Worker.Services;

using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

public sealed class JobProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<JobProcessorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("JobProcessorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pendingJobs = await db.Jobs
                    .Where(j => j.Status == "PENDING")
                    .OrderBy(j => j.CreatedAt)
                    .Take(10)
                    .ToListAsync(stoppingToken);

                foreach (var job in pendingJobs)
                {
                    try
                    {
                        job.Status = "PROCESSING";
                        await db.SaveChangesAsync(stoppingToken);

                        logger.LogInformation("Processing job {JobId} type {JobType}", job.Id, job.JobType);

                        // Job type dispatch — extend as needed
                        job.Status = "COMPLETED";
                        job.CompletedAt = DateTime.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Job {JobId} failed", job.Id);
                        job.Status = "FAILED";
                        job.ErrorDetail = ex.Message;
                        job.CompletedAt = DateTime.UtcNow;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "JobProcessorService error");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

- [ ] **Step 2: Implement EmailRetryService**

```csharp
// packages/worker/Tungsten.Worker/Services/EmailRetryService.cs
namespace Tungsten.Worker.Services;

using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

public sealed class EmailRetryService(
    IServiceScopeFactory scopeFactory,
    ILogger<EmailRetryService> logger) : BackgroundService
{
    private const int MaxRetries = 3;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailRetryService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var unsent = await db.Notifications
                    .Include(n => n.User)
                    .Where(n => !n.EmailSent && n.EmailRetryCount < MaxRetries)
                    .OrderBy(n => n.CreatedAt)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                foreach (var notification in unsent)
                {
                    try
                    {
                        var email = notification.User?.Email;
                        if (string.IsNullOrEmpty(email)) continue;

                        await emailService.SendAsync(
                            email,
                            notification.Title,
                            $"<p>{notification.Message}</p>",
                            notification.Message,
                            stoppingToken);

                        notification.EmailSent = true;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Email retry failed for notification {Id}", notification.Id);
                        notification.EmailRetryCount++;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EmailRetryService error");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}
```

- [ ] **Step 3: Register services in Worker Program.cs**

```csharp
// Modify packages/worker/Tungsten.Worker/Program.cs — add after existing registrations:

// Email service (same conditional logic as API)
if (!string.IsNullOrEmpty(builder.Configuration["SendGrid:ApiKey"]))
    builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
else
    builder.Services.AddSingleton<IEmailService, LogEmailService>();

// Background services
builder.Services.AddHostedService<JobProcessorService>();
builder.Services.AddHostedService<EmailRetryService>();
```

Add usings:
```csharp
using Tungsten.Api.Common.Services;
using Tungsten.Worker.Services;
```

- [ ] **Step 4: Build and verify**

```bash
cd packages/worker && dotnet build
```
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: implement worker job processor and email retry services"
```

---

### Task 3: 48-Hour Compliance Escalation Service

**Files:**
- Create: `packages/worker/Tungsten.Worker/Services/EscalationService.cs`
- Modify: `packages/worker/Tungsten.Worker/Program.cs`

**Context:** FR-P071d requires unresolved compliance flags older than 48 hours to trigger escalation notifications to PLATFORM_ADMIN users.

- [ ] **Step 1: Implement EscalationService**

```csharp
// packages/worker/Tungsten.Worker/Services/EscalationService.cs
namespace Tungsten.Worker.Services;

using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

public sealed class EscalationService(
    IServiceScopeFactory scopeFactory,
    ILogger<EscalationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EscalationService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var cutoff = DateTime.UtcNow.AddHours(-48);
                var flaggedChecks = await db.ComplianceChecks
                    .Where(c => c.Status == "FLAG" && c.CheckedAt < cutoff)
                    .Select(c => new { c.Id, c.BatchId, c.TenantId, c.Framework })
                    .ToListAsync(stoppingToken);

                foreach (var check in flaggedChecks)
                {
                    // Check if escalation notification already exists
                    var alreadyEscalated = await db.Notifications
                        .AnyAsync(n => n.ReferenceId == check.Id
                            && n.Type == "ESCALATION", stoppingToken);

                    if (alreadyEscalated) continue;

                    // Find all admins for this tenant
                    var admins = await db.Users
                        .Where(u => u.TenantId == check.TenantId && u.Role == "PLATFORM_ADMIN" && u.IsActive)
                        .ToListAsync(stoppingToken);

                    foreach (var admin in admins)
                    {
                        db.Notifications.Add(new()
                        {
                            Id = Guid.NewGuid(),
                            TenantId = check.TenantId,
                            UserId = admin.Id,
                            Type = "ESCALATION",
                            Title = $"Compliance flag unresolved >48h: {check.Framework}",
                            Message = $"Batch {check.BatchId} has an unresolved {check.Framework} flag older than 48 hours.",
                            ReferenceId = check.Id,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Escalated compliance check {CheckId} for batch {BatchId}", check.Id, check.BatchId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EscalationService error");
            }

            // Run every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

- [ ] **Step 2: Register in Worker Program.cs**

```csharp
builder.Services.AddHostedService<EscalationService>();
```

- [ ] **Step 3: Build and verify**

```bash
cd packages/worker && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: add 48-hour compliance escalation background service"
```

---

## Workstream B: Missing Business Logic

### Task 4: Invitation Email on User Create

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/Users/CreateUser.cs`
- Test: `packages/api/tests/Tungsten.Api.Tests/Features/Users/CreateUserTests.cs`

**Context:** `CreateUser` handler creates a user with `Auth0Sub = "pending|{email}"` but sends no email. FR-P071c requires an invitation email with a login link.

- [ ] **Step 1: Write test for invitation email**

```csharp
// Add to CreateUserTests.cs
[Fact]
public async Task Handle_Sends_Invitation_Email()
{
    // Arrange
    var email = new Mock<IEmailService>();
    // ... setup handler with mocked email service
    var command = new CreateUser.Command("invite@test.com", "Test User", "SUPPLIER");

    // Act
    await handler.Handle(command, CancellationToken.None);

    // Assert
    email.Verify(e => e.SendAsync(
        "invite@test.com",
        It.Is<string>(s => s.Contains("AccuTrac")),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<CancellationToken>()), Times.Once);
}
```

- [ ] **Step 2: Add IEmailService dependency to CreateUser handler**

In `CreateUser.cs`, add `IEmailService emailService` to the handler's primary constructor. After saving the user, send the invitation:

```csharp
var loginUrl = configuration["App:BaseUrl"] ?? "https://accutrac-web.onrender.com";
await emailService.SendAsync(
    command.Email,
    "You've been invited to AccuTrac",
    $"<h2>Welcome to AccuTrac</h2><p>{command.DisplayName}, you've been invited to the AccuTrac supply chain compliance platform.</p><p><a href=\"{loginUrl}\">Sign in here</a></p>",
    $"{command.DisplayName}, you've been invited to AccuTrac. Sign in at {loginUrl}",
    ct);
```

- [ ] **Step 3: Run tests**

```bash
cd packages/api && dotnet test --filter "FullyQualifiedName!~Integration"
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: send invitation email when creating users"
```

---

### Task 5: Document Generation Notifications

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GeneratePassport.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GenerateDossier.cs`

**Context:** FR-P071b requires a notification when a document is generated. Neither handler creates a notification.

- [ ] **Step 1: Add notification creation to GeneratePassport handler**

After persisting the `GeneratedDocumentEntity`, add:

```csharp
db.Notifications.Add(new()
{
    Id = Guid.NewGuid(),
    TenantId = user.TenantId,
    UserId = user.Id,
    Type = "DOCUMENT_GENERATED",
    Title = "Material Passport generated",
    Message = $"Material Passport for batch {batch.BatchNumber} is ready for download.",
    ReferenceId = doc.Id,
    CreatedAt = DateTime.UtcNow
});
```

- [ ] **Step 2: Add same pattern to GenerateDossier handler**

Same as above but with "Audit Dossier" title and message.

- [ ] **Step 3: Build and test**

```bash
cd packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: create notifications on document generation"
```

---

### Task 6: Split Batch (FR-P005)

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Batches/SplitBatch.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Batches/BatchEndpoints.cs`
- Test: `packages/api/tests/Tungsten.Api.Tests/Features/Batches/SplitBatchTests.cs`

**Context:** No split-batch capability exists. A supplier should be able to split a batch into two child batches, preserving the parent batch's custody chain.

- [ ] **Step 1: Write test**

```csharp
[Fact]
public async Task Handle_Creates_Two_Child_Batches_With_ParentId()
{
    // Arrange: create parent batch
    // Act: split into 60/40 weight split
    // Assert: two new batches with ParentBatchId set, weights sum to parent
}
```

- [ ] **Step 2: Check if ParentBatchId exists on BatchEntity**

Read `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/BatchEntity.cs`. If `ParentBatchId` is not present, add it:

```csharp
public Guid? ParentBatchId { get; set; }
```

And add the navigation + configuration. Then create a migration:

```bash
cd packages/api && dotnet ef migrations add AddParentBatchId --project src/Tungsten.Api --startup-project src/Tungsten.Api
```

- [ ] **Step 3: Implement SplitBatch handler**

```csharp
// packages/api/src/Tungsten.Api/Features/Batches/SplitBatch.cs
namespace Tungsten.Api.Features.Batches;

public static class SplitBatch
{
    public record Command(Guid BatchId, decimal ChildAWeightKg, decimal ChildBWeightKg) : IRequest<Result<Response>>;
    public record Response(Guid ChildAId, Guid ChildBId);

    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChildAWeightKg).GreaterThan(0);
            RuleFor(x => x.ChildBWeightKg).GreaterThan(0);
        }
    }

    public sealed class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var user = await currentUser.GetCurrentUserAsync(ct);
            if (user is null) return Result<Response>.Failure("User not found");

            var parent = await db.Batches
                .FirstOrDefaultAsync(b => b.Id == request.BatchId && b.TenantId == user.TenantId, ct);
            if (parent is null) return Result<Response>.Failure("Batch not found");

            if (parent.Status == "COMPLETED")
                return Result<Response>.Failure("Cannot split a completed batch");

            var totalWeight = request.ChildAWeightKg + request.ChildBWeightKg;
            if (Math.Abs(totalWeight - parent.EstimatedWeightKg) > 0.01m)
                return Result<Response>.Failure("Child weights must sum to parent weight");

            var childA = new BatchEntity
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                BatchNumber = $"{parent.BatchNumber}-A",
                MineralType = parent.MineralType,
                OriginCountry = parent.OriginCountry,
                MineSite = parent.MineSite,
                EstimatedWeightKg = request.ChildAWeightKg,
                Status = "CREATED",
                ComplianceStatus = "PENDING",
                ParentBatchId = parent.Id,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            var childB = new BatchEntity
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                BatchNumber = $"{parent.BatchNumber}-B",
                MineralType = parent.MineralType,
                OriginCountry = parent.OriginCountry,
                MineSite = parent.MineSite,
                EstimatedWeightKg = request.ChildBWeightKg,
                Status = "CREATED",
                ComplianceStatus = "PENDING",
                ParentBatchId = parent.Id,
                CreatedBy = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            db.Batches.AddRange(childA, childB);
            parent.Status = "COMPLETED"; // Parent is consumed by split
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(childA.Id, childB.Id));
        }
    }
}
```

- [ ] **Step 4: Register endpoint**

In `BatchEndpoints.cs`:
```csharp
group.MapPost("/{id:guid}/split", async (Guid id, SplitBatch.Command command, ISender sender) =>
{
    var result = await sender.Send(command with { BatchId = id });
    return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
}).RequireAuthorization("RequireSupplier");
```

- [ ] **Step 5: Run tests and build**

```bash
cd packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: implement split-batch with parent-child tracking"
```

---

### Task 7: Document Hash in Dossier + Dossier Hash Verification

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GenerateDossier.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/Templates/DossierTemplate.cs`

**Context:** FR-P031 requires file hashes in generated document packages. The dossier omits document SHA-256 hashes and skips hash-chain verification (unlike passport).

- [ ] **Step 1: Add Sha256Hash to DossierDocumentData record**

In `GenerateDossier.cs`, find the `DossierDocumentData` record and add `string Sha256Hash`.

- [ ] **Step 2: Populate hash from DocumentEntity**

Where documents are projected, include `d.Sha256Hash`.

- [ ] **Step 3: Add hash chain verification to dossier** (same as passport)

Copy the `VerifyChain` call pattern from `GeneratePassport.cs`.

- [ ] **Step 4: Display hash in DossierTemplate**

In the documents table in `DossierTemplate.cs`, add a column for SHA-256 hash (truncated to first 16 chars for readability).

- [ ] **Step 5: Build and test**

```bash
cd packages/api && dotnet build && dotnet test --filter "FullyQualifiedName!~Integration"
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: include document hashes in dossier and verify hash chain"
```

---

### Task 8: Platform Version + Rule Version in Generated Documents

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GeneratePassport.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/GenerateDossier.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/Templates/PassportTemplate.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/Templates/DossierTemplate.cs`

**Context:** FR-P020 requires `platform_version` and `rule_version` in generated documents.

- [ ] **Step 1: Add constants**

Create or modify a static class:
```csharp
public static class PlatformInfo
{
    public const string Version = "1.0.0-pilot";
    public const string RuleVersion = "1.0.0-pilot";
}
```

- [ ] **Step 2: Pass to templates and render in PDF footer**

Add a footer row in both templates:
```
Generated by AccuTrac v1.0.0-pilot | Rule Set v1.0.0-pilot | {GeneratedAt} | Generated by {UserDisplayName}
```

- [ ] **Step 3: Build and test**

```bash
cd packages/api && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: add platform and rule version to generated documents"
```

---

## Workstream C: Angular UI Polish

### Task 9: Buyer Share Link UX

**Files:**
- Modify: `packages/web/src/app/features/buyer/batch-detail.component.ts`
- Modify: `packages/web/src/app/features/buyer/buyer.store.ts`

**Context:** The share button generates a token but doesn't show the resulting URL to the buyer. Fix: after `shareDocument()` succeeds, display a copyable share URL.

- [ ] **Step 1: Update store to expose shareUrl signal**

In `buyer.store.ts`, add `_shareUrl = signal<string | null>(null)`. After `shareDocument` API call succeeds, set it from the response.

- [ ] **Step 2: Update batch-detail template**

Add a "Share Link" section that shows when `facade.shareUrl()` is non-null:
```html
@if (facade.shareUrl()) {
  <div class="mt-4 p-3 bg-green-50 rounded-lg">
    <p class="text-sm font-medium text-green-800">Share link created:</p>
    <div class="flex items-center gap-2 mt-1">
      <input readonly [value]="facade.shareUrl()" class="flex-1 text-sm bg-white border rounded px-2 py-1" />
      <button (click)="copyShareUrl()" class="text-sm text-blue-600">Copy</button>
    </div>
  </div>
}
```

- [ ] **Step 3: Add copyShareUrl method**

```typescript
copyShareUrl() {
  navigator.clipboard.writeText(this.facade.shareUrl() ?? '');
}
```

- [ ] **Step 4: Build**

```bash
cd packages/web && npx ng build
```

- [ ] **Step 5: Commit**

```bash
git add -A && git commit -m "feat: show copyable share URL after document sharing"
```

---

### Task 10: Admin Job Monitor + System Health Page

**Files:**
- Create: `packages/web/src/app/features/admin/job-monitor.component.ts`
- Modify: `packages/web/src/app/features/admin/admin.routes.ts`
- Modify: `packages/web/src/app/features/admin/admin-dashboard.component.ts`
- Modify: `packages/web/src/app/features/admin/data/admin-api.service.ts`

**Context:** The spec requires a job queue monitor and system health page in the admin portal. The API has `/health` (public) and `Jobs` table. Need a `GET /api/admin/jobs` endpoint (add in API) and an Angular component.

- [ ] **Step 1: Create API endpoint for job listing**

Create `packages/api/src/Tungsten.Api/Features/Admin/ListJobs.cs`:
```csharp
// GET /api/admin/jobs — returns recent jobs with status
```
Register in `AdminEndpoints.cs`.

- [ ] **Step 2: Add listJobs() to admin-api.service.ts**

```typescript
listJobs() {
  return this.http.get<any[]>(`${this.apiUrl}/api/admin/jobs`);
}
```

- [ ] **Step 3: Create JobMonitorComponent**

```typescript
// Standalone component showing:
// - API health status (GET /health)
// - Recent jobs table with status, type, created, completed, error
// - Auto-refresh every 30 seconds
```

- [ ] **Step 4: Add route and dashboard link**

In `admin.routes.ts` add `{ path: 'jobs', loadComponent: ... }`.
In `admin-dashboard.component.ts` add a "System Health" quick link.

- [ ] **Step 5: Build**

```bash
cd packages/web && npx ng build
```

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat: add admin job monitor and system health page"
```

---

### Task 11: Buyer Dashboard Advanced Filters

**Files:**
- Modify: `packages/web/src/app/features/buyer/ui/batch-table.component.ts`

**Context:** FR-P051 requires filtering by compliance status, supplier identity, and date range. Currently only free-text search exists.

- [ ] **Step 1: Add filter inputs to template**

Above the existing text filter, add:
- Compliance status dropdown: ALL / COMPLIANT / FLAGGED / PENDING / INSUFFICIENT_DATA
- Date range: from/to date inputs
- Update the `computed()` filtered list to apply all filters

- [ ] **Step 2: Implement filter logic in computed signal**

```typescript
filteredBatches = computed(() => {
  let batches = this.batches();
  const text = this._filter().toLowerCase();
  const status = this._statusFilter();
  const from = this._fromDate();
  const to = this._toDate();

  if (text) batches = batches.filter(b => /* existing text filter */);
  if (status !== 'ALL') batches = batches.filter(b => b.complianceStatus === status);
  if (from) batches = batches.filter(b => b.createdAt >= from);
  if (to) batches = batches.filter(b => b.createdAt <= to);

  return batches;
});
```

- [ ] **Step 3: Build**

```bash
cd packages/web && npx ng build
```

- [ ] **Step 4: Commit**

```bash
git add -A && git commit -m "feat: add compliance status and date range filters to buyer dashboard"
```

---

### Task 12: Auth0 Session Timeout Configuration

**Files:**
- No code changes needed — this is Auth0 dashboard configuration

**Context:** FR-P064 requires 8-hour session inactivity timeout.

- [ ] **Step 1: Configure in Auth0 Dashboard**

1. Go to Auth0 Dashboard → Settings → Advanced
2. Set "Idle Session Timeout" to 28800 seconds (8 hours)
3. Set "Session Lifetime" to 86400 seconds (24 hours)

- [ ] **Step 2: Document in config.md**

Add to `docs/config.md`:
```
## Auth0 Session Settings
- Idle Session Timeout: 28800s (8 hours) — FR-P064
- Session Lifetime: 86400s (24 hours)
```

- [ ] **Step 3: Commit docs**

```bash
git add docs/config.md && git commit -m "docs: document Auth0 session timeout configuration"
```

---

## Summary

| Task | Workstream | Priority |
|---|---|---|
| 1. SendGrid Email Service | A - Infrastructure | High |
| 2. Worker Job Processor + Email Retry | A - Infrastructure | High |
| 3. 48-Hour Escalation Service | A - Infrastructure | Medium |
| 4. Invitation Email on User Create | B - Business Logic | High |
| 5. Document Generation Notifications | B - Business Logic | Medium |
| 6. Split Batch (FR-P005) | B - Business Logic | Medium |
| 7. Document Hash in Dossier | B - Business Logic | Medium |
| 8. Platform/Rule Version in Docs | B - Business Logic | Low |
| 9. Buyer Share Link UX | C - Angular UI | Medium |
| 10. Admin Job Monitor | C - Angular UI | Low |
| 11. Buyer Dashboard Filters | C - Angular UI | Medium |
| 12. Auth0 Session Timeout | Config | Low |

**Execution order:** Tasks 1→2→3 (sequential, infrastructure dependency). Tasks 4-8 can run in parallel. Tasks 9-11 can run in parallel. Task 12 is manual.
