import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient, SupplierEngagement } from '../../../shared/src/index.js';

export function registerEngagementTools(server: McpServer, api: AuditraksApiClient) {
  server.tool('get_supplier_engagement', 'Get supplier engagement metrics (buyer role)', {}, async () => {
    const data = await api.get<SupplierEngagement>('/api/buyer/supplier-engagement');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('nudge_supplier', 'Send a reminder email to a supplier (buyer role, 7-day rate limit)', {
    supplierId: z.string().describe('Supplier user ID (UUID)'),
  }, async ({ supplierId }) => {
    const data = await api.post('/api/buyer/nudge-supplier', { supplierId });
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('list_filing_cycles', 'List Form SD filing cycles', {}, async () => {
    const data = await api.get('/api/form-sd/filing-cycles');
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_form_sd_status', 'Check if a batch is ready for Form SD filing (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/form-sd/batches/${batchId}/status`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_supply_chain_description', 'Generate AI supply chain narrative for a batch (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/form-sd/batches/${batchId}/supply-chain`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_due_diligence_summary', 'Generate AI due diligence summary for a batch (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/form-sd/batches/${batchId}/due-diligence`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('get_risk_assessment', 'Generate AI risk assessment for a batch (buyer role)', {
    batchId: z.string().describe('Batch ID (UUID)'),
  }, async ({ batchId }) => {
    const data = await api.get(`/api/form-sd/batches/${batchId}/risk-assessment`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });

  server.tool('generate_form_sd_package', 'Generate Form SD support package for a reporting year (buyer role)', {
    reportingYear: z.number().describe('Reporting year (e.g. 2026)'),
  }, async ({ reportingYear }) => {
    const data = await api.post(`/api/form-sd/generate/${reportingYear}`);
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
