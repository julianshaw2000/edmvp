# auditraks System Administration Manual

**Audience:** Platform Administrator (platform owner)
**Platform:** auditraks SaaS — Tungsten Supply Chain Compliance
**Last updated:** 2026-03-24

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Environment Configuration](#2-environment-configuration)
3. [Deployment](#3-deployment)
4. [Authentication (ASP.NET Core Identity)](#4-authentication-aspnet-core-identity)
5. [Stripe Administration](#5-stripe-administration)
6. [Tenant Management](#6-tenant-management)
7. [Database](#7-database)
8. [Monitoring](#8-monitoring)
9. [Security](#9-security)
10. [Troubleshooting](#10-troubleshooting)
11. [Maintenance Tasks](#11-maintenance-tasks)

---

## 1. Architecture Overview

auditraks is a three-tier SaaS application deployed entirely on managed cloud infrastructure.

### Services

| Service | Technology | Host |
|---|---|---|
| Frontend | Angular 21+ (standalone, signal-based) | Render Static Site |
| API | ASP.NET Core (.NET 10), Minimal APIs, MediatR CQRS | Render Web Service |
| Background Worker | .NET 10 hosted service | Render Background Worker |
| Database | PostgreSQL | Neon (serverless) |
| Auth | ASP.NET Core Identity (self-issued JWT, HS256) | Self-hosted (API) |
| File Storage | Cloudflare R2 (S3-compatible) | Cloudflare |
| Email | Resend | Resend cloud |
| Billing | Stripe Subscriptions + Webhooks | Stripe cloud |
| Error Tracking | Sentry | Sentry cloud |

### Monorepo Layout

```
packages/
  api/      — .NET 10 Web API (Vertical Slice architecture)
  worker/   — Background worker (compliance checks, email retry)
  web/      — Angular 21+ SPA
  shared/   — Domain types, Zod schemas, compliance rule interfaces
```

### Request Flow

1. User authenticates via `POST /api/auth/login` with email and password. The API issues a 15-minute JWT access token (HS256) and sets a 14-day HttpOnly refresh token cookie.
2. Angular includes the JWT in the `Authorization: Bearer <token>` header with every API request.
3. The API validates the JWT (issuer + audience + symmetric key), then resolves the user's role and tenant from the database via the `/api/me` endpoint — never from JWT claims alone.
4. Every API endpoint enforces role requirements server-side independently of any client-side route guard.
5. Write operations are logged by `AuditLoggingMiddleware` (SHA-256 of request body + IdentityUserId).
6. Background worker processes compliance checks and document generation jobs asynchronously.

### Multi-Tenancy Model

Tenancy is row-level: every `BatchEntity`, `UserEntity`, and related record carries a `TenantId` (UUID). All queries filter by the caller's `TenantId`. There is a single PostgreSQL database on Neon with no per-tenant schema separation. The `SchemaPrefix` field on `TenantEntity` is a human-readable slug used for display purposes only.

---

## 2. Environment Configuration

All configuration is injected as environment variables on the Render services. There are no secrets committed to the repository.

### API Service (Render Web Service)

#### Authentication

| Variable | Description | Example |
|---|---|---|
| `Jwt__Key` | Symmetric signing key for JWT tokens (HS256) | A long random string (min 32 characters) |
| `Jwt__Issuer` | JWT issuer claim | `https://accutrac-api.onrender.com` |
| `Jwt__Audience` | JWT audience claim | `https://api.auditraks.com` |

**Note:** The API will fail to start without `Jwt__Key`. This is intentional — there is no dev mode that skips token validation.

#### Database

| Variable | Description | Example |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | Neon PostgreSQL connection string | `Host=ep-xxx.neon.tech;Database=auditraks;Username=...;Password=...;SSL Mode=Require` |

The API enables retry-on-failure (up to 5 retries, 10-second max delay) and a 30-second command timeout to handle Neon cold-start latency.

#### Cloudflare R2 File Storage

| Variable | Description |
|---|---|
| `R2__AccountId` | Cloudflare account ID |
| `R2__AccessKeyId` | R2 API token access key |
| `R2__SecretAccessKey` | R2 API token secret |
| `R2__BucketName` | R2 bucket name for document uploads |

If `R2__AccountId` is absent, the API falls back to `LocalFileStorageService` (writes to disk — development only, not suitable for production).

#### Stripe Billing

| Variable | Description |
|---|---|
| `Stripe__SecretKey` | Stripe secret key (`sk_live_...` in production, `sk_test_...` in test) |
| `Stripe__WebhookSecret` | Webhook signing secret (`whsec_...`) from the Stripe webhook endpoint |
| `Stripe__PriceId` | Stripe Price ID for the **Pro** plan |
| `Stripe__StarterPriceId` | Stripe Price ID for the **Starter** plan |

If `Stripe__SecretKey` is absent, Stripe is not configured and billing endpoints will fail silently.

#### Email

| Variable | Description |
|---|---|
| `Resend__ApiKey` | Resend API key (`re_...`) |
| `Resend__FromEmail` | Sending address (e.g. `noreply@auditraks.com`) |

If `Resend__ApiKey` is absent, the API uses `LogEmailService` which writes emails to the application log rather than sending them.

#### Error Tracking

| Variable | Description |
|---|---|
| `Sentry__Dsn` | Sentry DSN for the API project |

Traces sample rate is fixed at 20% (`TracesSampleRate = 0.2`). If the DSN is absent, Sentry is not initialised and no errors are forwarded.

#### URL and CORS

| Variable | Description | Default |
|---|---|---|
| `BaseUrl` | Public base URL of the frontend (used in email links) | `https://auditraks.com` |
| `App__BaseUrl` | Alias for `BaseUrl` used in some handlers | same |
| `Cors__AllowedOrigins__0` | First allowed CORS origin | `http://localhost:4200` |
| `Cors__AllowedOrigins__1` | Second allowed CORS origin | `https://auditraks.com` |

The API reads `Cors:AllowedOrigins` as a string array from configuration. If the section is absent, it falls back to `["http://localhost:4200", "https://auditraks.com", "https://auditraks.com"]`.

#### CI/CD Secrets (GitHub Actions)

These are GitHub repository secrets, not Render environment variables:

| Secret | Description |
|---|---|
| `RENDER_API_DEPLOY_HOOK` | Render deploy hook URL for the API Web Service |
| `RENDER_WEB_DEPLOY_HOOK` | Render deploy hook URL for the Static Site |

### Frontend (Render Static Site)

The Angular app reads configuration from `packages/web/src/environments/environment.prod.ts` at build time:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://<render-api-service>.onrender.com',
};
```

Authentication is handled entirely by the API (ASP.NET Core Identity). The Angular app calls `POST /api/auth/login` directly — there is no external auth provider or SDK to configure. These values are baked into the build artefact. To change them, update the file and trigger a new deployment.

---

## 3. Deployment

### CI/CD Pipeline

The GitHub Actions workflow at `.github/workflows/ci.yml` runs on every push to `main` and on pull requests targeting `main`.

**Jobs:**

1. **API — Build & Test** (`api` job)
   - `dotnet build` in `packages/api`
   - `dotnet test --no-build`
   - `dotnet format --verify-no-changes` (formatting is a hard gate)

2. **Web — Build** (`web` job)
   - `npm ci` in `packages/web`
   - `npx ng build`

3. **Deploy to Render** (`deploy` job — only on push to `main`, not PRs)
   - Calls `RENDER_API_DEPLOY_HOOK` via HTTP GET to trigger an API redeploy.
   - Calls `RENDER_WEB_DEPLOY_HOOK` via HTTP GET to trigger a web redeploy.
   - The deploy job only runs after both `api` and `web` jobs pass.

### Manual Deployment

To deploy without a code push (e.g. after changing environment variables):

1. In the Render dashboard, navigate to the service.
2. Click **Manual Deploy > Deploy latest commit**.

Or trigger via curl using the deploy hook URL:

```bash
curl -s "https://api.render.com/deploy/srv-XXXX?key=YYYY"
```

### Health Check Endpoints

The API exposes three health check endpoints:

| Endpoint | Purpose |
|---|---|
| `GET /health` | Basic liveness — always returns 200 once Kestrel is up. No checks run. |
| `GET /health/live` | Same as `/health` — for Render's HTTP health check (no check predicates). |
| `GET /health/ready` | Readiness — includes the `migrations` check. Returns `Degraded` while `DatabaseMigrationService` is still running, `Healthy` when complete. |

Configure Render's health check path to `/health/live` so the service is marked healthy as soon as Kestrel starts, without waiting for migrations to complete.

### Database Migrations on Startup

Migrations run automatically in a background hosted service (`DatabaseMigrationService`) so Kestrel starts accepting requests — including health checks — immediately, even before the database is ready.

Sequence:
1. `DatabaseMigrationService.ExecuteAsync` is called on startup.
2. `db.Database.MigrateAsync()` applies any pending EF migrations.
3. A defensive SQL block runs to add `ParentBatchId` to the `batches` table if it is somehow missing (guards against a known migration recording vs. column creation discrepancy).
4. `SeedData.SeedAsync` runs reference data seeds.
5. The platform admin row (`julianshaw2000@gmail.com`) is promoted to `PLATFORM_ADMIN` if it exists with a different role.
6. `SeedData.SeedDemoBatchesIfNeededAsync` adds demo batches. Failures here are non-fatal and logged as warnings.
7. `DatabaseMigrationService.IsReady` is set to `true`. The `/health/ready` endpoint returns `Healthy`.

If migrations fail (step 2), the service throws and Render will restart it.

---

## 4. Authentication (ASP.NET Core Identity)

### Overview

Authentication is self-hosted using ASP.NET Core Identity. There is no external identity provider. Users authenticate with email and password via `POST /api/auth/login`. The API issues:

- A **15-minute JWT access token** (HS256, signed with `Jwt__Key`)
- A **14-day HttpOnly refresh token cookie** (automatically renewed on use)

The Angular app stores the access token in memory and includes it as `Authorization: Bearer <token>` on every API request. When the access token expires, the app calls `POST /api/auth/refresh` to obtain a new one using the refresh cookie.

### JWT Configuration

| Environment Variable | Purpose |
|---|---|
| `Jwt__Key` | Symmetric signing key (HS256). Must be at least 32 characters. The API will fail to start without this. |
| `Jwt__Issuer` | Issuer claim written into and validated on every token. |
| `Jwt__Audience` | Audience claim written into and validated on every token. |

There is no JWKS endpoint — the signing key is symmetric and shared only within the API process.

### Password Reset

Users reset their own passwords via a self-service flow:

1. User clicks **Forgot password?** on the login page.
2. The API sends a password reset email via Resend.
3. The user clicks the link and sets a new password.

No admin intervention is required. If a user claims the reset email never arrived, check the Resend dashboard for delivery status and verify the email address is correct in the platform.

### User Management

The auditraks database is the sole source of truth for users, roles, and tenant membership. Each user has an `IdentityUserId` column that links to the ASP.NET Core Identity user record.

**User lifecycle:**

1. Admin invites a user via the Admin UI. A `UserEntity` is created with `IdentityUserId = "pending-<guid>"`.
2. The platform sends a setup email via Resend with a link to set their password.
3. The user sets their password and logs in.
4. On first login, the `/api/me` endpoint matches the email to the pending user, updates `IdentityUserId` to the real Identity user ID, and returns the user profile.

**To manually promote a user to PLATFORM_ADMIN:**

```sql
UPDATE "Users"
SET "Role" = 'PLATFORM_ADMIN', "UpdatedAt" = now()
WHERE "Email" = 'user@example.com';
```

**To deactivate a user:**

```sql
UPDATE "Users"
SET "IsActive" = false, "UpdatedAt" = now()
WHERE "Email" = 'user@example.com';
```

**If a user's IdentityUserId becomes mislinked:**

Reset it so the user re-links on their next login:

```sql
UPDATE "Users"
SET "IdentityUserId" = 'pending-' || gen_random_uuid(), "UpdatedAt" = now()
WHERE "Email" = 'affected.user@example.com';
```

---

## 5. Stripe Administration

### Plans and Prices

| Plan | Price | Batch Limit | User Limit | Stripe Price ID (test) |
|---|---|---|---|---|
| Starter | $99/month | 50 batches | 5 users | `price_1TEK1zCvOGA4undoEH4fPTVr` |
| Pro | $249/month | Unlimited | Unlimited | `price_1TEEQ1CvOGA4undoCj5R57Yd` |

The Pro plan's trial period is 60 days (set at Stripe Checkout session creation, `TrialPeriodDays = 60`).

Configure the live Price IDs in the API environment variables:
- `Stripe__PriceId` — Pro plan live price ID
- `Stripe__StarterPriceId` — Starter plan live price ID

### Webhook Configuration

The Stripe webhook must be configured to send events to the API:

**Endpoint URL:** `https://<render-api-service>.onrender.com/api/stripe/webhook`

**Events to subscribe:**

| Event | Effect |
|---|---|
| `checkout.session.completed` | Provisions a new tenant, creates admin user, sends welcome email |
| `invoice.paid` | Activates tenant (moves from TRIAL or SUSPENDED to ACTIVE) |
| `invoice.payment_failed` | Suspends tenant, sends payment failure email to tenant admin |
| `customer.subscription.deleted` | Cancels tenant |

The `Stripe__WebhookSecret` (`whsec_...`) must be set on the API service. Stripe signs each webhook event and the API verifies the HMAC signature before processing.

**Test webhook secret (sandbox):** `whsec_7xUxWKZhNVmGLN0s8xbAq1iur5H3equH`

When switching to live mode, create a new live webhook endpoint in the Stripe dashboard and update `Stripe__WebhookSecret` and `Stripe__SecretKey` on Render.

### Customer Portal

The billing portal is exposed to tenant admins at `POST /api/billing/portal`. It creates a Stripe Customer Portal session and returns the URL. Enable the Customer Portal in the Stripe dashboard under Billing > Customer Portal and configure:

- Cancellation: allow customers to cancel subscriptions
- Plan changes: allow upgrades/downgrades between Starter and Pro

### Test vs Production Keys

| Environment | Secret Key Prefix | Publishable Key Prefix |
|---|---|---|
| Test (sandbox) | `sk_test_...` | `pk_test_...` |
| Production (live) | `sk_live_...` | `pk_live_...` |

The publishable key is used only in the Angular frontend for Stripe.js. The secret key is only ever on the API server.

The sandbox account is `Auditraks sandbox`. Current test secret key starts with `sk_test_51TEBTt...`.

### Monitoring Subscriptions

In the Stripe dashboard:

- **Customers:** lists all tenants by Stripe Customer ID. The `StripeCustomerId` on `TenantEntity` maps to this.
- **Subscriptions:** check for past-due or unpaid subscriptions. A subscription moving to `past_due` triggers `invoice.payment_failed`, which suspends the tenant.
- **Events:** inspect webhook delivery. If a webhook event fails delivery, Stripe retries with exponential backoff. You can manually re-send from the Events tab.

---

## 6. Tenant Management

### Tenant Status Lifecycle

```
TRIAL ──── invoice.paid ──────► ACTIVE
  │                                │
  │         invoice.payment_failed │
  └──── (manual) ──────────────► SUSPENDED
                                   │
                            invoice.paid
                                   │
                                   ▼
                                 ACTIVE
                                   │
                       subscription.deleted
                                   │
                                   ▼
                               CANCELLED
```

Status transitions happen automatically via Stripe webhooks. The platform admin can also change status manually.

**Blocked operations for SUSPENDED/CANCELLED tenants:** The `TenantStatusBehaviour` MediatR pipeline behaviour checks tenant status on every command. Commands from SUSPENDED or CANCELLED tenants are rejected with a 403 before the handler runs.

### Creating a Tenant

#### Via Platform Admin Dashboard (recommended)

Navigate to the Platform Admin section in the web UI and use the Create Tenant form. Provide:
- **Organisation name** — the tenant's display name
- **Admin email** — the email address of the first user (will be created as TENANT_ADMIN)

The system generates a URL-safe `SchemaPrefix` from the organisation name (lowercase, non-alphanumeric characters replaced with `_`, max 50 characters, de-duplicated with a numeric suffix if necessary).

#### Via API

```http
POST /api/platform/tenants
Authorization: Bearer <PLATFORM_ADMIN_JWT>
Content-Type: application/json

{
  "name": "Acme Mining Co",
  "adminEmail": "admin@acmemining.com"
}
```

Response (201 Created):

```json
{
  "id": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "name": "Acme Mining Co",
  "status": "ACTIVE",
  "adminEmail": "admin@acmemining.com",
  "createdAt": "2026-03-24T12:00:00Z"
}
```

The admin user is created with `IdentityUserId = "pending-<guid>"` and will link to a real Identity user on first login. A setup email is sent via Resend — the admin clicks the link to set their password.

#### Via Stripe Checkout (self-serve)

A prospective tenant completes the signup flow at `POST /api/signup/checkout`, which creates a Stripe Checkout Session. On `checkout.session.completed`, `StripeWebhookHandler` automatically provisions the tenant and admin user and sends a welcome email.

### Listing Tenants

```http
GET /api/platform/tenants?page=1&pageSize=20
Authorization: Bearer <PLATFORM_ADMIN_JWT>
```

### Suspending a Tenant

```http
PATCH /api/platform/tenants/{id}/status
Authorization: Bearer <PLATFORM_ADMIN_JWT>
Content-Type: application/json

{
  "status": "SUSPENDED"
}
```

Valid status values: `ACTIVE`, `SUSPENDED`, `CANCELLED`.

The API prevents a platform admin from suspending their own tenant.

### Setting Custom Plan Limits

Override the default plan limits directly in the database:

```sql
UPDATE "Tenants"
SET "MaxBatches" = 100, "MaxUsers" = 10
WHERE "Id" = 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx';
```

Set `MaxBatches` or `MaxUsers` to `NULL` for unlimited. Default limits by plan:

| Plan | MaxBatches | MaxUsers |
|---|---|---|
| STARTER | 50 | 5 |
| PRO | NULL (unlimited) | NULL (unlimited) |

---

## 7. Database

### Infrastructure

The database is PostgreSQL hosted on **Neon** (serverless). Neon scales to zero when idle; the API's retry-on-failure configuration handles the cold-start latency.

Connection string format (store in `ConnectionStrings__DefaultConnection`):

```
Host=ep-<branch>.neon.tech;Database=auditraks;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=false
```

### Migrations

EF Core migrations are stored in `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Migrations/`.

Migrations are applied automatically on every startup via `DatabaseMigrationService`. You do not need to run `dotnet ef database update` manually against production.

To create a new migration during development:

```bash
cd packages/api/src/Tungsten.Api
dotnet ef migrations add <MigrationName> --project ../Tungsten.Api.csproj
```

To verify the database is in sync locally:

```bash
dotnet ef database update
```

**Important:** The migration service includes a defensive SQL fix for the `ParentBatchId` column on the `batches` table (a known discrepancy where the migration was recorded but the column was not created in one deployment). This guard is idempotent and safe to leave in place permanently.

### Seed Data

`SeedData.SeedAsync` runs on every startup and is idempotent. It inserts reference data (risk countries, sanctioned entities, etc.) only if those rows do not already exist. No existing data is modified.

`SeedData.SeedDemoBatchesIfNeededAsync` inserts demo batch data for the default tenant. Failures are caught, logged as warnings, and do not prevent startup.

### Backup Strategy

Neon provides automated daily backups with point-in-time recovery (PITR) for the retention window configured on your Neon plan. For critical operations (e.g. bulk data import, schema changes outside EF migrations), take a manual snapshot via the Neon console before proceeding.

To export the full database manually:

```bash
pg_dump "Host=ep-xxx.neon.tech;Database=auditraks;Username=...;Password=...;SSL Mode=Require" \
  -Fc -f auditraks-$(date +%Y%m%d).dump
```

### Key Tables

| Table | Description |
|---|---|
| `Tenants` | One row per organisation. Central to row-level multi-tenancy. |
| `Users` | One row per user. `IdentityUserId` links to the ASP.NET Core Identity user record. |
| `Batches` | Tungsten batches tracked through the custody chain. |
| `CustodyEvents` | Custody transfer events, each with a SHA-256 hash chain. |
| `ComplianceChecks` | Results of RMAP/OECD compliance evaluation per batch. |
| `RmapSmelters` | RMAP smelter list (uploaded by platform admin). |
| `RiskCountries` | Risk country reference data. |
| `SanctionedEntities` | Sanctioned entity reference data. |
| `AuditLogs` | Immutable audit trail of write operations. |
| `ApiKeys` | Hashed API keys for machine-to-machine access. |
| `WebhookEndpoints` | Tenant-configured outbound webhook targets. |
| `Documents` | Document metadata for uploaded supply chain documents. |
| `GeneratedDocuments` | Generated Material Passports and audit dossiers. |
| `Jobs` | Background job records for async operations. |
| `Notifications` | In-app notifications queue. |

---

## 8. Monitoring

### Health Checks

| Endpoint | HTTP Method | Authentication | Notes |
|---|---|---|---|
| `/health` | GET | None | Always healthy once Kestrel is up |
| `/health/live` | GET | None | Identical to `/health` — no check predicates run |
| `/health/ready` | GET | None | Includes `migrations` check — Degraded while migrating |

Use `/health/live` as the Render health check URL. Use `/health/ready` to confirm the service is fully operational after a deploy.

### Sentry Error Tracking

All unhandled exceptions are forwarded to Sentry. Sentry user context is set to the `IdentityUserId` (no PII such as email or name). Traces sample rate is 20%.

To investigate an error:

1. Open the Sentry project for `tungsten-api`.
2. Filter by the relevant release (each deploy creates a new release automatically if Sentry release integration is configured).
3. The `User.Id` field in Sentry corresponds to the `IdentityUserId` in the `Users` table.

### Audit Log

All write operations (POST, PUT, PATCH, DELETE) are logged by `AuditLoggingMiddleware`. Log entries contain:

- Timestamp (UTC, ISO 8601)
- IdentityUserId of the caller
- HTTP method and path
- SHA-256 hash of the request body
- HTTP status code

These entries appear in the Render log stream and can be forwarded to your log aggregator (e.g. Datadog, Papertrail) via Render's log drain feature.

Structured audit log records (for MediatR commands tagged with `IAuditable`) are also written to the `AuditLogs` table and are queryable via:

```http
GET /api/admin/audit-logs?page=1&pageSize=20&action=CreateTenant&from=2026-01-01
Authorization: Bearer <PLATFORM_ADMIN_JWT>
```

Export the audit log as CSV:

```http
GET /api/admin/audit-logs/export?from=2026-01-01&to=2026-03-31
Authorization: Bearer <PLATFORM_ADMIN_JWT>
```

### Render Logs

Access live and historical logs in the Render dashboard under each service > Logs. For structured log aggregation, configure a Render log drain (Settings > Log Drain) to forward to your preferred SIEM or log service.

---

## 9. Security

### JWT Authentication (ASP.NET Core Identity)

All API endpoints except health checks require a valid JWT bearer token. Token validation parameters:

- Issuer must match `Jwt__Issuer`
- Audience must match `Jwt__Audience`
- Signature verified with the symmetric key from `Jwt__Key` (HS256)
- Access token lifetime: 15 minutes. Refresh token (HttpOnly cookie): 14 days.

### API Key Authentication

Machine-to-machine callers may use an API key instead of a JWT. Keys are passed via the `X-API-Key` request header. The API:

1. Checks if the request is not already JWT-authenticated.
2. Computes SHA-256 of the provided key.
3. Looks up the hash in the `ApiKeys` table (only active keys).
4. If found, synthesises a `ClaimsPrincipal` from the key owner's IdentityUserId, allowing all downstream auth logic to function identically.
5. Updates `LastUsedAt` asynchronously (fire-and-forget, non-critical).

API keys are never stored in plaintext — only the SHA-256 hex digest is persisted.

### Role-Based Authorization

There are four roles, enforced by `RoleAuthorizationHandler`:

| Role | Scope |
|---|---|
| `PLATFORM_ADMIN` | Full system access: tenant management, RMAP upload, audit log export, all platform endpoints |
| `TENANT_ADMIN` | Full access within their own tenant: user management, batch management, document generation |
| `SUPPLIER` | Create/edit batches and custody events within their tenant |
| `BUYER` | Read batches and documents within their tenant |

Roles are stored on `UserEntity.Role` in the database. They are not encoded in JWT claims.

### Tenant Isolation

Every data-access query that could return tenant-specific records is filtered by `TenantId`. This is enforced at the application layer. There is no database-level row security policy — the application must be relied upon to filter correctly.

The `TenantAccessHandler` authorization policy prevents any user from accessing another tenant's resources even if they know the resource GUID.

### HMAC Webhook Signing

Stripe webhook events are verified using Stripe's HMAC signature (`Stripe-Signature` header, `whsec_...` secret). Only events that pass signature verification are processed.

Outbound webhooks (from auditraks to tenant-configured endpoints) are signed with a per-endpoint secret generated at registration (`RandomNumberGenerator.GetBytes(32)`, hex-encoded). Tenants receive this secret once at endpoint creation.

### SHA-256 Hash Chains on Custody Events

Each custody event is written with a SHA-256 hash of its content at the time of creation. This creates a tamper-evident audit trail. Any modification to a custody event's data after the fact will cause the hash to no longer match.

### Rate Limiting

The `public` rate limiter policy applies a fixed-window limit of 30 requests per minute. This is applied to public endpoints (e.g. signup) to prevent abuse.

---

## 10. Troubleshooting

### Deployment Fails: API Tests Fail in CI

The CI pipeline runs `dotnet test` and `dotnet format --verify-no-changes`. The deploy job is blocked until both pass.

- **Test failure:** check the GitHub Actions log for the failing test name.
- **Format failure:** run `dotnet format` in `packages/api` locally and commit the changes.

### Health Check Timeout on Render

**Symptom:** Render marks the service as unhealthy during deployment because the health check times out before the service responds.

**Cause:** Database migrations are slow (e.g. large migration or Neon cold start). Kestrel starts before migrations complete, but if the health check pings `/health/ready`, it will return `Degraded` until migrations finish.

**Fix:** Set Render's health check path to `/health/live` (not `/health/ready`). `/health/live` returns 200 as soon as Kestrel is up, regardless of migration state. Use `/health/ready` manually to confirm readiness after a deploy, not as the Render health check path.

### Missing Environment Variables

**Symptom:** API starts but features silently fail (email not sent, files not stored, errors not tracked).

The API is designed to degrade gracefully when optional integrations are absent:
- Missing `Resend__ApiKey` → emails logged only, not sent
- Missing `R2__AccountId` → files stored to local disk (lost on Render restart)
- Missing `Sentry__Dsn` → no error tracking
- Missing `Stripe__SecretKey` → billing endpoints throw exceptions

**Missing `Jwt__Key`** is the most critical — without it, the API will fail to start. This is intentional — there is no fallback mode that skips token validation.

Verify all required environment variables are set in Render under Service > Environment.

### Migration Failures

**Symptom:** API logs show `Database migration failed` and the service restarts in a loop.

**Diagnosis:**

1. Check the Render logs for the specific exception (EF exception, Npgsql error, connection refused).
2. Verify `ConnectionStrings__DefaultConnection` is correct and the Neon project is running.
3. Check Neon console for project status and recent connection logs.

**Common causes:**

- Neon project suspended (free tier inactivity). Resume it in the Neon console.
- Connection string contains a typo or stale password.
- A migration conflicts with existing schema (e.g. adding a NOT NULL column without a default to a populated table). Fix by updating the migration to include a default value or to split it into a nullable addition followed by a backfill.

**To run migrations manually** against production (emergency only):

```bash
cd packages/api/src/Tungsten.Api
ConnectionStrings__DefaultConnection="<neon-connection-string>" dotnet ef database update
```

### Tenant Stuck in TRIAL After Stripe Checkout

**Symptom:** A customer completed checkout but their tenant status remains TRIAL or does not exist.

**Diagnosis:**

1. In Stripe dashboard > Events, find the `checkout.session.completed` event and check delivery status.
2. If the webhook failed, the tenant was not provisioned. Re-send the event from the Stripe dashboard.
3. If the webhook succeeded but the tenant does not exist, check the API logs for errors in `StripeWebhookHandler.HandleCheckoutCompleted`.
4. If the email already existed in the database, the webhook is skipped with a warning log: "Checkout completed but email {Email} already exists, skipping."

**Fix:** If the tenant was not created, create it manually via `POST /api/platform/tenants`, then update the Stripe Customer ID and Subscription ID:

```sql
UPDATE "Tenants"
SET "StripeCustomerId" = 'cus_xxx',
    "StripeSubscriptionId" = 'sub_xxx',
    "Status" = 'ACTIVE',
    "PlanName" = 'PRO'
WHERE "Id" = 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx';
```

### User Cannot Log In ("No account found")

**Symptom:** User sees "No account found. Contact your administrator to get access."

**Cause:** The `/api/me` endpoint found no matching user record for the IdentityUserId or email.

**Diagnosis:**

1. Verify the user exists in the `Users` table with the correct email.
2. Check that `IsActive = true`.
3. Check `IdentityUserId` — if it still starts with `pending-`, the user has not yet completed their first login.

**Fix:** If the user exists but `IdentityUserId` is wrong, reset it:

```sql
UPDATE "Users"
SET "IdentityUserId" = 'pending-' || gen_random_uuid(), "UpdatedAt" = now()
WHERE "Email" = 'user@example.com';
```

---

## 11. Maintenance Tasks

### Uploading RMAP Smelter Lists

The RMAP (Responsible Minerals Assurance Process) smelter list is the reference data used during compliance checking. It must be updated periodically as the RMI publishes new audit results.

**Upload via API:**

```http
POST /api/admin/rmap/upload
Authorization: Bearer <PLATFORM_ADMIN_JWT>
Content-Type: multipart/form-data

[file field: CSV file]
```

**CSV format (required columns in order):**

```
SmelterId,SmelterName,Country,ConformanceStatus,LastAuditDate
W-0001,Example Smelter Ltd,Germany,Conformant,2025-06-15
W-0002,Another Refinery,China,Active,
```

- `SmelterId` — unique identifier, used as the upsert key
- `SmelterName` — display name
- `Country` — ISO country name or code
- `ConformanceStatus` — e.g. `Conformant`, `Active`, `Withdrawn`
- `LastAuditDate` — optional, ISO 8601 date

The handler upserts rows (insert new, update existing by `SmelterId`) and invalidates the in-memory `rmap-smelters` cache immediately so compliance checks pick up the new data without a restart.

**Response:**

```json
{
  "imported": 15,
  "updated": 312,
  "total": 327
}
```

**View current list:**

```http
GET /api/admin/rmap
Authorization: Bearer <PLATFORM_ADMIN_JWT>
```

### Managing Risk Countries and Sanctioned Entities

Risk countries and sanctioned entities are seeded from `SeedData.SeedAsync` on startup. To update them:

1. Update the seed data in `packages/api/src/Tungsten.Api/Infrastructure/Persistence/SeedData.cs`.
2. Deploy. The seed runs on next startup and upserts the reference data.

Alternatively, update directly in the database:

```sql
-- Add a risk country
INSERT INTO "RiskCountries" ("Id", "Name", "IsoCode", "RiskLevel")
VALUES (gen_random_uuid(), 'Country Name', 'XX', 'HIGH');

-- Add a sanctioned entity
INSERT INTO "SanctionedEntities" ("Id", "Name", "Type", "Reason")
VALUES (gen_random_uuid(), 'Entity Name', 'COMPANY', 'OFAC SDN List');
```

### Reviewing Compliance Flags

Compliance checks are stored in `ComplianceChecks`. Each record links a batch to a compliance result with a flag indicating whether it passed or failed RMAP and OECD DDG rules.

Query flagged batches across all tenants (platform admin only):

```http
GET /api/compliance?flagged=true
Authorization: Bearer <PLATFORM_ADMIN_JWT>
```

Or directly in the database:

```sql
SELECT
    b."Id" AS batch_id,
    b."BatchNumber",
    t."Name" AS tenant,
    c."Status",
    c."Flags",
    c."CheckedAt"
FROM "ComplianceChecks" c
JOIN "Batches" b ON b."Id" = c."BatchId"
JOIN "Tenants" t ON t."Id" = b."TenantId"
WHERE c."Status" != 'PASS'
ORDER BY c."CheckedAt" DESC;
```

### Monitoring Background Jobs

Background jobs (compliance evaluation, document generation) are tracked in the `Jobs` table.

```http
GET /api/admin/jobs
Authorization: Bearer <PLATFORM_ADMIN_JWT>
```

Query stuck or failed jobs:

```sql
SELECT
    "Id",
    "Type",
    "Status",
    "CreatedAt",
    "UpdatedAt",
    "Error"
FROM "Jobs"
WHERE "Status" IN ('FAILED', 'PENDING')
ORDER BY "CreatedAt" DESC;
```

Failed jobs should be investigated by checking the Render worker logs for the corresponding job ID. Jobs do not auto-retry — resubmit them via the appropriate API endpoint if needed.

### Rotating API Keys

If an API key is compromised:

1. Identify the key in the `ApiKeys` table by `LastUsedAt` or the caller's `CreatedByUserId`.
2. Deactivate it:

```sql
UPDATE "ApiKeys"
SET "IsActive" = false
WHERE "Id" = 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx';
```

3. The caller must generate a new key via the API Keys UI.

### MCP Admin Server

The auditraks admin MCP server provides AI assistants with platform administration capabilities.

**Authentication:** Email and password (Platform Admin account). The server authenticates via `POST /api/auth/login` and auto-refreshes JWT tokens.

**Available tools (15):**

| Category | Tools |
|----------|-------|
| Tenant management | List tenants, create tenant, update status, delete tenant |
| User management | List users, create/invite user, update user, delete user |
| Analytics | Platform-wide analytics (optional tenant filter) |
| Audit logs | Search with filters (action, entity type, user, date) |
| RMAP data | List all smelters, search by name/ID |
| Batches | List batches (cross-tenant), get details, compliance status |

**Configuration:**

Add to your Claude Desktop or Claude Code MCP settings:

```json
{
  "mcpServers": {
    "auditraks-admin": {
      "command": "node",
      "args": ["packages/mcp/admin-server/dist/admin-server/src/index.js"],
      "env": {
        "AUDITRAKS_EMAIL": "your_admin@email.com",
        "AUDITRAKS_PASSWORD": "your_password",
        "AUDITRAKS_API_URL": "https://accutrac-api.onrender.com"
      }
    }
  }
}
```

**Security note:** The admin MCP server has full platform access. Only configure it on trusted machines. Never share the configuration file containing your credentials.

### Rotating Stripe Webhook Secret

If the webhook secret is rotated in the Stripe dashboard:

1. In Stripe: Developers > Webhooks > select the endpoint > roll the signing secret.
2. In Render: update `Stripe__WebhookSecret` to the new `whsec_...` value.
3. Trigger a manual redeploy so the API picks up the new secret.

---

*End of document. For application-level user guides (supplier portal, buyer portal), see the separate user documentation.*
