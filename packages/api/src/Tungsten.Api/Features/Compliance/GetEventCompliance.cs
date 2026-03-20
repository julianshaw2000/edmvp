using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Compliance;

public static class GetEventCompliance
{
    public record Query(Guid EventId) : IRequest<Result<Response>>;

    public record CheckItem(Guid Id, string Framework, string Status, object? Details, DateTime CheckedAt);

    public record Response(Guid EventId, IReadOnlyList<CheckItem> Checks);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
        {
            var user = await db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub && u.IsActive, ct);
            if (user is null)
                return Result<Response>.Failure("User not found");

            var checks = await db.ComplianceChecks.AsNoTracking()
                .Where(c => c.CustodyEventId == query.EventId && c.TenantId == user.TenantId)
                .Select(c => new CheckItem(c.Id, c.Framework, c.Status, c.Details, c.CheckedAt))
                .ToListAsync(ct);

            return Result<Response>.Success(new Response(query.EventId, checks));
        }
    }
}
