# Tungsten Pilot MVP — Design Specification

## 1. Overview

A commercial supply chain custody-tracking platform deployed on Render. Tracks tungsten batches from mine to refinery, validates compliance against RMAP and OECD DDG frameworks, and generates Material Passports and Audit Dossiers for buyers.

**Success criteria (from spec):** Demonstrable custody tracking across a real batch lifecycle, at least two live suppliers, at least one buyer generating a Material Passport — all on Render within ten weeks.

**Source spec:** `docs/Tungsten_Pilot_MVP_Spec_Render_v0_1.docx`

### 1.1 Stack Divergence from Source Spec

The source spec mandates Node.js 20 + Express + TypeScript for the API and worker. This design uses **ASP.NET Core 10 (.NET 10)** instead, based on the team's existing `.rules/DOTNET.md` coding standards and expertise. Rationale:

- The `.rules/` directory contains mature, detailed .NET and Angular coding standards — indicating this is the team's primary stack.
- The source spec's OBJ-P05 ("validate the Render stack") is satisfied regardless of backend language — Render supports both Node.js and .NET web services identically.
- The monorepo shared package (Zod schemas, TypeScript domain types) remains in TypeScript and is portable to any backend.
- Migration to Azure (spec Section 11) is equally straightforward with .NET.

### 1.2 Deferred Source Spec Requirements

The following source spec requirements are intentionally deferred from the pilot design to reduce complexity. They are tracked here for the full build:

- **FR-P005 (Split-batch support):** Sub-batch operations with parent-child references. Adds significant data model complexity; not needed for the six core event types in pilot.
- **FR-P006 (GPS coordinate validation):** Validation of GPS coordinates against plausible country ranges. Pilot accepts GPS as free-text; validation deferred to full build.
- **FR-P007 (Mass balance tolerance check):** Flagging when output quantities exceed input by >5%. Requires accumulation logic across events; deferred.
- **FR-P008 (Out-of-order event acceptance with flag):** Accepting out-of-order events with a flag. Pilot accepts events in any order without flagging temporal inconsistencies.
- **API versioning:** Pilot uses `/api/` without version prefix. Version prefix (`/api/v1/`) to be added in full build.

---

## 2. Monorepo Structure

```
packages/
  shared/            TypeScript — Zod schemas, domain types, compliance rule interfaces
  api/               ASP.NET Core 10 Web API — Vertical Slice + MediatR CQRS
  worker/            .NET 10 Background Service — compliance checking, document generation
  web/               Angular 21+ SPA — Tailwind CSS, three lazy-loaded feature modules
```

The shared package is portable to the full Azure SaaS build without modification (spec Section 11 constraint).

**Angular version note:** Source spec references Angular 17+. This design targets Angular 21+ (current as of March 2026) per the `.rules/ANGULAR_*.md` standards, which are written for Angular 21+.

---

## 3. Technology Stack

| Layer | Technology | Render Service Type |
|---|---|---|
| Frontend | Angular 21+, Tailwind CSS, Auth0 Angular SDK | Static Site |
| API | ASP.NET Core 10, MediatR, FluentValidation, EF Core 10 | Web Service |
| Worker | .NET 10 BackgroundService | Background Worker |
| Database | PostgreSQL (Neon) | External (Neon serverless) |
| Auth | Auth0 (JWT bearer) | External |
| PDF Generation | QuestPDF | Bundled in API/Worker |
| File Storage | Cloudflare R2 (S3-compatible) | External |
| Email | SendGrid | External |
| Error Monitoring | Sentry (free tier) | External |
| Observability | OpenTelemetry | Bundled |
| E2E Tests | Playwright | Local/CI |

**File Storage:** Source spec mandates Cloudflare R2 with pre-signed URLs for direct browser download. Documents are uploaded via the API (which writes to R2), and downloads use pre-signed URLs to avoid proxying through the API.

---

## 4. Data Model

### 4.1 Tenancy

Single-tenant by database schema prefix (e.g., `tenant_acme_`). Each pilot customer gets an isolated schema. The public schema holds shared reference data (RMAP smelter list, risk countries, sanctions list).

