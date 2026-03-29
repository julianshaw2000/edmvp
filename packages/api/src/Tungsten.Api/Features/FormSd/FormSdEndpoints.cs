using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.FormSd;

public static class FormSdEndpoints
{
    public static IEndpointRouteBuilder MapFormSdEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/form-sd").RequireAuthorization();

        group.MapGet("/batches/{batchId:guid}/status", async (Guid batchId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetBatchFormSdStatus.Query(batchId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/batches/{batchId:guid}/supply-chain", async (Guid batchId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GenerateSupplyChainDescription.Query(batchId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/batches/{batchId:guid}/due-diligence", async (Guid batchId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GenerateDueDiligenceSummary.Query(batchId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/batches/{batchId:guid}/risk-assessment", async (Guid batchId, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GenerateRiskAssessment.Query(batchId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/filing-cycles", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListFilingCycles.Query(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        });

        group.MapPatch("/filing-cycles/{cycleId:guid}", async (Guid cycleId, UpdateFilingCycleStatus.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = command with { CycleId = cycleId };
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        group.MapPost("/generate/{reportingYear:int}", async (int reportingYear, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GenerateFormSdPackage.Command(reportingYear), ct);
            return result.IsSuccess
                ? Results.Created($"/api/form-sd/packages/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        return app;
    }
}
