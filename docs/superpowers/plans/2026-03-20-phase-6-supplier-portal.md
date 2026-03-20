# Phase 6: Supplier Portal — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the Supplier Portal Angular feature module with dashboard, event submission form, batch detail view, document upload, and shared UI components.

**Architecture:** Smart/Dumb component pattern with Facade per feature. Signal-based state stores. `httpResource()` for reads, `HttpClient` for mutations. Adapter pattern at data boundary. All per Angular 21+ rules in `.rules/ANGULAR_*.md`.

**Tech Stack:** Angular 21, Tailwind CSS 4, Signals, Signal Forms

**Spec:** `docs/superpowers/specs/2026-03-20-tungsten-pilot-mvp-design.md` — Section 9

---

## File Structure

```
src/app/
  shared/
    ui/
      status-badge.component.ts      ← compliance status badge (color-coded)
      data-table.component.ts        ← sortable, paginated table
      page-header.component.ts       ← page title + action buttons
      loading-spinner.component.ts   ← spinner
      empty-state.component.ts       ← "no data" state
      file-upload.component.ts       ← drag-and-drop upload
    utils/
      error.utils.ts                 ← error message extraction
  features/
    supplier/
      data/
        supplier-api.service.ts      ← HTTP calls
        supplier.models.ts           ← domain interfaces
      supplier.store.ts              ← signal state store
      supplier.facade.ts             ← facade for smart components
      supplier-dashboard.component.ts ← smart: batch list dashboard
      submit-event.component.ts      ← smart: event submission form
      batch-detail.component.ts      ← smart: batch detail view
      ui/
        batch-card.component.ts      ← dumb: batch summary card
        event-form.component.ts      ← dumb: event form fields
        event-timeline.component.ts  ← dumb: event list timeline
        document-list.component.ts   ← dumb: document list
      supplier.routes.ts             ← updated routes
```

---

## Task 1: Shared UI Components

**Files:**
- Create: `src/app/shared/ui/status-badge.component.ts`
- Create: `src/app/shared/ui/page-header.component.ts`
- Create: `src/app/shared/ui/loading-spinner.component.ts`
- Create: `src/app/shared/ui/empty-state.component.ts`
- Create: `src/app/shared/utils/error.utils.ts`

- [ ] **Step 1: Create all shared components**

`status-badge.component.ts`:
```typescript
import { Component, input, computed } from '@angular/core';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `
    <span [class]="badgeClass()">{{ status() }}</span>
  `,
})
export class StatusBadgeComponent {
  status = input.required<string>();

  badgeClass = computed(() => {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium';
    switch (this.status()) {
      case 'COMPLIANT':
      case 'PASS':
        return `${base} bg-green-100 text-green-800`;
      case 'FLAGGED':
      case 'FLAG':
        return `${base} bg-amber-100 text-amber-800`;
      case 'NON_COMPLIANT':
      case 'FAIL':
        return `${base} bg-red-100 text-red-800`;
      case 'INSUFFICIENT_DATA':
        return `${base} bg-yellow-100 text-yellow-800`;
      case 'PENDING':
        return `${base} bg-slate-100 text-slate-600`;
      default:
        return `${base} bg-slate-100 text-slate-600`;
    }
  });
}
```

`page-header.component.ts`:
```typescript
import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-page-header',
  standalone: true,
  template: `
    <div class="flex items-center justify-between mb-6">
      <div>
        <h1 class="text-2xl font-bold text-slate-900">{{ title() }}</h1>
        @if (subtitle()) {
          <p class="mt-1 text-sm text-slate-500">{{ subtitle() }}</p>
        }
      </div>
      <div>
        @if (actionLabel()) {
          <button
            (click)="actionClicked.emit()"
            class="bg-blue-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
          >
            {{ actionLabel() }}
          </button>
        }
      </div>
    </div>
  `,
})
export class PageHeaderComponent {
  title = input.required<string>();
  subtitle = input('');
  actionLabel = input('');
  actionClicked = output();
}
```

