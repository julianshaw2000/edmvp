using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Analytics;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analytics", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetAnalytics.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        return app;
    }
}
