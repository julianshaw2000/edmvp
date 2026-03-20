using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Documents;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        // Upload document to an event
        app.MapPost("/api/events/{eventId:guid}/documents", async (
            Guid eventId,
            IFormFile file,
            string documentType,
            IMediator mediator) =>
        {
            using var stream = file.OpenReadStream();
            var command = new UploadDocument.Command(
                eventId, file.FileName, file.ContentType,
                stream, file.Length, documentType);

            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/documents/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(AuthorizationPolicies.RequireSupplier)
        .DisableAntiforgery();

        // Get document (returns download URL)
        app.MapGet("/api/documents/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetDocument.Query(id));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireAuthorization();

        // List all documents for a batch
        app.MapGet("/api/batches/{batchId:guid}/documents", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new ListBatchDocuments.Query(batchId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization();

        return app;
    }
}
