import { z } from 'zod';
export function registerAiTools(server, api) {
    server.tool('churn_prediction', 'Get churn risk analysis for tenants', {}, async () => {
        const data = await api.get('/api/ai/churn-prediction');
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('tenant_health', 'Get health scores for all tenants', {}, async () => {
        const data = await api.get('/api/ai/tenant-health');
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('revenue_summary', 'Get revenue breakdown and analysis', {}, async () => {
        const data = await api.get('/api/ai/revenue-summary');
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
    server.tool('natural_language_query', 'Ask a natural language question about platform data', {
        question: z.string().describe('Your question (e.g. "How many batches were created last month?")'),
    }, async ({ question }) => {
        const data = await api.post('/api/ai/query', { question });
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
