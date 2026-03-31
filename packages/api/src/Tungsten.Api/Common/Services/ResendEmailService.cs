namespace Tungsten.Api.Common.Services;

using Resend;

public sealed class ResendEmailService(
    IConfiguration configuration,
    IResend resend,
    ILogger<ResendEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken ct)
    {
        var fromEmail = configuration["Resend:FromEmail"] ?? "noreply@auditraks.com";

        var replyTo = configuration["Resend:ReplyToEmail"] ?? "support@auditraks.com";

        var message = new EmailMessage
        {
            From = fromEmail,
            Subject = subject,
            HtmlBody = htmlBody,
            TextBody = textBody,
        };
        message.To.Add(to);
        message.ReplyTo ??= [];
        message.ReplyTo.Add(replyTo);

        await resend.EmailSendAsync(message, ct);

        logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}