`loading-spinner.component.ts`:
```typescript
import { Component } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  template: `
    <div class="flex items-center justify-center py-12">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
    </div>
  `,
})
export class LoadingSpinnerComponent {}
```

`empty-state.component.ts`:
```typescript
import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  standalone: true,
  template: `
    <div class="text-center py-12">
      <p class="text-slate-400 text-lg">{{ message() }}</p>
    </div>
  `,
})
export class EmptyStateComponent {
  message = input('No data found');
}
```

`error.utils.ts`:
```typescript
import { HttpErrorResponse } from '@angular/common/http';

export function extractErrorMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    return err.error?.error ?? err.error?.message ?? err.message ?? 'Unknown server error';
  }
  if (err instanceof Error) return err.message;
  return 'An unexpected error occurred';
}
```

- [ ] **Step 2: Build**

```bash
cd /c/__edMVP/packages/web && ng build
```

- [ ] **Step 3: Commit**

---

## Task 2: Supplier Data Layer (API Service + Models)

**Files:**
- Create: `src/app/features/supplier/data/supplier.models.ts`
- Create: `src/app/features/supplier/data/supplier-api.service.ts`

- [ ] **Step 1: Create models**

```typescript
export interface BatchResponse {
  id: string;
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
  status: string;
  complianceStatus: string;
  createdAt: string;
  eventCount: number;
}

export interface CreateBatchRequest {
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
}

export interface CustodyEventResponse {
  id: string;
  batchId: string;
  eventType: string;
  eventDate: string;
  location: string;
  actorName: string;
  isCorrection: boolean;
  sha256Hash: string;
  createdAt: string;
}

export interface CreateEventRequest {
  eventType: string;
  eventDate: string;
  location: string;
  gpsCoordinates?: string;
  actorName: string;
  smelterId?: string;
  description: string;
  metadata: Record<string, unknown>;
}

export interface DocumentResponse {
  id: string;
  fileName: string;
  fileSizeBytes: number;
  contentType: string;
  documentType: string;
  downloadUrl: string;
  createdAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ComplianceSummary {
  batchId: string;
  overallStatus: string;
  checks: { framework: string; status: string; checkedAt: string }[];
}
```

- [ ] **Step 2: Create API service**

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../../../core/http/api-url.token';
import {
  BatchResponse, CreateBatchRequest, CustodyEventResponse,
  CreateEventRequest, DocumentResponse, PagedResponse, ComplianceSummary
} from './supplier.models';

@Injectable({ providedIn: 'root' })
export class SupplierApiService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  // Batches
  listBatches(page = 1, pageSize = 20): Observable<PagedResponse<BatchResponse>> {
    return this.http.get<PagedResponse<BatchResponse>>(
      `${this.apiUrl}/api/batches?page=${page}&pageSize=${pageSize}`);
  }

  getBatch(id: string): Observable<BatchResponse> {
    return this.http.get<BatchResponse>(`${this.apiUrl}/api/batches/${id}`);
  }

  createBatch(req: CreateBatchRequest): Observable<BatchResponse> {
    return this.http.post<BatchResponse>(`${this.apiUrl}/api/batches`, req);
  }

  // Events
  listEvents(batchId: string, page = 1, pageSize = 50): Observable<PagedResponse<CustodyEventResponse>> {
    return this.http.get<PagedResponse<CustodyEventResponse>>(
      `${this.apiUrl}/api/batches/${batchId}/events?page=${page}&pageSize=${pageSize}`);
  }

  createEvent(batchId: string, req: CreateEventRequest): Observable<CustodyEventResponse> {
    return this.http.post<CustodyEventResponse>(
      `${this.apiUrl}/api/batches/${batchId}/events`, req);
  }

  // Documents
  listDocuments(batchId: string): Observable<{ documents: DocumentResponse[]; totalCount: number }> {
    return this.http.get<{ documents: DocumentResponse[]; totalCount: number }>(
      `${this.apiUrl}/api/batches/${batchId}/documents`);
  }

  uploadDocument(eventId: string, file: File, documentType: string): Observable<DocumentResponse> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('documentType', documentType);
    return this.http.post<DocumentResponse>(
      `${this.apiUrl}/api/events/${eventId}/documents`, formData);
  }

  // Compliance
  getBatchCompliance(batchId: string): Observable<ComplianceSummary> {
    return this.http.get<ComplianceSummary>(`${this.apiUrl}/api/batches/${batchId}/compliance`);
  }
}
```

- [ ] **Step 3: Build**
- [ ] **Step 4: Commit**

---

## Task 3: Supplier Store and Facade

**Files:**
- Create: `src/app/features/supplier/supplier.store.ts`
- Create: `src/app/features/supplier/supplier.facade.ts`

- [ ] **Step 1: Create store**

```typescript
import { Injectable, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SupplierApiService } from './data/supplier-api.service';
import { BatchResponse, CustodyEventResponse, DocumentResponse } from './data/supplier.models';
import { extractErrorMessage } from '../../shared/utils/error.utils';

