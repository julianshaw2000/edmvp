using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class ResendConfirmation
{
    public record Request(string Email);

    public static async Task<IResult> Handle(
        Request request, UserManager<AppIdentityUser> userManager,
        IEmailService emailService, IConfiguration config)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || user.EmailConfirmed)
            return TypedResults.Ok(new { message = "If that email exists and is unconfirmed, a new link has been sent." });

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var apiBaseUrl = config["ApiBaseUrl"] ?? "https://accutrac-api.onrender.com";
        var confirmUrl = $"{apiBaseUrl}/api/auth/confirm-email?userId={Uri.EscapeDataString(user.Id)}&token={Uri.EscapeDataString(token)}";

        var html = $"""
            <h2>Confirm your email</h2>
            <p>Click below to confirm your auditraks account.</p>
            <p><a href="{confirmUrl}">Confirm Email</a></p>
            """;

        await emailService.SendAsync(request.Email, "Confirm your auditraks email", html,
            $"Confirm: {confirmUrl}", CancellationToken.None);

        return TypedResults.Ok(new { message = "If that email exists and is unconfirmed, a new link has been sent." });
    }
}
