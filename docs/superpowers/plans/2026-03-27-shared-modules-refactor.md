# Shared Modules Refactoring Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extract duplicated batch/event/document logic from supplier and buyer features into shared modules, then give admin and tenant users their own routes that compose these shared components — eliminating cross-feature navigation hacks.

**Architecture:** Move batch models, UI components, API service, and state management into `shared/`. Each role-based feature (supplier, buyer, admin) keeps its own routes, dashboard, and shell — but delegates batch operations to shared components. Admin gets its own `/admin/batches/new` and `/admin/batches/:id` routes instead of linking into `/supplier/*`.

**Tech Stack:** Angular 21, standalone components, signals, facades, `inject()` DI.

---

## Current Problems

1. **Admin links to supplier routes** — onboarding wizard sends admin to `/supplier/new-batch`, requiring role guard hacks
2. **Duplicate models** — `BatchResponse`, `CustodyEventResponse`, `DocumentResponse`, `ComplianceSummary`, `PagedResponse` are identical in `supplier.models.ts` and `buyer.models.ts`
3. **Duplicate UI components** — `event-timeline`, `document-list` exist in both `supplier/ui/` and `shared/ui/`; `activity-feed` is in `supplier/ui/` but imported by buyer
4. **Cross-feature imports** — buyer imports `ActivityFeedComponent` from supplier; both import `BatchActivity` from admin
5. **Role check hacks** — `CreateBatchComponent` sniffs role to decide return route

---

## File Map

### New Shared Files
- `shared/models/batch.models.ts` — consolidated batch/event/doc/compliance types
- `shared/services/batch-api.service.ts` — common batch API calls (CRUD, events, docs, compliance, activity)
- `shared/state/batch.facade.ts` — shared facade for batch operations
- `shared/state/batch.store.ts` — shared signal store for batch state
- `shared/components/create-batch.component.ts` — role-agnostic batch creation form
- `shared/components/batch-detail.component.ts` — role-agnostic batch detail (tabs: overview, events, docs, compliance, activity)
- `shared/components/submit-event.component.ts` — role-agnostic event submission form

### Moved to Shared (from supplier/ui → shared/ui)
- `shared/ui/activity-feed.component.ts` — move from `supplier/ui/`
- `shared/ui/compliance-summary.component.ts` — move from `buyer/ui/`

### Deleted (duplicates)
- `supplier/ui/event-timeline.component.ts` — use `shared/ui/` version
- `supplier/ui/document-list.component.ts` — use `shared/ui/` version

### Modified — Admin Routes
- `admin/admin.routes.ts` — add `/admin/batches/new`, `/admin/batches/:id`, `/admin/submit-event`
- `admin/onboarding-wizard.component.ts` — link to `/admin/batches/new` instead of `/supplier/new-batch`

### Modified — Supplier Feature (slim down)
- `supplier/supplier.routes.ts` — routes use shared components
- `supplier/create-batch.component.ts` — DELETE, replaced by shared version
- `supplier/submit-event.component.ts` — DELETE, replaced by shared version
- `supplier/batch-detail.component.ts` — DELETE, replaced by shared version
- `supplier/supplier.facade.ts` — delegate to shared BatchFacade
- `supplier/supplier.store.ts` — delegate to shared BatchStore
- `supplier/supplier-api.service.ts` — DELETE, replaced by shared BatchApiService

### Modified — Buyer Feature (slim down)
- `buyer/buyer.routes.ts` — routes use shared batch-detail + buyer-specific generate tab
- `buyer/batch-detail.component.ts` — keep only buyer-specific "Generate & Share" tab as wrapper
- `buyer/buyer.facade.ts` — delegate batch ops to shared, keep generate/share
- `buyer/buyer.store.ts` — delegate batch ops to shared, keep generate/share
- `buyer/buyer-api.service.ts` — keep only generate/share endpoints

