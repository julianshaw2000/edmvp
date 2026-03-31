# Phase C: Automated Supplier Reminders + Manual Nudge — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add automated inactivity/stale reminders via a daily background worker and a manual "Send Reminder" nudge button on the buyer dashboard, so buyers can proactively engage suppliers.

**Architecture:** New DB columns on BatchEntity (LastReminderSentAt) and UserEntity (LastNudgedAt) with migration. Two new email templates. A daily BackgroundService in the worker project. A new NudgeSupplier endpoint in the buyer API. A nudge button added to the existing supplier engagement panel.

**Tech Stack:** .NET 10 BackgroundService, EF Core migration, MediatR CQRS, Resend email, Angular 21+ signals

---

## File Structure

### Backend — API (new + modified)
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/BatchEntity.cs` — Add LastReminderSentAt column
- `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs` — Add LastNudgedAt column
- `packages/api/src/Tungsten.Api/Common/Services/EmailTemplates.cs` — Add 2 templates
- `packages/api/src/Tungsten.Api/Features/Buyer/NudgeSupplier.cs` — New MediatR handler
- `packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs` — Register nudge endpoint

### Backend — Worker (new)
- `packages/worker/Tungsten.Worker/Services/SupplierReminderService.cs` — Daily background service

### Frontend (modified)
- `packages/web/src/app/features/buyer/ui/supplier-engagement-panel.component.ts` — Add nudge button
- `packages/web/src/app/features/buyer/data/buyer-api.service.ts` — Add nudge API call
- `packages/web/src/app/features/buyer/buyer.store.ts` — Add nudge state
- `packages/web/src/app/features/buyer/buyer.facade.ts` — Expose nudge method

### Migration
- New EF Core migration for LastReminderSentAt + LastNudgedAt columns

---

## Chunk 1: Database Migration + Email Templates

### Task 1: Add columns and create migration

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/BatchEntity.cs`
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs`

- [ ] **Step 1: Add LastReminderSentAt to BatchEntity**

In `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/BatchEntity.cs`, add after the `UpdatedAt` property:

```csharp
public DateTime? LastReminderSentAt { get; set; }
```

- [ ] **Step 2: Add LastNudgedAt to UserEntity**

In `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs`, add after the `StripeSessionId` property:

```csharp
public DateTime? LastNudgedAt { get; set; }
```

- [ ] **Step 3: Create migration**

Run from the API project directory:
```bash
cd packages/api/src/Tungsten.Api && dotnet ef migrations add AddReminderColumns --context AppDbContext
```

- [ ] **Step 4: Build and verify**

Run: `cd packages/api && dotnet build`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/BatchEntity.cs packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs packages/api/src/Tungsten.Api/Migrations/
git commit -m "feat: add LastReminderSentAt and LastNudgedAt columns with migration (GAP-4)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Add email templates

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Services/EmailTemplates.cs`

- [ ] **Step 1: Add BatchInactivityReminder template**

Add to `EmailTemplates.cs` after the last template method:

```csharp
public static (string subject, string htmlBody, string textBody) BatchInactivityReminder(
    string supplierName, string batchNumber, int daysSinceLastEvent)
{
    var subject = $"Your batch {batchNumber} needs attention";
    var htmlBody = $"""
        <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
            <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">Batch Update Needed</h1>
            <p style="color: #64748b; font-size: 14px; margin-bottom: 24px;">Hi {supplierName}, your batch <strong>{batchNumber}</strong> has had no custody events for <strong>{daysSinceLastEvent} days</strong>.</p>
            <p style="color: #334155; font-size: 14px; margin-bottom: 24px;">To maintain compliance and keep your supply chain data current, please log in and submit your next custody event.</p>
            <a href="https://auditraks.com/supplier" style="display: inline-block; background: #4f46e5; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 8px; font-size: 14px; font-weight: 600;">Go to Supplier Portal</a>
            <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;" />
            <p style="color: #94a3b8; font-size: 12px;">&copy; 2026 auditraks. Tungsten supply chain compliance, automated.</p>
        </div>
        """;
    var textBody = $"""
        Batch Update Needed

        Hi {supplierName}, your batch {batchNumber} has had no custody events for {daysSinceLastEvent} days.

        To maintain compliance, please log in and submit your next custody event.

        Go to Supplier Portal: https://auditraks.com/supplier
        """;
    return (subject, htmlBody, textBody);
}
```

- [ ] **Step 2: Add BuyerNudge template**

Add after the BatchInactivityReminder template:

