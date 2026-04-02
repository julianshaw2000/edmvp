import { describe, it, expect, beforeEach } from 'vitest';
import { createMockApiClient } from '../../../shared/src/mock-api-client.js';
import { registerTenantTools } from '../tools/tenants.js';
import { registerUserTools } from '../tools/users.js';
import { registerAnalyticsTools } from '../tools/analytics.js';
import { registerAuditTools } from '../tools/audit.js';
import { registerRmapTools } from '../tools/rmap.js';
import { registerBatchTools } from '../tools/batches.js';
import { registerJobTools } from '../tools/jobs.js';
import { registerAiTools } from '../tools/ai.js';
import { registerEmailTools } from '../tools/email.js';
function createFakeServer() {
    const tools = {};
    return {
        tool(name, description, schema, handler) {
            tools[name] = { handler, description };
        },
        tools,
        async callTool(name, params = {}) {
            const tool = tools[name];
            if (!tool)
                throw new Error(`Tool ${name} not registered`);
            return tool.handler(params);
        },
    };
}
describe('Admin MCP Tools', () => {
    let server;
    let api;
    beforeEach(() => {
        server = createFakeServer();
        api = createMockApiClient();
        api._setMockResponse({ items: [], totalCount: 0 });
        registerTenantTools(server, api);
        registerUserTools(server, api);
        registerAnalyticsTools(server, api);
        registerAuditTools(server, api);
        registerRmapTools(server, api);
        registerBatchTools(server, api);
        registerJobTools(server, api);
        registerAiTools(server, api);
        registerEmailTools(server, api);
    });
    // === TENANT TOOLS (4) ===
    describe('Tenant Tools', () => {
        it('list_tenants calls GET /api/platform/tenants', async () => {
            await server.callTool('list_tenants', { page: 1, pageSize: 20 });
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toContain('/api/platform/tenants');
        });
        it('create_tenant calls POST /api/platform/tenants', async () => {
            await server.callTool('create_tenant', { name: 'Acme Corp' });
            expect(api._lastCall?.method).toBe('POST');
            expect(api._lastCall?.path).toBe('/api/platform/tenants');
            expect(api._lastCall?.body).toEqual({ name: 'Acme Corp' });
        });
        it('update_tenant_status calls PATCH /api/platform/tenants/{id}/status', async () => {
            await server.callTool('update_tenant_status', { tenantId: 't-123', status: 'SUSPENDED' });
            expect(api._lastCall?.method).toBe('PATCH');
            expect(api._lastCall?.path).toBe('/api/platform/tenants/t-123/status');
            expect(api._lastCall?.body).toEqual({ status: 'SUSPENDED' });
        });
        it('delete_tenant calls DELETE /api/platform/tenants/{id}', async () => {
            await server.callTool('delete_tenant', { tenantId: 't-123' });
            expect(api._lastCall?.method).toBe('DELETE');
            expect(api._lastCall?.path).toBe('/api/platform/tenants/t-123');
        });
    });
    // === USER TOOLS (4) ===
    describe('User Tools', () => {
        it('list_users calls GET /api/users', async () => {
            await server.callTool('list_users', {});
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/users');
        });
        it('list_users with tenantId calls GET /api/users?tenantId=', async () => {
            await server.callTool('list_users', { tenantId: 't-123' });
            expect(api._lastCall?.path).toContain('tenantId=t-123');
        });
        it('create_user calls POST /api/users', async () => {
            await server.callTool('create_user', { email: 'a@b.com', displayName: 'Test', role: 'SUPPLIER' });
            expect(api._lastCall?.method).toBe('POST');
            expect(api._lastCall?.path).toBe('/api/users');
        });
        it('update_user calls PATCH /api/users/{id}', async () => {
            await server.callTool('update_user', { userId: 'u-123', role: 'BUYER' });
            expect(api._lastCall?.method).toBe('PATCH');
            expect(api._lastCall?.path).toBe('/api/users/u-123');
        });
        it('delete_user calls DELETE /api/users/{id}', async () => {
            await server.callTool('delete_user', { userId: 'u-123' });
            expect(api._lastCall?.method).toBe('DELETE');
            expect(api._lastCall?.path).toBe('/api/users/u-123');
        });
    });
    // === ANALYTICS (1) ===
    describe('Analytics Tools', () => {
        it('get_analytics calls GET /api/analytics', async () => {
            await server.callTool('get_analytics', {});
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/analytics');
        });
        it('get_analytics with tenantId filters', async () => {
            await server.callTool('get_analytics', { tenantId: 't-123' });
            expect(api._lastCall?.path).toContain('tenantId=t-123');
        });
    });
    // === AUDIT TOOLS (2) ===
    describe('Audit Tools', () => {
        it('list_audit_logs calls GET /api/admin/audit-logs', async () => {
            await server.callTool('list_audit_logs', { page: 1, pageSize: 20 });
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toContain('/api/admin/audit-logs');
        });
        it('list_audit_logs passes filters', async () => {
            await server.callTool('list_audit_logs', { page: 1, pageSize: 10, action: 'CreateBatch', entityType: 'Batch' });
            expect(api._lastCall?.path).toContain('action=CreateBatch');
            expect(api._lastCall?.path).toContain('entityType=Batch');
        });
        it('export_audit_logs calls GET /api/admin/audit-logs/export', async () => {
            await server.callTool('export_audit_logs', {});
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toContain('/api/admin/audit-logs/export');
        });
    });
    // === RMAP TOOLS (2) ===
    describe('RMAP Tools', () => {
        it('list_rmap_smelters calls GET /api/admin/rmap', async () => {
            await server.callTool('list_rmap_smelters', {});
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/admin/rmap');
        });
        it('search_smelters calls GET /api/smelters with query', async () => {
            await server.callTool('search_smelters', { query: 'Wolfram', pageSize: 10 });
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toContain('/api/smelters');
            expect(api._lastCall?.path).toContain('q=Wolfram');
        });
    });
    // === BATCH TOOLS (5) ===
    describe('Batch Tools', () => {
        it('list_batches calls GET /api/batches', async () => {
            await server.callTool('list_batches', { page: 1, pageSize: 20 });
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toContain('/api/batches');
        });
        it('get_batch calls GET /api/batches/{id}', async () => {
            await server.callTool('get_batch', { batchId: 'abc-123' });
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/batches/abc-123');
        });
        it('get_batch_compliance calls GET /api/batches/{id}/compliance', async () => {
            await server.callTool('get_batch_compliance', { batchId: 'abc-123' });
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/batches/abc-123/compliance');
        });
        it('get_batch_activity calls GET /api/batches/{id}/activity', async () => {
            await server.callTool('get_batch_activity', { batchId: 'abc-123' });
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/batches/abc-123/activity');
        });
        it('list_events calls GET /api/batches/{id}/events', async () => {
            await server.callTool('list_events', { batchId: 'abc-123', page: 1, pageSize: 50 });
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toContain('/api/batches/abc-123/events');
        });
    });
    // === JOB TOOLS (1) ===
    describe('Job Tools', () => {
        it('list_jobs calls GET /api/admin/jobs', async () => {
            await server.callTool('list_jobs', {});
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/admin/jobs');
        });
    });
    // === AI TOOLS (4) ===
    describe('AI Tools', () => {
        it('churn_prediction calls GET /api/ai/churn-prediction', async () => {
            await server.callTool('churn_prediction', {});
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/ai/churn-prediction');
        });
        it('tenant_health calls GET /api/ai/tenant-health', async () => {
            await server.callTool('tenant_health', {});
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/ai/tenant-health');
        });
        it('revenue_summary calls GET /api/ai/revenue-summary', async () => {
            await server.callTool('revenue_summary', {});
            expect(api._lastCall?.method).toBe('GET');
            expect(api._lastCall?.path).toBe('/api/ai/revenue-summary');
        });
        it('natural_language_query calls POST /api/ai/query', async () => {
            await server.callTool('natural_language_query', { question: 'How many batches?' });
            expect(api._lastCall?.method).toBe('POST');
            expect(api._lastCall?.path).toBe('/api/ai/query');
            expect(api._lastCall?.body).toEqual({ question: 'How many batches?' });
        });
    });
    // === EMAIL TOOLS (1) ===
    describe('Email Tools', () => {
        it('send_email calls POST /api/admin/send-email', async () => {
            await server.callTool('send_email', {
                recipientEmail: 'test@example.com',
                subject: 'Test Subject',
                body: 'Hello, this is a test email.',
            });
            expect(api._lastCall?.method).toBe('POST');
            expect(api._lastCall?.path).toBe('/api/admin/send-email');
            expect(api._lastCall?.body).toEqual({
                recipientEmail: 'test@example.com',
                subject: 'Test Subject',
                body: 'Hello, this is a test email.',
                attachmentFileName: null,
                attachmentBase64: null,
            });
        });
    });
    // === TOOL REGISTRATION ===
    describe('Tool Registration', () => {
        it('registers all 23 admin tools', () => {
            const toolNames = Object.keys(server.tools);
            expect(toolNames.length).toBe(24);
        });
        it('all tools return content with text type', async () => {
            api._setMockResponse({ test: true });
            for (const [name, tool] of Object.entries(server.tools)) {
                const result = await tool.handler({
                    tenantId: 'test', userId: 'test', batchId: 'test',
                    page: 1, pageSize: 10, name: 'Test', email: 'a@b.com',
                    displayName: 'T', role: 'SUPPLIER', status: 'ACTIVE',
                    isActive: true, action: 'x', entityType: 'x',
                    query: 'x', question: 'x', from: '2026-01-01', to: '2026-12-31',
                    recipientEmail: 'a@b.com', subject: 'Test', body: 'Test',
                });
                expect(result.content).toBeDefined();
                expect(result.content[0].type).toBe('text');
            }
        });
    });
});