**Tenant**
- `id` (UUID, PK)
- `name` (string)
- `schema_prefix` (string, unique)
- `status` (enum: ACTIVE, SUSPENDED)
- `created_at` (timestamptz)

### 4.2 Core Entities

**User**
- `id` (UUID, PK)
- `auth0_sub` (string, unique) — Auth0 subject identifier
- `email` (string)
- `display_name` (string)
- `role` (enum: SUPPLIER, BUYER, PLATFORM_ADMIN)
- `tenant_id` (FK → Tenant)
- `is_active` (bool)
- `created_at`, `updated_at` (timestamptz)

**Batch**
- `id` (UUID, PK)
- `tenant_id` (FK)
- `batch_number` (string, unique per tenant)
- `mineral_type` (string — "tungsten" for pilot, extensible)
- `origin_country` (string, ISO 3166-1 alpha-2)
- `origin_mine` (string)
- `weight_kg` (decimal)
- `status` (enum: CREATED, IN_TRANSIT, AT_PROCESSOR, PROCESSING, REFINED, COMPLETED)
- `compliance_status` (enum: PENDING, COMPLIANT, FLAGGED, INSUFFICIENT_DATA)
- `created_by` (FK → User)
- `created_at`, `updated_at` (timestamptz)

**CustodyEvent**
- `id` (UUID, PK)
- `batch_id` (FK)
- `tenant_id` (FK)
- `event_type` (enum — see Section 4.3)
- `idempotency_key` (string, unique per batch) — deterministic key derived from event key fields (FR-P002)
- `event_date` (timestamptz)
- `location` (string)
- `gps_coordinates` (string, nullable) — country-level validated
- `actor_name` (string) — company/individual performing the action
- `smelter_id` (string, nullable) — RMAP smelter identifier, required for smelter events
- `description` (text)
- `metadata` (jsonb) — event-type-specific mandatory fields (see Section 4.3)
- `schema_version` (int, default 1) — for forward compatibility
- `is_correction` (bool, default false)
- `corrects_event_id` (FK → CustodyEvent, nullable) — links correction to original event
- `sha256_hash` (string) — SHA-256 of canonical event payload, computed at write time
- `previous_event_hash` (string, nullable) — hash chain for tamper evidence
- `created_by` (FK → User)
- `created_at` (timestamptz) — immutable, no updated_at

**Document**
- `id` (UUID, PK)
- `tenant_id` (FK)
- `custody_event_id` (FK, nullable)
- `batch_id` (FK)
- `file_name` (string)
- `storage_key` (string) — Cloudflare R2 object key
- `file_size_bytes` (bigint)
- `content_type` (string) — validated MIME type
- `sha256_hash` (string) — SHA-256 of file content, computed server-side at upload (FR-P031)
- `document_type` (enum: CERTIFICATE_OF_ORIGIN, ASSAY_REPORT, TRANSPORT_DOCUMENT, SMELTER_CERTIFICATE, MINERALOGICAL_CERTIFICATE, EXPORT_PERMIT, OTHER)
- `uploaded_by` (FK → User)
- `created_at` (timestamptz)

**ComplianceCheck**
- `id` (UUID, PK)
- `custody_event_id` (FK)
- `batch_id` (FK)
- `tenant_id` (FK)
- `framework` (enum: RMAP, OECD_DDG)
- `status` (enum: PASS, FAIL, FLAG, INSUFFICIENT_DATA, PENDING)
- `details` (jsonb) — rule-by-rule results
- `checked_at` (timestamptz)

**GeneratedDocument** (covers Material Passports and Audit Dossiers)
- `id` (UUID, PK)
- `batch_id` (FK)
- `tenant_id` (FK)
- `document_type` (enum: MATERIAL_PASSPORT, AUDIT_DOSSIER)
- `storage_key` (string) — R2 object key
- `generated_by` (FK → User)
- `share_token` (string, nullable, unique) — for time-limited external sharing
- `share_expires_at` (timestamptz, nullable)
- `generated_at` (timestamptz)