@Injectable({ providedIn: 'root' })
export class SupplierStore {
  private api = inject(SupplierApiService);

  // Batch list
  private _batches = signal<BatchResponse[]>([]);
  private _batchesLoading = signal(false);
  private _batchesError = signal<string | null>(null);
  private _totalBatches = signal(0);

  readonly batches = this._batches.asReadonly();
  readonly batchesLoading = this._batchesLoading.asReadonly();
  readonly batchesError = this._batchesError.asReadonly();
  readonly totalBatches = this._totalBatches.asReadonly();
  readonly hasBatches = computed(() => this._batches().length > 0);

  // Selected batch detail
  private _selectedBatch = signal<BatchResponse | null>(null);
  private _events = signal<CustodyEventResponse[]>([]);
  private _documents = signal<DocumentResponse[]>([]);
  private _detailLoading = signal(false);

  readonly selectedBatch = this._selectedBatch.asReadonly();
  readonly events = this._events.asReadonly();
  readonly documents = this._documents.asReadonly();
  readonly detailLoading = this._detailLoading.asReadonly();

  // Submission state
  private _submitting = signal(false);
  private _submitError = signal<string | null>(null);

  readonly submitting = this._submitting.asReadonly();
  readonly submitError = this._submitError.asReadonly();

  loadBatches(page = 1) {
    this._batchesLoading.set(true);
    this._batchesError.set(null);
    this.api.listBatches(page).subscribe({
      next: (res) => {
        this._batches.set(res.items);
        this._totalBatches.set(res.totalCount);
        this._batchesLoading.set(false);
      },
      error: (err) => {
        this._batchesError.set(extractErrorMessage(err));
        this._batchesLoading.set(false);
      },
    });
  }

  loadBatchDetail(batchId: string) {
    this._detailLoading.set(true);
    this.api.getBatch(batchId).subscribe({
      next: (batch) => {
        this._selectedBatch.set(batch);
        this._detailLoading.set(false);
      },
      error: () => this._detailLoading.set(false),
    });

    this.api.listEvents(batchId).subscribe({
      next: (res) => this._events.set(res.items),
    });

    this.api.listDocuments(batchId).subscribe({
      next: (res) => this._documents.set(res.documents),
    });
  }

  createBatch(req: { batchNumber: string; mineralType: string; originCountry: string; originMine: string; weightKg: number }) {
    this._submitting.set(true);
    this._submitError.set(null);
    return this.api.createBatch(req).subscribe({
      next: () => {
        this._submitting.set(false);
        this.loadBatches();
      },
      error: (err) => {
        this._submitError.set(extractErrorMessage(err));
        this._submitting.set(false);
      },
    });
  }

