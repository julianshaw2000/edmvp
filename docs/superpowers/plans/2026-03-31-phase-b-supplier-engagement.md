# Phase B: Supplier Engagement Metrics — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a supplier engagement metrics API endpoint and buyer dashboard panel so buyers can track supplier activity health (active, stale, flagged suppliers).

**Architecture:** New `Buyer/` feature folder in the API with a MediatR query that aggregates supplier activity from existing Users, Batches, and CustodyEvents tables. Frontend adds a new panel component to the buyer dashboard with 4 metric cards and an expandable supplier list.

**Tech Stack:** .NET 10 MediatR CQRS, EF Core aggregation queries, Angular 21+ standalone components, signal-first state

---

## File Structure

### Backend (new)
- `packages/api/src/Tungsten.Api/Features/Buyer/GetSupplierEngagement.cs` — MediatR query + handler
- `packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs` — Endpoint registration

### Frontend (new + modified)
- `packages/web/src/app/features/buyer/ui/supplier-engagement-panel.component.ts` — New panel component
- `packages/web/src/app/features/buyer/data/buyer-api.service.ts` — Add engagement API call
- `packages/web/src/app/features/buyer/buyer.store.ts` — Add engagement state
- `packages/web/src/app/features/buyer/buyer.facade.ts` — Expose engagement signals
- `packages/web/src/app/features/buyer/buyer-dashboard.component.ts` — Integrate panel

### Registration
- `packages/api/src/Tungsten.Api/Program.cs` — Register buyer endpoints

---

## Chunk 1: Backend — Supplier Engagement Endpoint

### Task 1: Create GetSupplierEngagement query handler

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Buyer/GetSupplierEngagement.cs`

- [ ] **Step 1: Create the handler**

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Buyer;

public static class GetSupplierEngagement
{
    public record Query : IRequest<Result<Response>>;

    public record SupplierItem(
        Guid Id,
        string DisplayName,
        DateTime? LastEventDate,
        int BatchCount,
        int FlaggedBatchCount,
        string Status);

    public record Response(
        int TotalSuppliers,
        int ActiveSuppliers,
        int StaleSuppliers,
        int FlaggedSuppliers,
        List<SupplierItem> Suppliers);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

            var suppliers = await db.Users.AsNoTracking()
                .Where(u => u.TenantId == tenantId && u.Role == Roles.Supplier && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.DisplayName,
                    Batches = db.Batches.AsNoTracking()
                        .Where(b => b.CreatedBy == u.Id && b.TenantId == tenantId)
                        .Select(b => new
                        {
                            b.ComplianceStatus,
                            LatestEventDate = b.CustodyEvents
                                .OrderByDescending(e => e.EventDate)
                                .Select(e => (DateTime?)e.EventDate)
                                .FirstOrDefault()
                        }).ToList()
                })
                .ToListAsync(ct);

            var items = suppliers.Select(s =>
            {
                var batchCount = s.Batches.Count;
                var flaggedCount = s.Batches.Count(b => b.ComplianceStatus == "FLAGGED");
                var lastEvent = s.Batches
                    .Where(b => b.LatestEventDate.HasValue)
                    .Select(b => b.LatestEventDate!.Value)
                    .OrderByDescending(d => d)
                    .FirstOrDefault();

                var status = flaggedCount > 0 ? "flagged"
                    : batchCount == 0 ? "new"
                    : lastEvent == default ? "stale"
                    : lastEvent >= ninetyDaysAgo ? "active"
                    : "stale";

                return new SupplierItem(
                    s.Id,
                    s.DisplayName,
                    lastEvent == default ? null : lastEvent,
                    batchCount,
                    flaggedCount,
                    status);
            }).OrderBy(s => s.Status == "flagged" ? 0 : s.Status == "stale" ? 1 : s.Status == "new" ? 2 : 3)
              .ThenBy(s => s.DisplayName)
              .ToList();

            return Result<Response>.Success(new Response(
                TotalSuppliers: items.Count,
                ActiveSuppliers: items.Count(s => s.Status == "active"),
                StaleSuppliers: items.Count(s => s.Status == "stale"),
                FlaggedSuppliers: items.Count(s => s.Status == "flagged"),
                Suppliers: items));
        }
    }
}
```

- [ ] **Step 2: Build to verify compilation**

Run: `cd packages/api && dotnet build`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Buyer/GetSupplierEngagement.cs
git commit -m "feat: add GetSupplierEngagement query handler (GAP-3)

Aggregates supplier activity from Users, Batches, CustodyEvents. Returns
total/active/stale/flagged counts with sorted supplier list. 90-day
threshold for active vs stale classification.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Create BuyerEndpoints and register

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs`
- Modify: `packages/api/src/Tungsten.Api/Program.cs`

- [ ] **Step 1: Create the endpoints file**

```csharp
using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Buyer;

public static class BuyerEndpoints
{
    public static IEndpointRouteBuilder MapBuyerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/buyer")
            .RequireAuthorization(AuthorizationPolicies.RequireBuyer);

