# Auditraks ROI Implementation Plan — Top 8 Items

---

## Item 1 — Compliance Rule Engine Authoring Spec (ROI: 9.0)

### 1. Objective
A published technical specification that defines how compliance rules are expressed, stored, versioned, and updated — enabling the Compliance Lead to modify OECD risk scoring, RMAP thresholds, and sanctions lists without code deployments.

### 2. Owner and dependencies
**Owner:** CTO (Julian)
**Dependencies:** None. This is document work that must precede all compliance engine build tasks (Items 3, 4, 5, 8).

### 3. Work breakdown
1. **Audit current rule implementations** — Document the five existing check implementations (RMAP smelter verification, OECD origin risk, sanctions screening, mass balance, sequence integrity). Record every hardcoded threshold, country list, and decision branch.
2. **Define rule expression format** — Specify a JSON-based rule schema stored in PostgreSQL. Each rule record contains: `framework`, `rule_version`, `effective_date`, `rule_definition` (JSONB), `status` (DRAFT/ACTIVE/RETIRED). No DSL, no compiled classes — pure data-driven configuration.
3. **Define rule schema for each check type:**
   - RMAP: `{ conformanceStatuses: ["CONFORMANT", "ACTIVE_PARTICIPATING"], failStatuses: ["NON_CONFORMANT"] }`
   - OECD DDG: `{ highRiskCountries: ["CD","RW",...], mediumRiskCountries: [...], requiredDocsByEventType: {...} }`
   - Sanctions: `{ sources: ["UN Security Council", "EU Sanctions List"], matchThreshold: 0.85 }`
   - Mass Balance: `{ maxLossPercent: 5.0, maxGainPercent: 0.0 }`
   - Sequence: `{ requireChronologicalOrder: true, requireHashChain: true }`
4. **Define versioning and activation model** — Rules are versioned. Only one version per framework is ACTIVE at a time. Activating a new version retires the previous. All compliance checks record the `rule_version` they used.
5. **Define admin UI requirements** — Admin dashboard gets a "Compliance Rules" section: view active rules per framework, edit JSON configuration, save as DRAFT, activate (with confirmation), view version history.
6. **Write the spec document** — `docs/specs/compliance-rule-engine-spec.md`. Include: data model, API endpoints, admin UI wireframes (text-based), migration path from hardcoded rules, rollback procedure.
7. **Review with stakeholder** — Julian reviews and signs off.

### 4. Acceptance criteria
- [ ] Spec document exists at `docs/specs/compliance-rule-engine-spec.md`
- [ ] Every existing hardcoded rule is represented in the proposed schema
- [ ] The spec defines how a Compliance Lead updates OECD risk country list without code deployment
- [ ] The spec defines rule versioning and rollback
- [ ] Julian has reviewed and approved

### 5. Estimated timeline
**2 weeks.** No external dependencies.
- Week 1: Audit current rules, define schema, write draft
- Week 2: Review, revise, finalise

### 6. Risks and mitigations
| Risk | Mitigation |
|------|-----------|
| Over-engineering the rule format into a full DSL | Constrain to typed JSON schemas per framework. No expression language. |
| Spec not actionable enough for implementation | Include exact PostgreSQL table DDL and sample JSON for every check type |
| Delay cascades to Items 3, 4, 5, 8 | Timebox to 2 weeks. The five current checks work — this spec improves them, doesn't gate their operation |

### 7. Competitive impact
IPOINT and Source Intelligence both offer configurable compliance rule sets that customers can tune without vendor involvement. Auditraks currently hardcodes every rule, which means every customer-specific threshold change requires a code deployment. Publishing this spec and building against it puts Auditraks on parity with the compliance configuration capability that enterprise buyers expect, and removes the most common objection from compliance teams evaluating the platform: "can we adjust the rules to match our internal risk appetite?"

---

## Item 2 — Pricing Schedule + Commercial Framework (ROI: 9.0)

### 1. Objective
A published pricing schedule with absolute numbers for Starter, Professional, and Enterprise tiers, plus a commercial terms document that sales conversations can reference immediately.

