# PROMPT — Auditraks ROI Implementation Plan (Top 8 Items)

## Context
 use the existing code base settings


A combined gap analysis and competitive ROI scoring exercise has produced a ranked list of 10 priority items. Your task covers the **top 8 items by ROI score**, listed below in rank order.

---

## Items to Plan

### Item 1 — Compliance rule engine authoring spec (ROI: 9.0)
**Gap:** Nine regulatory frameworks are defined as outputs — traffic-light statuses, check descriptions — but the spec never defines how rules are expressed, stored, or updated. Whether it is a database record, a DSL, a configuration file, or a compiled class has major downstream consequences. The Compliance Lead must be able to update OECD risk scoring without a code deployment.
**Why it matters:** Without this spec, every compliance feature built in weeks 7–16 is at rework risk. It is the highest-leverage item in the project — one document prevents months of rebuilding.

### Item 2 — Pricing schedule + commercial framework (ROI: 9.0)
**Gap:** No signed pricing schedule or commercial framework exists. The three subscription tiers (Starter, Professional, Enterprise) are defined in the spec but have no absolute pricing. Enterprise buyers cannot be engaged and no sales conversations can be closed without it.
**Why it matters:** Form SD filing season (February–April) is the highest-priority outreach window. Every warm lead from RMI and ITIA engagement stalls immediately without a pricing answer. No build required — this is a document and a decision.

### Item 3 — Live smelter reference database (RMAP integration) (ROI: 8.5)
**Gap:** The platform checks RMAP conformance status based on supplier declarations but has no native smelter reference database to cross-check against. RMAP smelter data is publicly available. The smelter-origin coherence check — validating that a declared smelter's sourcing countries are consistent with the upstream mine origin of the batch — is currently absent from the compliance engine.
**Why it matters:** Closes two gaps simultaneously: the internal compliance engine coherence check and the competitive feature gap versus IPOINT and Source Intelligence, both of which maintain live smelter libraries. Transforms the RMAP check from a declaration check to a verified cross-reference — a material difference in audit defensibility.

### Item 4 — Cross-event country consistency rules (ROI: 8.5)
**Gap:** The compliance engine checks individual events but has no rules that validate consistency across events in a chain. A batch can currently pass all individual event checks while having a materially impossible country-of-origin journey (e.g. a batch mined in a sanctioned origin arriving at a smelter whose sourcing countries do not include that origin). No new infrastructure is required — this is pure logic on data already captured.
**Why it matters:** Closes the single biggest hole in compliance defensibility. Identified as the highest-value compliance engine gap across two separate reviews.

### Item 5 — Digital Product Passport (DPP) schema alignment (ROI: 8.0)
**Gap:** The platform produces a Material Passport but it is not aligned to the EU Digital Product Passport schema. Competitors IPOINT, Minespider, and Circulor offer DPP-compliant outputs. The underlying custody data is already captured — the gap is schema mapping and output formatting, not a new data model.
**Why it matters:** Opens the European commercial market and removes the most frequent ESG buyer procurement objection. EU DPP mandates are expanding to materials categories that include tungsten supply chain actors.

### Item 6 — Supplier value proposition formalisation (ROI: 7.5)
**Gap:** The spec defines what buyers receive from the platform but no formal supplier value proposition exists. Suppliers currently participate by contractual obligation, not by benefit. The Material Passport (free, shareable with the supplier's own customers as evidence of responsible sourcing) is the primary value lever — but it is not yet formalised or embedded in the onboarding journey.
**Why it matters:** Supplier disengagement breaks chain completeness. Chain completeness is the entire value of the platform to buyers. This is low-cost product and commercial work with a direct retention effect.

### Item 7 — SOC 2 Type I — initiate (ROI: 7.5)
**Gap:** No SOC 2 certification is in progress. Enterprise procurement at mid-to-large manufacturers includes a security questionnaire gate. SOC 2 Type I requires a point-in-time audit of controls; Type II requires a minimum 6-month observation window. Neither has been started.
**Why it matters:** Every large commercial buyer will stall at the security review without SOC 2 on record. Starting the observation window now is the only path to having Type II available when the first enterprise contract conversation reaches procurement stage.

### Item 8 — EU Conflict Minerals Regulation (EU CMR) framework (ROI: 7.0)
**Gap:** The platform covers RMAP and OECD DDG. The EU CMR (in force since 2021) is not in scope. OECD DDG coverage is approximately 80% of the way there; EU CMR alignment is largely additive. Without it the platform is not viable for EU-based buyers or US buyers supplying into EU customers.
**Why it matters:** Adding EU CMR roughly doubles total addressable market. It is becoming the international baseline expectation for tungsten buyers regardless of geography, and all five primary competitors (IPOINT, Source Intelligence, Minespider, Circulor, TrusTrace) either cover it or are building toward it.

---

## Your Task

For **each of the eight items above**, produce a structured implementation plan containing the following sections:

### 1. Objective
One or two sentences: what "done" looks like for this item.

### 2. Owner and dependencies
Who owns this item (CTO, Compliance Lead, external vendor, etc.) and what must be true before this item can start.

### 3. Work breakdown
A numbered list of concrete, actionable tasks. For build items, specify which layer of the stack is affected (Angular module, .NET service, PostgreSQL schema, external API). For non-build items (commercial, certification, documentation), specify the exact deliverable for each task.

### 4. Acceptance criteria
How you will know this item is complete. Each criterion must be testable or verifiable by a named role.

### 5. Estimated timeline
A realistic timeline in weeks from start date. Flag any external dependency that could extend the timeline (vendor lead times, regulatory timelines, third-party data availability).

### 6. Risks and mitigations
The two or three most likely failure modes for this item and a specific mitigation for each.

### 7. Competitive impact
One paragraph. Reference specific competitors where relevant (IPOINT, Source Intelligence, Minespider, Circulor, TrusTrace). Be specific about what the capability change means for a buyer's procurement decision. Do not use the words "differentiation" or "unique."

---

## Constraints and principles

- Do not recommend Azure-specific services. All implementation must be Render-compatible.
- Do not recommend Auth0. Auth is Microsoft Entra External ID.
- Do not recommend SendGrid. Email is Resend, abstracted behind `INotificationService`.
- Compliance scope for the Render/commercial platform is RMAP and OECD DDG only — do not expand DFARS, CMMC, or ITAR scope into any plan for this platform.
- All new backend interfaces must follow the existing `I{Entity}Repository` / `I{Service}` abstraction pattern so that future migration to Azure is a config/SDK swap, not a rewrite.
- Any new database work must preserve row-level security (RLS) at the PostgreSQL layer — not just application-layer enforcement.
- All timelines must assume a solo technical lead (Julian) with no additional engineering resource unless you explicitly note where an external resource is required and justify why.
- Items 1 and 2 are document/decision work. Do not treat them as build tasks. Produce concrete outlines of the documents themselves as part of the work breakdown.
- Be direct. Do not pad with caveats. If something is a hard dependency, state it as such.

---

## Output format

Produce the eight plans sequentially in rank order (items 1 through 8). Use consistent heading structure throughout.

After all eight plans, produce:

**A. Consolidated timeline view** — all eight items on a shared week-by-week schedule. Identify which items can run in parallel, which are sequentially constrained, and which have hard external lead times that make them schedule-critical regardless of effort.

**B. Top 3 cross-cutting risks** — the three risks that, if they materialise, would have the highest combined impact across the most items simultaneously. For each, state the trigger event, the items affected, and the recovery path.

**C. Quick wins summary** — a bulleted list of any tasks across the eight plans that can be completed within 5 working days and require no external dependencies. These are the actions to take this week.