### Modified — Models (remove duplicates)
- `supplier/data/supplier.models.ts` — re-export from shared
- `buyer/data/buyer.models.ts` — re-export from shared, keep `GeneratedDocumentResponse`, `ShareResponse`
- `admin/data/audit-log.models.ts` — move `BatchActivity` to shared models

---

## Chunk 1: Shared Models & API Service

### Task 1: Create shared batch models

**Files:**
- Create: `packages/web/src/app/shared/models/batch.models.ts`

- [ ] **Step 1: Create the consolidated models file**

```typescript
// Consolidated batch/event/document/compliance types used by all features
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
  eventCount?: number;
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
  metadata?: Record<string, unknown>;
}

export interface DocumentResponse {
  id: string;
  fileName: string;
  documentType: string;
  contentType: string;
  fileSizeBytes: number;
  createdAt: string;
}

export interface ComplianceSummary {
  overallStatus: string;
  checks: ComplianceCheck[];
}

export interface ComplianceCheck {
  framework: string;
  status: string;
  checkedAt: string;
}

export interface PagedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

export interface BatchActivity {
  id: string;
  action: string;
  entityType: string;
  entityId?: string;
  timestamp: string;
  userId: string;
  userDisplayName?: string;
  payload?: unknown;
}
```

- [ ] **Step 2: Update supplier models to re-export**

In `supplier/data/supplier.models.ts`, replace all duplicate interfaces with:
```typescript
export {
  BatchResponse,
  CreateBatchRequest,
  CustodyEventResponse,
  CreateEventRequest,
  DocumentResponse,
  ComplianceSummary,
  ComplianceCheck,
  PagedResponse,
  BatchActivity,
} from '../../../shared/models/batch.models';
```

- [ ] **Step 3: Update buyer models to re-export + keep buyer-specific types**

In `buyer/data/buyer.models.ts`, replace batch duplicates with re-exports, keep `GeneratedDocumentResponse` and `ShareResponse`.

- [ ] **Step 4: Update admin audit-log models**

In `admin/data/audit-log.models.ts`, remove `BatchActivity` interface and import from shared.

- [ ] **Step 5: Verify build**

Run: `cd packages/web && npx ng build`
Expected: PASS — no interface changes, just moved.

- [ ] **Step 6: Commit**

```bash
git add packages/web/src/app/shared/models/ packages/web/src/app/features/
git commit -m "refactor: consolidate batch models into shared/models"
```

---

### Task 2: Create shared batch API service

**Files:**
- Create: `packages/web/src/app/shared/services/batch-api.service.ts`

- [ ] **Step 1: Create the shared API service**

```typescript
import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_URL } from '../../core/http/api-url.token';
import {
  BatchResponse, CreateBatchRequest, CustodyEventResponse, CreateEventRequest,
  DocumentResponse, ComplianceSummary, PagedResponse, BatchActivity,
} from '../models/batch.models';

@Injectable({ providedIn: 'root' })
export class BatchApiService {
  private http = inject(HttpClient);
  private apiUrl = inject(API_URL);

  listBatches(page = 1, pageSize = 20) {
    return this.http.get<PagedResponse<BatchResponse>>(
      `${this.apiUrl}/api/batches?page=${page}&pageSize=${pageSize}`);
  }

  getBatch(id: string) {
    return this.http.get<BatchResponse>(`${this.apiUrl}/api/batches/${id}`);
  }

  createBatch(req: CreateBatchRequest) {
    return this.http.post<BatchResponse>(`${this.apiUrl}/api/batches`, req);
  }

  listEvents(batchId: string) {
    return this.http.get<PagedResponse<CustodyEventResponse>>(
      `${this.apiUrl}/api/batches/${batchId}/events`);
  }

  createEvent(batchId: string, req: CreateEventRequest) {
    return this.http.post<CustodyEventResponse>(
      `${this.apiUrl}/api/batches/${batchId}/events`, req);
  }

  listDocuments(batchId: string) {
    return this.http.get<DocumentResponse[]>(
      `${this.apiUrl}/api/batches/${batchId}/documents`);
  }

  uploadDocument(eventId: string, batchId: string, file: File, documentType: string) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('documentType', documentType);
    formData.append('batchId', batchId);
    return this.http.post<DocumentResponse>(
      `${this.apiUrl}/api/events/${eventId}/documents`, formData);
  }

  getCompliance(batchId: string) {
    return this.http.get<ComplianceSummary>(
      `${this.apiUrl}/api/batches/${batchId}/compliance`);
  }

  getActivity(batchId: string) {
    return this.http.get<BatchActivity[]>(
      `${this.apiUrl}/api/batches/${batchId}/activity`);
  }
}
```

