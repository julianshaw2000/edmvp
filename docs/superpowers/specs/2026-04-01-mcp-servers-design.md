# MCP Servers for auditraks — Design Spec

**Date:** 2026-04-01
**Status:** Approved

---

## Overview

Two MCP (Model Context Protocol) servers that give AI assistants access to the auditraks platform:

1. **Customer MCP Server** — for customers connecting their AI assistants to their own tenant data via API key
2. **Admin MCP Server** — for platform administrators managing tenants, RMAP data, analytics, and audit logs

Both share a common API client library. Each exposes a curated set of tools appropriate to its audience.

---

## Architecture

```
packages/mcp/
├── shared/
│   ├── api-client.ts        ← HTTP client wrapping auditraks REST API
│   ├── types.ts              ← Shared response types
│   └── auth.ts               ← API key and JWT auth helpers
├── customer-server/
│   ├── index.ts              ← MCP server entry point
│   ├── tools/                ← Tool definitions (one file per feature area)
│   │   ├── batches.ts
│   │   ├── events.ts
│   │   ├── compliance.ts
│   │   ├── documents.ts
│   │   ├── smelters.ts
│   │   └── engagement.ts
│   └── package.json
└── admin-server/
    ├── index.ts              ← MCP server entry point
    ├── tools/
    │   ├── tenants.ts
    │   ├── users.ts
    │   ├── analytics.ts
    │   ├── audit.ts
    │   ├── rmap.ts
    │   ├── jobs.ts
    │   └── batches.ts
    └── package.json
```

**Tech stack:** TypeScript, `@modelcontextprotocol/sdk`, `node-fetch` or built-in fetch

**Auth:**
- Customer MCP: API key passed via `X-API-Key` header (existing API key system)
- Admin MCP: JWT access token obtained via `POST /api/auth/login` with email/password, refreshed automatically

---

## Customer MCP Server — Tools

### Batches (6 tools)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `list_batches` | `GET /api/batches` | List batches with optional pagination |
| `get_batch` | `GET /api/batches/{id}` | Get batch details by ID |
| `get_batch_activity` | `GET /api/batches/{id}/activity` | Get batch activity log |
| `create_batch` | `POST /api/batches` | Create a new batch |
| `update_batch_status` | `PATCH /api/batches/{id}/status` | Update batch status |
| `verify_batch_integrity` | `GET /api/batches/{id}/verify-integrity` | Verify hash chain integrity |

### Custody Events (4 tools)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `list_events` | `GET /api/batches/{batchId}/events` | List events for a batch |
| `get_event` | `GET /api/events/{id}` | Get event details |
| `create_event` | `POST /api/batches/{batchId}/events` | Log a custody event |
| `create_correction` | `POST /api/events/{id}/corrections` | Submit event correction |

### Compliance (2 tools)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `get_batch_compliance` | `GET /api/batches/{id}/compliance` | Get compliance status |
| `get_event_compliance` | `GET /api/events/{id}/compliance` | Get event compliance checks |

### Documents (4 tools)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `list_batch_documents` | `GET /api/batches/{id}/documents` | List documents for a batch |
| `generate_passport` | `POST /api/batches/{id}/passport` | Generate Material Passport |
| `share_document` | `POST /api/generated-documents/{id}/share` | Create share link |
| `share_document_email` | `POST /api/generated-documents/{id}/share-email` | Email document to recipient |

### Smelters (1 tool)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `search_smelters` | `GET /api/smelters` | Search RMAP smelter database |

### Supplier Engagement (3 tools — buyer role)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `get_supplier_engagement` | `GET /api/buyer/supplier-engagement` | Supplier activity metrics |
| `nudge_supplier` | `POST /api/buyer/nudge-supplier` | Send reminder to supplier |
| `list_cmrt_imports` | `GET /api/buyer/cmrt-imports` | List CMRT import history |

### Form SD (4 tools — buyer role)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `get_form_sd_status` | `GET /api/form-sd/batches/{id}/status` | Form SD readiness |
| `get_supply_chain_description` | `GET /api/form-sd/batches/{id}/supply-chain` | AI supply chain narrative |
| `get_risk_assessment` | `GET /api/form-sd/batches/{id}/risk-assessment` | AI risk assessment |
| `list_filing_cycles` | `GET /api/form-sd/filing-cycles` | List filing cycles |

**Total: 24 customer tools**

---

## Admin MCP Server — Tools

### Tenants (4 tools)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `list_tenants` | `GET /api/platform/tenants` | List all tenants |
| `create_tenant` | `POST /api/platform/tenants` | Create a tenant |
| `update_tenant_status` | `PATCH /api/platform/tenants/{id}/status` | Activate/suspend tenant |
| `delete_tenant` | `DELETE /api/platform/tenants/{id}` | Delete tenant |

