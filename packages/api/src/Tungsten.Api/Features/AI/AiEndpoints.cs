using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.AI;

public static class AiEndpoints
{
    public static IEndpointRouteBuilder MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        // Chat is available to all authenticated users
        app.MapPost("/api/ai/chat", async (ChatAssistant.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization();

        // Admin-only AI features
        var group = app.MapGroup("/api/ai")
            .RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        group.MapPost("/compliance-report", async (GenerateComplianceReport.Command command, IMediator mediator, CancellationToken ct) =>
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

        // Feature 1: Churn Prediction
        group.MapGet("/churn-prediction", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ChurnPrediction.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin);

        // Feature 2: Usage Coaching
        group.MapGet("/usage-coaching", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new UsageCoaching.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin);

        // Feature 3: Revenue Summary
        group.MapGet("/revenue-summary", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RevenueSummary.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin);

        // Feature 4: Tenant Health Check
        group.MapGet("/tenant-health", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new TenantHealthCheck.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin);

        // Feature 5: Incident Report — RequireAdmin (both PLATFORM_ADMIN and TENANT_ADMIN)
        group.MapPost("/incident-report", async (GenerateIncidentReport.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        // Feature 6: Natural Language Query
        group.MapPost("/query", async (NaturalLanguageQuery.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin);

        // Feature 7: Regulatory Monitor
        group.MapGet("/regulatory-updates", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new RegulatoryMonitor.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin);

        return app;
    }
}
