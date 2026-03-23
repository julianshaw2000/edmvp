# Phase 11: Production Readiness + Audit Logging — Design Spec

**Date:** 2026-03-23
**Status:** Approved
**Prerequisite:** All 10 MVP phases complete, platform deployed on Render

---

## Overview

Two workstreams preparing the platform for real pilot customers:

- **Workstream A:** Business-event audit logging via MediatR pipeline behaviour
- **Workstream B:** Production hardening (Auth0, error tracking, health checks, CI/CD)

---

## Workstream A: Business-Event Audit Logging

### Problem

The platform tracks compliance events with SHA-256 hash chains, but there is no record of *who did what and when* at the application level. Auditors and admins need a searchable trail of all user actions — batch creation, custody event logging, document generation, user management — to satisfy compliance requirements and investigate incidents.

### Prerequisite: Extend ICurrentUserService

The current `ICurrentUserService` only exposes `Auth0Sub` (string). Every handler resolves `UserId` and `TenantId` via its own DB lookup. The audit behaviour needs these values without duplicating the lookup.

**Change:** Extend `ICurrentUserService` to cache `UserId` (Guid) and `TenantId` (Guid) per-request. On first access, resolve from DB via `Auth0Sub` and cache for the remainder of the request scope. This also simplifies existing handlers that currently do their own lookups.

```csharp
public interface ICurrentUserService
{
    string Auth0Sub { get; }
    Task<Guid> GetUserIdAsync(CancellationToken ct);
    Task<Guid> GetTenantIdAsync(CancellationToken ct);
}
```

Implementation caches after first DB hit (scoped lifetime = one request).

### Approach: MediatR Pipeline Behaviour

A new `AuditBehaviour<TRequest, TResponse>` in the MediatR pipeline automatically logs every command that implements the `IAuditable` marker interface. This guarantees coverage without modifying existing handlers.

```
Request → ValidationBehaviour → AuditBehaviour → Handler → Response
                                     ↓
                              Writes AuditLog row
```

**Pipeline registration order:** `AuditBehaviour` must be registered AFTER `ValidationBehaviour` in `Program.cs` so that validation failures short-circuit before reaching the audit step:

```csharp
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehaviour<,>));
```

### Data Model

Implemented via EF Core entity configuration (`AuditLogConfiguration.cs`) and a new migration, consistent with existing patterns in `Infrastructure/Persistence/Configurations/`.

```
AuditLogEntity:
    Id              Guid, PK
    TenantId        Guid, FK → Tenants, NOT NULL
    UserId          Guid, FK → Users, NOT NULL
    Action          string(100), NOT NULL       -- e.g., "CreateBatch"
    EntityType      string(50), NOT NULL        -- e.g., "Batch"
    EntityId        Guid?                       -- the affected resource ID (nullable)
    BatchId         Guid?                       -- denormalized for activity feed queries
    Payload         jsonb                       -- serialized command (redacted)
    Result          string(20), NOT NULL        -- "Success" or "Failure"
    FailureReason   string?                     -- if failed, why
    IpAddress       string(45)?
    UserAgent       string(500)?
    Timestamp       DateTimeOffset, NOT NULL
```

**Indexes:**
- `(TenantId, Timestamp DESC)` — admin audit log viewer
- `(TenantId, EntityType, EntityId)` — entity-specific lookups
- `(TenantId, BatchId, Timestamp)` — batch activity feed

**Retention:** No auto-purge. Compliance platforms require indefinite audit trails.

**BatchId denormalization:** For custody events, documents, and generated documents, `BatchId` is populated from the command's `BatchId` property. For batch operations, `BatchId` equals `EntityId`. This avoids expensive joins when querying the batch activity feed.

### Marker Interface

```csharp
public interface IAuditable
{
    string AuditAction { get; }    // e.g., "CreateBatch"
    string EntityType { get; }     // e.g., "Batch"
}
```

Commands opt in:

```csharp
public record Command(...) : IRequest<Result<Response>>, IAuditable
{
    public string AuditAction => "CreateBatch";
    public string EntityType => "Batch";
}
```

### AuditBehaviour Logic

