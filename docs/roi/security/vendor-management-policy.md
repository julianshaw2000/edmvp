# Vendor Management Policy

**Company:** Auditraks Ltd
**Version:** 1.0
**Last Reviewed:** 2026-03-28
**Owner:** Julian Shaw, CTO

---

## 1. Purpose
Document all third-party vendors with access to Auditraks systems or data, their security posture, and review procedures.

## 2. Vendor Inventory

| Vendor | Service | Data Access | SOC 2 | Contract |
|--------|---------|------------|-------|----------|
| Render | Application hosting (API + frontend) | Application runtime, env vars | Yes (Type II) | Terms of Service |
| Neon | PostgreSQL database | All application data | Yes (Type II) | Terms of Service |
| Cloudflare | R2 storage, DNS, CDN | Document files | Yes (Type II) | Terms of Service |
| Stripe | Payment processing | Customer email, subscription data | Yes (Type II) | Connected Account Agreement |
| Resend | Transactional email | Recipient email addresses, email content | SOC 2 pending | Terms of Service |
| GitHub | Source code repository | Application source code | Yes (Type II) | Terms of Service |
| Sentry | Error monitoring | Error logs (may contain user context) | Yes (Type II) | Terms of Service |
| OpenAI | AI features | Prompt/response data (no PII by design) | Yes (Type II) | API Terms |
| Anthropic | AI features | Prompt/response data (no PII by design) | Yes (Type II) | API Terms |

## 3. Vendor Selection Criteria
- SOC 2 Type II preferred (6 of 9 vendors compliant)
- Data processing agreement (DPA) available for GDPR compliance
- Encryption at rest and in transit
- No data sharing with third parties without consent
- Documented incident response procedure

## 4. Review Schedule

| Review | Frequency | Actions |
|--------|-----------|---------|
| Vendor SOC 2 status | Annually | Verify current SOC 2 report available |
| Data processing agreements | Annually | Review DPA terms, confirm compliance |
| Credential rotation | Quarterly | Rotate API keys for all vendors |
| Vendor security incidents | Ongoing | Monitor vendor status pages and security advisories |
| Vendor cost review | Quarterly | Review billing, identify cost optimisation |

## 5. Vendor Offboarding
When discontinuing a vendor:
1. Revoke all API keys and credentials
2. Export/migrate data before termination
3. Confirm data deletion per vendor's DPA
4. Update this inventory
5. Update application configuration

## 6. Data Processing Locations

| Vendor | Primary Region | Data Residency |
|--------|---------------|----------------|
| Render | US (Oregon) | United States |
| Neon | US (East) | United States |
| Cloudflare R2 | Auto (global) | United States (primary) |
| Stripe | US/EU (varies) | Per Stripe DPA |
| Resend | US | United States |
| GitHub | US | United States |
| Sentry | US | United States |

All data processing occurs within the United States. For EU customers, standard contractual clauses (SCCs) apply per each vendor's DPA.
