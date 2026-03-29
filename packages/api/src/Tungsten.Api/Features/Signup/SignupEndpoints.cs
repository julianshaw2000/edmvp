using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Identity;
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

        app.MapPost("/api/stripe/webhook", async (HttpContext httpContext, AppDbContext db, UserManager<AppIdentityUser> userManager, IConfiguration config, ILogger<StripeWebhookHandler> logger, IEmailService emailService, CancellationToken ct) =>
        {
            var json = await new StreamReader(httpContext.Request.Body).ReadToEndAsync();
            var webhookSecret = config["Stripe:WebhookSecret"] ?? throw new InvalidOperationException("Stripe:WebhookSecret is not configured");

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

            var handler = new StripeWebhookHandler(db, userManager, logger, emailService, config);

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
                            session.Metadata.GetValueOrDefault("plan", "PRO"),
                            session.Id,
                            ct);
                    }
                    break;

                case Stripe.EventTypes.InvoicePaid:
                    var paidInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    var paidSubId = paidInvoice?.Parent?.SubscriptionDetails?.SubscriptionId;
                    if (paidSubId != null)
                        await handler.HandleInvoicePaid(paidSubId, ct);
                    break;

                case Stripe.EventTypes.InvoicePaymentFailed:
                    var failedInvoice = stripeEvent.Data.Object as Stripe.Invoice;
                    var failedSubId = failedInvoice?.Parent?.SubscriptionDetails?.SubscriptionId;
                    if (failedSubId != null)
                        await handler.HandlePaymentFailed(failedSubId, ct);
                    break;

                case Stripe.EventTypes.CustomerSubscriptionDeleted:
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    if (subscription?.Id != null)
                        await handler.HandleSubscriptionDeleted(subscription.Id, ct);
                    break;
            }

            return Results.Ok();
        }).DisableAntiforgery().WithMetadata(new RequestSizeLimitAttribute(1_048_576));

        app.MapGet("/api/signup/session/{sessionId}", async (
            string sessionId,
            AppDbContext db,
            CancellationToken ct) =>
            await GetSignupSessionStatus.Handle(sessionId, db, ct));

        app.MapPost("/api/signup/set-password", async (
            SetInitialPassword.Request request,
            UserManager<AppIdentityUser> userManager,
            IJwtTokenService jwtTokenService,
            AppDbContext db,
            HttpContext httpContext,
            CancellationToken ct) =>
            await SetInitialPassword.Handle(request, userManager, jwtTokenService, db, httpContext, ct));

        app.MapPost("/api/signup/resend-setup", async (
            ResendSetupEmail.Request request,
            AppDbContext db,
            IEmailService emailService,
            IConfiguration config,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
            await ResendSetupEmail.Handle(request, db, emailService, config, loggerFactory, ct));

        return app;
    }
}
