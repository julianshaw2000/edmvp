# Competitor Gap Closure — Test Walkthrough

**Purpose:** Step-by-step manual for testing all features added in the competitor gap closure implementation (GAP-1 through GAP-5).

**Prerequisites:**
- Platform deployed and running (API + frontend + worker)
- At least one tenant with demo users seeded (see `docs/manuals/demo-accounts.md`)
- Access to supplier, buyer, and admin accounts

**Estimated time:** 20–30 minutes

---

## 1. Supplier Onboarding Checklist (GAP-5)

**Login as:** A supplier account (e.g., Maria Uwimana — `maria@pilottenant.com`)

### Test Steps

1. **Clear previous state** — Open browser dev tools → Application → Local Storage → delete keys starting with `auditraks_supplier_`
2. **Refresh the page** — navigate to `/supplier`
3. **Verify checklist appears** at the top of the dashboard:
   - [ ] "Getting Started" card is visible with "0/3 steps complete"
   - [ ] Progress bar is at 0%
   - [ ] Three steps listed: Create batch, Submit event, Review compliance
   - [ ] Each step shows an empty circle (not checked)

4. **Test Step 1 — Create a batch:**
   - Click "Create your first batch" in the checklist
   - [ ] Navigates to `/supplier/batches/new`
   - Fill in: Batch Number `TEST-GAP5-001`, Mineral Type `Tungsten (Wolframite)`, Origin Country (start typing `Rwanda` — should show dropdown), Mine Site `Test Mine`, Weight `100`
   - [ ] Country typeahead works — shows filtered country list as you type
   - [ ] Selecting a country fills the field with `Rwanda (RW)` format
   - Click "Create Batch"
   - Navigate back to `/supplier`
   - [ ] Checklist now shows "1/3 steps complete"
   - [ ] Step 1 has a green checkmark and strikethrough text

5. **Test Step 2 — Submit an event:**
   - Click "Submit a custody event" in the checklist
   - [ ] Navigates to `/supplier/submit`
   - [ ] Batch field is a typeahead — start typing `TEST-GAP5` and see your batch appear
   - Select the batch, fill in event type (Mine Extraction), date, location, actor, description
   - Submit the event
   - Navigate back to `/supplier`
   - [ ] Checklist now shows "2/3 steps complete"

6. **Test Step 3 — Review compliance:**
   - Click "Review compliance status" in the checklist
   - [ ] Navigates to the batch detail page
   - [ ] Checklist now shows "3/3 steps complete"
   - Navigate back to `/supplier`
   - [ ] Checklist auto-hides (all steps complete)

7. **Test dismiss:**
   - Clear localStorage again and refresh
   - Click the X button on the checklist
   - [ ] Checklist disappears
   - Refresh the page
   - [ ] Checklist stays hidden (dismissed state persisted)

**Result:** PASS / FAIL

---

## 2. Material Passport Sharing — Supplier Side (GAP-2)

**Login as:** A supplier with a COMPLIANT batch (or use the platform admin to mark a batch compliant)

### Test Steps

1. **Navigate to a COMPLIANT batch** on the supplier dashboard
   - Open the batch detail page
   - [ ] "Material Passport Ready" card appears with indigo gradient
   - [ ] Three buttons visible: Download PDF, Copy Link, Email to Customer

2. **Test Download PDF:**
   - Click "Download PDF"
   - [ ] Button shows "Generating..." while loading
   - [ ] PDF opens in a new tab or downloads
   - [ ] PDF contains batch info, custody chain, compliance summary, QR code

3. **Test Copy Link:**
   - Click "Copy Link"
   - [ ] Button shows "Creating..." briefly, then "Copied!"
   - [ ] After ~2 seconds, button reverts to "Copy Link"
   - Paste from clipboard into a browser
   - [ ] Shared passport page loads without requiring login
   - [ ] Link shows passport content

4. **Test Email to Customer:**
   - Click "Email to Customer"
   - [ ] Inline form expands with email and message fields
   - Enter a test email address (your own email)
   - Enter optional message: "Please review our compliance documentation"
   - Click "Send Passport"
   - [ ] Button shows "Sending..."
   - [ ] Success message: "Passport sent successfully!"
   - [ ] Check your email inbox — branded email received from `support@auditraks.com`
   - [ ] Email contains passport link, sender name, batch number, your message
   - [ ] Email footer says "This link expires in 30 days"
   - Click the link in the email
   - [ ] Passport loads in browser

