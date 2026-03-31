# Phase A: Supplier Experience Quick Wins — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Close GAP-5 (supplier onboarding checklist) and GAP-2 (material passport sharing UI) to improve supplier experience and surface the Material Passport as a marketable asset.

**Architecture:** Two independent frontend components plus one new backend endpoint and auth policy fix. GAP-5 is frontend-only (localStorage + signals). GAP-2 adds a share-email endpoint, updates auth on existing passport/share endpoints, and adds a new email template.

**Tech Stack:** Angular 21+ standalone components, signal-first state, .NET 10 MediatR CQRS, Resend email, Result pattern

---

## Chunk 1: GAP-5 — Supplier Onboarding Checklist

### Task 1: Create Supplier Onboarding Component

**Files:**
- Create: `packages/web/src/app/features/supplier/ui/supplier-onboarding.component.ts`

- [ ] **Step 1: Create the onboarding component**

```typescript
import { Component, inject, computed, signal, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BatchFacade } from '../../../shared/state/batch.facade';

@Component({
  selector: 'app-supplier-onboarding',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (!dismissed() && !allComplete()) {
      <div class="mb-6 bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="p-5 sm:p-6">
          <div class="flex items-center justify-between mb-4">
            <div class="flex items-center gap-3">
              <div class="w-9 h-9 rounded-lg bg-indigo-50 flex items-center justify-center">
                <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"/>
                </svg>
              </div>
              <div>
                <h3 class="text-sm font-semibold text-slate-900">Getting Started</h3>
                <p class="text-xs text-slate-500">{{ completedCount() }}/3 steps complete</p>
              </div>
            </div>
            <button (click)="dismiss()" class="text-slate-400 hover:text-slate-600 p-1">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <!-- Progress bar -->
          <div class="w-full h-1.5 bg-slate-100 rounded-full mb-5">
            <div class="h-1.5 bg-indigo-600 rounded-full transition-all duration-500"
              [style.width.%]="(completedCount() / 3) * 100"></div>
          </div>

          <div class="space-y-3">
            <!-- Step 1: Create batch -->
            <a routerLink="/supplier/batches/new"
              class="flex items-center gap-3 p-3 rounded-lg hover:bg-slate-50 transition-colors group">
              <div class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
                [class]="step1Complete() ? 'bg-emerald-100' : 'border-2 border-slate-300'">
                @if (step1Complete()) {
                  <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                  </svg>
                }
              </div>
              <div class="flex-1">
                <p class="text-sm font-medium" [class]="step1Complete() ? 'text-slate-400 line-through' : 'text-slate-900'">Create your first batch</p>
                <p class="text-xs text-slate-400">Register a mineral batch in the supply chain</p>
              </div>
              @if (!step1Complete()) {
                <svg class="w-4 h-4 text-slate-300 group-hover:text-indigo-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                </svg>
              }
            </a>

            <!-- Step 2: Submit event -->
            <a routerLink="/supplier/submit"
              class="flex items-center gap-3 p-3 rounded-lg hover:bg-slate-50 transition-colors group">
              <div class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
                [class]="step2Complete() ? 'bg-emerald-100' : 'border-2 border-slate-300'">
                @if (step2Complete()) {
                  <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                  </svg>
                }
              </div>
              <div class="flex-1">
                <p class="text-sm font-medium" [class]="step2Complete() ? 'text-slate-400 line-through' : 'text-slate-900'">Submit a custody event</p>
                <p class="text-xs text-slate-400">Record an event in the batch lifecycle</p>
              </div>
              @if (!step2Complete()) {
                <svg class="w-4 h-4 text-slate-300 group-hover:text-indigo-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                </svg>
              }
            </a>

            <!-- Step 3: Review compliance -->
            <div (click)="onViewCompliance()"
              class="flex items-center gap-3 p-3 rounded-lg hover:bg-slate-50 transition-colors group cursor-pointer">
              <div class="w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0"
                [class]="step3Complete() ? 'bg-emerald-100' : 'border-2 border-slate-300'">
                @if (step3Complete()) {
                  <svg class="w-3.5 h-3.5 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7"/>
                  </svg>
                }
              </div>
              <div class="flex-1">
                <p class="text-sm font-medium" [class]="step3Complete() ? 'text-slate-400 line-through' : 'text-slate-900'">Review compliance status</p>
                <p class="text-xs text-slate-400">Check your batch compliance results</p>
              </div>
              @if (!step3Complete()) {
                <svg class="w-4 h-4 text-slate-300 group-hover:text-indigo-500 transition-colors" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"/>
                </svg>
              }
            </div>
          </div>
        </div>
      </div>
    }
  `,
})
export class SupplierOnboardingComponent {
  private facade = inject(BatchFacade);
  private router = inject(Router);