        group.MapGet("/supplier-engagement", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSupplierEngagement.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
```

- [ ] **Step 2: Register in Program.cs**

In `packages/api/src/Tungsten.Api/Program.cs`, find the endpoint registration block (around line 276 where `app.MapBatchEndpoints()` is called) and add:

```csharp
app.MapBuyerEndpoints();
```

Add it near the other feature endpoint registrations.

- [ ] **Step 3: Build and run tests**

Run: `cd packages/api && dotnet build && dotnet test`
Expected: Build succeeds, all tests pass

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Buyer/BuyerEndpoints.cs packages/api/src/Tungsten.Api/Program.cs
git commit -m "feat: register buyer engagement endpoint at GET /api/buyer/supplier-engagement

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 2: Frontend — Supplier Engagement Panel

### Task 3: Add engagement API call, store, and facade

**Files:**
- Modify: `packages/web/src/app/features/buyer/data/buyer-api.service.ts`
- Modify: `packages/web/src/app/features/buyer/buyer.store.ts`
- Modify: `packages/web/src/app/features/buyer/buyer.facade.ts`

- [ ] **Step 1: Add interface and API method**

In `packages/web/src/app/features/buyer/data/buyer-api.service.ts`, add the interface (at the top with other interfaces or at the bottom of the file) and the API method:

Add interface:
```typescript
export interface SupplierEngagement {
  totalSuppliers: number;
  activeSuppliers: number;
  staleSuppliers: number;
  flaggedSuppliers: number;
  suppliers: SupplierEngagementItem[];
}

export interface SupplierEngagementItem {
  id: string;
  displayName: string;
  lastEventDate: string | null;
  batchCount: number;
  flaggedBatchCount: number;
  status: 'active' | 'stale' | 'flagged' | 'new';
}
```

Add method to the `BuyerApiService` class:
```typescript
getSupplierEngagement(): Observable<SupplierEngagement> {
  return this.http.get<SupplierEngagement>(`${this.apiUrl}/api/buyer/supplier-engagement`);
}
```

- [ ] **Step 2: Add state to BuyerStore**

In `packages/web/src/app/features/buyer/buyer.store.ts`, add the engagement signals and load method.

Add import for the new interfaces:
```typescript
import { SupplierEngagement } from './data/buyer-api.service';
```

Add signals (with the other private signals):
```typescript
private _engagement = signal<SupplierEngagement | null>(null);
private _engagementLoading = signal(false);
```

Add public readonly signals:
```typescript
readonly engagement = this._engagement.asReadonly();
readonly engagementLoading = this._engagementLoading.asReadonly();
```

Add method:
```typescript
loadEngagement() {
  this._engagementLoading.set(true);
  this.api.getSupplierEngagement().subscribe({
    next: (res) => {
      this._engagement.set(res);
      this._engagementLoading.set(false);
    },
    error: () => {
      this._engagementLoading.set(false);
    },
  });
}
```

- [ ] **Step 3: Expose in BuyerFacade**

In `packages/web/src/app/features/buyer/buyer.facade.ts`, add:

```typescript
readonly engagement = this.store.engagement;
readonly engagementLoading = this.store.engagementLoading;

loadEngagement() { this.store.loadEngagement(); }
```

- [ ] **Step 4: Build to verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeds

- [ ] **Step 5: Commit**

```bash
git add packages/web/src/app/features/buyer/data/buyer-api.service.ts packages/web/src/app/features/buyer/buyer.store.ts packages/web/src/app/features/buyer/buyer.facade.ts
git commit -m "feat: add supplier engagement state management to buyer store

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Create SupplierEngagementPanel component

**Files:**
- Create: `packages/web/src/app/features/buyer/ui/supplier-engagement-panel.component.ts`

- [ ] **Step 1: Create the component**

```typescript
import { Component, input, signal, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { SupplierEngagement, SupplierEngagementItem } from '../data/buyer-api.service';

@Component({
  selector: 'app-supplier-engagement-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    @if (engagement(); as data) {
      <div class="mb-8">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-base font-semibold text-slate-900">Supplier Engagement</h2>
          <button (click)="expanded.set(!expanded())"
            class="text-xs font-medium text-indigo-600 hover:text-indigo-700">
            {{ expanded() ? 'Collapse' : 'View suppliers' }}
          </button>
        </div>

        <!-- Metric Cards -->
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-4">
          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-8 h-8 rounded-lg bg-slate-100 flex items-center justify-center">
                <svg class="w-4 h-4 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/>
                  <circle cx="9" cy="7" r="4"/>
                </svg>
              </div>
              <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Total</span>
            </div>
            <p class="text-3xl font-bold text-slate-900">{{ data.totalSuppliers }}</p>
          </div>

          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-8 h-8 rounded-lg bg-emerald-50 flex items-center justify-center">
                <svg class="w-4 h-4 text-emerald-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/>
                </svg>
              </div>
              <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Active</span>
            </div>
            <p class="text-3xl font-bold text-slate-900">{{ data.activeSuppliers }}</p>
          </div>

          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-8 h-8 rounded-lg bg-amber-50 flex items-center justify-center">
                <svg class="w-4 h-4 text-amber-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
                </svg>
              </div>
              <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Stale</span>
            </div>
            <p class="text-3xl font-bold text-slate-900">{{ data.staleSuppliers }}</p>
          </div>

          <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-5">
            <div class="flex items-center gap-3 mb-3">
              <div class="w-8 h-8 rounded-lg bg-rose-50 flex items-center justify-center">
                <svg class="w-4 h-4 text-rose-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4.5c-.77-.833-2.694-.833-3.464 0L3.34 16.5c-.77.833.192 2.5 1.732 2.5z"/>
                </svg>
              </div>
              <span class="text-xs font-semibold text-slate-500 uppercase tracking-wider">Flagged</span>
            </div>
            <p class="text-3xl font-bold text-slate-900">{{ data.flaggedSuppliers }}</p>
          </div>
        </div>

        <!-- Expandable Supplier List -->
        @if (expanded()) {
          <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
            <table class="w-full text-sm">
              <thead>
                <tr class="bg-slate-50 border-b border-slate-200">
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Supplier</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Last Activity</th>
                  <th class="text-center px-4 py-3 font-semibold text-slate-600">Batches</th>
                  <th class="text-center px-4 py-3 font-semibold text-slate-600">Flagged</th>
                  <th class="text-left px-4 py-3 font-semibold text-slate-600">Status</th>
                </tr>
              </thead>
              <tbody>
                @for (s of data.suppliers; track s.id) {
                  <tr class="border-b border-slate-100 last:border-0"
                    [class]="s.status === 'flagged' ? 'border-l-2 border-l-rose-400' : s.status === 'stale' ? 'border-l-2 border-l-amber-400' : ''">
                    <td class="px-4 py-3 font-medium text-slate-900">{{ s.displayName }}</td>
                    <td class="px-4 py-3 text-slate-500">
                      {{ s.lastEventDate ? (s.lastEventDate | date:'mediumDate') : 'No activity' }}
                    </td>
                    <td class="px-4 py-3 text-center text-slate-700">{{ s.batchCount }}</td>
                    <td class="px-4 py-3 text-center">
                      @if (s.flaggedBatchCount > 0) {
                        <span class="text-rose-600 font-medium">{{ s.flaggedBatchCount }}</span>
                      } @else {
                        <span class="text-slate-400">0</span>
                      }
                    </td>
                    <td class="px-4 py-3">
                      <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                        [class]="s.status === 'active' ? 'bg-emerald-50 text-emerald-700' :
                                  s.status === 'stale' ? 'bg-amber-50 text-amber-700' :
                                  s.status === 'flagged' ? 'bg-rose-50 text-rose-700' :
                                  'bg-slate-100 text-slate-600'">
                        {{ s.status === 'new' ? 'New' : s.status === 'active' ? 'Active' : s.status === 'stale' ? 'Stale' : 'Flagged' }}
                      </span>
                    </td>
                  </tr>
                }
                @if (data.suppliers.length === 0) {
                  <tr>
                    <td colspan="5" class="px-4 py-8 text-center text-slate-400">No suppliers in this tenant</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    }
  `,
})
export class SupplierEngagementPanelComponent {
  engagement = input.required<SupplierEngagement | null>();
  expanded = signal(false);
}
```

- [ ] **Step 2: Build to verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/features/buyer/ui/supplier-engagement-panel.component.ts
git commit -m "feat: create supplier engagement panel component (GAP-3)

4 metric cards (total/active/stale/flagged) with expandable supplier table
showing name, last activity, batch counts, and status badges.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Integrate panel into buyer dashboard

**Files:**
- Modify: `packages/web/src/app/features/buyer/buyer-dashboard.component.ts`

- [ ] **Step 1: Add import and integrate**

In `packages/web/src/app/features/buyer/buyer-dashboard.component.ts`:

Add import:
```typescript
import { SupplierEngagementPanelComponent } from './ui/supplier-engagement-panel.component';
```

Add to `imports` array:
```typescript
SupplierEngagementPanelComponent
```

Add to `ngOnInit()`:
```typescript
this.facade.loadEngagement();
```

Add to template, between the compliance overview stat cards grid and the batch table section (after the closing `</div>` of the stats grid and before the `@if (facade.batchesLoading())` block):

```html
    <!-- Supplier Engagement -->
    <app-supplier-engagement-panel [engagement]="facade.engagement()" />
```

- [ ] **Step 2: Build and verify**

Run: `cd packages/web && npx ng build`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/features/buyer/buyer-dashboard.component.ts
git commit -m "feat: integrate supplier engagement panel into buyer dashboard (GAP-3)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 6: Full build, push, and verify

- [ ] **Step 1: Full build check**

Run: `cd packages/api && dotnet build && dotnet test`
Run: `cd packages/web && npx ng build`
Expected: Both pass

- [ ] **Step 2: Push**

```bash
git push origin main
```
