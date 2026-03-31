using Microsoft.AspNetCore.Authorization;

namespace Tungsten.Api.Common.Auth;

public static class AuthorizationPolicies
{
    public const string RequireSupplier = "RequireSupplier";
    public const string RequireSupplierOrAdmin = "RequireSupplierOrAdmin";
    public const string RequireBuyer = "RequireBuyer";
    public const string RequireAdmin = "RequireAdmin";
    public const string RequirePlatformAdmin = "RequirePlatformAdmin";
    public const string RequireSupplierOrBuyer = "RequireSupplierOrBuyer";
    public const string RequireTenantAccess = "RequireTenantAccess";

    public static void AddTungstenPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(RequireSupplier, policy =>
            policy.Requirements.Add(new RoleRequirement(Roles.Supplier)));

        options.AddPolicy(RequireSupplierOrAdmin, policy =>
            policy.Requirements.Add(new RoleRequirement(Roles.Supplier, Roles.TenantAdmin)));

        options.AddPolicy(RequireBuyer, policy =>
            policy.Requirements.Add(new RoleRequirement(Roles.Buyer)));

        options.AddPolicy(RequireAdmin, policy =>
            policy.Requirements.Add(new RoleRequirement(Roles.Admin, Roles.TenantAdmin)));

        options.AddPolicy(RequirePlatformAdmin, policy =>
            policy.AddRequirements(new RoleRequirement(Roles.Admin)));

        options.AddPolicy(RequireSupplierOrBuyer, policy =>
            policy.Requirements.Add(new RoleRequirement(Roles.Supplier, Roles.Buyer)));

        options.AddPolicy(RequireTenantAccess, policy =>
            policy.Requirements.Add(new TenantAccessRequirement()));
    }
}
