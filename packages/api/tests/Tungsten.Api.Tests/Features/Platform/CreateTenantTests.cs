using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Platform;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Platform;

public class CreateTenantTests
{
    [Fact]
    public async Task Handle_ValidInput_CreatesTenantAndAdmin()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var handler = new CreateTenant.Handler(db);
        var result = await handler.Handle(
            new CreateTenant.Command("Acme Mining", "admin@acme.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Acme Mining", result.Value.Name);
        Assert.Equal("ACTIVE", result.Value.Status);

        var tenant = await db.Tenants.FirstOrDefaultAsync();
        Assert.NotNull(tenant);
        Assert.Equal("acme_mining", tenant.SchemaPrefix);

        var user = await db.Users.FirstOrDefaultAsync();
        Assert.NotNull(user);
        Assert.Equal("admin@acme.com", user.Email);
        Assert.Equal("TENANT_ADMIN", user.Role);
        Assert.StartsWith("pending|", user.IdentityUserId);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Existing", SchemaPrefix = "existing", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), IdentityUserId = "auth0|x", Email = "admin@acme.com", DisplayName = "X", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new CreateTenant.Handler(db);
        var result = await handler.Handle(
            new CreateTenant.Command("Acme Mining", "admin@acme.com"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already in use", result.Error!);
    }

    [Fact]
    public async Task Handle_SchemaPrefixCollision_AppendsNumericSuffix()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        db.Tenants.Add(new TenantEntity { Id = Guid.NewGuid(), Name = "Existing", SchemaPrefix = "acme_mining", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new CreateTenant.Handler(db);
        var result = await handler.Handle(
            new CreateTenant.Command("Acme Mining", "new@acme.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var tenant = await db.Tenants.OrderByDescending(t => t.CreatedAt).FirstAsync();
        Assert.Equal("acme_mining_2", tenant.SchemaPrefix);
    }
}
