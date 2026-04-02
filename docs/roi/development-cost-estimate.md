# auditraks — Development Cost Estimate

**Single developer using Claude Code**
**Date:** April 2026

---

## Project Summary

| Metric | Value |
|--------|-------|
| **Development period** | 13 days (20 March – 2 April 2026) |
| **Total commits** | 317 |
| **Total lines of code** | 51,622 |
| **Test code** | 6,060 lines (53 test files, 210 tests) |
| **Documentation** | 71 markdown files, 24 PDF/Word documents |
| **Design specs** | 12 |
| **Implementation plans** | 27 |
| **Database migrations** | 33 |

---

## Codebase Breakdown

| Component | Language | Files | Lines | Purpose |
|-----------|---------|-------|-------|---------|
| API | C# (.NET 10) | 236 | 32,639 | REST API, MediatR CQRS, EF Core |
| Web App | TypeScript (Angular 21) | 101 | 11,727 | SPA with 3 portals (supplier, buyer, admin) |
| Worker | C# (.NET 10) | 9 | 441 | Background jobs (compliance, reminders, escalation) |
| MCP Servers | TypeScript | 20 | 755 | 55 AI assistant tools (customer + admin) |
| Tests | C# | 53 | 6,060 | 210 unit/integration tests |
| **Total** | | **419** | **51,622** | |

---

## Features Delivered

### Core Platform (Phases 1–10)
- Multi-tenant architecture with schema isolation
- ASP.NET Core Identity authentication (JWT + refresh tokens)
- 12-event custody chain engine with SHA-256 hash chain
- RMAP + OECD DDG compliance engine (5 automated checks)
- Document upload and storage (Cloudflare R2)
- Material Passport, Audit Dossier, and Digital Product Passport generation (QuestPDF)
- Supplier, Buyer, and Admin portals (Angular standalone components)
- Public batch verification with QR codes

### Infrastructure (Phases 11–18)
- Audit logging with CSV export
- Multi-tenant management with tenant isolation
- Stripe billing integration (checkout, webhooks, portal, plan enforcement)
- Landing page with signup flow
- Resend email integration (6 branded templates)
- Zoho Mail setup for inbound email
- 3TG mineral expansion (tungsten, tin, tantalum, gold)
- Plan enforcement (batch/user limits per tier)

### Intelligence & Engagement (Phases 19–25)
- Onboarding wizard
- Supplier onboarding checklist
- Material Passport sharing (download, link, email)
- Supplier engagement metrics panel
- Automated supplier reminders (30-day inactivity, 60-day stale)
- Manual buyer nudge with 7-day rate limiting
- CMRT v6.x import (Excel parser, preview/confirm flow)
- Batch ID and country typeahead search
- Analytics dashboard
- Public API with API key authentication
- Webhook notifications
- AI features (compliance reports, risk assessment, Form SD generation)
- Form SD (Dodd-Frank §1502) filing support

### Mobile / PWA
- Offline event queue (IndexedDB)
- Sync engine with auto-reconnect
- Mobile-optimized event logger (GPS + camera)
- QR code scanner
- Batch caching for offline viewing
- Offline connectivity banner

### Map View
- Leaflet/OpenStreetMap event location map
- Numbered markers with polyline custody journey
- Integrated into supplier and buyer batch detail

### MCP Integration
- Customer MCP server (32 tools, API key auth)
- Admin MCP server (23 tools, JWT auth)
- Shared API client library

### Documentation
- User Manual (v4)
- Admin System Manual (v2)
- Platform Maintenance Manual
- Tenant Admin Manual
- Platform Walkthrough
- QA Test Walkthrough
- Demo Walkthrough
- Demo Accounts
- Third-Party Services Guide
- Competitor Features Test Walkthrough
- MCP Demo Script
- Service Level Agreement
- PWA Mobile Supplier Requirements
- Competitor Workflow Analysis
- 12 design specs, 27 implementation plans

---

## Traditional Development Estimate (Without AI)

Estimating what this project would cost with a traditional development team, without Claude Code:

### Time Estimate

