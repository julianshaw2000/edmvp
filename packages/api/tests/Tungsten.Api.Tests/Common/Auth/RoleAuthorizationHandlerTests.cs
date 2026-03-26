using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Common.Auth;

public class RoleAuthorizationHandlerTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SupplierPolicy_SupplierUser_Succeeds()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), EntraOid = "auth0|1", Email = "s@test.com",
            DisplayName = "Supplier", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.EntraOid.Returns("auth0|1");

        var handler = new RoleAuthorizationHandler(db, currentUser);
        var requirement = new RoleRequirement(Roles.Supplier);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task SupplierPolicy_BuyerUser_Fails()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), EntraOid = "auth0|2", Email = "b@test.com",
            DisplayName = "Buyer", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.EntraOid.Returns("auth0|2");

        var handler = new RoleAuthorizationHandler(db, currentUser);
        var requirement = new RoleRequirement(Roles.Supplier);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task AnyPolicy_AdminUser_AlwaysSucceeds()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), EntraOid = "auth0|admin", Email = "a@test.com",
            DisplayName = "Admin", Role = "PLATFORM_ADMIN", TenantId = tenant.Id, IsActive = true
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.EntraOid.Returns("auth0|admin");

        var handler = new RoleAuthorizationHandler(db, currentUser);
        var requirement = new RoleRequirement(Roles.Supplier);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);
        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task AnyPolicy_InactiveUser_Fails()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), EntraOid = "auth0|inactive", Email = "i@test.com",
            DisplayName = "Inactive", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = false
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.EntraOid.Returns("auth0|inactive");

        var handler = new RoleAuthorizationHandler(db, currentUser);
        var requirement = new RoleRequirement(Roles.Supplier);
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), null);

        await handler.HandleAsync(context);
        context.HasSucceeded.Should().BeFalse();
    }
}