- [ ] **Step 2: Update SupplierApiService to delegate**

Replace batch-related methods in `supplier-api.service.ts` with calls to `BatchApiService`, or simply delete and re-export.

- [ ] **Step 3: Update BuyerApiService to delegate**

Keep only `generatePassport()`, `generateDossier()`, `shareDocument()` in `buyer-api.service.ts`. Batch methods come from `BatchApiService`.

- [ ] **Step 4: Verify build**

Run: `cd packages/web && npx ng build`

- [ ] **Step 5: Commit**

```bash
git commit -m "refactor: create shared BatchApiService, delegate from feature services"
```

---

## Chunk 2: Move UI Components to Shared

### Task 3: Move activity-feed and compliance-summary to shared/ui

**Files:**
- Move: `supplier/ui/activity-feed.component.ts` → `shared/ui/activity-feed.component.ts`
- Move: `buyer/ui/compliance-summary.component.ts` → `shared/ui/compliance-summary.component.ts`
- Delete: `supplier/ui/event-timeline.component.ts` (duplicate of shared)
- Delete: `supplier/ui/document-list.component.ts` (duplicate of shared)

- [ ] **Step 1: Move activity-feed to shared/ui**

Copy `supplier/ui/activity-feed.component.ts` to `shared/ui/activity-feed.component.ts`. Update the import path for `BatchActivity` to use shared models.

- [ ] **Step 2: Move compliance-summary to shared/ui**

Copy `buyer/ui/compliance-summary.component.ts` to `shared/ui/compliance-summary.component.ts`.

- [ ] **Step 3: Delete supplier duplicate UI components**

Delete `supplier/ui/event-timeline.component.ts` and `supplier/ui/document-list.component.ts` — the shared/ui versions are canonical.

- [ ] **Step 4: Update all imports across codebase**

Update imports in:
- `supplier/batch-detail.component.ts` — import from `shared/ui/`
- `buyer/batch-detail.component.ts` — import `ActivityFeedComponent` from `shared/ui/` instead of `supplier/ui/`
- `admin/` components if any reference these

- [ ] **Step 5: Verify build**

Run: `cd packages/web && npx ng build`

- [ ] **Step 6: Commit**

```bash
git commit -m "refactor: move activity-feed and compliance-summary to shared/ui, remove duplicates"
```

---

## Chunk 3: Shared Batch State (Facade + Store)

### Task 4: Create shared BatchStore and BatchFacade

**Files:**
- Create: `packages/web/src/app/shared/state/batch.store.ts`
- Create: `packages/web/src/app/shared/state/batch.facade.ts`

- [ ] **Step 1: Create shared BatchStore**

Extract the common batch state management from `supplier.store.ts` into `shared/state/batch.store.ts`:
- Batch list: `batches`, `batchesLoading`, `batchesError`, `totalBatches`
- Batch detail: `selectedBatch`, `events`, `documents`, `compliance`, `detailLoading`
- Submission: `submitting`, `submitError`
- Methods: `loadBatches()`, `loadBatchDetail()`, `createBatch()`, `createEvent()`, `uploadDocument()`

- [ ] **Step 2: Create shared BatchFacade**

Thin facade exposing store signals as readonly + delegating method calls.

- [ ] **Step 3: Update SupplierFacade to delegate**

