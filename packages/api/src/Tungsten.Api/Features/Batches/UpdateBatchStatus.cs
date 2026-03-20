using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Batches;

public static class UpdateBatchStatus
{
    public record Command(Guid BatchId, string NewStatus) : IRequest<Result<Response>>;

    public record Response(Guid Id, string BatchNumber, string Status, DateTime UpdatedAt);

    private static readonly Dictionary<string, string[]> ValidTransitions = new()
    {
        ["CREATED"] = ["ACTIVE"],
        ["ACTIVE"] = ["COMPLETED"],
    };

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.BatchId).NotEmpty();
            RuleFor(x => x.NewStatus).NotEmpty()
                .Must(s => s is "ACTIVE" or "COMPLETED")
                .WithMessage("NewStatus must be ACTIVE or COMPLETED");
        }
    }

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var batch = await db.Batches
                .FirstOrDefaultAsync(b => b.Id == cmd.BatchId && b.TenantId == user.TenantId, ct);
            if (batch is null)
                return Result<Response>.Failure("Batch not found");

            if (!ValidTransitions.TryGetValue(batch.Status, out var allowed) || !allowed.Contains(cmd.NewStatus))
                return Result<Response>.Failure(
                    $"Invalid status transition: cannot move from '{batch.Status}' to '{cmd.NewStatus}'");

            batch.Status = cmd.NewStatus;
            batch.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(batch.Id, batch.BatchNumber, batch.Status, batch.UpdatedAt));
        }
    }
}
