using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Auth;

public static class Logout
{
    public static async Task<IResult> Handle(
        HttpContext httpContext,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        try
        {
            await jwtTokenService.RevokeAllUserTokensAsync(currentUser.IdentityUserId, ct);
        }
        catch (UnauthorizedAccessException) { }

        httpContext.Response.Cookies.Delete("refresh_token", new CookieOptions
        {
            Path = "/api/auth/refresh",
            Secure = true,
            SameSite = SameSiteMode.None,
        });

        return TypedResults.Ok(new { message = "Logged out." });
    }
}
