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
- **PLATFORM_ADMIN** — super admin (platform owner). Can list/create/suspend tenants, view all data across tenants. Only assignable by another PLATFORM_ADMIN. Continues to bypass all authorization policies (existing behavior in `RoleAuthorizationHandler` line 28).
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
2. API validates: name not empty, admin email globally unique across all tenants (`db.Users.AnyAsync(u => u.Email == cmd.AdminEmail)`)
3. API creates `TenantEntity` (status: ACTIVE, SchemaPrefix auto-generated from name)
4. If generated SchemaPrefix already exists, append numeric suffix (e.g., `acme_mining_2`)
5. API creates `UserEntity` with role TENANT_ADMIN, Auth0Sub set to `pending|{email}`, IsActive = true, assigned to the new tenant
6. Platform admin tells the customer: "Go to the app URL and sign in with Google"
7. When that person logs in, `/api/me` matches by email and links their Auth0 identity

### Tenant Statuses
- **ACTIVE** — normal operation
- **SUSPENDED** — users cannot log in, API returns 403 for all requests from this tenant

### Suspension Enforcement

Implemented as a MediatR pipeline behavior (`TenantStatusBehaviour<TRequest, TResponse>`) registered after `ValidationBehaviour` and before `AuditBehaviour`. This follows the Result pattern per CLAUDE.md rules — no exceptions thrown for expected business conditions.

**Logic:**
1. Resolve tenant status via `ICurrentUserService` (extend `ResolveUserAsync` to also load `Tenant.Status`)
2. If tenant status is SUSPENDED and the user is not PLATFORM_ADMIN, return `Result.Failure("Your organization's account has been suspended. Contact support.")`
3. PLATFORM_ADMIN users bypass the suspension check (they need to access the system to reactivate tenants)
4. Skip the check if `IHttpContextAccessor.HttpContext` is null (background worker)

---

## API Changes

### New Platform Admin Endpoints

**Create Tenant:**
```
POST /api/platform/tenants
Body: { "name": "Acme Mining Corp", "adminEmail": "admin@acme.com" }
Response: { "id": "guid", "name": "...", "status": "ACTIVE", "adminEmail": "...", "createdAt": "..." }
```
- RequirePlatformAdmin policy
- Creates tenant + TENANT_ADMIN user
- Validates: name not empty, adminEmail globally unique across all tenants
- Auto-generates SchemaPrefix from name (lowercase, underscored, truncated to 50 chars, with numeric suffix on collision)
- Implements `IAuditable` (AuditAction: "CreateTenant", EntityType: "Tenant")

**List Tenants:**
```
GET /api/platform/tenants?page=1&pageSize=20
Response: PagedResponse<TenantDto> with { id, name, status, userCount, batchCount, createdAt }
```
- RequirePlatformAdmin policy
- Includes aggregate counts per tenant

**Update Tenant Status:**
```
PATCH /api/platform/tenants/{id}/status
Body: { "status": "SUSPENDED" | "ACTIVE" }
Response: { "id": "guid", "name": "...", "status": "..." }
```
- RequirePlatformAdmin policy
- Cannot suspend the tenant containing the requesting PLATFORM_ADMIN
- Implements `IAuditable` (AuditAction: "UpdateTenantStatus", EntityType: "Tenant")

### Modified Endpoints

**`POST /api/users` (Invite User):**
- Currently: RequireAdmin (PLATFORM_ADMIN only due to existing bypass)
- New: RequireAdmin policy accepts both PLATFORM_ADMIN and TENANT_ADMIN
- Add TENANT_ADMIN to the CreateUser validator's allowed role values
- **Handler-level enforcement:** If the calling user is TENANT_ADMIN and `cmd.Role` is not SUPPLIER or BUYER, return `Result.Failure("You can only assign Supplier or Buyer roles")`
- PLATFORM_ADMIN can assign any role including TENANT_ADMIN

**`PATCH /api/users/{id}` (Update User):**
- Currently: RequireAdmin (PLATFORM_ADMIN only)
- New: RequireAdmin accepts both roles
- **Handler-level enforcement:** If the calling user is TENANT_ADMIN:
  - Reject if `cmd.Role` is PLATFORM_ADMIN or TENANT_ADMIN → `Result.Failure("You cannot assign this role")`
  - Reject if the target user's current role is TENANT_ADMIN or PLATFORM_ADMIN → `Result.Failure("You cannot modify this user")`
  - Reject if the target user is in a different tenant → `Result.Failure("User not found")`
- PLATFORM_ADMIN bypasses all these checks

**`GET /api/users` (List Users):**
- Already filtered by TenantId — no change to base query
- Both PLATFORM_ADMIN and TENANT_ADMIN can access (RequireAdmin policy)
- **Handler-level enforcement:** When the calling user is TENANT_ADMIN, exclude users with role PLATFORM_ADMIN from results

### Modified `/api/me` Auto-Provisioning

