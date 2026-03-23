# Investor Demo Polish — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Polish the existing AccuTrac app to investor-demo quality by closing the gaps between the wireframes and the live app — focused on visual credibility, not new features.

**Architecture:** All changes are Angular frontend-only except Task 2 (API already has the data, just need to surface it). The app's functionality is complete; this plan addresses presentation gaps that matter for an investor walkthrough.

**Tech Stack:** Angular 21, Tailwind CSS (inline), existing facade/store patterns

**Wireframe reference:** `.superpowers/brainstorm/175-1774121327/` (screens 01-10)

---

## Chunk 1: Event Timeline + Compliance Credibility

These two tasks add the most investor credibility — showing SHA-256 hashes and real compliance check results makes the platform feel production-grade rather than a prototype.

### Task 1: Show SHA-256 hashes in event timeline

The wireframe (screen 04) shows each event with a `SHA-256: a3f2c8d1...7e9b` footer. The current timeline omits hashes entirely, which undermines the tamper-evidence story.

**Files:**
- Modify: `packages/web/src/app/shared/ui/event-timeline.component.ts`

- [ ] **Step 1: Update the CustodyEvent input interface to include hash**

Add `integrityHash?: string` to the component's event input type (or verify it already comes from the API response). Check the facade/store to confirm the field is passed through.

- [ ] **Step 2: Add hash display to each timeline event**

After the event date line, add a hash footer block:

```html
@if (event.integrityHash) {
  <div class="mt-2 px-3 py-2 bg-slate-50 rounded-lg border border-slate-100 text-xs text-slate-400 font-mono">
    <span class="text-slate-300">SHA-256:</span> {{ event.integrityHash | slice:0:12 }}...{{ event.integrityHash | slice:-4 }}
  </div>
}
```

- [ ] **Step 3: Verify hash data flows from API**

Check that the batch detail API response includes `integrityHash` on each custody event. If the field exists in the API response but isn't mapped in the adapter/model, add the mapping.

- [ ] **Step 4: Visual test**

Run `ng serve`, navigate to a batch with events, confirm hashes display with the truncated format.

- [ ] **Step 5: Commit**

```bash
git add packages/web/src/app/shared/ui/event-timeline.component.ts
git commit -m "feat: show SHA-256 hashes in event timeline for tamper evidence"
```

---

### Task 2: Wire up compliance check details on batch detail

The wireframe (screen 05) shows 5 named checks with pass/fail badges. The current Compliance tab shows a placeholder message. The API already has compliance check data — it just needs surfacing.

**Files:**
- Modify: `packages/web/src/app/features/supplier/batch-detail.component.ts` (Compliance tab)
- Modify: `packages/web/src/app/features/buyer/ui/compliance-summary.component.ts`
- Possibly modify: supplier facade/store to load compliance data

- [ ] **Step 1: Check what the compliance API returns**

Read `packages/api/src/Tungsten.Api/Features/Compliance/` to understand the response shape — check names, results, messages.

- [ ] **Step 2: Ensure supplier facade exposes compliance data**

Verify the supplier batch detail facade/store fetches and exposes compliance checks. If not, add a `compliance` signal that calls the compliance endpoint for the current batch.

- [ ] **Step 3: Replace the placeholder compliance tab with real data**

In `batch-detail.component.ts`, replace the "Detailed compliance checks are performed automatically..." placeholder with the compliance-summary component or inline check list matching the wireframe:

```html
<!-- Overall status card -->
<div class="flex items-center justify-between p-5 bg-slate-50 rounded-xl mb-4">
  <div class="flex items-center gap-3">
    <app-status-badge [status]="batch()?.complianceStatus ?? 'PENDING'" />
    <span class="text-sm font-semibold text-slate-900">Overall Compliance Status</span>
  </div>
</div>

<!-- Individual checks -->
@for (check of facade.compliance(); track check.id) {
  <div class="flex items-center justify-between py-4 px-5 border-b border-slate-100 last:border-0">
    <div class="flex items-center gap-3">
      <div class="w-9 h-9 rounded-lg flex items-center justify-center"
           [class]="check.result === 'PASS' ? 'bg-emerald-50' : 'bg-amber-50'">
        <!-- checkmark or warning icon -->
      </div>
      <div>
        <div class="text-sm font-semibold text-slate-900">{{ check.checkType }}</div>
        <div class="text-xs text-slate-500 mt-0.5">{{ check.details }}</div>
      </div>
    </div>
    <app-status-badge [status]="check.result" />
  </div>
}
```