5. **Test non-COMPLIANT batch:**
   - Navigate to a batch that is FLAGGED or PENDING
   - [ ] Passport share card does NOT appear

**Result:** PASS / FAIL

---

## 3. Supplier Engagement Metrics — Buyer Dashboard (GAP-3)

**Login as:** A buyer account (e.g., Klaus Steinberger — `klaus@pilottenant.com`)

### Test Steps

1. **Navigate to Buyer Dashboard** (`/buyer`)
   - [ ] "Supplier Engagement" panel visible between compliance overview and batch table
   - [ ] Four metric cards: Total, Active, Stale, Flagged
   - [ ] Numbers correspond to actual supplier state in the tenant

2. **Test expand/collapse:**
   - Click "View suppliers"
   - [ ] Supplier table expands below the metric cards
   - [ ] Table shows columns: Supplier, Last Activity, Batches, Flagged, Status, Action
   - [ ] Suppliers sorted by status: flagged first, then stale, then new, then active
   - [ ] Status badges are color-coded (green=Active, amber=Stale, red=Flagged, grey=New)
   - [ ] Stale/flagged rows have a colored left border
   - Click "Collapse"
   - [ ] Table hides, only metric cards remain

3. **Verify metric accuracy:**
   - [ ] Total = number of SUPPLIER role users in this tenant
   - [ ] Active = suppliers with events in last 90 days
   - [ ] Stale = suppliers with batches but no events in 90+ days
   - [ ] Flagged = suppliers with at least one FLAGGED batch

**Result:** PASS / FAIL

---

## 4. Manual Supplier Nudge (GAP-4)

**Login as:** A buyer account

### Test Steps

1. **Expand the supplier engagement panel** on the buyer dashboard
2. **Find a stale or flagged supplier** in the list
   - [ ] "Remind" button visible in the Action column
   - Active or new suppliers should NOT have the button

3. **Send a nudge:**
   - Click "Remind" on a stale/flagged supplier
   - [ ] Button shows "Sending..." with disabled state
   - [ ] After success, button returns to normal
   - [ ] Check the supplier's email — branded nudge email received
   - [ ] Email subject: "{Company} is requesting an update on your supply chain data"

4. **Test rate limiting:**
   - Click "Remind" on the same supplier again
   - [ ] Error returned — "Reminder already sent X days ago. Please wait 7 days between reminders."
   - [ ] Button is re-enabled (rate limit is server-enforced, not hidden)

5. **Verify in-app notification (supplier side):**
   - Log in as the supplier who was nudged
   - [ ] Notification bell shows new notification
   - [ ] Notification says "{Company} is requesting an update on your supply chain data"

**Result:** PASS / FAIL

---

## 5. Automated Supplier Reminders (GAP-4 — Background Worker)

> **Note:** The `SupplierReminderService` runs once every 24 hours. To test it immediately, you would need to restart the worker service or temporarily reduce the interval. For manual verification, check the following:

### Verification Steps

1. **Check worker logs** in Render dashboard for the Background Worker service
   - [ ] Look for `SupplierReminderService` log entries
   - [ ] Verify it runs without errors

2. **Verify inactivity reminder setup:**
   - Create a batch with no events, with a `CreatedAt` date >30 days ago (via database)
   - After the worker runs:
   - [ ] Supplier receives "Your batch {batchNumber} needs attention" email
   - [ ] In-app notification created with type `INACTIVITY_REMINDER`
   - [ ] `LastReminderSentAt` column updated on the batch

3. **Verify stale warning setup:**
   - Ensure a supplier has no events across any batch for 60+ days
   - After the worker runs:
   - [ ] Tenant admins receive in-app notification: "Supplier going stale"
   - [ ] Notification type is `STALE_WARNING`

4. **Verify deduplication:**
   - [ ] Running the worker again does NOT send duplicate reminders (checks `LastReminderSentAt` and existing notifications)

**Result:** PASS / FAIL

---

## 6. CMRT v6.x Import (GAP-1)

**Login as:** A buyer account

### Test Steps

1. **Navigate to CMRT Import:**
   - [ ] "CMRT Import" link visible in the buyer sidebar (after Form SD)
   - Click it
   - [ ] Page loads at `/buyer/cmrt-import` with upload dropzone and import history

2. **Test file validation:**
   - Try uploading a `.pdf` file
   - [ ] Error: "Only .xlsx files are supported"
   - Try uploading without a file
   - [ ] Error shown appropriately

