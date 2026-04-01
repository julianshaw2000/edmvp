import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient, ComplianceSummary } from '../../../shared/src/index.js';

export function registerComplianceTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('get_batch_compliance', 'Get compliance status for a batch', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get<ComplianceSummary>(`/api/batches/${batchId}/compliance`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_event_compliance', 'Get compliance checks for an event', {
    eventId: z.string().describe('Event ID (UUID)'),
  }, async ({ eventId }) => {
    const data = await api.get(`/api/events/${eventId}/compliance`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