  private readonly DISMISSED_KEY = 'auditraks_supplier_onboarding_dismissed';
  private readonly VIEWED_KEY = 'auditraks_supplier_viewed_compliance';

  dismissed = signal(localStorage.getItem(this.DISMISSED_KEY) === 'true');

  step1Complete = computed(() => this.facade.batches().length > 0);
  step2Complete = computed(() => this.facade.batches().some(b => b.eventCount > 0));
  step3Complete = signal(localStorage.getItem(this.VIEWED_KEY) === 'true');

  completedCount = computed(() =>
    [this.step1Complete(), this.step2Complete(), this.step3Complete()]
      .filter(Boolean).length
  );
  allComplete = computed(() => this.completedCount() === 3);

  dismiss() {
    localStorage.setItem(this.DISMISSED_KEY, 'true');
    this.dismissed.set(true);
  }

  onViewCompliance() {
    const batches = this.facade.batches();
    if (batches.length > 0) {
      localStorage.setItem(this.VIEWED_KEY, 'true');
      this.step3Complete.set(true);
      this.router.navigate(['/supplier/batch', batches[0].id]);
    }
  }
}
```

Note: Add missing `Router` import at top: `import { Router } from '@angular/router';` — it's already in the RouterLink import line, just destructure it.

- [ ] **Step 2: Integrate into supplier dashboard**

In `packages/web/src/app/features/supplier/supplier-dashboard.component.ts`:

Add import:
```typescript
import { SupplierOnboardingComponent } from './ui/supplier-onboarding.component';
```

Add to `imports` array:
```typescript
imports: [RouterLink, PageHeaderComponent, LoadingSpinnerComponent, BatchCardComponent, StatusBadgeComponent, SupplierOnboardingComponent],
```

Add to template, right after the `<app-page-header>` closing tag and before the stat cards grid:
```html
    <app-supplier-onboarding />
```

- [ ] **Step 3: Build and verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeds with no errors

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/features/supplier/ui/supplier-onboarding.component.ts packages/web/src/app/features/supplier/supplier-dashboard.component.ts
git commit -m "feat: add supplier onboarding checklist (GAP-5)

Guided 3-step checklist on supplier dashboard: create batch, submit event,
review compliance. Tracks progress via signals and localStorage. Dismissable."
```

---

## Chunk 2: GAP-2 — Material Passport Sharing (Backend)

### Task 2: Add RequireSupplierOrBuyer auth policy

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/AuthorizationPolicies.cs`

- [ ] **Step 1: Add the new policy**

Add constant and policy definition:
```csharp
public const string RequireSupplierOrBuyer = "RequireSupplierOrBuyer";
```

In `AddTungstenPolicies` method, add:
```csharp
options.AddPolicy(RequireSupplierOrBuyer, policy =>
    policy.Requirements.Add(new RoleRequirement(Roles.Supplier, Roles.Buyer)));
```

- [ ] **Step 2: Update DocumentGenerationEndpoints auth**

In `packages/api/src/Tungsten.Api/Features/DocumentGeneration/DocumentGenerationEndpoints.cs`, change the auth policy on passport generation and share endpoints from `RequireBuyer` to `RequireSupplierOrBuyer`:

Line with `.RequireAuthorization(AuthorizationPolicies.RequireBuyer)` for the passport endpoint → change to `.RequireAuthorization(AuthorizationPolicies.RequireSupplierOrBuyer)`

Same for the share endpoint.

Keep dossier and DPP as `RequireBuyer` — those are buyer-specific documents.

- [ ] **Step 3: Build and run tests**

Run: `cd packages/api && dotnet build && dotnet test`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Auth/AuthorizationPolicies.cs packages/api/src/Tungsten.Api/Features/DocumentGeneration/DocumentGenerationEndpoints.cs
git commit -m "fix: allow suppliers to generate and share material passports

Add RequireSupplierOrBuyer policy. Update passport generation and share
endpoints to accept both roles. Dossier and DPP remain buyer-only."
```

---

