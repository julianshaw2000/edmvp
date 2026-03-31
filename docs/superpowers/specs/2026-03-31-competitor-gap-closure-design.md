# Competitor Workflow Gap Closure — Design Spec

**Date:** 2026-03-31
**Status:** Draft
**Scope:** 5 competitive workflow gaps identified in `docs/roi/CLAUDE_CODE_COMPETITOR_WORKFLOW_PROMPT.md`

---

## Overview

This spec defines the design for closing 5 competitive workflow gaps between Auditraks and the top conflict minerals compliance platforms (Source Intelligence, iPoint, Minespider, Circulor). The gaps are implemented in dependency order across 4 phases, delivering visible value to suppliers first.

## Phasing Strategy

| Phase | Gap | Effort | Backend Changes | Why This Order |
|-------|-----|--------|-----------------|----------------|
| A | GAP-5: Supplier Onboarding Checklist | Low | None | Quick win, immediate UX improvement |
| A | GAP-2: Material Passport Sharing UI | Low-Med | 1 endpoint + 1 email template | Backend exists, needs frontend + email send |
| B | GAP-3: Supplier Engagement Metrics | Medium | 1 endpoint (derived query) | Buyer intelligence, prerequisite for GAP-4 |
| C | GAP-4: Automated Supplier Reminders | Medium | Worker service + 1 endpoint + 1 column + 2 templates | Requires GAP-3 data to be meaningful |
| D | GAP-1: CMRT v6.x Import | High | Parser service + endpoint + entity + buyer UI page | Heaviest lift, deferred but critical for adoption |

---

## Phase A: Supplier Experience Quick Wins

### GAP-5: Supplier Onboarding Checklist

**Problem:** Suppliers who accept an invitation and log in for the first time see a raw dashboard with no guidance. Competitors (iPoint) use guided onboarding to reduce friction and drive first actions.

**Solution:** A dismissable checklist card at the top of the supplier dashboard guiding suppliers through 3 first actions.

**Checklist steps:**
1. **Create your first batch** — links to `/supplier/batches/new`. Complete when `batches().length > 0`.
2. **Submit a custody event** — links to `/supplier/submit`. Complete when any batch has `eventCount > 0`.
3. **Review compliance status** — links to first batch detail. Complete when supplier clicks through.

**Implementation:**
- New `SupplierOnboardingComponent` rendered at top of `supplier-dashboard.component.ts`
- Progress derived reactively from `SupplierFacade.batches()` signal — no API calls needed
- Step 3 tracked via localStorage key `auditraks_supplier_viewed_compliance`
- Entire checklist dismissable via localStorage key `auditraks_supplier_onboarding_dismissed`
- Shows on every login until dismissed or all 3 steps complete
- Progress bar showing X/3 complete

**Backend changes:** None.

**Files affected:**
- New: `packages/web/src/app/features/supplier/ui/supplier-onboarding.component.ts`
- Modified: `packages/web/src/app/features/supplier/supplier-dashboard.component.ts` (add component to template)

---

### GAP-2: Material Passport Sharing UI

**Problem:** The backend generates Material Passports and supports shareable links with 30-day tokens, but suppliers have no UI to access this. The doc identifies this as the key supplier value proposition — participation generates a marketable asset.

**Solution:** A prominent "Material Passport" card on the supplier batch detail page when a batch is COMPLIANT, with download, copy-link, and email-to-customer actions.

**Supplier batch detail UI (when complianceStatus === 'COMPLIANT'):**
- Card with headline: "Your Material Passport is ready — share with your customers"
- **Download PDF** button — calls existing `POST /api/batches/{id}/passport`
- **Copy Share Link** button — calls existing `POST /api/documents/{id}/share`, copies URL to clipboard, shows "Copied!" toast
- **Email to Customer** button — expands inline form with:
  - Recipient email (required)
  - Optional message text
  - Send button → calls new endpoint

**Supplier dashboard enhancement:**
- Batches with COMPLIANT status show a small document icon badge in the batch table, indicating a passport is available

**New API endpoint:** `POST /api/documents/{id}/share-email`
- Request: `{ recipientEmail: string, message?: string }`
- Validates document belongs to user's tenant
- Calls `ShareDocument` to get/create the share token if one doesn't exist
- Sends branded email via `IEmailService` with share link and optional message
- Returns `{ success: true, shareUrl: string }`

**New email template** in `EmailTemplates.cs`: `PassportShared`
- Subject: "Material Passport shared with you — {batchNumber}"
- Body: Branded email with passport link, batch summary, optional sender message
- Footer: "This link expires in 30 days"

