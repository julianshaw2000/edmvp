using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class ConfirmEmail
{
    public static async Task<IResult> Handle(
        string userId, string token,
        UserManager<AppIdentityUser> userManager, IConfiguration config)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
            return TypedResults.Json(new { error = "Invalid confirmation link." }, statusCode: 400);

        var result = await userManager.ConfirmEmailAsync(user, token);
        if (!result.Succeeded)
            return TypedResults.Json(new { error = "Invalid or expired confirmation link." }, statusCode: 400);

        var baseUrl = config["BaseUrl"] ?? "https://auditraks.com";
        return TypedResults.Redirect($"{baseUrl}/login?emailConfirmed=true");
    }
}
