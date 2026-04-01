# auditraks Platform Walkthrough

**Complete guide to every key feature across all roles**

**Version:** 4.0 — April 2026
**URL:** https://auditraks.com

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [Public Features](#2-public-features)
3. [Supplier Portal](#3-supplier-portal)
4. [Buyer Portal](#4-buyer-portal)
5. [Admin Dashboard — Tenant Admin](#5-admin-dashboard--tenant-admin)
6. [Admin Dashboard — Platform Admin](#6-admin-dashboard--platform-admin)
7. [Compliance Framework](#7-compliance-framework)
8. [Notifications & Reminders](#8-notifications--reminders)
9. [Quick Reference](#9-quick-reference)
10. [Glossary — Terms, Acronyms & Definitions](#10-glossary--terms-acronyms--definitions)

---

## Demo Accounts

| Role | Email | Password | Portal |
|------|-------|----------|--------|
| Supplier | `supplier@auditraks.com` | `Demo1234!` | `/supplier` |
| Buyer | `buyer@auditraks.com` | `Demo1234!` | `/buyer` |
| Tenant Admin | `admin@auditraks.com` | `Demo1234!` | `/admin` |
| Platform Admin | `julianshaw2000@gmail.com` | `Auditraks2026!` | `/admin` |

### Pre-Seeded Batches

| Batch | Mineral | Origin | Compliance | Events | Purpose |
|-------|---------|--------|------------|--------|---------|
| W-2026-041 | Tungsten (Wolframite) | Rwanda, Nyungwe Mine | COMPLIANT | 6 | Full journey — all checks pass |
| W-2026-038 | Tungsten (Wolframite) | DRC, Bisie Mine | FLAGGED | 4 | DRC origin triggers OECD DDG flag |
| W-2026-045 | Tungsten (Wolframite) | Bolivia, Huanuni Mine | PENDING | 0 | Empty — for live demo of adding events |
| W-2026-035 | Tungsten (Cassiterite) | Rwanda, Rutongo Mine | COMPLIANT | 5 | Cassiterite with tin trace |

---

## 1. Getting Started

### 1.1 Landing Page

1. Open https://auditraks.com
2. The landing page shows the platform value proposition, pricing tiers, and call-to-action buttons
3. Click **Login** to sign in, or **Start Free Trial** to create a new organization

### 1.2 Signing Up (New Organization)

1. Click **Start Free Trial** on the landing page
2. Choose a plan:
   - **Starter** — $99/month (50 batches, 5 users)
   - **Pro** — $249/month (unlimited)
3. Fill in: company name, your name, email address
4. Click **Start 60-day free trial**
5. Complete Stripe checkout (card details — not charged until trial ends)
6. You receive a **setup email** from `noreply@auditraks.com`
7. Click the link in the email → set your password
8. Log in at `/login`

### 1.3 Logging In

1. Navigate to https://auditraks.com/login
2. Enter email and password
3. Click **Sign In**
4. You are redirected to your role's dashboard:
   - Suppliers → `/supplier`
   - Buyers → `/buyer`
   - Admins → `/admin`

### 1.4 Password Reset

1. On the login page, click **Forgot password?**
2. Enter your email address
3. Check your inbox for a reset link from `noreply@auditraks.com`
4. Click the link → enter a new password
5. Log in with the new password

---

## 2. Public Features

These features work without logging in.

### 2.1 Batch Verification

1. Open `https://auditraks.com/verify/{batchId}` (or scan the QR code on a Material Passport)
2. The page shows:
   - Batch identification (number, mineral, origin)
   - Compliance status with color-coded badge
   - Custody chain event count
   - Hash chain integrity status

### 2.2 Shared Document Access

1. Open a shared document link (`https://auditraks.com/shared/{token}`)
2. The Material Passport or Audit Dossier is displayed
3. Links expire after 30 days

---

## 3. Supplier Portal

**Login as:** `supplier@auditraks.com` / `Demo1234!`

### 3.1 Onboarding Checklist

*First-time suppliers see a guided checklist at the top of the dashboard.*

1. After logging in, the **Getting Started** card appears with 3 steps:
   - Create your first batch
   - Submit a custody event
   - Review compliance status
2. Progress bar fills as steps are completed
3. Each step links directly to the relevant page
4. Click the X button to dismiss permanently

### 3.2 Supplier Dashboard

1. Navigate to `/supplier`
2. The dashboard shows:
   - **Stat cards** — Total batches, Compliant, Flagged, Pending counts
   - **Search bar** — filter batches by number, mineral, or country
   - **Status filter** — dropdown to show ALL, COMPLIANT, FLAGGED, PENDING
   - **Batch cards** — each showing batch number, mineral, origin, weight, compliance badge, event count

### 3.3 Creating a Batch

1. Click **New Batch** on the dashboard
2. Fill in the form:

| Field | How to use |
|-------|-----------|
| Batch Number | Unique identifier (e.g., `W-2026-050`) |
| Mineral Type | Select from dropdown (Tungsten Wolframite/Cassiterite, Tin, Tantalum, Gold) |
| Origin Country | **Typeahead** — start typing a country name or ISO code, select from dropdown |
| Mine Site | Name of the extraction site |
| Estimated Weight (kg) | Weight of material at creation |

3. Click **Create Batch**
4. The batch appears on your dashboard with PENDING status

### 3.4 Submitting a Custody Event

1. Click **Submit Event** in the sidebar, or click **Add Event** from a batch detail page
2. **Batch selection** — typeahead search by batch number, mineral, country, or ID. Select from dropdown.
3. Select **Event Type** — the form adapts to show event-specific fields:

| Event Type | Key Fields |
|-----------|------------|
| Mine Extraction | GPS coordinates, mine operator, certificate ref |
| Concentration | Facility name, input/output weight, concentration ratio |
| Trading/Transfer | Seller, buyer, transfer date, contract ref |
| Laboratory Assay | Lab name, assay method, tungsten content %, certificate ref |
| Primary Processing (Smelting) | **Smelter search** (typeahead against RMAP database), process type, weights |
| Export/Shipment | Origin/destination country, transport mode, export permit ref |

4. Fill in common fields: event date, location, actor name, description
5. Click **Submit Event**
6. The event is added to the batch's custody chain with a SHA-256 hash

### 3.5 Viewing Batch Details

1. Click any batch card on the dashboard
2. The detail page has 5 tabs:

| Tab | Shows |
|-----|-------|
| **Overview** | Batch info card (mineral, weight, status, compliance) + event timeline |
| **Events** | Chronological list of all custody events with hashes |
| **Documents** | All documents attached to events — click to download |
| **Compliance** | Detailed compliance check results (RMAP, OECD DDG, Sanctions, Mass Balance, Sequence) |
| **Activity** | Full audit log of every action taken on the batch |

### 3.6 Uploading Documents

1. Open a batch detail page → **Documents** tab
2. Enter the Event ID the document relates to
3. Select document type (Certificate of Origin, Assay Report, Transport Document, etc.)
4. Click **Choose File** and select a PDF, JPEG, PNG, TIFF, or GIF (max 25MB)
5. Click **Upload**
6. The document is stored with a SHA-256 file hash for integrity verification

### 3.7 Material Passport Sharing

*Available when a batch reaches COMPLIANT status.*

1. Open a COMPLIANT batch detail page
2. The **"Material Passport Ready"** card appears with three actions:

| Action | What it does |
|--------|-------------|
| **Download PDF** | Generates and downloads the passport as a PDF with QR code, custody chain, compliance summary |
| **Copy Link** | Creates a 30-day shareable URL and copies it to clipboard |
| **Email to Customer** | Opens an inline form — enter recipient email and optional message. Sends a branded email with the passport link |

3. Recipients can view the passport without an auditraks account
4. Links expire after 30 days

---

## 4. Buyer Portal

**Login as:** `buyer@auditraks.com` / `Demo1234!`

### 4.1 Buyer Dashboard

1. Navigate to `/buyer`
2. The dashboard shows three sections:

**Compliance Overview:**
- Donut chart showing batch distribution by compliance status
- Stat cards: Compliant (green), Flagged (amber), Pending (grey), Insufficient (red)

**Supplier Engagement Panel:**
- 4 metric cards: Total Suppliers, Active, Stale, Flagged
- Click **View suppliers** to expand the supplier table:
  - Columns: Supplier name, Last Activity, Batches, Flagged, Status, Action
  - Status badges: Active (green), Stale (amber), Flagged (red), New (grey)
  - **Remind** button on stale/flagged suppliers — sends a nudge email

**Batch Table:**
- All batches across the supply chain
- Click any row to view batch detail

### 4.2 Supplier Engagement & Nudge

1. On the buyer dashboard, expand the **Supplier Engagement** panel
2. Review supplier health: which are active, which are stale (no events in 90+ days), which have flagged batches
3. Click **Remind** on a stale or flagged supplier
4. The supplier receives:
   - A branded email: "{Company} is requesting an update on your supply chain data"
   - An in-app notification
5. Rate limit: one nudge per supplier per 7 days

### 4.3 Viewing Batches

1. Click any batch in the table
2. Same tabbed detail view as the supplier portal (Overview, Events, Documents, Compliance, Activity)
3. Buyers see all batches across the tenant — not limited to their own

### 4.4 Form SD Compliance (Dodd-Frank §1502)

1. Click **Form SD** in the sidebar
2. The Form SD dashboard shows:

**Filing Cycles:**
- Current and upcoming reporting years
- Status: Open, Due Soon, Closed
- Click to manage a cycle

**For each filing cycle:**
- **Generate Support Package** — creates a ZIP with all required Form SD documentation
- **AI-Powered Analysis:**
  - **Supply Chain Description** — auto-generated narrative of the custody chain
  - **Due Diligence Summary** — risk assessment and mitigation measures
  - **Risk Assessment** — identifies CAHRA countries, conflict zones, sanctions concerns

3. Click **Generate Package** to create the support package
4. Download the ZIP when ready

### 4.5 CMRT Import

1. Click **CMRT Import** in the sidebar
2. **Upload:** Drag and drop a CMRT v6.x `.xlsx` file, or click to browse
3. **Preview:** The platform parses the file and shows:
   - Declaration summary (company, reporting year, scope)
   - Match statistics (total smelters, matched in RMAP, unmatched)
   - Smelter table — each row shows metal type, name, ID, country, and match status:
     - Green = matched with conformance status
     - Amber = unmatched (not in RMAP database)
   - Parsing errors (if any)
4. **Confirm:** Click **Confirm Import** to save smelter associations
   - Matched smelters saved as "verified"
   - Unmatched smelters with IDs saved as "unverified" for later resolution
5. **Import History:** Below the upload area — shows past imports with stats

---

## 5. Admin Dashboard — Tenant Admin

**Login as:** `admin@auditraks.com` / `Demo1234!`

### 5.1 Admin Dashboard

1. Navigate to `/admin`
2. **Status banner** shows trial/plan status with **Manage Billing** button
3. Metric cards: Users, Batches, Compliance Flags
4. Quick action links to all admin sections

### 5.2 Onboarding Wizard (First Login)

*Appears on first admin login.*

1. **Welcome** — overview of the platform
2. **Invite Your Team** — add supplier and buyer users
3. **Create Your First Batch** — start tracking
4. **Run Compliance Checks** — review results

### 5.3 User Management

1. Click **Users** in the sidebar
2. **Invite a user:**
   - Click **Invite User**
   - Enter email, display name, select role (Supplier, Buyer, Tenant Admin)
   - Click **Send Invitation**
   - The user receives a setup email with a password link
3. **User table** shows: Name, Email, Role, Status (Active/Inactive), Last Login
4. Toggle users active/inactive
5. Delete users

### 5.4 Compliance Review

1. Click **Compliance** in the sidebar
2. View all batches with compliance issues
3. Each batch shows detailed check results:
   - RMAP (smelter verification)
   - OECD DDG (origin country risk)
   - Sanctions (actor screening)
   - Mass Balance (weight reconciliation)
   - Sequence (hash chain integrity)

### 5.5 Audit Log

1. Navigate to `/admin/audit-log`
2. View chronological log of every action in your tenant
3. **Filter by:**
   - Date range
   - User
   - Action type
   - Entity type
4. Click **Export CSV** to download the full log

### 5.6 API Keys

1. Navigate to `/admin/api-keys`
2. **Create a key:**
   - Click **Create API Key**
   - Enter a name
   - Copy the full key (shown once only)
3. **Manage keys:** View last used date, revoke when needed

### 5.7 Billing Management

1. Click **Manage Billing** in the status banner or sidebar
2. Opens the **Stripe Customer Portal**:
   - View plan and next billing date
   - Update payment method
   - Download invoices
   - Upgrade/downgrade plan
   - Cancel subscription

---

## 6. Admin Dashboard — Platform Admin

**Login as:** `julianshaw2000@gmail.com` / `Auditraks2026!`

*Platform Admin sees everything Tenant Admin sees, plus:*

### 6.1 Tenant Management

1. Navigate to `/admin/tenants`
2. View all organizations on the platform
3. Each tenant shows: Name, Status, Users, Batches, Created Date
4. **Create tenant** — set up a new organization
5. **Update status** — Active, Suspended, Trial
6. **Cross-tenant analytics** — filter any view by tenant

### 6.2 RMAP Smelter Data

1. Navigate to `/admin/rmap`
2. View the full RMAP smelter database:
   - Smelter ID, Name, Country, Conformance Status, Mineral Type
3. **Upload updated RMAP list** — import CSV/Excel from RMI
4. The smelter database is used for:
   - Compliance checks (is this smelter RMAP conformant?)
   - Smelter typeahead in event submission
   - CMRT import matching

### 6.3 Job Monitor

1. Navigate to `/admin/jobs`
2. View all background jobs:
   - Document generation (passports, dossiers, support packages)
   - Compliance checks
   - Email delivery
3. Each job shows: Type, Status (Pending/Processing/Complete/Failed), timestamps

### 6.4 Analytics

1. Navigate to `/admin/analytics`
2. Platform-wide metrics:
   - Total batches, compliance rates
   - Distribution by mineral type and origin country
   - Monthly activity trends (6-month chart)
   - User activity

### 6.5 Data Quality

1. Navigate to `/admin/data-quality`
2. Batch completeness scores
3. Data anomaly reports

### 6.6 Platform AI

1. Navigate to `/admin/platform-ai`
2. AI-powered insights:
   - Churn prediction
   - Tenant health scores
   - Revenue analysis

---

## 7. Compliance Framework

Every batch is checked against 5 compliance frameworks automatically after each custody event:

### 7.1 RMAP Conformance

- **What it checks:** Is the declared smelter on the RMAP conformant list?
- **PASS:** Smelter ID matches a CONFORMANT smelter in the RMAP database
- **FAIL:** Smelter not found, or status is NON_CONFORMANT
- **Example:** Batch W-2026-041 uses smelter CID001100 (Wolfram Bergbau, Austria) — CONFORMANT → PASS

### 7.2 OECD Due Diligence Guidance (DDG)

- **What it checks:** Does the origin country appear on the Conflict-Affected and High-Risk Areas (CAHRA) list?
- **PASS:** Origin country is not on the CAHRA list
- **FLAG:** Origin country is on the CAHRA list (e.g., DRC, Sudan, CAR)
- **Example:** Batch W-2026-038 originates from DRC → FLAGGED

### 7.3 Sanctions Screening

- **What it checks:** Are any actors in the custody chain on UN/EU sanctions lists?
- **PASS:** No matches found
- **FAIL:** Actor name matches a sanctioned entity

### 7.4 Mass Balance

- **What it checks:** Is the weight reconciliation within tolerance (5%)?
- **PASS:** Output weight is within 5% of input weight (accounting for processing losses)
- **FAIL:** Weight discrepancy exceeds 5%
- **Example:** W-2026-041: 450kg ore → 385kg concentrate → 310kg APT (31% total loss — within expected range for tungsten processing)

### 7.5 Sequence Integrity

- **What it checks:** Are events in the correct chronological order? Is the SHA-256 hash chain intact?
- **PASS:** Events are sequential, and each event's hash links correctly to the previous
- **FAIL:** Temporal anomaly detected, or hash chain is broken (indicates possible tampering)

### Compliance Status Summary

| Status | Badge | Meaning |
|--------|-------|---------|
| COMPLIANT | Green | All 5 checks pass |
| FLAGGED | Amber | One or more checks flagged for review |
| INSUFFICIENT_DATA | Red | Not enough data to run all checks |
| PENDING | Grey | Checks not yet triggered (no events) |

---

## 8. Notifications & Reminders

### 8.1 In-App Notifications

- Click the bell icon in the top bar to view notifications
- Types: compliance alerts, document generation, buyer nudges, stale warnings
- Click to mark as read

### 8.2 Automated Email Reminders

The platform sends automated reminders (no configuration needed):

| Reminder | Trigger | Recipient |
|----------|---------|-----------|
| Inactivity | Batch with no events for 30+ days (non-compliant) | Supplier |
| Stale warning | Supplier with no activity for 60+ days | Tenant Admin, Platform Admin |
| Compliance flag | Compliance check fails | Supplier, Admins |
| Escalation | Flag unresolved for 48+ hours | Platform Admin |

### 8.3 Manual Buyer Nudge

Buyers can manually remind suppliers from the engagement panel (one per supplier per 7 days).

### 8.4 Email Configuration

- **From:** `noreply@auditraks.com` (via Resend)
- **Reply-To:** `support@auditraks.com` (Zoho Mail inbox)
- Replies from users go to the Zoho mailbox for manual follow-up

---

## 9. Quick Reference

### Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `/` | Focus search bar (where available) |

### URL Structure

| Path | Role | Description |
|------|------|-------------|
| `/` | Public | Landing page |
| `/login` | Public | Sign in |
| `/signup` | Public | New organization signup |
| `/verify/{batchId}` | Public | Batch verification |
| `/shared/{token}` | Public | Shared document access |
| `/supplier` | Supplier | Supplier dashboard |
| `/supplier/batches/new` | Supplier | Create batch |
| `/supplier/submit` | Supplier | Submit custody event |
| `/supplier/batch/{id}` | Supplier | Batch detail |
| `/buyer` | Buyer | Buyer dashboard |
| `/buyer/form-sd` | Buyer | Form SD compliance |
| `/buyer/cmrt-import` | Buyer | CMRT import |
| `/buyer/batch/{id}` | Buyer | Batch detail |
| `/admin` | Admin | Admin dashboard |
| `/admin/users` | Admin | User management |
| `/admin/compliance` | Admin | Compliance review |
| `/admin/audit-log` | Admin | Audit log |
| `/admin/api-keys` | Admin | API key management |
| `/admin/tenants` | Platform Admin | Tenant management |
| `/admin/rmap` | Platform Admin | RMAP smelter data |
| `/admin/jobs` | Platform Admin | Background job monitor |
| `/admin/analytics` | Platform Admin | Platform analytics |
| `/admin/data-quality` | Platform Admin | Data quality reports |
| `/admin/platform-ai` | Platform Admin | AI insights |

### Feature Access by Role

| Feature | Supplier | Buyer | Tenant Admin | Platform Admin |
|---------|:--------:|:-----:|:------------:|:--------------:|
| Create batch | Yes | — | Yes | Yes |
| Submit event | Yes | — | — | — |
| View batches | Own | All | All | All tenants |
| Material Passport | Generate + Share | View | View | View |
| Form SD | — | Yes | — | — |
| CMRT Import | — | Yes | — | — |
| Supplier Engagement | — | Yes | — | — |
| Manage Users | — | — | Yes | Yes |
| Compliance Review | — | — | Yes | Yes |
| Audit Log | — | — | Tenant | All |
| API Keys | — | — | Yes | Yes |
| Manage Billing | — | — | Yes | — |
| RMAP Data | — | — | — | Yes |
| Tenant Management | — | — | — | Yes |
| Job Monitor | — | — | — | Yes |
| Analytics | — | — | — | Yes |

---

## 10. Glossary — Terms, Acronyms & Definitions

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