### Task 3: Add PassportShared email template

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Services/EmailTemplates.cs`

- [ ] **Step 1: Add the template method**

Add to `EmailTemplates.cs` after the last template method:

```csharp
public static (string subject, string htmlBody, string textBody) PassportShared(
    string batchNumber, string senderName, string shareUrl, string? message)
{
    var subject = $"Material Passport shared with you — {batchNumber}";
    var messageBlock = string.IsNullOrWhiteSpace(message)
        ? ""
        : $"""<div style="background: #f8fafc; border-left: 3px solid #4f46e5; padding: 12px 16px; margin: 16px 0; border-radius: 0 8px 8px 0;"><p style="margin: 0; font-size: 14px; color: #334155;">{message}</p></div>""";
    var htmlBody = $"""
        <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
            <h1 style="color: #4f46e5; font-size: 24px; margin-bottom: 8px;">Material Passport</h1>
            <p style="color: #64748b; font-size: 14px; margin-bottom: 24px;">{senderName} has shared a Material Passport with you for batch <strong>{batchNumber}</strong>.</p>
            {messageBlock}
            <a href="{shareUrl}" style="display: inline-block; background: #4f46e5; color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 8px; font-size: 14px; font-weight: 600; margin: 16px 0;">View Material Passport</a>
            <p style="color: #94a3b8; font-size: 12px; margin-top: 24px;">This link expires in 30 days.</p>
            <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;" />
            <p style="color: #94a3b8; font-size: 12px;">&copy; 2026 auditraks. Tungsten supply chain compliance, automated.</p>
        </div>
        """;
    var textBody = $"""
        Material Passport — {batchNumber}

        {senderName} has shared a Material Passport with you for batch {batchNumber}.

        {(string.IsNullOrWhiteSpace(message) ? "" : $"Message: {message}\n")}
        View it here: {shareUrl}

        This link expires in 30 days.
        """;
    return (subject, htmlBody, textBody);
}
```

- [ ] **Step 2: Build**

Run: `cd packages/api && dotnet build`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Services/EmailTemplates.cs
git commit -m "feat: add PassportShared email template for GAP-2"
```

---

### Task 4: Create ShareDocumentEmail endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/ShareDocumentEmail.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/DocumentGenerationEndpoints.cs`

