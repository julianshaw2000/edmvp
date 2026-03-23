# Phase 12: Tenant Isolation + Management — Design Spec

**Date:** 2026-03-23
**Status:** Approved
**Prerequisite:** Phase 11 complete (audit logging, production hardening)

---

## Overview

Transform the platform from single-tenant (one hardcoded "Pilot Tenant") to proper multi-tenancy with tenant provisioning, role separation, and tenant-scoped administration. Row-level filtering (already in place) remains the isolation mechanism.

---

## Role Model

### Current Roles
- SUPPLIER, BUYER, PLATFORM_ADMIN

### New Roles
- **PLATFORM_ADMIN** — super admin (platform owner). Can list/create/suspend tenants, view all data across tenants. Only assignable by another PLATFORM_ADMIN.
- **TENANT_ADMIN** — first user created when a tenant is provisioned. Can invite users to their tenant, assign roles (SUPPLIER/BUYER), deactivate users. Cannot see other tenants.
- **SUPPLIER** — unchanged. Creates batches, logs custody events.
- **BUYER** — unchanged. Views batches, generates Material Passports.

### Role Hierarchy
- PLATFORM_ADMIN > TENANT_ADMIN > SUPPLIER/BUYER
- TENANT_ADMIN cannot assign PLATFORM_ADMIN or TENANT_ADMIN roles
- PLATFORM_ADMIN can assign any role including TENANT_ADMIN

---

## Tenant Lifecycle

### Creation Flow
1. PLATFORM_ADMIN enters: company name, admin email
2. API creates `TenantEntity` (status: ACTIVE, SchemaPrefix auto-generated from name)
3. API creates `UserEntity` with role TENANT_ADMIN, Auth0Sub set to `pending|{email}`, IsActive = true
4. Platform admin tells the customer: "Go to the app URL and sign in with Google"
5. When that person logs in, `/api/me` matches by email and links their Auth0 identity

### Tenant Statuses
- **ACTIVE** — normal operation
- **SUSPENDED** — users cannot log in, API returns 403 for all requests from this tenant

### Suspension Enforcement
- On every authenticated request, after resolving the user's tenant, check tenant status
- If SUSPENDED, return 403 with "Your organization's account has been suspended. Contact support."
- Implemented in `ICurrentUserService` or as a middleware check

---

## API Changes

### New Platform Admin Endpoints

**Create Tenant:**
```
POST /api/platform/tenants
Body: { "name": "Acme Mining Corp", "adminEmail": "admin@acme.com" }
Response: { "id": "guid", "name": "...", "status": "ACTIVE", "adminEmail": "...", "createdAt": "..." }
```
- PLATFORM_ADMIN only
- Creates tenant + TENANT_ADMIN user
- Validates: name not empty, email not already in use
- Auto-generates SchemaPrefix from name (lowercase, underscored, truncated to 50 chars)

**List Tenants:**
```
GET /api/platform/tenants?page=1&pageSize=20
Response: PagedResponse<TenantDto> with { id, name, status, userCount, batchCount, createdAt }
```
- PLATFORM_ADMIN only
- Includes aggregate counts per tenant

**Update Tenant Status:**
```
PATCH /api/platform/tenants/{id}/status
Body: { "status": "SUSPENDED" | "ACTIVE" }
Response: { "id": "guid", "name": "...", "status": "..." }
```
- PLATFORM_ADMIN only
- Cannot suspend the tenant containing the requesting PLATFORM_ADMIN

### Modified Endpoints

**`POST /api/users` (Invite User):**
- Currently: PLATFORM_ADMIN only
- New: TENANT_ADMIN can also invoke, but restricted to their own tenant
- TENANT_ADMIN can only assign roles: SUPPLIER, BUYER
- PLATFORM_ADMIN can assign any role including TENANT_ADMIN

**`PATCH /api/users/{id}` (Update User):**
- Currently: PLATFORM_ADMIN only
- New: TENANT_ADMIN can update users within their own tenant
- TENANT_ADMIN cannot change a user's role to PLATFORM_ADMIN or TENANT_ADMIN
- TENANT_ADMIN cannot modify other TENANT_ADMIN users

**`GET /api/users` (List Users):**
- Already filtered by TenantId — no change needed
- Both PLATFORM_ADMIN and TENANT_ADMIN can access

### Modified `/api/me` Auto-Provisioning

**Remove** the fallback that creates a new user as PLATFORM_ADMIN on the first active tenant.

**New flow:**
1. Check if Auth0Sub matches an existing user → return user
2. Extract email from token claims
3. Check if email matches a `pending|` user → link Auth0Sub, return user
4. Check if email matches an existing active user → relink Auth0Sub, return user
5. **No match found:** return 403 with `{ "error": "No account found. Contact your administrator to get access." }`

