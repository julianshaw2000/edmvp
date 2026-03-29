using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Signup;

public static class GetSignupSessionStatus
{
    public record Response(bool Provisioned);

    public static async Task<IResult> Handle(
        string sessionId,
        AppDbContext db,
        CancellationToken ct)
    {
        // Fetch session from Stripe to get the customer email
        Stripe.Checkout.Session session;
        try
        {
            var service = new Stripe.Checkout.SessionService();
            session = await service.GetAsync(sessionId, cancellationToken: ct);
        }
        catch (Stripe.StripeException)
        {
            return TypedResults.NotFound(new { error = "Session not found." });
        }

        if (session.Status != "complete")
            return TypedResults.BadRequest(new { error = "Checkout session is not complete." });

        var email = session.CustomerDetails?.Email ?? session.CustomerEmail;
        if (string.IsNullOrEmpty(email))
            return TypedResults.BadRequest(new { error = "No email found on session." });

        var provisioned = await CheckProvisionedAsync(db, email, ct);
        return TypedResults.Ok(new Response(provisioned));
    }

    // Extracted for unit testing (bypasses Stripe API call)
    public static async Task<bool> CheckProvisionedAsync(AppDbContext db, string email, CancellationToken ct)
    {
        // Provisioned = UserEntity exists (webhook fired and created tenant + user).
        // The user still has pending| prefix — that's expected until they set a password.
        return await db.Users.AsNoTracking()
            .AnyAsync(u => u.Email == email, ct);
    }
}
