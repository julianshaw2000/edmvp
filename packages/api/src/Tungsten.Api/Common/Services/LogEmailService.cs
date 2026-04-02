namespace Tungsten.Api.Common.Services;

public class LogEmailService(ILogger<LogEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken ct)
    {
        logger.LogInformation("EMAIL: To={To}, Subject={Subject}, TextBody={TextBody}", to, subject, textBody[..Math.Min(200, textBody.Length)]);
        return Task.CompletedTask;
    }

    public Task SendWithAttachmentAsync(string to, string subject, string htmlBody, string textBody,
        string? attachmentFileName, byte[]? attachmentContent, CancellationToken ct)
    {
        logger.LogInformation("Email (attachment: {Attachment}) to {To}: {Subject}",
            attachmentFileName ?? "none", to, subject);
        return Task.CompletedTask;
    }
}
