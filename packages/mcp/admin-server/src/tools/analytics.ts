import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient, AnalyticsResponse } from '../../../shared/src/index.js';

export function registerAnalyticsTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('get_analytics', 'Get platform-wide analytics', {
    tenantId: z.string().optional().describe('Filter by tenant (UUID)'),
  }, async ({ tenantId }) => {
    const path = tenantId ? `/api/analytics?tenantId=${tenantId}` : '/api/analytics';
    const data = await api.get<AnalyticsResponse>(path);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
