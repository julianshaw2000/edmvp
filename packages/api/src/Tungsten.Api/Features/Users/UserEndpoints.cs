using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization(AuthorizationPolicies.RequireAdmin);

        group.MapGet("/", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new ListUsers.Query());
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/", async (CreateUser.Command command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return result.IsSuccess
                ? Results.Created($"/api/users/{result.Value.Id}", result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPatch("/{id:guid}", async (Guid id, UpdateUser.Command command, IMediator mediator) =>
        {
            var cmd = command with { Id = id };
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.NoContent()
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapDelete("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new UpdateUser.Command(id, null, false));
            return result.IsSuccess
                ? Results.NoContent()
                : Results.NotFound(new { error = result.Error });
        });

        return app;
    }
}
