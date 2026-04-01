# auditraks User Manual

## Mineral Supply Chain Compliance Platform

**Version 3.0 — March 2026**

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Roles](#2-roles)
3. [Supplier Portal](#3-supplier-portal)
4. [Buyer Portal](#4-buyer-portal)
5. [Admin Dashboard](#5-admin-dashboard)
6. [Public Features](#6-public-features)
7. [API Access](#7-api-access)
8. [Compliance Framework](#8-compliance-framework)
9. [Glossary — Terms, Acronyms & Definitions](#9-glossary--terms-acronyms--definitions)
10. [FAQ](#10-faq)

---

## 1. Getting Started

### What is auditraks?

auditraks is a supply chain compliance platform that tracks mineral batches from mine to refinery. Every custody event is recorded with a cryptographic fingerprint, creating a tamper-evident chain that allows buyers, suppliers, and auditors to verify responsible sourcing against international standards.

The platform supports the full 3TG mineral suite — tungsten, tin, tantalum, and gold — and validates against two leading compliance frameworks: RMAP (Responsible Minerals Assurance Process) and the OECD Due Diligence Guidance (DDG).

### How to Sign Up

auditraks is a SaaS platform. To create a new organization account:

1. Go to [accutrac.org](https://accutrac.org) and click **Start Free Trial** on the landing page, or navigate directly to `/signup`.
2. Choose a plan:
   - **Starter** — $99/month after trial
   - **Pro** — $249/month after trial
3. Fill in the sign-up form:
   - **Company name** — your organization's name
   - **Your name** — the name of the account owner
   - **Email address** — used for your admin account and billing
   - **Confirm email** — must match exactly
4. Click **Start 60-day free trial**.
5. You are redirected to a **Stripe checkout page** to enter payment details. Your card will not be charged until the trial ends.
6. After completing checkout, you are redirected to the **signup success page**. Your account is created and you can sign in immediately.

> Your 60-day free trial begins on the day you sign up. You can cancel at any time from the billing portal before the trial ends and you will not be charged.

### How to Sign In

Navigate to `/login` and sign in with your email and password. Enter the credentials associated with your auditraks account and click **Sign In**.

**Forgot your password?** Click **Forgot password?** on the login screen. A reset link will be sent to your email.

**First-time users invited by an admin:** You will receive an email invitation with an activation link. The link expires after 7 days. If it has expired, ask your admin to resend it.

### After Signing In

auditraks detects your role and sends you to the correct portal:

- **Suppliers** go to the Supplier Dashboard at `/supplier`
- **Buyers** go to the Buyer Dashboard at `/buyer`
- **Tenant Admins** go to the Admin Dashboard at `/admin`

If you are redirected to the wrong portal, contact your Tenant Admin to check your role assignment.

### First-Time Setup (Tenant Admins)

When you sign in for the first time as a Tenant Admin, a **Getting Started wizard** appears on the Admin Dashboard. It walks you through four steps:

1. **Welcome** — overview of the platform
2. **Invite your team** — add suppliers and buyers so they can access their portals
3. **Create your first batch** — register the first mineral batch to track
4. **Run compliance checks** — compliance runs automatically once custody events are logged; no action needed

You can navigate between steps using the numbered dots or the Previous/Next buttons. Click **Dismiss** or **Done** at any time — the wizard will not reappear once dismissed.

---

## 2. Roles

auditraks has three user roles. Access is enforced at both the interface and API level — each role only sees what is relevant to them.

### Tenant Admin

The Tenant Admin manages your organization's auditraks account. This is typically the compliance manager or operations lead.

**A Tenant Admin can:**
- Invite users and assign them as Supplier or Buyer
- Change user roles and deactivate accounts
- View the audit log of all platform actions
- Access analytics (compliance trends, mineral distribution, monthly activity)
- Manage API keys for programmatic integrations
- Manage billing via the Stripe portal
- Monitor trial status and days remaining
- View and review compliance flags (if also granted Platform Admin access)

**A Tenant Admin cannot:** create batches or log custody events (those are Supplier functions).

### Supplier

A Supplier is typically a miner, trader, processor, or exporter. Suppliers interact with the platform to create batch records and document every step the material takes through the supply chain.

**A Supplier can:**
- Create new mineral batches
- Log custody events (extraction, assay, concentration, trading, smelting, export)
- Upload supporting documents to batches and events
- View the full event timeline and compliance status for their batches
- View the activity feed for each batch
- Split batches into child batches
- Update batch status (active, completed)

### Buyer

A Buyer is typically a manufacturer, refiner, or downstream customer that needs to verify compliance and generate documentation.

**A Buyer can:**
- View all batches and their compliance status
- Browse the full event timeline for any batch
- Generate Material Passports (PDF with QR code)
- Generate Audit Dossiers (comprehensive compliance PDFs)
- Share documents via time-limited secure links
- Download documents and files
- View the activity feed for each batch

---

## 3. Supplier Portal

The Supplier Portal is accessible at `/supplier`. It is where batches are created and custody events are recorded.

### Dashboard Overview

The Supplier Dashboard shows all batches associated with your organization. Each batch is displayed as a card showing:

- **Batch number** — the unique batch identifier
- **Mineral type** — the material being tracked
- **Origin** — the country and mine site
- **Current weight** — the most recently recorded weight
- **Compliance status** — a color-coded badge (green = COMPLIANT, red = FLAGGED, amber = INSUFFICIENT_DATA, grey = PENDING)
- **Last updated** — date and time of the most recent event

Use the search bar to find a batch by number, and use the filter controls to narrow by compliance status.

### Getting Started Checklist

When you first log in as a supplier, a **Getting Started** checklist appears at the top of your dashboard. It guides you through your first actions on the platform:

1. **Create your first batch** — register a mineral batch
2. **Submit a custody event** — record the first event in the batch lifecycle
3. **Review compliance status** — view your batch's compliance results

Each step is marked complete as you perform it. A progress bar shows your overall progress. You can dismiss the checklist at any time by clicking the X button — it will not reappear once dismissed.

### Creating a Batch

A batch represents a discrete, trackable quantity of mineral material.

**To create a new batch:**

1. Click **New Batch** on the Supplier Dashboard.
2. Complete the form:

| Field | Description |
|---|---|
| **Batch Number** | A unique identifier. Example: `W-2026-050`. Must be unique on the platform. |
| **Mineral Type** | Select from the supported minerals (see list below). |
| **Origin Country** | Start typing a country name or ISO code to search. Select from the dropdown. Example: Rwanda (RW). |
| **Mine Site** | Name of the extraction site or mine. |
| **Initial Weight (kg)** | Weight of material at creation. |

3. Click **Create Batch**.

The batch appears on your dashboard with a **PENDING** status until events trigger compliance checks.

**Supported mineral types:**

- Tungsten (Wolframite)
- Tungsten (Cassiterite)
- Tin (Cassiterite)
- Tantalum (Coltan)
- Tantalum (Tantalite)
- Gold (Alluvial)
- Gold (Hard Rock)

> Batch numbers must be unique. If you receive a "batch already exists" error, check whether a colleague has already created it, or use a different identifier.

### Logging Custody Events

Custody events record what happened to a batch at each stage of the supply chain. Together they form a chronological chain documenting the material's full journey.

**To add an event:**

1. Open the batch from your dashboard.
2. Click **Add Event**.
3. Select the event type.
4. Fill in the required fields (described below).
5. Optionally attach supporting documents.
6. Click **Submit Event**.

Once submitted, an event is permanent and cannot be edited or deleted. If you made an error, submit a Correction event (see below).

Compliance checks run automatically after each event submission.

---

#### Mine Extraction

Records the extraction of material from the mine.

| Field | Description |
|---|---|
| **GPS Coordinates** | Latitude and longitude of the extraction site. Example: `-1.9441, 30.0619` |
| **Mine Operator** | Name of the company or person operating the mine. |
| **Mineralogical Certificate Reference** | Reference number of the certificate issued at extraction. |

---

#### Laboratory Assay

Records an analytical test performed on the material.

| Field | Description |
|---|---|
| **Laboratory Name** | Name of the testing laboratory. |
| **Method** | Analytical method used (e.g., XRF, ICP-MS). |
| **Tungsten Content (%)** | Mineral content as determined by the assay. |
| **Certificate Reference** | Reference number of the assay certificate. |

---

#### Concentration

Records a beneficiation or concentration process.

| Field | Description |
|---|---|
| **Facility Name** | Name of the processing facility. |
| **Process Description** | Brief description of the method (e.g., gravity separation, flotation). |
| **Input Weight (kg)** | Weight entering the process. |
| **Output Weight (kg)** | Weight leaving the process. |
| **Concentration Ratio** | Calculated automatically if left blank. |

---

#### Trading / Transfer

Records a transfer of ownership or custody between parties.

| Field | Description |
|---|---|
| **Seller** | Name of the party transferring the material. |
| **Buyer** | Name of the party receiving the material. |
| **Transfer Date** | Date the transfer took place. |
| **Contract Reference** | Purchase contract or transfer agreement reference number. |

---

#### Primary Processing (Smelting)

Records a smelting or primary refining step.

| Field | Description |
|---|---|
| **Smelter ID (RMAP)** | The RMAP-assigned identifier for the smelter. This triggers an RMAP compliance check. |
| **Process Type** | Type of smelting process (e.g., electric arc furnace, hydrometallurgical). |
| **Input Weight (kg)** | Weight entering the smelter. |
| **Output Weight (kg)** | Weight leaving the smelter. |

> The Smelter ID must match an entry on the current RMAP-approved smelter list. An unrecognized ID will flag the batch. If you believe the smelter is RMAP-certified, contact your Tenant Admin to check whether the list needs updating.

---

#### Export / Shipment

Records the export or international shipment of the material.

| Field | Description |
|---|---|
| **Origin Country** | Country the shipment departs from. |
| **Destination Country** | Country the shipment is sent to. |
| **Transport Mode** | Method of transport (e.g., air freight, sea freight, road). |
| **Export Permit Reference** | Reference number of the export permit or customs declaration. |

---

### Viewing the Event Timeline

Open any batch and click the **Events** tab to see the chronological timeline of all custody events. Each entry shows:

- Event type and date
- Submitting user and organization
- Key field values
- The SHA-256 hash of the event

The hash of each event includes the hash of the previous event, forming a cryptographic chain. If any event were altered after submission, the chain would break and be immediately detectable.

### Viewing the Activity Feed

The **Activity Feed** on each batch detail page shows a running log of all actions taken on that batch — events submitted, documents uploaded, compliance checks run, and status changes. This gives a quick summary of recent activity without needing to open the full event timeline.

### Document Uploads

Supporting documents can be attached to a batch or to specific events.

**Accepted formats:** PDF, JPEG, PNG, TIFF, GIF

**Maximum file size:** 25 MB per file

**To upload a document:**

1. Open the batch or event.
2. Click **Upload Document** or drag and drop the file.
3. Enter a description (e.g., "Mineralogical Certificate — Batch W-2026-041").
4. Click **Save**.

Documents cannot be deleted after upload. If you uploaded the wrong file, upload the correct version and note in the description which document supersedes the other.

### Splitting a Batch

When a batch is physically divided between two parties, you can split it into two child batches.

1. Open the batch detail view.
2. Click **Split Batch**.
3. Enter the weight for Child A and Child B. The two values must sum exactly to the parent batch weight.
4. Click **Confirm Split**.

auditraks creates two new child batches with `-A` and `-B` suffixes, marks the parent as COMPLETED, and carries the origin, mineral type, and mine site to both children. Each child batch continues through the supply chain independently.

### Updating Batch Status

As a batch progresses, update its status:

- **CREATED → ACTIVE** — when the batch is in transit and events are being recorded
- **ACTIVE → COMPLETED** — when the batch has reached its destination and all events are logged

Open the batch detail and click **Mark Active** or **Mark Completed**. Status transitions are one-way and cannot be reversed.

### Understanding Compliance Statuses

| Status | Color | Meaning |
|---|---|---|
| **COMPLIANT** | Green | All checks passed. The batch meets RMAP and OECD DDG requirements. |
| **FLAGGED** | Red | One or more checks failed. Review is required. |
| **INSUFFICIENT_DATA** | Amber | Not enough information to complete all checks. Add events or documents. |
| **PENDING** | Grey | Batch created but no checks have been triggered yet. |

Compliance status is recalculated automatically after each event submission.

### Material Passport Sharing (Suppliers)

When a batch reaches **COMPLIANT** status, a "Material Passport Ready" card appears on the batch detail page. This is your key deliverable — a marketable asset you can share with your customers to demonstrate responsible sourcing.

**Available actions:**

- **Download PDF** — generates and downloads the Material Passport as a PDF document
- **Copy Link** — creates a 30-day shareable URL and copies it to your clipboard
- **Email to Customer** — opens an inline form where you enter a recipient email and optional message. The platform sends a branded email with the passport link on your behalf from `support@auditraks.com`

> The Material Passport is only available for COMPLIANT batches. Resolve any compliance flags first.

---

## 4. Buyer Portal

The Buyer Portal is accessible at `/buyer`. It is where purchasing organizations monitor compliance, generate reports, and share documentation.

### Dashboard Overview

The Buyer Dashboard shows a summary at the top:

- Total active batches
- Number of compliant batches
- Number of flagged batches
- Number with insufficient data

Below the summary, a sortable batch table lists all batches. You can:

- **Search** by batch number, origin country, or mineral type
- **Filter by compliance status** using the dropdown (ALL, COMPLIANT, FLAGGED, PENDING, INSUFFICIENT_DATA)
- **Filter by date range** using the From and To date pickers
- **Clear filters** using the Clear button
- **Sort** by clicking any column header

Click any row to open the Batch Detail view.

### Viewing Batches and Compliance Status

The Batch Detail view has three tabs:

**Events tab** — chronological log of all custody events, showing event type, date, submitting organization, key field values, and the SHA-256 hash of each event.

**Compliance Checks tab** — a detailed breakdown of every compliance check run against the batch:
- Check type (RMAP, OECD DDG, Mass Balance, Sequence)
- Result (Pass / Fail / Inconclusive)
- The specific rule evaluated
- Notes explaining any failures

**Documents tab** — all documents attached to the batch and its events. Click any document to preview or download it.

### Generating Material Passports

A Material Passport is a PDF report summarizing the verified custody chain and compliance status of a batch. It is designed to be shared with customers, regulators, or auditors.

The Material Passport includes:
- Batch identification (batch number, mineral type, origin)
- Summary of the custody chain
- Compliance summary (RMAP, OECD DDG results)
- Hash chain integrity status
- A QR code linking to the public batch verification page
- Platform version and compliance rule set version
- Name of the generating user and generation timestamp

**To generate a Material Passport:**

1. Open the batch detail view.
2. Click the **Generate & Share** tab.
3. Click **Generate Material Passport**.
4. Wait a few seconds for preparation.
5. Click **Download** when ready.

> Material Passports can only be generated for batches with a **COMPLIANT** status. If the button is greyed out, resolve compliance flags or missing data issues first.

### Generating Audit Dossiers

An Audit Dossier is a comprehensive PDF for formal audits and due diligence reviews. It contains the complete event log, all compliance check results with full detail, references to every attached document with their SHA-256 file hashes, and hash chain integrity verification.

**To generate an Audit Dossier:**

1. Open the batch detail view.
2. Click **Generate Audit Dossier**.
3. Optionally select a date range to limit the included events.
4. Click **Generate** and then **Download**.

Audit Dossiers are typically larger than Material Passports and may take slightly longer to prepare for batches with many events.

### Sharing Documents via Secure Links

You can share a Material Passport with external parties — auditors, regulators, customers — using a time-limited secure link. The recipient does not need an auditraks account.

**Shared links are valid for 30 days from creation.**

**To create a shared link:**

1. Generate the Material Passport.
2. Click **Share** next to the passport.
3. auditraks generates a unique URL, shown in a confirmation box.
4. Click **Copy** and send the link via email or any other channel.

The link resolves to `/shared/{token}` and can be opened in any browser. After 30 days the link expires and displays an expiry message. Generate a new link if access is needed after expiry.

> Treat shared links as confidential. Anyone with the link can view the passport during its active period.

### Viewing the Activity Feed

Just as in the Supplier Portal, each batch detail page in the Buyer Portal includes an activity feed showing a chronological log of all actions taken on the batch.

### Supplier Engagement

The **Supplier Engagement** panel appears on the Buyer Dashboard between the compliance overview and the batch table. It shows how actively your suppliers are participating:

**Metric cards:**
- **Total** — all suppliers in your organization
- **Active** (green) — suppliers with at least one custody event in the last 90 days
- **Stale** (amber) — suppliers with batches but no events in 90+ days
- **Flagged** (red) — suppliers with at least one flagged batch

Click **View suppliers** to expand the full supplier list, showing each supplier's name, last activity date, batch count, flagged batch count, and status badge.

**Sending reminders:**

For stale or flagged suppliers, a **Remind** button appears in the Action column. Clicking it sends a branded email to the supplier requesting an update on their supply chain data and creates an in-app notification. Reminders are rate-limited to one per supplier per 7 days.

### CMRT Import

The **CMRT Import** page is accessible from the buyer sidebar at `/buyer/cmrt-import`. It allows you to import smelter data from a Conflict Minerals Reporting Template (CMRT v6.x format).

**How to import a CMRT file:**

1. Navigate to **CMRT Import** in the sidebar.
2. Drag and drop a `.xlsx` file onto the upload area, or click to browse.
3. The platform parses the file and displays a **preview**:
   - **Declaration summary** — company name, reporting year, scope
   - **Match statistics** — total smelters, matched (found in RMAP database), unmatched
   - **Smelter table** — each row shows metal type, smelter name, ID, country, and match status (green = matched with conformance status, amber = unmatched)
   - **Parsing errors** — any rows that could not be read
4. Review the preview. Click **Confirm Import** to create the smelter associations, or **Cancel** to discard.
5. On confirmation, matched smelters are saved as "verified" associations and unmatched smelters with IDs are saved as "unverified" for later resolution.

**Import history** appears below the upload area, showing past imports with file name, company, match/unmatch counts, and date.

> Only `.xlsx` files are supported. The parser reads the CMRT v6.x template format (Declaration, Smelter List, and Product List tabs).

---

## 5. Admin Dashboard

The Admin Dashboard is accessible at `/admin`. It is available to Tenant Admins and Platform Admins.

### Dashboard Overview

The Admin Dashboard shows three metric cards at the top:

- **Users** — total number of registered users in your organization
- **Batches** — total number of batches tracked
- **Flags** — total number of compliance flags raised

Below the metrics, a **Quick Actions** grid provides one-click navigation to all admin sections.

**Tenant Admins** also see a status banner showing their plan status:
- **Trial** — amber banner showing days remaining, with a **Manage Billing** button
- **Active** — green banner confirming the Pro plan is active, with a **Manage Billing** button

### User Management

Navigate to **Admin > Manage Users** or click the **Manage Users** card.

#### Inviting a User

1. Click **Invite User**.
2. Enter the user's email address.
3. Select their role: **Supplier** or **Buyer**.
4. Click **Send Invitation**.

The user receives an email with an activation link (valid for 7 days). You can resend the invitation from the Users list if needed.

#### Assigning or Changing a Role

1. Find the user in the list.
2. Click their name to open their profile.
3. Select the new role from the Role dropdown.
4. Click **Save Changes**.

Role changes take effect immediately. On their next page load, the user is redirected to the appropriate portal.

#### Deactivating a User

1. Open the user's profile.
2. Click **Deactivate Account** and confirm.

Deactivated users cannot sign in, but all records they created remain intact. To reactivate, find them using the "Show deactivated users" filter, open their profile, and click **Reactivate Account**.

### Audit Log

Navigate to **Admin > Audit Log**.

The Audit Log records every action taken on the platform — batch creation, event submissions, document uploads, user changes, and more.

**Columns:** Timestamp, User, Action, Entity Type, Result (Success / Failure)

**Filtering:** Use the dropdowns to filter by:
- **Action** — specific action types (e.g., Batch Created, Event Submitted)
- **Entity Type** — Batch, Custody Event, Document, User, RMAP Smelter
- **Result** — Success or Failure

**Expanding a row:** Click any row to see additional detail including failure reason (if applicable), entity ID, IP address, and the full event payload.

**Pagination:** 20 entries per page. Use the Previous and Next buttons to navigate.

**Exporting:** Click **Export CSV** to download the current filtered view as a CSV file (`audit-log.csv`).

### Analytics Dashboard

Navigate to **Admin > Analytics**.

The Analytics page provides a visual overview of supply chain activity. It shows:

**Metric cards:**
- Total Batches
- Completed Batches
- Flagged Batches
- Active Users
- Total Custody Events
- Pending Compliance

**Charts:**
- **Compliance Breakdown** — horizontal progress bars showing the percentage of batches that are Compliant, Flagged, or Pending, with summary pills
- **Monthly Batch Activity** — bar chart of batch creation volume over the last 6 months
- **Mineral Distribution** — breakdown of batches by mineral type
- **Top Origin Countries** — ranked list of the most common origin countries

### Webhook Management

Webhooks allow external systems to receive real-time notifications when events occur on the platform. auditraks sends an HTTP POST request to your endpoint whenever a configured event fires.

**To create a webhook endpoint:**

1. Navigate to **Admin > Webhooks** (or access via the API keys section if visible).
2. Click **Create Endpoint**.
3. Enter the URL of your receiving endpoint.
4. Select the events to subscribe to (e.g., batch created, event submitted, compliance flag raised).
5. Save the endpoint.

auditraks signs each webhook payload with an **HMAC-SHA256 signature** using your endpoint's secret. Verify this signature on your receiving server to confirm the payload is authentic.

**To verify a webhook signature:**
1. Retrieve the `X-Signature` header from the incoming request.
2. Compute HMAC-SHA256 of the raw request body using your endpoint secret as the key.
3. Compare your computed signature to the header value. If they match, the payload is genuine.

### API Key Management

API keys allow external systems and integrations to access the auditraks API programmatically without user authentication.

Navigate to **Admin > API Keys**.

#### Creating an API Key

1. Click **Create API Key**.
2. Enter a descriptive name (e.g., "CI Pipeline", "ERP Integration").
3. Click **Create**.
4. The full key is displayed once in an amber box. **Copy it immediately and store it securely.** It will not be shown again.

API keys follow the format `at_<hex string>`.

#### Revoking an API Key

Find the key in the table and click **Revoke**. The key is immediately deactivated and all requests using it will be rejected. Revoked keys remain visible in the table with a "Revoked" badge for audit purposes.

#### Key Table Columns

| Column | Description |
|---|---|
| Name | The descriptive name you gave the key |
| Prefix | First characters of the key (e.g., `at_3f8a...`) for identification |
| Status | Active (green) or Revoked (grey) |
| Last Used | Date the key was last used to make a request |
| Created | Date the key was created |

### Managing Billing

Tenant Admins can access the **Stripe billing portal** to manage their subscription.

**To open the billing portal:**

1. From the Admin Dashboard, locate the trial/plan status banner at the top of the page.
2. Click **Manage Billing**.
3. auditraks redirects you to the Stripe portal where you can:
   - View your current plan and billing period
   - Update payment method
   - View invoices and payment history
   - Upgrade, downgrade, or cancel your subscription

> Changes made in the Stripe portal take effect according to Stripe's billing rules. Cancellations take effect at the end of the current billing period.

### Plan Status

The Admin Dashboard banner shows your current subscription status:

- **Trial** — amber banner. Shows exact days remaining (e.g., "Trial — 47 days remaining"). Click **Manage Billing** to add a payment method or convert to a paid plan early.
- **Active** — green banner showing "Pro Plan — Active". Click **Manage Billing** to manage your subscription.

---

## 6. Public Features

### Batch Verification Page

Each batch has a publicly accessible verification page that anyone can view — no auditraks account required. This is the trust layer: customers, end consumers, and auditors can independently confirm compliance status.

**Access via QR code:** Scan the QR code printed on a Material Passport. Any smartphone camera or QR reader app will open the verification page directly.

**Access via URL:** Navigate to:

```
https://accutrac.org/verify/{batchId}
```

Example: `https://accutrac.org/verify/W-2026-041`

The public verification page displays:
- Batch number and mineral type
- Origin country
- Overall compliance status (COMPLIANT, FLAGGED, INSUFFICIENT_DATA, or PENDING)
- Date the compliance status was last updated
- Summary of which compliance frameworks were evaluated

The page does **not** display detailed event data, document contents, or commercially sensitive information.

### Shared Document Links

When a buyer shares a Material Passport using the Share function, recipients access it at:

```
https://accutrac.org/shared/{token}
```

Shared links are valid for **30 days**. After expiry, the page shows an expiry message. Contact the buyer who shared the link to request a new one.

---

## 7. API Access

### Authentication

Authenticate API requests using your API key in the `X-API-Key` request header:

```
X-API-Key: at_your_key_here
```

API keys are created and managed by Tenant Admins in the Admin Dashboard under **API Keys**. Keys follow the format `at_<hex string>`.

Never include API keys in client-side code, public repositories, or any location accessible to unauthorized parties. If a key is compromised, revoke it immediately from the Admin Dashboard and create a new one.

### Available Endpoints

The auditraks API is a REST API. Common operations include:

| Operation | Description |
|---|---|
| `GET /api/batches` | List all batches for the tenant |
| `GET /api/batches/{id}` | Get a specific batch with its events and compliance status |
| `POST /api/batches` | Create a new batch |
| `POST /api/batches/{id}/events` | Submit a custody event for a batch |
| `GET /api/batches/{id}/documents` | List documents for a batch |
| `GET /api/analytics` | Get analytics summary |
| `GET /api/admin/audit-logs` | List audit log entries (admin only) |
| `GET /api/verify/{batchId}` | Public batch verification (no auth required) |

For the full API reference, contact your auditraks administrator or refer to the API documentation provided with your subscription.

### MCP Server Access (AI Assistants)

auditraks provides an MCP (Model Context Protocol) server that allows AI assistants (such as Claude) to interact with the platform on your behalf.

**Requirements:**
- An active API key (generate one from Admin Dashboard → API Keys)
- An MCP-compatible AI assistant (Claude Desktop, Claude Code, or other MCP clients)

**What AI assistants can do via MCP:**
- Query batches and custody events
- Check compliance status
- Search the RMAP smelter database
- Generate and share Material Passports
- View supplier engagement metrics (buyer role)
- Send supplier reminders (buyer role)
- View Form SD filing cycles

**What AI assistants cannot do via MCP:**
- Upload files (documents, CMRT imports)
- Manage users or billing
- Access data outside your tenant

The tools available to the AI assistant depend on your role. A supplier's API key gives access to supplier tools; a buyer's API key gives access to buyer tools. The API enforces role-based access server-side.

See `packages/mcp/README.md` for configuration instructions.

---

## 8. Compliance Framework

auditraks runs five automated compliance checks against every batch. Checks trigger automatically when relevant events are submitted — no manual action is required.

### RMAP Smelter Verification

**Triggered by:** Submitting a Primary Processing (Smelting) event.

**What it checks:** The Smelter ID entered in the event is looked up against the current RMAP-approved smelter list (maintained by Platform Admins).

**Pass:** The smelter ID is found on the RMAP list — the smelter has been audited under the Responsible Minerals Initiative.

**Fail:** The smelter ID is not found. The batch is flagged. Verify the ID was entered correctly. If correct, the smelter may not have current RMAP certification — contact the smelter and notify your admin.

### OECD DDG Origin Country Risk

**Triggered by:** Batch creation (the origin country is assessed immediately).

**What it checks:** The origin country is evaluated against the OECD's risk classification for conflict-affected and high-risk areas.

**Pass:** The origin country is classified as low risk.

**Fail:** The origin country is identified as conflict-affected or high-risk (e.g., DRC). The batch is flagged automatically. No manual review step is needed — the system catches it immediately.

### Sanctions Screening

**Triggered by:** Batch creation and when trading/transfer events are submitted.

**What it checks:** The origin country, trading parties, and smelter are checked against applicable sanctions lists.

**Pass:** No sanctions match.

**Fail:** A match is found. The batch is flagged and requires review before any further trading.

### Mass Balance Check

**Triggered by:** Submitting a Concentration or Primary Processing event with both input and output weights.

**What it checks:** The output weight is compared to the input weight. If the output exceeds the input by more than 5%, the check fails. This guards against reporting errors and fraudulent weight inflation.

**Pass:** Output weight is within acceptable range of input weight.

**Fail:** Output exceeds input by more than 5%. Review the weights on the event. If there is a genuine data entry error, submit a Correction event. If the discrepancy is legitimate (e.g., added materials), document the reason and contact your admin.

### Event Sequence Integrity

**Triggered by:** Every custody event submission.

**What it checks:** The date of the new event is compared to the date of the most recent existing event. Out-of-order events are flagged.

**Pass:** Events are in chronological order.

**Fail:** The new event's date is earlier than the previous event. Check the date entered. If events genuinely occurred out of order (e.g., backdated lab results), document the reason.

### SHA-256 Hash Chain

Every custody event submitted to auditraks receives a cryptographic fingerprint — a SHA-256 hash. This hash is calculated from:

- The event type and all field values
- The submission timestamp
- The submitting user
- The hash of the previous event in the chain

Because each event's hash depends on the previous event's hash, the entire chain is mathematically linked. Altering any event — even a single character — would change its hash, causing all subsequent events to no longer match. This makes undetected tampering computationally infeasible.

**What this means for you:**
- Buyers and auditors can trust that the event log has not been modified since it was recorded
- Material Passports and Audit Dossiers include the hash chain integrity status
- Platform Admins can run a manual integrity check on any batch at any time
- The public verification page reflects the current integrity status

### Compliance Status Rollup

A batch's overall compliance status is determined by combining all individual check results:

| Condition | Overall Status |
|---|---|
| All checks pass | **COMPLIANT** |
| Any check fails | **FLAGGED** |
| Checks cannot complete due to missing data | **INSUFFICIENT_DATA** |
| No checks triggered yet | **PENDING** |

Status is recalculated after every event submission. Relevant users are notified by email when a batch is flagged or when a flag is resolved.

---

## 9. Glossary — Terms, Acronyms & Definitions

### Acronyms

| Acronym | Full Name | Description |
|---------|-----------|-------------|
| **3TG** | Tin, Tantalum, Tungsten, Gold | The four "conflict minerals" regulated under Dodd-Frank §1502 and the EU Conflict Minerals Regulation |
| **APT** | Ammonium Paratungstate | An intermediate tungsten product produced during smelting/processing |
| **CAHRA** | Conflict-Affected and High-Risk Areas | Geographic regions identified by the OECD as having armed conflict, weak governance, or widespread human rights abuses. Sourcing from CAHRA countries triggers enhanced due diligence |
| **CID** | Company Identification Number | The unique identifier assigned to each smelter/refiner in the RMAP programme (e.g., CID001100) |
| **CMRT** | Conflict Minerals Reporting Template | A standardised Excel workbook (published by RMI) used by companies to declare their 3TG sourcing. Version 6.x is the current standard |
| **DDG** | Due Diligence Guidance | The OECD's framework for responsible supply chain management of minerals from conflict-affected areas |
| **DPP** | Digital Product Passport | A structured JSON-LD document containing product traceability data, aligned to EU regulatory schemas |
| **OECD** | Organisation for Economic Co-operation and Development | International body that publishes the DDG framework for conflict mineral due diligence |
| **RMAP** | Responsible Minerals Assurance Process | An audit programme managed by RMI that assesses smelter/refiner compliance with responsible sourcing standards |
| **RMI** | Responsible Minerals Initiative | The industry body that manages RMAP and publishes the CMRT template |
| **SEC** | Securities and Exchange Commission | US federal agency that administers Form SD filing requirements under Dodd-Frank |
| **SHA-256** | Secure Hash Algorithm 256-bit | A cryptographic hash function used by auditraks to create tamper-evident event chains |

### Compliance & Regulatory Terms

| Term | Definition |
|------|-----------|
| **Compliance status** | The overall result of all compliance checks on a batch: COMPLIANT (all pass), FLAGGED (one or more checks failed), INSUFFICIENT_DATA (not enough information to evaluate), or PENDING (no checks run yet) |
| **Compliance check** | An individual automated assessment run against a batch — RMAP conformance, OECD DDG, sanctions screening, mass balance, or sequence integrity |
| **Conformant smelter** | A smelter that has passed an independent RMAP audit and is listed on the RMI conformant smelter list |
| **Dodd-Frank §1502** | Section 1502 of the US Dodd-Frank Wall Street Reform Act, which requires SEC-reporting companies to disclose their use of conflict minerals |
| **Form SD** | Specialized Disclosure form filed with the SEC by companies reporting on conflict mineral sourcing under Dodd-Frank §1502 |
| **Mass balance** | A compliance check that verifies whether the weight of material out of a process step is consistent with the weight in, within a defined tolerance (5%) |
| **Sanctions screening** | A check that compares actors in the custody chain against UN and EU sanctions lists |
| **Sequence integrity** | A check that verifies custody events are in correct chronological order and that the SHA-256 hash chain linking them is unbroken |

### Platform Terms

| Term | Definition |
|------|-----------|
| **Batch** | A discrete, trackable quantity of mineral material that moves through the supply chain. Each batch has a unique number (e.g., W-2026-041), a mineral type, origin, weight, and compliance status |
| **Custody event** | A recorded action in the lifecycle of a batch — extraction, assay, concentration, trading, smelting, or export. Events are append-only and cryptographically chained |
| **Event type** | One of 6 stages in the custody chain: Mine Extraction, Laboratory Assay, Concentration, Trading/Transfer, Primary Processing (Smelting), Export/Shipment |
| **Hash chain** | A sequence of SHA-256 hashes where each event's hash includes the previous event's hash, creating a tamper-evident chain. If any event is altered, all subsequent hashes become invalid |
| **Material Passport** | A PDF document summarising a batch's verified custody chain, compliance status, and a QR code for public verification. Designed to be shared with customers and auditors |
| **Audit Dossier** | A comprehensive PDF for formal audits containing the complete event log, all compliance check details, document references with file hashes, and hash chain verification |
| **Tenant** | An organisation (company) on the auditraks platform. Each tenant has isolated data, separate users, and its own subscription |
| **Custody chain** | The complete sequence of custody events from mine to refinery, forming a chronological record of who handled the material, when, and where |
| **Idempotency key** | A unique identifier sent with each event to prevent duplicate submissions. If the same key is submitted twice, the second is rejected |
| **Correction** | A special custody event that amends a previous event. Both the original and correction remain visible in the timeline for audit transparency |
| **Pending event** | An event created on the mobile PWA while offline, stored locally in IndexedDB, waiting to be synced to the server when connectivity returns |
| **Share token** | A cryptographically random URL-safe string that grants time-limited (30-day) public access to a Material Passport or Audit Dossier without requiring login |

### Roles

| Role | Description |
|------|-----------|
| **Supplier** | Creates batches and logs custody events. Sees only their own batches. Can generate and share Material Passports for compliant batches |
| **Buyer** | Reviews all batches in the tenant. Monitors supplier engagement. Generates Form SD support packages. Imports CMRT data |
| **Tenant Admin** | Manages users, reviews compliance, views audit logs, manages billing for their organisation |
| **Platform Admin** | Full access across all tenants. Manages RMAP smelter data, tenant lifecycle, platform analytics, and background jobs |

---

## 10. FAQ

**How do I fix an event I submitted with wrong information?**
Events cannot be edited or deleted after submission — this is by design to maintain data integrity. Submit a Correction event referencing the original event's ID. Enter the corrected values and a written explanation. Both the original event and the correction remain visible in the timeline. Compliance checks are re-run after a correction.

**My batch is showing FLAGGED after I submitted a smelting event. What do I do?**
The most likely cause is an unrecognized Smelter ID. First, verify you entered the ID correctly — even a small typo will cause a failed RMAP check. If the ID is correct, contact your Tenant Admin. The RMAP smelter list may need to be updated, or the smelter's certification may have lapsed.

**My batch shows INSUFFICIENT_DATA. How do I resolve it?**
Open the batch and go to the Compliance Checks tab. The compliance engine will indicate which checks are inconclusive and what information is missing. Add the required events or documents and the status will update automatically.

**Why has my DRC-origin batch been flagged immediately?**
DRC (Democratic Republic of Congo) is classified as a conflict-affected and high-risk area under OECD guidance. Any batch with a DRC origin country will automatically trigger an OECD DDG flag. This is expected behavior. You can continue adding events and pursuing verification — your Tenant Admin or Platform Admin can override the flag with documented justification if your sourcing practices meet OECD requirements.

**The Material Passport button is greyed out. Why?**
Material Passports can only be generated for batches with a **COMPLIANT** status. Resolve any compliance flags or add missing data to satisfy the INSUFFICIENT_DATA checks, then try again.

**A shared link I sent has expired. What do I do?**
Shared links are valid for 30 days. Generate a new shared link from the batch detail view and send the updated link to the recipient.

**Can I split a batch after it has been marked completed?**
No. A completed batch cannot be split. You must split the batch while it is still in CREATED or ACTIVE status.

**How do I verify that a Material Passport is genuine?**
Scan the QR code on the passport with any smartphone. This opens the public batch verification page at `accutrac.org/verify/{batchId}`, which shows the live compliance status directly from the database. Alternatively, navigate to that URL in any browser.

**Is my data secure?**
Yes. All data in transit is encrypted using TLS. Data at rest is encrypted. Access to batch data is restricted to authorized users in the relevant organization. Every action is logged in the tamper-evident audit log.

**Can multiple people in my organization use auditraks?**
Yes. Each person should have their own individual account. Contact your Tenant Admin to invite additional users. Sharing login credentials is not permitted and can compromise your audit trail.

**What is the difference between a Material Passport and an Audit Dossier?**
A **Material Passport** is a concise summary designed for sharing with customers and downstream parties. It includes the custody chain summary, compliance status, and a QR verification code. An **Audit Dossier** is a comprehensive document for formal due diligence — it contains the complete event log, all compliance check details, document references with file hashes, and full hash chain verification. Use the Audit Dossier for regulatory submissions and formal audits.

**How is this different from blockchain?**
auditraks uses SHA-256 hash chains — the same tamper-evidence guarantee as blockchain, but without the cost, latency, or environmental overhead. Every event is cryptographically linked to the previous one. If any record were altered, the chain would break and the tampering would be immediately detectable.

**What happens when my trial ends?**
At the end of your 60-day trial, your subscription converts to the plan you selected at sign-up and your card on file is charged. You can upgrade, downgrade, or cancel at any time via the Stripe billing portal accessible from the Admin Dashboard. Cancellations take effect at the end of the current billing period.

**My invitation email never arrived. What should I do?**
Check your spam or junk folder first. If the email is not there, ask your Tenant Admin to resend the invitation. Make sure your organization's email system allows messages from the auditraks platform address. Invitation links expire after 7 days.

**What do I do if I suspect a data integrity issue?**
Contact your Tenant Admin immediately. Do not attempt to correct anything yourself. Admins can run an integrity check on any batch and escalate to the auditraks support team if needed.

---

*auditraks User Manual — Version 3.0 — March 2026*

*This document is provided for guidance. Features and interfaces may be updated as the platform evolves. For the most current information, refer to the latest version of this manual.*
