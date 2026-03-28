# Backup and Disaster Recovery Policy

**Company:** Auditraks Ltd
**Version:** 1.0
**Last Reviewed:** 2026-03-28
**Owner:** Julian Shaw, CTO

---

## 1. Purpose
Define backup procedures and disaster recovery capabilities for all Auditraks production systems.

## 2. Systems and Backup Coverage

| System | Backup Method | Frequency | Retention | RTO | RPO |
|--------|-------------|-----------|-----------|-----|-----|
| PostgreSQL (Neon) | Neon automated backups | Continuous (WAL) | 7 days (free), 30 days (paid) | <1 hour | ~0 (point-in-time) |
| Document storage (R2) | Cloudflare R2 replication | Continuous | Indefinite | <1 hour | ~0 |
| Application code | GitHub repository | Every push | Indefinite | <15 min (Render redeploy) | Last commit |
| Environment variables | Render dashboard | Manual export | N/A | <30 min (manual restore) | Last export |
| Email templates | In codebase (Resend) | Every push | Indefinite | <15 min | Last commit |

**RTO:** Recovery Time Objective — how quickly service is restored
**RPO:** Recovery Point Objective — maximum acceptable data loss

## 3. Database Backup (Neon)

### Automated Backups
- Neon provides continuous backup via Write-Ahead Log (WAL) streaming
- Point-in-time recovery available within retention window
- No manual backup configuration required

### Recovery Procedure
1. Log into Neon dashboard
2. Select project → Branches
3. Create new branch from point-in-time (before incident)
4. Update Render `ConnectionStrings__DefaultConnection` to new branch endpoint
5. Redeploy API service
6. Verify data integrity

### Manual Backup (Monthly)
1. Export critical tables via `pg_dump` (users, tenants, batches, custody_events, compliance_checks)
2. Store export in R2 bucket `auditraks-backups/`
3. Document in `docs/security/backup-log.md`

## 4. Application Recovery

### Complete Redeployment
1. GitHub contains all application code
2. Render auto-deploys from `main` branch
3. EF Core migrations run automatically on startup
4. Seed data populates if database is empty
5. Environment variables restored from documented list

### Environment Variable Recovery
Documented in `docs/security/env-vars-inventory.md` (encrypted, stored separately):
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`
- `ConnectionStrings__DefaultConnection`
- `Resend__ApiKey`
- `Stripe__SecretKey`, `Stripe__WebhookSecret`, `Stripe__PriceId`
- `R2__AccountId`, `R2__AccessKeyId`, `R2__SecretAccessKey`, `R2__BucketName`
- `Sentry__Dsn`
- `OpenAI__ApiKey`, `Anthropic__ApiKey`

## 5. Disaster Scenarios

| Scenario | Recovery Path | Estimated Time |
|----------|-------------|----------------|
| Render outage | Wait for Render recovery. Render SLA: 99.95% | Hours (external) |
| Neon database corruption | Point-in-time recovery to before corruption | <1 hour |
| Accidental data deletion | Neon branch from point-in-time | <1 hour |
| GitHub repository loss | Local clones exist. Re-push to new repo. | <30 min |
| R2 storage failure | Cloudflare SLA: 99.99%. Re-upload from source if needed. | Hours (unlikely) |
| Domain/DNS failure | Render manages DNS for auditraks.com. Render support. | Hours (external) |

## 6. Testing
- **Quarterly:** Verify Neon point-in-time recovery works (create test branch, verify data)
- **Quarterly:** Verify Render rollback works (roll back to previous deploy, verify)
- **Annually:** Full disaster recovery drill (simulate complete redeployment from scratch)
