# Admin Send Email MCP Tool — Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a `POST /api/admin/send-email` API endpoint and corresponding `send_email` MCP tool so the platform admin can send correspondence to any user from `support@auditraks.com`, with optional file attachment.

**Architecture:** New MediatR handler in the API sends email via existing `IEmailService` (Resend). Resend supports attachments via base64-encoded content. The MCP tool sends the email body and optional attachment (base64) to the API. The API validates, sends via Resend, and logs to audit.

**Tech Stack:** .NET 10 MediatR, Resend SDK (attachments), TypeScript MCP tool

---

## File Structure

### Backend (new + modified)
- `packages/api/src/Tungsten.Api/Features/Admin/SendEmail.cs` — MediatR handler
- `packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs` — Register endpoint
- `packages/api/src/Tungsten.Api/Common/Services/ResendEmailService.cs` — Add attachment support

### MCP (modified)
- `packages/mcp/admin-server/src/tools/email.ts` — New tool file
- `packages/mcp/admin-server/src/index.ts` — Register email tools

---

## Task 1: Add attachment support to ResendEmailService

**Files:**
- Modify: `packages/api/src/Tungsten.Api/Common/Services/IEmailService.cs`
- Modify: `packages/api/src/Tungsten.Api/Common/Services/ResendEmailService.cs`
- Modify: `packages/api/src/Tungsten.Api/Common/Services/LogEmailService.cs`

- [ ] **Step 1: Add SendWithAttachmentAsync to IEmailService**

Read `packages/api/src/Tungsten.Api/Common/Services/IEmailService.cs` and add a new method:

```csharp
Task SendWithAttachmentAsync(string to, string subject, string htmlBody, string textBody,
    string? attachmentFileName, byte[]? attachmentContent, CancellationToken ct);
```

- [ ] **Step 2: Implement in ResendEmailService**

Read `packages/api/src/Tungsten.Api/Common/Services/ResendEmailService.cs` and add the new method:

```csharp
public async Task SendWithAttachmentAsync(string to, string subject, string htmlBody, string textBody,
    string? attachmentFileName, byte[]? attachmentContent, CancellationToken ct)
{
    var fromEmail = configuration["Resend:FromEmail"] ?? "noreply@auditraks.com";
    var replyTo = configuration["Resend:ReplyToEmail"] ?? "support@auditraks.com";

    var message = new EmailMessage
    {
        From = $"auditraks Support <{replyTo}>",
        Subject = subject,
        HtmlBody = htmlBody,
        TextBody = textBody,
    };
    message.To.Add(to);
    message.ReplyTo ??= [];
    message.ReplyTo.Add(replyTo);

    if (attachmentFileName is not null && attachmentContent is not null)
    {
        message.Attachments.Add(new EmailAttachment
        {
            Filename = attachmentFileName,
            Content = Convert.ToBase64String(attachmentContent),
        });
    }

    await resend.EmailSendAsync(message, ct);
    logger.LogInformation("Email with attachment sent to {To}: {Subject}", to, subject);
}
```

Note: Check the Resend SDK for the exact `EmailAttachment` class — it may be `Attachment` instead. Read the `Resend` namespace types available. The key properties are `Filename` and `Content` (base64 string).

- [ ] **Step 3: Implement in LogEmailService (fallback)**

Read `packages/api/src/Tungsten.Api/Common/Services/LogEmailService.cs` and add:

```csharp
public Task SendWithAttachmentAsync(string to, string subject, string htmlBody, string textBody,
    string? attachmentFileName, byte[]? attachmentContent, CancellationToken ct)
{
    // Log-only fallback — same as SendAsync but note the attachment
    logger.LogInformation("Email (with attachment {Attachment}) to {To}: {Subject}",
        attachmentFileName ?? "none", to, subject);
    return Task.CompletedTask;
}
```

- [ ] **Step 4: Build**

```bash
cd packages/api && dotnet build
```

- [ ] **Step 5: Commit**

```bash
git add packages/api/src/Tungsten.Api/Common/Services/
git commit -m "feat: add attachment support to IEmailService

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 2: Create SendEmail handler and endpoint

**Files:**
- Create: `packages/api/src/Tungsten.Api/Features/Admin/SendEmail.cs`
- Modify: `packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs`

- [ ] **Step 1: Create the handler**

Create `packages/api/src/Tungsten.Api/Features/Admin/SendEmail.cs`:

```csharp
using MediatR;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;

namespace Tungsten.Api.Features.Admin;

public static class SendEmail
{
    public record Command(
        string RecipientEmail,
        string Subject,
        string Body,
        string? AttachmentFileName,
        string? AttachmentBase64) : IRequest<Result>;

