import { z } from 'zod';
export function registerBatchTools(server, api) {
    server.tool('list_batches', 'List mineral batches with pagination', {
        page: z.number().optional().default(1).describe('Page number'),
        pageSize: z.number().optional().default(20).describe('Items per page'),
    }, async ({ page, pageSize }) => {
        const data = await api.get(`/api/batches?page=${page}&pageSize=${pageSize}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('get_batch', 'Get batch details by ID', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.get(`/api/batches/${batchId}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('create_batch', 'Create a new mineral batch', {
        batchNumber: z.string().describe('Unique batch number (e.g. W-2026-050)'),
        mineralType: z.string().describe('Mineral type (e.g. Tungsten (Wolframite))'),
        originCountry: z.string().describe('Origin country ISO code (e.g. RW)'),
        originMine: z.string().describe('Mine site name'),
        weightKg: z.number().describe('Weight in kilograms'),
    }, async (params) => {
        const data = await api.post('/api/batches', params);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('get_batch_activity', 'Get activity log for a batch', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.get(`/api/batches/${batchId}/activity`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('verify_batch_integrity', 'Verify SHA-256 hash chain integrity of a batch', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.get(`/api/batches/${batchId}/verify-integrity`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('update_batch_status', 'Update the status of a batch', {
        batchId: z.string().describe('Batch ID (UUID)'),
        status: z.string().describe('New status (e.g. ACTIVE, COMPLETED)'),
    }, async ({ batchId, status }) => {
        const data = await api.patch(`/api/batches/${batchId}/status`, { status });
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('split_batch', 'Split a batch into sub-batches', {
        batchId: z.string().describe('Batch ID (UUID)'),
        splits: z.array(z.object({
            batchNumber: z.string().describe('New batch number'),
            weightKg: z.number().describe('Weight for this split'),
        })).describe('Array of splits with batch numbers and weights'),
    }, async ({ batchId, splits }) => {
        const data = await api.post(`/api/batches/${batchId}/split`, { splits });
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