`SupplierFacade` becomes a thin wrapper that injects `BatchFacade` and re-exposes its signals. Any supplier-specific logic (if any) stays here.

- [ ] **Step 4: Update BuyerFacade to delegate**

`BuyerFacade` injects `BatchFacade` for batch operations. Keeps its own `generating`, `generatedDoc`, `shareUrl` state for document generation.

- [ ] **Step 5: Verify build**

Run: `cd packages/web && npx ng build`

- [ ] **Step 6: Commit**

```bash
git commit -m "refactor: create shared BatchStore/BatchFacade, delegate from feature facades"
```

---

## Chunk 4: Shared Batch Components

### Task 5: Create shared CreateBatchComponent

**Files:**
- Create: `packages/web/src/app/shared/components/create-batch.component.ts`

- [ ] **Step 1: Create the shared component**

Extract from `supplier/create-batch.component.ts`. Key changes:
- Input: `returnRoute` (string) — where to navigate after create/cancel
- Inject `BatchFacade` instead of `SupplierFacade`
- Remove role-sniffing logic — caller passes `returnRoute`

- [ ] **Step 2: Update supplier route to use shared component**

In `supplier.routes.ts`, the `/new-batch` route loads a thin supplier wrapper:
```typescript
{
  path: 'new-batch',
  loadComponent: () => import('../../../shared/components/create-batch.component')
    .then(m => m.SharedCreateBatchComponent),
  data: { returnRoute: '/supplier' },
}
```
Or create a one-line wrapper that sets `returnRoute="/supplier"`.

- [ ] **Step 3: Delete old supplier CreateBatchComponent**

- [ ] **Step 4: Verify build**

Run: `cd packages/web && npx ng build`

- [ ] **Step 5: Commit**

```bash
git commit -m "refactor: extract CreateBatch to shared component with configurable return route"
```

---

### Task 6: Create shared SubmitEventComponent

**Files:**
- Create: `packages/web/src/app/shared/components/submit-event.component.ts`

- [ ] **Step 1: Extract from supplier/submit-event.component.ts**

Same approach: inject `BatchFacade`, accept `returnRoute` input.

- [ ] **Step 2: Update supplier route**

- [ ] **Step 3: Delete old supplier version**

- [ ] **Step 4: Build + commit**

---

### Task 7: Create shared BatchDetailComponent

**Files:**
- Create: `packages/web/src/app/shared/components/batch-detail.component.ts`

- [ ] **Step 1: Extract common batch detail logic**

The shared version includes tabs: Overview, Events, Documents, Compliance, Activity.
Accept optional input: `extraTabs` or use content projection for role-specific tabs.

- [ ] **Step 2: Update supplier route to use shared version**

- [ ] **Step 3: Update buyer batch detail to wrap shared + add Generate tab**

Buyer's `batch-detail.component.ts` becomes a wrapper that uses the shared component and adds the "Generate & Share" tab.

- [ ] **Step 4: Delete old supplier batch-detail**

- [ ] **Step 5: Build + commit**

---

## Chunk 5: Admin Routes for Batch Operations

### Task 8: Add batch routes to admin

**Files:**
- Modify: `packages/web/src/app/features/admin/admin.routes.ts`
- Modify: `packages/web/src/app/features/admin/onboarding-wizard.component.ts`

- [ ] **Step 1: Add admin batch routes**

```typescript
{
  path: 'batches/new',
  loadComponent: () => import('../../shared/components/create-batch.component')
    .then(m => m.SharedCreateBatchComponent),
  data: { returnRoute: '/admin' },
},
{
  path: 'batches/:id',
  loadComponent: () => import('../../shared/components/batch-detail.component')
    .then(m => m.SharedBatchDetailComponent),
  data: { returnRoute: '/admin' },
},
{
  path: 'submit-event',
  loadComponent: () => import('../../shared/components/submit-event.component')
    .then(m => m.SharedSubmitEventComponent),
  data: { returnRoute: '/admin' },
},
```

