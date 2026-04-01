# MCP Servers — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build two MCP servers (customer + admin) that expose the auditraks REST API as MCP tools for AI assistants.

**Architecture:** Shared API client library in `packages/mcp/shared/`, two thin server packages in `packages/mcp/customer-server/` and `packages/mcp/admin-server/`. Each server registers tools that map to REST API endpoints. TypeScript with `@modelcontextprotocol/sdk`.

**Tech Stack:** TypeScript, `@modelcontextprotocol/sdk`, `zod` (parameter validation), native `fetch`

---

## Chunk 1: Shared Library + Project Setup

### Task 1: Initialize packages/mcp workspace

**Files:**
- Create: `packages/mcp/shared/package.json`
- Create: `packages/mcp/shared/tsconfig.json`
- Create: `packages/mcp/customer-server/package.json`
- Create: `packages/mcp/customer-server/tsconfig.json`
- Create: `packages/mcp/admin-server/package.json`
- Create: `packages/mcp/admin-server/tsconfig.json`

- [ ] **Step 1: Create shared package**

```bash
mkdir -p packages/mcp/shared/src
cd packages/mcp/shared
```

Create `packages/mcp/shared/package.json`:
```json
{
  "name": "@auditraks/mcp-shared",
  "version": "1.0.0",
  "private": true,
  "type": "module",
  "main": "dist/index.js",
  "types": "dist/index.d.ts",
  "scripts": {
    "build": "tsc",
    "dev": "tsc --watch"
  }
}
```

Create `packages/mcp/shared/tsconfig.json`:
```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler",
    "declaration": true,
    "outDir": "dist",
    "rootDir": "src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true
  },
  "include": ["src"]
}
```

- [ ] **Step 2: Create customer-server package**

Create `packages/mcp/customer-server/package.json`:
```json
{
  "name": "auditraks-mcp",
  "version": "1.0.0",
  "type": "module",
  "bin": {
    "auditraks-mcp": "dist/index.js"
  },
  "scripts": {
    "build": "tsc",
    "dev": "tsc --watch",
    "start": "node dist/index.js"
  },
  "dependencies": {
    "@modelcontextprotocol/sdk": "^1.0.0",
    "zod": "^3.23.0"
  }
}
```

Create `packages/mcp/customer-server/tsconfig.json`:
```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler",
    "outDir": "dist",
    "rootDir": "src",
    "strict": true,
    "esModuleInterop": true,
    "skipLibCheck": true
  },
  "include": ["src"]
}
```

- [ ] **Step 3: Create admin-server package**

Create `packages/mcp/admin-server/package.json`:
```json
{
  "name": "auditraks-mcp-admin",
  "version": "1.0.0",
  "type": "module",
  "bin": {
    "auditraks-mcp-admin": "dist/index.js"
  },
  "scripts": {
    "build": "tsc",
    "dev": "tsc --watch",
    "start": "node dist/index.js"
  },
  "dependencies": {
    "@modelcontextprotocol/sdk": "^1.0.0",
    "zod": "^3.23.0"
  }
}
```

Create `packages/mcp/admin-server/tsconfig.json` (same as customer-server).

- [ ] **Step 4: Install dependencies**

```bash
cd packages/mcp/customer-server && npm install
cd ../admin-server && npm install
```

- [ ] **Step 5: Commit**

```bash
git add packages/mcp/
git commit -m "chore: initialize MCP server packages (shared, customer, admin)

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 2: Create shared API client

**Files:**
- Create: `packages/mcp/shared/src/api-client.ts`
- Create: `packages/mcp/shared/src/types.ts`
- Create: `packages/mcp/shared/src/index.ts`

- [ ] **Step 1: Create types**

Create `packages/mcp/shared/src/types.ts`:

```typescript
export interface BatchResponse {
  id: string;
  batchNumber: string;
  mineralType: string;
  originCountry: string;
  originMine: string;
  weightKg: number;
  status: string;
  complianceStatus: string;
  createdAt: string;
  eventCount: number;
}

