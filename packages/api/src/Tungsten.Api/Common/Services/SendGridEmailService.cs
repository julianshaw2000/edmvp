namespace Tungsten.Api.Common.Services;

using SendGrid;
using SendGrid.Helpers.Mail;

public sealed class SendGridEmailService(
    IConfiguration configuration,
    ILogger<SendGridEmailService> logger) : IEmailService
{
    public async Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken ct)
    {
        var apiKey = configuration["SendGrid:ApiKey"]
            ?? throw new InvalidOperationException("SendGrid:ApiKey not configured");
        var fromEmail = configuration["SendGrid:FromEmail"] ?? "noreply@auditraks.com";
        var fromName = configuration["SendGrid:FromName"] ?? "auditraks";

        var client = new SendGridClient(apiKey);
        var msg = MailHelper.CreateSingleEmail(
            new EmailAddress(fromEmail, fromName),
            new EmailAddress(to),
            subject,
            textBody,
            htmlBody);

        var response = await client.SendEmailAsync(msg, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            logger.LogError("SendGrid failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"SendGrid returned {response.StatusCode}");
        }

        logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }
}
