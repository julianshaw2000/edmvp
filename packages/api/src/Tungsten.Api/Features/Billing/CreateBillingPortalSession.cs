using MediatR;
using Microsoft.EntityFrameworkCore;
using Stripe.BillingPortal;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;

namespace Tungsten.Api.Features.Billing;

public static class CreateBillingPortalSession
{
    public record Command : IRequest<Result<Response>>;
    public record Response(string PortalUrl);

    public class Handler(AppDbContext db, ICurrentUserService currentUser, IConfiguration config)
        : IRequestHandler<Command, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Command cmd, CancellationToken ct)
        {
            var tenantId = await currentUser.GetTenantIdAsync(ct);
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

            if (tenant?.StripeCustomerId is null)
                return Result<Response>.Failure("No billing account found for this organization");

            var baseUrl = config["BaseUrl"] ?? "https://auditraks.com";

            var options = new SessionCreateOptions
            {
                Customer = tenant.StripeCustomerId,
                ReturnUrl = $"{baseUrl}/admin",
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: ct);

            return Result<Response>.Success(new Response(session.Url));
        }
    }
}
