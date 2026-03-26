# auditraks — Tenant Admin Manual

**Audience:** Tenant Administrator (TENANT_ADMIN role)
**Platform:** auditraks — Mineral Supply Chain Compliance
**Version:** 1.0 — March 2026

---

## Table of Contents

1. [Welcome](#1-welcome)
2. [Getting Started](#2-getting-started)
3. [Managing Your Team](#3-managing-your-team)
4. [Audit Log](#4-audit-log)
5. [Analytics Dashboard](#5-analytics-dashboard)
6. [API Keys](#6-api-keys)
7. [Webhook Notifications](#7-webhook-notifications)
8. [Managing Your Subscription](#8-managing-your-subscription)
9. [Compliance Reporting](#9-compliance-reporting)
10. [FAQ](#10-faq)

---

## 1. Welcome

### What is auditraks?

auditraks is a mineral supply chain compliance platform. It tracks batches of raw minerals — tungsten, tin, tantalum, and gold — from mine to refinery. Every step a batch takes through the supply chain is recorded as a custody event with a cryptographic fingerprint, creating a tamper-evident chain of custody that buyers, suppliers, and auditors can independently verify.

The platform validates each batch against two internationally recognised compliance frameworks:

- **RMAP** (Responsible Minerals Assurance Process) — confirms smelters have been independently audited
- **OECD Due Diligence Guidance (DDG)** — flags material from conflict-affected and high-risk origin countries

When a batch passes all checks, a **Material Passport** can be generated — a PDF certificate with a QR code that any downstream customer or regulator can scan to verify compliance status.

### What you can do as a Tenant Admin

As a Tenant Admin, you are the account owner for your organisation on auditraks. You have full visibility across everything your team does on the platform, plus management capabilities your team members do not have.

**Your responsibilities typically include:**

- Inviting and managing your Supplier and Buyer users
- Monitoring compliance status across all batches
- Reviewing the audit log for regulatory reporting
- Creating API keys for integrations
- Managing your subscription and billing

You do not create batches or log custody events yourself — those are Supplier functions. You also cannot generate Material Passports — that is a Buyer function. Your role is oversight, administration, and compliance governance.

### Quick overview of the platform

auditraks has three portals, each designed for a different role:

| Portal | URL | Who uses it |
|---|---|---|
| Supplier Portal | `/supplier` | Your team members who create batches and log custody events |
| Buyer Portal | `/buyer` | Your team members who verify compliance and generate documents |
| Admin Dashboard | `/admin` | You (Tenant Admin) — user management, audit log, analytics, API keys, billing |

The platform also has a publicly accessible batch verification page at `https://auditraks.com/verify/{batchId}` — anyone can check the compliance status of a batch using its ID or by scanning the QR code on a Material Passport, without needing an account.

---

## 2. Getting Started

### First login after signup

When you sign up at [auditraks.com](https://auditraks.com), you enter your company name, your name, and your email address, then complete a Stripe checkout to activate your 60-day free trial. Your card is saved but not charged until the trial ends.

After checkout completes, you are taken to a signup success page. Click **Sign in** (or navigate to `/login`) and authenticate with:

- **Google** — click **Continue with Google** and sign in with the Google account matching your signup email
- **Email and password** — use the email address you signed up with

After authentication, auditraks detects your Tenant Admin role and sends you directly to the **Admin Dashboard** at `/admin`.

### The onboarding wizard (4 steps)

The first time you sign in after creating your account, a **Getting Started wizard** appears on the Admin Dashboard. It walks you through the key tasks to get your organisation operational.

The wizard has four steps:

**Step 1 — Welcome**
An overview of the platform and what you can do. Read through this to orient yourself.

**Step 2 — Invite your team**
This is where you add your first users. Invite at least one Supplier (someone who will create batches and log events) and at least one Buyer (someone who will review compliance and generate documents). See [Inviting Users](#inviting-users) for the full process.

**Step 3 — Create your first batch**
A prompt to navigate to the Supplier Portal and register your first mineral batch. The wizard links to `/supplier` so your invited Supplier can do this, or you can navigate there to explore the interface.

**Step 4 — Run compliance checks**
Compliance checks run automatically after each custody event is submitted — there is nothing to manually trigger. This step explains what to expect once your team starts logging events.

**Navigating the wizard:** Use the numbered dots at the top or the Previous/Next buttons to move between steps. Click **Dismiss** at any time to close the wizard — once dismissed, it will not reappear when you refresh or return to the dashboard.

> If you accidentally dismiss the wizard before completing setup, everything it covers is available in the Admin Dashboard. Use the Quick Actions grid (the cards in the centre of the dashboard) to navigate to each area.

### Understanding your dashboard

The Admin Dashboard at `/admin` is your home base. Here is what you see:

**Status banner (top of page)**
This shows your current subscription status:
- Amber banner: "Trial — X days remaining" with a **Manage Billing** button
- Green banner: "Pro Plan — Active" (once the trial converts to a paid subscription)

**Metric cards**
Three cards give you a quick read on activity across your organisation:
- **Users** — total number of registered users in your account
- **Batches** — total number of batches tracked (across all statuses)
- **Flags** — total number of active compliance flags requiring attention

**Quick Actions grid**
Cards that link directly to the main admin areas:
- Manage Users
- Audit Log
- Analytics
- API Keys
- Webhooks

Click any card to navigate to that section. You can also use the sidebar navigation.

### Trial status and plan info

Your 60-day free trial begins on the day you sign up. During the trial, all features are fully available.

The trial counter on the status banner counts down daily. When the trial ends, your subscription converts automatically to the plan you selected at signup and your card on file is charged.

You can upgrade, downgrade, or cancel at any time from the Stripe billing portal — see [Managing Your Subscription](#8-managing-your-subscription).

**Plan limits:**

| Plan | Price | Batch Limit | User Limit |
|---|---|---|---|
| Starter | $99/month | 50 batches | 5 users |
| Pro | $249/month | Unlimited | Unlimited |

If you are on the Starter plan and approach your batch or user limit, you will see an error when attempting to create a new batch or invite a new user. Upgrade to Pro from the billing portal to remove these limits.

---

## 3. Managing Your Team

Navigate to **Admin Dashboard > Manage Users**, or click the **Manage Users** card in the Quick Actions grid.

### Inviting Users

#### How to invite a Supplier user

A Supplier user is someone at your organisation who will create batch records and log custody events — typically a miner, trader, processor, or logistics coordinator.

1. On the Users page, click **Invite User**.
2. Enter their **email address**.
3. Enter a **display name** (their name as it will appear on audit records).
4. Set their **Role** to `Supplier`.
5. Click **Send Invitation**.

The user appears in your users list immediately with a status of **Pending** or **Invited**.

#### How to invite a Buyer user

A Buyer user is someone who needs to review compliance status and generate Material Passports — typically a procurement manager, compliance officer, or downstream customer representative.

Follow the same steps as above, but set the **Role** to `Buyer`.

#### What happens when you send an invite

auditraks sends the invited user an email containing an **activation link**. This link is valid for **7 days**.

The email prompts them to sign in to auditraks. On their first sign-in, they authenticate via Google or by setting up an email/password account — whichever they prefer. The platform automatically matches their email address to the pending invitation and links their account.

After their first successful sign-in, they are taken to the portal appropriate to their role:
- Supplier users go to `/supplier`
- Buyer users go to `/buyer`

> If the invitation email does not arrive, ask the user to check their spam or junk folder. If it is not there after a few minutes, return to the Users page and resend the invitation. If the link has expired (after 7 days), resend it — a new link is generated each time.

### Managing Existing Users

#### Viewing your user list

The Users page shows all users in your organisation in a table. Columns include:
- **Name** — display name
- **Email** — account email address
- **Role** — current role (Supplier or Buyer)
- **Status** — Active, Pending (invited but not yet signed in), or Deactivated
- **Last Active** — approximate date of last sign-in activity

Use the search box to find a specific user by name or email. Use the **Show deactivated users** toggle to include deactivated accounts in the list.

#### Changing a user's role (Supplier to Buyer or vice versa)

You may need to change a user's role if their responsibilities change — for example, if a Supplier user moves to a procurement role and needs Buyer access.

1. Click the user's name in the list to open their profile.
2. In the **Role** dropdown, select the new role.
3. Click **Save Changes**.

The role change takes effect immediately. On the user's next page load, they are redirected to the portal for their new role. They do not need to log out and back in.

#### Deactivating a user

If a team member leaves your organisation or no longer needs platform access, deactivate their account. Deactivation prevents them from signing in while preserving all records they created (batches, events, documents) exactly as they are — the audit trail remains complete.

1. Open the user's profile.
2. Click **Deactivate Account**.
3. Confirm the deactivation in the prompt that appears.

The user's status changes to **Deactivated** and they cannot sign in. All their records remain fully intact.

**To reactivate a user later:** Use the **Show deactivated users** toggle on the Users page to find them, open their profile, and click **Reactivate Account**.

#### Restrictions on role assignment

There are two restrictions you should be aware of:

- **You cannot assign the Tenant Admin or Platform Admin roles.** When inviting or editing users, only Supplier and Buyer are available as role options. If you need an additional Tenant Admin for your organisation, contact auditraks support.
- **You cannot edit other Tenant Admin users.** If another Tenant Admin exists on your account, their profile is read-only to you.

### Role Permissions

Here is a clear summary of what each role can do:

#### What Suppliers can do

- Create new mineral batches (batch number, mineral type, origin country, mine site, weight)
- Log custody events against batches (Mine Extraction, Laboratory Assay, Concentration, Trading/Transfer, Smelting, Export/Shipment)
- Upload supporting documents to batches and events (PDF, JPEG, PNG, TIFF, GIF — up to 25 MB per file)
- View the full event timeline and compliance status for any batch
- View the activity feed on each batch
- Split a batch into two child batches
- Update batch status (Created > Active > Completed)

**Suppliers cannot:** generate Material Passports or Audit Dossiers, share documents, or access the Admin Dashboard.

#### What Buyers can do

- View all batches and their compliance status
- Browse the full event timeline for any batch
- View compliance check results (RMAP, OECD DDG, Mass Balance, Sequence, Sanctions)
- Generate Material Passports as downloadable PDFs (for COMPLIANT batches only)
- Generate Audit Dossiers as downloadable PDFs
- Share Material Passports via time-limited secure links (valid 30 days, no login required for recipient)
- Download documents and files attached to batches
- View the activity feed on each batch

**Buyers cannot:** create batches, log events, upload documents, or access the Admin Dashboard.

#### What you (Tenant Admin) can do

Everything above, plus:

- Invite users and manage their roles and access
- View the full audit log of every action taken on the platform
- Access the analytics dashboard (compliance trends, mineral distribution, monthly activity, origin countries)
- Create and revoke API keys for programmatic integrations
- Configure webhook notifications for external systems
- Manage billing via the Stripe portal (view invoices, update payment method, change plan, cancel)
- Monitor trial status and days remaining

---

## 4. Audit Log

Navigate to **Admin Dashboard > Audit Log**, or click the **Audit Log** card.

### What gets logged

The audit log records every significant action taken on the platform by any user in your organisation. This includes:

- Batch creation, status changes, and splits
- Custody event submissions
- Document uploads
- Compliance check runs and results
- User invitations, role changes, and deactivations
- API key creation and revocations
- Material Passport and Audit Dossier generation
- Document share link creation
- Sign-in events

Every entry records the timestamp, the acting user, the action taken, the type of entity affected, and whether the action succeeded or failed.

### Viewing the audit log

The audit log table has the following columns:

| Column | Description |
|---|---|
| **Timestamp** | Date and time the action occurred (UTC) |
| **User** | Email address of the user who performed the action |
| **Action** | The specific action taken (e.g., "Batch Created", "Event Submitted") |
| **Entity Type** | The type of record affected (Batch, Custody Event, Document, User, etc.) |
| **Result** | Success (green) or Failure (red) |

The log is paginated at 20 entries per page. Use the **Previous** and **Next** buttons at the bottom to navigate.

### Filtering by action, entity type, date range

Three filter controls sit above the table:

- **Action** dropdown — filter to a specific action type (e.g., show only "Batch Created" entries)
- **Entity Type** dropdown — filter to a specific entity category (e.g., show only "Custody Event" entries)
- **Result** dropdown — filter to show only successes, only failures, or both

For date range filtering: if you need entries from a specific period, use the filter dropdowns in combination. The entries are ordered by timestamp descending (newest first), so you can also paginate to find older records.

To clear all filters and return to the full log, select the blank/default option in each dropdown.

### Expanding entries to see payload details

Click any row in the audit log table to expand it and see full details:

- **Entity ID** — the unique ID of the affected record (useful for cross-referencing)
- **IP Address** — the IP address of the request (for security auditing)
- **Failure Reason** — if the Result was Failure, a description of what went wrong
- **Full Payload** — the complete JSON data that was submitted with the request

The payload detail is particularly useful when reviewing what data was submitted for a specific event or document upload.

### Exporting to CSV for compliance reporting

Click **Export CSV** to download the current view as a CSV file named `audit-log.csv`. The export reflects whatever filters you currently have active — so if you want to export only compliance-related actions for a specific month, apply those filters first, then export.

The CSV contains the same columns as the on-screen table and can be opened in Microsoft Excel, Google Sheets, or any spreadsheet application. This file is suitable for submission to auditors and compliance reviewers.

### How long audit data is retained

Audit log data is retained **indefinitely**. Nothing is automatically deleted. You can query and export the full history of your organisation's activity at any time.

---

## 5. Analytics Dashboard

Navigate to **Admin Dashboard > Analytics**, or click the **Analytics** card.

The Analytics page gives you a visual summary of supply chain activity across your entire organisation. It is designed for management reporting and trend analysis — not for reviewing individual batches (use the Supplier or Buyer portals for that).

### Understanding the metrics

Six metric cards appear at the top of the Analytics page:

| Metric | What it means |
|---|---|
| **Total Batches** | Total batches created in your organisation, regardless of status |
| **Completed Batches** | Batches that have been marked as Completed (fully through the supply chain) |
| **Flagged Batches** | Batches with at least one active compliance failure |
| **Active Users** | Number of users who have signed in and performed actions recently |
| **Total Custody Events** | Total number of custody events logged across all batches |
| **Pending Compliance** | Batches awaiting compliance checks (not yet triggered, or checks queued) |

A high Flagged Batches number relative to Total Batches is an indicator that your team needs to resolve compliance issues — perhaps incorrect Smelter IDs, unresolved origin country flags, or data entry errors on event weights.

### Compliance breakdown

The **Compliance Breakdown** chart shows horizontal progress bars for each compliance status category (Compliant, Flagged, Pending, Insufficient Data), along with the percentage of total batches in each category.

Summary pills above the chart show the exact counts at a glance. Use this view when reporting overall compliance health to management or when preparing for an external audit — "X% of our batches are currently compliant" is a simple metric that communicates your programme's effectiveness.

### Mineral distribution chart

The **Mineral Distribution** chart breaks down your batches by mineral type (Tungsten Wolframite, Tungsten Cassiterite, Tin, Tantalum Coltan, Tantalum Tantalite, Gold Alluvial, Gold Hard Rock).

This is useful for understanding your supply chain composition and for preparing commodity-specific reports — for example, if your compliance programme is audited specifically for tungsten sourcing.

### Monthly activity chart

The **Monthly Batch Activity** bar chart shows how many new batches were created each month over the last 6 months.

Sudden drops in batch creation may indicate that your Supplier users have stopped using the platform consistently. Sudden spikes may indicate a bulk import or a seasonal production peak. Use this chart alongside the audit log to investigate anomalies.

### Origin countries list

The **Top Origin Countries** section shows a ranked list of the most common countries from which your batches originate.

This is directly relevant to compliance reporting. If you source significant volumes from high-risk countries (e.g., Democratic Republic of Congo), this list makes that visible immediately and supports the enhanced due diligence documentation those origins require under OECD DDG.

### Using analytics for compliance reporting

The Analytics dashboard does not generate a formal report, but it provides the data you need to build one:

1. **For trend reporting:** Take a screenshot of the Compliance Breakdown chart and Monthly Activity chart to include in quarterly compliance reviews.
2. **For supply chain mapping:** The Mineral Distribution and Top Origin Countries charts document the scope of your sourcing for inclusion in due diligence reports.
3. **For OECD DDG reporting:** Cross-reference the origin countries list with the OECD's conflict-affected and high-risk area classifications to document your exposure and mitigation steps.
4. **For formal audit documentation:** Use the Audit Log CSV export (see section 4) alongside the Analytics data for comprehensive, evidence-based reporting.

---

## 6. API Keys

Navigate to **Admin Dashboard > API Keys**, or click the **API Keys** card.

### What Are API Keys?

API keys give external systems and automated scripts direct access to the auditraks API without requiring a user to sign in through the browser. They are intended for technical integrations, not for human use.

Common use cases for API keys:

- **ERP integration** — automatically create batches in auditraks when shipments are raised in your ERP system
- **Bulk data import** — import historical batch records from a spreadsheet or legacy system
- **Automation scripts** — scheduled scripts that check compliance status and send alerts
- **Custom reporting tools** — pull batch and compliance data into a BI tool or custom dashboard

Each API key is scoped to your tenant. A request made with your API key can only access your organisation's data — not data from other tenants.

### Creating an API Key

1. Navigate to **Admin Dashboard > API Keys**.
2. Click **Create API Key**.
3. Enter a descriptive **name** for the key — use a name that identifies what the key is for, such as "ERP Integration", "Bulk Import Script", or "Monthly Report Tool". You may have multiple keys, so names matter.
4. Click **Create**.

The full API key is displayed **once only** in an amber highlighted box. The key format is:

```
at_<long hex string>
```

For example: `at_3f8a2c1d9e4b7f6a...`

**Copy the key immediately and store it securely.** Once you close or navigate away from this screen, the full key is gone — auditraks stores only a hash of the key for security reasons. If you lose a key, you must revoke it and create a new one.

After creation, only the **prefix** of the key (e.g., `at_3f8a...`) is shown in the keys table, so you can identify which key is which without exposing the full value.

### Using API Keys

Add the `X-API-Key` header to every API request, with your full key as the value.

**Base URL:** `https://accutrac-api.onrender.com`

> Note: If the API is unresponsive for the first 20–30 seconds, this is a normal cold start. Render may spin down services during quiet periods. Retry the request after a brief wait.

#### Example: List Your Batches

```bash
curl -H "X-API-Key: at_your_key_here" \
  https://accutrac-api.onrender.com/api/batches
```

Returns a JSON array of all batches for your organisation.

#### Example: Create a Batch

```bash
curl -X POST https://accutrac-api.onrender.com/api/batches \
  -H "X-API-Key: at_your_key_here" \
  -H "Content-Type: application/json" \
  -d '{
    "batchNumber": "W-2026-100",
    "mineralType": "Tungsten (Wolframite)",
    "originCountry": "RW",
    "originMine": "Nyungwe Mine",
    "weightKg": 500
  }'
```

Returns the created batch object including its generated ID.

#### Example: Log a Custody Event

```bash
curl -X POST https://accutrac-api.onrender.com/api/batches/BATCH_ID/events \
  -H "X-API-Key: at_your_key_here" \
  -H "Content-Type: application/json" \
  -d '{
    "eventType": "MINE_EXTRACTION",
    "eventDate": "2026-03-25T10:00:00Z",
    "location": "Nyungwe Mine, Rwanda",
    "actorName": "Jean-Baptiste Habimana",
    "description": "Initial extraction from shaft 3",
    "metadata": {}
  }'
```

Replace `BATCH_ID` with the `id` value returned when you created the batch.

#### Example: Get Batch Compliance Status

```bash
curl -H "X-API-Key: at_your_key_here" \
  https://accutrac-api.onrender.com/api/batches/BATCH_ID/compliance
```

Returns a JSON object with each compliance check and its current result.

#### Example: Generate Material Passport

```bash
curl -X POST https://accutrac-api.onrender.com/api/batches/BATCH_ID/passport \
  -H "X-API-Key: at_your_key_here"
```

Triggers passport generation. The batch must have COMPLIANT status. Returns a document reference once generation is complete.

### Available API Endpoints

The following endpoints accept API key authentication:

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/batches` | List all batches for your organisation |
| `POST` | `/api/batches` | Create a new batch |
| `GET` | `/api/batches/{id}` | Get batch detail with events and compliance status |
| `GET` | `/api/batches/{id}/events` | List all custody events for a batch |
| `POST` | `/api/batches/{id}/events` | Submit a new custody event |
| `GET` | `/api/batches/{id}/compliance` | Get compliance check results for a batch |
| `GET` | `/api/batches/{id}/documents` | List documents attached to a batch |
| `POST` | `/api/batches/{id}/passport` | Generate a Material Passport |
| `POST` | `/api/batches/{id}/dossier` | Generate an Audit Dossier |
| `GET` | `/api/batches/{id}/activity` | Get the activity feed for a batch |
| `GET` | `/api/users` | List all users in your organisation |

For full request/response schemas, refer to the API reference documentation included with your subscription.

### Revoking API Keys

If a key is no longer needed, or if you suspect it may have been exposed, revoke it immediately.

1. Navigate to **Admin Dashboard > API Keys**.
2. Find the key in the table (identify it by the name you gave it, or by the prefix shown).
3. Click **Revoke**.
4. Confirm the revocation.

The key stops working immediately. Any system using the revoked key will receive a `401 Unauthorized` response on its next request. The key remains visible in the table with a **Revoked** (grey) badge for your audit records.

To replace a revoked key, create a new one and update the key value in the system that was using the old key.

### Security Best Practices

API keys provide the same access as a signed-in user. Treat them as passwords.

- **Never share API keys in email, chat, or documents.** If a key needs to be handed to a third party, create a dedicated key for that integration so you can revoke it independently.
- **Store keys in environment variables, not in code.** If a key is hardcoded in a script and that script ends up in a shared folder or version control repository, the key is compromised.
- **Use one key per integration.** Separate keys for separate systems means you can revoke one without disrupting others.
- **Rotate keys every 90 days.** Create a new key, update the system to use it, then revoke the old one.
- **Revoke keys when people leave.** If a staff member who managed an integration leaves your organisation, revoke their associated keys and create new ones.
- **Monitor the Last Used column.** The keys table shows when each key was last used. Keys showing unexpected recent activity — especially if they have not been used in a long time — may indicate unauthorised use. Revoke and replace immediately if you suspect compromise.

---

## 7. Webhook Notifications

### What Are Webhooks?

Webhooks are automated notifications that auditraks sends to your external systems when something happens on the platform. Instead of your system polling the API repeatedly to check for changes, auditraks pushes a notification to you the moment an event occurs.

Your system receives an HTTP POST request at a URL you configure, with a JSON payload describing what happened.

Common uses for webhooks:

- Syncing batch records to an ERP or inventory management system
- Triggering a compliance review workflow when a batch is flagged
- Sending Slack or Teams notifications when a Material Passport is generated
- Monitoring for custody events that need approval

### Setting Up Webhooks

1. Navigate to **Admin Dashboard > Webhooks**.
2. Click **Create Endpoint**.
3. Enter your **endpoint URL** — the URL where your system will receive notifications. This must be an HTTPS URL (plain HTTP is not accepted).
4. Select the **events** you want to subscribe to, or select `*` to receive all events.
5. Click **Save**.

After saving, auditraks shows you a **signing secret** for the endpoint. Copy it and store it securely — you will need it to verify that incoming webhooks are genuine. The secret is shown only once.

> Your endpoint URL must respond to the POST request with a `200 OK` status within 10 seconds. If it does not respond in time, auditraks will retry the delivery. A URL that consistently fails to respond will be marked inactive.

### Webhook Events

| Event | When it fires |
|---|---|
| `batch.created` | A new batch is created |
| `batch.updated` | A batch's status changes (e.g., Active, Completed) |
| `event.created` | A custody event is logged on a batch |
| `document.generated` | A Material Passport or Audit Dossier is generated |
| `user.created` | A new user is invited or activates their account |
| `compliance.flagged` | A compliance check fails and a batch is flagged |

### Webhook Payload Format

All webhook payloads follow this structure:

```json
{
  "event": "batch.created",
  "timestamp": "2026-03-25T10:00:00Z",
  "data": {
    "action": "CreateBatch",
    "entityType": "Batch",
    "entityId": "guid-here",
    "result": "Success"
  }
}
```

The `entityId` field contains the ID of the affected record. Use this to make a follow-up API request to retrieve the full record if you need more detail than the payload contains.

### Verifying Webhook Signatures

Every webhook request includes an `X-Webhook-Signature` header. This is an HMAC-SHA256 hash of the request body, computed using your endpoint's signing secret as the key.

You must verify this signature on your receiving server before processing the payload. This ensures the request actually came from auditraks and was not tampered with in transit.

**Example verification in Python:**

```python
import hmac
import hashlib

def verify_signature(body: str, signature: str, secret: str) -> bool:
    expected = hmac.new(
        secret.encode('utf-8'),
        body.encode('utf-8'),
        hashlib.sha256
    ).hexdigest()
    return hmac.compare_digest(expected, signature)
```

Call `verify_signature(raw_request_body, request_header['X-Webhook-Signature'], your_signing_secret)`. If it returns `False`, reject the request and do not process it.

> Use `hmac.compare_digest` (or your language's equivalent constant-time comparison) rather than a regular string equality check. This prevents timing attacks.

---

## 8. Managing Your Subscription

### Viewing Your Plan

Your current subscription status is always visible in the **status banner at the top of the Admin Dashboard**:

- **Amber banner:** "Trial — X days remaining" — your 60-day free trial is active
- **Green banner:** "Pro Plan — Active" (or "Starter Plan — Active") — your trial has converted to a paid subscription

The banner also shows a **Manage Billing** button that opens the Stripe Customer Portal directly.

### Managing Billing

1. From the Admin Dashboard, click **Manage Billing** in the status banner, or locate the Manage Billing option in the sidebar.
2. You are redirected to the **Stripe Customer Portal** (hosted on `billing.stripe.com`).
3. In the portal you can:
   - View your current plan and next billing date
   - Update your payment method (credit or debit card)
   - View and download invoices for accounting or tax purposes
   - Upgrade from Starter to Pro
   - Downgrade from Pro to Starter (takes effect at the end of the current billing period)
   - Cancel your subscription

Use the Stripe portal's navigation or **Return to auditraks** link to come back to the platform when you are done.

> Changes to your plan take effect according to Stripe's billing rules. Upgrades typically take effect immediately (with prorated charges for the rest of the current period). Cancellations and downgrades take effect at the end of the current billing period.

### Cancelling Your Subscription

1. Open the Stripe Customer Portal via **Manage Billing**.
2. Navigate to the subscription section and click **Cancel subscription**.
3. Follow the Stripe cancellation flow.

After cancellation:
- Your access continues until the end of the current billing period (you have paid for that time)
- After the period ends, your account is downgraded and access to new features may be restricted
- **Your data is retained for 30 days after the subscription ends.** During this period you can reactivate by contacting support or signing up again
- After 30 days, data may be permanently deleted

If you wish to reactivate an account after cancellation, contact auditraks support before the 30-day retention window closes.

---

## 9. Compliance Reporting

### Understanding Compliance Statuses

Every batch has an overall compliance status, shown as a colour-coded badge throughout the platform:

| Status | Colour | Meaning |
|---|---|---|
| **COMPLIANT** | Green | All five compliance checks passed |
| **FLAGGED** | Red | One or more checks failed — attention required |
| **INSUFFICIENT_DATA** | Amber | Not enough information to complete all checks — more events or documents needed |
| **PENDING** | Grey | The batch was just created and no checks have been triggered yet |

Compliance status is recalculated automatically after every custody event submission. There is no manual trigger.

### The Five Compliance Checks

auditraks runs five automated checks against each batch. The checks are triggered by different events:

**1. RMAP Smelter Verification**
Triggered when a Primary Processing (Smelting) event is submitted. The Smelter ID entered in the event is checked against the current RMAP-approved smelter list. If the ID is not found, the batch is flagged. Ask your Supplier to verify the Smelter ID was entered correctly — a single digit error will cause a failure.

**2. OECD DDG Origin Country Risk**
Triggered at batch creation. The origin country is evaluated against the OECD's classification of conflict-affected and high-risk areas. DRC origin batches are flagged automatically — this is expected and does not mean the sourcing is illegal, but it does require enhanced due diligence documentation.

**3. Sanctions Screening**
Triggered at batch creation and when Trading/Transfer events are submitted. Origin country, trading parties, and smelter details are checked against applicable sanctions lists. A sanctions match requires immediate review before any further trading.

**4. Mass Balance Check**
Triggered when Concentration or Primary Processing events include both input and output weights. If the output weight exceeds the input weight by more than 5%, the check fails. This catches data entry errors and guards against fraudulent weight inflation. Your Supplier should submit a Correction event if the entered weights were wrong.

**5. Event Sequence Integrity**
Triggered on every custody event submission. Checks that the new event's date is not earlier than the previous event's date. Out-of-order events are flagged. If events genuinely occurred out of sequence (e.g., backdated lab certificates), the reason should be documented in the event description.

### Generating Compliance Reports

auditraks provides three ways to generate formal compliance documentation:

**Audit Log CSV Export**
The most comprehensive record of all platform activity. Export this from Admin Dashboard > Audit Log for formal compliance documentation, regulatory submission, or external auditor requests. See [Audit Log](#4-audit-log) for details on filtering and exporting.

**Material Passport**
Generated by a Buyer user for individual batches with COMPLIANT status. A PDF certificate containing the custody chain summary, compliance results (RMAP and OECD DDG), hash chain integrity status, and a QR verification code. Suitable for sharing with customers and regulators.

**Audit Dossier**
Generated by a Buyer user for any batch regardless of compliance status. A comprehensive PDF containing the complete event log, all compliance check results with full detail, references to every attached document with their SHA-256 file hashes, and full hash chain verification. Use this for formal due diligence reviews, regulatory submissions, and external audits.

To generate either document, a Buyer user navigates to the batch detail view and uses the **Generate & Share** tab.

---

## 10. FAQ

**How do I add more users?**
Navigate to Admin Dashboard > Manage Users > Invite User. Enter their email, name, and role (Supplier or Buyer) and click Send Invitation. They will receive an email with an activation link valid for 7 days. If you are on the Starter plan and have reached your 5-user limit, you will need to upgrade to Pro before inviting more users.

**What if a user leaves the company?**
Deactivate their account immediately. Open their profile in Admin > Manage Users, click **Deactivate Account**, and confirm. Deactivated users cannot sign in. All records they created (batches, events, documents) remain intact and are not affected. If they also have access to your API key values, revoke those keys and create new ones.

**How do I change my plan?**
Click **Manage Billing** from the Admin Dashboard to open the Stripe Customer Portal. From there you can upgrade from Starter to Pro or downgrade from Pro to Starter. Upgrades take effect immediately; downgrades take effect at the end of the current billing period.

**Can I export all my data?**
The Audit Log CSV export gives you a complete record of all platform activity. For batch and event data, use the API (see [API Keys](#6-api-keys)) with a script to retrieve all batches and their events. For documents, download them individually from the batch detail view. If you need a full data export for migration or legal purposes, contact auditraks support.

**What happens when my trial ends?**
At the end of your 60-day trial, your subscription automatically converts to the plan you selected at signup and your card on file is charged the first month's fee. You will receive an email from Stripe before this happens. You can cancel or change your plan at any time before the trial ends from the billing portal — no charge is made if you cancel before the trial period is up.

**How do I contact support?**
Send an email to support@auditraks.com. Include your organisation name, a description of the issue, and any relevant batch IDs or user emails. For urgent issues such as suspected security incidents or data integrity concerns, mark your email as urgent.

**Can I have multiple Tenant Admins?**
By default, each organisation has one Tenant Admin (the person who signed up). If you need an additional Tenant Admin — for example, a backup admin or a second person to manage billing — contact auditraks support. You cannot promote a user to Tenant Admin yourself from within the platform.

**What is the difference between a Material Passport and an Audit Dossier?**
A **Material Passport** is a concise summary intended for external sharing with customers, downstream partners, and regulators. It includes the custody chain summary, overall compliance status, and a QR code linking to the public verification page. It can only be generated for COMPLIANT batches.

An **Audit Dossier** is a comprehensive document for formal due diligence and regulatory submissions. It includes the complete event log, all compliance check results with full technical detail, SHA-256 hashes of every attached document, and full hash chain integrity verification. It can be generated for any batch regardless of compliance status, and you can optionally restrict it to a specific date range.

**A batch is showing FLAGGED — what do I do?**
Open the batch in the Buyer Portal (or ask a Buyer user to do so) and click the **Compliance Checks** tab. This shows every check and its result, including notes explaining the failure. Common causes:
- RMAP flag: Smelter ID entered incorrectly — ask your Supplier to verify and submit a Correction event if needed
- OECD DDG flag: Origin country is high-risk — this is automatic for certain countries and requires enhanced due diligence documentation
- Mass Balance flag: Output weight exceeds input — data entry error, ask Supplier to submit a Correction event
- Sequence flag: Events entered out of order — ask Supplier to check event dates

**What does INSUFFICIENT_DATA mean?**
The compliance engine could not complete one or more checks because the required events have not been submitted yet. For example, the RMAP check cannot run until a Smelting event is logged. Open the Compliance Checks tab on the batch to see which checks are inconclusive and what information they are waiting for, then ask your Supplier to add the missing events.

**A user says their invite email never arrived — what do I do?**
Ask them to check their spam or junk folder first. If the email is not there, go to Admin > Manage Users, find the user (they will show as Pending), open their profile, and click **Resend Invitation**. A new activation link (valid for 7 days) is generated and sent. Also confirm that your organisation's email filtering is not blocking messages from the auditraks sending domain.

**Is my data secure?**
Yes. All data in transit is encrypted using TLS. Data at rest is encrypted at the storage level. Every action is recorded in the tamper-evident audit log. Access to batch data is restricted to authorised users within your organisation — no other tenant can see your data. The SHA-256 hash chain on custody events means that any tampering with historical records would be immediately detectable.

**Can I verify that a Material Passport is genuine?**
Yes. Scan the QR code printed on the passport using any smartphone camera or QR reader app. This opens the public batch verification page at `https://auditraks.com/verify/{batchId}`, which shows the live compliance status directly from the database — not from the PDF itself. If the compliance status on the verification page does not match what the passport says, contact auditraks support.

---

*auditraks Tenant Admin Manual — Version 1.0 — March 2026*

*This document is provided for guidance. Features and interfaces may be updated as the platform evolves. For the most current information, refer to the latest version of this manual or contact support@auditraks.com.*