- [ ] **Step 1: Create the handler**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class ShareDocumentEmail
{
    public record Command(Guid DocumentId, string RecipientEmail, string? Message)
        : IRequest<Result<Response>>;

    public record Response(string ShareUrl);

    public class Handler(
        AppDbContext db,
        IMediator mediator,
        ICurrentUserService currentUser,
        IEmailService emailService,
        IConfiguration config,
        ILogger<Handler> logger) : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command request, CancellationToken ct)
        {
            var doc = await db.GeneratedDocuments.AsNoTracking()
                .Include(d => d.Batch)
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct);

            if (doc is null)
                return Result<Response>.Failure("Document not found");

            var tenantId = await currentUser.GetTenantIdAsync(ct);
            if (doc.TenantId != tenantId)
                return Result<Response>.Failure("Document not found");

            // Get or create share token via existing ShareDocument handler
            string shareUrl;
            if (!string.IsNullOrEmpty(doc.ShareToken) && doc.ShareExpiresAt > DateTime.UtcNow)
            {
                var baseUrl = config["App:BaseUrl"] ?? "https://auditraks.com";
                shareUrl = $"{baseUrl}/api/shared/{doc.ShareToken}";
            }
            else
            {
                var shareResult = await mediator.Send(new ShareDocument.Command(request.DocumentId), ct);
                if (!shareResult.IsSuccess)
                    return Result<Response>.Failure(shareResult.Error!);
                shareUrl = shareResult.Value.ShareUrl;
            }

            // Get sender info
            var userId = await currentUser.GetUserIdAsync(ct);
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId, ct);
            var senderName = user?.DisplayName ?? "A supplier";

            var (subject, htmlBody, textBody) = EmailTemplates.PassportShared(
                doc.Batch.BatchNumber, senderName, shareUrl, request.Message);

            await emailService.SendAsync(request.RecipientEmail, subject, htmlBody, textBody, ct);

            logger.LogInformation("Passport shared via email to {Recipient} for batch {BatchId}",
                request.RecipientEmail, doc.BatchId);

            return Result<Response>.Success(new Response(shareUrl));
        }
    }
}
```

- [ ] **Step 2: Register the endpoint**

In `DocumentGenerationEndpoints.cs`, add after the share endpoint registration:

```csharp
group.MapPost("/generated-documents/{id:guid}/share-email", async (
    Guid id,
    ShareDocumentEmail.Command command,
    IMediator mediator) =>
{
    var result = await mediator.Send(command with { DocumentId = id });
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization(AuthorizationPolicies.RequireSupplierOrBuyer);
```

- [ ] **Step 3: Build and run tests**

Run: `cd packages/api && dotnet build && dotnet test`
Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/DocumentGeneration/ShareDocumentEmail.cs packages/api/src/Tungsten.Api/Features/DocumentGeneration/DocumentGenerationEndpoints.cs
git commit -m "feat: add share-via-email endpoint for material passports (GAP-2)"
```

---

## Chunk 3: GAP-2 — Material Passport Sharing (Frontend)

### Task 5: Create Passport Share Card component

**Files:**
- Create: `packages/web/src/app/features/supplier/ui/passport-share-card.component.ts`

- [ ] **Step 1: Create the component**

```typescript
import { Component, input, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { API_URL } from '../../../core/http/api-url.token';

@Component({
  selector: 'app-passport-share-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule],
  template: `
    <div class="bg-gradient-to-br from-indigo-50 to-white rounded-xl border border-indigo-200 shadow-sm overflow-hidden">
      <div class="p-5 sm:p-6">
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 rounded-xl bg-indigo-100 flex items-center justify-center">
            <svg class="w-5 h-5 text-indigo-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z"/>
            </svg>
          </div>
          <div>
            <h3 class="text-sm font-semibold text-slate-900">Material Passport Ready</h3>
            <p class="text-xs text-slate-500">Share with your customers to demonstrate compliance</p>
          </div>
        </div>

        <div class="flex flex-wrap gap-2 mb-4">
          <!-- Generate/Download PDF -->
          <button (click)="generatePassport()"
            [disabled]="generating()"
            class="inline-flex items-center gap-1.5 px-3 py-2 bg-indigo-600 text-white rounded-lg text-xs font-semibold hover:bg-indigo-700 disabled:opacity-50 transition-colors">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
            </svg>
            {{ generating() ? 'Generating...' : 'Download PDF' }}
          </button>

          <!-- Copy Share Link -->
          <button (click)="copyShareLink()"
            [disabled]="sharing()"
            class="inline-flex items-center gap-1.5 px-3 py-2 bg-white border border-slate-200 text-slate-700 rounded-lg text-xs font-semibold hover:bg-slate-50 disabled:opacity-50 transition-colors">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 5H6a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2v-1M8 5a2 2 0 002 2h2a2 2 0 002-2M8 5a2 2 0 012-2h2a2 2 0 012 2m0 0h2a2 2 0 012 2v3m2 4H10m0 0l3-3m-3 3l3 3"/>
            </svg>
            {{ copied() ? 'Copied!' : sharing() ? 'Creating...' : 'Copy Link' }}
          </button>

          <!-- Email to Customer -->
          <button (click)="showEmailForm.set(!showEmailForm())"
            class="inline-flex items-center gap-1.5 px-3 py-2 bg-white border border-slate-200 text-slate-700 rounded-lg text-xs font-semibold hover:bg-slate-50 transition-colors">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
            </svg>
            Email to Customer
          </button>
        </div>

        <!-- Email Form -->
        @if (showEmailForm()) {
          <div class="border-t border-indigo-100 pt-4 space-y-3">
            <div>
              <label class="block text-xs font-medium text-slate-600 mb-1">Recipient Email</label>
              <input type="email" [(ngModel)]="recipientEmail" name="recipientEmail"
                placeholder="customer@example.com"
                class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"/>
            </div>
            <div>
              <label class="block text-xs font-medium text-slate-600 mb-1">Message (optional)</label>
              <textarea [(ngModel)]="emailMessage" name="emailMessage" rows="2"
                placeholder="Add a note for your customer..."
                class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 resize-none"></textarea>
            </div>
            <button (click)="sendEmail()"
              [disabled]="sending() || !recipientEmail"
              class="inline-flex items-center gap-1.5 px-4 py-2 bg-indigo-600 text-white rounded-lg text-xs font-semibold hover:bg-indigo-700 disabled:opacity-50 transition-colors">
              {{ sending() ? 'Sending...' : 'Send Passport' }}
            </button>
            @if (emailSent()) {
              <p class="text-xs text-emerald-600 font-medium">Passport sent successfully!</p>
            }
          </div>
        }

        <!-- Error -->
        @if (error()) {
          <p class="text-xs text-rose-600 mt-2">{{ error() }}</p>
        }
      </div>
    </div>
  `,
})
export class PassportShareCardComponent {
  batchId = input.required<string>();

  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  generating = signal(false);
  sharing = signal(false);
  copied = signal(false);
  showEmailForm = signal(false);
  sending = signal(false);
  emailSent = signal(false);
  error = signal<string | null>(null);

  recipientEmail = '';
  emailMessage = '';

  private passportDocId = signal<string | null>(null);

  generatePassport() {
    this.generating.set(true);
    this.error.set(null);
    this.http.post<{ id: string; downloadUrl: string }>(
      `${this.apiUrl}/api/batches/${this.batchId()}/passport`, {}
    ).subscribe({
      next: (res) => {
        this.passportDocId.set(res.id);
        this.generating.set(false);
        window.open(res.downloadUrl, '_blank');
      },
      error: (err) => {
        this.error.set(err.error?.error ?? 'Failed to generate passport');
        this.generating.set(false);
      },
    });
  }

  copyShareLink() {
    this.sharing.set(true);
    this.error.set(null);
    this.ensurePassportDoc().then(docId => {
      if (!docId) return;
      this.http.post<{ shareUrl: string }>(
        `${this.apiUrl}/api/generated-documents/${docId}/share`, {}
      ).subscribe({
        next: (res) => {
          navigator.clipboard.writeText(res.shareUrl);
          this.copied.set(true);
          this.sharing.set(false);
          setTimeout(() => this.copied.set(false), 2000);
        },
        error: (err) => {
          this.error.set(err.error?.error ?? 'Failed to create share link');
          this.sharing.set(false);
        },
      });
    });
  }

  sendEmail() {
    this.sending.set(true);
    this.error.set(null);
    this.emailSent.set(false);
    this.ensurePassportDoc().then(docId => {
      if (!docId) return;
      this.http.post(
        `${this.apiUrl}/api/generated-documents/${docId}/share-email`,
        { recipientEmail: this.recipientEmail, message: this.emailMessage || null }
      ).subscribe({
        next: () => {
          this.emailSent.set(true);
          this.sending.set(false);
          this.recipientEmail = '';
          this.emailMessage = '';
        },
        error: (err) => {
          this.error.set(err.error?.error ?? 'Failed to send email');
          this.sending.set(false);
        },
      });
    });
  }

  private async ensurePassportDoc(): Promise<string | null> {
    if (this.passportDocId()) return this.passportDocId()!;

    // Check if a passport already exists for this batch
    return new Promise(resolve => {
      this.http.get<{ items: { id: string; documentType: string }[] }>(
        `${this.apiUrl}/api/generated-documents?batchId=${this.batchId()}`
      ).subscribe({
        next: (res) => {
          const passport = res.items.find(d => d.documentType === 'MATERIAL_PASSPORT');
          if (passport) {
            this.passportDocId.set(passport.id);
            resolve(passport.id);
          } else {
            // Generate one first
            this.http.post<{ id: string }>(
              `${this.apiUrl}/api/batches/${this.batchId()}/passport`, {}
            ).subscribe({
              next: (r) => {
                this.passportDocId.set(r.id);
                resolve(r.id);
              },
              error: () => {
                this.error.set('Failed to generate passport');
                this.sharing.set(false);
                this.sending.set(false);
                resolve(null);
              },
            });
          }
        },
        error: () => {
          this.error.set('Failed to check for existing passport');
          this.sharing.set(false);
          this.sending.set(false);
          resolve(null);
        },
      });
    });
  }
}
```

- [ ] **Step 2: Integrate into supplier batch detail**

In `packages/web/src/app/features/supplier/batch-detail.component.ts`:

Add import:
```typescript
import { PassportShareCardComponent } from './ui/passport-share-card.component';
```

Add to `imports` array:
```typescript
PassportShareCardComponent
```

Add to template, inside the overview tab section, after the batch info and before the event timeline — when batch is COMPLIANT:

```html
          @if (facade.selectedBatch()?.complianceStatus === 'COMPLIANT') {
            <app-passport-share-card [batchId]="id()" />
          }
```

- [ ] **Step 3: Build and verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/features/supplier/ui/passport-share-card.component.ts packages/web/src/app/features/supplier/batch-detail.component.ts
git commit -m "feat: add material passport share card to supplier batch detail (GAP-2)

Suppliers can download PDF, copy share link, or email passport to customers
when a batch is COMPLIANT. Uses existing passport generation and share
endpoints with new share-email endpoint."
```

---

### Task 6: Build, push, and verify

- [ ] **Step 1: Full build check**

Run: `cd packages/api && dotnet build && dotnet test`
Run: `cd packages/web && npx ng build`
Expected: Both pass

- [ ] **Step 2: Push all Phase A changes**

```bash
git push origin main
```

- [ ] **Step 3: Verify deployment**

After Render deploys, check:
1. Login as supplier → see onboarding checklist on dashboard
2. Navigate to a COMPLIANT batch → see passport share card
3. Test download, copy link, and email actions
