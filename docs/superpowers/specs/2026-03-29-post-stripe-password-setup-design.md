# Post-Stripe Password Setup Flow — Design Spec

**Date:** 2026-03-29
**Status:** Approved

---

## Problem

After a successful Stripe checkout, the current flow redirects to a static `/signup/success` page that tells the user to "Sign in with Google" — the platform uses email/password auth, not Google. The actual account setup path requires clicking a password reset link in a welcome email. This is too many steps and creates a poor first impression at the most sensitive moment of onboarding.

Additionally, if the user abandons the browser after Stripe checkout, there is no clear recovery path back into account setup.

---

## Goals

1. After Stripe checkout, take the user directly to a password creation screen.
2. Auto-login and redirect to `/admin` after password is set — no extra sign-in step.
3. Handle the webhook race condition gracefully (account may not exist yet when the browser redirects).
4. Provide two recovery paths if the user abandons before setting a password:
   - **A** — A setup email sent by the webhook with a direct link back.
   - **B** — Detection at the login page with a resend option.

---

## Non-Goals

- Google / OAuth login.
- Changing the Stripe checkout experience itself.
- Changing how invited team members (non-admin) register — the existing `/register` route and `Register` handler are untouched.

---

## Architectural Decision: Webhook No Longer Creates AppIdentityUser

The current webhook (`StripeWebhookHandler.HandleCheckoutCompleted`) creates a `UserEntity` with `IdentityUserId = "pending|{guid}"` **and** immediately creates a fully-formed `AppIdentityUser` with a random `tempPassword`.

This design removes the `AppIdentityUser` creation from the webhook entirely. After the webhook fires:
- `UserEntity` exists with `IdentityUserId = "pending|{guid}"` — genuinely pending.
- No `AppIdentityUser` exists yet.
- `StripeSessionId` is stored on `UserEntity`.

The `POST /api/signup/set-password` endpoint creates the `AppIdentityUser` when the user sets their password. This is cleaner and removes the need for any `RemovePasswordAsync/AddPasswordAsync` dance.

---

## User Flow

### Happy Path

```
1. Signup form submitted
   - Email uniqueness validated before creating Stripe session (existing behaviour — no change)
   - 409 returned to the form if email already in use

2. Redirected to Stripe checkout

3. Stripe checkout completed
   - Stripe redirects browser to:
     /signup/set-password?session={CHECKOUT_SESSION_ID}
   - Note: {CHECKOUT_SESSION_ID} is a Stripe-supplied literal placeholder in the success URL

4. SetPasswordComponent loads
   - Reads `session` query param
   - If param is absent or empty: immediately redirect to /signup
   - Begins polling GET /api/signup/session/{sessionId} every 2 seconds (POLL_INTERVAL_MS = 2000)
   - Shows "Setting up your account…" spinner
   - Stripe webhook fires (typically within 1–3 seconds), provisions tenant + user

5. Polling detects provisioned=true (or times out after POLL_TIMEOUT_MS = 30000)
   - On provisioned=true: reveal password form
   - On timeout: show error state (see Error Path below)

6. User submits password
   - Fields: Password, Confirm password
   - Validation: 8+ chars, at least one uppercase, one lowercase, one digit
   - POST /api/signup/set-password { sessionId, password }

7. Backend (set-password handler)
   - Retrieves email from Stripe API using sessionId (source of truth — no client-supplied email trusted)
   - Finds UserEntity with that email and IdentityUserId.StartsWith("pending|") — 400 if not found
   - If AppIdentityUser already exists for this email → 409 "Account already set up"
   - Creates AppIdentityUser with provided password
   - Sets EmailConfirmed = true (Stripe checkout verified the address — no confirmation email needed)
   - Updates UserEntity.IdentityUserId to real Identity user ID
   - Issues JWT access token (15 min)
   - Sets HttpOnly refresh token cookie (14 days) — same cookie options as Login.cs exactly
   - Returns { accessToken: string }

8. Frontend
   - Stores access token
   - Calls authService.loadProfile() → GET /api/me
   - Navigates to /admin
```

### Error / Timeout Path

