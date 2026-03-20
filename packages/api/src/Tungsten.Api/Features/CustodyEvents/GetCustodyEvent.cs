using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.CustodyEvents;

public static class GetCustodyEvent
{
    public record Query(Guid Id) : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        Guid BatchId,
        string EventType,
        DateTime EventDate,
        string Location,
        string? GpsCoordinates,
        string ActorName,
        string? SmelterId,
        string Description,
        bool IsCorrection,
        Guid? CorrectsEventId,
        string Sha256Hash,
        string? PreviousEventHash,
        DateTime CreatedAt);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var evt = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.Id == query.Id && e.TenantId == user.TenantId)
                .Select(e => new Response(
                    e.Id, e.BatchId, e.EventType, e.EventDate,
                    e.Location, e.GpsCoordinates, e.ActorName, e.SmelterId,
                    e.Description, e.IsCorrection, e.CorrectsEventId,
                    e.Sha256Hash, e.PreviousEventHash, e.CreatedAt))
                .FirstOrDefaultAsync(ct);

            return evt is null
                ? Result<Response>.Failure("Event not found")
                : Result<Response>.Success(evt);
        }
    }
}
