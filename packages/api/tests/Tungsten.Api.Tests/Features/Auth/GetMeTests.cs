using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Auth;

public class GetMeTests
{
    [Fact]
    public async Task Handle_ValidAuth0Sub_ReturnsUserProfile()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "Test Corp", SchemaPrefix = "tenant_test", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|123", Email = "test@example.com",
            DisplayName = "Test User", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|123");

        var handler = new GetMe.Handler(db, currentUser);
        var result = await handler.Handle(new GetMe.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Role.Should().Be("SUPPLIER");
        result.Value.TenantName.Should().Be("Test Corp");
    }

    [Fact]
    public async Task Handle_UnknownAuth0Sub_ReturnsNotFound()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|unknown");

        var handler = new GetMe.Handler(db, currentUser);
        var result = await handler.Handle(new GetMe.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