export interface CustodyEventResponse {
  id: string;
  batchId: string;
  eventType: string;
  eventDate: string;
  location: string;
  actorName: string;
  isCorrection: boolean;
  sha256Hash: string;
  createdAt: string;
}

export interface ComplianceSummary {
  batchId: string;
  overallStatus: string;
  checks: { framework: string; status: string; checkedAt: string }[];
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface SmelterResponse {
  smelterId: string;
  smelterName: string;
  country: string;
  conformanceStatus: string;
  mineralType?: string;
}

export interface SupplierEngagement {
  totalSuppliers: number;
  activeSuppliers: number;
  staleSuppliers: number;
  flaggedSuppliers: number;
  suppliers: {
    id: string;
    displayName: string;
    lastEventDate: string | null;
    batchCount: number;
    flaggedBatchCount: number;
    status: string;
  }[];
}

export interface AnalyticsResponse {
  totalBatches: number;
  completedBatches: number;
  flaggedBatches: number;
  pendingBatches: number;
  totalEvents: number;
  totalUsers: number;
}

export interface AuditLogEntry {
  id: string;
  userDisplayName: string;
  action: string;
  entityType: string;
  entityId: string;
  result: string;
  timestamp: string;
}

export interface TenantResponse {
  id: string;
  name: string;
  status: string;
  createdAt: string;
  userCount: number;
  batchCount: number;
}

export interface UserResponse {
  id: string;
  email: string;
  displayName: string;
  role: string;
  isActive: boolean;
}

export interface ApiError {
  error: string;
  status: number;
}
```

- [ ] **Step 2: Create API client**

Create `packages/mcp/shared/src/api-client.ts`:

```typescript
import type { ApiError } from './types.js';

export interface ApiClientConfig {
  baseUrl: string;
  apiKey?: string;
  email?: string;
  password?: string;
}

export class AuditraksApiClient {
  private baseUrl: string;
  private apiKey?: string;
  private accessToken?: string;
  private refreshToken?: string;
  private email?: string;
  private password?: string;

  constructor(config: ApiClientConfig) {
    this.baseUrl = config.baseUrl.replace(/\/$/, '');
    this.apiKey = config.apiKey;
    this.email = config.email;
    this.password = config.password;
  }

