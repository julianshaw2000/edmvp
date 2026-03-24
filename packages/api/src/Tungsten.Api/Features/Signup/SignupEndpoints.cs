using MediatR;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Signup;

public static class SignupEndpoints
{
    public static IEndpointRouteBuilder MapSignupEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/signup/checkout", async (CreateCheckoutSession.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }); // Rate limiting temporarily disabled for testing

        app.MapPost("/api/stripe/webhook", async (HttpContext httpContext, AppDbContext db, IConfiguration config, ILogger<StripeWebhookHandler> logger, IEmailService emailService) =>
        {
            var json = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            var webhookSecret = config["Stripe:WebhookSecret"];

            Stripe.Event stripeEvent;
            try
            {
                stripeEvent = Stripe.EventUtility.ConstructEvent(
                    json, httpContext.Request.Headers["Stripe-Signature"]!, webhookSecret);
            }
            catch (Stripe.StripeException)
            {
                return Results.BadRequest("Invalid signature");
            }

            var handler = new StripeWebhookHandler(db, logger, emailService, config);

            switch (stripeEvent.Type)
            {
                case Stripe.EventTypes.CheckoutSessionCompleted:
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session?.Metadata != null)
                    {
                        await handler.HandleCheckoutCompleted(
                            session.CustomerId ?? session.Customer?.Id ?? "",
                            session.SubscriptionId ?? session.Subscription?.Id ?? "",
                            session.Metadata.GetValueOrDefault("companyName", ""),
                            session.Metadata.GetValueOrDefault("adminName", ""),
                            session.Metadata.GetValueOrDefault("adminEmail", ""),
                            session.Metadata.GetValueOrDefault("plan", "PRO"));
                    }
                    break;

                case Stripe.EventTypes.InvoicePaid:
                    var paidInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    var paidSubId = paidInvoice?.Parent?.SubscriptionDetails?.SubscriptionId;
                    if (paidSubId != null)
                        await handler.HandleInvoicePaid(paidSubId);
                    break;

                case Stripe.EventTypes.InvoicePaymentFailed:
                    var failedInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    var failedSubId = failedInvoice?.Parent?.SubscriptionDetails?.SubscriptionId;
                    if (failedSubId != null)
                        await handler.HandlePaymentFailed(failedSubId);
                    break;

                case Stripe.EventTypes.CustomerSubscriptionDeleted:
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    if (subscription?.Id != null)
                        await handler.HandleSubscriptionDeleted(subscription.Id);
                    break;
            }

            return Results.Ok();
        }).DisableAntiforgery();

        return app;
    }
}
