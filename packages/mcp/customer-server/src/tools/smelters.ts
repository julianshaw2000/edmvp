import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient, SmelterResponse, PagedResponse } from '../../../shared/src/index.js';

export function registerSmelterTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('search_smelters', 'Search the RMAP smelter database by name or ID', {
    query: z.string().describe('Search query (smelter name or ID)'),
    pageSize: z.number().optional().default(10),
  }, async ({ query, pageSize }) => {
    const data = await api.get<PagedResponse<SmelterResponse>>(`/api/smelters?q=${encodeURIComponent(query)}&pageSize=${pageSize}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
