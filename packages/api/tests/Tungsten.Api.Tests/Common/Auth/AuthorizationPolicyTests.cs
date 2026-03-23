using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Tests.Common.Auth;

public class AuthorizationPolicyTests
{
    [Fact]
    public void RequireAdmin_AllowsBothPlatformAdminAndTenantAdmin()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options => options.AddTungstenPolicies());
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = authOptions.GetPolicyAsync(AuthorizationPolicies.RequireAdmin).Result!;
        var requirement = policy.Requirements.OfType<RoleRequirement>().First();
        Assert.Contains(Roles.Admin, requirement.AllowedRoles);
        Assert.Contains(Roles.TenantAdmin, requirement.AllowedRoles);
    }

    [Fact]
    public void RequirePlatformAdmin_AllowsOnlyPlatformAdmin()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options => options.AddTungstenPolicies());
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = authOptions.GetPolicyAsync(AuthorizationPolicies.RequirePlatformAdmin).Result!;
        var requirement = policy.Requirements.OfType<RoleRequirement>().First();
        Assert.Contains(Roles.Admin, requirement.AllowedRoles);
        Assert.DoesNotContain(Roles.TenantAdmin, requirement.AllowedRoles);
    }
}
