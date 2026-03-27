using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Identity;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Auth;

public static class Login
{
    public record Request(string Email, string Password);

    public static async Task<IResult> Handle(
        Request request,
        SignInManager<AppIdentityUser> signInManager,
        UserManager<AppIdentityUser> userManager,
        IJwtTokenService jwtTokenService,
        AppDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var identityUser = await userManager.FindByEmailAsync(request.Email);
        if (identityUser is null)
            return TypedResults.Json(new { error = "Invalid email or password." }, statusCode: 401);

        if (!identityUser.EmailConfirmed)
            return TypedResults.Json(new { error = "Please confirm your email before signing in." }, statusCode: 401);

        var result = await signInManager.CheckPasswordSignInAsync(
            identityUser, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
            return TypedResults.Json(new { error = "Account locked. Try again in 15 minutes." }, statusCode: 429);

        if (!result.Succeeded)
            return TypedResults.Json(new { error = "Invalid email or password." }, statusCode: 401);

        var appUser = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == identityUser.Id && u.IsActive, ct);

        if (appUser is null)
            return TypedResults.Json(new { error = "No active account found." }, statusCode: 403);

        var accessToken = jwtTokenService.GenerateAccessToken(identityUser.Id, identityUser.Email!);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        await jwtTokenService.SaveRefreshTokenAsync(identityUser.Id, refreshToken, ct);

        httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
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
