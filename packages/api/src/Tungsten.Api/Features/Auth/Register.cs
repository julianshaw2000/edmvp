using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Identity;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Auth;

public static class Register
{
    public record Request(string Email, string Password, string DisplayName);

    public static async Task<IResult> Handle(
        Request request, UserManager<AppIdentityUser> userManager,
        AppDbContext db, IEmailService emailService, IConfiguration config)
    {
        var appUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IdentityUserId.StartsWith("pending|"));

        if (appUser is null)
            return TypedResults.Json(new { error = "No invitation found for this email." }, statusCode: 400);

        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return TypedResults.Json(new { error = "An account with this email already exists." }, statusCode: 409);

        var identityUser = new AppIdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
            AppUserId = appUser.Id,
        };

        var result = await userManager.CreateAsync(identityUser, request.Password);
        if (!result.Succeeded)
            return TypedResults.Json(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) }, statusCode: 400);

        appUser.IdentityUserId = identityUser.Id;
        appUser.DisplayName = request.DisplayName;
        appUser.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var token = await userManager.GenerateEmailConfirmationTokenAsync(identityUser);
        var apiBaseUrl = config["ApiBaseUrl"] ?? "https://accutrac-api.onrender.com";
        var confirmUrl = $"{apiBaseUrl}/api/auth/confirm-email?userId={Uri.EscapeDataString(identityUser.Id)}&token={Uri.EscapeDataString(token)}";

        var html = $"""
            <h2>Confirm your email</h2>
            <p>Welcome to auditraks! Click below to confirm your email.</p>
            <p><a href="{confirmUrl}">Confirm Email</a></p>
            """;

        await emailService.SendAsync(request.Email, "Confirm your auditraks email", html,
            $"Confirm: {confirmUrl}", CancellationToken.None);

        return TypedResults.Ok(new { message = "Account created. Check your email to confirm." });
    }
}
