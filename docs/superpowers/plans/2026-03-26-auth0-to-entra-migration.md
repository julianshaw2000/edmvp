# Auth0 → Microsoft Entra External ID Migration Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace Auth0 with Microsoft Entra External ID for authentication across Angular frontend and .NET API backend.

**Architecture:** MSAL (Microsoft Authentication Library) replaces Auth0 SDK on frontend. Microsoft.Identity.Web replaces JWT bearer auth on backend. Database column `Auth0Sub` renamed to `EntraOid`. All business logic unchanged.

**Tech Stack:** Angular 21 + @azure/msal-angular, .NET 10 + Microsoft.Identity.Web, PostgreSQL

**Reference files:** `packages/auth-reference/` (already written)

---

## Prerequisites (Manual — Before Code Changes)

### Entra External ID Setup
- [ ] Create Microsoft Entra External ID tenant at entra.microsoft.com
- [ ] Register SPA application (Angular frontend)
  - Redirect URIs: `https://auditraks.com/login`, `http://localhost:4200`
  - Platform: Single-page application
  - Note the **Client ID** and **Authority** (e.g., `https://auditraks.ciamlogin.com/`)
- [ ] Register API application (.NET backend)
  - Expose an API with scope `api://<API_CLIENT_ID>/access`
  - Note the **API Client ID**
- [ ] Configure Google as external identity provider
- [ ] Enable self-service password reset (SSPR)
- [ ] Note all values for Render env vars:
  - `MSAL_CLIENT_ID`
  - `MSAL_AUTHORITY`
  - `MSAL_REDIRECT_URI`
  - `API_CLIENT_ID`

---

## Chunk 1: Angular — Remove Auth0, Add MSAL

### Task 1: Install MSAL, Remove Auth0

**Files:**
- Modify: `packages/web/package.json`

- [ ] **Step 1: Uninstall Auth0**
```bash
cd packages/web
npm uninstall @auth0/auth0-angular
```

- [ ] **Step 2: Install MSAL**
```bash
npm install @azure/msal-angular @azure/msal-browser
```

- [ ] **Step 3: Verify package.json**
- No `@auth0/auth0-angular` in dependencies
- `@azure/msal-angular` and `@azure/msal-browser` present

- [ ] **Step 4: Commit**
```bash
git add packages/web/package.json packages/web/package-lock.json
git commit -m "chore: replace Auth0 with MSAL packages"
```

---

### Task 2: Update Environment Files

**Files:**
- Modify: `packages/web/src/environments/environment.ts`
- Modify: `packages/web/src/environments/environment.production.ts`

- [ ] **Step 1: Update environment.ts**
Replace auth0 config with MSAL config:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',
  msal: {
    clientId: 'MSAL_CLIENT_ID',
    authority: 'https://auditraks.ciamlogin.com/',
    redirectUri: 'http://localhost:4200',
    apiClientId: 'API_CLIENT_ID',
    apiScopes: ['api://API_CLIENT_ID/access'],
  },
};
```

- [ ] **Step 2: Update environment.production.ts**
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://accutrac-api.onrender.com',
  msal: {
    clientId: 'MSAL_CLIENT_ID',
    authority: 'https://auditraks.ciamlogin.com/',
    redirectUri: 'https://auditraks.com',
    apiClientId: 'API_CLIENT_ID',
    apiScopes: ['api://API_CLIENT_ID/access'],
  },
};
```

- [ ] **Step 3: Commit**
```bash
git commit -m "feat: update environment files with MSAL config"
```

---

### Task 3: Replace App Config (Auth0 → MSAL)

**Files:**
- Modify: `packages/web/src/app/app.config.ts`
- Reference: `packages/auth-reference/app.config.ts`

- [ ] **Step 1: Read reference file**
Read `packages/auth-reference/app.config.ts` for the exact MSAL provider setup.

- [ ] **Step 2: Replace app.config.ts**
Key changes:
- Remove `provideAuth0()`
- Add MSAL providers: `MsalModule`, `MsalInterceptor`, `MsalGuard`, `MsalRedirectComponent`
- Set `cacheLocation: 'sessionStorage'` (not localStorage)
- Configure `protectedResourceMap` for the API URL
- Set `knownAuthorities` to the CIAM hostname

- [ ] **Step 3: Remove Auth0 auth interceptor**
Delete or empty `packages/web/src/app/core/auth/auth.interceptor.ts` — MSAL's `MsalInterceptor` replaces it.

- [ ] **Step 4: Commit**
```bash
git commit -m "feat: replace Auth0 providers with MSAL in app.config.ts"
```

---

### Task 4: Update Auth Service

**Files:**
- Modify: `packages/web/src/app/core/auth/auth.service.ts`
- Reference: `packages/auth-reference/auth.service.ts`

- [ ] **Step 1: Read reference file**
Read `packages/auth-reference/auth.service.ts`.

