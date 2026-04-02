# MCP Additional Tools — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add 20 additional tools across both MCP servers — 14 new customer tools and 6 new admin tools.

**Architecture:** Extend existing tool files and add new tool files in the same structure. No new services or shared code needed — just more tool registrations calling existing API endpoints.

**Tech Stack:** TypeScript, `@modelcontextprotocol/sdk`, `zod`

---

## Chunk 1: Customer MCP — Additional Tools

### Task 1: Add batch management tools (update status, split)

**Files:**
- Modify: `packages/mcp/customer-server/src/tools/batches.ts`

- [ ] **Step 1: Add tools to existing batches.ts**

Read `packages/mcp/customer-server/src/tools/batches.ts` and add these tools after the existing ones:

```typescript
  server.tool('update_batch_status', 'Update the status of a batch', {
    batchId: z.string().describe('Batch ID (UUID)'),
    status: z.string().describe('New status (e.g. ACTIVE, COMPLETED)'),
  }, async ({ batchId, status }) => {
    const data = await api.patch(`/api/batches/${batchId}/status`, { status });
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('split_batch', 'Split a batch into sub-batches', {
    batchId: z.string().describe('Batch ID (UUID)'),
    splits: z.array(z.object({
      batchNumber: z.string().describe('New batch number'),
      weightKg: z.number().describe('Weight for this split'),
    })).describe('Array of splits with batch numbers and weights'),
  }, async ({ batchId, splits }) => {
    const data = await api.post(`/api/batches/${batchId}/split`, { splits });
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
```

- [ ] **Step 2: Build and commit**

