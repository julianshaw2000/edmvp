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

        var tenantStatus = await currentUser.GetTenantStatusAsync(ct);

        if (tenantStatus == "SUSPENDED")
        {
            var role = await currentUser.GetRoleAsync(ct);
            if (role != Roles.Admin)
            {
                if (typeof(TResponse).IsGenericType &&
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var failureMethod = typeof(TResponse).GetMethod("Failure", [typeof(string)])!;
                    return (TResponse)failureMethod.Invoke(null, ["Your organization's account has been suspended. Contact support."])!;
                }

                if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure("Your organization's account has been suspended. Contact support.");
                }
            }
        }

        return await next();
    }
}