**Backend changes:**
- New: `Features/DocumentGeneration/ShareDocumentEmail.cs` (MediatR handler)
- Modified: `Common/Services/EmailTemplates.cs` (add PassportShared template)

**Frontend changes:**
- New: `packages/web/src/app/features/supplier/ui/passport-share-card.component.ts`
- Modified: `packages/web/src/app/features/supplier/batch-detail.component.ts`

---

## Phase B: Buyer Intelligence

### GAP-3: Supplier Engagement Metrics

**Problem:** The buyer dashboard shows batch compliance status but no supplier-level engagement data. Competitors (Source Intelligence, iPoint) surface supplier response rates as a primary buyer KPI.

**Solution:** A new "Supplier Engagement" panel on the buyer dashboard with 4 metric cards and an expandable supplier activity list.

**New API endpoint:** `GET /api/buyer/supplier-engagement`

Response shape:
```json
{
  "totalSuppliers": 12,
  "activeSuppliers": 8,
  "staleSuppliers": 3,
  "flaggedSuppliers": 1,
  "suppliers": [
    {
      "id": "guid",
      "displayName": "Acme Mining Co",
      "lastEventDate": "2026-03-15T...",
      "batchCount": 4,
      "flaggedBatchCount": 1,
      "status": "flagged"
    }
  ]
}
```

**Status derivation (hardcoded thresholds):**
- `active` — at least one custody event in the last 90 days
- `stale` — has batches but no events in 90+ days
- `flagged` — at least one batch with complianceStatus FLAGGED
- `new` — has user account but no batches yet

