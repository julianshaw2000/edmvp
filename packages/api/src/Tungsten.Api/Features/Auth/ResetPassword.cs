using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class ResetPassword
{
    public record Request(string Email, string Token, string NewPassword);

    public static async Task<IResult> Handle(Request request, UserManager<AppIdentityUser> userManager)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return TypedResults.Json(new { error = "Invalid reset link." }, statusCode: 400);

        var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!result.Succeeded)
            return TypedResults.Json(new { error = string.Join(" ", result.Errors.Select(e => e.Description)) }, statusCode: 400);

        return TypedResults.Ok(new { message = "Password reset. You can now sign in." });
    }
}