3. **Test upload and preview** (requires a CMRT v6.x .xlsx file):
   - Drag and drop a CMRT v6.x file onto the dropzone (or click to browse)
   - [ ] "Parsing CMRT file..." spinner appears
   - [ ] Preview loads with:
     - Declaration summary (company, reporting year, scope)
     - Match summary cards (Total Smelters, Matched in RMAP, Unmatched)
     - Smelter preview table with row numbers, metal types, names, IDs, countries
     - Matched smelters show green badge with conformance status
     - Unmatched smelters show amber "Unmatched" badge and light amber row background
   - [ ] Any parsing errors shown in red section

4. **Test cancel:**
   - Click "Cancel"
   - [ ] Preview clears, returns to upload dropzone

5. **Test confirm:**
   - Upload the file again
   - Click "Confirm Import"
   - [ ] Button shows "Importing..."
   - [ ] Success message: "Import complete: X associations created, Y skipped"
   - [ ] Import history table below updates with the new import

6. **Test import history:**
   - [ ] History shows: file name, company, matched count, unmatched count, import date
   - [ ] Most recent import appears first

7. **Test duplicate import:**
   - Upload and confirm the same file again
   - [ ] Associations that already exist are skipped (skipped count increases)
   - [ ] No duplicate `TenantSmelterAssociation` records created

**Result:** PASS / FAIL

---

## 7. TENANT_ADMIN Sidebar Fix

**Login as:** A tenant admin account

### Test Steps

1. **Check sidebar navigation:**
   - [ ] Sidebar shows "Overview" group with Dashboard link
   - [ ] Sidebar shows "Management" group with Users and Compliance links
   - [ ] Links navigate correctly

**Result:** PASS / FAIL

---

## 8. Batch ID Typeahead (Submit Event)

**Login as:** A supplier account

### Test Steps

1. Navigate to Submit Custody Event (`/supplier/submit`)
2. **Test batch search:**
   - [ ] Batch field shows "Search by batch number, mineral, or country..."
   - Start typing a batch number
   - [ ] Dropdown appears with matching batches
   - [ ] Each result shows: batch number, mineral type, country, weight, compliance status
   - Select a batch
   - [ ] Green confirmation bar appears with batch details
   - [ ] X button to clear selection

3. **Test search by different fields:**
   - Clear and type a mineral type (e.g., "Tungsten")
   - [ ] Batches with that mineral type appear
   - Clear and type a country (e.g., "Rwanda")
   - [ ] Batches from that country appear

4. **Test pre-filled batch ID:**
   - Navigate from a batch detail page via "Add Event" button
   - [ ] Batch is pre-selected in the typeahead

**Result:** PASS / FAIL

---

## 9. Country Typeahead (Create Batch)

**Login as:** A supplier account

### Test Steps

1. Navigate to Create Batch (`/supplier/batches/new`)
2. **Test country search:**
   - [ ] Origin Country field shows "Search country..."
   - Type "Con"
   - [ ] Dropdown shows "Congo (DRC) CD" and "Congo (Republic) CG"
   - Select "Congo (DRC)"
   - [ ] Field shows "Congo (DRC) (CD)"
   - [ ] Field border turns green

3. **Test search by ISO code:**
   - Clear and type "RW"
   - [ ] Rwanda appears in dropdown

4. **Test no match:**
   - Type "XYZ"
   - [ ] "No matching country found" message appears

**Result:** PASS / FAIL

---

## 10. Email Reply-To Header

### Test Steps

1. Trigger any transactional email (e.g., invite a user, reset password)
2. Check the received email headers
   - [ ] `Reply-To` header is set to `support@auditraks.com`
   - [ ] `From` is `noreply@auditraks.com`
3. Hit reply on the email
   - [ ] Reply is addressed to `support@auditraks.com`
   - [ ] Check Zoho Mail inbox — reply arrives in `support@auditraks.com` mailbox

**Result:** PASS / FAIL

---

## Summary Checklist

| # | Feature | Status |
|---|---------|--------|
| 1 | Supplier Onboarding Checklist (GAP-5) | |
| 2 | Material Passport Sharing (GAP-2) | |
| 3 | Supplier Engagement Metrics (GAP-3) | |
| 4 | Manual Supplier Nudge (GAP-4) | |
| 5 | Automated Supplier Reminders (GAP-4) | |
| 6 | CMRT v6.x Import (GAP-1) | |
| 7 | TENANT_ADMIN Sidebar Fix | |
| 8 | Batch ID Typeahead | |
| 9 | Country Typeahead | |
| 10 | Email Reply-To Header | |

**Tested by:** _______________
**Date:** _______________
**Overall result:** PASS / FAIL
