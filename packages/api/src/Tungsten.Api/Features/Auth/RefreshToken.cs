using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Identity;

namespace Tungsten.Api.Features.Auth;

public static class RefreshToken
{
    public static async Task<IResult> Handle(
        HttpContext httpContext,
        IJwtTokenService jwtTokenService,
        UserManager<AppIdentityUser> userManager,
        CancellationToken ct)
    {
        var oldRefreshToken = httpContext.Request.Cookies["refresh_token"];
        if (string.IsNullOrEmpty(oldRefreshToken))
            return TypedResults.Json(new { error = "No refresh token." }, statusCode: 401);

        var stored = await jwtTokenService.ValidateRefreshTokenAsync(oldRefreshToken, ct);
        if (stored is null)
            return TypedResults.Json(new { error = "Invalid or expired refresh token." }, statusCode: 401);

        await jwtTokenService.RevokeRefreshTokenAsync(stored.TokenHash, ct);

        var identityUser = await userManager.FindByIdAsync(stored.IdentityUserId);
        if (identityUser is null)
            return TypedResults.Json(new { error = "User not found." }, statusCode: 401);

        var accessToken = jwtTokenService.GenerateAccessToken(identityUser.Id, identityUser.Email!);
        var newRefreshToken = jwtTokenService.GenerateRefreshToken();
        await jwtTokenService.SaveRefreshTokenAsync(identityUser.Id, newRefreshToken, ct);

        httpContext.Response.Cookies.Append("refresh_token", newRefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/api/auth/refresh",
            MaxAge = TimeSpan.FromDays(14),
        });

        return TypedResults.Ok(new { accessToken });
    }
}