### Users (4 tools)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `list_users` | `GET /api/users` | List users (optional tenant filter) |
| `create_user` | `POST /api/users` | Create/invite user |
| `update_user` | `PATCH /api/users/{id}` | Update user role/status |
| `delete_user` | `DELETE /api/users/{id}` | Delete user |

### Analytics (1 tool)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `get_analytics` | `GET /api/analytics` | Platform-wide analytics |

### Audit (2 tools)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `list_audit_logs` | `GET /api/admin/audit-logs` | Search audit logs with filters |
| `export_audit_logs` | `GET /api/admin/audit-logs/export` | Export audit logs as CSV |

### RMAP (2 tools)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `list_rmap_smelters` | `GET /api/admin/rmap` | List RMAP smelter database |
| `search_smelters` | `GET /api/smelters` | Search smelters by name/ID |

### Jobs (1 tool)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `list_jobs` | `GET /api/admin/jobs` | List background jobs |

### Batches (read-only, cross-tenant)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `list_batches` | `GET /api/batches` | List batches (cross-tenant for admin) |
| `get_batch` | `GET /api/batches/{id}` | Get batch details |
| `get_batch_compliance` | `GET /api/batches/{id}/compliance` | Get compliance status |

### AI Insights (5 tools)

| Tool | API Endpoint | Description |
|------|-------------|-------------|
| `churn_prediction` | `GET /api/ai/churn-prediction` | Churn risk analysis |
| `tenant_health` | `GET /api/ai/tenant-health` | Tenant health scores |
| `revenue_summary` | `GET /api/ai/revenue-summary` | Revenue analysis |
| `usage_coaching` | `GET /api/ai/usage-coaching` | Usage recommendations |
| `regulatory_updates` | `GET /api/ai/regulatory-updates` | Regulatory monitor |

**Total: 22 admin tools**

---

## Authentication

### Customer MCP

Configuration via environment variables or MCP settings:

```json
{
  "mcpServers": {
    "auditraks": {
      "command": "npx",
      "args": ["auditraks-mcp"],
      "env": {
        "AUDITRAKS_API_KEY": "at_your_api_key_here",
        "AUDITRAKS_API_URL": "https://accutrac-api.onrender.com"
      }
    }
  }
}
```

The API key determines which tenant and role the tools operate as. All tool calls include `X-API-Key: {key}` header. The existing API key system already enforces role-based access — no additional auth logic needed in the MCP server.

### Admin MCP

Configuration with email/password:

```json
{
  "mcpServers": {
    "auditraks-admin": {
      "command": "npx",
      "args": ["auditraks-mcp-admin"],
      "env": {
        "AUDITRAKS_EMAIL": "julianshaw2000@gmail.com",
        "AUDITRAKS_PASSWORD": "your_password",
        "AUDITRAKS_API_URL": "https://accutrac-api.onrender.com"
      }
    }
  }
}
```

The admin server authenticates via `POST /api/auth/login` on startup, stores the JWT, and auto-refreshes via `POST /api/auth/refresh` when it expires.

---

## Tool Design Principles

1. **Read-heavy, write-cautious** — read tools return data freely; write tools (create, update, delete) require explicit confirmation parameters
2. **Structured output** — all tools return JSON, not HTML or raw text
3. **Error handling** — API errors returned as structured error objects with status code and message
4. **Pagination** — list tools accept `page` and `pageSize` parameters, return `totalCount` for context
5. **No file uploads** — tools that require file upload (document upload, CMRT import, RMAP upload) are excluded from MCP; use the web UI for those

---

## What We Do Not Build

- File upload tools (CMRT import, document upload, RMAP upload) — binary data over MCP is unreliable
- Webhook management — admin-only, rarely used, better via web UI
- Billing/Stripe tools — sensitive operations, web UI only
- Signup/onboarding tools — one-time operations, web UI only
- Notification management — low value for AI assistant use case

---

## Distribution

- **Customer MCP:** Published as `auditraks-mcp` npm package — customers install via `npx auditraks-mcp`
- **Admin MCP:** Published as `auditraks-mcp-admin` npm package — internal use
- Both runnable via `npx` without global install

---

## Success Criteria

1. Claude (or any MCP client) can list batches, check compliance, and search smelters via natural language
2. Customer MCP authenticates with existing API keys — no new auth system
3. Admin MCP provides cross-tenant visibility for platform management
4. Write operations require explicit parameters (no accidental mutations)
5. Both servers start in under 2 seconds
6. Tools return structured JSON that AI assistants can reason about