```bash
cd packages/mcp/customer-server && npx tsc
git add packages/mcp/customer-server/
git commit -m "feat: add batch update status and split tools to customer MCP

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Add event correction tool

**Files:**
- Modify: `packages/mcp/customer-server/src/tools/events.ts`

- [ ] **Step 1: Add correction tool**

Read `packages/mcp/customer-server/src/tools/events.ts` and add:

```typescript
  server.tool('create_correction', 'Submit a correction for a custody event', {
    eventId: z.string().describe('Original event ID (UUID) to correct'),
    location: z.string().describe('Corrected location'),
    actorName: z.string().describe('Actor name'),
    description: z.string().describe('Explanation of the correction'),
  }, async ({ eventId, ...body }) => {
    const data = await api.post(`/api/events/${eventId}/corrections`, body);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
```

- [ ] **Step 2: Build and commit**

```bash
cd packages/mcp/customer-server && npx tsc
git add packages/mcp/customer-server/
git commit -m "feat: add event correction tool to customer MCP

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 3: Add document generation tools (dossier, DPP, list generated)

**Files:**
- Modify: `packages/mcp/customer-server/src/tools/documents.ts`

- [ ] **Step 1: Add tools**

Read `packages/mcp/customer-server/src/tools/documents.ts` and add:

```typescript
  server.tool('generate_dossier', 'Generate an Audit Dossier PDF for a batch (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.post(`/api/batches/${batchId}/dossier`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('generate_dpp', 'Generate a Digital Product Passport (JSON-LD) for a batch (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.post(`/api/batches/${batchId}/dpp`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('list_generated_documents', 'List all generated documents for a batch', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/generated-documents?batchId=${batchId}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
```

- [ ] **Step 2: Build and commit**

```bash
cd packages/mcp/customer-server && npx tsc
git add packages/mcp/customer-server/
git commit -m "feat: add dossier, DPP, and list generated documents tools to customer MCP

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 4: Add Form SD tools

**Files:**
- Modify: `packages/mcp/customer-server/src/tools/engagement.ts`

- [ ] **Step 1: Add Form SD tools**

Read `packages/mcp/customer-server/src/tools/engagement.ts` and add:

```typescript
  server.tool('get_form_sd_status', 'Check if a batch is ready for Form SD filing (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/form-sd/batches/${batchId}/status`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_supply_chain_description', 'Generate AI supply chain narrative for a batch (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/form-sd/batches/${batchId}/supply-chain`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_due_diligence_summary', 'Generate AI due diligence summary for a batch (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/form-sd/batches/${batchId}/due-diligence`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_risk_assessment', 'Generate AI risk assessment for a batch (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/form-sd/batches/${batchId}/risk-assessment`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('generate_form_sd_package', 'Generate Form SD support package for a reporting year (buyer role)', {
    reportingYear: z.number().describe('Reporting year (e.g. 2026)'),
  }, async ({ reportingYear }) => {
    const data = await api.post(`/api/form-sd/generate/${reportingYear}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
```

- [ ] **Step 2: Build and commit**

```bash
cd packages/mcp/customer-server && npx tsc
git add packages/mcp/customer-server/
git commit -m "feat: add Form SD tools to customer MCP (status, supply chain, due diligence, risk, generate)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Add notification and public verification tools

**Files:**
- Create: `packages/mcp/customer-server/src/tools/notifications.ts`
- Modify: `packages/mcp/customer-server/src/index.ts`

- [ ] **Step 1: Create notifications tool file**

Create `packages/mcp/customer-server/src/tools/notifications.ts`:

```typescript
import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient } from '../../../shared/src/index.js';

export function registerNotificationTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('list_notifications', 'List your notifications', {}, async () => {
    const data = await api.get('/api/notifications');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('mark_notification_read', 'Mark a notification as read', {
    notificationId: z.string().describe('Notification ID (UUID)'),
  }, async ({ notificationId }) => {
    const data = await api.patch(`/api/notifications/${notificationId}/read`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('verify_batch_public', 'Publicly verify a batch (no auth required)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/verify/${batchId}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

- [ ] **Step 2: Register in index.ts**

Read `packages/mcp/customer-server/src/index.ts` and add:

Import:
```typescript
import { registerNotificationTools } from './tools/notifications.js';
```

Registration (after the other `register*Tools` calls):
```typescript
registerNotificationTools(server, api);
```

- [ ] **Step 3: Build and commit**

```bash
cd packages/mcp/customer-server && npx tsc
git add packages/mcp/customer-server/
git commit -m "feat: add notification and public verification tools to customer MCP

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 2: Admin MCP — Additional Tools

### Task 6: Add missing admin tools (jobs, export, AI insights, batch detail)

**Files:**
- Modify: `packages/mcp/admin-server/src/tools/audit.ts`
- Modify: `packages/mcp/admin-server/src/tools/batches.ts`
- Create: `packages/mcp/admin-server/src/tools/jobs.ts`
- Create: `packages/mcp/admin-server/src/tools/ai.ts`
- Modify: `packages/mcp/admin-server/src/index.ts`

- [ ] **Step 1: Add export to audit tools**

Read `packages/mcp/admin-server/src/tools/audit.ts` and add:

```typescript
  server.tool('export_audit_logs', 'Export audit logs as CSV', {
    from: z.string().optional().describe('Start date (ISO 8601)'),
    to: z.string().optional().describe('End date (ISO 8601)'),
  }, async ({ from, to }) => {
    const params = new URLSearchParams();
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    const data = await api.get(`/api/admin/audit-logs/export?${params}`);
    return { content: [{ type: 'text' as const, text: typeof data === 'string' ? data : JSON.stringify(data, null, 2) }] };
  });
```

- [ ] **Step 2: Add activity and events to admin batch tools**

Read `packages/mcp/admin-server/src/tools/batches.ts` and add:

```typescript
  server.tool('get_batch_activity', 'Get activity log for a batch', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/batches/${batchId}/activity`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('list_events', 'List custody events for a batch', {
    batchId: z.string().describe('Batch ID (UUID)'),
    page: z.number().optional().default(1),
    pageSize: z.number().optional().default(50),
  }, async ({ batchId, page, pageSize }) => {
    const data = await api.get(`/api/batches/${batchId}/events?page=${page}&pageSize=${pageSize}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
```

- [ ] **Step 3: Create jobs tool file**

Create `packages/mcp/admin-server/src/tools/jobs.ts`:

```typescript
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient } from '../../../shared/src/index.js';

export function registerJobTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('list_jobs', 'List background jobs and their status', {}, async () => {
    const data = await api.get('/api/admin/jobs');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

- [ ] **Step 4: Create AI insights tool file**

Create `packages/mcp/admin-server/src/tools/ai.ts`:

```typescript
import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient } from '../../../shared/src/index.js';

export function registerAiTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('churn_prediction', 'Get churn risk analysis for tenants', {}, async () => {
    const data = await api.get('/api/ai/churn-prediction');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('tenant_health', 'Get health scores for all tenants', {}, async () => {
    const data = await api.get('/api/ai/tenant-health');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('revenue_summary', 'Get revenue breakdown and analysis', {}, async () => {
    const data = await api.get('/api/ai/revenue-summary');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('natural_language_query', 'Ask a natural language question about platform data', {
    question: z.string().describe('Your question (e.g. "How many batches were created last month?")'),
  }, async ({ question }) => {
    const data = await api.post('/api/ai/query', { question });
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

- [ ] **Step 5: Register new tools in admin index.ts**

Read `packages/mcp/admin-server/src/index.ts` and add:

Imports:
```typescript
import { registerJobTools } from './tools/jobs.js';
import { registerAiTools } from './tools/ai.js';
```

Registrations (after existing `register*Tools` calls):
```typescript
registerJobTools(server, api);
registerAiTools(server, api);
```

- [ ] **Step 6: Build and commit**

```bash
cd packages/mcp/admin-server && npx tsc
git add packages/mcp/admin-server/
git commit -m "feat: add jobs, AI insights, audit export, and batch detail tools to admin MCP

Jobs (1), AI insights (4), audit export (1), batch activity + events (2).

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 7: Update README and push

**Files:**
- Modify: `packages/mcp/README.md`

- [ ] **Step 1: Update tool counts in README**

Read `packages/mcp/README.md` and update:
- Customer MCP tool count from 18 to 38
- Admin MCP tool count from 15 to 23
- Add the new tools to the tool tables

- [ ] **Step 2: Push**

```bash
git add packages/mcp/README.md
git commit -m "docs: update MCP README with additional tools

Customer MCP: 38 tools. Admin MCP: 23 tools.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
git push origin main
```
