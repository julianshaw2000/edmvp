# EU Conflict Minerals Regulation (EU 2017/821) — Gap Analysis

**Date:** 2026-03-28
**Reference:** Regulation (EU) 2017/821 (in force since 1 January 2021)
**Scope:** Tungsten (one of 3TG minerals covered)

---

## 1. Overview

EU CMR requires EU importers of tin, tantalum, tungsten, and gold to carry out supply chain due diligence aligned with the OECD DDG. Auditraks already covers OECD DDG — the gap is the EU CMR-specific wrapper requirements.

## 2. EU CMR Obligations Mapped to Auditraks Coverage

| EU CMR Article | Obligation | Auditraks Coverage | Gap? |
|---------------|-----------|-------------------|------|
| Art. 3 | Importer identification (>100kg tungsten/year threshold) | Not tracked | **YES** — need volume threshold tracking per importer |
| Art. 4 | Supply chain due diligence system | Custody chain + compliance checks | Partial — no explicit "due diligence system" documentation |
| Art. 5 | Supply chain policy (OECD DDG Step 1) | Not documented in platform | **YES** — need policy template/upload |
| Art. 6 | Traceability (OECD DDG Steps 1-2) | Full custody event chain | **Covered** |
| Art. 7 | Risk identification (OECD DDG Step 2) | OECD DDG origin risk check | **Covered** |
| Art. 8 | Risk mitigation (OECD DDG Step 3) | Compliance flagging + notifications | **Covered** |
| Art. 9 | Third-party audit (OECD DDG Step 4) | Not tracked | **YES** — need audit tracking |
| Art. 10 | Annual reporting (OECD DDG Step 5) | Not supported | **YES** — need report template |
| Art. 11 | Competent authority reporting | No EU authority integration | **YES** — need export format |
| Art. 14 | Record keeping (5 years) | Audit logs + immutable events | **Covered** |

## 3. Gap Summary

### Already Covered (~70%)
- Supply chain traceability (custody events)
- Origin risk identification (OECD DDG checker)
- Sanctions screening
- Record keeping (audit logs, hash chains)
- Risk notification system

### Gaps to Close (~30%)

#### Gap 1: Importer Volume Threshold
EU CMR applies to importers exceeding annual volume thresholds (100 kg for tungsten). Need to track cumulative import volume per tenant per calendar year and flag when threshold is approached/exceeded.

**Implementation:** Add `annual_import_volume_kg` computed from batch data. EU CMR checker evaluates against threshold.

#### Gap 2: Supply Chain Policy Documentation
Art. 5 requires importers to have a documented supply chain policy. The platform should provide a template and track whether the tenant has uploaded/confirmed their policy.

**Implementation:** Add `tenant_documents` table. Admin uploads supply chain policy PDF. EU CMR checker verifies policy exists and is current (reviewed within 12 months).

#### Gap 3: Third-Party Audit Tracking
Art. 9 requires importers to have independent third-party audits of their supply chain due diligence. Need to track audit records: auditor name, date, scope, result.

**Implementation:** Add `audit_records` table. Admin records third-party audits. EU CMR checker verifies audit exists for current reporting period.

#### Gap 4: Annual Reporting
Art. 10 requires importers to publicly report on their due diligence. Need to generate an annual report template pre-filled with batch data, compliance results, and risk mitigation actions.

**Implementation:** New document generation type: `EU_CMR_ANNUAL_REPORT`. QuestPDF template. Endpoint: `POST /api/batches/generate/eu-cmr-report?year=2026`.

#### Gap 5: Competent Authority Reporting Format
Art. 11 requires reporting to national competent authorities in a specified format.

**Implementation:** Deferred. Format varies by EU member state. Provide CSV/JSON export of required data fields. Full authority integration is a future item.

## 4. EU CMR Checker Rules

```
Rule 1 — Importer Threshold
  Condition: Tenant cumulative tungsten import volume > 100 kg/year
  Result: If exceeded → tenant is subject to EU CMR obligations
  If not exceeded → EU CMR checks return PASS (below threshold)

Rule 2 — Supply Chain Policy
  Condition: Tenant has uploaded a current supply chain policy document
  Result: PASS if policy exists and reviewed within 12 months
          INSUFFICIENT_DATA if no policy uploaded
          FLAG if policy older than 12 months

Rule 3 — Due Diligence System
  Condition: All OECD DDG steps documented in custody chain
  Result: PASS if Steps 1-3 covered (traceability, risk ID, risk mitigation)
          INSUFFICIENT_DATA if chain incomplete

Rule 4 — Third-Party Audit
  Condition: Audit record exists for current reporting period
  Result: PASS if audit recorded and result = SATISFACTORY
          FLAG if audit recorded with findings
          INSUFFICIENT_DATA if no audit recorded

Rule 5 — Annual Report
  Condition: Annual report generated for previous year
  Result: PASS if report exists
          INSUFFICIENT_DATA if not generated
```

## 5. Tenant Regulation Selection

Not all tenants need EU CMR. Add a `regulations` configuration per tenant:

```json
{
  "regulations": ["RMAP", "OECD_DDG"]           // Default (non-EU)
  "regulations": ["RMAP", "OECD_DDG", "EU_CMR"]  // EU importers
}
```

EU CMR checks only run for tenants with `EU_CMR` in their regulations list.

## 6. Priority

| Gap | Effort | Impact | Priority |
|-----|--------|--------|----------|
| Volume threshold tracking | Low | High — gates all other EU CMR checks | 1 |
| Annual report generation | Medium | High — most visible compliance output | 2 |
| Supply chain policy tracking | Low | Medium — documentation requirement | 3 |
| Third-party audit tracking | Low | Medium — audit record keeping | 4 |
| Competent authority format | High | Low — varies by member state | 5 (defer) |
