# Incident Response Plan

**Company:** Auditraks Ltd
**Version:** 1.0
**Last Reviewed:** 2026-03-28
**Owner:** Julian Shaw, CTO

---

## 1. Purpose
Define how Auditraks identifies, responds to, and recovers from security incidents.

## 2. Scope
All systems: Render-hosted API and frontend, Neon PostgreSQL database, Cloudflare R2 storage, Resend email service, Stripe billing integration, GitHub repository.

## 3. Severity Classification

| Severity | Definition | Response Time | Example |
|----------|-----------|---------------|---------|
| Critical | Data breach, system compromise, complete outage | 1 hour | Unauthorized database access, credential leak |
| High | Partial outage, data integrity issue, auth bypass | 4 hours | Compliance check producing wrong results, JWT key leak |
| Medium | Performance degradation, minor data issue | 24 hours | Neon connection timeouts, elevated error rates |
| Low | Cosmetic, non-security functional issue | 72 hours | UI rendering issue, non-critical feature bug |

## 4. Response Procedure

### 4.1 Detection
- Sentry error monitoring (API exceptions)
- Render health checks (uptime monitoring)
- Neon dashboard (database metrics)
- Manual user reports

### 4.2 Triage (15 minutes)
1. Classify severity
2. Determine scope (which tenants/data affected)
3. Decide: contain immediately or investigate first

### 4.3 Containment
- **Auth compromise:** Rotate JWT signing key (Render env var `Jwt__Key`). All sessions invalidated.
- **Database breach:** Neon dashboard → suspend compute. Contact Neon support.
- **API vulnerability:** Render dashboard → roll back to last known-good deploy.
- **Third-party breach:** Rotate affected API keys (Resend, Stripe, R2, OpenAI).

### 4.4 Investigation
1. Review Sentry error logs
2. Review Render deploy history
3. Review audit_logs table for suspicious activity
4. Review GitHub commit history for unauthorized changes

### 4.5 Recovery
1. Deploy fix or rollback
2. Verify system functionality (health checks, login, batch creation)
3. Notify affected tenants within 72 hours (if data breach)
4. Document incident

### 4.6 Post-Incident Review
Within 5 business days:
- Root cause analysis
- Timeline of events
- What worked, what didn't
- Action items to prevent recurrence
- Document in `docs/security/incidents/YYYY-MM-DD-summary.md`

## 5. Communication

| Audience | Channel | When |
|----------|---------|------|
| Internal (Julian) | Sentry alerts, email | Immediate |
| Affected tenants | Email via Resend | Within 72 hours for data breaches |
| Regulatory (if required) | ICO (UK GDPR) | Within 72 hours for personal data breaches |

## 6. Annual Review
This plan is reviewed annually or after any Critical/High incident.
