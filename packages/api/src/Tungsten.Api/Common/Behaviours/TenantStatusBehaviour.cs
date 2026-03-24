using MediatR;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Common.Behaviours;

public class TenantStatusBehaviour<TRequest, TResponse>(
    ICurrentUserService currentUser,
    IHttpContextAccessor httpContextAccessor)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (httpContextAccessor.HttpContext is null)
            return await next();

        // Skip for unauthenticated requests (e.g., public signup endpoint)
        if (httpContextAccessor.HttpContext.User.Identity?.IsAuthenticated != true)
            return await next();

        var tenantStatus = await currentUser.GetTenantStatusAsync(ct);

        if (tenantStatus is "SUSPENDED" or "CANCELLED")
        {
            var role = await currentUser.GetRoleAsync(ct);
            if (role != Roles.Admin)
            {
                var message = tenantStatus == "SUSPENDED"
                    ? "Your organization's account has been suspended. Contact support."
                    : "Your subscription has been cancelled.";

                if (typeof(TResponse).IsGenericType &&
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(string)])!;
                    return (TResponse)failureMethod.Invoke(null, [message])!;
                }

                if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure(message);
                }
            }
        }

        return await next();
    }
}
