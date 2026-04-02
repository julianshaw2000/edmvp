# auditraks MCP Servers

Two MCP (Model Context Protocol) servers that give AI assistants access to the auditraks platform.

## Customer MCP (`auditraks-mcp`)

For customers connecting AI assistants to their auditraks tenant via API key.

**32 tools:** batches (7), events (4), compliance (2), documents (7), smelters (1), engagement (8), notifications (3)

### Setup

Generate an API key in the auditraks Admin Dashboard → API Keys.

### Configuration (Claude Desktop / Claude Code)

```json
{
  "mcpServers": {
    "auditraks": {
      "command": "node",
      "args": ["packages/mcp/customer-server/dist/customer-server/src/index.js"],
      "env": {
        "AUDITRAKS_API_KEY": "at_your_api_key",
        "AUDITRAKS_API_URL": "https://accutrac-api.onrender.com"
      }
    }
  }
}
```

### Available Tools

**Batches (7)**

| Tool | Description |
|------|-------------|
| `list_batches` | List mineral batches with pagination |
| `get_batch` | Get batch details by ID |
| `create_batch` | Create a new mineral batch |
| `get_batch_activity` | Get activity log for a batch |
| `verify_batch_integrity` | Verify SHA-256 hash chain integrity |
| `update_batch_status` | Update batch status (e.g. ACTIVE, COMPLETED) |
| `split_batch` | Split a batch into sub-batches |

**Events (4)**

| Tool | Description |
|------|-------------|
| `list_events` | List custody events for a batch |
| `get_event` | Get custody event details |
| `create_event` | Log a custody event on a batch |
| `create_correction` | Submit a correction for a custody event |

**Compliance (2)**

| Tool | Description |
|------|-------------|
| `get_batch_compliance` | Get compliance status for a batch |
| `get_event_compliance` | Get compliance checks for an event |

**Documents (7)**

| Tool | Description |
|------|-------------|
| `list_batch_documents` | List documents for a batch |
| `generate_passport` | Generate Material Passport PDF |
| `generate_dossier` | Generate Audit Dossier PDF (buyer) |
| `generate_dpp` | Generate Digital Product Passport JSON-LD (buyer) |
| `list_generated_documents` | List all generated documents for a batch |
| `share_document` | Create a 30-day shareable link |
| `share_document_email` | Email a document to a recipient |

**Smelters (1)**

| Tool | Description |
|------|-------------|
| `search_smelters` | Search the RMAP smelter database |

**Engagement & Form SD (8)**

| Tool | Description |
|------|-------------|
| `get_supplier_engagement` | Supplier engagement metrics (buyer) |
| `nudge_supplier` | Send reminder to supplier (buyer) |
| `list_filing_cycles` | List Form SD filing cycles |
| `get_form_sd_status` | Check batch Form SD readiness (buyer) |
| `get_supply_chain_description` | AI supply chain narrative (buyer) |
| `get_due_diligence_summary` | AI due diligence summary (buyer) |
| `get_risk_assessment` | AI risk assessment (buyer) |
| `generate_form_sd_package` | Generate Form SD support package (buyer) |

**Notifications (3)**

| Tool | Description |
|------|-------------|
| `list_notifications` | List your notifications |
| `mark_notification_read` | Mark a notification as read |
| `verify_batch_public` | Publicly verify a batch |

---

## Admin MCP (`auditraks-mcp-admin`)

For platform administrators managing tenants, users, and analytics.

**23 tools:** tenants (4), users (4), analytics (1), audit (2), RMAP (2), batches (5), jobs (1), AI insights (4)

### Configuration

```json
{
  "mcpServers": {
    "auditraks-admin": {
      "command": "node",
      "args": ["packages/mcp/admin-server/dist/admin-server/src/index.js"],
      "env": {
        "AUDITRAKS_EMAIL": "your@email.com",
        "AUDITRAKS_PASSWORD": "your_password",
        "AUDITRAKS_API_URL": "https://accutrac-api.onrender.com"
      }
    }
  }
}
```

### Available Tools

**Tenants (4)**

| Tool | Description |
|------|-------------|
| `list_tenants` | List all tenants on the platform |
| `create_tenant` | Create a new tenant organisation |
| `update_tenant_status` | Activate, suspend, or trial a tenant |
| `delete_tenant` | Delete a tenant (irreversible) |

**Users (4)**

| Tool | Description |
|------|-------------|
| `list_users` | List users (optional tenant filter) |
| `create_user` | Create/invite a user |
| `update_user` | Update user role or status |
| `delete_user` | Delete a user |

**Analytics (1)**

| Tool | Description |
|------|-------------|
| `get_analytics` | Platform-wide analytics (optional tenant filter) |

**Audit (2)**

| Tool | Description |
|------|-------------|
| `list_audit_logs` | Search audit logs with filters |
| `export_audit_logs` | Export audit logs as CSV |

**RMAP (2)**

| Tool | Description |
|------|-------------|
| `list_rmap_smelters` | List all RMAP smelters |
| `search_smelters` | Search smelters by name or ID |

**Batches (5)**

| Tool | Description |
|------|-------------|
| `list_batches` | List batches (cross-tenant) |
| `get_batch` | Get batch details |
| `get_batch_compliance` | Get compliance status |
| `get_batch_activity` | Get batch activity log |
| `list_events` | List custody events for a batch |

**Jobs (1)**

| Tool | Description |
|------|-------------|
| `list_jobs` | List background jobs and their status |

**AI Insights (4)**

| Tool | Description |
|------|-------------|
| `churn_prediction` | Get churn risk analysis for tenants |
| `tenant_health` | Get health scores for all tenants |
| `revenue_summary` | Get revenue breakdown and analysis |
| `natural_language_query` | Ask a natural language question about platform data |

---

## Building

```bash
cd packages/mcp/shared && npx tsc
cd ../customer-server && npm install && npx tsc
cd ../admin-server && npm install && npx tsc
```

## Example Prompts

**Customer (supplier):**
- "Show me all my batches"
- "What's the compliance status of batch W-2026-041?"
- "Search for smelter Wolfram Bergbau"
- "Generate a Material Passport for batch W-2026-041"
- "Do I have any unread notifications?"
- "Correct the location on event X"

**Customer (buyer):**
- "Which suppliers are stale?"
- "Send a reminder to the inactive supplier"
- "Is batch W-2026-041 ready for Form SD?"
- "Generate a risk assessment for this batch"
- "Generate the 2026 Form SD package"
- "Generate an audit dossier for batch W-2026-041"

**Admin:**
- "List all tenants and their status"
- "Show audit logs from today"
- "Export this month's audit logs"
- "Are there any failed background jobs?"
- "Which tenants are at risk of churning?"
- "What's the revenue summary?"
- "How many batches were created last month?"
