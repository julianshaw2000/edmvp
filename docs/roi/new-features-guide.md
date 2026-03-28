# Auditraks — New Features Guide

**Date:** March 2026
**Covers:** Smelter Reference Database, Country Consistency Checks, Digital Product Passport, EU CMR Compliance

---

## 1. Smelter Reference Database & Origin Coherence Check

### What it does
The platform now maintains a live database of RMAP-conformant smelters with their sourcing countries. When a supplier logs a PRIMARY_PROCESSING (smelting) event, the system cross-references the declared smelter against the batch's origin country to verify the supply chain is geographically coherent.

### For Suppliers — Smelter Autocomplete

When submitting a custody event with type **Primary Processing (Smelting)**:

1. Navigate to **Submit Event** (sidebar or batch detail)
2. Select event type: **Primary Processing (Smelting)**
3. In the **Smelter (RMAP)** field, start typing the smelter name or ID
4. A dropdown appears showing matching smelters with their conformance status:
   - Green: CONFORMANT
   - Amber: ACTIVE_PARTICIPATING
   - Red: NON_CONFORMANT
5. Click to select — the smelter ID is automatically filled
6. Complete the remaining fields and submit

The system runs two checks on submission:
- **RMAP Check:** Is the smelter conformant?
- **Smelter Origin Coherence:** Does the smelter source from the batch's origin country?

### For Admins — Managing the Smelter Database

1. Go to **Admin > RMAP Data** (`/admin/rmap`)
2. View all smelters: name, ID, country, conformance status, sourcing countries
3. Click **Upload RMAP List** to import an updated smelter list

**CSV format for upload:**
```
SmelterId,SmelterName,Country,ConformanceStatus,LastAuditDate,MineralType,FacilityLocation,SourcingCountries
CID001100,Wolfram Bergbau und Hutten AG,AT,CONFORMANT,2025-06-15,Tungsten,St. Martin Austria,RW|CD|BO|CN|PT|ES
```

- `SourcingCountries` uses pipe (`|`) as delimiter: `RW|CD|BO`
- `MineralType`: Tungsten, Tin, Tantalum, or Gold
- `LastAuditDate`: YYYY-MM-DD format
- New fields (MineralType, FacilityLocation, SourcingCountries) are optional — existing CSV files without them still work

### Compliance Results

Results appear in the batch compliance tab as:

| Framework | Status | Meaning |
|-----------|--------|---------|
| RMAP | PASS | Smelter is conformant |
| RMAP | FLAG | Smelter not in RMAP list |
| RMAP | FAIL | Smelter is non-conformant |
| SMELTER_ORIGIN | PASS | Smelter sources from batch origin country |
| SMELTER_ORIGIN | FLAG | Smelter has no sourcing data (cannot verify) |
| SMELTER_ORIGIN | FAIL | Smelter does not source from batch origin country |

### API

**Search smelters:**
```
GET /api/smelters?q=wolfram&mineral=Tungsten&page=1&pageSize=10
```
Response:
```json
{
  "items": [
    {
      "smelterId": "CID001100",
      "smelterName": "Wolfram Bergbau und Hutten AG",
      "country": "AT",
      "conformanceStatus": "CONFORMANT",
      "mineralType": "Tungsten",
      "sourcingCountries": ["RW", "CD", "BO", "CN", "PT", "ES"]
    }
  ],
  "totalCount": 1
}
```

---

## 2. Cross-Event Country Consistency Checks

### What it does
Validates that the geographic journey of a batch makes sense across all custody events. Catches impossible supply chain routes — for example, a batch mined in Rwanda showing up at a smelter that only sources from China.

### Rules