**Notification**
- `id` (UUID, PK)
- `tenant_id` (FK)
- `user_id` (FK → User)
- `type` (enum: COMPLIANCE_FLAG, DOCUMENT_AVAILABLE, PASSPORT_GENERATED, USER_INVITED, COMPLIANCE_ESCALATION)
- `title` (string)
- `message` (text)
- `reference_id` (UUID, nullable) — links to batch/event/document
- `is_read` (bool, default false)
- `email_sent` (bool, default false)
- `email_retry_count` (int, default 0)
- `created_at` (timestamptz)

**Job** (background task tracking)
- `id` (UUID, PK)
- `tenant_id` (FK)
- `job_type` (enum: COMPLIANCE_CHECK, PASSPORT_GENERATION, DOSSIER_GENERATION, EMAIL_SEND)
- `status` (enum: QUEUED, RUNNING, COMPLETED, FAILED)
- `reference_id` (UUID) — entity being processed
- `error_detail` (text, nullable)
- `created_at`, `completed_at` (timestamptz)

### 4.3 Custody Event Types (Pilot)

Six event types per source spec Table 4. Each has mandatory metadata fields validated via Zod schemas:

1. **MINE_EXTRACTION** — Ore extracted at mine site.
   - Mandatory metadata: `gps_coordinates`, `mine_operator_identity`, `mineralogical_certificate_ref`
   - Required docs: Certificate of Origin, Mineralogical Certificate

2. **CONCENTRATION** — Concentration/beneficiation processing.
   - Mandatory metadata: `facility_name`, `process_description`, `input_weight_kg`, `output_weight_kg`, `concentration_ratio`
   - Required docs: Assay Report

3. **TRADING_TRANSFER** — Trading/ownership transfer.
   - Mandatory metadata: `seller_identity`, `buyer_identity`, `transfer_date`, `contract_reference`
   - Required docs: Transport Document

4. **LABORATORY_ASSAY** — Laboratory analysis and assay.
   - Mandatory metadata: `laboratory_name`, `assay_method`, `tungsten_content_pct`, `assay_certificate_ref`
   - Required docs: Assay Report

5. **PRIMARY_PROCESSING** — Primary processing (smelting).
   - Mandatory metadata: `smelter_id` (required), `process_type`, `input_weight_kg`, `output_weight_kg`
   - Required docs: Smelter Certificate
   - Triggers RMAP compliance check

6. **EXPORT_SHIPMENT** — Export/shipment.
   - Mandatory metadata: `origin_country`, `destination_country`, `transport_mode`, `export_permit_ref`
   - Required docs: Export Permit, Transport Document

### 4.4 Reference Data (Public Schema)

**RmapSmelter**
- `smelter_id` (string, PK)
- `smelter_name` (string)
- `country` (string)
- `conformance_status` (enum: CONFORMANT, ACTIVE_PARTICIPATING, NON_CONFORMANT)
- `last_audit_date` (date, nullable)
- `loaded_at` (timestamptz)

**RiskCountry**
- `country_code` (string, PK) — ISO 3166-1 alpha-2
- `country_name` (string)
- `risk_level` (enum: HIGH, MEDIUM, LOW)
- `source` (string) — e.g., "OECD Annex II"

**SanctionedEntity**
- `id` (UUID, PK)
- `entity_name` (string)
- `entity_type` (enum: INDIVIDUAL, ORGANIZATION, COUNTRY)
- `source` (string) — e.g., "UN Security Council"
- `loaded_at` (timestamptz)

---

## 5. Authentication & Authorization

### 5.1 Auth0 Configuration

- Single Auth0 tenant for the pilot.
- Three roles: SUPPLIER, BUYER, PLATFORM_ADMIN.
- Users are invitation-only — Platform Admin creates users in the platform, which provisions them in Auth0.
- JWT bearer token authentication on all API endpoints (except `/api/verify/{batchId}`).
- **Session expiry:** 8 hours of inactivity. No refresh tokens issued for the pilot (FR-P064).

### 5.2 Role Resolution

Per spec: role is resolved from the platform database via `/api/me`, not from JWT claims alone.