    public class Handler(
        ICurrentUserService currentUser,
        IEmailService emailService,
        ILogger<Handler> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken ct)
        {
            var role = await currentUser.GetRoleAsync(ct);
            if (role != Roles.Admin)
                return Result.Failure("Only platform admins can send emails");

            if (string.IsNullOrWhiteSpace(request.RecipientEmail))
                return Result.Failure("Recipient email is required");
            if (string.IsNullOrWhiteSpace(request.Subject))
                return Result.Failure("Subject is required");
            if (string.IsNullOrWhiteSpace(request.Body))
                return Result.Failure("Body is required");

            var htmlBody = $"""
                <div style="font-family: system-ui, sans-serif; max-width: 600px; margin: 0 auto; padding: 40px 20px;">
                    {request.Body.Replace("\n", "<br/>")}
                    <hr style="border: none; border-top: 1px solid #e2e8f0; margin: 24px 0;" />
                    <p style="color: #94a3b8; font-size: 12px;">&copy; 2026 auditraks. Tungsten supply chain compliance, automated.</p>
                </div>
                """;

            byte[]? attachmentContent = null;
            if (!string.IsNullOrEmpty(request.AttachmentBase64))
            {
                try
                {
                    attachmentContent = Convert.FromBase64String(request.AttachmentBase64);
                }
                catch
                {
                    return Result.Failure("Invalid attachment: not valid base64");
                }
            }

            await emailService.SendWithAttachmentAsync(
                request.RecipientEmail,
                request.Subject,
                htmlBody,
                request.Body,
                request.AttachmentFileName,
                attachmentContent,
                ct);

            logger.LogInformation("Admin email sent to {Recipient}: {Subject}", request.RecipientEmail, request.Subject);

            return Result.Success();
        }
    }
}
```

- [ ] **Step 2: Register the endpoint**

Read `packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs` and add:

```csharp
group.MapPost("/send-email", async (SendEmail.Command command, IMediator mediator, CancellationToken ct) =>
{
    var result = await mediator.Send(command, ct);
    return result.IsSuccess
        ? Results.Ok(new { message = "Email sent" })
        : Results.BadRequest(new { error = result.Error });
}).RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin);
```

Note: Check how the admin endpoints group is set up — it may use `RequireAdmin` or `RequirePlatformAdmin`. Use `RequirePlatformAdmin` since only platform admin should send emails on behalf of the platform.

- [ ] **Step 3: Build and test**

```bash
cd packages/api && dotnet build && dotnet test
```

- [ ] **Step 4: Commit**

```bash
git add packages/api/src/Tungsten.Api/Features/Admin/SendEmail.cs packages/api/src/Tungsten.Api/Features/Admin/AdminEndpoints.cs
git commit -m "feat: add POST /api/admin/send-email endpoint for platform admin

Sends email from support@auditraks.com with optional base64 attachment.
Platform admin only.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 3: Add send_email MCP tool

**Files:**
- Create: `packages/mcp/admin-server/src/tools/email.ts`
- Modify: `packages/mcp/admin-server/src/index.ts`

- [ ] **Step 1: Create email tool file**

Create `packages/mcp/admin-server/src/tools/email.ts`:

```typescript
import { z } from 'zod';
import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import type { AuditraksApiClient } from '../../../shared/src/index.js';

export function registerEmailTools(server: McpServer, api: AuditraksApiClient) {
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
    return { content: [{ type: 'text' as const, text: JSON.stringify(data, null, 2) }] };
  });
}
```

- [ ] **Step 2: Register in admin index.ts**

Read `packages/mcp/admin-server/src/index.ts` and add:

Import:
```typescript
import { registerEmailTools } from './tools/email.js';
```

Registration (after existing register calls):
```typescript
registerEmailTools(server, api);
```

- [ ] **Step 3: Build**

```bash
cd packages/mcp/admin-server && npx tsc
```

- [ ] **Step 4: Commit**

```bash
git add packages/mcp/admin-server/src/tools/email.ts packages/mcp/admin-server/src/index.ts
git commit -m "feat: add send_email tool to admin MCP server

Sends email from support@auditraks.com with optional attachment.

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
```

---

## Task 4: Add test and push

**Files:**
- Modify: `packages/mcp/admin-server/src/__tests__/tools.test.ts`

- [ ] **Step 1: Add email tool test**

Read `packages/mcp/admin-server/src/__tests__/tools.test.ts` and add:

Import at the top:
```typescript
import { registerEmailTools } from '../tools/email.js';
```

In the `beforeEach`, add:
```typescript
registerEmailTools(server as any, api);
```

Add a new describe block:
```typescript
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
```

Update the tool count assertion from `23` to `24`.

- [ ] **Step 2: Run tests**

```bash
cd packages/mcp/admin-server && npx vitest run
```

- [ ] **Step 3: Commit and push**

```bash
git add packages/mcp/admin-server/
git commit -m "test: add send_email tool test to admin MCP

Co-Authored-By: Claude Opus 4.6 (1M context) <noreply@anthropic.com>"
git push origin main
```
