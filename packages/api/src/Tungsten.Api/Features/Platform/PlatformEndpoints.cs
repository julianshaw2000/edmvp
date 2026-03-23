using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Platform;

public static class PlatformEndpoints
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/platform/tenants")
            .RequireAuthorization(AuthorizationPolicies.RequirePlatformAdmin);

        group.MapPost("/", async (CreateTenant.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/platform/tenants/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/", async (int? page, int? pageSize, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListTenants.Query(page ?? 1, pageSize ?? 20), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPatch("/{id:guid}/status", async (Guid id, UpdateTenantStatus.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var cmd = command with { TenantId = id };
            var result = await mediator.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
