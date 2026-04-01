import { z } from 'zod';
export function registerSmelterTools(server, api) {
    server.tool('search_smelters', 'Search the RMAP smelter database by name or ID', {
        query: z.string().describe('Search query (smelter name or ID)'),
        pageSize: z.number().optional().default(10),
    }, async ({ query, pageSize }) => {
        const data = await api.get(`/api/smelters?q=${encodeURIComponent(query)}&pageSize=${pageSize}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
