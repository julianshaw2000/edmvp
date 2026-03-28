# Compliance Rule Engine Specification

**Version:** 1.0
**Status:** Draft
**Author:** Julian Shaw
**Date:** 2026-03-28

---

## 1. Purpose

Define how compliance rules are expressed, stored, versioned, and updated so the Compliance Lead can modify risk scoring, thresholds, and reference lists without code deployments.

## 2. Current State (Hardcoded Rules)

### 2.1 RMAP Checker
| Rule | Hardcoded Value | Location |
|------|----------------|----------|
| Passing smelter statuses | `["CONFORMANT", "ACTIVE_PARTICIPATING"]` | RmapChecker.cs |
| Trigger event type | `PRIMARY_PROCESSING` | RmapChecker.cs |
| Requires SmelterId | Yes | RmapChecker.cs |
| Cache TTL | 1 hour | RmapChecker.cs |

### 2.2 OECD DDG Checker
| Rule | Hardcoded Value | Location |
|------|----------------|----------|
| High-risk origin trigger | Countries where `RiskLevel == "HIGH"` | OecdDdgChecker.cs |
| Sanctions match | Exact name match against `SanctionedEntityEntity.EntityName` | OecdDdgChecker.cs |
| Required docs (MINE_EXTRACTION) | `CERTIFICATE_OF_ORIGIN`, `MINERALOGICAL_CERTIFICATE` | OecdDdgChecker.cs |
| Required docs (CONCENTRATION) | `ASSAY_REPORT` | OecdDdgChecker.cs |
| Required docs (TRADING_TRANSFER) | `TRANSPORT_DOCUMENT` | OecdDdgChecker.cs |
| Required docs (LABORATORY_ASSAY) | `ASSAY_REPORT` | OecdDdgChecker.cs |
| Required docs (PRIMARY_PROCESSING) | `SMELTER_CERTIFICATE` | OecdDdgChecker.cs |
| Required docs (EXPORT_SHIPMENT) | `EXPORT_PERMIT`, `TRANSPORT_DOCUMENT` | OecdDdgChecker.cs |
| Status hierarchy | `FAIL > FLAG > INSUFFICIENT_DATA > PASS` | OecdDdgChecker.cs |

### 2.3 Mass Balance Checker
| Rule | Hardcoded Value | Location |
|------|----------------|----------|
| Tolerance | 5% (multiplier: 1.05) | MassBalanceChecker.cs |
| Trigger event types | `CONCENTRATION`, `PRIMARY_PROCESSING` | MassBalanceChecker.cs |
| Required metadata fields | `inputWeightKg`, `outputWeightKg` | MassBalanceChecker.cs |

### 2.4 Sequence Checker
| Rule | Hardcoded Value | Location |
|------|----------------|----------|
| Chronological order required | Yes | SequenceChecker.cs |
| First event always valid | Yes | SequenceChecker.cs |
| Only records violations | Yes (no PASS record) | SequenceChecker.cs |

## 3. Proposed Data Model

### 3.1 Table: `compliance_rules`

```sql
CREATE TABLE compliance_rules (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id       UUID REFERENCES tenants("Id"),  -- NULL = global default
    framework       VARCHAR(30) NOT NULL,            -- RMAP, OECD_DDG, MASS_BALANCE, SEQUENCE_CHECK
    rule_version    VARCHAR(20) NOT NULL,            -- semver: "1.0.0", "1.1.0"
    status          VARCHAR(10) NOT NULL DEFAULT 'DRAFT',  -- DRAFT, ACTIVE, RETIRED
    rule_definition JSONB NOT NULL,
    effective_date  TIMESTAMP WITH TIME ZONE,
    created_by      UUID REFERENCES users("Id"),
    created_at      TIMESTAMP WITH TIME ZONE DEFAULT now(),
    updated_at      TIMESTAMP WITH TIME ZONE DEFAULT now(),

    CONSTRAINT uq_active_rule UNIQUE (tenant_id, framework, status)
        WHERE status = 'ACTIVE'  -- Only one ACTIVE rule per framework per tenant
);

CREATE INDEX idx_compliance_rules_lookup ON compliance_rules (framework, status, tenant_id);
```

### 3.2 Rule Definition Schemas (JSONB)

**RMAP:**
```json
{
  "passingStatuses": ["CONFORMANT", "ACTIVE_PARTICIPATING"],
  "triggerEventTypes": ["PRIMARY_PROCESSING"],
  "requireSmelterId": true,
  "cacheTtlMinutes": 60
}
```