1. Check if request implements `IAuditable` — if not, skip (pass through to handler).
2. Execute the handler.
3. After handler returns, unwrap `Result<T>`:
   - If `Result.IsSuccess`: extract `EntityId` from `result.Value` by reflecting for a `Guid Id` property. If no `Id` property exists (e.g., `UploadRmapList.Response` has `int Imported`), `EntityId` is null.
   - If `Result.IsFailure`: `EntityId` is null, `FailureReason` is set from `result.Error`.
4. Serialize the command payload to JSON:
   - Properties marked with `[AuditRedact]` are replaced with `"[REDACTED]"`.
   - Properties of type `Stream` are excluded from serialization (replaced with `"[STREAM]"`).
   - A custom `JsonConverter` handles both cases during serialization.
5. Extract `BatchId`:
   - If `EntityType` is `"Batch"`, `BatchId` = `EntityId`.
   - Otherwise, reflect on the command for a `Guid BatchId` property and use it if present.
6. Resolve `UserId` and `TenantId` from `ICurrentUserService.GetUserIdAsync()` / `GetTenantIdAsync()`.
7. Resolve IP address and User-Agent from `IHttpContextAccessor`.
8. Write the `AuditLogEntity` row via `AppDbContext`.

### Design Decisions

- **Audit after handler execution:** Failed validations (caught by `ValidationBehaviour` before reaching the handler) are not audited since no business action occurred.
- **Failed business operations are audited:** When a handler returns `Result.Failure`, the audit entry records the failure reason. These represent real user actions that were denied by business rules.
- **Fire-and-forget write:** The audit write is wrapped in a try/catch. If audit persistence fails, it logs an error via `ILogger` but does not fail the user's request.
- **No read auditing:** Only write commands are audited. Read operations (queries) are not logged to avoid noise and performance overhead.
- **Worker-triggered commands are not audited:** The `AuditBehaviour` checks `IHttpContextAccessor.HttpContext` — if null (background worker context), it skips audit logging. Worker operations are tracked via the existing `Jobs` table. If worker auditing is needed later, a `SystemAuditService` can be added.
- **Coexistence with AuditLoggingMiddleware:** The existing `AuditLoggingMiddleware` is kept as-is. It serves a different purpose: infrastructure-level request logging (method, path, body hash, status code) to structured logs. The new `AuditBehaviour` provides business-level audit trails to the database. They complement each other — middleware for ops/security, behaviour for compliance/admin.

### Commands to Mark as IAuditable

| Command | AuditAction | EntityType | Has BatchId? |
|---------|-------------|------------|-------------|
| `CreateBatch` | CreateBatch | Batch | EntityId = BatchId |
| `UpdateBatchStatus` | UpdateBatchStatus | Batch | EntityId = BatchId |
| `SplitBatch` | SplitBatch | Batch | EntityId = BatchId |
| `CreateCustodyEvent` | CreateCustodyEvent | CustodyEvent | Yes (cmd.BatchId) |
| `CreateCorrection` | CreateCorrection | CustodyEvent | Yes (cmd.BatchId) |
| `UploadDocument` | UploadDocument | Document | Yes (cmd.BatchId) |
| `GeneratePassport` | GeneratePassport | GeneratedDocument | Yes (cmd.BatchId) |
| `GenerateDossier` | GenerateDossier | GeneratedDocument | Yes (cmd.BatchId) |
| `ShareDocument` | ShareDocument | GeneratedDocument | No |
| `CreateUser` | CreateUser | User | No |
| `UpdateUser` | UpdateUser | User | No |
| `UploadRmapList` | UploadRmapList | RmapSmelter | No |

**Note on UploadRmapList:** The `Stream CsvStream` property is serialized as `"[STREAM]"` in the audit payload. The audit entry captures the action and result (`Imported: N, Updated: M, Total: T`) but not the file contents.

### API Endpoints

**Admin audit log viewer:**
```
GET /api/admin/audit-logs?page=1&size=20&userId=&action=&entityType=&from=&to=
```
- Admin role required
- Returns `PagedResponse<AuditLogDto>`
- Filters: userId, action, entityType, date range (from/to as ISO 8601)

