using MediatR;

namespace Tungsten.Api.Features.Billing;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/billing/portal", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new CreateBillingPortalSession.Command(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization();

        return app;
    }
}
