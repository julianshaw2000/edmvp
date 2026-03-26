using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Common.Auth;

public class CurrentUserServiceTests
{
    private static (AppDbContext db, IHttpContextAccessor accessor) CreateContext(string auth0Sub)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, auth0Sub) };
        var identity = new ClaimsIdentity(claims, "Test");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        var accessor = new HttpContextAccessor { HttpContext = httpContext };

        return (db, accessor);
    }

    [Fact]
    public async Task GetUserIdAsync_ReturnsUserId_WhenUserExists()
    {
        var (db, accessor) = CreateContext("auth0|test123");
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = "auth0|test123", Email = "test@test.com", DisplayName = "Test", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new CurrentUserService(accessor, db);
        var result = await svc.GetUserIdAsync(CancellationToken.None);

        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task GetTenantIdAsync_ReturnsTenantId_WhenUserExists()
    {
        var (db, accessor) = CreateContext("auth0|test456");
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), Auth0Sub = "auth0|test456", Email = "t@t.com", DisplayName = "T", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new CurrentUserService(accessor, db);
        var result = await svc.GetTenantIdAsync(CancellationToken.None);

        Assert.Equal(tenantId, result);
    }

    [Fact]
    public async Task GetUserIdAsync_CachesResult_OnSecondCall()
    {
        var (db, accessor) = CreateContext("auth0|cache");
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = "auth0|cache", Email = "c@c.com", DisplayName = "C", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new CurrentUserService(accessor, db);
        var first = await svc.GetUserIdAsync(CancellationToken.None);
        var second = await svc.GetUserIdAsync(CancellationToken.None);

        Assert.Equal(first, second);
    }

    [Fact]
    public void EntraOid_ReadsOidClaim_WhenPresent()
    {
        var oid = Guid.NewGuid().ToString();
        var claims = new[]
        {
            new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", oid),
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var svc = new CurrentUserService(accessor, db);
        svc.EntraOid.Should().Be(oid);
    }
}