**Remove** the fallback that creates a new user as PLATFORM_ADMIN on the first active tenant.

**New flow:**
1. Check if Auth0Sub matches an existing user → return user
2. Extract email from token claims
3. Check if email matches a `pending|` user → link Auth0Sub, return user
4. Check if email matches an existing active user → relink Auth0Sub, return user
5. **No match found:** return 403 with `{ "error": "No account found. Contact your administrator to get access." }`

**Note:** Consider extracting the `/api/me` inline logic from `Program.cs` (~50 lines) into a proper MediatR handler (`Features/Auth/AutoProvision.cs`) consistent with Vertical Slice architecture.

---

## Authorization Policy Changes

### Implementation Details

The existing `RoleAuthorizationHandler` (line 28) has a PLATFORM_ADMIN bypass: `if (user.Role == Roles.Admin || requirement.AllowedRoles.Contains(user.Role))`. This means PLATFORM_ADMIN passes ALL policies. This behavior is kept.

### New Policies

Add to `AuthorizationPolicies.cs`:
- `RequirePlatformAdmin` — `new RoleRequirement(Roles.Admin)` — only PLATFORM_ADMIN (and the existing bypass means this is effectively the same, but it's semantically clear)
- `RequireAdmin` — changed from `new RoleRequirement(Roles.Admin)` to `new RoleRequirement(Roles.Admin, Roles.TenantAdmin)` — allows both roles

Add to `Roles.cs`:
- `public const string TenantAdmin = "TENANT_ADMIN";`

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
When TENANT_ADMIN invokes user management endpoints, the handler must verify the target user belongs to the same tenant. PLATFORM_ADMIN bypasses this check. This enforcement is in the MediatR handlers, not in authorization policies.

---

## Frontend Changes

### TypeScript Type Updates
Update `UserProfile.role` union type in `packages/web/src/app/core/auth/auth.service.ts` to include `'TENANT_ADMIN'`:
```typescript
role: 'SUPPLIER' | 'BUYER' | 'PLATFORM_ADMIN' | 'TENANT_ADMIN';
```

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
- TENANT_ADMIN cannot see PLATFORM_ADMIN users in the user list (API enforces this)

### Login Flow Change
- Remove auto-provisioning fallback
- On 403 from `/api/me`, show: "No account found. Contact your administrator to get access."
- Keep the "Sign in with Google" button — it just won't auto-create accounts anymore

### Route Guards
- Existing `roleGuard('PLATFORM_ADMIN')` on admin routes becomes `roleGuard('PLATFORM_ADMIN', 'TENANT_ADMIN')`
- Add separate route for `/admin/tenants` with `roleGuard('PLATFORM_ADMIN')` only
- Note: the existing `roleGuard` already has a PLATFORM_ADMIN bypass (line 13 of `role.guard.ts`), so PLATFORM_ADMIN always passes all guards

---

## Tenant Suspension Enforcement

Implemented as a MediatR pipeline behavior, not an exception. See Tenant Lifecycle section above for details.

Pipeline order:
```
Request → ValidationBehaviour → TenantStatusBehaviour → AuditBehaviour → Handler → Response
```

---

## Database Changes

**No new tables. No new migrations.**

- `TenantEntity` already has: Id, Name, SchemaPrefix (unique), Status, CreatedAt
- `UserEntity` already has: Role field (string) — adding "TENANT_ADMIN" as a valid value is a code-only change
- `Roles.cs` constants file gets the new `TenantAdmin` constant

---

## Out of Scope

- Self-service signup (Phase 13)
- Stripe billing (Phase 13)
- Tenant settings/branding
- Tenant-level feature flags
- Cross-tenant data sharing
- Schema-per-tenant isolation
- Tenant deletion (tenants can only be suspended, not deleted)
- Pending user invitation expiration/cleanup

---

## Success Criteria

1. PLATFORM_ADMIN can create a new tenant with a TENANT_ADMIN user
2. Admin email is validated as globally unique across all tenants
3. SchemaPrefix collisions are handled with numeric suffixes
4. TENANT_ADMIN can invite SUPPLIER and BUYER users to their tenant
5. TENANT_ADMIN cannot assign PLATFORM_ADMIN or TENANT_ADMIN roles (handler enforcement)
6. TENANT_ADMIN cannot see PLATFORM_ADMIN users in user list (API enforcement)
7. TENANT_ADMIN cannot modify other TENANT_ADMIN or PLATFORM_ADMIN users
8. Suspended tenants return failure via TenantStatusBehaviour (Result pattern, no exceptions)
9. PLATFORM_ADMIN bypasses tenant suspension check
10. New Google logins without a pre-existing account see "Contact your administrator"
11. PLATFORM_ADMIN dashboard shows tenant management page
12. TENANT_ADMIN dashboard shows only Users and Audit Log
13. All existing tenant isolation (row-level filtering) continues to work
14. Existing demo data and Pilot Tenant remain functional
15. Tenant creation and status changes are audit-logged via IAuditable
