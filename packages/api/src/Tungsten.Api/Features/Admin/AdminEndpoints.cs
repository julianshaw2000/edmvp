using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/rmap/upload", async (IFormFile file, IMediator mediator) =>
        {
            using var stream = file.OpenReadStream();
            var result = await mediator.Send(new UploadRmapList.Command(stream));
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        })
        .RequireAuthorization(AuthorizationPolicies.RequireAdmin)
        .DisableAntiforgery();

        app.MapGet("/api/admin/rmap", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new ListRmapSmelters.Query());
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        app.MapGet("/api/admin/jobs", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListJobs.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        return app;
    }
}