  createEvent(batchId: string, req: any) {
    this._submitting.set(true);
    this._submitError.set(null);
    return this.api.createEvent(batchId, req).subscribe({
      next: () => {
        this._submitting.set(false);
        this.loadBatchDetail(batchId);
      },
      error: (err) => {
        this._submitError.set(extractErrorMessage(err));
        this._submitting.set(false);
      },
    });
  }

  uploadDocument(eventId: string, batchId: string, file: File, documentType: string) {
    this._submitting.set(true);
    return this.api.uploadDocument(eventId, file, documentType).subscribe({
      next: () => {
        this._submitting.set(false);
        this.loadBatchDetail(batchId);
      },
      error: (err) => {
        this._submitError.set(extractErrorMessage(err));
        this._submitting.set(false);
      },
    });
  }
}
```

- [ ] **Step 2: Create facade**

```typescript
import { Injectable, inject } from '@angular/core';
import { SupplierStore } from './supplier.store';
import { CreateEventRequest } from './data/supplier.models';

@Injectable({ providedIn: 'root' })
export class SupplierFacade {
  private store = inject(SupplierStore);

  // Read-only signals
  readonly batches = this.store.batches;
  readonly batchesLoading = this.store.batchesLoading;
  readonly batchesError = this.store.batchesError;
  readonly totalBatches = this.store.totalBatches;
  readonly hasBatches = this.store.hasBatches;
  readonly selectedBatch = this.store.selectedBatch;
  readonly events = this.store.events;
  readonly documents = this.store.documents;
  readonly detailLoading = this.store.detailLoading;
  readonly submitting = this.store.submitting;
  readonly submitError = this.store.submitError;

  loadBatches(page?: number) { this.store.loadBatches(page); }
  loadBatchDetail(batchId: string) { this.store.loadBatchDetail(batchId); }

  createBatch(req: { batchNumber: string; mineralType: string; originCountry: string; originMine: string; weightKg: number }) {
    this.store.createBatch(req);
  }

  submitEvent(batchId: string, req: CreateEventRequest) {
    this.store.createEvent(batchId, req);
  }

  uploadDocument(eventId: string, batchId: string, file: File, documentType: string) {
    this.store.uploadDocument(eventId, batchId, file, documentType);
  }
}
```

- [ ] **Step 3: Build**
- [ ] **Step 4: Commit**

---

## Task 4: Dumb UI Components

**Files:**
- Create: `src/app/features/supplier/ui/batch-card.component.ts`
- Create: `src/app/features/supplier/ui/event-timeline.component.ts`
- Create: `src/app/features/supplier/ui/document-list.component.ts`

- [ ] **Step 1: Create batch-card**

```typescript
import { Component, input, output } from '@angular/core';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';

@Component({
  selector: 'app-batch-card',
  standalone: true,
  imports: [StatusBadgeComponent],
  template: `
    <div
      (click)="selected.emit()"
      class="bg-white rounded-lg border border-slate-200 p-5 hover:border-blue-300 hover:shadow-sm cursor-pointer transition-all"
    >
      <div class="flex items-start justify-between">
        <div>
          <h3 class="font-semibold text-slate-900">{{ batch().batchNumber }}</h3>
          <p class="text-sm text-slate-500 mt-1">{{ batch().originMine }}, {{ batch().originCountry }}</p>
        </div>
        <app-status-badge [status]="batch().complianceStatus" />
      </div>
      <div class="mt-3 flex items-center gap-4 text-sm text-slate-500">
        <span>{{ batch().weightKg }} kg</span>
        <span>{{ batch().eventCount }} events</span>
        <span>{{ batch().status }}</span>
      </div>
    </div>
  `,
})
export class BatchCardComponent {
  batch = input.required<{
    batchNumber: string; originMine: string; originCountry: string;
    complianceStatus: string; weightKg: number; eventCount: number; status: string;
  }>();
  selected = output();
}
```

- [ ] **Step 2: Create event-timeline**

```typescript
import { Component, input } from '@angular/core';
import { StatusBadgeComponent } from '../../../shared/ui/status-badge.component';