Flow:
1. User authenticates via Auth0 → receives JWT.
2. Angular app calls `GET /api/me` with JWT.
3. API validates JWT, extracts `sub` claim, looks up User by `auth0_sub`.
4. Returns user profile including role and tenant.
5. Angular stores user context, route guards use role for UX routing.
6. Every API endpoint independently validates role via authorization policies.

### 5.3 Authorization Policies

- `RequireSupplier` — SUPPLIER or PLATFORM_ADMIN
- `RequireBuyer` — BUYER or PLATFORM_ADMIN
- `RequireAdmin` — PLATFORM_ADMIN only
- `RequireTenantAccess` — validates user belongs to the tenant referenced in the request

---

## 6. API Design

### 6.1 Endpoint Groups (Minimal APIs)

**Auth & User**
- `GET /api/me` — current user profile + role
- `GET /api/users` (Admin) — list tenant users
- `POST /api/users` (Admin) — create/invite user
- `PATCH /api/users/{id}` (Admin) — update user role/status
- `DELETE /api/users/{id}` (Admin) — deactivate user

**Batches**
- `GET /api/batches` — list batches (filtered by role: supplier sees own, buyer sees all tenant batches)
- `GET /api/batches/{id}` — batch detail with events and compliance summary
- `POST /api/batches` (Supplier) — create new batch

**Custody Events**
- `GET /api/batches/{batchId}/events` — list events for a batch
- `POST /api/batches/{batchId}/events` (Supplier) — submit custody event (idempotent via `idempotency_key`)
- `POST /api/events/{eventId}/corrections` (Supplier) — submit correction event (creates new linked record)
- `GET /api/events/{id}` — event detail with compliance checks

**Documents**
- `POST /api/events/{eventId}/documents` (Supplier) — upload document (stored in R2, SHA-256 computed server-side)
- `GET /api/documents/{id}` — returns pre-signed R2 URL for download
- `GET /api/batches/{batchId}/documents` — list all documents for a batch

**Compliance**
- `GET /api/batches/{batchId}/compliance` — compliance summary for batch
- `GET /api/events/{eventId}/compliance` — compliance checks for event
- `POST /api/admin/rmap/upload` (Admin) — upload RMAP smelter list

**Document Generation**
- `POST /api/batches/{batchId}/passport` (Buyer) — generate Material Passport
- `GET /api/generated-documents/{id}` — download generated document (pre-signed URL)
- `POST /api/generated-documents/{id}/share` (Buyer) — generate time-limited share link (30 days)
- `POST /api/batches/{batchId}/dossier` (Buyer/Admin) — generate Audit Dossier

**Public (Unauthenticated)**
- `GET /api/verify/{batchId}` — public batch verification endpoint. Returns: batch identity, current compliance status, custody event count, hash verification result. Resolves QR codes on Material Passports (FR-P060). This is the **only unauthenticated endpoint**.
- `GET /api/shared/{token}` — download a shared Material Passport via time-limited token (FR-P053).

**Notifications**
- `GET /api/notifications` — list notifications for current user
- `PATCH /api/notifications/{id}/read` — mark as read

**Integrity**
- `GET /api/batches/{batchId}/verify-integrity` — recompute and verify hash chain for a batch

All error responses use RFC 7807 Problem Details. All handlers use the Result pattern for expected failures (no exception-driven flow per `.rules/DOTNET.md`).

### 6.2 API Conventions

- All responses are DTOs — never EF entities.
- Pagination: `?page=1&pageSize=20` with response envelope `{ items: [], totalCount, page, pageSize }`.
- Sorting: `?sortBy=createdAt&sortDir=desc`.
- Filtering: query parameters per resource (e.g., `?status=FLAGGED&eventType=PRIMARY_PROCESSING`).
- CORS: explicitly configured for the Angular static site URL only. No wildcard origins.
- Rate limiting: applied on public endpoints (`/api/verify`, `/api/shared`) and auth endpoints via `AddRateLimiter`.
- Audit logging: all API write operations logged with timestamp, user identity, endpoint, request payload hash, and response status (NFR-P08).

