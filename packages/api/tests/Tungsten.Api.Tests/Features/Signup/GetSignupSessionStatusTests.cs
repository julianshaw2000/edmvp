using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class GetSignupSessionStatusTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Returns_Provisioned_True_WhenUserExistsWithPendingPrefix()
    {
        // Provisioned = webhook fired and created user entity (even if still pending password setup)
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

        var result = await GetSignupSessionStatus.CheckProvisionedAsync(db, "jane@acme.com", CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Returns_Provisioned_False_WhenNoUserExists()
    {
        var db = CreateDb();
        var result = await GetSignupSessionStatus.CheckProvisionedAsync(db, "nobody@acme.com", CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public async Task Returns_Provisioned_True_WhenIdentityUserIdIsReal()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "TRIAL", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "real-identity-user-id", Email = "jane@acme.com",
            DisplayName = "Jane", Role = "TENANT_ADMIN", TenantId = tenantId,
            StripeSessionId = "cs_test_123", IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = await GetSignupSessionStatus.CheckProvisionedAsync(db, "jane@acme.com", CancellationToken.None);

        Assert.True(result);
    }
}
