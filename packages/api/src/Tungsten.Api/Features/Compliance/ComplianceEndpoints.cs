using MediatR;

namespace Tungsten.Api.Features.Compliance;

public static class ComplianceEndpoints
{
    public static IEndpointRouteBuilder MapComplianceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/batches/{batchId:guid}/compliance", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetBatchCompliance.Query(batchId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        app.MapGet("/api/events/{eventId:guid}/compliance", async (Guid eventId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetEventCompliance.Query(eventId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        return app;
    }
}