@Component({
  selector: 'app-event-timeline',
  standalone: true,
  imports: [StatusBadgeComponent],
  template: `
    <div class="space-y-4">
      @for (event of events(); track event.id) {
        <div class="flex gap-4">
          <div class="flex flex-col items-center">
            <div class="w-3 h-3 rounded-full" [class]="event.isCorrection ? 'bg-amber-400' : 'bg-blue-500'"></div>
            @if (!$last) {
              <div class="w-0.5 flex-1 bg-slate-200 mt-1"></div>
            }
          </div>
          <div class="flex-1 pb-4">
            <div class="flex items-center gap-2">
              <span class="font-medium text-slate-900 text-sm">{{ event.eventType }}</span>
              @if (event.isCorrection) {
                <span class="text-xs bg-amber-100 text-amber-700 px-1.5 py-0.5 rounded">Correction</span>
              }
            </div>
            <p class="text-sm text-slate-500 mt-0.5">
              {{ event.location }} &middot; {{ event.actorName }}
            </p>
            <p class="text-xs text-slate-400 mt-0.5">{{ event.eventDate | date:'medium' }}</p>
          </div>
        </div>
      } @empty {
        <p class="text-slate-400 text-center py-4">No events yet</p>
      }
    </div>
  `,
})
export class EventTimelineComponent {
  events = input.required<{
    id: string; eventType: string; eventDate: string;
    location: string; actorName: string; isCorrection: boolean;
  }[]>();
}
```

Note: The `date` pipe needs to be imported. Add `import { DatePipe } from '@angular/common';` and `imports: [StatusBadgeComponent, DatePipe]`.

- [ ] **Step 3: Create document-list**

```typescript
import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-document-list',
  standalone: true,
  imports: [DatePipe],
  template: `
    <div class="space-y-2">
      @for (doc of documents(); track doc.id) {
        <div class="flex items-center justify-between py-2 px-3 bg-slate-50 rounded-lg">
          <div>
            <p class="text-sm font-medium text-slate-900">{{ doc.fileName }}</p>
            <p class="text-xs text-slate-500">{{ doc.documentType }} &middot; {{ formatSize(doc.fileSizeBytes) }}</p>
          </div>
          <a
            [href]="doc.downloadUrl"
            target="_blank"
            class="text-sm text-blue-600 hover:text-blue-700"
          >Download</a>
        </div>
      } @empty {
        <p class="text-slate-400 text-sm text-center py-4">No documents uploaded</p>
      }
    </div>
  `,
})
export class DocumentListComponent {
  documents = input.required<{
    id: string; fileName: string; fileSizeBytes: number;
    documentType: string; downloadUrl: string; createdAt: string;
  }[]>();

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
```

- [ ] **Step 4: Build**
- [ ] **Step 5: Commit**

---

## Task 5: Smart Components (Dashboard, Batch Detail, Submit Event)

**Files:**
- Modify: `src/app/features/supplier/supplier-dashboard.component.ts`
- Create: `src/app/features/supplier/batch-detail.component.ts`
- Create: `src/app/features/supplier/submit-event.component.ts`

- [ ] **Step 1: Rewrite supplier-dashboard**

```typescript
import { Component, inject, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { EmptyStateComponent } from '../../shared/ui/empty-state.component';
import { BatchCardComponent } from './ui/batch-card.component';

@Component({
  selector: 'app-supplier-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, LoadingSpinnerComponent, EmptyStateComponent, BatchCardComponent],
  template: `
    <app-page-header
      title="Supplier Dashboard"
      subtitle="Manage your batches and custody events"
      actionLabel="New Batch"
      (actionClicked)="onNewBatch()"
    />

    @if (facade.batchesLoading()) {
      <app-loading-spinner />
    } @else if (!facade.hasBatches()) {
      <app-empty-state message="No batches yet. Create your first batch to get started." />
    } @else {
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        @for (batch of facade.batches(); track batch.id) {
          <app-batch-card
            [batch]="batch"
            (selected)="onBatchSelected(batch.id)"
          />
        }
      </div>
    }
  `,
})
export class SupplierDashboardComponent implements OnInit {
  protected facade = inject(SupplierFacade);
  private router = inject(Router);

  ngOnInit() {
    this.facade.loadBatches();
  }

  onNewBatch() {
    this.router.navigate(['/supplier/submit']);
  }

  onBatchSelected(batchId: string) {
    this.router.navigate(['/supplier/batch', batchId]);
  }
}
```

- [ ] **Step 2: Create batch-detail**

```typescript
import { Component, inject, OnInit, input } from '@angular/core';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';
import { StatusBadgeComponent } from '../../shared/ui/status-badge.component';
import { LoadingSpinnerComponent } from '../../shared/ui/loading-spinner.component';
import { EventTimelineComponent } from './ui/event-timeline.component';
import { DocumentListComponent } from './ui/document-list.component';
import { Router } from '@angular/router';

@Component({
  selector: 'app-batch-detail',
  standalone: true,
  imports: [
    PageHeaderComponent, StatusBadgeComponent, LoadingSpinnerComponent,
    EventTimelineComponent, DocumentListComponent,
  ],
  template: `
    @if (facade.detailLoading()) {
      <app-loading-spinner />
    } @else if (facade.selectedBatch(); as batch) {
      <app-page-header
        [title]="'Batch: ' + batch.batchNumber"
        [subtitle]="batch.originMine + ', ' + batch.originCountry"
        actionLabel="Submit Event"
        (actionClicked)="onSubmitEvent()"
      />

      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Batch Info -->
        <div class="bg-white rounded-lg border border-slate-200 p-5">
          <h3 class="font-semibold text-slate-900 mb-3">Batch Info</h3>
          <dl class="space-y-2 text-sm">
            <div class="flex justify-between">
              <dt class="text-slate-500">Mineral</dt>
              <dd class="text-slate-900">{{ batch.mineralType }}</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-slate-500">Weight</dt>
              <dd class="text-slate-900">{{ batch.weightKg }} kg</dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-slate-500">Status</dt>
              <dd><app-status-badge [status]="batch.status" /></dd>
            </div>
            <div class="flex justify-between">
              <dt class="text-slate-500">Compliance</dt>
              <dd><app-status-badge [status]="batch.complianceStatus" /></dd>
            </div>
          </dl>
        </div>

        <!-- Event Timeline -->
        <div class="bg-white rounded-lg border border-slate-200 p-5 lg:col-span-2">
          <h3 class="font-semibold text-slate-900 mb-3">Custody Events</h3>
          <app-event-timeline [events]="facade.events()" />
        </div>
      </div>

      <!-- Documents -->
      <div class="mt-6 bg-white rounded-lg border border-slate-200 p-5">
        <h3 class="font-semibold text-slate-900 mb-3">Documents</h3>
        <app-document-list [documents]="facade.documents()" />
      </div>
    }
  `,
})
export class BatchDetailComponent implements OnInit {
  id = input.required<string>();
  protected facade = inject(SupplierFacade);
  private router = inject(Router);

  ngOnInit() {
    this.facade.loadBatchDetail(this.id());
  }

  onSubmitEvent() {
    this.router.navigate(['/supplier/submit'], { queryParams: { batchId: this.id() } });
  }
}
```

- [ ] **Step 3: Create submit-event**

```typescript
import { Component, inject, signal } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SupplierFacade } from './supplier.facade';
import { PageHeaderComponent } from '../../shared/ui/page-header.component';

const EVENT_TYPES = [
  { value: 'MINE_EXTRACTION', label: 'Mine Extraction' },
  { value: 'CONCENTRATION', label: 'Concentration' },
  { value: 'TRADING_TRANSFER', label: 'Trading/Transfer' },
  { value: 'LABORATORY_ASSAY', label: 'Laboratory Assay' },
  { value: 'PRIMARY_PROCESSING', label: 'Primary Processing (Smelting)' },
  { value: 'EXPORT_SHIPMENT', label: 'Export/Shipment' },
];

const METADATA_FIELDS: Record<string, { key: string; label: string; type: string }[]> = {
  MINE_EXTRACTION: [
    { key: 'gpsCoordinates', label: 'GPS Coordinates', type: 'text' },
    { key: 'mineOperatorIdentity', label: 'Mine Operator', type: 'text' },
    { key: 'mineralogicalCertificateRef', label: 'Certificate Ref', type: 'text' },
  ],
  CONCENTRATION: [
    { key: 'facilityName', label: 'Facility Name', type: 'text' },
    { key: 'processDescription', label: 'Process Description', type: 'text' },
    { key: 'inputWeightKg', label: 'Input Weight (kg)', type: 'number' },
    { key: 'outputWeightKg', label: 'Output Weight (kg)', type: 'number' },
    { key: 'concentrationRatio', label: 'Concentration Ratio', type: 'number' },
  ],
  TRADING_TRANSFER: [
    { key: 'sellerIdentity', label: 'Seller', type: 'text' },
    { key: 'buyerIdentity', label: 'Buyer', type: 'text' },
    { key: 'transferDate', label: 'Transfer Date', type: 'datetime-local' },
    { key: 'contractReference', label: 'Contract Ref', type: 'text' },
  ],
  LABORATORY_ASSAY: [
    { key: 'laboratoryName', label: 'Laboratory', type: 'text' },
    { key: 'assayMethod', label: 'Assay Method', type: 'text' },
    { key: 'tungstenContentPct', label: 'Tungsten Content (%)', type: 'number' },
    { key: 'assayCertificateRef', label: 'Certificate Ref', type: 'text' },
  ],
  PRIMARY_PROCESSING: [
    { key: 'smelterId', label: 'Smelter ID (RMAP)', type: 'text' },
    { key: 'processType', label: 'Process Type', type: 'text' },
    { key: 'inputWeightKg', label: 'Input Weight (kg)', type: 'number' },
    { key: 'outputWeightKg', label: 'Output Weight (kg)', type: 'number' },
  ],
  EXPORT_SHIPMENT: [
    { key: 'originCountry', label: 'Origin Country (ISO)', type: 'text' },
    { key: 'destinationCountry', label: 'Destination Country (ISO)', type: 'text' },
    { key: 'transportMode', label: 'Transport Mode', type: 'text' },
    { key: 'exportPermitRef', label: 'Export Permit Ref', type: 'text' },
  ],
};

@Component({
  selector: 'app-submit-event',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent],
  template: `
    <app-page-header title="Submit Custody Event" />

    <div class="max-w-2xl">
      <form (ngSubmit)="onSubmit()" class="space-y-6">
        <!-- Batch ID -->
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Batch ID</label>
          <input
            type="text"
            [(ngModel)]="batchId"
            name="batchId"
            required
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            placeholder="Batch UUID"
          />
        </div>

        <!-- Event Type -->
        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Event Type</label>
          <select
            [(ngModel)]="eventType"
            name="eventType"
            (ngModelChange)="onEventTypeChange()"
            required
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Select event type...</option>
            @for (type of eventTypes; track type.value) {
              <option [value]="type.value">{{ type.label }}</option>
            }
          </select>
        </div>

        <!-- Common Fields -->
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">Event Date</label>
            <input type="datetime-local" [(ngModel)]="eventDate" name="eventDate" required
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label class="block text-sm font-medium text-slate-700 mb-1">Location</label>
            <input type="text" [(ngModel)]="location" name="location" required
              class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500" />
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Actor Name</label>
          <input type="text" [(ngModel)]="actorName" name="actorName" required
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500" />
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700 mb-1">Description</label>
          <textarea [(ngModel)]="description" name="description" required rows="3"
            class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500"></textarea>
        </div>

        <!-- Dynamic Metadata Fields -->
        @if (currentMetadataFields().length > 0) {
          <div class="border-t border-slate-200 pt-4">
            <h3 class="text-sm font-semibold text-slate-700 mb-3">Event-Specific Fields</h3>
            <div class="space-y-3">
              @for (field of currentMetadataFields(); track field.key) {
                <div>
                  <label class="block text-sm font-medium text-slate-600 mb-1">{{ field.label }}</label>
                  <input
                    [type]="field.type"
                    [ngModel]="metadata()[field.key]"
                    (ngModelChange)="setMetadata(field.key, $event)"
                    [name]="'meta_' + field.key"
                    required
                    class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              }
            </div>
          </div>
        }

        <!-- Error -->
        @if (facade.submitError()) {
          <div class="bg-red-50 border border-red-200 rounded-lg p-3 text-sm text-red-700">
            {{ facade.submitError() }}
          </div>
        }

        <!-- Submit -->
        <button
          type="submit"
          [disabled]="facade.submitting()"
          class="w-full bg-blue-600 text-white py-2.5 px-4 rounded-lg font-medium hover:bg-blue-700 disabled:opacity-50 transition-colors"
        >
          {{ facade.submitting() ? 'Submitting...' : 'Submit Event' }}
        </button>
      </form>
    </div>
  `,
})
export class SubmitEventComponent {
  protected facade = inject(SupplierFacade);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  eventTypes = EVENT_TYPES;

