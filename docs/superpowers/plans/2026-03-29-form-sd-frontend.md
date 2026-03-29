# Form SD Frontend — Buyer Dashboard + Supplier Prompts Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Form SD compliance UI to the buyer portal (filing cycle dashboard, batch applicability status) and supplier portal (data completeness prompts for in-scope batches).

**Architecture:** New lazy-loaded route `/buyer/form-sd` with a filing cycle dashboard. Batch list table gets an applicability status badge. Supplier submit-event form shows prompts for missing Form SD fields on in-scope batches. All data comes from the Form SD backend API (Plan A).

**Tech Stack:** Angular 21, standalone components, signals, `inject()` DI, Tailwind CSS.

**Depends on:** Plan A (backend endpoints) must be deployed.

---

## File Map

### New Frontend Files
- `features/buyer/form-sd-dashboard.component.ts` — filing cycle dashboard with package generation
- `features/buyer/data/form-sd-api.service.ts` — HTTP calls to Form SD endpoints

### Modified Frontend Files
- `features/buyer/buyer.routes.ts` — add `/buyer/form-sd` route
- `features/buyer/batch-detail.component.ts` — add Form SD applicability section
- `features/supplier/submit-event.component.ts` — add data completeness prompts
- `core/layout/sidebar.component.ts` — add Form SD nav item for buyers

---

## Chunk 1: Form SD API Service + Buyer Route

### Task 1: Create Form SD API service

**Files:**
- Create: `packages/web/src/app/features/buyer/data/form-sd-api.service.ts`

- [ ] **Step 1: Create the service**

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/http/api-url.token';

export interface FormSdStatus {
  applicabilityStatus: string;
  ruleSetVersion: string | null;
  reasoning: string | null;
  assessedAt: string | null;
}

export interface FilingCycle {
  id: string;
  reportingYear: number;
  dueDate: string;
  status: string;
  submittedAt: string | null;
  notes: string | null;
}

export interface SupplyChainDescription {
  batchNumber: string;
  narrativeText: string;
  chain: { eventType: string; eventDate: string; location: string; actorName: string; smelterId: string | null }[];
  gaps: { description: string; severity: string }[];
}

export interface RiskAssessment {
  overallRating: string;
  categories: { category: string; rating: string; detail: string }[];
  summaryText: string;
}

export interface FormSdPackageResult {
  id: string;
  downloadUrl: string;
  generatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class FormSdApiService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  getBatchStatus(batchId: string): Observable<FormSdStatus> {
    return this.http.get<FormSdStatus>(`${this.apiUrl}/api/form-sd/batches/${batchId}/status`);
  }

  getSupplyChain(batchId: string): Observable<SupplyChainDescription> {
    return this.http.get<SupplyChainDescription>(`${this.apiUrl}/api/form-sd/batches/${batchId}/supply-chain`);
  }

  getRiskAssessment(batchId: string): Observable<RiskAssessment> {
    return this.http.get<RiskAssessment>(`${this.apiUrl}/api/form-sd/batches/${batchId}/risk-assessment`);
  }

  listFilingCycles(): Observable<{ cycles: FilingCycle[] }> {
    return this.http.get<{ cycles: FilingCycle[] }>(`${this.apiUrl}/api/form-sd/filing-cycles`);
  }

  generatePackage(reportingYear: number): Observable<FormSdPackageResult> {
    return this.http.post<FormSdPackageResult>(`${this.apiUrl}/api/form-sd/generate/${reportingYear}`, {});
  }

  updateCycleStatus(cycleId: string, status: string, notes?: string): Observable<{ id: string; status: string }> {
    return this.http.patch<{ id: string; status: string }>(
      `${this.apiUrl}/api/form-sd/filing-cycles/${cycleId}`, { status, notes });
  }
}
```

- [ ] **Step 2: Commit**

```bash
git commit -m "feat: Form SD API service for buyer portal"
```

---

### Task 2: Create Form SD Dashboard component

**Files:**
- Create: `packages/web/src/app/features/buyer/form-sd-dashboard.component.ts`

- [ ] **Step 1: Create the component**

```typescript
import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormSdApiService, FilingCycle, FormSdPackageResult } from './data/form-sd-api.service';
import { BuyerFacade } from './buyer.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';

