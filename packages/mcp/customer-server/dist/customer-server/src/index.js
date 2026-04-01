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
});
registerBatchTools(server, api);
registerEventTools(server, api);
registerComplianceTools(server, api);
registerDocumentTools(server, api);
registerSmelterTools(server, api);
registerEngagementTools(server, api);
const transport = new StdioServerTransport();
await server.connect(transport);
