export function registerJobTools(server, api) {
    server.tool('list_jobs', 'List background jobs and their status', {}, async () => {
        const data = await api.get('/api/admin/jobs');
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
