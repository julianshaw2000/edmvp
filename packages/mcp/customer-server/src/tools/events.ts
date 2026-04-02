import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient, CustodyEventResponse, PagedResponse } from '../../../shared/src/index.js';

export function registerEventTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('list_events', 'List custody events for a batch', {
    batchId: z.string().describe('Batch ID (UUID)'),
    page: z.number().optional().default(1),
    pageSize: z.number().optional().default(50),
  }, async ({ batchId, page, pageSize }) => {
    const data = await api.get<PagedResponse<CustodyEventResponse>>(`/api/batches/${batchId}/events?page=${page}&pageSize=${pageSize}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_event', 'Get custody event details', {
    eventId: z.string().describe('Event ID (UUID)'),
  }, async ({ eventId }) => {
    const data = await api.get<CustodyEventResponse>(`/api/events/${eventId}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('create_event', 'Log a custody event on a batch', {
    batchId: z.string().describe('Batch ID (UUID)'),
    eventType: z.enum(['MINE_EXTRACTION', 'LABORATORY_ASSAY', 'CONCENTRATION', 'TRADING_TRANSFER', 'PRIMARY_PROCESSING', 'EXPORT_SHIPMENT']).describe('Event type'),
    eventDate: z.string().describe('Event date (ISO 8601)'),
    location: z.string().describe('Location name'),
    actorName: z.string().describe('Actor performing the event'),
    description: z.string().optional().describe('Description'),
  }, async ({ batchId, ...body }) => {
    const data = await api.post(`/api/batches/${batchId}/events`, body);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('create_correction', 'Submit a correction for a custody event', {
    eventId: z.string().describe('Original event ID (UUID) to correct'),
    location: z.string().describe('Corrected location'),
    actorName: z.string().describe('Actor name'),
    description: z.string().describe('Explanation of the correction'),
  }, async ({ eventId, ...body }) => {
    const data = await api.post(`/api/events/${eventId}/corrections`, body);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