### 2. Owner and dependencies
**Owner:** Julian (CEO/CTO)
**Dependencies:** None. This is a business decision, not a build task.

### 3. Work breakdown
1. **Competitive pricing research** — Document pricing from IPOINT (enterprise-only, undisclosed), Source Intelligence ($15k–$50k/yr), Minespider (per-trace pricing), Circulor (enterprise), TrusTrace (per-supplier). Source from public G2/Capterra data and RFP responses.
2. **Define tier pricing:**
   - **Starter** ($99/mo): Up to 50 batches, 5 users, RMAP + OECD DDG checks, Material Passport generation
   - **Pro** ($249/mo): Unlimited batches, unlimited users, API access, webhooks, priority support
   - **Enterprise** (custom, starting $999/mo): SSO, dedicated support, custom compliance rules, SLA, audit dossier generation
3. **Draft commercial terms document** — `docs/commercial/pricing-schedule-v1.md`. Include: tier comparison table, billing terms (annual discount: 2 months free), trial terms (60-day, no credit card), overage handling, cancellation policy.
4. **Draft enterprise engagement template** — `docs/commercial/enterprise-engagement-template.md`. One-page document for enterprise sales conversations: pricing, onboarding timeline, support SLA, security posture summary, procurement FAQ.
5. **Update Stripe configuration** — Verify Stripe products/prices match the published schedule. Currently has Starter and Pro configured.
6. **Update landing page pricing section** — Ensure https://auditraks.com pricing matches the published schedule.
7. **Finalise and publish** — PDF version for email attachment. Landing page live.

### 4. Acceptance criteria
- [ ] Pricing schedule document exists with absolute numbers
- [ ] Enterprise engagement template exists
- [ ] Stripe products match published pricing
- [ ] Landing page pricing section matches
- [ ] Julian can send pricing to a prospect within 24 hours of request

### 5. Estimated timeline
**1 week.** No external dependencies.

### 6. Risks and mitigations
| Risk | Mitigation |
|------|-----------|
| Pricing too high for SME market | Starter at $99/mo is below all named competitors. Validate against 3 prospect conversations before finalising. |
| Enterprise pricing undefined | Start with "custom, starting $999/mo" and refine after first 3 enterprise conversations |
| Currency/tax complexity (UK entity selling USD) | Stripe handles currency. Note in terms that all prices are USD exclusive of tax. |

### 7. Competitive impact
Source Intelligence and IPOINT do not publish pricing, forcing every prospect into a sales cycle. Auditraks publishing transparent pricing with a self-service trial removes the single largest friction point in the SME buying process. For the Form SD filing season (February–April), every warm lead from RMI/ITIA engagement can convert to a trial within minutes rather than stalling on "send me pricing."

---

## Item 3 — Live Smelter Reference Database (RMAP Integration) (ROI: 8.5)

### 1. Objective
A live, queryable smelter reference database cross-checked against RMAP public data, with a smelter-origin coherence check that validates declared smelter sourcing countries against batch origin.

### 2. Owner and dependencies
**Owner:** Julian (CTO)
**Dependencies:** Item 1 (spec) is recommended but not blocking — the current RMAP check already works, this enhances it.

### 3. Work breakdown
1. **Expand RmapSmelterEntity schema** — Add columns: `sourcing_countries` (text[], nullable), `mineral_type` (text), `facility_location` (text), `last_sync_at` (timestamp). Migration: `ALTER TABLE rmap_smelters`.
   - **Layer:** PostgreSQL schema, EF Core entity + configuration
2. **Build RMAP data import endpoint** — Admin-only endpoint `POST /api/admin/rmap/import` that accepts a CSV/Excel upload of the RMAP Conformant Smelter List (publicly available from RMI). Parses and upserts smelter records.
   - **Layer:** .NET Minimal API endpoint, CSV parser
3. **Build smelter search API** — `GET /api/smelters?q={query}&mineral={type}` — searchable by name, ID, country, mineral type. Used by the frontend when suppliers select a smelter during PRIMARY_PROCESSING events.
   - **Layer:** .NET Minimal API, EF Core query