  batchId = '';
  eventType = '';
  eventDate = '';
  location = '';
  actorName = '';
  description = '';
  metadata = signal<Record<string, unknown>>({});

  currentMetadataFields = signal<{ key: string; label: string; type: string }[]>([]);

  constructor() {
    const qp = this.route.snapshot.queryParams;
    if (qp['batchId']) this.batchId = qp['batchId'];
  }

  onEventTypeChange() {
    this.currentMetadataFields.set(METADATA_FIELDS[this.eventType] ?? []);
    this.metadata.set({});
  }

  setMetadata(key: string, value: unknown) {
    this.metadata.update(m => ({ ...m, [key]: value }));
  }

  onSubmit() {
    const smelterId = this.eventType === 'PRIMARY_PROCESSING'
      ? this.metadata()['smelterId'] as string
      : undefined;

    this.facade.submitEvent(this.batchId, {
      eventType: this.eventType,
      eventDate: new Date(this.eventDate).toISOString(),
      location: this.location,
      actorName: this.actorName,
      smelterId,
      description: this.description,
      metadata: this.metadata(),
    });
  }
}
```

- [ ] **Step 4: Build**
- [ ] **Step 5: Commit**

---

## Task 6: Update Routes

**Files:**
- Modify: `src/app/features/supplier/supplier.routes.ts`

- [ ] **Step 1: Update routes**

```typescript
import { Routes } from '@angular/router';

export const SUPPLIER_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./supplier-dashboard.component').then(m => m.SupplierDashboardComponent),
  },
  {
    path: 'submit',
    loadComponent: () => import('./submit-event.component').then(m => m.SubmitEventComponent),
  },
  {
    path: 'batch/:id',
    loadComponent: () => import('./batch-detail.component').then(m => m.BatchDetailComponent),
  },
];
```

- [ ] **Step 2: Build**

```bash
cd /c/__edMVP/packages/web && ng build
```

- [ ] **Step 3: Commit**

---

## Summary

**Phase 6 delivers:**
1. Shared UI: StatusBadge, PageHeader, LoadingSpinner, EmptyState, error utils
2. Supplier data layer: SupplierApiService, models, SupplierStore, SupplierFacade
3. Dashboard: batch card grid with compliance badges, "New Batch" action
4. Batch Detail: batch info panel, event timeline, document list
5. Submit Event: form with dynamic metadata fields per event type
6. Dumb components: BatchCard, EventTimeline, DocumentList
7. Lazy-loaded routes: /, /submit, /batch/:id

**Next:** Phase 7 — Buyer Portal