@Component({
  selector: 'app-form-sd-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, RouterLink, PageHeaderComponent, StatusBadgeComponent, LoadingSpinnerComponent],
  template: `
    <a routerLink="/buyer" class="inline-flex items-center gap-1.5 text-sm text-slate-500 hover:text-indigo-600 mb-4 group">
      <svg class="w-4 h-4 transition-transform group-hover:-translate-x-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7"/>
      </svg>
      Back to Dashboard
    </a>

    <app-page-header
      title="Form SD Compliance"
      subtitle="Dodd-Frank §1502 — Filing cycle management and support package generation"
    />

    @if (loading()) {
      <app-loading-spinner />
    } @else {
      <!-- Current Filing Cycle -->
      @if (currentCycle(); as cycle) {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-6 mb-6">
          <div class="flex items-center justify-between mb-4">
            <div>
              <h2 class="text-lg font-semibold text-slate-900">Reporting Year {{ cycle.reportingYear }}</h2>
              <p class="text-sm text-slate-500">Due: {{ cycle.dueDate | date:'mediumDate' }}</p>
            </div>
            <app-status-badge [status]="cycle.status" />
          </div>

          <div class="grid grid-cols-3 gap-4 mb-6">
            <div class="bg-amber-50 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-amber-700">{{ inScopeCount() }}</p>
              <p class="text-xs text-amber-600">In Scope</p>
            </div>
            <div class="bg-emerald-50 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-emerald-700">{{ outOfScopeCount() }}</p>
              <p class="text-xs text-emerald-600">Out of Scope</p>
            </div>
            <div class="bg-rose-50 rounded-xl p-4 text-center">
              <p class="text-2xl font-bold text-rose-700">{{ indeterminateCount() }}</p>
              <p class="text-xs text-rose-600">Indeterminate</p>
            </div>
          </div>

          <div class="flex gap-3">
            <button
              (click)="onGeneratePackage(cycle.reportingYear)"
              [disabled]="generating()"
              class="bg-indigo-600 text-white py-2.5 px-6 rounded-xl text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 transition-all"
            >
              {{ generating() ? 'Generating...' : 'Generate Support Package' }}
            </button>

            @if (packageResult(); as pkg) {
              <a [href]="pkg.downloadUrl" target="_blank"
                class="inline-flex items-center gap-2 bg-emerald-50 text-emerald-700 py-2.5 px-6 rounded-xl text-sm font-semibold border border-emerald-200 hover:bg-emerald-100 transition-all">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                Download Package
              </a>
            }
          </div>

          @if (generateError()) {
            <div class="mt-4 bg-rose-50 border border-rose-200 rounded-xl p-4">
              <p class="text-sm text-rose-700">{{ generateError() }}</p>
            </div>
          }
        </div>
      } @else {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm p-8 text-center mb-6">
          <p class="text-slate-500">No filing cycle found for the current year.</p>
        </div>
      }

      <!-- All Filing Cycles -->
      @if (cycles().length > 0) {
        <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
          <div class="px-6 py-4 border-b border-slate-200">
            <h3 class="text-sm font-semibold text-slate-700">Filing Cycle History</h3>
          </div>
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-slate-200 bg-slate-50">
                <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase">Year</th>
                <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase">Due Date</th>
                <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase">Status</th>
                <th class="text-left px-6 py-3 font-semibold text-slate-600 text-xs uppercase">Filed</th>
              </tr>
            </thead>
            <tbody>
              @for (cycle of cycles(); track cycle.id) {
                <tr class="border-b border-slate-100">
                  <td class="px-6 py-3 font-medium">{{ cycle.reportingYear }}</td>
                  <td class="px-6 py-3 text-slate-500">{{ cycle.dueDate | date:'mediumDate' }}</td>
                  <td class="px-6 py-3"><app-status-badge [status]="cycle.status" /></td>
                  <td class="px-6 py-3 text-slate-500">{{ cycle.submittedAt ? (cycle.submittedAt | date:'mediumDate') : '—' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    }
  `,
})
export class FormSdDashboardComponent {
  private formSdApi = inject(FormSdApiService);
  private facade = inject(BuyerFacade);

  protected loading = signal(true);
  protected cycles = signal<FilingCycle[]>([]);
  protected generating = signal(false);
  protected generateError = signal<string | null>(null);
  protected packageResult = signal<FormSdPackageResult | null>(null);