- [ ] **Step 4: Visual test**

Navigate to a batch with compliance checks. Confirm individual checks display with pass/fail badges.

- [ ] **Step 5: Commit**

```bash
git add packages/web/src/app/features/supplier/batch-detail.component.ts
git commit -m "feat: show real compliance check results in batch detail"
```

---

## Chunk 2: Public Verification + Material Passport QR

These connect the "public trust layer" story — the QR code on the Material Passport links to a branded public verification page.

### Task 3: Add a branded public batch verification page

The wireframe (screen 09) shows a polished, mobile-friendly verification page at `/verify/:batchId`. The current `shared-document.component.ts` handles document sharing but not batch verification. The API endpoint `GET /api/verify/{batchId}` already exists.

**Files:**
- Create: `packages/web/src/app/features/public/verify-batch.component.ts`
- Modify: `packages/web/src/app/app.routes.ts` (add route)

- [ ] **Step 1: Create the verify-batch component**

Standalone component with no auth required. On init, read `batchId` from route params and call `GET /api/verify/{batchId}`. Display:
- AccuTrac logo + "Supply Chain Verification" header
- Green/amber/red status banner based on compliance result
- Batch info grid: batch number, mineral, origin, last updated
- Compliance frameworks evaluated (badges)
- Hash chain integrity status
- Footer: "Verified by AccuTrac"

Match the wireframe styling — centered card on slate-50 background, gradient status header.

- [ ] **Step 2: Add the route**

In `app.routes.ts`, add an unauthenticated route:

```typescript
{ path: 'verify/:batchId', loadComponent: () => import('./features/public/verify-batch.component').then(m => m.VerifyBatchComponent) }
```

Ensure this route does NOT use the shell layout (no sidebar/topbar).

- [ ] **Step 3: Visual test**

Navigate to `/verify/W-2026-041` (or any known batch ID). Confirm the page renders without requiring login.

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/features/public/verify-batch.component.ts packages/web/src/app/app.routes.ts
git commit -m "feat: add branded public batch verification page"
```

---

### Task 4: Add QR code to Material Passport generation

The wireframe (screen 07) shows a QR code on the Material Passport PDF. The QR links to the public verification page.

**Files:**
- Check: `packages/api/src/Tungsten.Api/Features/DocumentGeneration/` — the QuestPDF Material Passport generator
- Modify: the Material Passport PDF template to include a QR code

- [ ] **Step 1: Check current Material Passport PDF generator**

Read the DocumentGeneration feature to understand the current PDF template structure.

- [ ] **Step 2: Add QR code to the PDF**

QuestPDF supports QR codes via `Image()` with a generated QR bitmap, or via a library like `QRCoder`. Add a QR code in the top-right of the passport header that encodes the URL `https://accutrac.org/verify/{batchId}`.

If QRCoder is not already a dependency:
```bash
dotnet add packages/api/src/Tungsten.Api/Tungsten.Api.csproj package QRCoder
```

Generate the QR as a PNG byte array and place it in the PDF header.

- [ ] **Step 3: Build and verify**

```bash
cd packages/api && dotnet build
```

- [ ] **Step 4: Commit**

```bash
git add packages/api/
git commit -m "feat: add QR code linking to public verification on Material Passport"
```

---

## Chunk 3: Dashboard Polish

### Task 5: Add search/filter to Supplier Dashboard

The wireframe (screen 02) shows batch cards in a grid. The current dashboard has no search or filter capability — for an investor demo with sample data, filtering by compliance status is a quick win.

**Files:**
- Modify: `packages/web/src/app/features/supplier/supplier-dashboard.component.ts`

- [ ] **Step 1: Add filter state signals**