- [ ] **Step 2: Update onboarding wizard**

Change `actionRoute: '/supplier/new-batch'` → `actionRoute: '/admin/batches/new'`

- [ ] **Step 3: Update admin dashboard batch links**

Any links to batch details should go to `/admin/batches/:id` instead of `/supplier/batch/:id`.

- [ ] **Step 4: Verify build**

Run: `cd packages/web && npx ng build`

- [ ] **Step 5: Commit**

```bash
git commit -m "feat: add batch routes to admin, remove cross-feature navigation"
```

---

## Chunk 6: Remove Role Guard Hacks & Clean Up

### Task 9: Remove TENANT_ADMIN bypass from role guard

**Files:**
- Modify: `packages/web/src/app/core/auth/role.guard.ts`
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/RoleAuthorizationHandler.cs`

- [ ] **Step 1: Remove TENANT_ADMIN bypass from frontend guard**

Revert to only `PLATFORM_ADMIN` bypass. `TENANT_ADMIN` no longer needs to access `/supplier/*` — they use `/admin/batches/*`.

```typescript
if (role === 'PLATFORM_ADMIN' || allowedRoles.includes(role)) return true;
```

- [ ] **Step 2: Remove TENANT_ADMIN bypass from backend handler**

Revert `RoleAuthorizationHandler` — `TENANT_ADMIN` should only access admin-authorized endpoints. The batch API endpoints need their policies updated instead.

- [ ] **Step 3: Update batch API endpoint policies**

In `BatchEndpoints.cs`, change `RequireSupplier` to a new policy `RequireSupplierOrAdmin` that allows `SUPPLIER`, `TENANT_ADMIN`, and `PLATFORM_ADMIN`:

```csharp
options.AddPolicy(RequireSupplierOrAdmin, policy =>
    policy.Requirements.Add(new RoleRequirement(Roles.Supplier, Roles.TenantAdmin)));
```

- [ ] **Step 4: Update CustodyEvent and Document endpoint policies similarly**

- [ ] **Step 5: Build both projects**

Run: `cd packages/api && dotnet build && cd ../../web && npx ng build`

- [ ] **Step 6: Run existing tests**

Run: `cd packages/api && dotnet test`

- [ ] **Step 7: Commit**

```bash
git commit -m "refactor: proper role policies instead of blanket TENANT_ADMIN bypass"
```

---

## Chunk 7: Final Cleanup & Verification

### Task 10: Delete dead code and verify

- [ ] **Step 1: Remove unused supplier files**

Delete if fully replaced by shared:
- `supplier/create-batch.component.ts`
- `supplier/submit-event.component.ts`
- `supplier/batch-detail.component.ts`
- `supplier/supplier-api.service.ts`
- `supplier/ui/event-timeline.component.ts`
- `supplier/ui/document-list.component.ts`
- `supplier/ui/activity-feed.component.ts`

- [ ] **Step 2: Verify no broken imports**

Run: `cd packages/web && npx ng build`

- [ ] **Step 3: Run full test suite**

Run: `cd packages/api && dotnet build && dotnet test`
Run: `cd packages/web && npx ng build`

- [ ] **Step 4: Final commit**

```bash
git commit -m "refactor: remove dead supplier files replaced by shared modules"
```

---

## Summary

| Before | After |
|--------|-------|
| Admin links to `/supplier/new-batch` | Admin uses `/admin/batches/new` |
| TENANT_ADMIN bypasses all role guards | Proper `SupplierOrAdmin` API policy |
| Batch models duplicated in supplier + buyer | Single source in `shared/models/` |
| 3 copies of event-timeline, document-list | 1 copy in `shared/ui/` |
| activity-feed in supplier, imported by buyer | 1 copy in `shared/ui/` |
| CreateBatch sniffs role for return route | Caller passes `returnRoute` via route data |
| Supplier facade/store/API = monolith | Thin wrapper over shared BatchFacade |
