# auditraks — Service Level Agreement (SLA)

**Effective Date:** April 2026
**Version:** 1.0
**Parties:** auditraks (the "Provider") and the subscribing organisation (the "Customer")

---

## 1. Scope

This Service Level Agreement ("SLA") defines the service commitments, performance targets, support obligations, and remedies that apply to the auditraks platform ("Service") provided to the Customer under their active subscription (Starter or Pro plan).

This SLA applies to the production environment at `https://auditraks.com` and the API at `https://accutrac-api.onrender.com`. It does not apply to development, staging, or sandbox environments.

---

## 2. Service Description

auditraks is a cloud-hosted SaaS platform for mineral supply chain compliance, providing:

- **Supplier Portal** — batch creation, custody event logging, document upload, material passport generation
- **Buyer Portal** — compliance monitoring, supplier engagement tracking, Form SD support, CMRT import
- **Admin Dashboard** — user management, compliance review, audit logging, billing management
- **Public Verification** — batch verification and shared document access without login
- **API** — programmatic access via REST API with API key authentication
- **Background Services** — compliance checking, notification delivery, supplier reminders

---

## 3. Availability

### 3.1 Uptime Commitment

| Plan | Monthly Uptime Target |
|------|----------------------|
| Starter | 99.5% |
| Pro | 99.9% |

**Uptime** is measured as the percentage of time the Service is available during a calendar month, calculated as:

```
Uptime % = ((Total Minutes − Downtime Minutes) / Total Minutes) × 100
```

### 3.2 What Counts as Downtime

Downtime begins when the Customer is unable to access the Service due to a failure within the Provider's reasonable control, and ends when the Service is restored.

**Downtime includes:**
- API returning 5xx errors for sustained periods (>5 consecutive minutes)
- Web application unreachable (HTTP connection refused or timeout)
- Database unavailable causing data access failures
- Authentication service failing to issue or validate tokens

**Downtime excludes:**
- Scheduled maintenance (see Section 4)
- Customer's internet connectivity, browser, or device issues
- Third-party service outages beyond the Provider's control (Stripe, Resend, Cloudflare global outage)
- Force majeure events (natural disasters, government actions, war)
- Customer-caused issues (API abuse, exceeding rate limits, misconfigured integrations)
- Features in beta or preview status

### 3.3 Measurement

Uptime is measured using synthetic health checks against the API health endpoint (`/health`) at 1-minute intervals from an external monitoring service.

---

## 4. Scheduled Maintenance

### 4.1 Maintenance Windows

The Provider may perform scheduled maintenance that temporarily reduces service availability.

| Type | Notice Period | Maximum Duration | Frequency |
|------|--------------|-----------------|-----------|
| Routine maintenance | 48 hours | 30 minutes | Monthly |
| Critical security patch | 4 hours | 15 minutes | As needed |
| Database migration | 72 hours | 60 minutes | Quarterly |

### 4.2 Notification

Maintenance notifications are sent via:
- Email to all Tenant Admin users
- In-app notification banner (when possible)

### 4.3 Scheduling

Routine maintenance is scheduled during low-usage periods, typically:
- Weekdays: 02:00–06:00 UTC
- Weekends preferred for longer maintenance windows

---

## 5. Support

### 5.1 Support Channels

| Channel | Availability | Response Target |
|---------|-------------|-----------------|
| Email (`support@auditraks.com`) | Business hours (Mon–Fri, 09:00–17:00 GMT) | See Section 5.2 |
| In-platform notifications | 24/7 (automated) | Immediate |
| Documentation (`docs/manuals/`) | 24/7 (self-service) | N/A |

### 5.2 Response Times

| Severity | Definition | Starter Response | Pro Response |
|----------|-----------|-----------------|-------------|
| **Critical** | Service completely unavailable; all users affected | 4 business hours | 1 business hour |
| **High** | Major feature unavailable; significant user impact | 8 business hours | 4 business hours |
| **Medium** | Feature degraded; workaround available | 2 business days | 1 business day |
| **Low** | Cosmetic issue, feature request, general question | 5 business days | 2 business days |

### 5.3 Severity Definitions

| Severity | Examples |
|----------|---------|
| **Critical** | Cannot log in, API returns errors for all requests, data loss, security breach |
| **High** | Cannot create batches, compliance checks not running, email notifications failing, passport generation broken |
| **Medium** | Slow page loads, incorrect display formatting, export function not working, search returning incomplete results |
| **Low** | Typo in UI, feature suggestion, documentation clarification, styling inconsistency |

### 5.4 Resolution Times

Resolution times are best-effort targets, not guarantees. Complex issues may require longer investigation.

| Severity | Resolution Target |
|----------|------------------|
| Critical | 8 hours |
| High | 24 hours |
| Medium | 5 business days |
| Low | 30 business days |

---

## 6. Data

### 6.1 Data Protection

| Measure | Implementation |
|---------|---------------|
| Encryption in transit | TLS 1.2+ on all connections |
| Encryption at rest | Database and file storage encrypted at rest |
| Access control | Role-based access with tenant isolation |
| Audit logging | All user actions logged with timestamp, user ID, and action detail |
| Tamper evidence | SHA-256 hash chain on all custody events |

