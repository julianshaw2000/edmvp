# Auditraks Enterprise Engagement Summary

**For:** [Company Name]
**Prepared by:** Julian Shaw, CEO
**Date:** [Date]

---

## What Auditraks Does

Auditraks is a tungsten supply chain compliance platform that automates RMAP and OECD due diligence from mine to refinery. Every custody event is SHA-256 hashed and cryptographically chained, creating tamper-evident audit trails that compliance teams can verify independently.

## What You Get

| Capability | Description |
|-----------|-------------|
| **Custody tracking** | Track mineral batches through extraction, assay, concentration, trading, smelting, and export |
| **Automated compliance** | 5 checks per batch: RMAP smelter verification, OECD origin risk, sanctions screening, mass balance, sequence integrity |
| **Material Passports** | PDF + Digital Product Passport with QR code for public verification |
| **Audit Dossiers** | Comprehensive compliance documentation for regulatory audits |
| **Tamper evidence** | SHA-256 hash chain — alter one record and the entire chain breaks |
| **Supplier portal** | Your suppliers log custody events directly. No spreadsheets, no email chains. |
| **API + Webhooks** | Integrate with your ERP, CMRT tools, or internal systems |

## Onboarding Timeline

| Week | Activity |
|------|----------|
| 1 | Account setup, user invitations, system configuration |
| 2 | Supplier onboarding (3–5 priority suppliers) |
| 3 | First batch tracked end-to-end |
| 4 | Compliance review, Material Passport generation |

## Pricing

Enterprise plans start at $999/month (annual billing: $9,990/year). Custom pricing based on:
- Number of supply chain tiers
- Compliance framework requirements
- Integration scope
- Support SLA

## Security

| Control | Status |
|---------|--------|
| Authentication | ASP.NET Core Identity + JWT bearer tokens |
| Encryption in transit | TLS 1.2+ |
| Encryption at rest | AES-256 (Cloudflare R2) |
| Tenant isolation | Row-level security in PostgreSQL |
| Audit logging | Every action logged with user, timestamp, payload hash |
| Data integrity | SHA-256 hash chains per batch |
| SOC 2 | [In progress / Type I scheduled] |

## FAQ

**Q: Where is our data stored?**
A: PostgreSQL database hosted on Neon (US East). Document storage on Cloudflare R2 (global CDN). All infrastructure is US-based.

**Q: Can we export our data?**
A: Yes. CSV, JSON, and API export available. Data portability guaranteed in contract.

**Q: Do our suppliers need to pay?**
A: No. Supplier accounts are free. They get Material Passports as a benefit of participation.

**Q: What compliance frameworks do you cover?**
A: RMAP and OECD DDG today. EU CMR in development. Custom framework support on Enterprise plans.

---

**Next step:** Schedule a 30-minute demo at [booking link] or reply to this email.

**Contact:** Julian Shaw — julian@auditraks.com
