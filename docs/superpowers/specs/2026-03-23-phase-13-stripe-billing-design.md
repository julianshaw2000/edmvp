# Phase 13: Self-Service Signup + Stripe Billing — Design Spec

**Date:** 2026-03-23
**Status:** Approved
**Prerequisite:** Phase 12 complete (multi-tenant management)

---

## Overview

Add self-service customer onboarding with Stripe billing. New customers sign up via a public page, enter payment details on Stripe Checkout (60-day free trial, $249/month after), and get auto-provisioned with a tenant and TENANT_ADMIN account.

---

## Pricing Model

- **Plan:** auditraks Pro — $249/month
- **Trial:** 60-day free trial (card required upfront)
- **Limits:** Unlimited batches, users, and compliance checks
- **No free tier.** Trial converts to paid or expires.

---

## Stripe Configuration (Already Set Up)

- **Product:** auditraks Pro (`prod_UCdhuviYCoWTsy`)
- **Price:** $249/month (`price_1TEEQ1CvOGA4undoCj5R57Yd`)
- **Webhook:** `https://accutrac-api.onrender.com/api/stripe/webhook`
- **Events:** `checkout.session.completed`, `invoice.paid`, `invoice.payment_failed`, `customer.subscription.deleted`
- **All keys and secrets:** see `docs/stripe.secrets` (gitignored)

### Environment Variables (Render)
- `Stripe__SecretKey` — see `docs/stripe.secrets`
- `Stripe__WebhookSecret` — see `docs/stripe.secrets`
- `Stripe__PriceId` — `price_1TEEQ1CvOGA4undoCj5R57Yd`
- `Stripe__PublishableKey` — see `docs/stripe.secrets`

---

## Signup Flow

1. User visits `/signup` (public, no auth required)
2. Fills form: company name, their name, email (with confirm email field)
3. Clicks "Start 60-day free trial"
4. API creates Stripe Checkout Session with:
   - Price: `price_1TEEQ1CvOGA4undoCj5R57Yd`
   - Trial period: 60 days
   - Customer email pre-filled
   - Metadata: `{ companyName, adminName, adminEmail }`
   - Success URL: `/signup/success`
   - Cancel URL: `/signup`
5. Frontend redirects to Stripe Checkout (hosted by Stripe)
6. User enters card details on Stripe's page
7. Stripe redirects to `/signup/success`
8. Meanwhile, Stripe fires `checkout.session.completed` webhook → API provisions tenant
9. Success page shows: "Your account is being set up. Sign in with Google to get started." (Note: webhook may arrive after redirect — user may need to wait a moment before signing in)

**Webhook timing gap:** The Stripe redirect to `/signup/success` may arrive before the webhook fires. The success page should communicate that account setup takes a moment. If the user signs in immediately and gets a 403 ("No account found"), they should try again in a few seconds.

---

## Database Changes

### TenantEntity — New Fields

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `StripeCustomerId` | string(100) | Yes | Stripe customer ID (e.g., `cus_xxx`) |
| `StripeSubscriptionId` | string(100) | Yes | Stripe subscription ID (e.g., `sub_xxx`) |
| `PlanName` | string(50) | Yes, default "PRO" | Plan identifier for future tier expansion |
| `TrialEndsAt` | DateTime | Yes | When the 60-day trial expires |

**Indexes:**
- Unique index on `StripeCustomerId` (where not null)
- Unique index on `StripeSubscriptionId` (where not null)

**Requires new EF Core migration.**

### Tenant Status Values

| Status | Access | Trigger |
|--------|--------|---------|
| TRIAL | Full access | `checkout.session.completed` |
| ACTIVE | Full access | `invoice.paid` (first payment after trial) |
| SUSPENDED | Blocked | `invoice.payment_failed` or manual |
| CANCELLED | Blocked | `customer.subscription.deleted` or manual |

---

## API Changes

### New Public Endpoints (No Auth)

**Create Checkout Session:**
```
POST /api/signup/checkout
Body: { "companyName": "Acme Mining", "name": "John Smith", "email": "john@acme.com" }
Response: { "checkoutUrl": "https://checkout.stripe.com/..." }
```
- No authentication required
- **Rate limited:** 5 requests per minute per IP (use the existing `public` rate limiter policy)
- Validates: companyName not empty, email valid, email not already in use globally
- Creates Stripe Checkout Session with 60-day trial
- Returns the Stripe-hosted checkout URL

**Stripe Webhook:**
```
POST /api/stripe/webhook
```
- No authentication (verified by Stripe signature)
- Reads raw request body, verifies with `Stripe__WebhookSecret`
- Handles 4 event types:

| Event | Action |
|-------|--------|
| `checkout.session.completed` | Create tenant (TRIAL) + TENANT_ADMIN user |
| `invoice.paid` | Set tenant status to ACTIVE |
| `invoice.payment_failed` | Set tenant status to SUSPENDED |
| `customer.subscription.deleted` | Set tenant status to CANCELLED |

### Webhook Handler Details