### 6.2 Data Backup

| Item | Frequency | Retention |
|------|-----------|-----------|
| Database | Continuous (point-in-time recovery) | 7 days |
| Document storage | Replicated across availability zones | Indefinite (while subscription active) |

### 6.3 Data Retention After Cancellation

| Period | Access |
|--------|--------|
| 0–30 days after cancellation | Read-only access to existing data |
| 30–90 days after cancellation | Data retained but not accessible; can be restored on request |
| After 90 days | Data permanently deleted |

### 6.4 Data Export

The Customer may export their data at any time via:
- **Audit log CSV export** — full action history
- **Material Passport PDF** — per-batch compliance documentation
- **Audit Dossier PDF** — comprehensive batch evidence package
- **API** — programmatic access to all batch, event, and document data

---

## 7. Performance

### 7.1 Response Time Targets

| Operation | Target (95th percentile) |
|-----------|-------------------------|
| Page load (web app) | < 3 seconds |
| API read requests (GET) | < 500ms |
| API write requests (POST/PATCH) | < 1 second |
| Document generation (passport/dossier) | < 10 seconds |
| Search and filter operations | < 1 second |

### 7.2 Capacity

| Limit | Starter Plan | Pro Plan |
|-------|-------------|----------|
| Batches | 50 | Unlimited |
| Users | 5 | Unlimited |
| Document storage | 1 GB | 10 GB |
| API requests | 1,000/day | 10,000/day |

---

## 8. Security

### 8.1 Security Measures

- Authentication via ASP.NET Core Identity with JWT tokens (15-minute access, 14-day refresh)
- All API endpoints require authentication (except public verification and health check)
- API key authentication available for programmatic access
- HTTPS enforced on all endpoints
- CORS restricted to authorised origins
- Rate limiting on authentication endpoints

### 8.2 Incident Response

In the event of a security incident:

| Step | Action | Timeline |
|------|--------|----------|
| 1 | Detect and contain the incident | Immediate |
| 2 | Assess scope and impact | Within 4 hours |
| 3 | Notify affected customers | Within 24 hours |
| 4 | Remediate and restore service | As soon as possible |
| 5 | Provide post-incident report | Within 5 business days |

### 8.3 Vulnerability Management

- Dependencies scanned for known vulnerabilities (GitHub Dependabot)
- Critical security patches applied within 48 hours of disclosure
- Platform security reviewed quarterly

---

## 9. Service Credits

### 9.1 Eligibility

If the Provider fails to meet the uptime commitment in Section 3.1, the Customer may request a service credit.

### 9.2 Credit Calculation

| Monthly Uptime | Credit (% of monthly fee) |
|----------------|--------------------------|
| 99.0%–99.5% (Starter) or 99.5%–99.9% (Pro) | 10% |
| 95.0%–99.0% | 25% |
| Below 95.0% | 50% |

### 9.3 Credit Conditions

- Credits must be requested within 30 days of the affected month
- Credits are applied to the next billing cycle (not refunded as cash)
- Credits do not exceed 50% of the monthly subscription fee
- Credits are not issued for downtime caused by excluded events (Section 3.2)
- The Customer must provide reasonable evidence of the outage (e.g., screenshots, timestamps, error messages)

### 9.4 How to Request Credits

Email `support@auditraks.com` with:
- Subject: "SLA Credit Request — [Month/Year]"
- Description of the outage, including dates, times, and impact
- Supporting evidence

---

## 10. Limitations and Exclusions

### 10.1 This SLA Does Not Cover

- Features labelled as "Beta", "Preview", or "Experimental"
- The mobile PWA when used offline (events are queued locally; sync depends on connectivity)
- Third-party integrations (Stripe billing portal, email delivery to recipient's inbox)
- Custom API integrations built by the Customer
- Data accuracy — the Provider ensures the platform functions correctly but is not responsible for the accuracy of data entered by users

### 10.2 Fair Use

The Service is intended for normal business use in mineral supply chain compliance. The Provider reserves the right to throttle or suspend access in cases of:
- Automated scraping or bulk data extraction beyond API rate limits
- Use of the platform for purposes unrelated to supply chain compliance
- Actions that degrade service performance for other customers

---

## 11. Changes to This SLA

The Provider may update this SLA with 30 days' written notice to all Tenant Admin users. Continued use of the Service after the effective date of a change constitutes acceptance of the updated SLA.

Material reductions in service commitments entitle the Customer to terminate their subscription without penalty within 30 days of the change notice.

---

## 12. Contact

| Purpose | Contact |
|---------|---------|
| Support requests | `support@auditraks.com` |
| SLA credit requests | `support@auditraks.com` |
| Security incidents | `support@auditraks.com` (subject: SECURITY) |
| Billing enquiries | Stripe Customer Portal (via Admin Dashboard) |
| General enquiries | `support@auditraks.com` |

---

**auditraks** — Mineral supply chain compliance, automated.
