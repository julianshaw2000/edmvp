# Digital Product Passport (DPP) Schema Mapping

**Version:** 1.0 Draft
**Date:** 2026-03-28
**Reference:** EU Battery Regulation (2023/1542) DPP schema, ESPR (Ecodesign for Sustainable Products Regulation) draft

---

## 1. Applicable DPP Requirements for Raw Minerals

The EU DPP mandate currently applies to batteries (2027) and is expanding via ESPR to additional product categories. Tungsten as a raw material is not yet directly mandated, but:
- Downstream products containing tungsten (cutting tools, electronics, automotive parts) will require DPP
- Supply chain actors providing material to DPP-mandated products need machine-readable provenance data
- Early alignment positions Auditraks for the regulatory wave

## 2. Schema Mapping: Auditraks → DPP

| DPP Field | DPP Category | Auditraks Source | Available? |
|-----------|-------------|-----------------|------------|
| Product identifier | Identification | `batch.batchNumber` | Yes |
| Product type | Identification | `batch.mineralType` | Yes |
| Manufacturer/Producer | Identification | Tenant name | Yes |
| Manufacturing date | Identification | `batch.createdAt` | Yes |
| Country of manufacture | Identification | `batch.originCountry` | Yes |
| GTIN/EAN | Identification | Not captured | No (placeholder) |
| Weight/Mass | Physical | `batch.weightKg` | Yes |
| Material composition | Materials | `batch.mineralType` + assay metadata (WO3 %) | Partial |
| Recycled content % | Materials | Not captured | No (placeholder) |
| Hazardous substances | Materials | Not applicable for raw minerals | N/A |
| Carbon footprint (total) | Sustainability | Not captured | No (placeholder) |
| Carbon footprint (supply chain) | Sustainability | Not captured | No (placeholder) |
| Supply chain actor list | Supply Chain | Custody event actors | Yes |
| Country-of-origin chain | Supply Chain | Event locations + batch origin | Yes |
| Due diligence report | Compliance | Compliance check results | Yes |
| Third-party verification | Compliance | RMAP check result + smelter ID | Partial |
| End-of-life instructions | Circularity | Not applicable for raw minerals | N/A |
| Repair/reuse information | Circularity | Not applicable for raw minerals | N/A |
| QR code / data carrier | Access | Generated per batch | Yes |
| Unique passport ID | Access | `batch.id` (UUID) | Yes |

## 3. Proposed DPP JSON-LD Output

```json
{
  "@context": {
    "@vocab": "https://schema.org/",
    "dpp": "https://auditraks.com/schemas/dpp/v1/"
  },
  "@type": "dpp:DigitalProductPassport",
  "@id": "https://auditraks.com/api/public/dpp/{shareToken}",
  "dpp:passportVersion": "1.0",
  "dpp:issuedDate": "2026-03-28T00:00:00Z",
  "dpp:issuer": {
    "@type": "Organization",
    "name": "Auditraks Ltd",
    "url": "https://auditraks.com"
  },

  "dpp:product": {
    "@type": "Product",
    "identifier": "W-2026-041",
    "name": "Tungsten (Wolframite)",
    "weight": {
      "@type": "QuantitativeValue",
      "value": 450,
      "unitCode": "KGM"
    },
    "countryOfOrigin": "RW",
    "productionFacility": "Nyungwe Mine"
  },

  "dpp:supplyChain": {
    "@type": "dpp:CustodyChain",
    "totalEvents": 6,
    "integrityMethod": "SHA-256 hash chain",
    "events": [
      {
        "@type": "dpp:CustodyEvent",
        "eventType": "MINE_EXTRACTION",
        "date": "2026-02-26T00:00:00Z",
        "location": "Nyungwe Mine, Rwanda",
        "actor": "Jean-Baptiste Habimana"
      }
    ]
  },

  "dpp:compliance": {
    "@type": "dpp:ComplianceStatus",
    "overallStatus": "COMPLIANT",
    "frameworks": [
      {
        "name": "RMAP",
        "status": "PASS",
        "checkedAt": "2026-03-22T00:00:00Z"
      },
      {
        "name": "OECD DDG",
        "status": "PASS",
        "checkedAt": "2026-03-22T00:00:00Z"
      }
    ]
  },

  "dpp:verification": {
    "@type": "dpp:VerificationInfo",
    "qrCodeUrl": "https://auditraks.com/verify/{batchId}",
    "publicViewUrl": "https://auditraks.com/api/public/dpp/{shareToken}",
    "hashChainIntact": true
  },

  "dpp:sustainability": {
    "@type": "dpp:SustainabilityData",
    "carbonFootprint": null,
    "recycledContent": null,
    "note": "Sustainability metrics not yet captured for raw mineral stage"
  }
}
```

## 4. Implementation Notes

- Use `application/ld+json` content type
- Host context definition at `https://auditraks.com/schemas/dpp/v1/context.jsonld`
- Store generated DPP as a `GeneratedDocument` with `documentType = "DPP"`
- QR code on Material Passport PDF links to the public DPP viewer
- DPP is accessible without login via share token (same pattern as Material Passport sharing)

## 5. Gap Fields (Future Work)

| Field | Status | Path to Fill |
|-------|--------|-------------|
| GTIN/EAN | Not applicable for raw minerals | May be needed for processed products |
| Material composition detail | Partial (mineral type only) | Enrich from assay metadata |
| Recycled content % | Not captured | Add optional field to batch creation |
| Carbon footprint | Not captured | Requires lifecycle assessment integration |
