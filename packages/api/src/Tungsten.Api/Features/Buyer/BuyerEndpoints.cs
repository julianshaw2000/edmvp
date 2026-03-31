using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Buyer;

public static class BuyerEndpoints
{
    public static IEndpointRouteBuilder MapBuyerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/buyer")
            .RequireAuthorization(AuthorizationPolicies.RequireBuyer);

        group.MapGet("/supplier-engagement", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSupplierEngagement.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/nudge-supplier", async (NudgeSupplier.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { message = "Reminder sent" })
                : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
