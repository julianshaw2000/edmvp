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
- **Webhook:** `https://accutrac-api.onrender.com/api/stripe/webhook` (`we_1TEEQUCvOGA4undoPhY3VSyn`)
- **Events:** `checkout.session.completed`, `invoice.paid`, `invoice.payment_failed`, `customer.subscription.deleted`
- **Keys:** stored in `docs/stripe.secrets`

### Environment Variables (Render)
- `Stripe__SecretKey` — `sk_test_...` from stripe.secrets
- `Stripe__WebhookSecret` — `whsec_7xUxWKZhNVmGLN0s8xbAq1iur5H3equH`
- `Stripe__PriceId` — `price_1TEEQ1CvOGA4undoCj5R57Yd`
- `Stripe__PublishableKey` — `pk_test_...` from stripe.secrets (passed to frontend for reference)

---

## Signup Flow

1. User visits `/signup` (public, no auth required)
2. Fills form: company name, their name, email
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
8. Meanwhile, Stripe fires `checkout.session.completed` webhook → API provisions:
   - New `TenantEntity` (status: TRIAL, StripeCustomerId, StripeSubscriptionId, TrialEndsAt = now + 60 days)
   - New `UserEntity` (role: TENANT_ADMIN, Auth0Sub: `pending|{email}`)
9. User clicks "Sign in with Google" on success page → `/api/me` matches by email → linked

---

## Database Changes

### TenantEntity — New Fields

| Field | Type | Nullable | Description |
|-------|------|----------|-------------|
| `StripeCustomerId` | string(100) | Yes | Stripe customer ID (e.g., `cus_xxx`) |
| `StripeSubscriptionId` | string(100) | Yes | Stripe subscription ID (e.g., `sub_xxx`) |
| `PlanName` | string(50) | Yes, default "PRO" | Plan identifier for future tier expansion |
| `TrialEndsAt` | DateTime | Yes | When the 60-day trial expires |

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
- Validates: companyName not empty, email valid, email not already in use
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
3. Create TenantEntity (status: TRIAL, StripeCustomerId, StripeSubscriptionId, PlanName: "PRO", TrialEndsAt: now + 60 days)
4. Create UserEntity (role: TENANT_ADMIN, email from metadata, Auth0Sub: `pending|{guid}`, DisplayName from metadata)
5. Log: "Tenant provisioned via Stripe checkout"

**`invoice.paid`:**
1. Look up tenant by StripeSubscriptionId (from `invoice.subscription`)
2. If tenant status is TRIAL or SUSPENDED, set to ACTIVE
3. If not found, log warning and return 200 (idempotent)

**`invoice.payment_failed`:**
1. Look up tenant by StripeSubscriptionId
2. Set status to SUSPENDED
3. Log warning

**`customer.subscription.deleted`:**
1. Look up tenant by StripeSubscriptionId
2. Set status to CANCELLED
3. Log warning

**All webhook handlers return 200** even on errors (Stripe retries on non-2xx, which would cause duplicate processing).

### Modified Endpoints

**UpdateTenantStatus** — add TRIAL and CANCELLED as valid status values.

---

## TenantStatusBehaviour Updates

Update the existing `TenantStatusBehaviour` to handle new statuses:

```
TRIAL → full access (same as ACTIVE)
ACTIVE → full access
SUSPENDED → blocked: "Your organization's account has been suspended. Contact support."
CANCELLED → blocked: "Your subscription has been cancelled."
```

PLATFORM_ADMIN bypasses all status checks (unchanged).

---

## Frontend Changes

### New Public Pages (No Auth Required)

**Signup Page (`/signup`):**
- Simple form: company name, your name, email
- "Start 60-day free trial" button
- Subtitle: "$249/month after trial. Cancel anytime."
- On submit: POST to `/api/signup/checkout`, redirect to returned `checkoutUrl`
- Styled consistently with the login page

**Success Page (`/signup/success`):**
- "Your account is ready!"
- "Sign in with Google to get started" button (links to `/login`)
- Explains: "You'll be signed in as the administrator for your company"

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

---

## Success Criteria

1. Public signup page at `/signup` with company name, name, email form
2. Form submits to API which creates Stripe Checkout Session with 60-day trial
3. After Stripe Checkout, webhook creates tenant (TRIAL) + TENANT_ADMIN user
4. New user can sign in with Google and access their tenant
5. Stripe automatically charges $249/month after 60 days
6. `invoice.paid` transitions tenant from TRIAL to ACTIVE
7. `invoice.payment_failed` suspends the tenant
8. `customer.subscription.deleted` cancels the tenant
9. Suspended/cancelled tenants are blocked by TenantStatusBehaviour
10. Platform admin can still override any tenant status manually
11. Login page links to signup page
12. Success page directs user to sign in with Google
