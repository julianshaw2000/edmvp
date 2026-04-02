import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient, AuditLogEntry, PagedResponse } from '../../../shared/src/index.js';

export function registerAuditTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('list_audit_logs', 'Search audit logs with filters', {
    page: z.number().optional().default(1),
    pageSize: z.number().optional().default(20),
    action: z.string().optional().describe('Filter by action type'),
    entityType: z.string().optional().describe('Filter by entity type'),
    userId: z.string().optional().describe('Filter by user ID (UUID)'),
  }, async ({ page, pageSize, action, entityType, userId }) => {
    const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    if (action) params.set('action', action);
    if (entityType) params.set('entityType', entityType);
    if (userId) params.set('userId', userId);
    const data = await api.get<PagedResponse<AuditLogEntry>>(`/api/admin/audit-logs?${params}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('export_audit_logs', 'Export audit logs as CSV', {
    from: z.string().optional().describe('Start date (ISO 8601)'),
    to: z.string().optional().describe('End date (ISO 8601)'),
  }, async ({ from, to }) => {
    const params = new URLSearchParams();
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    const data = await api.get(`/api/admin/audit-logs/export?${params}`);
    return { content: [{ type: 'text' as const, text: typeof data === 'string' ? data : JSON.stringify(data, null, 2) }] };
  });
}
