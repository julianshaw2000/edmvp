import { z } from 'zod';
export function registerBatchTools(server, api) {
    server.tool('list_batches', 'List batches (cross-tenant for admin)', {
        page: z.number().optional().default(1),
        pageSize: z.number().optional().default(20),
    }, async ({ page, pageSize }) => {
        const data = await api.get(`/api/batches?page=${page}&pageSize=${pageSize}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('get_batch', 'Get batch details', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.get(`/api/batches/${batchId}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('get_batch_compliance', 'Get compliance status for a batch', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.get(`/api/batches/${batchId}/compliance`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('get_batch_activity', 'Get activity log for a batch', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.get(`/api/batches/${batchId}/activity`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('list_events', 'List custody events for a batch', {
        batchId: z.string().describe('Batch ID (UUID)'),
        page: z.number().optional().default(1),
        pageSize: z.number().optional().default(50),
    }, async ({ batchId, page, pageSize }) => {
        const data = await api.get(`/api/batches/${batchId}/events?page=${page}&pageSize=${pageSize}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