| Rule | Triggers On | What It Checks | Result |
|------|------------|---------------|--------|
| Mine Origin Mismatch | MINE_EXTRACTION | Event metadata `originCountry` matches batch origin | FAIL if mismatch |
| Export Origin Mismatch | EXPORT_SHIPMENT | Export `originCountry` matches batch origin | FAIL if mismatch |
| Export-Smelter Mismatch | EXPORT_SHIPMENT | Export destination matches smelter country | FLAG if mismatch |
| Sanctioned Transit | CONCENTRATION, TRADING_TRANSFER | Event in sanctioned country ≠ batch origin | FLAG |

### How it works
These checks run automatically when any custody event is created. No configuration needed. Results appear in the batch compliance tab under the **COUNTRY_CONSISTENCY** framework.

### Example

Batch W-2026-041 (origin: Rwanda):
- Mine extraction in RW → PASS
- Export from RW to AT → PASS (matches smelter country)
- Smelter CID001100 in AT → PASS (export destination matches)

Batch with mismatch:
- Origin: RW (Rwanda)
- Export declares origin: CD (DRC) → **FAIL** — export origin doesn't match batch origin

### For suppliers
No action required. Just ensure your event metadata is accurate. The system checks consistency automatically.

---

## 3. Digital Product Passport (DPP) Generation

### What it does
Generates an EU Digital Product Passport in JSON-LD format for any completed batch. The DPP contains product identification, supply chain provenance, compliance status, and hash chain verification — all in a machine-readable format aligned with the EU Battery Regulation DPP schema.

### For Buyers — Generating a DPP

1. Log in as a **Buyer** (`buyer@auditraks.com` / `Demo1234!`)
2. Navigate to a batch detail page
3. Click the **Generate & Share** tab
4. Click **Digital Product Passport** (green globe icon)
5. Wait for generation (2-3 seconds)
6. Download the JSON-LD file or share via link

The DPP sits alongside the existing Material Passport (PDF) and Audit Dossier. All three can be generated from the same tab.

### DPP Contents

The generated JSON-LD document includes:

| Section | Fields |
|---------|--------|
| **Product** | Batch number, mineral type, weight (kg), country of origin, mine site |
| **Supply Chain** | Total events, integrity method (SHA-256), hash chain status, event list (type, date, location, actor) |
| **Compliance** | Overall status, framework results (RMAP, OECD DDG, etc.) |
| **Verification** | QR code URL, hash chain integrity boolean |
| **Issuer** | Organisation name, platform URL |

### API

```
POST /api/batches/{batchId}/dpp
```

Response:
```json
{
  "id": "uuid",
  "downloadUrl": "https://...",
  "generatedAt": "2026-03-28T00:00:00Z"
}
```

The generated DPP is stored and can be shared using the existing document sharing system (`POST /api/generated-documents/{id}/share`).

### When to use it

- **EU buyers** requesting DPP-compliant provenance data
- **ESG reporting** requiring machine-readable supply chain data
- **Customer-of-customer requests** where downstream manufacturers need mineral provenance
- **Regulatory submissions** where EU DPP alignment is expected

---

## 4. EU Conflict Minerals Regulation (EU CMR) Compliance

### What it does
Adds EU CMR (EU 2017/821) compliance checks for tenants that import tungsten into the EU. The checker evaluates whether the supply chain due diligence meets EU CMR requirements and tracks cumulative import volumes against the regulation's threshold.

### Enabling EU CMR for a Tenant

EU CMR checks are **off by default**. To enable:

1. An admin or platform admin needs to update the tenant's `regulations` field to include `EU_CMR`
2. This can currently be done via the database:
```sql
UPDATE tenants SET "Regulations" = ARRAY['RMAP', 'OECD_DDG', 'EU_CMR']
WHERE "Name" = 'Your Tenant Name';
```
3. Once enabled, EU CMR checks run automatically on every new custody event for that tenant

### EU CMR Rules

| Rule | What It Checks | Result |
|------|---------------|--------|
| Due Diligence System | Are OECD DDG Steps 1-3 documented in the custody chain? (MINE_EXTRACTION for traceability, TRADING_TRANSFER or PRIMARY_PROCESSING for risk mitigation) | PASS if documented, INSUFFICIENT_DATA if gaps |
| Volume Threshold | Has the tenant imported ≥100kg of tungsten this calendar year? | FLAG if exceeded (full EU CMR obligations apply), PASS if below |

