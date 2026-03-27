using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Users;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;
using Microsoft.Extensions.Configuration;

namespace Tungsten.Api.Tests.Features.Users;

public class CreateUserRoleEnforcementTests
{
    private static (AppDbContext db, ICurrentUserService currentUser) SetupWithRole(string callerRole)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, IdentityUserId = "auth0|caller", Email = "caller@test.com", DisplayName = "Caller", Role = callerRole, TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns("auth0|caller");
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns(callerRole);

        return (db, currentUser);
    }

    [Fact]
    public async Task TenantAdmin_CanInviteSupplier()
    {
        var (db, currentUser) = SetupWithRole("TENANT_ADMIN");
        var emailService = Substitute.For<IEmailService>();
        var config = Substitute.For<IConfiguration>();
        var planEnforcement = Substitute.For<IPlanEnforcementService>();
        planEnforcement.CheckUserLimitAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var handler = new CreateUser.Handler(db, currentUser, emailService, config, planEnforcement);

        var result = await handler.Handle(
            new CreateUser.Command("new@test.com", "New User", "SUPPLIER"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TenantAdmin_CannotAssignTenantAdmin()
    {
        var (db, currentUser) = SetupWithRole("TENANT_ADMIN");
        var emailService = Substitute.For<IEmailService>();
        var config = Substitute.For<IConfiguration>();
        var planEnforcement = Substitute.For<IPlanEnforcementService>();
        planEnforcement.CheckUserLimitAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var handler = new CreateUser.Handler(db, currentUser, emailService, config, planEnforcement);

        var result = await handler.Handle(
            new CreateUser.Command("new@test.com", "New User", "TENANT_ADMIN"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Supplier or Buyer", result.Error!);
    }

    [Fact]
    public async Task PlatformAdmin_CanAssignTenantAdmin()
    {
        var (db, currentUser) = SetupWithRole("PLATFORM_ADMIN");
        var emailService = Substitute.For<IEmailService>();
        var config = Substitute.For<IConfiguration>();
        var planEnforcement = Substitute.For<IPlanEnforcementService>();
        planEnforcement.CheckUserLimitAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var handler = new CreateUser.Handler(db, currentUser, emailService, config, planEnforcement);

        var result = await handler.Handle(
            new CreateUser.Command("new@test.com", "New User", "TENANT_ADMIN"), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