4. **Build smelter-origin coherence check** — New compliance check: when a batch reaches PRIMARY_PROCESSING with a declared smelterId, verify that the smelter's `sourcing_countries` includes the batch's `originCountry`. Result: PASS (match), FLAG (smelter sourcing data unavailable), FAIL (origin country not in smelter sourcing list).
   - **Layer:** .NET compliance checker class
5. **Add smelter autocomplete to frontend** — In the SubmitEventComponent, when event type is PRIMARY_PROCESSING, the smelterId field becomes an autocomplete that queries the smelter search API.
   - **Layer:** Angular shared component
6. **Admin RMAP management page** — Already exists (`/admin/rmap`). Enhance to show last sync date, total smelter count, and a "Upload RMAP List" button.
   - **Layer:** Angular admin feature
7. **Seed production data** — Import the current RMAP Conformant Smelter & Refiner List (publicly available, ~400 smelters across 4 minerals).
8. **Tests** — Unit tests for coherence check logic, integration test for import endpoint.

### 4. Acceptance criteria
- [ ] RMAP smelter list can be imported via admin UI
- [ ] Smelter search returns results filtered by mineral type
- [ ] Smelter-origin coherence check produces PASS/FLAG/FAIL
- [ ] Batch W-2026-041 (Rwanda → CID001100 Austria) produces PASS (if smelter sourcing includes RW)
- [ ] Batch with mismatched origin produces FAIL
- [ ] Admin RMAP page shows smelter count and last sync date

### 5. Estimated timeline
**3 weeks.**
- Week 1: Schema migration, import endpoint, smelter search API
- Week 2: Coherence check, frontend autocomplete
- Week 3: Admin UI, seed data, testing

**External dependency:** RMAP Conformant Smelter List is publicly available from RMI (responsiblemineralsinitiative.org). No vendor relationship required.

### 6. Risks and mitigations
| Risk | Mitigation |
|------|-----------|
| RMAP data format changes | CSV parser should handle column variations. Store raw upload for debugging. |
| Smelter sourcing country data incomplete | FLAG (not FAIL) when sourcing data unavailable. Gradually enrich through admin edits. |
| Data staleness | Show "last synced" date prominently. Admin can re-upload quarterly. |

### 7. Competitive impact
IPOINT and Source Intelligence both maintain live smelter libraries with cross-reference validation. Auditraks currently performs a declaration-only check — the supplier says "I used CID001100" and the system verifies that CID001100 is conformant, but does not check whether CID001100 actually sources from the declared origin country. Adding the coherence check transforms the RMAP verification from a declaration check to a cross-referenced validation, which is the standard that compliance auditors expect and that procurement teams at Materion, Kennametal, and similar buyers will evaluate against.

---

## Item 4 — Cross-Event Country Consistency Rules (ROI: 8.5)

### 1. Objective
Compliance rules that validate geographic consistency across the full custody event chain — detecting impossible country-of-origin journeys such as a batch mined in a sanctioned origin arriving at a smelter whose sourcing countries exclude that origin.

### 2. Owner and dependencies
**Owner:** Julian (CTO)
**Dependencies:** Item 3 (smelter sourcing data) provides the data needed for smelter-origin validation. The basic country consistency checks can proceed without Item 3.

### 3. Work breakdown
1. **Define consistency rules** — Document the specific cross-event validations:
   - Rule 1: `originCountry` on MINE_EXTRACTION must match batch `originCountry`
   - Rule 2: EXPORT_SHIPMENT `originCountry` metadata must match batch `originCountry`
   - Rule 3: EXPORT_SHIPMENT `destinationCountry` must match PRIMARY_PROCESSING country (if both exist)
   - Rule 4: No CONCENTRATION or TRADING_TRANSFER should occur in a sanctioned country unless the batch origin is also that country
   - Rule 5: Smelter sourcing countries must include batch origin (requires Item 3 data)
2. **Create CountryConsistencyChecker** — New compliance checker class that runs all rules against the full event chain for a batch. Produces one compliance check record per rule violation found.
   - **Layer:** .NET compliance checker, new file in `Features/Compliance/`
3. **Add COUNTRY_CONSISTENCY framework** — New framework constant alongside RMAP, OECD_DDG, MASS_BALANCE, SEQUENCE_CHECK.
   - **Layer:** .NET constants, frontend status badge
