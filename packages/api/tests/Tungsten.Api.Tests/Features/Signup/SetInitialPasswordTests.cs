using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Identity;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class SetInitialPasswordTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static UserManager<AppIdentityUser> CreateUserManager() =>
        Substitute.For<UserManager<AppIdentityUser>>(
            Substitute.For<IUserStore<AppIdentityUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

    [Fact]
    public async Task Returns400_WhenNoPendingUserFound()
    {
        var db = CreateDb();
        var userManager = CreateUserManager();
        userManager.FindByEmailAsync(Arg.Any<string>()).Returns((AppIdentityUser?)null);
        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContext = new DefaultHttpContext();

        var result = await SetInitialPassword.HandleCoreAsync(
            "no-one@acme.com", "Password123!", db, userManager, jwtService, httpContext, CancellationToken.None);

        var statusResult = result as IStatusCodeHttpResult;
        Assert.NotNull(statusResult);
        Assert.Equal(400, statusResult.StatusCode);
    }

    [Fact]
    public async Task Returns409_WhenIdentityUserAlreadyExists()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "TRIAL", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "pending|abc", Email = "jane@acme.com",
            DisplayName = "Jane", Role = "TENANT_ADMIN", TenantId = tenantId,
            StripeSessionId = "cs_test_123", IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var userManager = CreateUserManager();
        userManager.FindByEmailAsync("jane@acme.com")
            .Returns(new AppIdentityUser { Email = "jane@acme.com" });

        var jwtService = Substitute.For<IJwtTokenService>();
        var httpContext = new DefaultHttpContext();

        var result = await SetInitialPassword.HandleCoreAsync(
            "jane@acme.com", "Password123!", db, userManager, jwtService, httpContext, CancellationToken.None);

        var statusResult = result as IStatusCodeHttpResult;
        Assert.NotNull(statusResult);
        Assert.Equal(409, statusResult.StatusCode);
    }
}