| Component | Traditional Estimate | Reasoning |
|-----------|---------------------|-----------|
| Architecture & design | 2 weeks | Database schema, API design, auth, multi-tenancy |
| Core API (CQRS, entities, endpoints) | 6 weeks | 236 C# files, 83 endpoints, 33 migrations |
| Angular web app (3 portals) | 5 weeks | 101 components, 3 role-based portals, responsive |
| Compliance engine | 2 weeks | 5 compliance checks, rule evaluation, hash chain |
| Document generation (PDF, JSON-LD) | 2 weeks | QuestPDF templates, QR codes, share tokens |
| Stripe billing integration | 1.5 weeks | Checkout, webhooks, portal, plan enforcement |
| Auth system (Identity, JWT, refresh) | 1.5 weeks | Login, register, password reset, API keys |
| Background worker services | 1 week | Compliance, reminders, escalation, email retry |
| Competitor gap features | 2 weeks | Onboarding, passport sharing, engagement, CMRT |
| PWA offline features | 2 weeks | IndexedDB, sync engine, mobile views, QR |
| Map view | 0.5 weeks | Leaflet integration, markers, polyline |
| MCP servers | 1.5 weeks | 55 tools, shared client, two servers |
| Testing | 2 weeks | 210 tests, integration tests |
| Documentation | 2 weeks | 11 manuals, SLA, glossary, PDF generation |
| DevOps & deployment | 1 week | Render, CI/CD, DNS, email config |
| **Total** | **~32 weeks** | **~8 months** |

### Cost Estimate (Traditional)

| Scenario | Rate | Duration | Cost |
|----------|------|----------|------|
| **Solo senior full-stack developer (UK)** | £600/day | 32 weeks (160 days) | **£96,000** |
| **Solo senior full-stack developer (US)** | $800/day | 32 weeks (160 days) | **$128,000** |
| **Small agency (2-3 devs)** | £1,500/day | 16 weeks | **£120,000** |
| **Enterprise consultancy** | £2,500/day | 20 weeks | **£250,000** |

---

## Actual Cost With Claude Code

### Development Time

| Item | Actual |
|------|--------|
| Calendar days | 13 |
| Estimated developer hours | ~80–100 hours |
| Effective working days | ~10–12 |

### Claude Code Costs

| Item | Estimate |
|------|----------|
| Claude Code subscription | $200/month (Max plan) |
| Token usage (317 commits, extensive subagent use) | Included in Max plan |
| **Total AI tooling cost** | **~$200** |

### Developer Time Cost

| Scenario | Rate | Hours | Cost |
|----------|------|-------|------|
| Developer (UK) | £50/hour | 100 hours | £5,000 |
| Developer (US) | $75/hour | 100 hours | $7,500 |

### Total Actual Cost

| Item | UK (£) | US ($) |
|------|--------|--------|
| Developer time | £5,000 | $7,500 |
| Claude Code (1 month) | £160 | $200 |
| Render hosting (free tier) | £0 | $0 |
| Neon PostgreSQL (free tier) | £0 | $0 |
| Cloudflare (free tier) | £0 | $0 |
| Stripe (transaction fees only) | £0 | $0 |
| Zoho Mail (£24/year) | £2 | $3 |
| **Total** | **~£5,162** | **~$7,703** |

---

## ROI Summary

| Metric | Traditional | With Claude Code | Savings |
|--------|------------|-----------------|---------|
| **Duration** | ~8 months | 13 days | **95% faster** |
| **Developer cost (UK)** | £96,000 | £5,000 | **95% cheaper** |
| **Developer cost (US)** | $128,000 | $7,500 | **94% cheaper** |
| **Lines of code** | Same | Same | — |
| **Test coverage** | Same | Same | — |
| **Documentation** | Often skipped | 71 docs, 24 PDFs | Comprehensive |

### Key Observations

1. **13 days vs 8 months** — Claude Code handled architecture decisions, code generation, testing, documentation, and deployment configuration. The developer focused on requirements, review, and domain decisions.

2. **Documentation was free** — In traditional development, documentation is often deprioritised or skipped. With Claude Code, 71 markdown files, 12 design specs, and 24 PDF/Word documents were generated as a natural part of the workflow.

3. **Subagent parallelism** — Multiple implementation tasks ran in parallel via Claude Code subagents, reducing wall-clock time further.

4. **Quality maintained** — 210 unit tests, structured architecture (Vertical Slice, CQRS, signal-first Angular), and comprehensive error handling — not "quick and dirty" AI code.

5. **Infrastructure cost near zero** — All hosting services on free tiers during pilot. Production costs start at ~$130/month at 10 customers.

---

## Disclaimer

These estimates assume a competent developer who can review AI-generated code, make architectural decisions, and provide clear requirements. Claude Code accelerates implementation — it does not eliminate the need for a skilled developer to guide the process.
