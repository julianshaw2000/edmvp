# auditraks — QA Test Walkthrough

**Purpose:** End-to-end functional test coverage for the auditraks platform. Designed for a tester who has no prior knowledge of the application.

**Estimated time:** 3–4 hours for all 20 tests

**Last updated:** 2026-03-24

---

## Prerequisites

- Browser: Chrome recommended (also test in Firefox or Safari if available)
- App URL: `https://auditraks.com`
- An email account for testing — **do not use** `julianshaw2000@gmail.com` (that is the platform admin account)
- A second email address you can receive mail at (for invite testing)
- Stripe test card: `4242 4242 4242 4242` — expiry `12/28`, CVC `123`, postcode `12345`
- Optional: a mobile phone for Test 20

> **Note on cold starts:** The app is hosted on Render. If it has not been accessed recently, the first request may take 20–30 seconds to respond. This is normal — wait for the page to load before concluding it is broken.

---

## Test 1: Landing Page

**Goal:** Verify the landing page loads correctly and all navigation links work.

**Steps:**

1. Open `https://auditraks.com` in a fresh browser window.
2. Wait for the page to fully load.
3. Check that the following are visible:
   - A hero section with a headline and call-to-action
   - Three feature cards (look for compliance, tracking, and reporting content)
   - A pricing section showing two plans: **Starter ($99/month)** and **Pro ($249/month)**
   - A footer
4. Click the **Login** button (top-right or in the navigation).
5. Verify: you are taken to the login page (URL should change to `/login`).
6. Use the browser back button to return to the landing page.
7. Click **Start Free Trial** (or the equivalent call-to-action button in the hero).
8. Verify: you are taken to the signup page (URL should contain `/signup`).

**Expected result:** Landing page loads with all sections visible. Both navigation links route correctly. Page is functional on a desktop viewport.

**Also check (responsive):** Resize the browser to a narrow width (or use Chrome DevTools mobile emulation). Verify the layout adjusts — navigation should collapse to a menu icon, text should not overflow.

---

## Test 2: Self-Service Signup (Starter Plan)

**Goal:** Verify a new organization can sign up and complete the Stripe checkout flow.

**Steps:**

1. Navigate to `https://auditraks.com/signup?plan=starter`
2. Verify: the form shows **Starter** as the selected plan.
3. Fill in the form:
   - **Company Name:** `QA Test Company`
   - **Your Name:** `QA Tester`
   - **Email:** use a real email address you can access (this becomes the Tenant Admin account)
   - **Confirm Email:** enter the same email again
4. Click **Start 60-day free trial**.
5. Verify: you are redirected to a Stripe Checkout page (Stripe-hosted, URL starts with `checkout.stripe.com`).
6. On the Stripe checkout page, enter:
   - Card number: `4242 4242 4242 4242`
   - Expiry: `12/28`
   - CVC: `123`
   - Postcode/ZIP: `12345`
   - Cardholder name: `QA Tester` (if prompted)
7. Click the payment/confirm button.
8. Verify: you are redirected back to a success page on the auditraks domain.
9. Verify: the success page shows a confirmation message such as "Your account is being set up" or "Account created" and includes a sign-in button.

**Expected result:** Form submission redirects to Stripe, payment completes with the test card, success page loads with a sign-in prompt.

