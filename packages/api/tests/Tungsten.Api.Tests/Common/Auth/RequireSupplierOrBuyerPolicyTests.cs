using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Tests.Common.Auth;

public class RequireSupplierOrBuyerPolicyTests
{
    [Fact]
    public async Task RequireSupplierOrBuyer_AllowsSupplier()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options => options.AddTungstenPolicies());
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await authOptions.GetPolicyAsync(AuthorizationPolicies.RequireSupplierOrBuyer);
        var requirement = policy!.Requirements.OfType<RoleRequirement>().First();
        Assert.Contains(Roles.Supplier, requirement.AllowedRoles);
    }

    [Fact]
    public async Task RequireSupplierOrBuyer_AllowsBuyer()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options => options.AddTungstenPolicies());
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await authOptions.GetPolicyAsync(AuthorizationPolicies.RequireSupplierOrBuyer);
        var requirement = policy!.Requirements.OfType<RoleRequirement>().First();
        Assert.Contains(Roles.Buyer, requirement.AllowedRoles);
    }

    [Fact]
    public async Task RequireSupplierOrBuyer_DoesNotAllowAdmin()
    {
        var services = new ServiceCollection();
        services.AddAuthorization(options => options.AddTungstenPolicies());
        var provider = services.BuildServiceProvider();
        var authOptions = provider.GetRequiredService<IAuthorizationPolicyProvider>();
        var policy = await authOptions.GetPolicyAsync(AuthorizationPolicies.RequireSupplierOrBuyer);
        var requirement = policy!.Requirements.OfType<RoleRequirement>().First();
        Assert.DoesNotContain(Roles.Admin, requirement.AllowedRoles);
        Assert.DoesNotContain(Roles.TenantAdmin, requirement.AllowedRoles);
    }
}
