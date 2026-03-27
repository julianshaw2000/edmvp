using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Batches;

public static class BatchEndpoints
{
    public static IEndpointRouteBuilder MapBatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/batches").RequireAuthorization();

        group.MapPost("/", async (CreateBatch.Command command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/batches/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireSupplierOrAdmin);

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetBatch.Query(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        });

        group.MapGet("/", async (int? page, int? pageSize, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListBatches.Query(page ?? 1, pageSize ?? 20));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPatch("/{id:guid}/status", async (Guid id, UpdateBatchStatus.Command command, IMediator mediator) =>
        {
            var cmd = command with { BatchId = id };
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireSupplierOrAdmin);

        group.MapPost("/{id:guid}/split", async (Guid id, SplitBatch.Command command, IMediator mediator) =>
        {
            var cmd = command with { BatchId = id };
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireSupplierOrAdmin);

        group.MapGet("/{id:guid}/activity", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBatchActivity.Query(id), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
