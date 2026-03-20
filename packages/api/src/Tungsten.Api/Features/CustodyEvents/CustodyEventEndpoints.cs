using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.CustodyEvents;

public static class CustodyEventEndpoints
{
    public static IEndpointRouteBuilder MapCustodyEventEndpoints(this IEndpointRouteBuilder app)
    {
        // Events nested under batches
        var batchEvents = app.MapGroup("/api/batches/{batchId:guid}/events").RequireAuthorization();

        batchEvents.MapGet("/", async (Guid batchId, int? page, int? pageSize, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListCustodyEvents.Query(batchId, page ?? 1, pageSize ?? 20));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        batchEvents.MapPost("/", async (Guid batchId, CreateCustodyEvent.Command command, IMediator mediator) =>
        {
            var cmd = command with { BatchId = batchId };
            var result = await mediator.Send(cmd);
            if (result.IsSuccess)
                return Results.Created($"/api/events/{result.Value.Id}", result.Value);
            if (result.Error?.Contains("Duplicate") == true)
                return Results.Conflict(new { error = result.Error });
            return Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireSupplier);

        // Event detail
        var events = app.MapGroup("/api/events").RequireAuthorization();

        events.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCustodyEvent.Query(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        });

        // Corrections
        events.MapPost("/{eventId:guid}/corrections", async (Guid eventId, CreateCorrection.Command command, IMediator mediator) =>
        {
            var cmd = command with { OriginalEventId = eventId };
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/events/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireSupplier);

        // Integrity verification
        app.MapGet("/api/batches/{batchId:guid}/verify-integrity", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new VerifyIntegrity.Query(batchId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        return app;
    }
}
