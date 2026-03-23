using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Users;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Users;

public class UpdateUserRoleEnforcementTests
{
    [Fact]
    public async Task TenantAdmin_CannotEscalateToAdmin()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = callerId, Auth0Sub = "auth0|caller", Email = "c@t.com", DisplayName = "C", Role = "TENANT_ADMIN", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = targetId, Auth0Sub = "auth0|target", Email = "t@t.com", DisplayName = "T", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|caller");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("TENANT_ADMIN");

        var handler = new UpdateUser.Handler(db, currentUser);
        var result = await handler.Handle(
            new UpdateUser.Command(targetId, "PLATFORM_ADMIN", null), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task TenantAdmin_CannotModifyOtherTenantAdmin()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = callerId, Auth0Sub = "auth0|caller", Email = "c@t.com", DisplayName = "C", Role = "TENANT_ADMIN", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = targetId, Auth0Sub = "auth0|target", Email = "t@t.com", DisplayName = "T", Role = "TENANT_ADMIN", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|caller");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("TENANT_ADMIN");

        var handler = new UpdateUser.Handler(db, currentUser);
        var result = await handler.Handle(
            new UpdateUser.Command(targetId, null, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
    }
}
