import { z } from 'zod';
export function registerTenantTools(server, api) {
    server.tool('list_tenants', 'List all tenants on the platform', {
        page: z.number().optional().default(1),
        pageSize: z.number().optional().default(20),
    }, async ({ page, pageSize }) => {
        const data = await api.get(`/api/platform/tenants?page=${page}&pageSize=${pageSize}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('create_tenant', 'Create a new tenant organisation', {
        name: z.string().describe('Organisation name'),
    }, async ({ name }) => {
        const data = await api.post('/api/platform/tenants', { name });
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('update_tenant_status', 'Activate, suspend, or trial a tenant', {
        tenantId: z.string().describe('Tenant ID (UUID)'),
        status: z.enum(['ACTIVE', 'SUSPENDED', 'TRIAL']).describe('New status'),
    }, async ({ tenantId, status }) => {
        const data = await api.patch(`/api/platform/tenants/${tenantId}/status`, { status });
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('delete_tenant', 'Delete a tenant (irreversible)', {
        tenantId: z.string().describe('Tenant ID (UUID)'),
    }, async ({ tenantId }) => {
        await api.delete(`/api/platform/tenants/${tenantId}`);
        return { content: [{ type: 'text', text: 'Tenant deleted' }] };
    });
}
