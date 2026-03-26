# AI-Assisted Features — Implementation Roadmap

**Date:** 2026-03-26
**Status:** Planned
**Prerequisite:** Platform stable, 25 phases complete

---

## Overview

17 AI-powered features organized into 4 implementation waves by complexity and business impact. Each wave builds on the previous one. Claude API (Anthropic SDK) is the recommended AI provider — same vendor as the development tools.

---

## Wave 1: Quick Wins (1-2 weeks each)

Low complexity, high impact. Can be built with existing infrastructure + Claude API calls.

### 1.1 Natural Language Audit Log Queries
**What:** Admin types "Show me all failed compliance checks for DRC batches this quarter" and gets filtered results.
**How:**
- New endpoint: `POST /api/admin/audit-logs/query` accepting natural language
- Send the query to Claude API with the audit log schema as context
- Claude returns structured filters (action, entityType, from, to, etc.)
- Apply filters to existing ListAuditLogs query
- Frontend: add a search bar above the audit log table
**Dependencies:** Claude API key, `Anthropic` NuGet package
**Effort:** 3-4 days

### 1.2 AI Compliance Summary Reports
**What:** "Generate a compliance summary for Q1 2026" produces a formatted report.
**How:**
- New endpoint: `POST /api/admin/reports/compliance-summary`
- Gather: batch counts by status, flagged batches with reasons, compliance check pass/fail rates
- Send structured data to Claude API with prompt: "Generate a professional compliance summary report"
- Return markdown/HTML report
- Frontend: "Generate Report" button on analytics page
**Dependencies:** Claude API
**Effort:** 3-4 days

### 1.3 AI Chatbot / Help Assistant
**What:** Floating chat widget that answers questions about the platform.
**How:**
- Embed the user manual and tenant admin manual as context
- New endpoint: `POST /api/assistant/chat` accepting user message + conversation history
- Claude responds with contextual help
- Frontend: floating chat bubble component (bottom-right corner)
- Rate limit: 20 messages/hour per user
**Dependencies:** Claude API
**Effort:** 4-5 days

### 1.4 Smart Onboarding Assistant
**What:** Context-aware guidance during first batch creation.
**How:**
- Extend the existing onboarding wizard with AI-powered tips
- When user creates first batch, AI suggests: mineral type based on company name, common event sequences
- Tooltip hints powered by Claude
**Dependencies:** Claude API
**Effort:** 2-3 days

---

## Wave 2: Compliance Intelligence (2-4 weeks each)

Medium complexity. Requires data analysis patterns and background processing.

### 2.1 Anomaly Detection
**What:** Flag unusual patterns — sudden weight changes, unexpected origins, irregular timing.
**How:**
- Background job runs daily (add to existing Worker service)
- For each tenant, analyze recent batches against historical patterns:
  - Weight variance > 2 standard deviations from tenant average
  - New origin country not seen before
  - Event sequence gaps (e.g., export without smelting)
  - Unusually fast custody chain (< 24h mine to export)
- Store anomalies in new `AnomalyEntity` table
- Notify tenant admin via email + dashboard badge
- Dashboard: new "Anomalies" card showing flagged items
**Dependencies:** New DB table + migration, Worker job, email templates
**Effort:** 2 weeks

### 2.2 Enhanced Sanctions Screening (Fuzzy Matching)
**What:** Catch spelling variations, aliases, and transliterations when screening against sanctions lists.
**How:**
- Replace exact string match in `SanctionsChecker` with fuzzy matching
- Use Levenshtein distance + phonetic matching (Soundex/Metaphone)
- Threshold: similarity > 85% triggers a flag with "Possible match" (not auto-fail)
- AI review: send potential matches to Claude for contextual analysis ("Is 'Nyungwe Mining Coop' the same as 'Nyungwe Mining Cooperative Ltd'?")
- Admin can review and dismiss false positives
**Dependencies:** Fuzzy matching library (`FuzzySharp` NuGet), Claude API for review
**Effort:** 1-2 weeks

### 2.3 Document Verification (OCR + AI)
**What:** AI reads uploaded assay certificates and mining permits, extracts key data, flags inconsistencies.
**How:**
- When a document is uploaded, send it to Claude's vision API (PDF/image → text extraction)
- Claude extracts: mineral type, weight, origin, date, certificate number
- Compare extracted data against the batch record
- Flag mismatches: "Certificate says 450kg but batch weight is 500kg"
- Store verification results on the document record
**Dependencies:** Claude vision API, document storage (R2 already in place)
**Effort:** 2-3 weeks

### 2.4 Predictive Risk Scoring
**What:** Each batch gets an AI-generated risk score (1-100) based on origin, actors, route, timing.
**How:**
- After compliance checks run, send batch data to Claude with risk assessment prompt
- Factors: origin country risk level, smelter conformance status, actor history, transport route, time gaps
- Score stored on batch record: new `RiskScore` nullable int field
- Dashboard: risk score badge on batch cards, sortable
- High-risk batches (>75) get automatic email notification to tenant admin
**Dependencies:** New field + migration, Claude API, email notification
**Effort:** 2 weeks

---

## Wave 3: Analytics & Intelligence (3-6 weeks each)

Higher complexity. Requires data aggregation, visualization, and potentially ML models.

### 3.1 Supply Chain Mapping
**What:** Visual map showing custody chain from mine to refinery with risk highlighting.
**How:**
- Frontend: interactive map component using Leaflet.js or Mapbox
- Plot GPS coordinates from custody events on a world map
- Connect events with lines showing the custody flow
- Color-code by risk: green (low), amber (medium), red (high)
- Click on a node to see event details
- Filter by mineral type, origin country, date range
**Dependencies:** Mapping library (Leaflet is free), GPS data already captured on events
**Effort:** 3-4 weeks

