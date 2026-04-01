import { z } from 'zod';
export function registerRmapTools(server, api) {
    server.tool('list_rmap_smelters', 'List all RMAP smelters', {}, async () => {
        const data = await api.get('/api/admin/rmap');
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('search_smelters', 'Search smelters by name or ID', {
        query: z.string().describe('Search query'),
        pageSize: z.number().optional().default(10),
    }, async ({ query, pageSize }) => {
        const data = await api.get(`/api/smelters?q=${encodeURIComponent(query)}&pageSize=${pageSize}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
