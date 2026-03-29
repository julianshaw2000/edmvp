using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Identity;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Signup;

public static class SetInitialPassword
{
    public record Request(string SessionId, string Password);

    public class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.SessionId).NotEmpty();
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        }
    }

    public static async Task<IResult> Handle(
        Request request,
        UserManager<AppIdentityUser> userManager,
        IJwtTokenService jwtTokenService,
        AppDbContext db,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var validator = new Validator();
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return TypedResults.Json(new { error = string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)) }, statusCode: 400);

        // Get email from Stripe (source of truth — never trust client-supplied email)
        Stripe.Checkout.Session session;
        try
        {
            var service = new Stripe.Checkout.SessionService();
            session = await service.GetAsync(request.SessionId, cancellationToken: ct);
        }
        catch (Stripe.StripeException)
        {
            return TypedResults.Json(new { error = "Invalid session." }, statusCode: 400);
        }

        var email = session.CustomerDetails?.Email ?? session.CustomerEmail;
        if (string.IsNullOrEmpty(email))
            return TypedResults.Json(new { error = "No email found on session." }, statusCode: 400);

        return await HandleCoreAsync(email, request.Password, db, userManager, jwtTokenService, httpContext, ct);
    }

    // Extracted for unit testing (bypasses Stripe API call)
    internal static async Task<IResult> HandleCoreAsync(
        string email,
        string password,
        AppDbContext db,
        UserManager<AppIdentityUser> userManager,
        IJwtTokenService jwtTokenService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var appUser = await db.Users
            .FirstOrDefaultAsync(u => u.Email == email && u.IdentityUserId.StartsWith("pending|"), ct);

        if (appUser is null)
            return TypedResults.Json(new { error = "No pending account found for this email." }, statusCode: 400);

        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
            return TypedResults.Json(new { error = "Account already set up. Please sign in." }, statusCode: 409);

        var identityUser = new AppIdentityUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,  // Stripe verified the address
            AppUserId = appUser.Id,
        };

        var createResult = await userManager.CreateAsync(identityUser, password);
        if (!createResult.Succeeded)
            return TypedResults.Json(
                new { error = string.Join(" ", createResult.Errors.Select(e => e.Description)) },
                statusCode: 400);

        appUser.IdentityUserId = identityUser.Id;
        appUser.StripeSessionId = null;
        appUser.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var accessToken = jwtTokenService.GenerateAccessToken(identityUser.Id, email);
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