---

## 7. Compliance Engine

### 7.1 Trigger

Compliance checks run automatically after a custody event is persisted. Implemented as MediatR `INotification` handlers dispatched from a `SaveChangesInterceptor`.

### 7.2 RMAP Check

Triggered on events with `event_type = PRIMARY_PROCESSING` where `smelter_id` is present.

Rules:
1. Look up `smelter_id` in RmapSmelter reference table.
2. If not found → status: FLAG, detail: "Smelter not in RMAP list"
3. If found and conformance_status = CONFORMANT → status: PASS
4. If found and conformance_status = ACTIVE_PARTICIPATING → status: PASS (actively participating in audit)
5. If found and conformance_status = NON_CONFORMANT → status: FAIL, detail: "Smelter is non-conformant per RMAP"

### 7.3 OECD DDG Check

Runs on all custody events. Checks:

1. **Origin country risk:** Look up batch origin_country in RiskCountry. HIGH risk → FLAG. Country not found → PASS (assumed low risk).
2. **Sanctions check:** Check event actor_name against SanctionedEntity. Match → FAIL.
3. **Document completeness:** Verify required documents for the event type (per Section 4.3) are attached. Missing required docs → INSUFFICIENT_DATA.

Overall OECD status: worst-case of all sub-checks (FAIL > FLAG > INSUFFICIENT_DATA > PASS).

### 7.4 Batch Compliance Rollup

After each event compliance check, recalculate batch `compliance_status`:
- Any event FAIL → batch NON_COMPLIANT (displayed as FLAGGED in UI per spec)
- Any event FLAG → batch FLAGGED
- Any INSUFFICIENT_DATA and no FAILs/FLAGs → batch INSUFFICIENT_DATA
- All events PASS → batch COMPLIANT
- No checks yet → batch PENDING

### 7.5 Notifications

On FAIL or FLAG: create a notification record and send email to:
- The supplier who submitted the event
- All BUYER users in the tenant
- PLATFORM_ADMIN

**48-hour escalation (FR-P071d):** A scheduled background job checks for compliance flags not resolved within 48 hours and sends an escalation notification to PLATFORM_ADMIN.

**Email retry (FR-P072):** Failed email deliveries are retried up to 3 times over 2 hours before logging a permanent failure. Retry state tracked via `email_retry_count` on the Notification entity.

---

## 8. Document Generation

### 8.1 Material Passport (QuestPDF)

Generated on demand by buyer. Contains:
- Header: Platform logo, batch number, generation date, tenant name
- **QR code:** Encodes `{baseUrl}/api/verify/{batchId}` for public verification (FR-P060)
- Batch summary: mineral type, origin, weight, current status
- Custody chain: chronological list of all events with dates, locations, actors, event types
- Compliance summary: per-event RMAP and OECD DDG results, overall batch status
- Document registry: list of all attached documents with names, types, upload dates
- Tamper evidence: SHA-256 hash chain verification summary
- Footer: "Generated by Tungsten Supply Chain Compliance Platform" + timestamp

**Sharing (FR-P053):** Buyer can generate a time-limited share link (valid 30 days) via `POST /api/generated-documents/{id}/share`. External parties access via `GET /api/shared/{token}` without authentication.

### 8.2 Audit Dossier (QuestPDF)

Generated on demand by buyer or admin. Contains:
- Full event log (same as passport but with more detail including metadata)
- Complete document list with file sizes and uploaders
- Compliance check details: rule-by-rule results for each event
- Flags and failures highlighted

---

## 9. Angular Frontend

### 9.1 Design System (Tailwind CSS)

- **Color palette:** Slate grays for backgrounds/text, Blue-600 primary, Green-500 success/compliant, Amber-500 flagged/warning, Red-500 non-compliant/error.
- **Typography:** Inter font family via Google Fonts. Clean, modern, professional.
- **Layout:** Sidebar navigation (collapsible), top bar with user menu and notifications bell.
- **Components:** Cards for batch summaries, data tables with sort/filter/pagination, status badges (color-coded), form layouts following Signal Forms patterns.
- **Mobile responsive (FR-P043):** Supplier portal accessible on modern mobile browsers (iOS Safari, Android Chrome). Touch-friendly form design. Responsive breakpoints for sidebar collapse and stacked layouts on small screens.