4. **Wire into compliance pipeline** — Run CountryConsistencyChecker after each custody event is created, same as existing checks.
   - **Layer:** .NET MediatR handler or service
5. **Update frontend compliance display** — Ensure COUNTRY_CONSISTENCY results appear in batch compliance tab with clear rule descriptions.
   - **Layer:** Angular shared compliance-summary component
6. **Tests** — Unit tests for each of the 5 rules. Integration test with demo batch data.

### 4. Acceptance criteria
- [ ] Batch with consistent country chain (RW mine → RW export → AT smelter) passes all rules
- [ ] Batch with EXPORT_SHIPMENT from a different country than origin flags Rule 2
- [ ] Batch with smelter whose sourcing excludes origin country fails Rule 5
- [ ] Compliance results show COUNTRY_CONSISTENCY framework in the UI
- [ ] Existing demo batches produce expected results

### 5. Estimated timeline
**2 weeks.**
- Week 1: Define rules, implement checker, wire into pipeline
- Week 2: Frontend display, testing, demo data validation

### 6. Risks and mitigations
| Risk | Mitigation |
|------|-----------|
| False positives on legitimate transit routes | Rules check origin/destination, not transit. If false positives emerge, add a "transit country whitelist" configuration. |
| Missing event metadata | FLAG (not FAIL) when country data is absent from an event. Only FAIL on confirmed mismatch. |
| Rule 5 depends on Item 3 smelter data | Rule 5 deferred if Item 3 not complete. Rules 1–4 work independently. |

### 7. Competitive impact
This closes the single biggest compliance defensibility gap identified across two separate reviews. No competitor in the tungsten compliance space — including IPOINT and Source Intelligence — publicly advertises cross-event country consistency validation. Most perform per-event checks. An auditor reviewing an Auditraks-managed batch will see that the platform flagged a geographically impossible supply chain journey, which is exactly the kind of systemic risk detection that OECD DDG Annex II Step 2 requires and that auditors look for when assessing a company's due diligence program.

---

## Item 5 — Digital Product Passport (DPP) Schema Alignment (ROI: 8.0)

### 1. Objective
Material Passport output aligned to the EU Digital Product Passport schema, exportable as a standards-compliant JSON-LD document alongside the existing PDF.

### 2. Owner and dependencies
**Owner:** Julian (CTO)
**Dependencies:** None — the underlying custody data is already captured. This is schema mapping and output formatting.

### 3. Work breakdown
1. **Research EU DPP schema** — Review the EU Battery Regulation DPP schema (most mature) and the ESPR (Ecodesign for Sustainable Products Regulation) draft schema. Identify the subset applicable to raw mineral materials. Document the mapping in `docs/specs/dpp-schema-mapping.md`.
2. **Define Auditraks DPP output schema** — JSON-LD document containing:
   - Product identification (batch number, mineral type, weight)
   - Origin and provenance (country, mine, GPS coordinates)
   - Custody chain summary (event types, dates, actors)
   - Compliance status (framework results)
   - Carbon/sustainability data (placeholder for future)
   - Unique identifiers (QR code URL, batch ID)
3. **Build DPP generation endpoint** — `POST /api/batches/{id}/generate/dpp` — generates the JSON-LD document and stores it as a GeneratedDocument.
   - **Layer:** .NET Minimal API, JSON-LD serialisation
4. **Update Material Passport PDF** — Add a "DPP Reference" section to the existing QuestPDF-generated passport with a link/QR to the machine-readable DPP.
   - **Layer:** .NET QuestPDF template
5. **Build DPP viewer** — Public endpoint `GET /api/public/dpp/{token}` that renders the DPP JSON-LD in a human-readable format. Accessible via QR code without login.
   - **Layer:** .NET Minimal API, Angular public component
6. **Frontend: Add DPP generation button** — In buyer batch detail "Generate & Share" tab, add "Digital Product Passport" alongside existing "Material Passport" and "Audit Dossier".
   - **Layer:** Angular buyer feature
