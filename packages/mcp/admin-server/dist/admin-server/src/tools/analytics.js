import { z } from 'zod';
export function registerAnalyticsTools(server, api) {
    server.tool('get_analytics', 'Get platform-wide analytics', {
        tenantId: z.string().optional().describe('Filter by tenant (UUID)'),
    }, async ({ tenantId }) => {
        const path = tenantId ? `/api/analytics?tenantId=${tenantId}` : '/api/analytics';
        const data = await api.get(path);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
