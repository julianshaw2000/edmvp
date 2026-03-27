using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Users;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Users;

public class ListUsersTests
{
    [Fact]
    public async Task Handle_Admin_ReturnsAllTenantUsers()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var admin = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|admin", Email = "admin@test.com",
            DisplayName = "Admin", Role = "PLATFORM_ADMIN", TenantId = tenant.Id, IsActive = true
        };
        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|s", Email = "s@test.com",
            DisplayName = "Supplier", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(admin, supplier);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns("auth0|admin");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult("PLATFORM_ADMIN"));

        var handler = new ListUsers.Handler(db, currentUser);
        var result = await handler.Handle(new ListUsers.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Users.Should().HaveCount(2);
    }
}