**Batch activity feed:**
```
GET /api/batches/{id}/activity
```
- Supplier or Buyer role (scoped to tenant)
- Queries `audit_logs` where `BatchId = {id}` AND `TenantId = user's tenant` (single indexed query, no joins)
- Chronological order, not paginated (batch-scoped, bounded volume — typically <100 entries per batch)

### Frontend

**Admin Dashboard — Audit Log Page (`/admin/audit-log`):**
- Searchable, filterable table: Timestamp, User, Action, Entity, Result
- Filters: date range picker, user dropdown, action type dropdown, entity type dropdown, success/failure toggle
- Click row to expand full payload (redacted)
- Paginated via existing `PagedRequest`/`PagedResponse`
- Signal-based state store + `httpResource()` for reads

**Batch Detail — Activity Feed Tab:**
- New tab on Supplier and Buyer batch detail screens
- Chronological list: timestamp, user display name, action description
- Read-only, no filters — scoped to one batch
- Uses `httpResource()` for the GET read

---

## Workstream B: Production Hardening

### Auth0 Production Setup

- Configure production Auth0 tenant (separate from dev/demo)
- Set callback URLs for Render production domain (`auditraks.com`)
- Configure role assignment rules for Supplier, Buyer, Admin
- Document setup steps in `docs/auth0-production.md`

### Health Check Endpoints

The existing `GET /health` endpoint returns `{ status: "starting" }` or `{ status: "healthy" }` based on `DatabaseMigrationService.IsReady`. This is refactored into two standard ASP.NET Core health check endpoints using `AddHealthChecks()` / `MapHealthChecks()`:

- `GET /health/live` — liveness probe (returns 200 if process is running, replaces basic `/health`)
- `GET /health/ready` — readiness probe (checks `DatabaseMigrationService.IsReady`, PostgreSQL connectivity via `AddNpgSql()`, and R2 bucket reachability)
- Render uses these for zero-downtime deploys and auto-restart on failure

### Error Tracking (Sentry)

The API already has Sentry SDK wired in `Program.cs` (conditional on `Sentry:Dsn` config). Remaining work:

- **API:** Attach user context middleware (Auth0 sub, role — no PII)
- **Angular frontend:** Add `@sentry/angular` package, configure `ErrorHandler` provider override in `app.config.ts`, set environment tag
- Environment-tagged: dev / staging / production

### CI/CD — GitHub Actions

**On push to `main`:**
1. `dotnet build` + `dotnet test` + `dotnet format --verify-no-changes`
2. `ng build` + `ng test`
3. Deploy API to Render via deploy hook (webhook URL stored in GitHub repository secrets as `RENDER_API_DEPLOY_HOOK` and `RENDER_WEB_DEPLOY_HOOK`)
4. Deploy web to Render via deploy hook
5. Verify deployment by hitting `/health/ready` endpoint

**On pull request:**
1. Build + test only (no deploy)

### Tenant Isolation Verification

- Review all EF Core queries to verify `TenantId` filtering is present
- Add integration test that creates two tenants with data and verifies cross-tenant queries return empty results
- This is verification of existing behaviour, not new architecture

---

## Out of Scope

- Full multi-tenant SaaS (self-service signup, billing, plan tiers) — deferred until post-revenue
- API-level request logging — already handled by existing `AuditLoggingMiddleware` (kept as-is for ops/security)
- Read operation auditing — unnecessary noise for compliance use case
- Audit log export/download — can be added later if auditors request it
- Worker operation auditing — tracked via existing `Jobs` table, can be added later via `SystemAuditService`

---

## Success Criteria

1. Every auditable write action creates an `AuditLog` entry with correct user, action, entity, and payload
2. Admin can search and filter audit logs in the dashboard
3. Batch detail screens show chronological activity feed via `BatchId` index
4. Failed business operations are logged with failure reason
5. Stream properties (e.g., CSV uploads) are safely excluded from serialization
6. Worker-triggered commands do not throw when no HTTP context exists
7. Health endpoints return correct status using ASP.NET Core HealthChecks middleware
8. Sentry captures unhandled exceptions in both API and Angular frontend
9. GitHub Actions pipeline runs build + test on every push, deploys on main
10. No cross-tenant data leakage in audit log queries
