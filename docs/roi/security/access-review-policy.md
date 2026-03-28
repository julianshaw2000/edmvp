# Access Review Policy

**Company:** Auditraks Ltd
**Version:** 1.0
**Last Reviewed:** 2026-03-28
**Owner:** Julian Shaw, CTO

---

## 1. Purpose
Ensure access to systems and data is limited to authorised personnel and reviewed regularly.

## 2. Systems in Scope

| System | Access Type | Who Has Access |
|--------|-----------|----------------|
| GitHub (julianshaw2000/edmvp) | Repository admin | Julian Shaw |
| Render Dashboard | Service management | Julian Shaw |
| Neon PostgreSQL | Database admin | Julian Shaw (via Render env var) |
| Cloudflare R2 | Storage admin | Julian Shaw |
| Stripe Dashboard | Billing admin | Julian Shaw |
| Resend Dashboard | Email admin | Julian Shaw |
| Sentry | Error monitoring | Julian Shaw |
| Auditraks Platform (PLATFORM_ADMIN) | All tenant data | Julian Shaw |

## 3. Access Principles
- **Least privilege:** Users get the minimum role needed (SUPPLIER, BUYER, TENANT_ADMIN)
- **No shared accounts:** Every user has their own credentials
- **MFA:** Enabled on GitHub, Render, Stripe, Cloudflare (all infrastructure services)
- **API keys:** Hashed (SHA-256), prefixed for identification, revocable by tenant admin

## 4. Review Schedule

| Review | Frequency | Reviewer |
|--------|-----------|----------|
| Infrastructure access (GitHub, Render, Neon) | Quarterly | Julian Shaw |
| Platform user access (active users per tenant) | Monthly | Tenant Admin |
| API key inventory | Quarterly | Julian Shaw |
| Third-party service credentials | Quarterly | Julian Shaw |

## 5. User Lifecycle

### Onboarding
1. Tenant admin invites user via `/admin/users`
2. User registers via `/register` (sets password)
3. Email confirmation required before login
4. User receives minimum required role

### Offboarding
1. Tenant admin deactivates user via `/admin/users`
2. User's `IsActive` set to false — cannot authenticate
3. User's refresh tokens revoked
4. User data retained per data retention policy (5 years for compliance records)

### Access Change
1. Tenant admin updates user role via `/admin/users`
2. Change logged in audit log
3. Takes effect on next authentication

## 6. Privileged Access
- `PLATFORM_ADMIN` role has cross-tenant visibility
- Only assigned to Julian Shaw
- All PLATFORM_ADMIN actions logged in audit log
- Quarterly review: confirm no unauthorised PLATFORM_ADMIN accounts exist
