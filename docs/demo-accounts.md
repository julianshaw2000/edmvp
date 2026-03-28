# auditraks Demo Accounts & Data

## Demo Login

**URL:** https://auditraks.com/login

All demo accounts use password: `Demo1234!`

| Email | Password | Role | Portal |
|-------|----------|------|--------|
| supplier@auditraks.com | Demo1234! | SUPPLIER | Supplier dashboard — batches, custody events, documents |
| buyer@auditraks.com | Demo1234! | BUYER | Buyer dashboard — review supplier batches, verify passports |
| admin@auditraks.com | Demo1234! | TENANT_ADMIN | Admin dashboard — users, compliance overview, audit logs |
| julianshaw2000@gmail.com | Auditraks2026! | PLATFORM_ADMIN | Admin + platform management (tenants) |

## Pre-Seeded Demo Batches

| Batch | Mineral | Origin | Status | Compliance | Events | Story |
|-------|---------|--------|--------|------------|--------|-------|
| W-2026-041 | Tungsten (Wolframite) | Rwanda, Nyungwe Mine | COMPLETED | COMPLIANT | 6 | Full mine-to-refinery journey. All checks pass. |
| W-2026-038 | Tungsten (Wolframite) | DRC, Bisie Mine | ACTIVE | FLAGGED | 4 | DRC high-risk origin triggers OECD DDG flag. |
| W-2026-045 | Tungsten (Wolframite) | Bolivia, Huanuni Mine | CREATED | PENDING | 0 | Empty batch — use for live demo of adding events. |
| W-2026-035 | Tungsten (Cassiterite) | Rwanda, Rutongo Mine | COMPLETED | COMPLIANT | 5 | Cassiterite with tin trace, exported to US. |

## Batch W-2026-041 — Full Compliant Journey

The flagship demo batch showing the complete mine-to-refinery custody chain:

1. **Mine Extraction** (Day -28) — Nyungwe Mine, Rwanda. 450 kg wolframite ore, 65.2% WO3. Actor: Jean-Baptiste Habimana.
2. **Laboratory Assay** (Day -25) — SGS Minerals Laboratory, Kigali. XRF + ICP-OES confirms 65.2% WO3. Actor: Dr. Marie Uwimana.
3. **Concentration** (Day -21) — Wolfram Mining & Processing, Gisenyi. 450 kg in → 385 kg out (71% WO3 grade). Actor: Emmanuel Nsengiyumva.
4. **Trading Transfer** (Day -16) — Kigali Export Zone. Nyungwe Mining Coop → Great Lakes Minerals Trading SA. Actor: Patrick Mugisha.
5. **Primary Processing** (Day -8) — Wolfram Bergbau und Hutten AG (CID001100), Austria. Smelting: 385 kg → 310 kg APT. Actor: Klaus Steinberger.
6. **Export Shipment** (Day -4) — Port of Mombasa, Kenya. Rwanda → Austria via sea freight. Actor: Grace Wanjiku.

**Compliance Results:** RMAP PASS (CID001100 conformant) · OECD DDG PASS · Sanctions PASS · Mass Balance PASS (31% loss) · Sequence PASS

## Batch W-2026-038 — Flagged (DRC Origin)

Demonstrates the compliance flagging system:

1. **Mine Extraction** (Day -42) — Bisie Mine, Walikale Territory, DRC. 780 kg, 58.7% WO3.
2. **Laboratory Assay** (Day -38) — Bureau Veritas, Goma. XRF confirms 58.7% WO3.
3. **Concentration** (Day -32) — Goma Processing Facility. 780 kg → 650 kg (64% WO3).
4. **Trading Transfer** (Day -26) — Goma Border Trading Post. Bisie Mining SA → Kivu Minerals Export SARL.

**Compliance Results:** OECD DDG **FAIL** (DRC is high-risk CAHRA requiring enhanced due diligence) · Sanctions PASS · Mass Balance PASS · Sequence PASS

## Demo Walkthrough Script

### 1. Supplier Portal (supplier@auditraks.com)
- View dashboard with batch overview and compliance summary
- Open batch W-2026-041 to see full custody event chain
- View hash chain integrity (SHA-256 tamper evidence)
- Open batch W-2026-038 to see OECD DDG compliance flag
- Add a custody event to batch W-2026-045 (empty batch)
- Generate a Material Passport PDF for W-2026-041

### 2. Buyer Portal (buyer@auditraks.com)
- View available batches from suppliers
- Review compliance status and custody chain for W-2026-041
- Verify Material Passport via QR code (public verification, no login needed)

### 3. Admin Dashboard (admin@auditraks.com)
- View all batches across the tenant
- Review compliance overview (3/4 compliant, 1 flagged)
- Manage users (supplier, buyer accounts)
- View audit log of all system activity
- Review notifications

### 4. Platform Admin (julianshaw2000@gmail.com)
- Everything in Admin, plus:
- Manage tenants across the platform
- View platform-wide analytics

## Reference Data (Pre-Seeded)

### RMAP Smelters (10)
| ID | Name | Country | Status |
|----|------|---------|--------|
| CID001100 | Wolfram Bergbau und Hutten AG | Austria | CONFORMANT |
| CID002158 | Global Tungsten & Powders Corp. | USA | CONFORMANT |
| CID002082 | Xiamen Tungsten Co., Ltd. | China | ACTIVE |
| CID000999 | Unaudited Smelter Example | — | NON-CONFORMANT |
| CID001070 | Malaysia Smelting Corporation | Malaysia | CONFORMANT |
| CID000468 | PT Timah Tbk | Indonesia | CONFORMANT |
| CID000211 | Global Advanced Metals | Australia | CONFORMANT |
| CID002544 | KEMET Blue Powder | USA | CONFORMANT |
| CID000058 | Argor-Heraeus SA | Switzerland | CONFORMANT |
| CID000694 | PAMP SA | Switzerland | CONFORMANT |

### Risk Countries (OECD Annex II)
| Country | Risk Level |
|---------|-----------|
| DRC | HIGH |
| Rwanda | HIGH |
| Burundi | HIGH |
| Uganda | HIGH |
| Tanzania | MEDIUM |
| Kenya | LOW |

### Compliance Frameworks (5 checks per batch)
1. **RMAP** — Smelter verification against RMAP conformant list
2. **OECD DDG** — Origin country risk assessment (CAHRA)
3. **Sanctions** — Actor screening against UN/EU sanctions lists
4. **Mass Balance** — Input/output weight reconciliation (5% tolerance)
5. **Sequence Check** — Temporal ordering and hash chain integrity

## API Endpoints

**Base URL:** https://accutrac-api.onrender.com

| Endpoint | Description |
|----------|-------------|
| POST /api/auth/login | Login (returns JWT access token) |
| POST /api/auth/refresh | Refresh access token (HttpOnly cookie) |
| POST /api/auth/register | Register invited user |
| POST /api/auth/forgot-password | Request password reset email |
| GET /api/me | Current user profile |
| GET /health | Health check |