### Compliance Results

Results appear in the batch compliance tab as **EU_CMR** framework:

```
EU_CMR: PASS
  - due_diligence: PASS — OECD DDG Steps 1-3 documented in custody chain
  - volume_threshold: PASS — Annual tungsten import volume (450 kg) below threshold
```

Or:
```
EU_CMR: FLAG
  - due_diligence: PASS
  - volume_threshold: FLAG — Annual tungsten import volume (1,250 kg) exceeds EU CMR threshold (100 kg). Full EU CMR obligations apply.
```

### Who needs EU CMR?

Enable EU CMR for tenants that:
- Import tungsten (or products containing tungsten) into the EU
- Are based in the EU and source tungsten from outside the EU
- Supply US-manufactured goods into EU markets (EU CMR applies to their EU customers)

### Tenant Regulation Options

The `Regulations` field accepts any combination:

| Value | Framework |
|-------|-----------|
| `RMAP` | RMAP smelter verification (always on by default) |
| `OECD_DDG` | OECD Due Diligence Guidance (always on by default) |
| `EU_CMR` | EU Conflict Minerals Regulation (opt-in) |

Default for new tenants: `["RMAP", "OECD_DDG"]`

---

## Summary of All Compliance Frameworks

After these updates, auditraks runs up to **7 compliance frameworks** per batch:

| Framework | Check | Runs When |
|-----------|-------|-----------|
| **RMAP** | Smelter conformance verification | PRIMARY_PROCESSING with SmelterId |
| **SMELTER_ORIGIN** | Smelter sourcing country matches batch origin | PRIMARY_PROCESSING with SmelterId |
| **OECD_DDG** | Origin risk, sanctions, document completeness | Every custody event |
| **MASS_BALANCE** | Output weight ≤ input weight + 5% | CONCENTRATION, PRIMARY_PROCESSING |
| **SEQUENCE_CHECK** | Events in chronological order | Every custody event |
| **COUNTRY_CONSISTENCY** | Geographic journey makes sense | Every custody event |
| **EU_CMR** | EU CMR due diligence + volume threshold | Every custody event (EU_CMR tenants only) |

### Compliance Status Hierarchy

The batch overall compliance status is determined by the worst result across all frameworks:

```
FAIL or FLAG in any framework  →  FLAGGED
INSUFFICIENT_DATA (no FAIL/FLAG)  →  INSUFFICIENT_DATA
All PASS  →  COMPLIANT
No checks yet  →  PENDING
```

---

## Demo Walkthrough

To test all new features with demo data:

### 1. Smelter Search
Log in as `supplier@auditraks.com` / `Demo1234!`
- Go to **Submit Event**
- Select batch W-2026-045 (the empty Bolivia batch)
- Select event type: **Primary Processing (Smelting)**
- Type "Wolfram" in the Smelter field
- See autocomplete results with conformance status

### 2. Country Consistency
Submit the smelting event above. The system will automatically check:
- Does Wolfram Bergbau (Austria) source from Bolivia?
- Check SMELTER_ORIGIN and COUNTRY_CONSISTENCY results in batch compliance tab

### 3. Digital Product Passport
Log in as `buyer@auditraks.com` / `Demo1234!`
- Go to batch W-2026-041 (the completed Rwanda batch)
- Click **Generate & Share** tab
- Click **Digital Product Passport**
- Download the JSON-LD file

### 4. EU CMR (requires database update)
Enable EU CMR for the Pilot Tenant:
```sql
UPDATE tenants SET "Regulations" = ARRAY['RMAP', 'OECD_DDG', 'EU_CMR']
WHERE "Name" = 'Pilot Tenant';
```
Then submit any new custody event. EU CMR results will appear in the compliance tab.
