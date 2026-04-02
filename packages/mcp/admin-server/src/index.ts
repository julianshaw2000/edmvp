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
import { registerJobTools } from './tools/jobs.js';
import { registerAiTools } from './tools/ai.js';
import { registerEmailTools } from './tools/email.js';

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
});

registerTenantTools(server, api);
registerUserTools(server, api);
registerAnalyticsTools(server, api);
registerAuditTools(server, api);
registerRmapTools(server, api);
registerBatchTools(server, api);
registerJobTools(server, api);
registerAiTools(server, api);
registerEmailTools(server, api);

const transport = new StdioServerTransport();
await server.connect(transport);
