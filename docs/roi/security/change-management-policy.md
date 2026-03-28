# Change Management Policy

**Company:** Auditraks Ltd
**Version:** 1.0
**Last Reviewed:** 2026-03-28
**Owner:** Julian Shaw, CTO

---

## 1. Purpose
All changes to production systems follow a controlled process to minimise risk of outages, data loss, or security vulnerabilities.

## 2. Scope
Production deployments to Render (API + frontend), database schema changes (Neon), environment variable changes, and third-party service configuration.

## 3. Change Categories

| Category | Definition | Approval | Example |
|----------|-----------|----------|---------|
| Standard | Routine, low-risk, pre-approved | Self-approved | Dependency updates, UI copy changes |
| Normal | Feature additions, schema changes | Code review (PR) | New API endpoint, database migration |
| Emergency | Production incident fix | Post-deploy review | Security patch, critical bug fix |

## 4. Process

### 4.1 Standard Changes
1. Develop on feature branch
2. Build passes locally (`dotnet build && ng build`)
3. Tests pass (`dotnet test`)
4. Merge to `main` — Render auto-deploys
5. Verify deployment via health check

### 4.2 Normal Changes
1. Develop on feature branch
2. Create PR with description of changes
3. Build + tests pass in PR
4. Code review (self-review for solo operator, external review when available)
5. Merge to `main`
6. Verify deployment
7. Monitor Sentry for errors (1 hour post-deploy)

### 4.3 Emergency Changes
1. Fix on `main` or hotfix branch
2. Push directly — Render auto-deploys
3. Verify fix
4. Create post-deploy PR documenting the change within 24 hours
5. Post-incident review within 5 business days

## 5. Database Changes
- All schema changes via EF Core migrations
- Migrations run automatically on startup (`DatabaseMigrationService`)
- Data-destructive changes (column drops, table deletions) require manual review
- Use `RenameColumn` instead of drop+add to preserve data

## 6. Environment Variable Changes
- Changed via Render dashboard
- "Save only" for pre-deploy configuration
- "Save, rebuild, and deploy" for immediate effect
- Sensitive values (API keys, JWT key) never logged or committed to git

## 7. Rollback
- Render supports instant rollback to any previous deploy
- Database rollbacks via EF Core `dotnet ef database update {migration}` (requires shell access)
- Environment variable rollback: manual restore via Render dashboard