  // TODO: These would come from batch-level Form SD status queries
  protected inScopeCount = signal(0);
  protected outOfScopeCount = signal(0);
  protected indeterminateCount = signal(0);

  protected currentCycle = computed(() => {
    const year = new Date().getFullYear();
    return this.cycles().find(c => c.reportingYear === year) ?? null;
  });

  constructor() {
    this.formSdApi.listFilingCycles().subscribe({
      next: (res) => {
        this.cycles.set(res.cycles);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onGeneratePackage(year: number) {
    this.generating.set(true);
    this.generateError.set(null);
    this.formSdApi.generatePackage(year).subscribe({
      next: (result) => {
        this.packageResult.set(result);
        this.generating.set(false);
      },
      error: (err) => {
        this.generateError.set(err?.error?.error ?? 'Failed to generate package');
        this.generating.set(false);
      },
    });
  }
}
```

- [ ] **Step 2: Add route to buyer routes**

In `packages/web/src/app/features/buyer/buyer.routes.ts`, add:

```typescript
{
  path: 'form-sd',
  loadComponent: () => import('./form-sd-dashboard.component').then(m => m.FormSdDashboardComponent),
},
```

- [ ] **Step 3: Add nav item to sidebar**

In `packages/web/src/app/core/layout/sidebar.component.ts`, add a "Form SD" link in the BUYER nav section:

```typescript
{ label: 'Form SD', route: '/buyer/form-sd', icon: '...' },
```

- [ ] **Step 4: Build and verify**

Run: `cd packages/web && npx ng build`

- [ ] **Step 5: Commit**

```bash
git commit -m "feat: Form SD dashboard — filing cycles, package generation, batch counts"
```

---

## Chunk 2: Batch Applicability Badge + Supplier Prompts

### Task 3: Add Form SD status badge to buyer batch list

**Files:**
- Modify: `packages/web/src/app/features/buyer/ui/batch-table.component.ts`

- [ ] **Step 1: Add applicability column to batch table**

Add a "Form SD" column header and badge cell. The badge shows IN_SCOPE (amber), OUT_OF_SCOPE (green), or INDETERMINATE (red). Data comes from a new `formSdStatus` field on the batch response, or fetched per-batch via `FormSdApiService.getBatchStatus()`.

For simplicity in v1, add a method that fetches status on component init for visible batches and stores in a map signal.

- [ ] **Step 2: Commit**

```bash
git commit -m "feat: Form SD applicability badge on buyer batch table"
```

---

### Task 4: Add supplier data completeness prompts

**Files:**
- Modify: `packages/web/src/app/features/supplier/submit-event.component.ts`

- [ ] **Step 1: Add Form SD field prompts**

When submitting a custody event on a batch that is `IN_SCOPE`:
- Check if `smelterId` is filled (for PRIMARY_PROCESSING)
- Check if `originCountry` metadata is present (for MINE_EXTRACTION)
- Show a non-blocking warning banner if fields are missing:

```html
@if (formSdWarnings().length > 0) {
  <div class="bg-amber-50 border border-amber-200 rounded-xl p-4 mb-4">
    <p class="text-sm font-semibold text-amber-700 mb-1">Form SD: Missing recommended fields</p>
    @for (warning of formSdWarnings(); track warning) {
      <p class="text-xs text-amber-600">{{ warning }}</p>
    }
    <p class="text-xs text-amber-500 mt-2 italic">You can still submit — these gaps will be flagged in the compliance report.</p>
  </div>
}
```

The component checks Form SD status via `FormSdApiService.getBatchStatus(batchId)` when the batch ID is set.

- [ ] **Step 2: Build and verify**

Run: `cd packages/web && npx ng build`

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: supplier data completeness prompts for Form SD in-scope batches"
```

---

## Summary

| Feature | Component | Route |
|---------|-----------|-------|
| Filing Cycle Dashboard | `FormSdDashboardComponent` | `/buyer/form-sd` |
| Package Generation | Button on dashboard → `POST /api/form-sd/generate/{year}` | — |
| Batch Applicability Badge | Badge column in buyer batch table | `/buyer` |
| Supplier Prompts | Warning banner in submit-event form | `/supplier/submit` |

**Navigation:** Buyer sidebar gets a "Form SD" link. Supplier sees prompts only on in-scope batches (non-blocking).
