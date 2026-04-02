using MediatR;
using Tungsten.Api.Common;
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
        IEmailService emailService,
        ILogger<Handler> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken ct)
        {
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

            logger.LogInformation("Admin email sent to {Recipient}: {Subject}",
                request.RecipientEmail, request.Subject);

            return Result.Success();
        }
    }
}
