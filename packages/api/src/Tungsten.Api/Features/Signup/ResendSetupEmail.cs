using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Signup;

public static class ResendSetupEmail
{
    public record Request(string Email);

    public static async Task<IResult> Handle(
        Request request,
        AppDbContext db,
        IEmailService emailService,
        IConfiguration config,
        ILoggerFactory loggerFactory,
        CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking()
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IdentityUserId.StartsWith("pending|"), ct);

        // Always return 200 — no information leak about whether email exists
        if (user is null || string.IsNullOrEmpty(user.StripeSessionId))
            return TypedResults.Ok();

        var baseUrl = config["BaseUrl"] ?? "https://auditraks.com";
        var setupUrl = $"{baseUrl}/signup/set-password?session={Uri.EscapeDataString(user.StripeSessionId)}";
        var (subject, htmlBody, textBody) = EmailTemplates.AccountSetup(user.DisplayName, user.Tenant.Name, setupUrl);

        var logger = loggerFactory.CreateLogger(nameof(ResendSetupEmail));
        try { await emailService.SendAsync(user.Email, subject, htmlBody, textBody, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Failed to send setup email to {Email}", request.Email); }

        return TypedResults.Ok();
    }
}