- [ ] **Step 2: Rewrite auth.service.ts**
Key changes:
- Remove `AuthService as Auth0Service` import
- Import `MsalService` from `@azure/msal-angular`
- `login()` → `loginRedirect()` with API scopes
- `logout()` → `logoutRedirect()` with postLogoutRedirectUri
- Add `resetPassword()` → `loginRedirect()` with SSPR authority
- `isAuthenticated` → check `getAllAccounts().length > 0`
- `loadProfile()` → HTTP GET `/api/me` (same as before)
- Update `UserProfile` interface — keep existing fields

- [ ] **Step 3: Commit**
```bash
git commit -m "feat: replace Auth0 auth service with MSAL wrapper"
```

---

### Task 5: Update Route Guards

**Files:**
- Modify: `packages/web/src/app/app.routes.ts`
- Modify: `packages/web/src/app/core/auth/auth.guard.ts`
- Modify: `packages/web/src/app/core/auth/role.guard.ts`
- Reference: `packages/auth-reference/app.routes.ts`, `packages/auth-reference/role.guard.ts`

- [ ] **Step 1: Replace auth.guard.ts with MsalGuard**
The Auth0 auth guard is no longer needed — `MsalGuard` from `@azure/msal-angular` replaces it.
Either delete `auth.guard.ts` or make it a thin wrapper around `MsalGuard`.

- [ ] **Step 2: Update role.guard.ts**
Read reference `packages/auth-reference/role.guard.ts`.
Update to use the new `AuthService.getProfile()` pattern.

- [ ] **Step 3: Update app.routes.ts**
- Replace `authGuard` with `MsalGuard` on all protected routes
- Keep `roleGuard` alongside `MsalGuard`
- Ensure public routes (`/`, `/signup`, `/signup/success`, `/verify/:batchId`) have no guards
- Add `/login-failed` route

- [ ] **Step 4: Commit**
```bash
git commit -m "feat: replace Auth0 guards with MsalGuard"
```

---

### Task 6: Update Login Component + App Component

**Files:**
- Modify: `packages/web/src/app/features/auth/login.component.ts`
- Modify: `packages/web/src/app/app.component.ts` (or `app.ts`)
- Reference: `packages/auth-reference/app.component.ts`

- [ ] **Step 1: Update app component**
Add MSAL redirect handling:
- `handleRedirectObservable().subscribe()` in `ngOnInit()`
- Active account resolution on `InteractionStatus.None`
- `LOGIN_SUCCESS` event handler
- `OnDestroy` with `destroying$` Subject

- [ ] **Step 2: Update login component**
The login component already auto-redirects to Auth0. Change it to auto-redirect via MSAL:
```typescript
this.authService.login(); // Now calls MSAL loginRedirect
```

- [ ] **Step 3: Search and remove ALL remaining Auth0 references**
```bash
grep -r "auth0\|Auth0" packages/web/src --include="*.ts" -l
```
Fix every file found.

- [ ] **Step 4: Build**
```bash
cd packages/web && npx ng build
```

- [ ] **Step 5: Commit**
```bash
git commit -m "feat: complete Angular migration from Auth0 to MSAL"
```

---

## Chunk 2: .NET API — Remove Auth0, Add Entra

### Task 7: Install Microsoft.Identity.Web, Remove Auth0 JWT

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Tungsten.Api.csproj`

- [ ] **Step 1: Install package**
```bash
cd packages/api/src/Tungsten.Api
dotnet add package Microsoft.Identity.Web
```

- [ ] **Step 2: Commit**
```bash
git commit -m "chore: add Microsoft.Identity.Web package"
```

---

### Task 8: Update Program.cs — Replace JWT Bearer

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Program.cs`
- Modify: `packages/api/src/Tungsten.Api/appsettings.json`
- Modify: `packages/api/src/Tungsten.Api/appsettings.Development.json`
- Reference: `packages/auth-reference/Program.cs`, `packages/auth-reference/appsettings.json`

- [ ] **Step 1: Update appsettings.json**
Add AzureAd block with placeholder values:
```json
"AzureAd": {
  "Instance": "https://TENANT_SUBDOMAIN.ciamlogin.com/",
  "TenantId": "TENANT_ID",
  "ClientId": "API_CLIENT_ID",
  "Audience": "api://API_CLIENT_ID"
}
```

- [ ] **Step 2: Replace JWT bearer in Program.cs**
Remove the Auth0 `AddJwtBearer()` configuration.
Add:
```csharp
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd");
```
Remove Auth0-specific using statements.

- [ ] **Step 3: Commit**
```bash
git commit -m "feat: replace Auth0 JWT bearer with Microsoft Identity Web"
```

---

### Task 9: Update CurrentUserService — OID Claim

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Auth/CurrentUserService.cs`

- [ ] **Step 1: Update Auth0Sub property to use OID claim**
```csharp
// Before
public string Auth0Sub =>
    httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
    ?? throw new UnauthorizedAccessException("No authenticated user");