```typescript
searchQuery = signal('');
statusFilter = signal<string>('ALL');

filteredBatches = computed(() => {
  let batches = this.facade.batches();
  const q = this.searchQuery().toLowerCase();
  const status = this.statusFilter();
  if (q) batches = batches.filter(b => b.batchNumber.toLowerCase().includes(q) || b.originCountry.toLowerCase().includes(q));
  if (status !== 'ALL') batches = batches.filter(b => b.complianceStatus === status);
  return batches;
});
```

- [ ] **Step 2: Add search bar and filter dropdown above the batch grid**

```html
<div class="flex items-center gap-3 mb-6">
  <div class="flex-1 relative">
    <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400">...</svg>
    <input type="text" placeholder="Search batches..."
           class="w-full pl-10 pr-4 py-2.5 border border-slate-300 rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500"
           [value]="searchQuery()" (input)="searchQuery.set($any($event.target).value)" />
  </div>
  <select class="px-4 py-2.5 border border-slate-300 rounded-xl text-sm"
          [value]="statusFilter()" (change)="statusFilter.set($any($event.target).value)">
    <option value="ALL">All Statuses</option>
    <option value="COMPLIANT">Compliant</option>
    <option value="FLAGGED">Flagged</option>
    <option value="PENDING">Pending</option>
  </select>
</div>
```

- [ ] **Step 3: Update the `@for` loop to use `filteredBatches()`**

Replace the existing batch iteration with `filteredBatches()`.

- [ ] **Step 4: Visual test**

Type in search bar, select filter dropdown, confirm batch grid updates.

- [ ] **Step 5: Commit**

```bash
git add packages/web/src/app/features/supplier/supplier-dashboard.component.ts
git commit -m "feat: add search and compliance filter to supplier dashboard"
```

---

### Task 6: Add batch progress indicator to batch cards

The wireframe shows each batch card with event count. For investor demo, showing progress through the custody chain (e.g., "4/6 steps") adds depth.

**Files:**
- Modify: `packages/web/src/app/features/supplier/ui/batch-card.component.ts`

- [ ] **Step 1: Add a progress indicator to the card footer**

After the existing weight + event count, add a small progress bar:

```html
<div class="w-16 h-1.5 bg-slate-100 rounded-full overflow-hidden ml-auto">
  <div class="h-full bg-indigo-500 rounded-full" [style.width.%]="(batch().eventCount / 6) * 100"></div>
</div>
```

This shows a visual sense of how far through the 6-step supply chain the batch has progressed.

- [ ] **Step 2: Visual test**

Confirm progress bars render correctly on batch cards.

- [ ] **Step 3: Commit**

```bash
git add packages/web/src/app/features/supplier/ui/batch-card.component.ts
git commit -m "feat: add custody progress bar to supplier batch cards"
```

---

## Chunk 4: Buyer + Shared Document Polish

### Task 7: Improve the shared document / public link page

The wireframe (screen 09) shows a branded verification page. The current shared-document page is functional but minimal. Add AccuTrac branding and compliance context.

**Files:**
- Modify: `packages/web/src/app/features/shared/shared-document.component.ts`

- [ ] **Step 1: Add AccuTrac branding header**

Add the logo + "AccuTrac" wordmark at the top, matching the public verification page style.

- [ ] **Step 2: Add compliance status context**

If the shared document is a Material Passport, show the compliance status badge and batch summary below the document metadata.

- [ ] **Step 3: Visual test**

Access a shared document link. Confirm branding and improved layout.

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/features/shared/shared-document.component.ts
git commit -m "feat: add AccuTrac branding to shared document page"
```

---

### Task 8: Add column sorting to buyer batch table

The wireframe (screen 06) implies a sortable table. Current table has no sorting.

**Files:**
- Modify: `packages/web/src/app/features/buyer/ui/batch-table.component.ts`

- [ ] **Step 1: Add sort state and logic**

```typescript
sortColumn = signal<string>('batchNumber');
sortDirection = signal<'asc' | 'desc'>('asc');