**OECD DDG:**
```json
{
  "originRisk": {
    "highRiskTrigger": "FLAG",
    "riskLevelField": "HIGH"
  },
  "sanctions": {
    "matchType": "EXACT",
    "matchResult": "FAIL"
  },
  "requiredDocuments": {
    "MINE_EXTRACTION": ["CERTIFICATE_OF_ORIGIN", "MINERALOGICAL_CERTIFICATE"],
    "CONCENTRATION": ["ASSAY_REPORT"],
    "TRADING_TRANSFER": ["TRANSPORT_DOCUMENT"],
    "LABORATORY_ASSAY": ["ASSAY_REPORT"],
    "PRIMARY_PROCESSING": ["SMELTER_CERTIFICATE"],
    "EXPORT_SHIPMENT": ["EXPORT_PERMIT", "TRANSPORT_DOCUMENT"]
  },
  "statusHierarchy": ["FAIL", "FLAG", "INSUFFICIENT_DATA", "PASS"]
}
```

**Mass Balance:**
```json
{
  "maxGainPercent": 5.0,
  "triggerEventTypes": ["CONCENTRATION", "PRIMARY_PROCESSING"],
  "requiredMetadataFields": ["inputWeightKg", "outputWeightKg"],
  "violationResult": "FLAG"
}
```

**Sequence Check:**
```json
{
  "requireChronologicalOrder": true,
  "firstEventAlwaysValid": true,
  "violationResult": "FLAG",
  "recordPassResults": false
}
```

## 4. Versioning and Activation Model

1. **Creating a rule:** Admin creates a new rule version in DRAFT status.
2. **Activating:** Admin activates the rule. The system:
   - Sets the new rule to ACTIVE
   - Sets the previous ACTIVE rule to RETIRED
   - Records `effective_date` as now
3. **Rolling back:** Admin can re-activate a RETIRED version — same process.
4. **Audit trail:** All compliance check records store the `rule_version` used, so historical results reference the rule that produced them.
5. **Tenant override:** If a tenant-specific rule exists for a framework, it takes precedence over the global default. If not, the global rule applies.

### Resolution Order
```
1. compliance_rules WHERE tenant_id = {current_tenant} AND framework = {f} AND status = 'ACTIVE'
2. compliance_rules WHERE tenant_id IS NULL AND framework = {f} AND status = 'ACTIVE'
3. Hardcoded default (fallback during migration)
```

## 5. API Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| GET | `/api/admin/compliance-rules` | List all rules for tenant | RequireAdmin |
| GET | `/api/admin/compliance-rules/{framework}` | Get active rule for framework | RequireAdmin |
| GET | `/api/admin/compliance-rules/{framework}/history` | Version history | RequireAdmin |
| POST | `/api/admin/compliance-rules` | Create new rule (DRAFT) | RequireAdmin |
| PUT | `/api/admin/compliance-rules/{id}/activate` | Activate a rule | RequireAdmin |
| PUT | `/api/admin/compliance-rules/{id}/retire` | Retire a rule | RequireAdmin |

## 6. Admin UI Requirements

### Location: `/admin/compliance-rules`

**Rule List View:**
- Table: Framework | Version | Status | Effective Date | Actions
- Filter by framework
- Actions: View, Edit (DRAFT only), Activate, Retire

**Rule Editor:**
- JSON editor with schema validation
- Preview: shows what the rule will check before activating
- Diff view: shows changes from the currently active version
- Confirm dialog on activation: "This will replace version X.Y.Z. Existing compliance results will not be recalculated."

## 7. Migration Path

### Phase 1 (Current)
Checkers use hardcoded rules. `rule_version` = `"1.0.0-pilot"`.

### Phase 2 (After this spec is implemented)
1. Create `compliance_rules` table
2. Seed global default rules matching current hardcoded values
3. Update each checker to load rule from DB (with hardcoded fallback)
4. Build admin UI
5. Remove hardcoded fallbacks once admin UI is validated

### Phase 3 (Future)
- Per-tenant rule customisation
- Rule templates for common regulatory frameworks
- Rule import/export

## 8. Rollback Procedure

If a newly activated rule produces incorrect results:
1. Admin navigates to `/admin/compliance-rules/{framework}/history`
2. Selects the previous version
3. Clicks "Activate" — new rule is retired, previous is restored
4. Existing compliance results are NOT recalculated automatically
5. Admin can trigger a manual recalculation for specific batches if needed