- **Polling timeout (30s):** Show message: *"Something went wrong setting up your account. Please contact support."* with a mailto link.
- **409 from set-password** (account already set up): Redirect to `/login` with query param `?hint=already-setup` — LoginComponent displays "Your account is already set up. Please sign in."
- **400 from set-password** (not found): Show error: *"We couldn't find your account. Please contact support."* with mailto link.
- **Missing `session` param on load:** Immediately redirect to `/signup`.

---

## Abandonment Recovery

### Path A — Setup Email (primary recovery)

The Stripe webhook sends a "Complete your account setup" email immediately after provisioning the user.

- **To:** Admin email from Stripe checkout metadata
- **Subject:** Complete your Tungsten account setup
- **CTA link:** `{baseUrl}/signup/set-password?session={sessionId}`
- **Note on session ID longevity:** Stripe checkout session *records* are permanently retrievable via the Stripe API — the expiry on a checkout session only prevents new payments from being made through the checkout URL. A completed session's data (including customer email) can be retrieved indefinitely. The setup email link is therefore long-lived.
- This email replaces the existing welcome email that contained a password reset link.

### Path B — Login Page Detection (secondary recovery)

If a user with an incomplete setup tries to log in:

**Backend change to `Login.cs`:**
- Before calling `userManager.FindByEmailAsync`, query `UserEntity` by email.
- If a `UserEntity` exists with that email and `IdentityUserId.StartsWith("pending|")`:
  - Return HTTP 400 with body `{ error: "ACCOUNT_SETUP_INCOMPLETE" }` — use the `error` field name to match all other error responses in `Login.cs` (e.g. `new { error = "Invalid email or password" }`).
  - Do not proceed to Identity lookup (no `AppIdentityUser` exists anyway).

**Frontend change to `LoginComponent`:**
All `LoginComponent` changes are consolidated here:

1. **`ACCOUNT_SETUP_INCOMPLETE` handling:** The existing error handler reads `err?.error?.error`. When this equals `"ACCOUNT_SETUP_INCOMPLETE"`, show inline message:
   > "Your account setup is incomplete. Check your email for a setup link, or [Resend setup email]."
   - [Resend setup email] calls `POST /api/signup/resend-setup { email }`.
   - On resend success: show "Setup email sent. Check your inbox."

2. **`hint=already-setup` query param:** On component init, read the `hint` query param. If `hint === 'already-setup'`, display a banner: *"Your account is already set up. Please sign in."* This is set by `SetPasswordComponent` when it receives a 409 from `set-password`.

---

## New Backend Endpoints

All three handlers follow the project's Vertical Slice / MediatR pattern: each is a `IRequest<Result<TResponse>>` query or command with a corresponding `Handler` class in its own file, registered via `IEndpointRouteBuilder` extensions in `SignupEndpoints.cs`.

### `GET /api/signup/session/{sessionId}`

- **Auth:** Anonymous
- **Rate limit:** 10 requests per minute per IP (same middleware as forgot-password)
- **Query:** `GetSignupSessionStatus.Query`
- **Logic:**
  1. Instantiate `new Stripe.Checkout.SessionService()` directly (consistent with `CreateCheckoutSession.cs` and `CreateBillingPortalSession.cs` — no DI abstraction) and call `.GetAsync(sessionId)`
  2. If Stripe throws `StripeException` with `HttpStatusCode.NotFound` → return 404
  3. Verify `session.PaymentStatus == "paid"` or `session.Status == "complete"` — 400 otherwise
  4. Extract customer email from `session.CustomerDetails.Email`
  5. Check if `UserEntity` exists with that email and `IdentityUserId.StartsWith("pending|")`
  6. Return `{ provisioned: bool }` — email is NOT returned (prevents email enumeration)
- **Errors:**
  - 404: Session not found on Stripe
  - 400: Session not in completed state

### `POST /api/signup/set-password`

- **Auth:** Anonymous
- **Body:** `{ sessionId: string, password: string }`
- **Command:** `SetInitialPassword.Command`
- **Logic:**
  1. Validate: sessionId and password required; password meets strength rules
  2. Retrieve checkout session from Stripe → extract email
  3. Find `UserEntity` by email with `pending|` prefix — 400 "No pending account found" if not found
  4. If `AppIdentityUser` already exists for this email → 409 "Account already set up"
  5. Create `AppIdentityUser` with provided password, `EmailConfirmed = true`
  6. Update `UserEntity.IdentityUserId` to new Identity user ID
  7. Generate JWT (15 min), generate refresh token, set HttpOnly cookie using same options as `Login.cs`
  8. Return `{ accessToken: string }`
