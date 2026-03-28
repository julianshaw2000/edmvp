# Auth Upgrade Agent — Auth0 → Microsoft Entra External ID

## Mission
Replace Auth0 with Microsoft Entra External ID across the Tungsten platform codebase.
Stack: Angular 17+ (standalone components) frontend, C# .NET 8 Web API backend, PostgreSQL database.
Add Google social login and self-service password reset via Entra configuration.

## What You Are Doing
This is a targeted auth swap — not a feature build. Touch only auth-related files.
Do not refactor unrelated code. Do not change business logic, compliance rules, or data models
beyond the single column rename specified below.

## Completed Reference Files
The following files have already been written and are in /packages/auth-reference/.
Read them before writing any code — they are your implementation blueprint:

- app.config.ts          — MSAL providers replacing Auth0 AuthModule
- app.component.ts       — handleRedirectObservable() + active account setup
- app.routes.ts          — MsalGuard replacing AuthGuard on all protected routes
- auth.service.ts        — login(), logout(), resetPassword(), getProfile() wrappers
- role.guard.ts          — RoleGuard using getProfile() for role-based routing
- environment.ts         — MSAL config shape (dev)
- environment.prod.ts    — MSAL config shape (prod)
- Program.cs             — AddMicrosoftIdentityWebApiAuthentication() setup
- appsettings.json       — AzureAd block structure

## Step-by-Step Tasks

### 1. Angular — Remove Auth0

```bash
cd packages/web
npm uninstall @auth0/auth0-angular
npm install @azure/msal-angular @azure/msal-browser
```

Search for and remove ALL imports of `@auth0/auth0-angular` across the codebase:
```bash
grep -r "auth0" src/ --include="*.ts" -l
```
For each file found: remove the import, replace the usage with the MSAL equivalent
from the reference files. Do not leave any Auth0 imports or references.

### 2. Angular — Environment Files

Update `src/environments/environment.ts` and `src/environments/environment.prod.ts`
to match the reference file structure exactly. Replace placeholder values with:
- `MSAL_CLIENT_ID` → from Render environment variable `MSAL_CLIENT_ID`
- `MSAL_AUTHORITY` → from Render environment variable `MSAL_AUTHORITY`
- `MSAL_REDIRECT_URI` → from Render environment variable `MSAL_REDIRECT_URI`
- `API_CLIENT_ID` → from Render environment variable `API_CLIENT_ID`
- `API_BASE_URL` → from Render environment variable `API_BASE_URL`

### 3. Angular — App Config

Replace the contents of `src/app/app.config.ts` with the reference implementation.
Key points:
- `msalInstanceFactory()` must set `knownAuthorities` to the CIAM hostname
- Cache location must be `SessionStorage` (not localStorage)
- `MsalInterceptor` must be registered as an HTTP_INTERCEPTORS provider
- `protectedResourceMap` must cover the full API base URL

### 4. Angular — App Component

Update `src/app/app.component.ts`:
- Add `MsalService` and `MsalBroadcastService` to constructor
- Add `handleRedirectObservable().subscribe()` in `ngOnInit()`
- Add active account resolution on `InteractionStatus.None`
- Add `LOGIN_SUCCESS` event handler to set active account
- Add `OnDestroy` with `destroying$` Subject to clean up subscriptions

### 5. Angular — Routes

Update `src/app/app.routes.ts`:
- Replace `AuthGuard` import with `MsalGuard` from `@azure/msal-angular`
- Apply `MsalGuard` to supplier, buyer, and admin route groups
- Keep `RoleGuard` alongside `MsalGuard` in `canActivate` arrays
- Ensure the public `/verify/:batchId` route has NO guards
- Add `/login-failed` route pointing to `LoginFailedComponent`

### 6. Angular — Auth Service

Create `src/app/core/auth/auth.service.ts` from the reference file.
This service wraps MsalService and exposes:
- `login()` — loginRedirect with API scopes
- `logout()` — logoutRedirect with postLogoutRedirectUri
- `resetPassword()` — loginRedirect with SSPR authority
- `getProfile()` — HTTP GET /api/auth/me, returns UserProfile
- `isLoggedIn()` — checks for active accounts

### 7. Angular — Role Guard