**`checkout.session.completed`:**
1. Extract metadata: companyName, adminName, adminEmail
2. Extract Stripe IDs: customer, subscription
3. **Dedup check:** If a user with this email already exists, log warning and return 200 (do not create duplicate tenant). This handles concurrent signups with the same email.
4. Create TenantEntity (status: TRIAL, StripeCustomerId, StripeSubscriptionId, PlanName: "PRO", TrialEndsAt: now + 60 days)
5. Create UserEntity (role: TENANT_ADMIN, email from metadata, Auth0Sub: `pending|{guid}`, DisplayName from metadata)
6. Log: "Tenant provisioned via Stripe checkout"

**`invoice.paid`:**
1. Look up tenant by StripeSubscriptionId (from `invoice.subscription`)
2. If tenant status is TRIAL or SUSPENDED, set to ACTIVE
3. If already ACTIVE, no-op (naturally idempotent — Stripe sends this monthly)
4. If not found, log warning and return 200

**`invoice.payment_failed`:**
1. Look up tenant by StripeSubscriptionId
2. Set status to SUSPENDED
3. Log warning

**`customer.subscription.deleted`:**
1. Look up tenant by StripeSubscriptionId
2. Set status to CANCELLED
3. Log warning

**All webhook handlers return 200** even on errors (Stripe retries on non-2xx, which would cause duplicate processing).

**Trial expiration:** Fully delegated to Stripe's subscription lifecycle. When the trial ends, Stripe attempts to charge the card and fires `invoice.paid` or `invoice.payment_failed`. No server-side trial enforcement needed.

### Modified Endpoints

**UpdateTenantStatus** — add CANCELLED as a valid status value. TRIAL is not manually settable (reserved for webhook provisioning only). Valid manual values: ACTIVE, SUSPENDED, CANCELLED.

---

## TenantStatusBehaviour Updates

Modify the existing `TenantStatusBehaviour` to check for both SUSPENDED and CANCELLED statuses, with distinct error messages for each:

```
TRIAL → full access (same as ACTIVE)
ACTIVE → full access
SUSPENDED → blocked: "Your organization's account has been suspended. Contact support."
CANCELLED → blocked: "Your subscription has been cancelled."
```

**Implementation:** Change the single `if (tenantStatus == "SUSPENDED")` check to handle both blocked statuses:
```csharp
if (tenantStatus is "SUSPENDED" or "CANCELLED")
{
    var message = tenantStatus == "SUSPENDED"
        ? "Your organization's account has been suspended. Contact support."
        : "Your subscription has been cancelled.";
    // return Result.Failure(message)
}
```

PLATFORM_ADMIN bypasses all status checks (unchanged).

---

## Frontend Changes

### New Public Pages (No Auth Required)

**Signup Page (`/signup`):**
- Simple form: company name, your name, email, confirm email
- "Start 60-day free trial" button
- Subtitle: "$249/month after trial. Cancel anytime."
- Client-side validation: emails must match
- On submit: POST to `/api/signup/checkout`, redirect to returned `checkoutUrl`
- Styled consistently with the login page

**Success Page (`/signup/success`):**
- "Your account is being set up!"
- "Sign in with Google to get started" button (links to `/login`)
- Note: "Account setup may take a few seconds. If you see an error on first sign-in, wait a moment and try again."

### Route Updates
- Add `/signup` and `/signup/success` as public routes (no auth guard)
- Add link on login page: "Don't have an account? Start a free trial"

### Tenant Status Display
- TENANT_ADMIN dashboard shows current plan status (TRIAL with days remaining, or ACTIVE)
- If TRIAL, show: "Trial ends in X days"

---

## Stripe NuGet Package

Add `Stripe.net` NuGet package to the API project for Stripe SDK.

---

## Out of Scope

- Customer billing portal (manage payment method, view invoices) — future phase
- Multiple plan tiers — future phase
- Usage-based billing / batch limits — future phase
- Proration on plan changes — no plan changes yet
- Dunning / retry logic — Stripe handles this automatically
- Email notifications for trial ending / payment failed — future phase
- Server-side trial expiration enforcement — delegated to Stripe
- Startup validation of Stripe config values — can be added later

---

## Success Criteria

1. Public signup page at `/signup` with company name, name, email (+ confirm) form
2. Form submits to API which creates Stripe Checkout Session with 60-day trial
3. Checkout endpoint is rate-limited (5 req/min per IP)
4. After Stripe Checkout, webhook creates tenant (TRIAL) + TENANT_ADMIN user
5. Duplicate `checkout.session.completed` for same email does not create duplicate tenants
6. New user can sign in with Google and access their tenant
7. Stripe automatically charges $249/month after 60 days
8. `invoice.paid` transitions tenant from TRIAL to ACTIVE (idempotent for renewals)
9. `invoice.payment_failed` suspends the tenant
10. `customer.subscription.deleted` cancels the tenant
11. SUSPENDED tenants blocked with suspension message
12. CANCELLED tenants blocked with cancellation message (distinct from suspension)
13. Platform admin can override tenant status (ACTIVE, SUSPENDED, CANCELLED — not TRIAL)
14. Login page links to signup page
15. Success page communicates potential setup delay
