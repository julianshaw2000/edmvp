# auditraks MCP Servers

Two MCP (Model Context Protocol) servers that give AI assistants access to the auditraks platform.

## Customer MCP (`auditraks-mcp`)

For customers connecting AI assistants to their auditraks tenant via API key.

**24 tools:** batches (5), events (3), compliance (2), documents (4), smelters (1), engagement (3), Form SD (1)

### Setup

Generate an API key in the auditraks Admin Dashboard → API Keys.

### Configuration (Claude Desktop / Claude Code)

```json
{
  "mcpServers": {
    "auditraks": {
      "command": "node",
      "args": ["packages/mcp/customer-server/dist/index.js"],
      "env": {
        "AUDITRAKS_API_KEY": "at_your_api_key",
        "AUDITRAKS_API_URL": "https://accutrac-api.onrender.com"
      }
    }
  }
}
```

### Available Tools

| Tool | Description |
|------|-------------|
| `list_batches` | List mineral batches with pagination |
| `get_batch` | Get batch details by ID |
| `create_batch` | Create a new mineral batch |
| `get_batch_activity` | Get activity log for a batch |
| `verify_batch_integrity` | Verify SHA-256 hash chain integrity |
| `list_events` | List custody events for a batch |
| `get_event` | Get custody event details |
| `create_event` | Log a custody event on a batch |
| `get_batch_compliance` | Get compliance status for a batch |
| `get_event_compliance` | Get compliance checks for an event |
| `list_batch_documents` | List documents for a batch |
| `generate_passport` | Generate Material Passport PDF |
| `share_document` | Create a 30-day shareable link |
| `share_document_email` | Email a document to a recipient |
| `search_smelters` | Search the RMAP smelter database |
| `get_supplier_engagement` | Supplier engagement metrics (buyer) |
| `nudge_supplier` | Send reminder to supplier (buyer) |
| `list_filing_cycles` | List Form SD filing cycles |

---

## Admin MCP (`auditraks-mcp-admin`)

For platform administrators managing tenants, users, and analytics.

**15 tools:** tenants (4), users (4), analytics (1), audit (1), RMAP (2), batches (3)

### Configuration

```json
{
  "mcpServers": {
    "auditraks-admin": {
      "command": "node",
      "args": ["packages/mcp/admin-server/dist/index.js"],
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

| Tool | Description |
|------|-------------|
| `list_tenants` | List all tenants on the platform |
| `create_tenant` | Create a new tenant organisation |
| `update_tenant_status` | Activate, suspend, or trial a tenant |
| `delete_tenant` | Delete a tenant (irreversible) |
| `list_users` | List users (optional tenant filter) |
| `create_user` | Create/invite a user |
| `update_user` | Update user role or status |
| `delete_user` | Delete a user |
| `get_analytics` | Platform-wide analytics |
| `list_audit_logs` | Search audit logs with filters |
| `list_rmap_smelters` | List all RMAP smelters |
| `search_smelters` | Search smelters by name or ID |
| `list_batches` | List batches (cross-tenant) |
| `get_batch` | Get batch details |
| `get_batch_compliance` | Get compliance status |

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

**Customer (buyer):**
- "Which suppliers are stale?"
- "Send a reminder to the inactive supplier"
- "What are the current Form SD filing cycles?"

**Admin:**
- "List all tenants and their status"
- "Show audit logs from today"
- "How many flagged batches are there across the platform?"
- "Create a new tenant called Acme Mining Corp"