### 9.2 Module Structure

```
src/app/
  core/
    auth/                 auth.service.ts, auth.guard.ts, auth.interceptor.ts
    http/                 error.interceptor.ts, retry.interceptor.ts, api-url.token.ts
    notifications/        notification.service.ts
    layout/               shell.component.ts (sidebar + topbar + router-outlet)
  shared/
    ui/
      data-table/         sortable, filterable, paginated table component
      status-badge/       compliance status badge (PASS/FAIL/FLAG/PENDING)
      file-upload/        drag-and-drop file upload component
      confirm-dialog/     confirmation modal
      page-header/        page title + breadcrumbs + action buttons
      loading-spinner/
      empty-state/        "No data" illustrations
    pipes/
      date-format.pipe.ts
    utils/
      error.utils.ts
  features/
    supplier/
      data/               supplier-api.service.ts, models, adapters
      ui/                 dumb components (event-form, batch-card, document-list)
      supplier.store.ts
      supplier.facade.ts
      supplier-dashboard.component.ts    (smart)
      submit-event.component.ts          (smart)
      batch-detail.component.ts          (smart)
      supplier.routes.ts
    buyer/
      data/               buyer-api.service.ts, models, adapters
      ui/                 dumb components (batch-table, compliance-summary, passport-card)
      buyer.store.ts
      buyer.facade.ts
      buyer-dashboard.component.ts       (smart)
      batch-detail.component.ts          (smart)
      buyer.routes.ts
    admin/
      data/               admin-api.service.ts, models, adapters
      ui/                 dumb components (user-table, user-form, rmap-upload)
      admin.store.ts
      admin.facade.ts
      admin-dashboard.component.ts       (smart)
      user-management.component.ts       (smart)
      rmap-management.component.ts       (smart)
      compliance-review.component.ts     (smart)
      admin.routes.ts
  app.config.ts
  app.routes.ts
```

### 9.3 Key Pages

**Supplier Portal:**
- Dashboard: active batches with status cards, recent events, compliance alerts
- Submit Event: multi-step form (select batch → event type → event details with type-specific mandatory fields → document upload → review & submit)
- Batch Detail: event timeline (with correction links), document list, compliance status per event

**Buyer Portal:**
- Dashboard: all tenant batches in a filterable table, compliance status overview (counts by status)
- Batch Detail: full event timeline, compliance details, document downloads, "Generate Passport" and "Generate Dossier" buttons, "Share Passport" link generation
- Document viewer: inline PDF preview for uploaded documents and generated passports

**Admin Portal:**
- Dashboard: system overview — user counts, batch counts, compliance flag counts
- User Management: table of users, invite new user form, edit role, deactivate
- RMAP Management: upload new smelter list (CSV), view current list, last sync date
- Compliance Review: list of flagged/failed events, ability to add notes (Platform Admin acts as Compliance Officer per spec)

### 9.4 Routing

```typescript
// app.routes.ts
export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', loadComponent: () => import('./features/auth/login.component') },
  {
    path: 'supplier',
    loadChildren: () => import('./features/supplier/supplier.routes'),
    canActivate: [authGuard, roleGuard('SUPPLIER')],
  },
  {
    path: 'buyer',
    loadChildren: () => import('./features/buyer/buyer.routes'),
    canActivate: [authGuard, roleGuard('BUYER')],
  },
  {
    path: 'admin',
    loadChildren: () => import('./features/admin/admin.routes'),
    canActivate: [authGuard, roleGuard('PLATFORM_ADMIN')],
  },
];
```

Post-login redirect based on role returned from `/api/me`.

---

## 10. Tamper Evidence

Each custody event is hashed at write time:

```
hash = SHA256(canonical_json(event_type, event_date, batch_id, location, actor_name, smelter_id, description, metadata, previous_event_hash))
```

