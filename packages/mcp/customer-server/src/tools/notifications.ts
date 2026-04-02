import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient } from '../../../shared/src/index.js';

export function registerNotificationTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('list_notifications', 'List your notifications', {}, async () => {
    const data = await api.get('/api/notifications');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('mark_notification_read', 'Mark a notification as read', {
    notificationId: z.string().describe('Notification ID (UUID)'),
  }, async ({ notificationId }) => {
    const data = await api.patch(`/api/notifications/${notificationId}/read`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('verify_batch_public', 'Publicly verify a batch (no auth required)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/verify/${batchId}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
