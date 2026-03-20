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

        return app;
    }
}