### 3.2 Churn Prediction
**What:** Identify tenants likely to cancel based on usage patterns.
**How:**
- Background job analyzes per-tenant metrics weekly:
  - Login frequency (declining = risk)
  - Batch creation rate (zero for 2+ weeks = risk)
  - Feature adoption (never used passports = risk)
  - Support tickets / errors (high = frustration)
- Assign churn risk score: Low/Medium/High
- Platform admin dashboard: "At Risk" tenant list
- Trigger automated "How can we help?" email for Medium/High risk
**Dependencies:** New metrics tracking, background job, email templates
**Effort:** 3 weeks

### 3.3 Regulatory Change Monitoring
**What:** Alert when RMAP, OECD, or Dodd-Frank requirements change.
**How:**
- Weekly background job scrapes key regulatory sources:
  - responsiblemineralsinitiative.org (RMAP updates)
  - oecd.org (DDG updates)
  - sec.gov (Dodd-Frank updates)
  - trade.ec.europa.eu (EU 2017/821)
- Send scraped content to Claude: "Has anything changed that affects 3TG compliance?"
- If changes detected, notify platform admin via email with summary
- Admin reviews and decides whether to update compliance rules
**Dependencies:** Web scraping (HttpClient), Claude API, email
**Effort:** 2 weeks

### 3.4 Usage Analytics & Pricing Optimization
**What:** Analyze usage patterns to recommend plan tier adjustments.
**How:**
- Track per-tenant metrics: batches/month, users, API calls, documents generated
- Dashboard for platform admin: usage heatmap, tenant comparison
- AI-powered recommendations: "Tenant X is at 45/50 batches — suggest Pro upgrade"
- Automated upgrade suggestion email to tenant admin when approaching limits
**Dependencies:** Usage tracking (extend audit log or new metrics table), Claude API
**Effort:** 3 weeks

---

## Wave 4: Advanced AI (6+ weeks each)

High complexity. Long-term roadmap items.

### 4.1 AI-Powered Incident Reports
**What:** Automatically generate compliance incident summaries from flagged batches for auditors.
**How:**
- When a batch is flagged, gather: batch details, all events, all compliance check results, documents
- Send to Claude: "Generate a formal compliance incident report suitable for an auditor"
- Output: structured PDF report with findings, evidence, recommendations
- Auditor can download directly from the batch detail page
**Dependencies:** Claude API, QuestPDF (already in use), complex prompt engineering
**Effort:** 4-6 weeks

### 4.2 Natural Language Supply Chain Queries
**What:** "Which batches from Rwanda were processed by non-RMAP smelters in Q1?"
**How:**
- Text-to-SQL: Claude translates natural language to database queries
- Sandboxed execution against read-only database replica
- Results displayed in a table with export option
- Safety: only SELECT queries allowed, tenant-scoped
**Dependencies:** Read replica database, Claude API, query sandbox
**Effort:** 6+ weeks

---

## Technical Architecture

### AI Provider
- **Claude API** (Anthropic) — recommended
- Model: Claude Sonnet for speed, Claude Opus for complex analysis
- SDK: `Anthropic` NuGet package for .NET

### New Infrastructure
- `Anthropic__ApiKey` environment variable on Render
- `IAiService` interface for dependency injection (swap providers easily)
- Rate limiting: per-tenant AI request limits to control costs
- Caching: cache AI responses for identical queries (HybridCache)

### Cost Estimation
| Wave | Monthly AI Cost (est.) | Infrastructure |
|------|----------------------|----------------|
| Wave 1 | $50-100 | Claude API only |
| Wave 2 | $100-300 | Claude API + background jobs |
| Wave 3 | $200-500 | Claude API + mapping library + scraping |
| Wave 4 | $300-800 | Claude API + read replica |

Costs scale with tenant count and usage. Per-tenant AI limits prevent runaway costs.

### Environment Variables
```
Anthropic__ApiKey=sk-ant-...
Anthropic__Model=claude-sonnet-4-20250514
Anthropic__MaxTokensPerRequest=4096
```

---

## Implementation Priority

| # | Feature | Wave | Effort | Business Impact |
|---|---------|------|--------|-----------------|
| 1 | Natural language audit queries | 1 | 3-4 days | High — immediate admin productivity |
| 2 | AI compliance reports | 1 | 3-4 days | High — replaces manual reporting |
| 3 | AI chatbot | 1 | 4-5 days | Medium — reduces support load |
| 4 | Anomaly detection | 2 | 2 weeks | High — proactive compliance |
| 5 | Fuzzy sanctions matching | 2 | 1-2 weeks | High — catches missed risks |
| 6 | Predictive risk scoring | 2 | 2 weeks | High — differentiator |
| 7 | Document verification | 2 | 2-3 weeks | Medium — reduces manual work |
| 8 | Smart onboarding | 1 | 2-3 days | Medium — reduces churn |
| 9 | Supply chain mapping | 3 | 3-4 weeks | High — visual differentiator |
| 10 | Churn prediction | 3 | 3 weeks | Medium — revenue retention |
| 11 | Regulatory monitoring | 3 | 2 weeks | Medium — proactive compliance |
| 12 | Pricing optimization | 3 | 3 weeks | Medium — revenue growth |
| 13 | AI incident reports | 4 | 4-6 weeks | High — auditor-ready output |
| 14 | NL supply chain queries | 4 | 6+ weeks | Medium — power users |

---

## Getting Started

To begin Wave 1:
1. Get a Claude API key from console.anthropic.com
2. Add `Anthropic__ApiKey` to Render environment variables
3. Install `Anthropic` NuGet package
4. Create `IAiService` interface and `ClaudeAiService` implementation
5. Start with feature 1.1 (natural language audit queries) — highest impact, lowest effort
