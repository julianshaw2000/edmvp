import { describe, it, expect, beforeEach } from 'vitest';
import { createMockApiClient } from '../../../shared/src/mock-api-client.js';
import { registerBatchTools } from '../tools/batches.js';
import { registerEventTools } from '../tools/events.js';
import { registerComplianceTools } from '../tools/compliance.js';
import { registerDocumentTools } from '../tools/documents.js';
import { registerSmelterTools } from '../tools/smelters.js';
import { registerEngagementTools } from '../tools/engagement.js';
import { registerNotificationTools } from '../tools/notifications.js';

// Fake MCP server that captures tool registrations
function createFakeServer() {
  const tools: Record<string, { handler: Function; description: string }> = {};
  return {
    tool(name: string, description: string, schema: any, handler: Function) {
      tools[name] = { handler, description };
    },
    tools,
    async callTool(name: string, params: Record<string, any> = {}) {
      const tool = tools[name];
      if (!tool) throw new Error(`Tool ${name} not registered`);
      return tool.handler(params);
    },
  };
}

describe('Customer MCP Tools', () => {
  let server: ReturnType<typeof createFakeServer>;
  let api: ReturnType<typeof createMockApiClient>;

  beforeEach(() => {
    server = createFakeServer();
    api = createMockApiClient();
    api._setMockResponse({ items: [], totalCount: 0 });

    registerBatchTools(server as any, api);
    registerEventTools(server as any, api);
    registerComplianceTools(server as any, api);
    registerDocumentTools(server as any, api);
    registerSmelterTools(server as any, api);
    registerEngagementTools(server as any, api);
    registerNotificationTools(server as any, api);
  });

  // === BATCH TOOLS (7) ===
  describe('Batch Tools', () => {
    it('list_batches calls GET /api/batches with pagination', async () => {
      await server.callTool('list_batches', { page: 2, pageSize: 10 });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toContain('/api/batches');
      expect(api._lastCall?.path).toContain('page=2');
      expect(api._lastCall?.path).toContain('pageSize=10');
    });

    it('get_batch calls GET /api/batches/{id}', async () => {
      await server.callTool('get_batch', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123');
    });

    it('create_batch calls POST /api/batches', async () => {
      const params = { batchNumber: 'W-001', mineralType: 'Tungsten', originCountry: 'RW', originMine: 'Mine1', weightKg: 100 };
      await server.callTool('create_batch', params);
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/batches');
      expect(api._lastCall?.body).toEqual(params);
    });

    it('get_batch_activity calls GET /api/batches/{id}/activity', async () => {
      await server.callTool('get_batch_activity', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/activity');
    });

    it('verify_batch_integrity calls GET /api/batches/{id}/verify-integrity', async () => {
      await server.callTool('verify_batch_integrity', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/verify-integrity');
    });

    it('update_batch_status calls PATCH /api/batches/{id}/status', async () => {
      await server.callTool('update_batch_status', { batchId: 'abc-123', status: 'COMPLETED' });
      expect(api._lastCall?.method).toBe('PATCH');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/status');
      expect(api._lastCall?.body).toEqual({ status: 'COMPLETED' });
    });

    it('split_batch calls POST /api/batches/{id}/split', async () => {
      const splits = [{ batchNumber: 'W-001a', weightKg: 50 }];
      await server.callTool('split_batch', { batchId: 'abc-123', splits });
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/split');
    });
  });

  // === EVENT TOOLS (4) ===
  describe('Event Tools', () => {
    it('list_events calls GET /api/batches/{id}/events', async () => {
      await server.callTool('list_events', { batchId: 'abc-123', page: 1, pageSize: 50 });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toContain('/api/batches/abc-123/events');
    });

    it('get_event calls GET /api/events/{id}', async () => {
      await server.callTool('get_event', { eventId: 'evt-456' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/events/evt-456');
    });

    it('create_event calls POST /api/batches/{id}/events', async () => {
      const params = { batchId: 'abc-123', eventType: 'MINE_EXTRACTION', eventDate: '2026-01-01', location: 'Mine', actorName: 'John' };
      await server.callTool('create_event', params);
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/events');
    });

    it('create_correction calls POST /api/events/{id}/corrections', async () => {
      await server.callTool('create_correction', { eventId: 'evt-456', location: 'Fixed', actorName: 'John', description: 'Corrected' });
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/events/evt-456/corrections');
    });
  });

  // === COMPLIANCE TOOLS (2) ===
  describe('Compliance Tools', () => {
    it('get_batch_compliance calls GET /api/batches/{id}/compliance', async () => {
      await server.callTool('get_batch_compliance', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/compliance');
    });

    it('get_event_compliance calls GET /api/events/{id}/compliance', async () => {
      await server.callTool('get_event_compliance', { eventId: 'evt-456' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/events/evt-456/compliance');
    });
  });

  // === DOCUMENT TOOLS (7) ===
  describe('Document Tools', () => {
    it('list_batch_documents calls GET /api/batches/{id}/documents', async () => {
      await server.callTool('list_batch_documents', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/documents');
    });

    it('generate_passport calls POST /api/batches/{id}/passport', async () => {
      await server.callTool('generate_passport', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/passport');
    });

    it('generate_dossier calls POST /api/batches/{id}/dossier', async () => {
      await server.callTool('generate_dossier', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/dossier');
    });

    it('generate_dpp calls POST /api/batches/{id}/dpp', async () => {
      await server.callTool('generate_dpp', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/batches/abc-123/dpp');
    });

    it('list_generated_documents calls GET /api/generated-documents', async () => {
      await server.callTool('list_generated_documents', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toContain('/api/generated-documents');
      expect(api._lastCall?.path).toContain('batchId=abc-123');
    });

    it('share_document calls POST /api/generated-documents/{id}/share', async () => {
      await server.callTool('share_document', { documentId: 'doc-789' });
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/generated-documents/doc-789/share');
    });

    it('share_document_email calls POST /api/generated-documents/{id}/share-email', async () => {
      await server.callTool('share_document_email', { documentId: 'doc-789', recipientEmail: 'test@test.com', message: 'Hi' });
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/generated-documents/doc-789/share-email');
    });
  });

  // === SMELTER TOOLS (1) ===
  describe('Smelter Tools', () => {
    it('search_smelters calls GET /api/smelters with query', async () => {
      await server.callTool('search_smelters', { query: 'Wolfram', pageSize: 10 });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toContain('/api/smelters');
      expect(api._lastCall?.path).toContain('q=Wolfram');
    });
  });

  // === ENGAGEMENT TOOLS (8) ===
  describe('Engagement & Form SD Tools', () => {
    it('get_supplier_engagement calls GET /api/buyer/supplier-engagement', async () => {
      await server.callTool('get_supplier_engagement', {});
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/buyer/supplier-engagement');
    });

    it('nudge_supplier calls POST /api/buyer/nudge-supplier', async () => {
      await server.callTool('nudge_supplier', { supplierId: 'sup-123' });
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/buyer/nudge-supplier');
      expect(api._lastCall?.body).toEqual({ supplierId: 'sup-123' });
    });

    it('list_filing_cycles calls GET /api/form-sd/filing-cycles', async () => {
      await server.callTool('list_filing_cycles', {});
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/form-sd/filing-cycles');
    });

    it('get_form_sd_status calls GET /api/form-sd/batches/{id}/status', async () => {
      await server.callTool('get_form_sd_status', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/form-sd/batches/abc-123/status');
    });

    it('get_supply_chain_description calls GET /api/form-sd/batches/{id}/supply-chain', async () => {
      await server.callTool('get_supply_chain_description', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/form-sd/batches/abc-123/supply-chain');
    });

    it('get_due_diligence_summary calls GET /api/form-sd/batches/{id}/due-diligence', async () => {
      await server.callTool('get_due_diligence_summary', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/form-sd/batches/abc-123/due-diligence');
    });

    it('get_risk_assessment calls GET /api/form-sd/batches/{id}/risk-assessment', async () => {
      await server.callTool('get_risk_assessment', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/form-sd/batches/abc-123/risk-assessment');
    });

    it('generate_form_sd_package calls POST /api/form-sd/generate/{year}', async () => {
      await server.callTool('generate_form_sd_package', { reportingYear: 2026 });
      expect(api._lastCall?.method).toBe('POST');
      expect(api._lastCall?.path).toBe('/api/form-sd/generate/2026');
    });
  });

  // === NOTIFICATION TOOLS (3) ===
  describe('Notification Tools', () => {
    it('list_notifications calls GET /api/notifications', async () => {
      await server.callTool('list_notifications', {});
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/notifications');
    });

    it('mark_notification_read calls PATCH /api/notifications/{id}/read', async () => {
      await server.callTool('mark_notification_read', { notificationId: 'notif-123' });
      expect(api._lastCall?.method).toBe('PATCH');
      expect(api._lastCall?.path).toBe('/api/notifications/notif-123/read');
    });

    it('verify_batch_public calls GET /api/verify/{id}', async () => {
      await server.callTool('verify_batch_public', { batchId: 'abc-123' });
      expect(api._lastCall?.method).toBe('GET');
      expect(api._lastCall?.path).toBe('/api/verify/abc-123');
    });
  });

  // === TOOL REGISTRATION ===
  describe('Tool Registration', () => {
    it('registers all 32 customer tools', () => {
      const toolNames = Object.keys(server.tools);
      expect(toolNames.length).toBe(32);
    });

    it('all tools return content with text type', async () => {
      api._setMockResponse({ test: true });
      for (const [name, tool] of Object.entries(server.tools)) {
        const result = await tool.handler({
          batchId: 'test', eventId: 'test', documentId: 'test',
          notificationId: 'test', supplierId: 'test', query: 'test',
          page: 1, pageSize: 10, reportingYear: 2026,
          batchNumber: 'W-1', mineralType: 'T', originCountry: 'RW',
          originMine: 'M', weightKg: 1, eventType: 'MINE_EXTRACTION',
          eventDate: '2026-01-01', location: 'L', actorName: 'A',
          description: 'D', status: 'ACTIVE', recipientEmail: 'a@b.com',
          message: 'hi', splits: [], question: 'test',
        });
        expect(result.content).toBeDefined();
        expect(result.content[0].type).toBe('text');
      }
    });
  });
});