- **Note:** `DisplayName` is not accepted here — it is already stored on `UserEntity` from Stripe metadata during webhook provisioning. No need for the user to re-enter it.

### `POST /api/signup/resend-setup`

- **Auth:** Anonymous
- **Body:** `{ email: string }`
- **Command:** `ResendSetupEmail.Command`
- **Rate limit:** Same policy as forgot-password (privacy-safe, returns 200 regardless)
- **Logic:**
  1. Find `UserEntity` by email with `IdentityUserId.StartsWith("pending|")`
  2. If not found: return 200 silently (no information leak)
  3. Send setup email with CTA link: `{baseUrl}/signup/set-password?session={user.StripeSessionId}`
  4. Return 200

---

## Changes to Existing Code

| File | Change |
|---|---|
| `CreateCheckoutSession.cs` | Change success URL to `/signup/set-password?session={CHECKOUT_SESSION_ID}` |
| `StripeWebhookHandler.cs` | Remove `AppIdentityUser` creation and temp password; add `sessionId` parameter; store `StripeSessionId` on `UserEntity`; send setup email instead of reset link |
| `SignupEndpoints.cs` | Pass `session.Id` into `HandleCheckoutCompleted` call; register the three new endpoint groups |
| `UserEntity` | Add `string? StripeSessionId` — requires EF Core migration |
| `EmailTemplates.cs` | Replace `Welcome` email template — remove "Sign in with Google" copy; add new `AccountSetup` template with CTA link to `/signup/set-password?session={sessionId}` |
| `Login.cs` | Before Identity lookup: check `UserEntity` by email for `pending|` prefix → return 400 `{ error: "ACCOUNT_SETUP_INCOMPLETE" }` |
| `LoginComponent` (Angular) | Handle `ACCOUNT_SETUP_INCOMPLETE` error string + `hint=already-setup` query param (see Path B section) |
| `signup-success.component.ts` | Delete file |
| `app.routes.ts` | Add `/signup/set-password` route; remove `/signup/success` route |

---

## New Frontend Component

**`SetPasswordComponent`** — standalone, `ChangeDetectionStrategy.OnPush`, signals-first.

| State (signal) | UI |
|---|---|
| `provisioning` | Spinner + "Setting up your account…" |
| `ready` | Password + Confirm password form, Submit button |
| `submitting` | Submit button in loading state, form disabled |
| `timeout` | Error: "Something went wrong…" + contact support mailto |
| `error` | API error message |

**Polling constants (named, not magic numbers):**
```typescript
const POLL_INTERVAL_MS = 2000;
const POLL_TIMEOUT_MS = 30_000;
```

**Lifecycle:**
- `ngOnInit` / `effect()`: read `session` query param; if absent → `router.navigate(['/signup'])`.
- Start polling `GET /api/signup/session/{session}` immediately.
- On `provisioned=true`: set state to `ready`.
- On elapsed > `POLL_TIMEOUT_MS`: set state to `timeout`.
- On submit: call `POST /api/signup/set-password`, store token, call `authService.loadProfile()`, `router.navigate(['/admin'])`.
- On 409: `router.navigate(['/login'], { queryParams: { hint: 'already-setup' } })`.
- **Polling teardown:** Use `DestroyRef` (injected via `inject(DestroyRef)`) and `takeUntilDestroyed` operator to cancel the polling interval when the component is destroyed. This prevents errors from polling into a destroyed component after navigation.

---

## Data Model Change

```csharp
// UserEntity
public string? StripeSessionId { get; set; }
```

One EF Core migration required.

---

## Security Notes

- Backend never trusts client-supplied email — always re-fetches from Stripe API using the session ID.
- Email confirmation is intentionally skipped: Stripe verified the address during checkout.
- `GET /api/signup/session/{id}` returns only `{ provisioned: bool }` — no email in the response, preventing enumeration.
- `resend-setup` returns 200 unconditionally regardless of whether the email exists.
- `set-password` is idempotent-safe: 409 if `AppIdentityUser` already exists.
- All anonymous endpoints are rate-limited.
- Refresh token cookie on `set-password` uses the same `SameSite`, `Secure`, and `Path` settings as `Login.cs`.
