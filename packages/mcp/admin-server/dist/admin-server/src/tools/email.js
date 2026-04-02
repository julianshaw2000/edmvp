import { z } from 'zod';
export function registerEmailTools(server, api) {
    server.tool('send_email', 'Send an email from support@auditraks.com to any recipient (platform admin only)', {
        recipientEmail: z.string().describe('Recipient email address'),
        subject: z.string().describe('Email subject line'),
        body: z.string().describe('Email body (plain text — newlines converted to <br/> in HTML)'),
        attachmentFileName: z.string().optional().describe('Attachment file name (e.g. report.pdf)'),
        attachmentBase64: z.string().optional().describe('Attachment content as base64-encoded string'),
    }, async ({ recipientEmail, subject, body, attachmentFileName, attachmentBase64 }) => {
        const data = await api.post('/api/admin/send-email', {
            recipientEmail,
            subject,
            body,
            attachmentFileName: attachmentFileName ?? null,
            attachmentBase64: attachmentBase64 ?? null,
        });
        return { content: [{ type: 'text', text: JSON.stringify(data, null, 2) }] };
    });
}
