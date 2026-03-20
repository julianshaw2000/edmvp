using System.Security.Claims;

namespace Tungsten.Api.Common.Auth;

public interface ICurrentUserService
{
    string Auth0Sub { get; }
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string Auth0Sub =>
        httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("No authenticated user");
}
