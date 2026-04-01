import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient, UserResponse } from '../../../shared/src/index.js';

export function registerUserTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('list_users', 'List users (optional tenant filter)', {
    tenantId: z.string().optional().describe('Filter by tenant ID (UUID)'),
  }, async ({ tenantId }) => {
    const path = tenantId ? `/api/users?tenantId=${tenantId}` : '/api/users';
    const data = await api.get<UserResponse[]>(path);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('create_user', 'Create/invite a user', {
    email: z.string().describe('User email'),
    displayName: z.string().describe('Display name'),
    role: z.enum(['SUPPLIER', 'BUYER', 'TENANT_ADMIN']).describe('Role'),
  }, async (params) => {
    const data = await api.post('/api/users', params);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('update_user', 'Update user role or status', {
    userId: z.string().describe('User ID (UUID)'),
    role: z.string().optional().describe('New role'),
    isActive: z.boolean().optional().describe('Active status'),
  }, async ({ userId, ...body }) => {
    const data = await api.patch(`/api/users/${userId}`, body);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('delete_user', 'Delete a user', {
    userId: z.string().describe('User ID (UUID)'),
  }, async ({ userId }) => {
    await api.delete(`/api/users/${userId}`);
    return { content: [{ type: 'text' as const, text: 'User deleted' }] };
  });
}