- `previous_event_hash` is the hash of the most recent prior event for the same batch (null for first event).
- This creates a per-batch hash chain.
- Integrity verification: recompute hashes for all events in a batch and compare. Any mismatch indicates tampering.
- Verification is on-demand via API endpoint `GET /api/batches/{batchId}/verify-integrity`.
- The public endpoint `GET /api/verify/{batchId}` also returns hash verification result.

---

## 11. Email Notifications

Triggered events:
1. **Compliance flag/failure** — sent to supplier who submitted the event + all tenant buyers + admin
2. **Document available** — sent to all tenant buyers when a supplier uploads a document
3. **Material Passport generated** — sent to the buyer who requested it + the admin
4. **User invited** — sent to the new user with Auth0 invite link
5. **Compliance escalation** — sent to PLATFORM_ADMIN when a compliance flag is not resolved within 48 hours (FR-P071d)

Implementation: SendGrid integration via a notification service. Email templates are code-deployed (no UI editor in pilot). **All HTML emails include a plain-text fallback (FR-P070).** Failed emails retry up to 3 times over 2 hours (FR-P072).

---

## 12. Testing Strategy

### 12.1 Unit Tests

**.NET (xUnit + NSubstitute):**
- Every MediatR handler (commands and queries)
- Every FluentValidation validator
- RMAP compliance checker — all smelter status scenarios (CONFORMANT, ACTIVE_PARTICIPATING, NON_CONFORMANT, not found)
- OECD DDG compliance checker — risk country, sanctions, document completeness
- SHA-256 hash computation and chain verification
- Idempotency key generation and duplicate rejection
- Event correction linking logic
- Adapter/mapping functions
- Domain logic (batch status rollup, compliance rollup)

**Angular (Jasmine + Karma):**
- All dumb/presentational components — input/output behavior
- Facade services — verify signal state management
- Store services — state transitions
- Adapter functions — DTO to domain transforms
- Pipes and utility functions
- Signal Forms — validation rules per event type

### 12.2 Integration Tests

**.NET (WebApplicationFactory + Testcontainers):**
- Full request → handler → DB round-trips for all endpoints
- Auth: valid JWT, invalid JWT, wrong role → 403
- Compliance engine: submit event → check runs → results persisted
- Document upload/download flow with R2
- Material Passport generation → PDF returned with QR code
- Tenant isolation: user from tenant A cannot access tenant B data
- Idempotency: duplicate event submission → 409 Conflict
- Event correction: correction links to original event
- Public verification endpoint: unauthenticated access works
- Share link: valid token returns document, expired token → 404

### 12.3 E2E Tests (Playwright)

Critical user journeys:
1. Supplier logs in → creates batch → submits custody events (with type-specific mandatory fields) with documents → sees compliance status
2. Buyer logs in → views batch list → drills into batch → generates Material Passport → downloads PDF → shares via link
3. Admin logs in → invites user → uploads RMAP list → reviews compliance flags
4. Compliance flow: supplier submits event with non-conformant smelter → flag appears → buyer sees flagged batch

---

## 13. Implementation Phases

### Phase 1: Foundation
- Monorepo scaffold (packages/shared, api, worker, web)
- Shared Zod schemas and TypeScript domain types for all six event types
- .NET project setup: API + Worker, EF Core, DbContext, migrations
- PostgreSQL schema creation with Tenant entity and tenant isolation
- Auth0 integration: JWT validation, `/api/me`, authorization policies, 8-hour session expiry
- Angular scaffold: Tailwind setup, Auth0 SDK, core module, shell layout
- Sentry integration, OpenTelemetry instrumentation
- Health check endpoint
- CORS and rate limiting configuration
- Audit logging middleware
- Unit tests: auth policies, user lookup

### Phase 2: Custody Events
- Batch CRUD endpoints + handlers + validators
- CustodyEvent creation endpoint with SHA-256 hashing and hash chain
- Idempotency key computation and duplicate rejection (FR-P002)
- Event correction endpoint (FR-P003) — creates linked correction record
- Per-event-type metadata validation via Zod schemas
- Integrity verification endpoint
- Unit tests: all handlers, validators, hash computation, chain verification, idempotency, corrections
- Integration tests: batch and event CRUD round-trips, idempotency, corrections

