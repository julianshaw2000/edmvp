import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient, SupplierEngagement } from '../../../shared/src/index.js';

export function registerEngagementTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('get_supplier_engagement', 'Get supplier engagement metrics (buyer role)', {}, async () => {
    const data = await api.get<SupplierEngagement>('/api/buyer/supplier-engagement');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('nudge_supplier', 'Send a reminder email to a supplier (buyer role, 7-day rate limit)', {
    supplierId: z.string().describe('Supplier user ID (UUID)'),
  }, async ({ supplierId }) => {
    const data = await api.post('/api/buyer/nudge-supplier', { supplierId });
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('list_filing_cycles', 'List Form SD filing cycles', {}, async () => {
    const data = await api.get('/api/form-sd/filing-cycles');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