```csharp
public static (string subject, string htmlBody, string textBody) BuyerNudge(
    string supplierName, string buyerCompanyName)
{
    var subject = $"{buyerCompanyName} is requesting an update on your supply chain data";
    var htmlBody = $"""
        <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
            <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">Update Requested</h1>
            <p style="color: #64748b; font-size: 14px; margin-bottom: 24px;">Hi {supplierName}, <strong>{buyerCompanyName}</strong> is requesting an update on your supply chain compliance data.</p>
            <p style="color: #334155; font-size: 14px; margin-bottom: 24px;">Please log in to review your batches, submit any pending custody events, and ensure your compliance status is current.</p>
            <a href="https://auditraks.com/supplier" style="display: inline-block; background: #4f46e5; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 8px; font-size: 14px; font-weight: 600;">Go to Supplier Portal</a>
            <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;" />
            <p style="color: #94a3b8; font-size: 12px;">&copy; 2026 auditraks. Tungsten supply chain compliance, automated.</p>
        </div>
        """;
    var textBody = $"""
        Update Requested

        Hi {supplierName}, {buyerCompanyName} is requesting an update on your supply chain compliance data.

        Please log in to review your batches and submit any pending custody events.

        Go to Supplier Portal: https://auditraks.com/supplier
        """;
    return (subject, htmlBody, textBody);
}
```

- [ ] **Step 3: Build**

Run: `cd packages/api && dotnet build`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Services/EmailTemplates.cs
git commit -m "feat: add BatchInactivityReminder and BuyerNudge email templates (GAP-4)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 2: Backend — NudgeSupplier Endpoint + Worker Service

### Task 3: Create NudgeSupplier handler and register endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Buyer/NudgeSupplier.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs`

- [ ] **Step 1: Create the handler**

Create `packages/api/src/Tungsten.Api/Features/Buyer/NudgeSupplier.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Buyer;

public static class NudgeSupplier
{
    public record Command(Guid SupplierId) : IRequest<Result>;

    public class Handler(
        AppDbContext db,
        ICurrentUserService currentUser,
        IEmailService emailService,
        ILogger<Handler> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);

            var supplier = await db.Users
                .FirstOrDefaultAsync(u => u.Id == request.SupplierId
                    && u.TenantId == tenantId
                    && u.Role == Roles.Supplier
                    && u.IsActive, ct);

            if (supplier is null)
                return Result.Failure("Supplier not found");

            // Rate limit: one nudge per 7 days
            if (supplier.LastNudgedAt.HasValue
                && supplier.LastNudgedAt.Value > DateTime.UtcNow.AddDays(-7))
                return Result.Failure($"Reminder already sent {(DateTime.UtcNow - supplier.LastNudgedAt.Value).Days} days ago. Please wait 7 days between reminders.");

            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            var companyName = tenant?.Name ?? "Your buyer";

            var (subject, htmlBody, textBody) = EmailTemplates.BuyerNudge(
                supplier.DisplayName, companyName);

            await emailService.SendAsync(supplier.Email, subject, htmlBody, textBody, ct);

            // Update nudge timestamp
            supplier.LastNudgedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            // Create in-app notification
            db.Notifications.Add(new Entities.NotificationEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = supplier.Id,
                Type = "BUYER_NUDGE",
                Title = "Update requested",
                Message = $"{companyName} is requesting an update on your supply chain data.",
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync(ct);

            logger.LogInformation("Nudge sent to supplier {SupplierId} by tenant {TenantId}",
                request.SupplierId, tenantId);

            return Result.Success();
        }
    }
}
```

- [ ] **Step 2: Register the endpoint**

In `packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs`, add after the existing supplier-engagement endpoint:

```csharp
group.MapPost("/nudge-supplier", async (NudgeSupplier.Command command, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(command, ct);
    return result.IsSuccess
        ? Results.Ok(new { message = "Reminder sent" })
        : Results.BadRequest(new { error = result.Error });
});
```

- [ ] **Step 3: Build and test**

Run: `cd packages/api && dotnet build && dotnet test`
Expected: Build succeeds, tests pass

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Buyer/NudgeSupplier.cs packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs
git commit -m "feat: add NudgeSupplier endpoint with 7-day rate limiting (GAP-4)

POST /api/buyer/nudge-supplier sends branded email and in-app notification
to supplier. Rate limited to one nudge per supplier per 7 days.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Create SupplierReminderService worker

**Files:**
- Create: `packages/worker/Tungsten.Worker/Services/SupplierReminderService.cs`
- Modify: `packages/worker/Tungsten.Worker/Program.cs`

- [ ] **Step 1: Create the service**