  async login(): Promise<void> {
    if (!this.email || !this.password) return;
    const res = await fetch(`${this.baseUrl}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: this.email, password: this.password }),
    });
    if (!res.ok) throw new Error(`Login failed: ${res.status}`);
    const data = await res.json() as { accessToken: string; refreshToken?: string };
    this.accessToken = data.accessToken;
    if (data.refreshToken) this.refreshToken = data.refreshToken;
  }

  private async getHeaders(): Promise<Record<string, string>> {
    const headers: Record<string, string> = { 'Content-Type': 'application/json' };
    if (this.apiKey) {
      headers['X-API-Key'] = this.apiKey;
    } else if (this.accessToken) {
      headers['Authorization'] = `Bearer ${this.accessToken}`;
    }
    return headers;
  }

  async request<T>(method: string, path: string, body?: unknown): Promise<T> {
    const headers = await this.getHeaders();
    const url = `${this.baseUrl}${path}`;

    const res = await fetch(url, {
      method,
      headers,
      body: body ? JSON.stringify(body) : undefined,
    });

    if (res.status === 401 && this.email && this.password) {
      await this.login();
      const retryHeaders = await this.getHeaders();
      const retry = await fetch(url, {
        method,
        headers: retryHeaders,
        body: body ? JSON.stringify(body) : undefined,
      });
      if (!retry.ok) {
        const err = await retry.json().catch(() => ({ error: retry.statusText })) as ApiError;
        throw new Error(`${retry.status}: ${err.error}`);
      }
      return retry.json() as Promise<T>;
    }

    if (!res.ok) {
      const err = await res.json().catch(() => ({ error: res.statusText })) as ApiError;
      throw new Error(`${res.status}: ${err.error}`);
    }

    if (res.status === 204) return {} as T;
    return res.json() as Promise<T>;
  }

  get<T>(path: string): Promise<T> { return this.request<T>('GET', path); }
  post<T>(path: string, body?: unknown): Promise<T> { return this.request<T>('POST', path, body); }
  patch<T>(path: string, body?: unknown): Promise<T> { return this.request<T>('PATCH', path, body); }
  delete<T>(path: string): Promise<T> { return this.request<T>('DELETE', path); }
}
```

- [ ] **Step 3: Create index**

Create `packages/mcp/shared/src/index.ts`:
```typescript
export { AuditraksApiClient, type ApiClientConfig } from './api-client.js';
export * from './types.js';
```

- [ ] **Step 4: Build shared**

```bash
cd packages/mcp/shared && npx tsc
```

- [ ] **Step 5: Commit**

```bash
git add packages/mcp/shared/
git commit -m "feat: add shared API client and types for MCP servers

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 2: Customer MCP Server

### Task 3: Create customer MCP server with batch and event tools

**Files:**
- Create: `packages/mcp/customer-server/src/index.ts`
- Create: `packages/mcp/customer-server/src/tools/batches.ts`
- Create: `packages/mcp/customer-server/src/tools/events.ts`
- Create: `packages/mcp/customer-server/src/tools/compliance.ts`
- Create: `packages/mcp/customer-server/src/tools/documents.ts`
- Create: `packages/mcp/customer-server/src/tools/smelters.ts`
- Create: `packages/mcp/customer-server/src/tools/engagement.ts`

- [ ] **Step 1: Create batch tools**

Create `packages/mcp/customer-server/src/tools/batches.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, BatchResponse, PagedResponse } from '../../../shared/src/index.js';

export function registerBatchTools(server: any, api: AuditraksApiClient) {
  server.tool('list_batches', 'List mineral batches with pagination', {
    page: z.number().optional().default(1).describe('Page number'),
    pageSize: z.number().optional().default(20).describe('Items per page'),
  }, async ({ page, pageSize }: { page: number; pageSize: number }) => {
    const data = await api.get<PagedResponse<BatchResponse>>(
      `/api/batches?page=${page}&pageSize=${pageSize}`
    );
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_batch', 'Get batch details by ID', {
    batchId: z.string().uuid().describe('Batch ID'),
  }, async ({ batchId }: { batchId: string }) => {
    const data = await api.get<BatchResponse>(`/api/batches/${batchId}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('create_batch', 'Create a new mineral batch', {
    batchNumber: z.string().describe('Unique batch number (e.g. W-2026-050)'),
    mineralType: z.string().describe('Mineral type (e.g. Tungsten (Wolframite))'),
    originCountry: z.string().describe('Origin country ISO code (e.g. RW)'),
    originMine: z.string().describe('Mine site name'),
    weightKg: z.number().describe('Weight in kilograms'),
  }, async (params: any) => {
    const data = await api.post<BatchResponse>('/api/batches', params);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_batch_activity', 'Get activity log for a batch', {
    batchId: z.string().uuid().describe('Batch ID'),
  }, async ({ batchId }: { batchId: string }) => {
    const data = await api.get(`/api/batches/${batchId}/activity`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('verify_batch_integrity', 'Verify SHA-256 hash chain integrity of a batch', {
    batchId: z.string().uuid().describe('Batch ID'),
  }, async ({ batchId }: { batchId: string }) => {
    const data = await api.get(`/api/batches/${batchId}/verify-integrity`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

- [ ] **Step 2: Create event tools**

Create `packages/mcp/customer-server/src/tools/events.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, CustodyEventResponse, PagedResponse } from '../../../shared/src/index.js';

export function registerEventTools(server: any, api: AuditraksApiClient) {
  server.tool('list_events', 'List custody events for a batch', {
    batchId: z.string().uuid().describe('Batch ID'),
    page: z.number().optional().default(1),
    pageSize: z.number().optional().default(50),
  }, async ({ batchId, page, pageSize }: any) => {
    const data = await api.get<PagedResponse<CustodyEventResponse>>(
      `/api/batches/${batchId}/events?page=${page}&pageSize=${pageSize}`
    );
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_event', 'Get custody event details', {
    eventId: z.string().uuid().describe('Event ID'),
  }, async ({ eventId }: { eventId: string }) => {
    const data = await api.get<CustodyEventResponse>(`/api/events/${eventId}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('create_event', 'Log a custody event on a batch', {
    batchId: z.string().uuid().describe('Batch ID'),
    eventType: z.enum(['MINE_EXTRACTION', 'LABORATORY_ASSAY', 'CONCENTRATION', 'TRADING_TRANSFER', 'PRIMARY_PROCESSING', 'EXPORT_SHIPMENT']).describe('Event type'),
    eventDate: z.string().describe('Event date (ISO 8601)'),
    location: z.string().describe('Location name'),
    actorName: z.string().describe('Actor performing the event'),
    description: z.string().optional().describe('Description'),
  }, async ({ batchId, ...body }: any) => {
    const data = await api.post(`/api/batches/${batchId}/events`, body);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

- [ ] **Step 3: Create compliance, documents, smelters, engagement tools**

Create `packages/mcp/customer-server/src/tools/compliance.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, ComplianceSummary } from '../../../shared/src/index.js';

export function registerComplianceTools(server: any, api: AuditraksApiClient) {
  server.tool('get_batch_compliance', 'Get compliance status for a batch', {
    batchId: z.string().uuid().describe('Batch ID'),
  }, async ({ batchId }: { batchId: string }) => {
    const data = await api.get<ComplianceSummary>(`/api/batches/${batchId}/compliance`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_event_compliance', 'Get compliance checks for an event', {
    eventId: z.string().uuid().describe('Event ID'),
  }, async ({ eventId }: { eventId: string }) => {
    const data = await api.get(`/api/events/${eventId}/compliance`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

Create `packages/mcp/customer-server/src/tools/documents.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient } from '../../../shared/src/index.js';

export function registerDocumentTools(server: any, api: AuditraksApiClient) {
  server.tool('list_batch_documents', 'List documents for a batch', {
    batchId: z.string().uuid().describe('Batch ID'),
  }, async ({ batchId }: { batchId: string }) => {
    const data = await api.get(`/api/batches/${batchId}/documents`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('generate_passport', 'Generate a Material Passport PDF for a compliant batch', {
    batchId: z.string().uuid().describe('Batch ID (must be COMPLIANT)'),
  }, async ({ batchId }: { batchId: string }) => {
    const data = await api.post(`/api/batches/${batchId}/passport`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('share_document', 'Create a 30-day shareable link for a generated document', {
    documentId: z.string().uuid().describe('Generated document ID'),
  }, async ({ documentId }: { documentId: string }) => {
    const data = await api.post(`/api/generated-documents/${documentId}/share`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('share_document_email', 'Email a generated document to a recipient', {
    documentId: z.string().uuid().describe('Generated document ID'),
    recipientEmail: z.string().email().describe('Recipient email address'),
    message: z.string().optional().describe('Optional message to include'),
  }, async ({ documentId, ...body }: any) => {
    const data = await api.post(`/api/generated-documents/${documentId}/share-email`, body);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

Create `packages/mcp/customer-server/src/tools/smelters.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, SmelterResponse, PagedResponse } from '../../../shared/src/index.js';

export function registerSmelterTools(server: any, api: AuditraksApiClient) {
  server.tool('search_smelters', 'Search the RMAP smelter database by name or ID', {
    query: z.string().describe('Search query (smelter name or ID)'),
    pageSize: z.number().optional().default(10),
  }, async ({ query, pageSize }: any) => {
    const data = await api.get<PagedResponse<SmelterResponse>>(
      `/api/smelters?q=${encodeURIComponent(query)}&pageSize=${pageSize}`
    );
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

Create `packages/mcp/customer-server/src/tools/engagement.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, SupplierEngagement } from '../../../shared/src/index.js';

export function registerEngagementTools(server: any, api: AuditraksApiClient) {
  server.tool('get_supplier_engagement', 'Get supplier engagement metrics (buyer role)', {}, async () => {
    const data = await api.get<SupplierEngagement>('/api/buyer/supplier-engagement');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('nudge_supplier', 'Send a reminder email to a supplier (buyer role, 7-day rate limit)', {
    supplierId: z.string().uuid().describe('Supplier user ID'),
  }, async ({ supplierId }: { supplierId: string }) => {
    const data = await api.post('/api/buyer/nudge-supplier', { supplierId });
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('list_filing_cycles', 'List Form SD filing cycles', {}, async () => {
    const data = await api.get('/api/form-sd/filing-cycles');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

- [ ] **Step 4: Create the server entry point**

Create `packages/mcp/customer-server/src/index.ts`:

```typescript
#!/usr/bin/env node
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { AuditraksApiClient } from '../../shared/src/index.js';
import { registerBatchTools } from './tools/batches.js';
import { registerEventTools } from './tools/events.js';
import { registerComplianceTools } from './tools/compliance.js';
import { registerDocumentTools } from './tools/documents.js';
import { registerSmelterTools } from './tools/smelters.js';
import { registerEngagementTools } from './tools/engagement.js';

const apiKey = process.env.AUDITRAKS_API_KEY;
const apiUrl = process.env.AUDITRAKS_API_URL ?? 'https://accutrac-api.onrender.com';

if (!apiKey) {
  console.error('AUDITRAKS_API_KEY environment variable is required');
  process.exit(1);
}

const api = new AuditraksApiClient({ baseUrl: apiUrl, apiKey });

const server = new McpServer({
  name: 'auditraks',
  version: '1.0.0',
  description: 'auditraks mineral supply chain compliance platform — query batches, check compliance, manage custody events',
});

registerBatchTools(server, api);
registerEventTools(server, api);
registerComplianceTools(server, api);
registerDocumentTools(server, api);
registerSmelterTools(server, api);
registerEngagementTools(server, api);

const transport = new StdioServerTransport();
await server.connect(transport);
```

- [ ] **Step 5: Build and test**

```bash
cd packages/mcp/customer-server && npx tsc
```

- [ ] **Step 6: Commit**

```bash
git add packages/mcp/customer-server/
git commit -m "feat: add customer MCP server with 24 tools

Batches (5), events (3), compliance (2), documents (4), smelters (1),
engagement (3), Form SD (1). Authenticates via API key.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Chunk 3: Admin MCP Server

### Task 4: Create admin MCP server

**Files:**
- Create: `packages/mcp/admin-server/src/index.ts`
- Create: `packages/mcp/admin-server/src/tools/tenants.ts`
- Create: `packages/mcp/admin-server/src/tools/users.ts`
- Create: `packages/mcp/admin-server/src/tools/analytics.ts`
- Create: `packages/mcp/admin-server/src/tools/audit.ts`
- Create: `packages/mcp/admin-server/src/tools/rmap.ts`
- Create: `packages/mcp/admin-server/src/tools/batches.ts`

- [ ] **Step 1: Create tenant tools**

Create `packages/mcp/admin-server/src/tools/tenants.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, TenantResponse, PagedResponse } from '../../../shared/src/index.js';

export function registerTenantTools(server: any, api: AuditraksApiClient) {
  server.tool('list_tenants', 'List all tenants on the platform', {
    page: z.number().optional().default(1),
    pageSize: z.number().optional().default(20),
  }, async ({ page, pageSize }: any) => {
    const data = await api.get<PagedResponse<TenantResponse>>(
      `/api/platform/tenants?page=${page}&pageSize=${pageSize}`
    );
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('create_tenant', 'Create a new tenant organisation', {
    name: z.string().describe('Organisation name'),
  }, async ({ name }: { name: string }) => {
    const data = await api.post('/api/platform/tenants', { name });
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('update_tenant_status', 'Activate, suspend, or trial a tenant', {
    tenantId: z.string().uuid().describe('Tenant ID'),
    status: z.enum(['ACTIVE', 'SUSPENDED', 'TRIAL']).describe('New status'),
  }, async ({ tenantId, status }: any) => {
    const data = await api.patch(`/api/platform/tenants/${tenantId}/status`, { status });
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('delete_tenant', 'Delete a tenant (irreversible)', {
    tenantId: z.string().uuid().describe('Tenant ID'),
  }, async ({ tenantId }: { tenantId: string }) => {
    await api.delete(`/api/platform/tenants/${tenantId}`);
    return { content: [{ type: 'text' as const, text: 'Tenant deleted' }] };
  });
}
```

- [ ] **Step 2: Create users, analytics, audit, rmap, batches tools**

Create `packages/mcp/admin-server/src/tools/users.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, UserResponse } from '../../../shared/src/index.js';

export function registerUserTools(server: any, api: AuditraksApiClient) {
  server.tool('list_users', 'List users (optional tenant filter)', {
    tenantId: z.string().uuid().optional().describe('Filter by tenant ID'),
  }, async ({ tenantId }: { tenantId?: string }) => {
    const path = tenantId ? `/api/users?tenantId=${tenantId}` : '/api/users';
    const data = await api.get<UserResponse[]>(path);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('create_user', 'Create/invite a user', {
    email: z.string().email().describe('User email'),
    displayName: z.string().describe('Display name'),
    role: z.enum(['SUPPLIER', 'BUYER', 'TENANT_ADMIN']).describe('Role'),
  }, async (params: any) => {
    const data = await api.post('/api/users', params);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('update_user', 'Update user role or status', {
    userId: z.string().uuid().describe('User ID'),
    role: z.string().optional().describe('New role'),
    isActive: z.boolean().optional().describe('Active status'),
  }, async ({ userId, ...body }: any) => {
    const data = await api.patch(`/api/users/${userId}`, body);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('delete_user', 'Delete a user', {
    userId: z.string().uuid().describe('User ID'),
  }, async ({ userId }: { userId: string }) => {
    await api.delete(`/api/users/${userId}`);
    return { content: [{ type: 'text' as const, text: 'User deleted' }] };
  });
}
```

Create `packages/mcp/admin-server/src/tools/analytics.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, AnalyticsResponse } from '../../../shared/src/index.js';

export function registerAnalyticsTools(server: any, api: AuditraksApiClient) {
  server.tool('get_analytics', 'Get platform-wide analytics', {
    tenantId: z.string().uuid().optional().describe('Filter by tenant'),
  }, async ({ tenantId }: { tenantId?: string }) => {
    const path = tenantId ? `/api/analytics?tenantId=${tenantId}` : '/api/analytics';
    const data = await api.get<AnalyticsResponse>(path);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

Create `packages/mcp/admin-server/src/tools/audit.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, AuditLogEntry, PagedResponse } from '../../../shared/src/index.js';

export function registerAuditTools(server: any, api: AuditraksApiClient) {
  server.tool('list_audit_logs', 'Search audit logs with filters', {
    page: z.number().optional().default(1),
    pageSize: z.number().optional().default(20),
    action: z.string().optional().describe('Filter by action type'),
    entityType: z.string().optional().describe('Filter by entity type'),
    userId: z.string().uuid().optional().describe('Filter by user ID'),
  }, async ({ page, pageSize, action, entityType, userId }: any) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (action) params.set('action', action);
    if (entityType) params.set('entityType', entityType);
    if (userId) params.set('userId', userId);
    const data = await api.get<PagedResponse<AuditLogEntry>>(`/api/admin/audit-logs?${params}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

Create `packages/mcp/admin-server/src/tools/rmap.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, SmelterResponse, PagedResponse } from '../../../shared/src/index.js';

export function registerRmapTools(server: any, api: AuditraksApiClient) {
  server.tool('list_rmap_smelters', 'List all RMAP smelters', {}, async () => {
    const data = await api.get('/api/admin/rmap');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('search_smelters', 'Search smelters by name or ID', {
    query: z.string().describe('Search query'),
    pageSize: z.number().optional().default(10),
  }, async ({ query, pageSize }: any) => {
    const data = await api.get<PagedResponse<SmelterResponse>>(
      `/api/smelters?q=${encodeURIComponent(query)}&pageSize=${pageSize}`
    );
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

Create `packages/mcp/admin-server/src/tools/batches.ts`:

```typescript
import { z } from 'zod';
import type { AuditraksApiClient, BatchResponse, ComplianceSummary, PagedResponse } from '../../../shared/src/index.js';

export function registerBatchTools(server: any, api: AuditraksApiClient) {
  server.tool('list_batches', 'List batches (cross-tenant for admin)', {
    page: z.number().optional().default(1),
    pageSize: z.number().optional().default(20),
  }, async ({ page, pageSize }: any) => {
    const data = await api.get<PagedResponse<BatchResponse>>(`/api/batches?page=${page}&pageSize=${pageSize}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_batch', 'Get batch details', {
    batchId: z.string().uuid().describe('Batch ID'),
  }, async ({ batchId }: { batchId: string }) => {
    const data = await api.get<BatchResponse>(`/api/batches/${batchId}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_batch_compliance', 'Get compliance status for a batch', {
    batchId: z.string().uuid().describe('Batch ID'),
  }, async ({ batchId }: { batchId: string }) => {
    const data = await api.get<ComplianceSummary>(`/api/batches/${batchId}/compliance`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

- [ ] **Step 3: Create admin server entry point**

Create `packages/mcp/admin-server/src/index.ts`:

```typescript
#!/usr/bin/env node
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { AuditraksApiClient } from '../../shared/src/index.js';
import { registerTenantTools } from './tools/tenants.js';
import { registerUserTools } from './tools/users.js';
import { registerAnalyticsTools } from './tools/analytics.js';
import { registerAuditTools } from './tools/audit.js';
import { registerRmapTools } from './tools/rmap.js';
import { registerBatchTools } from './tools/batches.js';

const email = process.env.AUDITRAKS_EMAIL;
const password = process.env.AUDITRAKS_PASSWORD;
const apiUrl = process.env.AUDITRAKS_API_URL ?? 'https://accutrac-api.onrender.com';

if (!email || !password) {
  console.error('AUDITRAKS_EMAIL and AUDITRAKS_PASSWORD environment variables are required');
  process.exit(1);
}

const api = new AuditraksApiClient({ baseUrl: apiUrl, email, password });
await api.login();

const server = new McpServer({
  name: 'auditraks-admin',
  version: '1.0.0',
  description: 'auditraks platform administration — manage tenants, users, RMAP data, audit logs, analytics',
});

registerTenantTools(server, api);
registerUserTools(server, api);
registerAnalyticsTools(server, api);
registerAuditTools(server, api);
registerRmapTools(server, api);
registerBatchTools(server, api);

const transport = new StdioServerTransport();
await server.connect(transport);
```

- [ ] **Step 4: Build**

```bash
cd packages/mcp/admin-server && npx tsc
```

- [ ] **Step 5: Commit**

```bash
git add packages/mcp/admin-server/
git commit -m "feat: add admin MCP server with 15 tools

Tenants (4), users (4), analytics (1), audit (1), RMAP (2), batches (3).
Authenticates via email/password JWT.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

### Task 5: Push and document

- [ ] **Step 1: Push**

```bash
git push origin main
```

- [ ] **Step 2: Add usage to README or docs**

Document in a brief readme at `packages/mcp/README.md`:

```markdown
# auditraks MCP Servers

## Customer MCP (auditraks-mcp)

For customers connecting AI assistants to their auditraks tenant.

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

24 tools: batches, events, compliance, documents, smelters, engagement, Form SD.

## Admin MCP (auditraks-mcp-admin)

For platform administrators.

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

15 tools: tenants, users, analytics, audit logs, RMAP, batches.
```

- [ ] **Step 3: Commit**

```bash
git add packages/mcp/README.md
git commit -m "docs: add MCP servers README with configuration examples

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
git push origin main
```