sortedBatches = computed(() => {
  const batches = [...this.filteredBatches()];
  const col = this.sortColumn();
  const dir = this.sortDirection();
  return batches.sort((a, b) => {
    const aVal = (a as any)[col] ?? '';
    const bVal = (b as any)[col] ?? '';
    const cmp = aVal < bVal ? -1 : aVal > bVal ? 1 : 0;
    return dir === 'asc' ? cmp : -cmp;
  });
});

toggleSort(column: string) {
  if (this.sortColumn() === column) {
    this.sortDirection.update(d => d === 'asc' ? 'desc' : 'asc');
  } else {
    this.sortColumn.set(column);
    this.sortDirection.set('asc');
  }
}
```

- [ ] **Step 2: Make table headers clickable**

Add `(click)="toggleSort('batchNumber')"` to each `<th>` and show a sort direction indicator arrow.

- [ ] **Step 3: Visual test**

Click column headers, confirm sort order changes.

- [ ] **Step 4: Commit**

```bash
git add packages/web/src/app/features/buyer/ui/batch-table.component.ts
git commit -m "feat: add column sorting to buyer batch table"
```

---

## Chunk 5: Demo Data + Final Polish

### Task 9: Seed realistic demo data for investor walkthrough

For an investor demo, the app needs pre-loaded sample data showing a complete mine-to-refinery journey.

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Infrastructure/Persistence/SeedData.cs`

- [ ] **Step 1: Add sample batches to seed data**

Add 3-4 batches with realistic data:
- `W-2026-041`: Rwanda, Nyungwe Mine, 450kg, COMPLIANT, 6 events (full chain)
- `W-2026-038`: DRC, Bisie Mine, 780kg, FLAGGED (high-risk origin), 4 events
- `W-2026-045`: Bolivia, Huanuni Mine, 220kg, PENDING, 0 events
- `W-2026-035`: Rwanda, Rutongo Mine, 320kg, COMPLIANT, 5 events

- [ ] **Step 2: Add sample custody events for W-2026-041**

Seed the full 6-event chain: Mine Extraction → Lab Assay → Concentration → Trading → Smelting (RMAP verified) → Export. Include realistic GPS coordinates, actor names, and SHA-256 hashes.

- [ ] **Step 3: Add sample compliance checks**

Seed compliance check records for the demo batches showing PASS results for compliant batches and FAIL for the DRC batch (origin country risk).

- [ ] **Step 4: Build and verify**

```bash
cd packages/api && dotnet build
```

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Infrastructure/Persistence/SeedData.cs
git commit -m "feat: add realistic demo data for investor walkthrough"
```

---

### Task 10: Add version number to sidebar footer

Small but important — investors often ask "what version is this?" during demos.

**Files:**
- Modify: `packages/web/src/app/core/layout/sidebar.component.ts`

- [ ] **Step 1: Add version display below user info**

```html
<div class="px-5 py-2 border-t border-slate-700/50">
  <div class="text-[10px] text-slate-600">AccuTrac v2.0 — Pilot MVP</div>
</div>
```

- [ ] **Step 2: Commit**

```bash
git add packages/web/src/app/core/layout/sidebar.component.ts
git commit -m "feat: show version number in sidebar footer"
```

---

## Execution Order

Tasks are ordered by investor-demo impact:

| Priority | Task | Impact | Effort |
|----------|------|--------|--------|
| 1 | Task 9: Demo seed data | Can't demo without data | Medium |
| 2 | Task 1: SHA-256 in timeline | Core value prop credibility | Small |
| 3 | Task 2: Compliance check detail | Shows automation depth | Medium |
| 4 | Task 3: Public verification page | Completes the trust story | Medium |
| 5 | Task 4: QR code on Material Passport | Ties PDF to public page | Small |
| 6 | Task 5: Supplier search/filter | Makes dashboard feel real | Small |
| 7 | Task 6: Batch progress indicator | Visual polish | Small |
| 8 | Task 8: Buyer table sorting | Professional table UX | Small |
| 9 | Task 7: Shared document branding | Polish | Small |
| 10 | Task 10: Version in sidebar | Quick win | Tiny |
