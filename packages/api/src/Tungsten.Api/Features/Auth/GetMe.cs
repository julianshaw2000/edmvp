using MediatR;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Auth;

public static class GetMe
{
    public record Query : IRequest<Result<Response>>;

    public record Response(
        Guid Id,
        string Email,
        string DisplayName,
        string Role,
        Guid TenantId,
        string TenantName);

    public class Handler(AppDbContext db, ICurrentUserService currentUser)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken ct)
        {
            var user = await db.Users
                .AsNoTracking()
                .Include(u => u.Tenant)
                .FirstOrDefaultAsync(u => u.Auth0Sub == currentUser.Auth0Sub, ct);

            if (user is null || !user.IsActive)
                return Result<Response>.Failure("User not found");

            return Result<Response>.Success(new Response(
                user.Id, user.Email, user.DisplayName, user.Role,
                user.TenantId, user.Tenant.Name));
        }
    }
}
