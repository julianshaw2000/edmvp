using MediatR;

namespace Tungsten.Api.Features.Public;

public static class PublicEndpoints
{
    public static IEndpointRouteBuilder MapPublicEndpoints(this IEndpointRouteBuilder app)
    {
        // Public verification endpoint (FR-P060) — unauthenticated
        app.MapGet("/api/verify/{batchId:guid}", async (Guid batchId, IMediator mediator) =>
        {
            var result = await mediator.Send(new VerifyBatch.Query(batchId));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireRateLimiting("public");

        // Shared document access (FR-P053) — unauthenticated
        app.MapGet("/api/shared/{token}", async (string token, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetSharedDocument.Query(token));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.NotFound(new { error = result.Error });
        }).RequireRateLimiting("public");

        return app;
    }
}
