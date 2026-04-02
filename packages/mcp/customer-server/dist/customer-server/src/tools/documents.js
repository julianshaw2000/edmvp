import { z } from 'zod';
export function registerDocumentTools(server, api) {
    server.tool('list_batch_documents', 'List documents for a batch', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.get(`/api/batches/${batchId}/documents`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('generate_passport', 'Generate a Material Passport PDF for a compliant batch', {
        batchId: z.string().describe('Batch ID (must be COMPLIANT)'),
    }, async ({ batchId }) => {
        const data = await api.post(`/api/batches/${batchId}/passport`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('share_document', 'Create a 30-day shareable link for a generated document', {
        documentId: z.string().describe('Generated document ID (UUID)'),
    }, async ({ documentId }) => {
        const data = await api.post(`/api/generated-documents/${documentId}/share`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('share_document_email', 'Email a generated document to a recipient', {
        documentId: z.string().describe('Generated document ID (UUID)'),
        recipientEmail: z.string().describe('Recipient email address'),
        message: z.string().optional().describe('Optional message to include'),
    }, async ({ documentId, ...body }) => {
        const data = await api.post(`/api/generated-documents/${documentId}/share-email`, body);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('generate_dossier', 'Generate an Audit Dossier PDF for a batch (buyer role)', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.post(`/api/batches/${batchId}/dossier`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('generate_dpp', 'Generate a Digital Product Passport (JSON-LD) for a batch (buyer role)', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.post(`/api/batches/${batchId}/dpp`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('list_generated_documents', 'List all generated documents for a batch', {
        batchId: z.string().describe('Batch ID (UUID)'),
    }, async ({ batchId }) => {
        const data = await api.get(`/api/generated-documents?batchId=${batchId}`);
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
