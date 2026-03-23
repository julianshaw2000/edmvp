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

### Approach: MediatR Pipeline Behaviour

A new `AuditBehaviour<TRequest, TResponse>` in the MediatR pipeline automatically logs every command that implements the `IAuditable` marker interface. This guarantees coverage without modifying existing handlers.

```
Request → ValidationBehaviour → AuditBehaviour → Handler → Response
                                     ↓
                              Writes AuditLog row
```

### Data Model

```sql
CREATE TABLE audit_logs (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL REFERENCES tenants(id),
    user_id UUID NOT NULL REFERENCES users(id),
    action VARCHAR(100) NOT NULL,        -- e.g., "CreateBatch"
    entity_type VARCHAR(50) NOT NULL,    -- e.g., "Batch"
    entity_id UUID,                      -- the affected resource ID
    payload JSONB,                       -- serialized command (sensitive fields redacted)
    result VARCHAR(20) NOT NULL,         -- "Success" or "Failure"
    failure_reason TEXT,                 -- if failed, why
    ip_address VARCHAR(45),
    user_agent VARCHAR(500),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_audit_logs_tenant_timestamp ON audit_logs (tenant_id, timestamp DESC);
CREATE INDEX ix_audit_logs_tenant_entity ON audit_logs (tenant_id, entity_type, entity_id);
```

**Retention:** No auto-purge. Compliance platforms require indefinite audit trails.

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
3. After handler returns, extract `EntityId` from the response via convention (response records with a `Guid Id` property).
4. Serialize the command payload to JSON. Fields marked with `[AuditRedact]` attribute are replaced with `"[REDACTED]"`.
5. Resolve user ID and tenant ID from `ICurrentUserService`.
6. Resolve IP address and User-Agent from `IHttpContextAccessor`.
7. Write the `AuditLogEntity` row.

### Design Decisions

- **Audit after handler execution:** Failed validations (caught by `ValidationBehaviour` before `AuditBehaviour` runs through to the handler) are not audited since no business action occurred.
- **Failed business operations are audited:** When a handler returns `Result.Failure`, the audit entry records the failure reason. These represent real user actions that were denied by business rules.
- **Fire-and-forget write:** The audit write uses a separate `SaveChangesAsync` call to avoid coupling with the handler's transaction. If audit persistence fails, it logs an error but does not fail the user's request.
- **No read auditing:** Only write commands are audited. Read operations (queries) are not logged to avoid noise and performance overhead.

### Commands to Mark as IAuditable

| Command | AuditAction | EntityType |
|---------|-------------|------------|
| `CreateBatch` | CreateBatch | Batch |
| `UpdateBatchStatus` | UpdateBatchStatus | Batch |
| `SplitBatch` | SplitBatch | Batch |
| `CreateCustodyEvent` | CreateCustodyEvent | CustodyEvent |
| `CreateCorrection` | CreateCorrection | CustodyEvent |
| `UploadDocument` | UploadDocument | Document |
| `GeneratePassport` | GeneratePassport | GeneratedDocument |
| `GenerateDossier` | GenerateDossier | GeneratedDocument |
| `ShareDocument` | ShareDocument | GeneratedDocument |
| `CreateUser` | CreateUser | User |
| `UpdateUser` | UpdateUser | User |
| `UploadRmapList` | UploadRmapList | RmapSmelter |

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
- Returns audit entries where `EntityId` matches the batch ID or any custody event / document belonging to that batch
- Chronological order, not paginated (batch-scoped, bounded volume)

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
- Set callback URLs for Render production domain (`accutrac.org`)
- Configure role assignment rules for Supplier, Buyer, Admin
- Document setup steps in `docs/auth0-production.md`

### Health Check Endpoints

- `GET /health` — liveness probe (returns 200 if process is running)
- `GET /health/ready` — readiness probe (checks PostgreSQL connectivity and R2 bucket reachability)
- Render uses these for zero-downtime deploys and auto-restart on failure

### Error Tracking (Sentry)

- Sentry SDK integrated in ASP.NET Core API (unhandled exceptions, failed requests)
- Sentry SDK integrated in Angular frontend (uncaught errors, console errors)
- Environment-tagged: dev / staging / production
- User context attached (Auth0 sub, role) — no PII (no email, no name)

### CI/CD — GitHub Actions

**On push to `main`:**
1. `dotnet build` + `dotnet test` + `dotnet format --verify-no-changes`
2. `ng build` + `ng test`
3. Deploy API to Render (deploy hook webhook)
4. Deploy web to Render (deploy hook webhook)

**On pull request:**
1. Build + test only (no deploy)

### Tenant Isolation Verification

- Review all EF Core queries to verify `TenantId` filtering is present
- Add integration test that creates two tenants with data and verifies cross-tenant queries return empty results
- This is verification of existing behaviour, not new architecture

---

## Out of Scope

- Full multi-tenant SaaS (self-service signup, billing, plan tiers) — deferred until post-revenue
- API-level request logging — already handled by existing `AuditLoggingMiddleware`
- Read operation auditing — unnecessary noise for compliance use case
- Audit log export/download — can be added later if auditors request it

---

## Success Criteria

1. Every auditable write action creates an `AuditLog` entry with correct user, action, entity, and payload
2. Admin can search and filter audit logs in the dashboard
3. Batch detail screens show chronological activity feed
4. Failed business operations are logged with failure reason
5. Health endpoints return correct status for Render monitoring
6. Sentry captures unhandled exceptions in both API and frontend
7. GitHub Actions pipeline runs build + test on every push
8. No cross-tenant data leakage in audit log queries