**What to check if it fails:**
- If the form shows an email mismatch error, verify both email fields are identical.
- If Stripe rejects the card, confirm you used `4242 4242 4242 4242` (Stripe's standard test approval card).
- If redirected back to the app with an error, note the error message and URL for the bug report.

---

## Test 3: First Login as Tenant Admin

**Goal:** Verify login works, the correct dashboard loads, and the onboarding wizard appears on first login.

**Steps:**

1. From the success page, click the sign-in button (or navigate to `/login`).
2. Enter the email address you used to sign up in Test 2 and the password you set during account setup.
3. Click **Sign In**.
4. Verify: you are redirected to the Admin Dashboard at `/admin`.
5. Verify: a **Getting Started wizard** or onboarding overlay appears. It should have 4 steps:
   - Welcome
   - Invite your team
   - Create your first batch
   - Run compliance checks
6. Click through each step using the Next button or numbered step indicators.
7. Click **Dismiss** or **Done** to close the wizard.
8. Refresh the page.
9. Verify: the wizard does **not** reappear after dismissal.
10. Verify the dashboard shows:
    - A status banner: amber, showing **"Trial — ~60 days remaining"** with a **Manage Billing** button
    - Three metric cards: **Users**, **Batches**, **Flags**
    - A Quick Actions grid with cards for Manage Users, Audit Log, Analytics, API Keys, and Webhooks

**Expected result:** Login succeeds, Admin Dashboard loads with trial banner, onboarding wizard shows then stays dismissed after refresh.

---

## Test 4: Invite a Supplier User

**Goal:** Verify a Tenant Admin can invite a new user and the invitation is recorded.

**Steps:**

1. From the Admin Dashboard, click **Manage Users** (either on the Quick Actions grid or the navigation).
2. Verify: the Users list loads. It may show only the current admin user initially.
3. Click **Invite User**.
4. Fill in:
   - **Email:** a second email address you have access to (e.g., a Gmail alias or another account)
   - **Display Name:** `Test Supplier`
   - **Role:** `Supplier`
5. Click **Send Invite** or **Send Invitation**.
6. Verify: the user appears in the users list with a status such as "Pending" or "Invited".
7. (Optional) Check the inbox of the invited email address. Look for an invitation email from auditraks. Note: if the email service is in dev/log mode, the email may not actually arrive — this is acceptable for the invite flow test.

**Expected result:** Invite form submits successfully, new user entry appears in the users list.

---

## Test 5: Supplier Portal — Create a Batch

**Goal:** Verify a user can create a new mineral batch.

**Note:** If testing alone (without a second account), you can navigate directly to `/supplier` while logged in as the Tenant Admin. The admin role may allow access for demo/testing purposes.

**Steps:**

1. Navigate to `/supplier` (either by logging in as the invited Supplier user, or by navigating directly as admin).
2. Verify: the Supplier Dashboard loads, showing existing demo batches or an empty state.
3. Click **New Batch**.
4. Fill in the batch creation form:
   - **Batch Number:** `TEST-001`
   - **Mineral Type:** `Tungsten (Wolframite)`
   - **Origin Country:** `RW` (Rwanda)
   - **Mine Site:** `Test Mine`
   - **Initial Weight (kg):** `500`
5. Click **Create Batch**.
6. Verify: you are redirected to the batch detail view, or the batch appears on the dashboard.
7. On the dashboard, find batch `TEST-001` and verify:
   - Compliance status badge shows **PENDING** (grey)
   - Origin country shows Rwanda
   - Weight shows 500

**Expected result:** Batch created successfully, visible on the dashboard with PENDING compliance status.

---

## Test 6: Log Custody Events

**Goal:** Verify events can be submitted and the SHA-256 hash chain is displayed correctly.

**Steps:**

**Event 1 — Mine Extraction:**

1. Open batch `TEST-001` from the Supplier Dashboard.
2. Click **Add Event** (or **Log Event**).
3. Select event type: **Mine Extraction**.
4. Fill in the fields:
   - **GPS Coordinates:** `-1.9441, 30.0619`
   - **Mine Operator:** `Test Operator Ltd`
   - **Mineralogical Certificate Reference:** `CERT-TEST-001`
5. Click **Submit Event**.
6. Verify: the event appears in the **Events** tab on the batch detail page.
7. Verify: the event entry shows a SHA-256 hash value (a long hexadecimal string).

**Event 2 — Laboratory Assay:**

8. Click **Add Event** again.
9. Select event type: **Laboratory Assay**.
10. Fill in the fields:
    - **Laboratory Name:** `Test Lab Kigali`
    - **Method:** `XRF`
    - **Tungsten Content (%):** `72`
    - **Certificate Reference:** `LAB-TEST-001`
11. Click **Submit Event**.
12. Verify: the second event appears in the timeline.
13. Verify: the second event shows its own SHA-256 hash.
14. Verify: the second event also shows a **previous event hash** or chain linkage indicator (confirming it references the first event's hash).

**Expected result:** Two events visible in the timeline. Each event has a SHA-256 hash. The second event references the first event's hash, demonstrating the cryptographic chain.

---

## Test 7: Compliance Checks

**Goal:** Verify that compliance checks are triggered automatically and display results correctly.

**Steps:**

1. On batch `TEST-001`, click the **Compliance** tab (or **Compliance Checks** tab).
2. If the checks have not yet run, wait 10–30 seconds and refresh the page. Compliance checks run asynchronously in the background worker.
3. Verify: at least some compliance checks appear. You should see entries for:
   - **RMAP Smelter Verification** — may show as Inconclusive (no smelting event yet)
   - **OECD DDG Origin Country Risk** — Rwanda (RW) should **PASS** (not a high-risk origin)
   - **Sanctions Screening** — should PASS for Rwanda
   - **Mass Balance** — may show Inconclusive until a Concentration event is added
   - **Event Sequence Integrity** — should PASS (events are in chronological order)
4. Each check should show: check name, result (Pass/Fail/Inconclusive), the rule evaluated, and any notes.

**Bonus — observe a flag:**

5. Navigate to the demo batch `W-2026-038` (from the pre-seeded demo data — visible on the dashboard).
6. Open its Compliance tab.
7. Verify: at least one check shows **FAIL** or **FLAGGED** — the DRC origin should trigger an OECD DDG failure.

**Expected result:** Compliance checks appear for TEST-001 with appropriate results. Demo batch W-2026-038 shows a compliance failure.

---

## Test 8: Batch Activity Feed

**Goal:** Verify the activity feed shows a chronological log of batch actions.

**Steps:**

1. Open batch `TEST-001`.
2. Click the **Activity** tab.
3. Verify: the feed is populated with entries, including:
   - Batch created (timestamped at creation time)
   - Each event submission (Mine Extraction, Laboratory Assay)
   - Compliance check runs
4. Verify each entry shows:
   - User name or email
   - Action description
   - Timestamp
5. Confirm the entries are in chronological order (oldest at the bottom, newest at the top — or verify the ordering is consistent).

**Expected result:** Activity feed shows all recent actions on the batch with correct metadata.

---

## Test 9: Buyer Portal — View Batches

**Goal:** Verify the Buyer Portal loads correctly and batch browsing works.

**Steps:**

1. Navigate to `/buyer` (log in as an invited Buyer user, or navigate directly if testing as admin).
2. Verify: the Buyer Dashboard loads with:
   - Summary metrics at the top: total batches, compliant count, flagged count, insufficient data count
   - A sortable batch table below
3. Test the search: type `W-2026` in the search box — verify the table filters to matching batch numbers.
4. Test the compliance filter: select **COMPLIANT** from the dropdown — verify only compliant batches appear.
5. Test the date filter: set a From date of one month ago — verify the table updates.
6. Click **Clear** to reset all filters.
7. Click any batch row to open the Batch Detail view.
8. Verify the detail view has these tabs:
   - **Events** (or Overview/Events)
   - **Compliance Checks** (or Compliance)
   - **Documents**
   - **Activity**
   - **Generate & Share**
9. Click each tab and verify it loads without errors.

**Expected result:** Buyer Dashboard loads with correct metrics, filters work, batch detail shows all tabs.

---

## Test 10: Generate Material Passport

**Goal:** Verify a Material Passport can be generated and downloaded as a PDF.

**Steps:**

1. In the Buyer Portal, open a batch that has **COMPLIANT** status — use demo batch `W-2026-041` if your test batch is not yet compliant.
2. Click the **Generate & Share** tab.
3. Click **Generate Material Passport**.
4. Wait for the generation to complete (may take 5–15 seconds — a spinner or progress indicator should appear).
5. When ready, click **Download**.
6. Verify: a PDF file downloads to your computer.
7. Open the PDF and verify it contains:
   - Batch number and mineral type
   - Origin country and mine site
   - Summary of custody chain events
   - Compliance status (RMAP, OECD DDG results)
   - Hash chain integrity status
   - A QR code
   - Generation timestamp and generating user name

**Expected result:** PDF downloads successfully and contains all required sections including a QR code.

**Note:** If the **Generate Material Passport** button is greyed out, the batch is not COMPLIANT. Switch to batch `W-2026-041` from the demo data which should be seeded as compliant.

---

## Test 11: Share Document

**Goal:** Verify a generated document can be shared via a time-limited link accessible without login.

**Steps:**

1. After generating the Material Passport in Test 10, click **Share** (next to the passport in the Generate & Share tab).
2. Verify: auditraks generates a unique URL and displays it in a confirmation box.
3. Click **Copy** to copy the link to the clipboard.
4. Open a new **incognito/private browser window** (where you are not logged in to auditraks).
5. Paste the link and navigate to it.
6. Verify: the shared document page loads **without** requiring login.
7. Verify: the page shows the Material Passport content (batch details, compliance status).
8. Verify: the URL is in the format `/shared/{token}`.

**Expected result:** Share link generated, document viewable without authentication in an incognito window.

---

## Test 12: Public Verification

**Goal:** Verify the public batch verification page works without any login.

**Steps:**

1. Open a new incognito/private browser window.
2. Navigate to: `https://auditraks.com/verify/W-2026-041`
3. Verify: the page loads without requiring a login.
4. Verify: the page displays:
   - Batch number (`W-2026-041`)
   - Mineral type
   - Origin country
   - Overall compliance status (should show COMPLIANT for this demo batch)
   - Date the compliance status was last updated
   - Which compliance frameworks were evaluated
5. Verify: the page does **not** display detailed event data or commercially sensitive information.
6. **Test the QR code path:** If you downloaded a Material Passport PDF in Test 10, scan the QR code with a mobile phone camera. Verify it opens the correct `/verify/{batchId}` page.

**Expected result:** Public verification page loads without authentication and shows compliance status. QR code from the PDF links to the same page.

---

## Test 13: Audit Log

**Goal:** Verify the audit log records all platform actions, filters work, and CSV export is functional.

**Steps:**

1. Navigate to the Admin Dashboard at `/admin`.
2. Click **Audit Log**.
3. Verify: a table loads with columns including **Timestamp**, **User**, **Action**, **Entity Type**, **Result**.
4. Verify: entries are present for the actions you performed in earlier tests (batch creation, event submissions, etc.).
5. **Test filtering by action:**
   - Use the **Action** filter dropdown — select `Batch Created`.
   - Verify: only batch creation entries appear.
   - Clear the filter.
6. **Test filtering by entity type:**
   - Use the **Entity Type** filter — select `Custody Event`.
   - Verify: only event-related entries appear.
   - Clear the filter.
7. **Expand a row:**
   - Click any row in the table.
   - Verify: a detail panel or modal expands showing additional information — entity ID, IP address, and the full event payload JSON.
8. **Export CSV:**
   - Click **Export CSV**.
   - Verify: a file named `audit-log.csv` (or similar) downloads.
   - Open the CSV in a spreadsheet application or text editor.
   - Verify: the columns match what is shown in the table and the data is not garbled.

**Expected result:** Audit log populated with actions from earlier tests. Filters narrow results correctly. Row expand shows payload. CSV downloads with correct data.

---

## Test 14: Analytics Dashboard

**Goal:** Verify all analytics charts and metrics render correctly.

**Steps:**

1. Navigate to **Admin > Analytics** (or click the Analytics card on the Admin Dashboard).
2. Verify the following **metric cards** are visible with numbers (may be zero if minimal activity, but cards must render):
   - Total Batches
   - Completed Batches
   - Flagged Batches
   - Active Users
   - Total Custody Events
   - Pending Compliance
3. Verify the following **charts** render:
   - **Compliance Breakdown** — horizontal bars showing percentage breakdown of COMPLIANT / FLAGGED / PENDING batches, with summary pills
   - **Monthly Batch Activity** — bar chart showing batch creation volume over recent months
   - **Mineral Distribution** — breakdown by mineral type (Tungsten, Tin, etc.)
   - **Top Origin Countries** — ranked list of origin countries
4. Verify: no charts show a blank/empty error state — they should either show data or a "no data yet" placeholder message.
5. Verify: the numbers in the metric cards are consistent with activity across the app (e.g., Total Batches should include your TEST-001 batch).

**Expected result:** All six metric cards and four charts render without JavaScript errors. Data is consistent with platform activity.

---

## Test 15: API Key Management

**Goal:** Verify API keys can be created, used for authentication, and revoked.

**Steps:**

1. Navigate to **Admin > API Keys**.
2. Click **Create API Key**.
3. Enter name: `QA Test Key`
4. Click **Create**.
5. Verify: the full API key is displayed **once** in an amber or highlighted box. It should start with `at_`.
6. Copy the key immediately. **You will not see the full key again.**
7. Verify: the key appears in the key table with only the prefix shown (e.g., `at_3f8a...`) — not the full key.
8. Verify: the Status column shows **Active** (green).

**Test the key with a live request:**

9. Open a terminal and run:
   ```
   curl -H "X-API-Key: at_YOUR_FULL_KEY_HERE" https://api.auditraks.com/api/batches
   ```
   (Replace `at_YOUR_FULL_KEY_HERE` with the actual key you copied.)
10. Verify: the response is JSON containing batch data (not a 401 or 403 error).

**Test revocation:**

11. Return to the Admin Dashboard → API Keys.
12. Find `QA Test Key` in the table and click **Revoke**.
13. Confirm the revocation if prompted.
14. Verify: the key's Status changes to **Revoked** (grey) in the table.
15. Run the same curl command again with the now-revoked key.
16. Verify: the response is a **401 Unauthorized** error.

**Expected result:** Key creation shows full key once, key prefix shown in table, live request succeeds, revoked key receives 401.

---

## Test 16: Webhook Management

**Goal:** Verify webhooks can be configured and fire correctly when platform events occur.

**Preparation:** Go to `https://webhook.site` in a separate browser tab and copy the unique URL shown (format: `https://webhook.site/xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`). This is a free service that captures incoming HTTP requests.

**Steps:**

1. Navigate to **Admin > Webhooks** (if visible in the admin navigation).
2. Click **Create Endpoint** (or equivalent).
3. Enter:
   - **URL:** the webhook.site URL you copied
   - **Events:** select `*` (all events), or select at least `batch.created` and `event.submitted`
4. Save the endpoint.
5. Verify: the endpoint appears in the webhooks list with an Active status.

**Trigger a webhook:**

6. Navigate to `/supplier` and create a new batch (e.g., Batch Number: `TEST-WH-001`, same details as Test 5).
7. Switch to the webhook.site browser tab.
8. Verify: within 5–15 seconds, a new incoming request appears on webhook.site.
9. Inspect the request:
   - Method: `POST`
   - Headers: should include `X-Webhook-Signature` or `X-Signature`
   - Body: JSON payload describing the event (batch created, entity ID, timestamp, etc.)

**Expected result:** Webhook endpoint saved, batch creation triggers a POST to webhook.site with a signed payload.

**Note:** If the Webhooks section is not visible in the admin navigation, record this as an observation (it may be present but navigated differently).

---

## Test 17: Billing Portal

**Goal:** Verify the Stripe Customer Portal loads and shows the correct subscription information.

**Steps:**

1. Log in as the Tenant Admin created in Test 2.
2. Navigate to the Admin Dashboard at `/admin`.
3. Locate the trial status banner (amber, showing "Trial — X days remaining").
4. Click **Manage Billing**.
5. Verify: you are redirected to a Stripe-hosted Customer Portal page (URL: `billing.stripe.com` or similar).
6. On the Stripe portal, verify:
   - Your subscription plan is shown (Starter or Pro)
   - Billing period is displayed
   - Payment method section shows the test card ending in `4242`
   - Invoice history section is present
7. Do **not** cancel the subscription during this test unless you are specifically testing cancellation.
8. Use the browser back button or the Stripe portal's back link to return to the auditraks app.

**Expected result:** Stripe Customer Portal loads with correct plan details and payment method. Navigation back to the app works.

---

## Test 18: Tenant Suspension (Platform Admin Only)

**Goal:** Verify the Platform Admin can suspend a tenant, which blocks access for that tenant's users, and reactivation restores access.

**Requirements:** Access to the platform admin account (`julianshaw2000@gmail.com`). Use a separate browser profile or incognito window for each account.

**Steps:**

**Browser A — Platform Admin:**

1. Log in as `julianshaw2000@gmail.com` with email and password.
2. Verify: you land on the Admin Dashboard with Platform Admin access.
3. Navigate to **Tenants** (in the admin navigation or a dedicated Platform Admin section).
4. Find the test tenant created in Test 2 (`QA Test Company`).
5. Click **Suspend** on that tenant.
6. Confirm the suspension.
7. Verify: the tenant's status updates to **Suspended** in the tenant list.

**Browser B — Tenant User:**

8. In a separate incognito window (or different browser profile), log in as the Tenant Admin from Test 2.
9. Verify: instead of the Admin Dashboard, the user sees a suspension message such as "Your organization's account has been suspended" or an access-denied page.
10. Verify: the user cannot navigate to any app page.

**Browser A — Reactivate:**

11. Back in the Platform Admin window, find the suspended tenant.
12. Click **Reactivate**.
13. Confirm reactivation.

**Browser B — Verify access restored:**

14. In the Tenant Admin window, refresh the page.
15. Verify: normal access is restored — the Admin Dashboard loads.

**Expected result:** Suspension immediately blocks tenant user access. Reactivation restores access without requiring the user to log out and back in.

---

## Test 19: Plan Limits (if configured)

**Goal:** Verify that plan limits (max batches, max users) are enforced when configured.

**Note:** If all plan limits are set to `null` (unlimited) in the test environment, skip this test and mark it as N/A.

**Steps — Batch limit:**

1. On the Admin Dashboard, check if a batch limit is visible (may appear in the plan status banner or in settings).
2. If a batch limit exists (e.g., Starter plan: 50 batches):
   - Create batches until the limit is reached.
   - Attempt to create one more batch.
   - Verify: the system rejects the creation with an error message such as "Batch limit reached" or "You have reached the maximum number of batches for your plan."

**Steps — User limit:**

3. If a user limit exists:
   - Invite users until the limit is reached.
   - Attempt to invite one more user.
   - Verify: the invite fails with an error message such as "User limit reached."

**Expected result:** Limits enforced at the boundary with a clear, user-readable error message. The 401st batch (if limit is 400) should fail, the 400th should succeed.

---

## Test 20: Mobile and PWA

**Goal:** Verify the app is usable on mobile and can be installed as a Progressive Web App.

**Steps:**

**Mobile browser test:**

1. Open `https://auditraks.com` on a mobile phone (iOS Safari or Android Chrome).
2. Verify the layout is responsive:
   - Navigation adapts to mobile (hamburger menu or bottom navigation)
   - Text is readable without horizontal scrolling
   - Buttons are tappable (not too small)
3. Log in using email and password on mobile.
4. Navigate to the Supplier Dashboard and verify batch cards display correctly.
5. Open a batch and verify the Events tab, Compliance tab, and Activity tab are accessible and usable.
6. Attempt to log a custody event on mobile — verify the form is usable on a small screen.

**PWA install test (Android Chrome):**

7. On Android Chrome, tap the browser menu (three dots, top-right).
8. Look for **Add to Home Screen** or **Install App**.
9. Tap it and confirm the installation.
10. Return to the Android home screen and find the auditraks icon.
11. Tap the icon to launch the app.
12. Verify: the app launches in standalone mode — no browser address bar or browser chrome is visible.
13. Test basic navigation: login, view batches, view a batch detail.

**PWA install test (iOS Safari):**

7. On iOS Safari, tap the Share button (box with arrow).
8. Select **Add to Home Screen**.
9. Confirm the name and tap **Add**.
10. Return to the home screen and tap the auditraks icon.
11. Verify: the app launches in standalone mode.

**Expected result:** App is fully usable on mobile. Installable as PWA on Android (and iOS if tested). Standalone launch works without browser chrome.

---

## Bug Report Template

For any issue found during testing, file a bug report using this format:

```
**Test:** [Test number and name, e.g., Test 6: Log Custody Events]

**Steps to reproduce:**
1. ...
2. ...
3. ...

**Expected:** [What should have happened]
**Actual:** [What actually happened]

**Screenshot / recording:** [Attach if applicable]

**Browser:** [e.g., Chrome 124 on Windows 11]
**Device:** [e.g., Desktop / iPhone 15 iOS 17.4]

**Severity:** [Critical / High / Medium / Low]
- Critical: Blocks core workflow (cannot create batch, cannot log in)
- High: Major feature broken but workaround exists
- Medium: Feature degraded, cosmetic or minor data issue
- Low: UI polish, copy error, minor layout issue
```

---

## Test Summary Checklist

Fill in the Pass/Fail column and add notes for any failures or observations.

| Test | Feature | Pass / Fail | Notes |
|------|---------|-------------|-------|
| 1 | Landing page — load and navigation | | |
| 2 | Self-service signup — Stripe checkout | | |
| 3 | First login — admin dashboard and onboarding wizard | | |
| 4 | Invite user — supplier role | | |
| 5 | Create batch — supplier portal | | |
| 6 | Log custody events — hash chain | | |
| 7 | Compliance checks — automatic triggers | | |
| 8 | Batch activity feed | | |
| 9 | Buyer portal — browse and filter batches | | |
| 10 | Generate Material Passport — PDF download | | |
| 11 | Share document — link without login | | |
| 12 | Public verification — QR and URL | | |
| 13 | Audit log — filters and CSV export | | |
| 14 | Analytics dashboard — all charts | | |
| 15 | API key — create, use, revoke | | |
| 16 | Webhooks — endpoint and payload | | |
| 17 | Billing portal — Stripe redirect | | |
| 18 | Tenant suspension and reactivation | | |
| 19 | Plan limits enforcement | | |
| 20 | Mobile and PWA install | | |

**Overall status:** Pass / Fail / Partial

**Tester name:**

**Date tested:**

**Environment:** `https://auditraks.com`

---

*auditraks QA Test Walkthrough — 2026-03-24*
