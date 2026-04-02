import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient } from '../../../shared/src/index.js';

export function registerJobTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('list_jobs', 'List background jobs and their status', {}, async () => {
    const data = await api.get('/api/admin/jobs');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
