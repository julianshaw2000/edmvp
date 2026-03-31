using MediatR;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Features.Buyer;

public static class BuyerEndpoints
{
    public static IEndpointRouteBuilder MapBuyerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/buyer")
            .RequireAuthorization(AuthorizationPolicies.RequireBuyer);

        group.MapGet("/supplier-engagement", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new GetSupplierEngagement.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/nudge-supplier", async (NudgeSupplier.Command command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { message = "Reminder sent" })
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapPost("/cmrt-import/preview", async (HttpRequest httpRequest, IMediator mediator, CancellationToken ct) =>
        {
            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files.FirstOrDefault();
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded" });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "Only .xlsx files are supported" });

            using var stream = file.OpenReadStream();
            var result = await mediator.Send(new ImportCmrt.PreviewCommand(stream, file.FileName), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        }).DisableAntiforgery();

        group.MapPost("/cmrt-import/confirm", async (ImportCmrt.ConfirmCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        group.MapGet("/cmrt-imports", async (IMediator mediator, CancellationToken ct) =>
        {
            var result = await mediator.Send(new ListCmrtImports.Query(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.BadRequest(new { error = result.Error });
        });

        return app;
    }
}
