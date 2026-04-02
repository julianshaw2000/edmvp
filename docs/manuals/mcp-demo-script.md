# auditraks MCP Demo Script

**Purpose:** Demonstrate the auditraks MCP integration to prospects, showing how AI assistants can query and manage the compliance platform via natural language.

**Setup:** Both `auditraks-admin` and `auditraks` MCP servers connected in Claude Code.

**Estimated time:** 10–15 minutes

---

## Opening Statement

> "auditraks includes MCP integration — that's Model Context Protocol. It means you can connect Claude or any AI assistant directly to the platform and manage your entire supply chain compliance workflow through natural conversation. Let me show you."

---

## Act 1: Platform Overview (Admin MCP) — 2 min

Paste this prompt:

```
I'm the platform admin for auditraks, a mineral supply chain compliance platform.

Using the auditraks-admin MCP tools, give me a complete overview of the platform right now:
1. List all tenants and their status
2. Show me the platform analytics — how many batches, users, and compliance flags do we have?
3. Are there any failed background jobs I should know about?

Summarise everything in a clean dashboard-style format.
```

---

## Act 2: Compliance Deep Dive (Customer MCP) — 3 min

Paste this prompt:

```
Using the auditraks MCP tools, I want to review our batch compliance:

1. List all our batches
2. For any batch that is FLAGGED, get the full compliance details — tell me exactly which checks failed and why
3. For any batch that is COMPLIANT, verify the hash chain integrity to confirm the data hasn't been tampered with
4. Search the RMAP smelter database for "Wolfram Bergbau" and tell me their conformance status

Present this as a compliance officer's briefing — clear, actionable, with risk flags highlighted.
```

---

## Act 3: Supply Chain Traceability (Customer MCP) — 3 min

Paste this prompt:

```
Using the auditraks MCP tools, trace the complete custody journey for our most complete batch:

1. List all batches and find the one with the most events
2. Get all custody events for that batch, in chronological order
3. Get the compliance status
4. Get the batch activity log to see who has been working on it

Tell the story of this batch — where did it start, who handled it at each stage, where did it end up, and is it compliant? Format it as a narrative that a buyer would want to read.
```

---

## Act 4: Buyer Workflow (Customer MCP) — 3 min

Paste this prompt:

```
I'm a buyer monitoring my supply chain. Using the auditraks MCP tools:

1. Show me my supplier engagement metrics — how many suppliers are active vs stale vs flagged?
2. For any stale suppliers, send them a reminder to update their data
3. Check if our compliant batch is ready for Form SD filing
4. Generate a risk assessment for that batch
5. List our Form SD filing cycles

Give me an executive summary of our supply chain health and regulatory readiness.
```

---

## Act 5: Document Generation (Customer MCP) — 2 min

Paste this prompt:

```
Using the auditraks MCP tools, I need to prepare documentation for an upcoming audit:

1. Find our COMPLIANT batch
2. Generate a Material Passport for it
3. Create a shareable link for the passport (valid for 30 days)
4. List all generated documents for this batch

Tell me what documents are ready and give me the share link I can send to the auditor.
```

---

## Act 6: AI-Powered Insights (Admin MCP) — 2 min

Paste this prompt:

```
Using the auditraks-admin MCP tools, I want AI-powered insights on our platform:

1. Run a churn prediction — which tenants are at risk?
2. Check the health scores for all tenants
3. Give me a revenue summary
4. Ask the platform: "How many custody events were logged in the last 30 days?"

Present this as a board-level summary with key metrics and action items.
```

---

## Act 7: Audit Trail (Admin MCP) — 2 min

Paste this prompt:

```
Using the auditraks-admin MCP tools, I need to review platform activity for compliance purposes:

1. Show me the most recent audit logs — who did what, when?
2. List all users across the platform and their roles
3. Search for any smelter with "Xiamen" in the name and check their conformance status

Format this as a compliance audit report.
```

---

## Closing Statement

> "Everything you just saw — querying batches, checking compliance, generating passports, monitoring suppliers, running AI analytics — all done through natural language. No clicking through dashboards, no learning a new UI. Your team can manage compliance the same way they'd ask a colleague for help. That's the power of MCP integration."

---

## Quick Single Prompts (for ad-hoc demos)

If you don't have time for the full script, use any of these standalone prompts:

**Impressive one-liner (admin):**
```
Using auditraks-admin tools, give me a full platform health check — tenants, analytics, any failing jobs, and churn risk. Format as an executive dashboard.
```

**Impressive one-liner (customer):**
```
Using auditraks tools, find all flagged batches, explain why they're flagged, search for the relevant smelters in the RMAP database, and suggest what needs to be fixed. Be specific.
```

**Supply chain story:**
```
Using auditraks tools, pick the batch with the most events and tell me its complete story from mine to market — every custody event, every actor, every location, and whether it passed all compliance checks.
```

**Buyer intelligence:**
```
Using auditraks tools, give me a supplier engagement report — who's active, who's gone quiet, who has compliance issues — and send reminders to anyone who needs a nudge.
```

**Document prep:**
```
Using auditraks tools, I have an audit next week. For every compliant batch, generate a Material Passport and create shareable links. Give me a table of batch numbers and their share URLs.
```