**Query:** Joins `Users` (role=SUPPLIER, tenantId=buyer's tenant) → `Batches` (createdBy) → `CustodyEvents` (latest eventDate). No new tables — pure derived aggregation.

**Buyer dashboard UI:**
- New section between compliance overview and batch table
- 4 metric cards in a row: Total / Active (green) / Stale (amber) / Flagged (red) — same card style as existing compliance stats
- Expandable supplier list below cards, sorted by status (flagged first, then stale, then active)
- Each row: supplier name, last activity date, batch count, status badge
- Stale/flagged rows have subtle amber/red left border

**Backend changes:**
- New: `Features/Buyer/GetSupplierEngagement.cs` (MediatR query + handler)
- New: `Features/Buyer/BuyerEndpoints.cs` (or extend existing)

**Frontend changes:**
- New: `packages/web/src/app/features/buyer/ui/supplier-engagement-panel.component.ts`
- Modified: `packages/web/src/app/features/buyer/buyer-dashboard.component.ts`
- Modified: `packages/web/src/app/features/buyer/buyer.facade.ts` (add engagement data)

---

## Phase C: Proactive Engagement

### GAP-4: Automated Supplier Reminders + Manual Nudge

**Problem:** Auditraks notifications are reactive (compliance status changes). Competitors automate supplier outreach (campaigns, reminders, escalations). The doc identifies every email touchpoint as a supplier engagement lever.

**Solution:** Two automated daily reminders + a manual nudge button on the buyer dashboard.

### Automated Reminders

**New worker service:** `SupplierReminderService` (daily `BackgroundService`)

**Reminder 1 — Inactivity:**
- Trigger: Batch where status != COMPLIANT and latest custody event is >30 days old
- Recipient: Batch creator (supplier)
- Dedup: Only sends if `LastReminderSentAt` on batch is null or >30 days ago
- Updates `LastReminderSentAt` after sending

**Reminder 2 — Stale warning:**
- Trigger: Batch where latest custody event is >60 days old (approaching 90-day stale threshold from GAP-3)
- Recipient: Batch creator (supplier)
- Escalation: Also notifies the buyer's tenant admins that a supplier is going stale

### Manual Nudge

**New API endpoint:** `POST /api/buyer/nudge-supplier`
- Request: `{ supplierId: string }`
- Validates supplier is in buyer's tenant
- Rate limit: One nudge per supplier per 7 days (returns 429 if too soon)
- Sends branded email to supplier: "Your buyer is requesting an update"
- Creates in-app notification for the supplier

**Buyer dashboard integration:**
- Each supplier row in the engagement panel (GAP-3) gets a "Send Reminder" button
- Button disabled with tooltip "Sent X days ago" if within 7-day cooldown
- Success toast: "Reminder sent to {supplier name}"

### Database changes

- New column: `BatchEntity.LastReminderSentAt` (nullable DateTime)
- Migration required

### New email templates (2)

1. `BatchInactivityReminder` — "Your batch {batchNumber} needs attention"
2. `BuyerNudge` — "{companyName} is requesting an update on your supply chain data"

### New notification types

- `INACTIVITY_REMINDER`
- `BUYER_NUDGE`

**Backend changes:**
- New: `packages/worker/Tungsten.Worker/Services/SupplierReminderService.cs`
- New: `Features/Buyer/NudgeSupplier.cs`
- Modified: `Common/Services/EmailTemplates.cs` (2 templates)
- Modified: `Infrastructure/Persistence/Entities/BatchEntity.cs` (add column)
- New migration

**Frontend changes:**
- Modified: `packages/web/src/app/features/buyer/ui/supplier-engagement-panel.component.ts` (nudge button)

---

## Phase D: CMRT Data Ingestion

### GAP-1: CMRT v6.x Import

**Problem:** Buyers switching from Source Intelligence or iPoint cannot bring their existing supplier data. CMRT import is the standard data ingestion mechanism in this market. This is an adoption blocker for the pilot.

**Solution:** A two-step upload-then-confirm CMRT import flow on the buyer portal, parsing CMRT v6.x Excel workbooks.

### CMRT v6.x Parser

**Library:** ClosedXML (MIT, .NET-native, no COM dependency)

**Extracted data from CMRT v6.x workbook:**

From **Declaration tab:**
- Company name, contact info, reporting year
- Declaration scope (company-wide vs. product-level)

From **Smelter List tab:**
- Smelter name, smelter ID (RMAP ID if available), metal type
- Smelter country, sourcing status
- Matched against existing `SmelterEntity` records by ID or name+country

From **Product List tab:**
- 3TG presence declarations per product
- Mapped to mineral types for batch creation context

### Import Flow

**Step 1 — Upload & Parse (dry run):**
- Buyer uploads `.xlsx` file at `/buyer/cmrt-import`
- Backend parses, returns a preview:
  - Matched smelters (green) — found in RMAP database
  - Unmatched smelters (amber) — not found, flagged for manual review
  - Parsing errors (red) — malformed rows, missing required fields
  - Summary: X smelters, Y matched, Z unmatched, W errors

**Step 2 — Confirm:**
- Buyer reviews preview, can exclude rows
- On confirm, backend creates:
  - Smelter associations for matched entries
  - Import record for audit trail
- Unmatched smelters are saved as "unverified" for later resolution
- Does NOT auto-create supplier user accounts (manual step)

### New backend pieces

- New: `Features/Buyer/ImportCmrt.cs` (MediatR handler, two-phase: preview + confirm)
- New: `Common/Services/CmrtParserService.cs` (ClosedXML-based parser)
- New: `Infrastructure/Persistence/Entities/CmrtImportEntity.cs` — tracks import history (filename, importDate, rowsParsed, rowsMatched, errors, importedBy)
- New migration for `CmrtImports` table
- NuGet: `ClosedXML` package

### Buyer portal UI

- New route: `/buyer/cmrt-import`
- New sidebar link under Buyer: "CMRT Import"
- Page components:
  - File upload dropzone (accepts `.xlsx` only)
  - Preview table with match status indicators
  - Confirm/Cancel buttons
  - Import history list (past imports with date, file, stats)

**Frontend changes:**
- New: `packages/web/src/app/features/buyer/cmrt-import.component.ts`
- Modified: `packages/web/src/app/features/buyer/buyer.routes.ts`
- Modified: `packages/web/src/app/core/layout/sidebar.component.ts` (add CMRT Import link for BUYER role)

---

## What We Explicitly Do Not Build

Per the competitor analysis document:
- No CMRT export (Material Passport is the superior deliverable)
- No mass-balance traceability (discrete batch tracking is the differentiator)
- No contract-based supplier mandates (incentive-driven, not enforcement-driven)
- No AI-driven CMRT analysis (deterministic rule evaluation only)
- No multilingual UI (US-only pilot market)
- No configurable reminder thresholds (fixed rules for pilot)
- No CMRT v5.x support (v6.x only)

---

## Tech Stack Notes

- All new endpoints follow existing Vertical Slice + MediatR CQRS pattern
- All new components are Angular 21+ standalone with signal-first state
- Email via existing Resend integration (`IEmailService`)
- Background jobs via existing worker infrastructure (`BackgroundService`)
- Excel parsing via ClosedXML (new dependency, Phase D only)
- No new external services required