Create `packages/worker/Tungsten.Worker/Services/SupplierReminderService.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Worker.Services;

public class SupplierReminderService(
    IServiceScopeFactory scopeFactory,
    ILogger<SupplierReminderService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                await SendInactivityReminders(db, emailService, stoppingToken);
                await SendStaleWarnings(db, emailService, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "SupplierReminderService failed");
            }

            // Run once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task SendInactivityReminders(AppDbContext db, IEmailService emailService, CancellationToken ct)
    {
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        var staleBatches = await db.Batches
            .Include(b => b.Creator)
            .Include(b => b.CustodyEvents)
            .Where(b => b.ComplianceStatus != "COMPLIANT"
                && b.Creator.Role == "SUPPLIER"
                && b.Creator.IsActive
                && (b.LastReminderSentAt == null || b.LastReminderSentAt < thirtyDaysAgo))
            .ToListAsync(ct);

        foreach (var batch in staleBatches)
        {
            var lastEvent = batch.CustodyEvents
                .OrderByDescending(e => e.EventDate)
                .FirstOrDefault();

            if (lastEvent is null || lastEvent.EventDate < thirtyDaysAgo)
            {
                var daysSince = lastEvent is null
                    ? (DateTime.UtcNow - batch.CreatedAt).Days
                    : (DateTime.UtcNow - lastEvent.EventDate).Days;

                try
                {
                    var (subject, htmlBody, textBody) = EmailTemplates.BatchInactivityReminder(
                        batch.Creator.DisplayName, batch.BatchNumber, daysSince);

                    await emailService.SendAsync(batch.Creator.Email, subject, htmlBody, textBody, ct);

                    batch.LastReminderSentAt = DateTime.UtcNow;

                    db.Notifications.Add(new NotificationEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = batch.TenantId,
                        UserId = batch.CreatedBy,
                        Type = "INACTIVITY_REMINDER",
                        Title = "Batch needs attention",
                        Message = $"Your batch {batch.BatchNumber} has had no events for {daysSince} days.",
                        ReferenceId = batch.Id,
                        CreatedAt = DateTime.UtcNow,
                    });

                    logger.LogInformation("Inactivity reminder sent for batch {BatchId} to {Email}",
                        batch.Id, batch.Creator.Email);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send inactivity reminder for batch {BatchId}", batch.Id);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task SendStaleWarnings(AppDbContext db, IEmailService emailService, CancellationToken ct)
    {
        var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);

        var staleSuppliers = await db.Users.AsNoTracking()
            .Where(u => u.Role == "SUPPLIER" && u.IsActive)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.DisplayName,
                u.TenantId,
                LatestEvent = db.CustodyEvents
                    .Where(e => e.CreatedBy == u.Id)
                    .OrderByDescending(e => e.EventDate)
                    .Select(e => (DateTime?)e.EventDate)
                    .FirstOrDefault(),
                HasBatches = db.Batches.Any(b => b.CreatedBy == u.Id),
                AlreadyNotified = db.Notifications
                    .Any(n => n.UserId == u.Id && n.Type == "STALE_WARNING"
                        && n.CreatedAt > sixtyDaysAgo)
            })
            .Where(u => u.HasBatches
                && (u.LatestEvent == null || u.LatestEvent < sixtyDaysAgo)
                && !u.AlreadyNotified)
            .ToListAsync(ct);

        foreach (var supplier in staleSuppliers)
        {
            try
            {
                var tenantAdmins = await db.Users.AsNoTracking()
                    .Where(u => u.TenantId == supplier.TenantId
                        && (u.Role == "TENANT_ADMIN" || u.Role == "PLATFORM_ADMIN")
                        && u.IsActive)
                    .ToListAsync(ct);

                // Notify tenant admins that supplier is going stale
                foreach (var admin in tenantAdmins)
                {
                    db.Notifications.Add(new NotificationEntity
                    {
                        Id = Guid.NewGuid(),
                        TenantId = supplier.TenantId,
                        UserId = admin.Id,
                        Type = "STALE_WARNING",
                        Title = "Supplier going stale",
                        Message = $"{supplier.DisplayName} has had no activity for 60+ days.",
                        CreatedAt = DateTime.UtcNow,
                    });
                }

                logger.LogInformation("Stale warning created for supplier {SupplierId}", supplier.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create stale warning for supplier {SupplierId}", supplier.Id);
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Register in worker Program.cs**

In `packages/worker/Tungsten.Worker/Program.cs`, add with the other hosted service registrations:

```csharp
builder.Services.AddHostedService<SupplierReminderService>();
```

Add the using if needed:
```csharp
using Tungsten.Worker.Services;
```

- [ ] **Step 3: Build worker**

Run: `cd packages/worker/Tungsten.Worker && dotnet build`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add packages/worker/Tungsten.Worker/Services/SupplierReminderService.cs packages/worker/Tungsten.Worker/Program.cs
git commit -m "feat: add SupplierReminderService daily background worker (GAP-4)

Sends inactivity reminders for batches with no events >30 days and stale
warnings at 60 days. Deduplicates via LastReminderSentAt and notification
checks. Notifies tenant admins of stale suppliers.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 3: Frontend — Nudge Button

### Task 5: Add nudge API call and state management

**Files:**
- Modify: `packages/web/src/app/features/buyer/data/buyer-api.service.ts`
- Modify: `packages/web/src/app/features/buyer/buyer.store.ts`
- Modify: `packages/web/src/app/features/buyer/buyer.facade.ts`

- [ ] **Step 1: Add nudge API method**

In `packages/web/src/app/features/buyer/data/buyer-api.service.ts`, add method to the BuyerApiService class:

```typescript
nudgeSupplier(supplierId: string): Observable<{ message: string }> {
  return this.http.post<{ message: string }>(`${this.apiUrl}/api/buyer/nudge-supplier`, { supplierId });
}
```

- [ ] **Step 2: Add nudge state to store**

In `packages/web/src/app/features/buyer/buyer.store.ts`, add:

Signal:
```typescript
private _nudgingSupplier = signal<string | null>(null);
```

Public:
```typescript
readonly nudgingSupplier = this._nudgingSupplier.asReadonly();
```

Method:
```typescript
nudgeSupplier(supplierId: string) {
  this._nudgingSupplier.set(supplierId);
  this.api.nudgeSupplier(supplierId).subscribe({
    next: () => {
      this._nudgingSupplier.set(null);
      this.loadEngagement(); // Refresh engagement data
    },
    error: () => {
      this._nudgingSupplier.set(null);
    },
  });
}
```

- [ ] **Step 3: Expose in facade**

In `packages/web/src/app/features/buyer/buyer.facade.ts`, add:

```typescript
readonly nudgingSupplier = this.store.nudgingSupplier;

