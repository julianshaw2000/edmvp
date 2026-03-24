# auditraks — Third-Party Services Guide

**Audience:** Platform administrator
**Last updated:** 2026-03-24
**Platform:** auditraks SaaS — Tungsten Supply Chain Compliance

---

## Overview

Every external service the auditraks platform depends on is listed below. Services marked **Critical** will cause platform downtime or data loss if unavailable. Services marked **Important** degrade functionality but the platform continues running. Services marked **Optional** are fully bypassed when not configured.

| # | Service | Role | Dashboard | Criticality |
|---|---|---|---|---|
| 1 | Render | Hosting (API, frontend, worker) | [render.com](https://render.com) | Critical |
| 2 | Neon | Managed PostgreSQL database | [console.neon.tech](https://console.neon.tech) | Critical |
| 3 | Auth0 | Authentication & JWT issuance | [manage.auth0.com](https://manage.auth0.com) | Critical |
| 4 | Stripe | Subscription billing | [dashboard.stripe.com](https://dashboard.stripe.com) | Critical |
| 5 | Cloudflare | DNS + R2 object storage | [dash.cloudflare.com](https://dash.cloudflare.com) | Critical |
| 6 | Resend | Transactional email delivery | [resend.com/emails](https://resend.com/emails) | Important |
| 7 | Sentry | Error tracking & tracing | [sentry.io](https://sentry.io) | Optional |
| 8 | GitHub | Source control + CI/CD | [github.com/julianshaw2000/edmvp](https://github.com/julianshaw2000/edmvp) | Important |

---

## 1. Render (Hosting)

**Dashboard:** https://render.com
**Criticality:** Critical

### What it does

Render hosts all three runtime tiers of the platform:

| Render Service | Type | Purpose |
|---|---|---|
| `accutrac-api` | Web Service (Docker/dotnet) | ASP.NET Core 10 REST API |
| `accutrac-web` | Static Site | Angular 21+ SPA |
| Background Worker | Background Worker | Compliance checks, document generation, email retry |

### Custom Domain (DNS via Cloudflare)

```
auditraks.com      → accutrac-web.onrender.com   (CNAME @)
www.auditraks.com  → accutrac-web.onrender.com   (CNAME www)
api.auditraks.com  → accutrac-api.onrender.com   (CNAME api)
```

### Health Check Endpoints

Configure Render's HTTP health check to `/health/live`. The API exposes three endpoints:

| Endpoint | Purpose |
|---|---|
| `GET /health` | Basic liveness — 200 as soon as Kestrel starts, no checks |
| `GET /health/live` | Identical to `/health` — use this as the Render health check URL |
| `GET /health/ready` | Readiness — returns `Degraded` while EF migrations are running, `Healthy` when complete |

### CI/CD Deploy Hooks

Deployments are triggered automatically by the GitHub Actions CI/CD pipeline (see Section 8). Manual redeploy via the Render dashboard: **Manual Deploy > Deploy latest commit**, or via curl:

```bash
curl -s "$RENDER_API_DEPLOY_HOOK"
curl -s "$RENDER_WEB_DEPLOY_HOOK"
```

Deploy hook URLs are stored as GitHub Actions secrets (`RENDER_API_DEPLOY_HOOK`, `RENDER_WEB_DEPLOY_HOOK`). They are not committed to the repository.

### Environment Variables

All secrets are injected as environment variables on the Render service — nothing is committed to source. Full variable reference is in `docs/admin-system-manual.md` § 2. Key groups:

- `Auth0__Domain`, `Auth0__Audience`
- `ConnectionStrings__DefaultConnection`
- `R2__AccountId`, `R2__AccessKeyId`, `R2__SecretAccessKey`, `R2__BucketName`
- `Stripe__SecretKey`, `Stripe__WebhookSecret`, `Stripe__PriceId`, `Stripe__StarterPriceId`
- `Resend__ApiKey`, `Resend__FromEmail`
- `Sentry__Dsn`
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`

### Free Tier vs Paid

The Render free tier spins down Web Services after 15 minutes of inactivity (cold starts take ~30 seconds). For production use, upgrade the API to a paid instance type (Starter or above) to eliminate cold starts. The Static Site tier is free with no spin-down. The Background Worker should also use a paid tier to ensure it processes jobs without interruption.

---

## 2. Neon (PostgreSQL Database)

**Dashboard:** https://console.neon.tech
**Criticality:** Critical

### What it does

Neon hosts the single PostgreSQL database for all tenant data. The platform uses row-level multi-tenancy — all tables carry a `TenantId` UUID column; there is no per-tenant schema separation.

### Connection Details

Connection string format (set as `ConnectionStrings__DefaultConnection`):

```
Host=ep-<branch>.neon.tech;Database=auditraks;Username=<user>;Password=<password>;SSL Mode=VerifyFull;Channel Binding=Require
```

The current pooler endpoint is `ep-soft-waterfall-a8j22jj1-pooler.eastus2.azure.neon.tech`. Full connection string (including credentials) is in `docs/neon.secrets`.

### Serverless Driver and Connection Pooling

Neon scales to zero when idle. The API is configured to handle cold-start latency:

- **Retry on failure:** up to 5 retries with a maximum 10-second delay between attempts (`EnableRetryOnFailure`)
- **Command timeout:** 30 seconds
- **Pooler endpoint:** use the `-pooler` hostname variant to route through Neon's PgBouncer connection pool, which is required for serverless workloads

### Backup and Branching

Neon provides:
- Automated daily backups with point-in-time recovery (PITR) — retention window depends on your Neon plan
- **Branching:** create an isolated database branch for staging or testing without copying data

Before bulk data imports or schema operations outside of EF migrations, take a manual snapshot from the Neon console.

Manual export:

```bash
pg_dump "<connection-string>" -Fc -f auditraks-$(date +%Y%m%d).dump
```

### EF Core Migrations

EF Core migrations live in `packages/api/src/Tungsten.Api/Infrastructure/Persistence/Migrations/`. They are applied automatically on startup by `DatabaseMigrationService` — no manual `dotnet ef database update` is required in production.

---

## 3. Auth0 (Authentication)

**Dashboard:** https://manage.auth0.com
**Criticality:** Critical

### What it does

Auth0 issues RS256-signed JWT access tokens. The API validates the JWT on every request (issuer + audience + lifetime). User roles and tenant membership are resolved from the auditraks database, never from JWT claims alone.

### Tenant and Application

| Setting | Value |
|---|---|
| Tenant | `dev-htzakhlu.us.auth0.com` |
| Application name | Tungsten Web (SPA) |
| Client ID | `4tuGZeyEKnK3VzI9fJNb8qFqBrhQIQZ6` |
| API name | auditraks API |
| API audience | `https://api.accutrac.org` |
| Signing algorithm | RS256 |

Full credentials are in `docs/auth0.secrets`.

### SPA Application Settings

| Setting | Value |
|---|---|
| Allowed Callback URLs | `https://auditraks.com, http://localhost:4200` |
| Allowed Logout URLs | `https://auditraks.com, http://localhost:4200` |
| Allowed Web Origins | `https://auditraks.com, http://localhost:4200` |
| ID Token Expiration | `28800` (8 hours) |
| Refresh Token grant | **Disabled** |

### Connections

- **Username-Password-Authentication** — email and password sign-up/sign-in
- **Google OAuth2** (`google-oauth2`) — social login via Google

### Post Login Action

A Post Login Action must be deployed to the Login flow. It adds `email` and `name` as both standard claims and namespaced custom claims (`https://auditraks.com/email`, `https://auditraks.com/name`) to the ID token and access token. Without this Action, `/api/me` cannot resolve the user's email from the JWT, and login will fail.

The Action code is documented in `docs/admin-system-manual.md` § 4.

### Token Configuration

- No refresh tokens (grant type disabled)
- 8-hour ID token expiry (28800 seconds)
- RS256 signing — the API fetches the public key from the Auth0 JWKS endpoint automatically

### API Environment Variables

| Variable | Value |
|---|---|
| `Auth0__Domain` | `dev-htzakhlu.us.auth0.com` |
| `Auth0__Audience` | `https://api.accutrac.org` |

**Warning:** If `Auth0__Domain` is absent, the API starts in dev mode with no token validation. Never deploy to production without this variable set.

---

## 4. Stripe (Billing)

**Dashboard:** https://dashboard.stripe.com
**Criticality:** Critical

### What it does

Stripe handles the full subscription billing lifecycle: checkout, recurring invoicing, payment failure handling, and the customer self-service portal.

### Account

**Account name:** Auditraks sandbox (currently test mode)

### Product and Prices

| Plan | Price | Batch Limit | User Limit | Stripe Price ID (test) |
|---|---|---|---|---|
| Starter | $99/month | 50 batches | 5 users | `price_1TEK1zCvOGA4undoEH4fPTVr` |
| Pro | $249/month | Unlimited | Unlimited | `price_1TEEQ1CvOGA4undoCj5R57Yd` |

**Product ID:** `prod_UCdhuviYCoWTsy`
The Pro plan trial period is 60 days (`TrialPeriodDays = 60` set at checkout session creation).

### Webhook Configuration

**Endpoint ID:** `we_1TEEQUCvOGA4undoPhY3VSyn`
**Endpoint URL:** `https://accutrac-api.onrender.com/api/stripe/webhook`
**Signing secret (test):** `whsec_7xUxWKZhNVmGLN0s8xbAq1iur5H3equH`

The API verifies the HMAC signature on every inbound webhook event before processing. `Stripe__WebhookSecret` must be set.

**Subscribed events:**

| Event | Effect |
|---|---|
| `checkout.session.completed` | Provisions new tenant, creates admin user, sends welcome email |
| `invoice.paid` | Activates tenant (TRIAL or SUSPENDED → ACTIVE) |
| `invoice.payment_failed` | Suspends tenant, sends payment failure email |
| `customer.subscription.deleted` | Cancels tenant |

### API Environment Variables

| Variable | Description |
|---|---|
| `Stripe__SecretKey` | `sk_test_...` (test) or `sk_live_...` (production) |
| `Stripe__WebhookSecret` | `whsec_...` signing secret from the webhook endpoint |
| `Stripe__PriceId` | Pro plan Price ID |
| `Stripe__StarterPriceId` | Starter plan Price ID |

Full keys are in `docs/stripe.secrets`. The publishable key (`pk_test_...`) is used in the Angular frontend only.

### Customer Portal

The billing portal is available to tenant admins at `POST /api/billing/portal`. Enable it in the Stripe dashboard under **Billing > Customer Portal** and configure:

- Cancellation allowed
- Plan changes (upgrades and downgrades between Starter and Pro)
- Invoice history
- Payment method updates

### Test Card

Use `4242 4242 4242 4242` (any future expiry, any CVC) for test payments.

### Switching to Production

1. Create a live webhook endpoint in the Stripe dashboard pointing to the same URL.
2. Update `Stripe__SecretKey` to the `sk_live_...` key.
3. Update `Stripe__WebhookSecret` to the new live endpoint's signing secret.
4. Update `Stripe__PriceId` and `Stripe__StarterPriceId` to the live Price IDs.
5. Update the Angular environment to use the live publishable key.

---

## 5. Cloudflare (DNS + R2 Storage)

**Dashboard:** https://dash.cloudflare.com
**Criticality:** Critical

### What it does

Cloudflare serves two functions:

1. **DNS management** for the `auditraks.com` domain, with CNAME records pointing to Render services.
2. **R2 object storage** for uploaded supply chain documents and generated Material Passport PDFs.

### DNS Records

Defined in `docs/cloudflare-dns-import.txt`:

```
@    CNAME  accutrac-web.onrender.com   (auditraks.com root)
www  CNAME  accutrac-web.onrender.com
api  CNAME  accutrac-api.onrender.com
```

### R2 Storage

**Bucket name:** `accutrac`
**Bucket endpoint:** `https://5418040d79f34e5e0b21cd4b6389adde.r2.cloudflarestorage.com/accutrac`
**Account ID:** extracted from the endpoint above (`5418040d79f34e5e0b21cd4b6389adde`). Full endpoint is in `docs/cloudfare.secrets`.

R2 is S3-compatible. The API uses `R2FileStorageService` when `R2__AccountId` is configured. When it is absent, `LocalFileStorageService` is used instead (writes to local disk — development only, not suitable for production).

### API Environment Variables

| Variable | Description |
|---|---|
| `R2__AccountId` | Cloudflare account ID |
| `R2__AccessKeyId` | R2 API token access key |
| `R2__SecretAccessKey` | R2 API token secret |
| `R2__BucketName` | Bucket name (e.g. `accutrac`) |

R2 API tokens are created in the Cloudflare dashboard under **R2 > Manage API Tokens**.

### Stored Content

- Material Passport PDFs (generated by the background worker)
- Uploaded supply chain documents (COO, smelter declarations, etc.)
- Audit dossier packages

---

## 6. Resend (Email)

**Dashboard:** https://resend.com/emails
**Criticality:** Important

### What it does

Resend delivers all transactional email from the platform. It is a graceful dependency: when the API key is absent, the platform falls back to `LogEmailService`, which writes email content to the application log rather than sending it.

### Configuration

**From address:** `noreply@auditraks.com`
**API key:** stored in `docs/resend.secrets` (prefix `re_...`)

### Email Types

| Email | Trigger |
|---|---|
| Welcome | New tenant provisioned via Stripe checkout |
| Trial ending warning | Approaching end of 60-day trial |
| Payment failed | `invoice.payment_failed` webhook from Stripe |

### Domain Authentication

The sending domain `auditraks.com` must be authenticated in the Resend dashboard (**Domains > Add Domain**) by adding the DNS records Resend provides to Cloudflare. Without domain authentication, emails may be flagged as spam or blocked.

### API Environment Variables

| Variable | Description |
|---|---|
| `Resend__ApiKey` | Resend API key (`re_...`) |
| `Resend__FromEmail` | From address (e.g. `noreply@auditraks.com`) |

### Fallback Behaviour

If `Resend__ApiKey` is absent, `LogEmailService` is registered. No emails are sent; instead, the email content is written to the structured application log. This is suitable for local development and CI but not for production.

---

## 7. Sentry (Error Tracking)

**Dashboard:** https://sentry.io
**Criticality:** Optional

### What it does

Sentry captures unhandled exceptions from the API and forwards them with stack traces and request context. It also records distributed traces at a 20% sample rate.

### Integration

Sentry is integrated via `builder.WebHost.UseSentry()` in `Program.cs`. It is only initialised when `Sentry__Dsn` is set. If the DSN is absent, no SDK is loaded and there is no performance overhead.

### Configuration

| Setting | Value |
|---|---|
| Traces sample rate | `0.2` (20% of requests) |
| User context | Auth0 `sub` identifier only — no email or name (no PII) |

### API Environment Variable

| Variable | Description |
|---|---|
| `Sentry__Dsn` | Sentry DSN for the API project (from Sentry project settings) |

### Status

Sentry is integrated in the codebase. Whether a DSN has been provisioned and set on Render should be verified in the Render environment variable panel.

---

## 8. GitHub (Source Control + CI/CD)

**Dashboard:** https://github.com/julianshaw2000/edmvp
**Criticality:** Important

### What it does

GitHub hosts the source code and runs the CI/CD pipeline via GitHub Actions on every push to `main` and on all pull requests targeting `main`.

### CI/CD Pipeline

Workflow file: `.github/workflows/ci.yml`

**Jobs:**

| Job | Runs on | Steps |
|---|---|---|
| `api` — API Build & Test | ubuntu-latest | `dotnet build`, `dotnet test --no-build`, `dotnet format --verify-no-changes` |
| `web` — Web Build | ubuntu-latest | `npm ci`, `npx ng build` |
| `deploy` — Deploy to Render | ubuntu-latest (push to `main` only) | curl `RENDER_API_DEPLOY_HOOK`, curl `RENDER_WEB_DEPLOY_HOOK` |

The `deploy` job only runs after both `api` and `web` jobs pass. Formatting is a hard gate — CI fails if `dotnet format` would make any changes.

### Required GitHub Secrets

Set these in **GitHub > Repository > Settings > Secrets and variables > Actions**:

| Secret | Description |
|---|---|
| `RENDER_API_DEPLOY_HOOK` | Render deploy hook URL for the API Web Service |
| `RENDER_WEB_DEPLOY_HOOK` | Render deploy hook URL for the Render Static Site |

---

## Service Dependencies Diagram

```
Browser
  │
  ├── Auth0 (JWT issuance, Google OAuth, user sign-in)
  │
  ├── Cloudflare DNS
  │         │
  │         ▼
  │   Render Static Site (Angular SPA)
  │         │
  │         └── Auth0 SDK (token validation client-side)
  │
  └── Cloudflare DNS
            │
            ▼
      Render Web Service (ASP.NET Core API)
            │
            ├── Neon PostgreSQL (all application data)
            │
            ├── Auth0 (JWT validation — JWKS endpoint)
            │
            ├── Cloudflare R2 (document storage)
            │
            ├── Stripe (billing webhooks, checkout sessions)
            │
            ├── Resend (transactional email)
            │
            └── Sentry (error reporting)

GitHub Actions
  └── on push to main → Render deploy hooks → Render (API + Web)
```

---

## Credentials Reference

All credential files are in `docs/` and are gitignored. Do not commit them.

| File | Service | Contents |
|---|---|---|
| `docs/auth0.secrets` | Auth0 | Domain, Client ID, Audience |
| `docs/neon.secrets` | Neon | PostgreSQL connection string |
| `docs/cloudfare.secrets` | Cloudflare R2 | Bucket endpoint URL (includes Account ID) |
| `docs/stripe.secrets` | Stripe | Publishable key, secret key, restricted key, product/price IDs, webhook ID and secret |
| `docs/resend.secrets` | Resend | API key |

### Render Environment Variable Mapping

| Render Variable | Source Secret | Service |
|---|---|---|
| `Auth0__Domain` | `docs/auth0.secrets` → Domain | Auth0 |
| `Auth0__Audience` | `docs/auth0.secrets` → Audience | Auth0 |
| `ConnectionStrings__DefaultConnection` | `docs/neon.secrets` | Neon |
| `R2__AccountId` | `docs/cloudfare.secrets` (from URL) | Cloudflare R2 |
| `R2__AccessKeyId` | Cloudflare dashboard — R2 API token | Cloudflare R2 |
| `R2__SecretAccessKey` | Cloudflare dashboard — R2 API token | Cloudflare R2 |
| `R2__BucketName` | `accutrac` | Cloudflare R2 |
| `Stripe__SecretKey` | `docs/stripe.secrets` → `STRIPE_SECRET_KEY` | Stripe |
| `Stripe__WebhookSecret` | `docs/stripe.secrets` → `STRIPE_WEBHOOK_SECRET` | Stripe |
| `Stripe__PriceId` | `docs/stripe.secrets` → `STRIPE_PRICE_ID` (Pro) | Stripe |
| `Stripe__StarterPriceId` | `docs/stripe.secrets` → `STRIPE_PRICE_ID` (Starter) | Stripe |
| `Resend__ApiKey` | `docs/resend.secrets` | Resend |
| `Resend__FromEmail` | `noreply@auditraks.com` | Resend |
| `Sentry__Dsn` | Sentry project settings | Sentry |

---

## Service Costs

Estimated monthly costs at different scales. All figures are approximate and in USD.

### Development / Pilot (current)

| Service | Tier | Est. Monthly Cost |
|---|---|---|
| Render API | Free (spins down) | $0 |
| Render Static Site | Free | $0 |
| Neon | Free tier (0.5 GB, scale-to-zero) | $0 |
| Auth0 | Free tier (7,500 MAU) | $0 |
| Stripe | No monthly fee; 2.9% + 30¢ per transaction | $0 + transaction fees |
| Cloudflare R2 | Free tier (10 GB storage, 1M requests) | $0 |
| Resend | Free tier (3,000 emails/month) | $0 |
| Sentry | Free tier (5,000 errors/month) | $0 |
| GitHub | Free (public or free private repos) | $0 |
| **Total** | | **~$0 + Stripe fees** |

### 10 Customers (paying subscribers)

| Service | Tier | Est. Monthly Cost |
|---|---|---|
| Render API | Starter instance ($7/month, no spin-down) | ~$7 |
| Render Background Worker | Starter instance | ~$7 |
| Render Static Site | Free | $0 |
| Neon | Launch plan (~$19/month, 10 GB) | ~$19 |
| Auth0 | Free tier likely sufficient at low MAU | $0–23 |
| Stripe | 2.9% + 30¢ on ~$1,490–3,490/month revenue | ~$50–110 |
| Cloudflare R2 | Free tier sufficient at low document volume | $0 |
| Resend | Pro plan ($20/month, 50,000 emails) | ~$20 |
| Sentry | Team plan (~$26/month) | ~$26 |
| **Total** | | **~$130–212/month** |

### 100 Customers

| Service | Tier | Est. Monthly Cost |
|---|---|---|
| Render API | Standard instance (~$25/month) | ~$25 |
| Render Background Worker | Standard instance | ~$25 |
| Render Static Site | Free | $0 |
| Neon | Scale plan (~$69/month, autoscale) | ~$69 |
| Auth0 | Essential plan ($240/month for 1,000 MAU) | ~$240 |
| Stripe | 2.9% + 30¢ on ~$15K–35K/month revenue | ~$450–1,050 |
| Cloudflare R2 | ~$0.015/GB stored + $0.36/million requests | ~$5–15 |
| Resend | Business plan (~$89/month, 250,000 emails) | ~$89 |
| Sentry | Team plan ($26/month) | ~$26 |
| **Total** | | **~$930–1,540/month** |

---

## Switching Services

Notes on migration difficulty if a service needs to be replaced.

| Service | Replacement Difficulty | Notes |
|---|---|---|
| **Render** | Low | A `Dockerfile` exists. Any Docker-capable host (Railway, Fly.io, AWS ECS, Azure App Service) works without code changes. |
| **Neon** | Low | Standard PostgreSQL. Any hosted PostgreSQL (Supabase, RDS, Azure Database) works with a connection string change. EF Core migrations are provider-agnostic. |
| **Auth0** | Medium | The platform uses standard OIDC / JWT bearer — any OIDC provider (Clerk, Supabase Auth, Azure AD B2C, Keycloak) can replace Auth0. The Post Login Action logic (injecting custom claims) must be reproduced in the new provider. |
| **Stripe** | High | Stripe is deeply integrated: checkout sessions, webhook event processing, Customer Portal, subscription lifecycle (TRIAL → ACTIVE → SUSPENDED → CANCELLED), and `StripeCustomerId` stored on `TenantEntity`. Replacing Stripe requires rewriting the `Billing` and `Webhooks` feature modules and re-provisioning all active subscriptions. |
| **Cloudflare R2** | Low | R2 is S3-compatible. The `R2FileStorageService` uses the AWS SDK. Any S3-compatible store (AWS S3, MinIO, Backblaze B2) works with environment variable changes only. |
| **Resend** | Low | The `IEmailService` interface abstracts all email sending. Replacing Resend requires implementing `IEmailService` for the new provider (SendGrid, Postmark, AWS SES) and registering the new implementation in `Program.cs`. No other code changes required. |
| **Sentry** | Low | Sentry is initialised in `Program.cs` only. Replacing it with another APM tool (Datadog, Honeycomb, Application Insights) requires changing the SDK initialisation and the user-context middleware. No business logic is affected. |
| **GitHub** | Low | The CI/CD pipeline is standard GitHub Actions. GitLab CI or Bitbucket Pipelines can replicate the same build + deploy hook pattern. |
