# auditraks — Platform Admin Maintenance Manual

**Audience:** Julian Shaw (platform owner)
**Last updated:** 2026-03-26
**Platform:** auditraks SaaS — Mineral Supply Chain Compliance

---

## Table of Contents

1. [Daily Operations](#1-daily-operations)
2. [Weekly Tasks](#2-weekly-tasks)
3. [Monthly Tasks](#3-monthly-tasks)
4. [Tenant Management](#4-tenant-management)
5. [Stripe Operations](#5-stripe-operations)
6. [Auth0 Operations](#6-auth0-operations)
7. [Troubleshooting](#7-troubleshooting)
8. [Emergency Procedures](#8-emergency-procedures)
9. [Useful Commands](#9-useful-commands)
10. [Key Contacts and URLs](#10-key-contacts-and-urls)

---

## 1. Daily Operations

### Monitoring Dashboard

Each morning, do a quick pass across these four areas before starting anything else.

**Render dashboard — https://dashboard.render.com**

Check that all three services show a green "Live" status:

- `accutrac-api` — Web Service (ASP.NET Core API)
- `accutrac-web` — Static Site (Angular SPA)
- Background Worker — compliance checks and email retry

If any service shows "Failed" or "Crashed", check its log output immediately. The most common causes are a failed startup (EF migration error or missing env var) or a Neon cold-start timeout.

**Health endpoints — hit these directly**

```bash
curl https://accutrac-api.onrender.com/health
curl https://accutrac-api.onrender.com/health/live
curl https://accutrac-api.onrender.com/health/ready
```

| Endpoint | What a healthy response looks like |
|---|---|
| `/health` | `{"status":"Healthy"}` — Kestrel is up |
| `/health/live` | Same as above — Render's own health check uses this |
| `/health/ready` | `{"status":"Healthy"}` — migrations complete and DB reachable; `Degraded` means migrations still running (normal briefly after deploy) |

If `/health/ready` stays `Degraded` for more than two minutes after a deploy, check Render logs for a database connection error or migration failure.

**Neon dashboard — https://console.neon.tech**

- Check the connection graph for the `auditraks` database. A flat line with no requests is normal if no tenants are active overnight.
- Check storage usage. The free tier allows 0.5 GB. The Launch plan allows 10 GB. If you are approaching a limit, archive old data or upgrade the plan.
- If a query is running unusually long (Neon shows this under Monitoring > Queries), check whether a compliance check job has stalled.

**Sentry — https://sentry.io** (if configured)

- Review any new unhandled exceptions from the previous 24 hours.
- Look especially for auth errors, database errors, and Stripe webhook failures.
- Dismiss or assign any issues that have been investigated.

### Reviewing New Signups

New signups come in through Stripe Checkout. The full automated flow:

1. Prospective customer fills in the sign-up form at `/signup`
2. They complete Stripe Checkout (60-day trial, card required)
3. Stripe fires `checkout.session.completed`
4. The API provisions a tenant and TENANT_ADMIN user, sends a welcome email

**To verify a signup completed correctly:**

1. Go to **Stripe dashboard > Customers** — the new customer should appear with a subscription in Trial status.
2. Go to your **Admin Dashboard** (log in as platform admin at `https://auditraks.com`) and check Tenants. The new tenant should be listed with status TRIAL.
3. Check that the TENANT_ADMIN user appears under the tenant's user list.
4. Check that the welcome email was sent — go to Resend dashboard > Emails and look for the most recent "Welcome" email.

**If a signup appears in Stripe but the tenant is missing:**

The webhook did not process. Go to **Stripe dashboard > Developers > Webhooks > your endpoint > Recent deliveries**. Find the `checkout.session.completed` event. If it shows a delivery failure, click "Resend" to replay it. Check Render logs at the same time to see why it failed.

Common webhook failure causes:
- `Stripe__WebhookSecret` is incorrect on Render
- API was cold (first request after spin-down timed out — just resend the event)
- A bug in tenant provisioning logic — check Render logs for a stack trace

### Monitoring Emails

**Resend dashboard — https://resend.com/emails**

Check the Emails tab for delivery failures. The three email types the platform sends:

| Email | Trigger |
|---|---|
| Welcome | New tenant provisioned via Stripe checkout |
| Trial ending warning | Approaching end of 60-day trial |
| Payment failed | `invoice.payment_failed` webhook from Stripe |

A healthy delivery dashboard shows no bounces and a delivery rate near 100%.

**Common email issues:**

- **Bounced email:** Usually a bad address at signup. The subscription is still active — contact the customer through another channel and fix their email in the database if needed.
- **Domain reputation drop:** If multiple emails bounce or are marked as spam, check that the Resend domain authentication DNS records for `auditraks.com` are still in place in Cloudflare. Go to **Resend > Domains** and verify the status shows "Verified".
- **Emails not sending at all:** Check that `Resend__ApiKey` is set on the Render API service. If it is absent, the API silently falls back to `LogEmailService` (writes to app log only — no email sent). Check Render logs for lines containing the email subject.

---

## 2. Weekly Tasks

### RMAP Smelter List Updates

The RMAP-approved smelter list is the reference data the platform uses to validate smelting events. It should be kept current — new smelters are added and certifications lapse quarterly.

**When to update:**

- The Responsible Minerals Initiative publishes an updated list at **https://www.responsiblemineralsinitiative.org**. Check the RMI website for new list releases, typically quarterly.
- A tenant reports that a known-good smelter ID is being flagged as unrecognized.

**How to update:**

1. Download the latest RMAP conformant smelter list from the RMI website (usually an Excel file — convert to CSV).
2. Ensure the CSV matches this format exactly:

```
SmelterId,SmelterName,Country,ConformanceStatus,LastAuditDate
RMI-001,Example Smelter,DE,CONFORMANT,2025-10-15
```

3. Log in to the auditraks platform as PLATFORM_ADMIN.
4. Navigate to **Admin Dashboard > RMAP Smelters**.
5. Click **Upload RMAP List**.
6. Select your CSV file and confirm the upload.
7. The platform replaces the existing list. Check the count matches the number of rows in your file.

After uploading, any batches previously flagged with "unrecognized smelter" will have their compliance checks re-evaluated on the next background worker run.

### Compliance Flag Review

Each week, review flagged batches across all tenants to ensure nothing serious is being overlooked.

**How to find flagged batches:**

1. Log in as PLATFORM_ADMIN.
2. Navigate to **Admin Dashboard > Analytics** — the Compliance Breakdown chart shows the platform-wide split across COMPLIANT, FLAGGED, PENDING, and INSUFFICIENT_DATA.
3. Use the tenant filter to drill into specific tenants if needed.

**What each compliance status means:**

| Status | Meaning | Your action |
|---|---|---|
| COMPLIANT | All checks passed | None required |
| FLAGGED | One or more checks failed | Review — may need tenant notification |
| INSUFFICIENT_DATA | Missing events or documents | Notify the tenant to complete data entry |
| PENDING | No checks triggered yet | Normal for newly created batches |

**When to escalate a FLAGGED batch:**

- A batch is flagged for OECD DDG origin risk (high-risk country) and the tenant has not acknowledged it — email the Tenant Admin to explain the flag and ask for documented justification if their sourcing practices comply.
- A batch is flagged for a sanctions match — treat as high priority. Contact the tenant immediately. Do not unilaterally dismiss a sanctions flag.
- A batch has been FLAGGED for more than 30 days with no action from the tenant — follow up.

### Audit Log Review

The audit log records every action on the platform: batch creation, event submissions, document uploads, user changes, and admin operations.

**How to export and review:**

1. Log in as PLATFORM_ADMIN.
2. Navigate to **Admin Dashboard > Audit Log**.
3. Use the filters: Action, Entity Type, Result, date range, and tenant.
4. Click **Export CSV** to download for offline review.

**What to look for:**

- **Repeated failed operations from the same user or IP:** Could indicate a brute-force attempt or a misconfigured integration.
- **PLATFORM_ADMIN actions at unusual times:** Actions by the platform admin account at odd hours could indicate compromised credentials.
- **Bulk document uploads in a short period:** Could indicate a tenant testing limits or an automated integration running unchecked.
- **User deactivation or role changes:** Ensure tenant admins are not making unexpected changes to other users.

**Normal patterns to recognize as benign:**

- Compliance check entries appearing after every custody event submission (the background worker logging its results)
- Batch status transitions from CREATED to ACTIVE to COMPLETED following normal workflow
- RMAP smelter upload events after you perform the weekly list update

---

## 3. Monthly Tasks

### Billing Review

**Stripe dashboard — https://dashboard.stripe.com**

Work through this checklist each month:

**Active subscriptions:**
- Confirm all paying tenants have an ACTIVE subscription status in both Stripe and the auditraks tenant list.
- Check for any subscriptions in `past_due` state — these mean a payment retry is outstanding. The tenant should already be SUSPENDED in the platform. Contact the customer directly.

**Failed payments:**
- Go to **Stripe > Payments** and filter for failed charges.
- Stripe retries failed payments automatically using Smart Retries (typically 3–4 attempts over several days).
- If a customer's card declines repeatedly, Stripe fires `invoice.payment_failed` which suspends the tenant. Contact the customer proactively before they lose access.

**Trial expirations coming up:**
- Go to **Stripe > Subscriptions** and filter by trial status. Check for any trials ending in the next 14 days.
- Consider sending a personal email to those customers to check they are getting value and answer any questions before they are charged.

**TRIAL → ACTIVE transitions:**
- When a trial ends and the first payment succeeds, Stripe fires `invoice.paid` and the tenant moves to ACTIVE.
- Verify in the auditraks admin that the tenant status updated correctly. If it is still showing TRIAL after a successful payment, check webhook delivery.

**Cancellations:**
- Review any `customer.subscription.deleted` events from the past month.
- Tenants will be marked CANCELLED in the platform — their data is retained but they have no API access.
- Consider a brief offboarding email to understand why they cancelled.

### Database Maintenance

**Neon dashboard — https://console.neon.tech**

- Check **Storage** under your project. Free tier: 0.5 GB. Launch: 10 GB. Scale: autoscale.
- Check **Connections** — the pooler handles connection bursts. If you see sustained high connection counts, the background worker or API may have a connection leak.
- Check **Query Performance** — Neon shows slow queries. Anything consistently over 1 second is worth investigating. EF Core generates efficient queries but check the analytics endpoint in particular (it aggregates across all tenants).

**No manual vacuuming required.** Neon manages PostgreSQL autovacuum automatically.

**Manual database check (run in Neon SQL Editor):**

```sql
SELECT COUNT(*) AS tenants FROM "Tenants";
SELECT COUNT(*) AS users FROM "Users";
SELECT COUNT(*) AS batches FROM "Batches";
SELECT COUNT(*) AS events FROM "CustodyEvents";
SELECT status, COUNT(*) FROM "Tenants" GROUP BY status;
```

This gives you a quick snapshot of platform size and tenant health.

### Security Review

**API key audit:**

1. Log in as PLATFORM_ADMIN.
2. Navigate to **Admin Dashboard > API Keys** (you can view across tenants).
3. Review keys with "Last Used" dates older than 90 days — check with the relevant tenant admin whether they are still in use, or if they should be revoked.
4. Check for any revoked keys that are cluttering the view — revoked keys remain visible for audit purposes but are permanently inactive.

**Webhook endpoint review:**

1. Navigate to **Admin Dashboard > Webhooks**.
2. Review all tenant webhook endpoints. Look for:
   - Endpoints pointing to localhost or non-HTTPS URLs (should not exist in production)
   - Endpoints that have not received a successful delivery in more than 30 days
3. Contact the relevant tenant admin for stale endpoints.

**Auth0 login activity:**

1. Go to **Auth0 Dashboard > Activity > Logs**.
2. Review failed login attempts. A spike in failed logins (type `fp` — failed password) could indicate credential stuffing.
3. If a specific user account is being targeted, block it from Auth0 until the account holder is notified.

**Render environment variable check:**

Verify all critical env vars are set on the `accutrac-api` service. The absence of key variables causes silent failures:

| Variable | Effect if missing |
|---|---|
| `Auth0__Domain` | API starts with NO token validation — anyone can call any endpoint |
| `Stripe__SecretKey` | All billing endpoints fail |
| `Stripe__WebhookSecret` | All Stripe webhook events are rejected |
| `R2__AccountId` | File uploads write to local disk (lost on redeploy) |
| `Resend__ApiKey` | No emails are sent |

---

## 4. Tenant Management

### Creating a Tenant Manually

Use this when a customer needs an account created outside of the self-serve signup flow (e.g. enterprise deals, trial extensions, internal testing).

**Via Admin Dashboard (recommended):**

1. Log in as PLATFORM_ADMIN at `https://auditraks.com`.
2. Navigate to the Platform Admin section > Tenants.
3. Click **Create Tenant**.
4. Enter:
   - **Organisation name** — the company's display name
   - **Admin email** — the email of the first user (will be created as TENANT_ADMIN)
5. Submit the form.

The system creates the tenant and a TENANT_ADMIN user with a pending Auth0 link. Tell the customer to go to `https://auditraks.com` and sign in with the email you provided (using either Google or email/password — whichever matches that email).

**Via API (if dashboard is unavailable):**

```bash
curl -X POST https://accutrac-api.onrender.com/api/platform/tenants \
  -H "Authorization: Bearer <YOUR_PLATFORM_ADMIN_JWT>" \
  -H "Content-Type: application/json" \
  -d '{"name":"Acme Mining Co","adminEmail":"admin@acmemining.com"}'
```

Note: no welcome email is sent when creating manually via the API. Notify the customer yourself.

### Setting Custom Plan Limits

To give a tenant more batches or users than their plan allows (e.g. a negotiated enterprise deal):

```sql
UPDATE "Tenants"
SET "MaxBatches" = 500, "MaxUsers" = 25, "UpdatedAt" = now()
WHERE "Id" = '<tenant-uuid>';
```

Run this in the **Neon SQL Editor** at console.neon.tech.

### Suspending a Tenant

**When to suspend:** payment failure (happens automatically), abuse, customer request for account pause.

**Via Admin Dashboard:**
1. Log in as PLATFORM_ADMIN.
2. Navigate to Tenants.
3. Find the tenant and click **Suspend**.

**Effect:** All users in that tenant receive a 403 on every API request. They can still reach the login page but cannot access any data or perform any actions.

**Via API:**
```bash
curl -X PATCH https://accutrac-api.onrender.com/api/platform/tenants/{id}/status \
  -H "Authorization: Bearer <YOUR_PLATFORM_ADMIN_JWT>" \
  -H "Content-Type: application/json" \
  -d '{"status":"SUSPENDED"}'
```

**To reactivate a suspended tenant:**

Same steps as above — set status to `ACTIVE`. If reactivating after a payment failure, verify the payment issue has been resolved in Stripe first. Normally Stripe's `invoice.paid` webhook does this automatically when the customer pays.

### Viewing Tenant Data

As PLATFORM_ADMIN, all platform views default to "All Tenants":

- **Analytics:** shows aggregated metrics across all tenants. Use the tenant filter dropdown to view one specific tenant.
- **Users:** shows all users across all tenants. Filter by tenant to see a specific organisation's team.
- **Audit Log:** shows all platform events. Filter by tenant, action, entity type, and date range. Export to CSV for detailed review.
- **Batches:** shows all batches across all tenants. Filter by tenant to see a specific organisation's supply chain data.

---

## 5. Stripe Operations

### Handling Failed Payments

Stripe's Smart Retries automatically retries failed payments 3–4 times over several days. You do not need to manually intervene unless retries are exhausted.

**Automated flow:**
1. Payment fails → Stripe fires `invoice.payment_failed`
2. API webhook handler receives it → tenant status set to SUSPENDED
3. Platform sends a payment failure email to the Tenant Admin
4. Stripe retries the charge (Smart Retries schedule)
5. If a retry succeeds → Stripe fires `invoice.paid` → tenant automatically reactivated

**If retries are exhausted and the customer contacts you:**
1. Ask them to update their payment method via the Billing Portal (they can access it by clicking "Manage Billing" — even suspended tenants can do this).
2. Once the card is updated, go to **Stripe > Subscriptions** and manually trigger a payment by clicking "Pay" on the outstanding invoice.
3. The `invoice.paid` webhook will fire and reactivate the tenant automatically.

**If you need to manually reactivate before payment is resolved** (e.g. customer has promised payment):
- Update tenant status to ACTIVE via Admin Dashboard or API.
- Keep monitoring Stripe until the payment comes through.

### Processing Refunds

Refunds are handled entirely in Stripe and do not automatically affect tenant status.

1. Go to **Stripe Dashboard > Payments**.
2. Find the charge you want to refund.
3. Click the charge → click **Refund**.
4. Enter the refund amount and a reason (optional but helpful for your records).
5. Confirm the refund.

After refunding, decide whether the tenant should remain ACTIVE or be moved to CANCELLED:
- **Partial refund (e.g. trial dispute):** Usually leave the tenant ACTIVE.
- **Full refund + cancellation:** Update the tenant status to CANCELLED in the Admin Dashboard.

### Moving to Production (Live Stripe Keys)

The platform is currently using Stripe test mode (`sk_test_...`). When you are ready for real payments:

1. In the Stripe dashboard, switch to **Live mode** (toggle in the top-left).
2. Create a new webhook endpoint:
   - URL: `https://accutrac-api.onrender.com/api/stripe/webhook`
   - Events: `checkout.session.completed`, `invoice.paid`, `invoice.payment_failed`, `customer.subscription.deleted`
3. Copy the new **signing secret** (`whsec_...`) from the webhook endpoint.
4. Create the products and prices in live mode (Stripe does not copy test prices to live):
   - Starter plan: $99/month
   - Pro plan: $249/month, 60-day trial
5. Update these **Render environment variables** on the `accutrac-api` service:

   | Variable | New value |
   |---|---|
   | `Stripe__SecretKey` | `sk_live_...` |
   | `Stripe__WebhookSecret` | New live webhook signing secret |
   | `Stripe__PriceId` | Live Pro Price ID |
   | `Stripe__StarterPriceId` | Live Starter Price ID |

6. Update the Angular environment file `packages/web/src/environments/environment.prod.ts` to use the live publishable key (`pk_live_...`) and trigger a new deploy.
7. Test with a real card before announcing to customers.

---

## 6. Auth0 Operations

### Adding a User Manually

**Via Admin Dashboard (preferred):**

1. Log in as the relevant tenant's TENANT_ADMIN (or as PLATFORM_ADMIN viewing that tenant).
2. Navigate to **Admin > Manage Users > Invite User**.
3. Enter the user's email and select their role (Supplier or Buyer).
4. Click **Send Invitation**.

The user receives an email with a 7-day activation link. They click it and complete sign-in (Google or email/password). On first login, their Auth0 identity is linked to the invitation record automatically.

**If an invitation email was not received:**

- Check Resend dashboard for delivery status.
- Check the user's spam folder.
- Use the Users list to find the pending invite and click **Resend Invitation**.
- Invitation links expire after 7 days — resend if expired.

### Resetting a User's Password

Users handle this themselves. Auth0 provides a self-service flow:

1. User goes to `https://auditraks.com/login`.
2. They click **Forgot password?**
3. Auth0 sends a password reset email directly.

You do not need to intervene. If a user claims the reset email never arrived, check the Auth0 Activity logs (see below) and verify the email address is correct in the platform.

### Unlocking a Blocked User

Auth0 blocks users after repeated failed login attempts. To unblock:

1. Go to **Auth0 Dashboard > User Management > Users**.
2. Search for the user by email.
3. Click on the user → click **Unblock**.

The user can log in immediately after unblocking.

### Reviewing Login Activity

1. Go to **Auth0 Dashboard > Activity > Logs**.
2. Use the filter to select event types:
   - `s` — successful login
   - `fp` — failed password login
   - `ss` — successful signup
   - `f` — general failure

Useful for diagnosing:
- A user who "can't log in" — check for recent failed events for their email
- Unusual login volumes from unexpected IP addresses
- Whether a new user has successfully activated their account (look for `ss` or `s` after their invitation was sent)

### Fixing a Mislinked Auth0 Account

This happens occasionally when a user first signs in with Google, then later with email/password (or vice versa). The `/api/me` endpoint self-heals most cases automatically.

If a user is stuck:

1. In the Neon SQL Editor, reset their Auth0 sub:

```sql
UPDATE "Users"
SET "Auth0Sub" = 'pending|' || gen_random_uuid(), "UpdatedAt" = now()
WHERE "Email" = 'affected.user@example.com';
```

2. Ask the user to sign in again. The `/api/me` endpoint will re-link their identity.

### Post Login Action — Critical Config

The Auth0 Post Login Action must be deployed for the platform to work. It injects `email` and `name` as custom claims into the JWT. Without it, the API cannot identify users.

**To verify it is active:**

1. Go to **Auth0 Dashboard > Actions > Flows > Login**.
2. Check that the Post Login action appears in the Login flow and shows "Deployed".

**The action code** (for reference if it ever needs to be recreated):

```javascript
exports.onExecutePostLogin = async (event, api) => {
  const namespace = 'https://auditraks.com';
  api.idToken.setCustomClaim(`${namespace}/email`, event.user.email);
  api.idToken.setCustomClaim(`${namespace}/name`, event.user.name);
  api.accessToken.setCustomClaim(`${namespace}/email`, event.user.email);
  api.accessToken.setCustomClaim(`${namespace}/name`, event.user.name);
  api.idToken.setCustomClaim('email', event.user.email);
  api.idToken.setCustomClaim('name', event.user.name);
};
```

---

## 7. Troubleshooting

### App Won't Load

Work through this in order:

1. **Check Render dashboard** — is the `accutrac-api` Web Service showing "Live"? Is `accutrac-web` Static Site deployed?
2. **Hit `/health`** — if you get a 200, Kestrel is up. If you get nothing or a 502, the service is down.
3. **Check Render logs** — look for an error on startup. The most common: missing env var, EF migration failure, or Neon connection refused.
4. **Check Neon status** — go to https://status.neon.tech. If Neon is having an incident, the API will fail to start (or return 503 on database operations) until it recovers.
5. **Free tier cold start** — if the API is on the free Render tier, it spins down after 15 minutes of inactivity. The first request after spin-down takes 20–30 seconds. This is not an outage — it is expected behavior. Upgrade to a paid Render instance to eliminate this.

### User Can't Log In

1. **Check the user exists** — Admin Dashboard > Users. If they are not listed, they have not been invited.
2. **Check tenant status** — if the tenant is SUSPENDED or CANCELLED, all users get a 403 after login. Verify tenant status in Admin Dashboard > Tenants.
3. **Check Auth0 logs** — Auth0 Dashboard > Activity > Logs. Search for the user's email. Look for recent failed logins (`fp`) or errors.
4. **Email mismatch** — the user must sign in with exactly the same email used in the invitation. If they have a Google account under a different email than they were invited with, the link will fail. Check both the invitation email and their Google account email.
5. **"Back button" error / redirect loop** — this is a browser cookie issue. Ask the user to clear cookies and site data for `auditraks.com` and try again.
6. **Invitation expired** — invitation links are valid for 7 days. Resend from Admin > Users.

### Signup Not Working

1. **Check Stripe env vars** — verify `Stripe__SecretKey` and `Stripe__WebhookSecret` are set on Render.
2. **Check Render logs** — filter to the `accutrac-api` service and look for errors around the time of the signup attempt.
3. **Test the checkout endpoint directly:**

```bash
curl -X POST https://accutrac-api.onrender.com/api/signup/checkout \
  -H "Content-Type: application/json" \
  -d '{"companyName":"Test Co","name":"Test User","email":"unique@test.com","plan":"PRO"}'
```

A successful response returns a Stripe checkout URL. An error response will show the problem.

4. **Check Stripe webhook delivery** — Stripe Dashboard > Developers > Webhooks > your endpoint > Recent deliveries. Look for `checkout.session.completed` delivery failures.
5. **Rate limiter** — if you recently enabled the rate limiter, it may be blocking rapid test signups from the same IP. Check Render logs for 429 responses.

### Emails Not Sending

1. **Check `Resend__ApiKey` is set** on Render. If absent, the API falls back to `LogEmailService` silently.
2. **Check Resend dashboard** — https://resend.com/emails. If the key is set but emails are not appearing here, check Render logs for errors when the email service is called.
3. **Verify domain is still authenticated** — Resend > Domains. The `auditraks.com` domain must show "Verified". If not, re-add the Resend DNS records in Cloudflare.
4. **Test Resend directly:**

```bash
curl -X POST https://api.resend.com/emails \
  -H "Authorization: Bearer YOUR_RESEND_KEY" \
  -H "Content-Type: application/json" \
  -d '{"from":"auditraks <noreply@auditraks.com>","to":"your@email.com","subject":"Test","html":"<p>Test</p>"}'
```

If this fails, the API key is invalid or the domain is not verified. If this succeeds but platform emails are not sending, the issue is in how the API constructs the email — check Render logs.

### Analytics Page Error

1. **Cold start timeout** — the analytics query aggregates across all tenants and can time out after a Neon cold start. Refresh the page and it should load on the second attempt.
2. **Check Render logs** — look for the actual error. Common: an EF Core query translation error (a LINQ expression that cannot be translated to SQL).
3. If the query fails consistently, check whether there is a large volume of data for a particular tenant causing a full table scan. Add a filter and refresh.

### Compliance Check Not Running

1. **Check Background Worker status** on Render — it must be running. If it has crashed, restart it.
2. **Check Render logs** for the worker service — look for exceptions or job failures.
3. Compliance checks run after each event submission (triggered by the API), with the worker handling async processing. If the API is up but the worker is down, compliance statuses will stay as PENDING until the worker recovers.

---

## 8. Emergency Procedures

### Database Issues

**If data appears corrupted or a migration fails catastrophically:**

1. **Do not run anything else against the database.**
2. Go to **Neon Console > Branches**. Neon keeps point-in-time recovery snapshots based on your plan's retention window.
3. Create a new branch from a recent restore point.
4. Update `ConnectionStrings__DefaultConnection` on Render to point to the new branch's connection string.
5. Restart the API service on Render.
6. Verify the platform is functional before promoting the branch as the primary.

**Manual backup before risky operations:**

```bash
pg_dump "YOUR_CONNECTION_STRING" -Fc -f auditraks-$(date +%Y%m%d).dump
```

Run this from a local machine with `psql` installed. Connection string is in `docs/neon.secrets`.

### Security Breach

Act quickly and in this order:

1. **Revoke all API keys** — Admin Dashboard > API Keys > Revoke all active keys. This immediately cuts off any programmatic access.
2. **Rotate Stripe webhook secret** — go to Stripe Dashboard > Developers > Webhooks > your endpoint > Reveal signing secret > Roll secret. Update `Stripe__WebhookSecret` on Render immediately.
3. **Rotate Auth0 client secret** if you believe Auth0 credentials are compromised — Auth0 Dashboard > Applications > your app > Settings > Rotate Secret. Update the frontend environment and redeploy.
4. **Check the audit log** — export the full audit log (Admin Dashboard > Audit Log > Export CSV) for the past 24–72 hours. Look for actions you do not recognize.
5. **Suspend affected tenants** if unauthorized data access is confirmed, to prevent further exposure while you investigate.
6. **Contact affected tenants** once you understand the scope. Be direct about what happened, what data was affected, and what you have done.

### Service Outage (External Dependency Down)

Check status pages first before assuming the problem is yours:

| Service | Status page |
|---|---|
| Render | https://status.render.com |
| Neon | https://status.neon.tech |
| Auth0 | https://status.auth0.com |
| Stripe | https://status.stripe.com |
| Cloudflare | https://www.cloudflarestatus.com |
| Resend | https://resend-status.com |

**If the issue is on your side:**

1. Check Render logs for the specific error.
2. For `accutrac-api`: go to the service on Render and click **Restart Service**.
3. For the Background Worker: same — click **Restart Service**.
4. If a new deploy broke something: go to Render > your service > **Deploys** tab > find the last working deploy > click **Rollback**.

### Accidental Data Change

If a PLATFORM_ADMIN action (e.g. tenant suspension, user role change) was done in error:

- Reverse it immediately via the Admin Dashboard using the same controls.
- Customer data (batches, events, documents) is append-only — events cannot be deleted by design. If a test batch was accidentally created under a real tenant, contact the tenant admin and flag it in their audit log.

---

## 9. Useful Commands

### Test API Health

```bash
curl https://accutrac-api.onrender.com/health
curl https://accutrac-api.onrender.com/health/ready
```

### Test Signup Flow (sends real Stripe checkout session)

```bash
curl -X POST https://accutrac-api.onrender.com/api/signup/checkout \
  -H "Content-Type: application/json" \
  -d '{"companyName":"Test Co","name":"Test User","email":"unique@test.com","plan":"PRO"}'
```

Use Stripe test card `4242 4242 4242 4242` with any future expiry and any CVC to complete the checkout without a real charge.

### Test Email Delivery (Resend API directly)

```bash
curl -X POST https://api.resend.com/emails \
  -H "Authorization: Bearer YOUR_RESEND_KEY" \
  -H "Content-Type: application/json" \
  -d '{"from":"auditraks <noreply@auditraks.com>","to":"your@email.com","subject":"Test","html":"<p>Test</p>"}'
```

### Trigger Manual Redeploy (Render deploy hooks)

```bash
curl -s "$RENDER_API_DEPLOY_HOOK"
curl -s "$RENDER_WEB_DEPLOY_HOOK"
```

Deploy hook URLs are stored as GitHub Actions secrets. Retrieve them from **GitHub > Repository > Settings > Secrets > Actions**.

### Database Health Check (Neon SQL Editor)

```sql
-- Platform snapshot
SELECT COUNT(*) AS tenants FROM "Tenants";
SELECT COUNT(*) AS users FROM "Users";
SELECT COUNT(*) AS batches FROM "Batches";
SELECT COUNT(*) AS events FROM "CustodyEvents";

-- Tenant status breakdown
SELECT "Status", COUNT(*) FROM "Tenants" GROUP BY "Status";

-- Tenants with FLAGGED batches (for compliance review)
SELECT t."Name", COUNT(b."Id") AS flagged_batches
FROM "Tenants" t
JOIN "Batches" b ON b."TenantId" = t."Id"
WHERE b."ComplianceStatus" = 'FLAGGED'
GROUP BY t."Name"
ORDER BY flagged_batches DESC;

-- Users created in the last 7 days
SELECT "Email", "Role", "CreatedAt"
FROM "Users"
WHERE "CreatedAt" > now() - interval '7 days'
ORDER BY "CreatedAt" DESC;
```

### Manual Export / Backup

```bash
pg_dump "YOUR_NEON_CONNECTION_STRING" -Fc -f auditraks-$(date +%Y%m%d).dump
```

Full connection string is in `docs/neon.secrets`.

### Promote a User to PLATFORM_ADMIN

```sql
UPDATE "Users"
SET "Role" = 'PLATFORM_ADMIN', "UpdatedAt" = now()
WHERE "Email" = 'user@example.com';
```

### Reset a Mislinked Auth0 Account

```sql
UPDATE "Users"
SET "Auth0Sub" = 'pending|' || gen_random_uuid(), "UpdatedAt" = now()
WHERE "Email" = 'affected.user@example.com';
```

### Deactivate a User

```sql
UPDATE "Users"
SET "IsActive" = false, "UpdatedAt" = now()
WHERE "Email" = 'user@example.com';
```

---

## 10. Key Contacts and URLs

### Platform

| Item | URL / Value |
|---|---|
| Production app | https://auditraks.com |
| API base URL | https://accutrac-api.onrender.com |
| Public batch verification | https://auditraks.com/verify/{batchId} |
| Shared document links | https://auditraks.com/shared/{token} |

### Service Dashboards

| Service | Dashboard | Status page |
|---|---|---|
| Render (hosting) | https://dashboard.render.com | https://status.render.com |
| Neon (database) | https://console.neon.tech | https://status.neon.tech |
| Auth0 (auth) | https://manage.auth0.com | https://status.auth0.com |
| Stripe (billing) | https://dashboard.stripe.com | https://status.stripe.com |
| Cloudflare (DNS + R2) | https://dash.cloudflare.com | https://www.cloudflarestatus.com |
| Resend (email) | https://resend.com/emails | https://resend-status.com |
| Sentry (errors) | https://sentry.io | https://status.sentry.io |
| GitHub (source + CI) | https://github.com/julianshaw2000/edmvp | https://githubstatus.com |

### Credentials and Secrets

All credential files are in `docs/` and are gitignored. Never commit them.

| File | Contains |
|---|---|
| `docs/auth0.secrets` | Auth0 domain, client ID, audience |
| `docs/neon.secrets` | PostgreSQL connection string |
| `docs/cloudfare.secrets` | Cloudflare R2 bucket endpoint (includes account ID) |
| `docs/stripe.secrets` | Stripe keys, price IDs, webhook secret |
| `docs/resend.secrets` | Resend API key |

### Render Environment Variables (Quick Reference)

| Variable | Service | Effect if missing |
|---|---|---|
| `Auth0__Domain` | API | No token validation — critical security gap |
| `Auth0__Audience` | API | All JWT validation fails |
| `ConnectionStrings__DefaultConnection` | API | No database access — full outage |
| `R2__AccountId` | API | File uploads go to local disk (lost on redeploy) |
| `R2__AccessKeyId` | API | File uploads fail |
| `R2__SecretAccessKey` | API | File uploads fail |
| `R2__BucketName` | API | File uploads fail |
| `Stripe__SecretKey` | API | All billing endpoints fail |
| `Stripe__WebhookSecret` | API | All Stripe webhook events rejected |
| `Stripe__PriceId` | API | Pro plan checkout fails |
| `Stripe__StarterPriceId` | API | Starter plan checkout fails |
| `Resend__ApiKey` | API | No emails sent (silent fallback to log) |
| `Resend__FromEmail` | API | Email from address blank |
| `Sentry__Dsn` | API | No error tracking (non-critical) |
| `Cors__AllowedOrigins__0` | API | CORS may block frontend requests |
| `Cors__AllowedOrigins__1` | API | CORS may block frontend requests |

---

*auditraks Platform Admin Maintenance Manual — March 2026*
