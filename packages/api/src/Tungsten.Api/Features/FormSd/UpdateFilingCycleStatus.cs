using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.FormSd;

public static class UpdateFilingCycleStatus
{
    public record Command(Guid CycleId, string Status, string? Notes = null) : IRequest<Result<Response>>;
    public record Response(Guid Id, string Status);

    public class Handler(AppDbContext db, ICurrentUserService currentUser) : IRequestHandler<Command, Result<Response>>
    {
        private static readonly string[] ValidStatuses = ["NOT_STARTED", "IN_PROGRESS", "PACKAGE_READY", "FILED"];

        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            if (!ValidStatuses.Contains(cmd.Status))
                return Result<Response>.Failure($"Invalid status. Valid: {string.Join(", ", ValidStatuses)}");

            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var cycle = await db.FormSdFilingCycles
                .FirstOrDefaultAsync(c => c.Id == cmd.CycleId && c.TenantId == tenantId, ct);
            if (cycle is null) return Result<Response>.Failure("Filing cycle not found");

            cycle.Status = cmd.Status;
            cycle.UpdatedAt = DateTime.UtcNow;
            if (cmd.Notes is not null) cycle.Notes = cmd.Notes;
            if (cmd.Status == "FILED") cycle.SubmittedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return Result<Response>.Success(new Response(cycle.Id, cycle.Status));
        }
    }
}