Create `src/app/core/guards/role.guard.ts` from the reference file.
This guard calls `authService.getProfile()` and redirects to the correct
portal if the user's role doesn't match `route.data['requiredRole']`.
This is client-side UX only — the API enforces roles independently.

### 8. C# API — NuGet

```bash
cd packages/api
dotnet add package Microsoft.Identity.Web
dotnet remove package <any Auth0 JWT packages if present>
```

Search for Auth0 references:
```bash
grep -r "Auth0\|auth0" . --include="*.cs" --include="*.json" -l
```

### 9. C# API — Program.cs

Replace JWT bearer configuration in `Program.cs`.
Remove any manual `AddJwtBearer()` pointing at Auth0.
Add `builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAd")`.
Ensure `app.UseAuthentication()` comes before `app.UseAuthorization()` in the pipeline.
Do not change CORS policy, controller mappings, or any other middleware.

### 10. C# API — appsettings.json

Add the `AzureAd` block to `appsettings.json`:
```json
"AzureAd": {
  "Instance": "https://<TENANT_SUBDOMAIN>.ciamlogin.com/",
  "TenantId": "<TENANT_ID>",
  "ClientId": "<API_CLIENT_ID>",
  "Audience": "api://<API_CLIENT_ID>"
}
```
Do NOT commit real values. Use placeholder strings — values come from Render secrets.
Add the corresponding keys to `appsettings.Development.json` pointing at dev tenant.

### 11. C# API — MeController / User Provisioning

Find the existing `/api/auth/me` endpoint (or create it if absent).
Update identity resolution: replace `auth0_sub` claim lookup with `oid` claim:

```csharp
// Before
var sub = User.FindFirst("sub")?.Value;

// After
var oid = User.FindFirst("oid")?.Value
    ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
```

The `/me` endpoint must handle first-time Google-authenticated users:
- Look up user by `entra_oid` in the users table
- If not found AND the user authenticated via Google (check `idp` claim):
  - Create a new user record with role = null and active = false
  - Return HTTP 403 with body `{ "status": "pending_activation" }`
  - Platform Admin must activate the user before they can access portals
- If not found AND email/password user: return HTTP 404 (invitation flow)
- If found and active: return full UserProfile

### 12. Database Migration

Run this migration script against PostgreSQL:
```sql
-- Rename auth0_sub to entra_oid
ALTER TABLE users RENAME COLUMN auth0_sub TO entra_oid;

-- Ensure index exists on new column name
DROP INDEX IF EXISTS idx_users_auth0_sub;
CREATE INDEX IF NOT EXISTS idx_users_entra_oid ON users(entra_oid);
```

After running, verify:
```sql
SELECT column_name FROM information_schema.columns
WHERE table_name = 'users' AND column_name = 'entra_oid';
```

## Verification Checklist

Before marking this task complete, confirm ALL of the following:

- [ ] `grep -r "auth0" packages/web/src --include="*.ts"` returns zero results
- [ ] `grep -r "auth0" packages/api --include="*.cs"` returns zero results
- [ ] `ng build` completes without errors
- [ ] `dotnet build` completes without errors
- [ ] No `@auth0/auth0-angular` in `packages/web/package.json`
- [ ] `Microsoft.Identity.Web` present in `packages/api/*.csproj`
- [ ] `users` table has `entra_oid` column, no `auth0_sub` column
- [ ] `/api/auth/me` endpoint resolves OID claim correctly
- [ ] Google-authenticated first-time users return 403 pending_activation

## Constraints

- Do not change any compliance rule logic, custody event code, or document generation code
- Do not change database schema beyond the column rename
- Do not change the CORS policy or any non-auth middleware
- Do not introduce any new npm or NuGet packages beyond those listed above
- Session storage must remain `SessionStorage` — never `localStorage`
- PII logging must remain disabled in MSAL config
- The public `/verify/:batchId` route must remain unauthenticated

## Placeholders to Substitute

All `<PLACEHOLDER>` values come from Render secret environment variables.
Do not hardcode any tenant IDs, client IDs, or secrets in source files.
If a value is needed at build time (Angular environments), read from `process.env`
or the CI/CD pipeline variable injection pattern already in use.