using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.CustodyEvents;

public static class VerifyIntegrity
{
    public record Query(Guid BatchId) : IRequest<Result<Response>>;

    public record Response(
        bool IsIntact,
        int EventCount,
        Guid? FirstTamperedEventId,
        string? TamperDetail);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var events = await db.CustodyEvents.AsNoTracking()
                .Where(e => e.BatchId == query.BatchId && e.TenantId == user.TenantId)
                .OrderBy(e => e.CreatedAt)
                .ToListAsync(ct);

            if (events.Count == 0)
                return Result<Response>.Success(new Response(true, 0, null, null));

            string? previousHash = null;
            foreach (var evt in events)
            {
                var metadataString = evt.Metadata.HasValue
                    ? evt.Metadata.Value.GetRawText()
                    : "{}";

                var expectedHash = HashService.ComputeEventHash(
                    evt.EventType,
                    HashService.NormalizeDate(evt.EventDate),
                    evt.BatchId,
                    evt.Location,
                    evt.ActorName,
                    evt.SmelterId,
                    evt.Description,
                    metadataString,
                    previousHash);

                if (evt.Sha256Hash != expectedHash)
                {
                    return Result<Response>.Success(new Response(
                        false, events.Count, evt.Id,
                        $"Hash mismatch at event {evt.Id}: expected {expectedHash[..16]}..., got {evt.Sha256Hash[..16]}..."));
                }

                if (evt.PreviousEventHash != previousHash)
                {
                    return Result<Response>.Success(new Response(
                        false, events.Count, evt.Id,
                        $"Chain break at event {evt.Id}: previous hash mismatch"));
                }

                previousHash = evt.Sha256Hash;
            }

            return Result<Response>.Success(new Response(true, events.Count, null, null));
        }
    }
}
