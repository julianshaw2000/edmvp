# auditraks Demo Walkthrough

**Duration:** ~12 minutes

---

## Setup: Open 3 browser windows

- **Window 1:** Incognito — Supplier account
- **Window 2:** Incognito — Buyer account (`julianshawuk2000@gmail.com`)
- **Window 3:** Incognito — Admin account (sign in with a different Google account)

**URL:** `https://auditraks.com`

---

## Act 1: Admin Overview (2 min)

1. Show the **Admin Dashboard** — users, batches, compliance flags
2. Click **Manage Users** — show the role-based access (Supplier, Buyer, Admin)
3. Click **RMAP Smelter List** — show the certified smelter reference data
4. Point out: *"Every role sees only what they need. Access is enforced server-side."*

---

## Act 2: Supplier Creates a Batch (3 min)

1. Switch to **Supplier window**
2. Show the **Supplier Dashboard** — demo batches with compliance badges
3. Click **New Batch** — create one:
   - Batch: `W-2026-050`
   - Mineral: Tungsten
   - Origin: Rwanda
   - Mine: Nyungwe Mine
   - Weight: 500 kg
4. Click into the new batch → **Add Event** → Mine Extraction
5. Say: *"Every event is SHA-256 hashed and chained to the previous one. You can't alter history without breaking the chain."*

---

## Act 3: The Compliance Story (3 min)

1. Click on demo batch **W-2026-041** (the compliant one with 6 events)
2. Show the **Events tab** — point out the SHA-256 hashes on each event
3. Click the **Compliance tab** — show all 5 checks passing (RMAP, OECD, Sanctions, Mass Balance, Sequence)
4. Then click on **W-2026-038** (the flagged DRC batch)
5. Show the compliance failure: *"DRC origin triggers an automatic OECD risk flag. No manual review needed — the system catches it instantly."*

---

## Act 4: Buyer Generates Material Passport (3 min)

1. Switch to **Buyer window**
2. Show the **Buyer Dashboard** — donut chart, sortable table, filters
3. Click into **W-2026-041**
4. Go to **Generate & Share** tab
5. Click **Generate Material Passport**
6. Download the PDF — show the QR code, custody chain, compliance status
7. Say: *"This replaces weeks of manual certification. One click, PDF with QR code, shareable with any auditor."*

---

## Act 5: Public Verification (1 min)

1. Open a new tab — go to `https://auditraks.com/verify/W-2026-041`
2. Show the branded public verification page
3. Say: *"Anyone with the QR code or URL can verify compliance — no account needed. This is the trust layer."*

---

### Act 6: Buyer Engagement Tools (2 min)

> Switch to the **buyer browser window** (Klaus Steinberger)

1. On the Buyer Dashboard, scroll to the **Supplier Engagement** panel
2. Point out the metric cards: Total suppliers, Active, Stale, Flagged
3. Click **View suppliers** to expand the supplier list
4. Show the status badges and the **Remind** button on stale/flagged suppliers
5. Navigate to **CMRT Import** in the sidebar
6. Show the upload dropzone and explain the two-step flow: "Upload a CMRT spreadsheet, preview matched smelters against our RMAP database, then confirm to import"
7. Show the import history section

**Key message:** "Unlike competitors that only show batch compliance, auditraks gives buyers visibility into supplier engagement health and lets them proactively nudge inactive suppliers."

---

## Key Talking Points

- **Problem:** Manual spreadsheets, email chains, weeks of auditing for mineral compliance
- **Solution:** Automated, tamper-evident digital platform — mine to refinery in real time
- **Differentiators:** SHA-256 hash chains, automated RMAP + OECD checks, public QR verification
- **Market:** Full 3TG suite — tungsten, tin, tantalum, and gold — under Dodd-Frank and EU conflict minerals regulation
- **Revenue:** Per-batch tracking fees, enterprise subscriptions, API access

---

## If Asked...

**"How is this different from blockchain?"**
We use SHA-256 hash chains — same tamper-evidence guarantee, but without the cost, latency, or environmental overhead of a blockchain. Every event is cryptographically linked to the previous one.

**"What compliance frameworks do you support?"**
RMAP (Responsible Minerals Assurance Process) and OECD Due Diligence Guidance. Five automated checks: smelter verification, origin country risk, sanctions screening, mass balance, and event sequence integrity.

**"Can this scale beyond tungsten?"**
Yes — the architecture is mineral-agnostic. The pilot targets tungsten, but expanding to tin, tantalum, and gold (the full 3TG suite under Dodd-Frank and EU regulation) is a configuration change, not a rebuild.

**"How do you handle data integrity?"**
Every custody event is SHA-256 hashed at write time, with each hash including the previous event's hash. This creates an immutable chain — altering any event would break the chain and be immediately detectable.

**"Who are your target customers?"**
Upstream: mining companies, traders, processors who need to prove responsible sourcing. Downstream: manufacturers and buyers who need compliance documentation for regulators and customers.