---

## Authorization Policy Changes

### Current Policies
- `RequireAdmin` — PLATFORM_ADMIN only
- `RequireSupplier` — SUPPLIER only

### New Policies
- `RequirePlatformAdmin` — PLATFORM_ADMIN only (tenant management, RMAP uploads, job monitor)
- `RequireAdmin` — PLATFORM_ADMIN or TENANT_ADMIN (user management, audit logs)
- `RequireSupplier` — SUPPLIER only (batch creation, event logging)

### Endpoint Policy Mapping
| Endpoint | Current Policy | New Policy |
|----------|---------------|------------|
| `POST /api/platform/tenants` | N/A (new) | RequirePlatformAdmin |
| `GET /api/platform/tenants` | N/A (new) | RequirePlatformAdmin |
| `PATCH /api/platform/tenants/{id}/status` | N/A (new) | RequirePlatformAdmin |
| `POST /api/admin/rmap/upload` | RequireAdmin | RequirePlatformAdmin |
| `GET /api/admin/rmap` | RequireAdmin | RequirePlatformAdmin |
| `GET /api/admin/jobs` | RequireAdmin | RequirePlatformAdmin |
| `GET /api/admin/audit-logs` | RequireAdmin | RequireAdmin (both roles) |
| `GET /api/users` | RequireAuthorization | RequireAdmin |
| `POST /api/users` | RequireAdmin | RequireAdmin |
| `PATCH /api/users/{id}` | RequireAdmin | RequireAdmin |

### Tenant-Scoped Authorization
When TENANT_ADMIN invokes user management endpoints, the handler must verify the target user belongs to the same tenant. PLATFORM_ADMIN bypasses this check.

---

## Frontend Changes

### Platform Admin — Tenant Management Page (`/admin/tenants`)
- Route: `/admin/tenants` (lazy-loaded, PLATFORM_ADMIN only)
- Table: tenant name, status badge, user count, batch count, created date
- "Create Tenant" button → modal/form with company name and admin email fields
- Row action: Suspend/Reactivate toggle button
- Back link to admin dashboard

### Admin Dashboard Visibility
- **PLATFORM_ADMIN sees:** Tenants, Users, Audit Log, RMAP Smelters, Jobs
- **TENANT_ADMIN sees:** Users, Audit Log (scoped to their tenant)
- Dashboard cards shown/hidden based on role

### User Management Updates
- TENANT_ADMIN sees invite form with role picker showing only SUPPLIER and BUYER
- PLATFORM_ADMIN sees full role picker including TENANT_ADMIN
- TENANT_ADMIN cannot see or modify PLATFORM_ADMIN users in the user list

### Login Flow Change
- Remove auto-provisioning fallback
- On 403 from `/api/me`, show: "No account found. Contact your administrator to get access."
- Keep the "Sign in with Google" button — it just won't auto-create accounts anymore

### Route Guards
- Add `platformAdminGuard` for `/admin/tenants` route
- Existing `roleGuard('PLATFORM_ADMIN')` becomes `roleGuard('PLATFORM_ADMIN', 'TENANT_ADMIN')` for admin routes that both roles can access

---

## Tenant Suspension Enforcement

When a tenant is suspended, all API requests from users in that tenant must fail:

**Implementation:** Extend `ICurrentUserService.ResolveUserAsync()` to also load the tenant status. If tenant status is SUSPENDED, throw a custom `TenantSuspendedException`. Handle this in a global exception handler that returns 403 with the suspension message.

This is a single check point — every request already goes through `ICurrentUserService` to resolve the user.

---

## Database Changes

**No new tables. No new migrations.**

- `TenantEntity` already has: Id, Name, SchemaPrefix, Status, CreatedAt
- `UserEntity` already has: Role field (string) — adding "TENANT_ADMIN" as a valid value is a code-only change
- `Roles.cs` constants file gets the new role constant

---

## Out of Scope

- Self-service signup (Phase 13)
- Stripe billing (Phase 13)
- Tenant settings/branding
- Tenant-level feature flags
- Cross-tenant data sharing
- Schema-per-tenant isolation

---

## Success Criteria

1. PLATFORM_ADMIN can create a new tenant with a TENANT_ADMIN user
2. TENANT_ADMIN can invite SUPPLIER and BUYER users to their tenant
3. TENANT_ADMIN cannot see or manage other tenants
4. Suspended tenants return 403 on all API requests
5. New Google logins without a pre-existing account see "Contact your administrator"
6. PLATFORM_ADMIN dashboard shows tenant management page
7. TENANT_ADMIN dashboard shows only Users and Audit Log
8. All existing tenant isolation (row-level filtering) continues to work
9. Existing demo data and Pilot Tenant remain functional
