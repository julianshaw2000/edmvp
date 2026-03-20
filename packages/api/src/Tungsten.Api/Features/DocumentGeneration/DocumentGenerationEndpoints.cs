using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.DocumentGeneration;

public static class DocumentGenerationEndpoints
{
    public static IEndpointRouteBuilder MapDocumentGenerationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/batches/{batchId:guid}/passport", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GeneratePassport.Command(batchId));
            return result.IsSuccess
                ? Results.Created($"/api/generated-documents/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireBuyer);

        app.MapPost("/api/batches/{batchId:guid}/dossier", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new GenerateDossier.Command(batchId));
            return result.IsSuccess
                ? Results.Created($"/api/generated-documents/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireBuyer);

        app.MapGet("/api/generated-documents/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetGeneratedDocument.Query(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        app.MapPost("/api/generated-documents/{id:guid}/share", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new ShareDocument.Command(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireBuyer);

        return app;
    }
}
