using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Signup;

public static class CreateCheckoutSession
{
    public record Command(string CompanyName, string Name, string Email, string Plan = "PRO") : IRequest<Result<Response>>;
    public record Response(string CheckoutUrl);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }

    public class Handler(AppDbContext db, IConfiguration config)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var emailExists = await db.Users.AnyAsync(u => u.Email == cmd.Email, ct);
            if (emailExists)
                return Result<Response>.Failure($"Email '{cmd.Email}' is already in use");

            var priceId = PlanConfiguration.GetPriceId(cmd.Plan, config);
            var baseUrl = config["BaseUrl"] ?? "https://accutrac-web.onrender.com";

            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                CustomerEmail = cmd.Email,
                LineItems =
                [
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    }
                ],
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    TrialPeriodDays = 60,
                    Metadata = new Dictionary<string, string>
                    {
                        ["companyName"] = cmd.CompanyName,
                        ["adminName"] = cmd.Name,
                        ["adminEmail"] = cmd.Email,
                        ["plan"] = cmd.Plan,
                    }
                },
                SuccessUrl = $"{baseUrl}/signup/success",
                CancelUrl = $"{baseUrl}/signup",
                Metadata = new Dictionary<string, string>
                {
                    ["companyName"] = cmd.CompanyName,
                    ["adminName"] = cmd.Name,
                    ["adminEmail"] = cmd.Email,
                    ["plan"] = cmd.Plan,
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: ct);

            return Result<Response>.Success(new Response(session.Url));
        }
    }
}
