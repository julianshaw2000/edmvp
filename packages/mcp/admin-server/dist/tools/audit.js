import { z } from 'zod';
export function registerAuditTools(server, api) {
    server.tool('list_audit_logs', 'Search audit logs with filters', {
        page: z.number().optional().default(1),
        pageSize: z.number().optional().default(20),
        action: z.string().optional().describe('Filter by action type'),
        entityType: z.string().optional().describe('Filter by entity type'),
        userId: z.string().optional().describe('Filter by user ID (UUID)'),
    }, async ({ page, pageSize, action, entityType, userId }) => {
        const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
        if (action)
            params.set('action', action);
        if (entityType)
            params.set('entityType', entityType);
        if (userId)
            params.set('userId', userId);
        const data = await api.get(`/api/admin/audit-logs?${params}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