nudgeSupplier(supplierId: string) { this.store.nudgeSupplier(supplierId); }
```

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/features/buyer/data/buyer-api.service.ts packages/web/src/app/features/buyer/buyer.store.ts packages/web/src/app/features/buyer/buyer.facade.ts
git commit -m "feat: add nudge supplier state management to buyer store (GAP-4)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Add nudge button to engagement panel

**Files:**
- Modify: `packages/web/src/app/features/buyer/ui/supplier-engagement-panel.component.ts`
- Modify: `packages/web/src/app/features/buyer/buyer-dashboard.component.ts`

- [ ] **Step 1: Update the engagement panel component**

In `packages/web/src/app/features/buyer/ui/supplier-engagement-panel.component.ts`:

Add new inputs to the component class:
```typescript
nudgingSupplier = input<string | null>(null);
nudgeClicked = output<string>();
```

Add import for `output`:
```typescript
import { Component, input, signal, output, ChangeDetectionStrategy } from '@angular/core';
```

Add a new column header "Action" to the table header row, after the Status column:
```html
<th class="text-center px-4 py-3 font-semibold text-slate-600">Action</th>
```

Add a new table cell in each supplier row, after the status badge cell, for stale/flagged suppliers:
```html
                    <td class="px-4 py-3 text-center">
                      @if (s.status === 'stale' || s.status === 'flagged') {
                        <button (click)="nudgeClicked.emit(s.id); $event.stopPropagation()"
                          [disabled]="nudgingSupplier() === s.id"
                          class="inline-flex items-center gap-1 px-2.5 py-1 bg-indigo-50 text-indigo-700 rounded-lg text-xs font-medium hover:bg-indigo-100 disabled:opacity-50 transition-colors">
                          <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
                          </svg>
                          {{ nudgingSupplier() === s.id ? 'Sending...' : 'Remind' }}
                        </button>
                      }
                    </td>
```

Also update the "No suppliers" empty row colspan from 5 to 6:
```html
<td colspan="6" class="px-4 py-8 text-center text-slate-400">No suppliers in this tenant</td>
```

- [ ] **Step 2: Connect in buyer dashboard**

In `packages/web/src/app/features/buyer/buyer-dashboard.component.ts`, update the engagement panel usage to pass nudge state and handle the event:

```html
    <app-supplier-engagement-panel
      [engagement]="facade.engagement()"
      [nudgingSupplier]="facade.nudgingSupplier()"
      (nudgeClicked)="facade.nudgeSupplier($event)"
    />
```

- [ ] **Step 3: Build and verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/features/buyer/ui/supplier-engagement-panel.component.ts packages/web/src/app/features/buyer/buyer-dashboard.component.ts
git commit -m "feat: add Send Reminder nudge button to supplier engagement panel (GAP-4)

Stale and flagged suppliers show a Remind button. Calls nudge endpoint
with loading state. 7-day rate limit enforced server-side.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Full build, push, and verify

- [ ] **Step 1: Full build check**

Run: `cd packages/api && dotnet build && dotnet test`
Run: `cd packages/worker/Tungsten.Worker && dotnet build`
Run: `cd packages/web && npx ng build`
Expected: All pass

- [ ] **Step 2: Push**

```bash
git push origin main
```