7. **Tests** — Validate JSON-LD structure against schema, test generation endpoint.

### 4. Acceptance criteria
- [ ] `POST /api/batches/{id}/generate/dpp` returns a valid JSON-LD document
- [ ] DPP contains all required fields from the mapping document
- [ ] QR code on Material Passport PDF links to the DPP viewer
- [ ] DPP is publicly accessible via share token without login
- [ ] Buyer can generate DPP from the batch detail page

### 5. Estimated timeline
**3 weeks.**
- Week 1: Schema research and mapping document
- Week 2: DPP generation endpoint, JSON-LD output
- Week 3: PDF update, frontend, public viewer, testing

**External dependency:** EU DPP schema is still evolving. Use the Battery Regulation schema as the baseline and flag fields that may change.

### 6. Risks and mitigations
| Risk | Mitigation |
|------|-----------|
| EU DPP schema not finalised for raw materials | Use Battery Regulation schema (most mature) as baseline. Mark "draft alignment" in output metadata. |
| JSON-LD complexity | Use a minimal context definition. Don't attempt full ontology — map to Schema.org where possible. |
| Scope creep into full lifecycle assessment | Exclude carbon/sustainability data from v1. Include placeholder fields in schema. |

### 7. Competitive impact
IPOINT, Minespider, and Circulor all advertise DPP-compliant outputs. Auditraks currently produces a proprietary Material Passport that, while useful, does not satisfy the EU buyer who needs a machine-readable passport for their own compliance obligations under ESPR. Adding DPP output opens the European commercial market and removes the most frequent ESG procurement objection. For US buyers supplying into EU customers (Kennametal's European operations, for example), DPP capability moves Auditraks from "nice to have" to "procurement requirement."

---

## Item 6 — Supplier Value Proposition Formalisation (ROI: 7.5)

### 1. Objective
A published supplier value proposition embedded in the onboarding flow, making the Material Passport the primary benefit suppliers receive for participation — not just contractual obligation.

### 2. Owner and dependencies
**Owner:** Julian (CEO/CTO)
**Dependencies:** None. This is product and commercial work.

### 3. Work breakdown
1. **Draft supplier value proposition document** — `docs/commercial/supplier-value-proposition.md`. Core message: "Your participation generates a Material Passport — a verified, shareable proof of responsible sourcing that you can use with your own customers, banks, and ESG auditors."
2. **Define supplier benefits list:**
   - Free Material Passport generation for every completed batch
   - Shareable verification link (public, no recipient login needed)
   - Compliance status dashboard showing their track record
   - Reduction in buyer audit requests (buyers verify via platform instead of requesting documents)
   - Exportable compliance summary for annual reports
3. **Update onboarding wizard** — Add a "Why participate?" step (Step 1) to the supplier onboarding that explains benefits before asking for data entry. Include a 30-second value summary.
   - **Layer:** Angular supplier feature
4. **Create supplier welcome email template** — When a supplier user is invited, the email explains what they get, not just what they must do. Update the Resend email template.
   - **Layer:** .NET email service, HTML template
5. **Add "Your Passports" section to supplier dashboard** — Show generated Material Passports with share links, making the value tangible on every login.
   - **Layer:** Angular supplier dashboard
6. **Create supplier one-pager PDF** — One-page document for buyers to share with their suppliers when onboarding them to the platform. Explains benefits from the supplier's perspective.

### 4. Acceptance criteria
- [ ] Supplier value proposition document exists
- [ ] Supplier welcome email explains benefits, not just obligations
- [ ] Supplier dashboard shows generated passports with share links
- [ ] One-pager PDF exists for buyer-to-supplier distribution
- [ ] Onboarding wizard includes a benefits step

### 5. Estimated timeline
**2 weeks.**
- Week 1: Documents (value prop, one-pager, email template)
- Week 2: Frontend changes (onboarding wizard, dashboard passports section)

### 6. Risks and mitigations
| Risk | Mitigation |
|------|-----------|
| Suppliers don't read onboarding materials | Keep the benefits step to 3 bullet points and a 15-second read. Don't bury it. |
| Material Passport value unclear to artisanal miners | The one-pager targets the mining cooperative manager, not the individual miner. Use language appropriate to that audience. |

### 7. Competitive impact
Minespider and Circulor both market supplier participation as beneficial rather than burdensome. Source Intelligence's supplier portal is widely criticised for being "just another questionnaire." Auditraks positioning the Material Passport as supplier-owned intellectual property — proof of responsible sourcing that the supplier can use with any customer, not just the buyer who mandated the platform — inverts the compliance burden narrative. Suppliers who see direct value participate more completely, which means more complete chains, which is the entire value proposition to buyers.

---

## Item 7 — SOC 2 Type I — Initiate (ROI: 7.5)

### 1. Objective
SOC 2 Type I readiness assessment initiated, with controls documented and an auditor engaged, starting the clock on the 6-month observation window needed for Type II.

### 2. Owner and dependencies
**Owner:** Julian (CEO/CTO)
**Dependencies:** External auditor selection and engagement.

### 3. Work breakdown
1. **Select SOC 2 auditor** — Evaluate 3 firms: Vanta (automated + auditor bundle), Drata (similar), or a traditional CPA firm. Vanta/Drata provide automated evidence collection which reduces solo-operator burden.
   - **Deliverable:** Signed engagement letter
2. **Complete readiness assessment** — Using the auditor's framework, document current controls across the 5 Trust Service Criteria:
   - Security: JWT auth, TLS, role-based access, hash chain integrity
   - Availability: Render hosting, health checks, retry logic
   - Processing Integrity: SHA-256 hash chains, compliance checks, audit logs
   - Confidentiality: Tenant isolation, R2 storage encryption
   - Privacy: Minimal PII, email-only user data
3. **Gap analysis** — Identify controls that are missing or undocumented. Common gaps for early-stage SaaS:
   - Formal incident response plan
   - Change management documentation
   - Access review process
   - Backup and disaster recovery documentation
   - Vendor management policy
4. **Remediate gaps** — Write the missing policies. Most are 1–2 page documents:
   - `docs/security/incident-response-plan.md`
   - `docs/security/change-management-policy.md`
   - `docs/security/access-review-policy.md`
   - `docs/security/backup-dr-policy.md`
   - `docs/security/vendor-management-policy.md`
5. **Begin observation window** — Once controls are documented and implemented, the Type II observation window starts. Minimum 3 months for some auditors, 6 months standard.
6. **Schedule Type I audit** — Type I is point-in-time. Can be completed while the Type II observation window runs.

### 4. Acceptance criteria
- [ ] Auditor engaged with signed letter
- [ ] Readiness assessment completed
- [ ] All identified control gaps remediated with documented policies
- [ ] Type II observation window start date recorded
- [ ] Type I audit scheduled

### 5. Estimated timeline
**4 weeks to initiate, then ongoing.**
- Week 1: Auditor evaluation and selection
- Week 2: Readiness assessment
- Weeks 3–4: Gap remediation (policy documents)
- Ongoing: Observation window (3–6 months)

**External dependency:** Auditor availability. Vanta/Drata can typically start within 2 weeks. Traditional CPA firms may have 4–6 week lead times.

### 6. Risks and mitigations
| Risk | Mitigation |
|------|-----------|
| Auditor lead time delays start | Begin evaluation immediately. Vanta/Drata offer fastest onboarding. |
| Gap remediation reveals infrastructure issues | Most gaps are documentation, not infrastructure. Render + Neon + R2 are SOC 2 compliant themselves. |
| Cost overrun | Vanta: ~$10k/yr platform + ~$10k audit. Budget $25k total for Year 1. |

### 7. Competitive impact
Every enterprise buyer's procurement process includes a security questionnaire. Without SOC 2, Auditraks stalls at the security review gate for any company with >$100M revenue. IPOINT and Source Intelligence both hold SOC 2 Type II. Starting the observation window now means Type II could be available by Q4 2026, aligning with the first enterprise contract conversations reaching procurement stage. Delaying this by even one quarter pushes Type II availability into 2027 and potentially loses the first enterprise customer to a competitor who already has it.

---

## Item 8 — EU Conflict Minerals Regulation (EU CMR) Framework (ROI: 7.0)

### 1. Objective
EU CMR compliance checks added to the compliance engine, covering the regulation's specific requirements for tungsten importers — enabling Auditraks to serve EU-based buyers and US buyers supplying into EU customers.

### 2. Owner and dependencies
**Owner:** Julian (CTO)
**Dependencies:** Item 1 (rule engine spec) recommended. Item 3 (smelter data) provides smelter due diligence data needed for EU CMR Step 3.

### 3. Work breakdown
1. **Map EU CMR requirements to existing data model** — Document which EU CMR obligations are already covered by OECD DDG (approximately 80%) and which are additive:
   - Already covered: Supply chain policy, origin identification, risk assessment (Steps 1–3 of OECD DDG)
   - Additive: Annual reporting obligation documentation, specific importer thresholds (triggering at 100kg tungsten/year), third-party audit requirement tracking, EU competent authority reporting format
   - **Deliverable:** `docs/specs/eu-cmr-gap-analysis.md`
2. **Create EU CMR checker** — New compliance checker class that evaluates:
   - Rule 1: Importer identification — does the buyer entity meet the EU CMR importer definition?
   - Rule 2: Due diligence system — are all 5 OECD DDG steps documented in the custody chain?
   - Rule 3: Third-party audit — is there a documented audit for the current reporting period?
   - Rule 4: Annual reporting — has the importer filed or prepared their annual report?
   - Rule 5: Volume threshold — does cumulative tungsten volume exceed 100kg/year?
   - **Layer:** .NET compliance checker class
3. **Add EU_CMR framework constant** — New framework alongside RMAP, OECD_DDG, MASS_BALANCE, SEQUENCE_CHECK, COUNTRY_CONSISTENCY.
   - **Layer:** .NET constants, frontend status badge
4. **Add tenant-level regulation selection** — Not all tenants need EU CMR. Add a `regulations` column to TenantEntity (text[], default: ["RMAP", "OECD_DDG"]). EU CMR checks only run for tenants that include "EU_CMR" in their regulations list.
   - **Layer:** PostgreSQL migration, EF Core entity, admin UI
5. **Update compliance pipeline** — Wire EU CMR checker into the existing compliance check flow, gated by tenant regulation selection.
   - **Layer:** .NET compliance service
6. **Frontend: EU CMR compliance display** — Ensure EU CMR results appear in compliance summary with clear descriptions of each rule.
   - **Layer:** Angular shared compliance-summary component
7. **Generate EU CMR annual report template** — New document generation type: a pre-filled annual report template based on the tenant's batch data, formatted per EU CMR Article 7 requirements.
   - **Layer:** .NET QuestPDF template, API endpoint
8. **Tests** — Unit tests for each EU CMR rule. Integration test with demo data.

### 4. Acceptance criteria
- [ ] EU CMR gap analysis document exists
- [ ] EU CMR checker produces results for all 5 rules
- [ ] EU CMR checks only run for tenants with EU_CMR in their regulations list
- [ ] Admin can enable/disable EU CMR for a tenant
- [ ] Annual report template can be generated for a reporting period
- [ ] Compliance summary displays EU CMR results

### 5. Estimated timeline
**4 weeks.**
- Week 1: Gap analysis, requirement mapping
- Week 2: EU CMR checker implementation, tenant regulation selection
- Week 3: Frontend display, annual report template
- Week 4: Testing, demo data, documentation

### 6. Risks and mitigations
| Risk | Mitigation |
|------|-----------|
| EU CMR requirements misinterpreted | Cross-reference with the regulation text (EU 2017/821) and the European Commission guidance document. Have a compliance advisor review. |
| Annual report format changes | Template is a starting point, not a submission-ready document. Mark as "draft for review" in output. |
| Scope creep into full EU regulatory compliance | EU CMR only. Do not expand to REACH, RoHS, or CBAM in this item. |

### 7. Competitive impact
All five primary competitors — IPOINT, Source Intelligence, Minespider, Circulor, TrusTrace — either cover EU CMR or are building toward it. EU CMR is becoming the international baseline for tungsten buyers regardless of geography. US manufacturers supplying into EU customers (which includes most Fortune 500 manufacturers with European operations) are already being asked about EU CMR compliance by their EU subsidiaries. Adding EU CMR roughly doubles Auditraks' total addressable market by making the platform viable for EU-based importers directly and for US companies whose supply chains cross into EU jurisdiction.

---

## A. Consolidated Timeline View

```
Week    1    2    3    4    5    6    7    8    9    10
Item 1  [====SPEC====]
Item 2  [==PRICING==]
Item 3            [=======SMELTER DB========]
Item 4                 [====CONSISTENCY=====]
Item 5                      [========DPP===========]
Item 6       [====SUPPLIER VALUE====]
Item 7  [==SELECT==][==ASSESS==][==REMEDIATE==]............observation window..........
Item 8                           [=========EU CMR============]
```

**Parallel streams:**
- Items 1 + 2 + 7 start Week 1 (all independent)
- Item 6 starts Week 2 (independent)
- Item 3 starts Week 3 (after Item 1 spec draft available)
- Item 4 starts Week 4 (can use Item 3 data if available, Rules 1–4 independent)
- Item 5 starts Week 5 (independent)
- Item 8 starts Week 7 (benefits from Items 1 + 3)

**Critical path:** Items 1 → 3 → 4 is the longest dependent chain (8 weeks). Item 7 has the longest wall-clock time due to the observation window (6+ months) but lowest weekly effort after initiation.

**Solo operator constraint:** Julian cannot execute all 8 items at full speed simultaneously. Recommended focus:
- Weeks 1–2: Items 1, 2, 7 (document/decision work, can be done in parallel)
- Weeks 3–5: Item 3 (heaviest build task)
- Weeks 4–6: Items 4 + 6 (lighter build + commercial work)
- Weeks 6–8: Items 5 + 8 (build tasks)
- Total elapsed: **10 weeks** with staggered starts

---

## B. Top 3 Cross-Cutting Risks

### 1. Solo operator bottleneck
**Trigger:** Julian is the only technical resource. Any illness, emergency, or competing priority (investor meetings, sales calls) delays all build items simultaneously.
**Items affected:** All 8, but especially Items 3, 4, 5, 8 (build work).
**Recovery path:** Identify one contract .NET developer who can execute against the implementation plans if Julian is unavailable for >1 week. The plans are written to be executable by someone with no prior codebase knowledge. Budget: $5k retainer.

### 2. Compliance rule engine spec delays cascade
**Trigger:** Item 1 spec takes longer than 2 weeks, or requires multiple revision cycles.
**Items affected:** Items 3, 4, 8 (all build against the rule engine).
**Recovery path:** The existing hardcoded rules work. Build Items 3, 4, 8 against current patterns. Refactor to configurable rules later when the spec is final. Accept technical debt as a conscious tradeoff.

### 3. Enterprise sales conversations outpace SOC 2 readiness
**Trigger:** An enterprise prospect reaches procurement stage before SOC 2 Type I is complete.
**Items affected:** Item 7, and indirectly Items 2 and the entire revenue timeline.
**Recovery path:** Offer a "SOC 2 in progress" letter from the auditor (Vanta/Drata provide these). Many procurement teams accept "in progress" for contracts under $100k/year with a commitment date for completion. This buys 3–6 months.

---

## C. Quick Wins Summary (Completable Within 5 Working Days, No External Dependencies)

- **Item 2, Task 2:** Define tier pricing with absolute numbers (1 day)
- **Item 2, Task 3:** Draft commercial terms document (1 day)
- **Item 2, Task 6:** Update landing page pricing section (0.5 days)
- **Item 6, Task 1:** Draft supplier value proposition document (1 day)
- **Item 6, Task 6:** Create supplier one-pager PDF (1 day)
- **Item 1, Task 1:** Audit current hardcoded compliance rules and document them (2 days)
- **Item 7, Task 1:** Send evaluation emails to Vanta, Drata, and one CPA firm (0.5 days)
- **Item 8, Task 1:** Write EU CMR gap analysis mapping existing OECD DDG coverage (2 days)

**Total quick win effort: ~9 days of work, all executable this week and next with no blockers.**
