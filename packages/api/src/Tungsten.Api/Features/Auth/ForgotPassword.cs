using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class ForgotPassword
{
    public record Request(string Email);

    public static async Task<IResult> Handle(
        Request request,
        UserManager<AppIdentityUser> userManager,
        IEmailService emailService,
        IConfiguration config)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.EmailConfirmed)
            return TypedResults.Ok(new { message = "If that email exists, a reset link has been sent." });

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var baseUrl = config["BaseUrl"] ?? "https://auditraks.com";
        var resetUrl = $"{baseUrl}/reset-password?email={Uri.EscapeDataString(request.Email)}&token={Uri.EscapeDataString(token)}";

        var html = $"""
            <h2>Reset your password</h2>
            <p>Click the link below to reset your auditraks password. This link expires in 24 hours.</p>
            <p><a href="{resetUrl}">Reset Password</a></p>
            <p>If you didn't request this, ignore this email.</p>
            """;

        await emailService.SendAsync(request.Email, "Reset your auditraks password", html,
            $"Reset your password: {resetUrl}", CancellationToken.None);

        return TypedResults.Ok(new { message = "If that email exists, a reset link has been sent." });
    }
}
