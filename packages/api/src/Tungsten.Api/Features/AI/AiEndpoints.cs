using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.AI;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai")
            .RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        group.MapPost("/compliance-report", async (GenerateComplianceReport.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/chat", async (ChatAssistant.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/data-completeness", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new DataCompletenessCheck.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