### Phase 3: Compliance Engine
- RMAP checker implementation (CONFORMANT, ACTIVE_PARTICIPATING, NON_CONFORMANT)
- Reference data seeding (RMAP smelter list, risk countries, sanctions)
- OECD DDG checker implementation (risk country, sanctions, doc completeness → INSUFFICIENT_DATA)
- MediatR notification dispatch after event save
- Batch compliance rollup logic
- Unit tests: every compliance rule scenario
- Integration tests: event submission triggers compliance checks

### Phase 4: Document Management
- Cloudflare R2 integration (upload, pre-signed download URLs)
- File upload endpoint with validation (type, size up to 25MB) and SHA-256 hash (FR-P031)
- File download via pre-signed URLs
- Document linking to events and batches
- Unit tests: validation, storage service, hash computation
- Integration tests: upload/download round-trip

### Phase 5: Document Generation
- QuestPDF setup
- Material Passport PDF template with QR code (encoding public verification URL)
- Audit Dossier PDF template
- Generation endpoints (POST triggers, GET downloads via pre-signed URLs)
- Share link generation (30-day expiry) and public share endpoint (FR-P053)
- Unit tests: PDF generation with mock data, share token generation/expiry
- Integration tests: generate and verify PDF content, share flow

### Phase 6: Supplier Portal
- Supplier dashboard component (batch list with status cards)
- Submit event multi-step form (Signal Forms) with per-event-type mandatory fields
- Event correction submission UI
- Document upload UI (drag-and-drop)
- Batch detail view (event timeline with corrections, compliance status)
- Shared components: data table, status badge, file upload, page header
- Mobile responsive layout (FR-P043)
- Unit tests: all components, store, facade

### Phase 7: Buyer Portal
- Buyer dashboard (batch table with compliance overview)
- Batch detail view (event timeline, compliance details, document list)
- Material Passport generation trigger + download + share link UI
- Audit Dossier generation trigger + download
- Inline document/PDF viewer
- Unit tests: all components, store, facade

### Phase 8: Admin Portal
- User management (list, invite, edit, deactivate)
- RMAP smelter list upload (CSV parsing and storage)
- Compliance flag review dashboard with notes
- Unit tests: all components, store, facade

### Phase 9: Notifications
- SendGrid integration with HTML + plain-text templates
- Email retry logic (3 retries over 2 hours) (FR-P072)
- 48-hour compliance escalation background job (FR-P071d)
- Notification persistence and read tracking
- Notification bell UI in shell topbar
- Unit tests: notification service, template rendering, retry logic, escalation scheduling

### Phase 10: E2E Tests & Polish
- Playwright test setup
- Four critical journey tests (see Section 12.3)
- UI polish: loading states, empty states, error states, responsive layout
- Final integration testing pass

---

## 14. Non-Functional Requirements (from spec)

- **NFR-P01:** API response < 3 seconds at p95 under pilot load (≤ 20 concurrent users). Page load < 4 seconds.
- **NFR-P02:** Document generation < 10 seconds.
- **NFR-P03:** File uploads up to 25MB.
- **NFR-P04:** Availability: standard Render SLA (no custom HA for pilot).
- **NFR-P08:** All API write operations logged with timestamp, user identity, endpoint, request payload hash, response status.
- **Data:** All pilot data is confidential commercial data. No classified/ITAR material.
- **Render tiers:** Standard (paid) for Web Service. PostgreSQL hosted on Neon (serverless, connection pooling via pooler endpoint).

---

## 15. Migration Considerations

Per spec Section 11, the following are non-negotiable design constraints:

- Monorepo with clear package boundaries
- Shared package portable without modification
- Compliance rule interface abstracted for future rule-authoring UI
- Tenant isolation model upgradeable to full multi-tenancy
- All infrastructure config externalized (env vars) for Azure migration
- OpenTelemetry instrumentation from pilot day one (for migration to Azure Monitor)