// After
public string EntraOid =>
    httpContextAccessor.HttpContext?.User.FindFirst("oid")?.Value
    ?? httpContextAccessor.HttpContext?.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
    ?? throw new UnauthorizedAccessException("No authenticated user");
```

- [ ] **Step 2: Update ResolveUserAsync**
Change `u.Auth0Sub == sub` to `u.EntraOid == oid` (after entity rename).

- [ ] **Step 3: Update ICurrentUserService interface**
Rename `Auth0Sub` to `EntraOid`.

- [ ] **Step 4: Search and fix all references**
```bash
grep -r "Auth0Sub\|auth0Sub\|Auth0\|auth0" packages/api --include="*.cs" -l
```
Update every file found.

- [ ] **Step 5: Commit**
```bash
git commit -m "feat: replace Auth0Sub with EntraOid in CurrentUserService"
```

---

### Task 10: Update UserEntity — Rename Column

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Entities/UserEntity.cs`
- Modify: All files referencing `Auth0Sub`

- [ ] **Step 1: Rename property in UserEntity**
```csharp
// Before
public required string Auth0Sub { get; set; }

// After
public required string EntraOid { get; set; }
```

- [ ] **Step 2: Update all references**
Search and replace `Auth0Sub` → `EntraOid` across the entire API codebase:
- Program.cs (/api/me endpoint)
- CreateUser.cs (pending| prefix logic)
- CreateTenant.cs
- StripeWebhookHandler.cs
- SeedData.cs
- ApiKeyMiddleware.cs
- GetMe.cs
- All test files

- [ ] **Step 3: Update EF configuration if needed**
Check if any Fluent API configuration references `Auth0Sub`.

- [ ] **Step 4: Generate migration**
```bash
dotnet ef migrations add RenameAuth0SubToEntraOid --project packages/api/src/Tungsten.Api
```

- [ ] **Step 5: Build and test**
```bash
cd packages/api && dotnet build && dotnet test
```

- [ ] **Step 6: Commit**
```bash
git commit -m "feat: rename Auth0Sub to EntraOid across entire codebase"
```

---

### Task 11: Update /api/me Endpoint

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Program.cs` (inline /api/me)

- [ ] **Step 1: Update identity resolution**
Replace all `Auth0Sub` references with `EntraOid`.
Replace `auth0Sub` variable with `entraOid`.
Update pending user prefix from `pending|` to keep same pattern.

- [ ] **Step 2: Add first-time Google user handling**
For users authenticated via Google (check `idp` claim):
- If no matching user by email → return 403 `{ "status": "pending_activation" }`

- [ ] **Step 3: Commit**
```bash
git commit -m "feat: update /api/me to use EntraOid claims"
```

---

## Chunk 3: Database Migration + Verification

### Task 12: Database Column Rename

- [ ] **Step 1: Run SQL migration in Neon**
```sql
ALTER TABLE users RENAME COLUMN "Auth0Sub" TO "EntraOid";
DROP INDEX IF EXISTS "IX_users_Auth0Sub";
CREATE INDEX IF NOT EXISTS "IX_users_EntraOid" ON users("EntraOid");
```

- [ ] **Step 2: Verify**
```sql
SELECT column_name FROM information_schema.columns
WHERE table_name = 'users' AND column_name = 'EntraOid';
```

---

### Task 13: Final Verification

- [ ] **Step 1: Search for Auth0 references in Angular**
```bash
grep -r "auth0" packages/web/src --include="*.ts"
```
Expected: zero results

- [ ] **Step 2: Search for Auth0 references in API**
```bash
grep -r "auth0\|Auth0" packages/api --include="*.cs"
```
Expected: zero results (except possibly comments)

- [ ] **Step 3: Build Angular**
```bash
cd packages/web && npx ng build
```

- [ ] **Step 4: Build and test API**
```bash
cd packages/api && dotnet build && dotnet test
```

- [ ] **Step 5: Commit final cleanup**
```bash
git commit -m "chore: final Auth0 removal verification"
```

---

## Render Environment Variables

After migration, update Render env vars:

### Remove
- `Auth0__Domain`
- `Auth0__Audience`

### Add
- `AzureAd__Instance` — `https://auditraks.ciamlogin.com/`
- `AzureAd__TenantId` — from Entra portal
- `AzureAd__ClientId` — API client ID from Entra
- `AzureAd__Audience` — `api://API_CLIENT_ID`

---

## Rollback Plan

If migration fails:
1. Revert git commits
2. Rename database column back: `ALTER TABLE users RENAME COLUMN "EntraOid" TO "Auth0Sub";`
3. Restore Auth0 env vars on Render
4. Redeploy

---

## Risk Notes

- All existing users will need their `Auth0Sub` values replaced with Entra OIDs
- Existing `pending|` users will need to re-login to link their Entra identity
- Stripe webhook handler creates users with `pending|{guid}` — this pattern stays the same
- API keys and webhook secrets are not affected
- All business logic, compliance checks, and data models are unchanged
