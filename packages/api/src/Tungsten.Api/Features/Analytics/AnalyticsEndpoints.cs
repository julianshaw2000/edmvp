using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Analytics;

public static class AnalyticsEndpoints
{
    public static IEndpointRouteBuilder MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/analytics", async (IMediator mediator, ILogger<GetAnalytics.Query> logger, CancellationToken ct) =>
        {
            try
            {
                var result = await mediator.Send(new GetAnalytics.Query(), ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : Results.BadRequest(new { error = result.Error });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Analytics endpoint failed");
                return Results.Json(new { error = $"{ex.GetType().Name}: {ex.Message}" }, statusCode: 500);
            }
        }).RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        return app;
    }
}
