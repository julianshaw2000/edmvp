namespace Tungsten.Api.Common.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, string textBody, CancellationToken ct);
}
