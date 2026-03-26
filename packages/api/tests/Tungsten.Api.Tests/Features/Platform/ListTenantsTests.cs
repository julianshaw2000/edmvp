using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Platform;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Platform;

public class ListTenantsTests
{
    [Fact]
    public async Task Handle_ReturnsTenants_WithCounts()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, EntraOid = "auth0|1", Email = "a@a.com", DisplayName = "A", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Batches.Add(new BatchEntity { Id = Guid.NewGuid(), TenantId = tenantId, BatchNumber = "B-001", MineralType = "Tungsten", OriginCountry = "RW", OriginMine = "Mine", WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING", CreatedBy = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new ListTenants.Handler(db);
        var result = await handler.Handle(new ListTenants.Query(1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Items);
        Assert.Equal("Acme", result.Value.Items[0].Name);
        Assert.Equal(1, result.Value.Items[0].UserCount);
        Assert.Equal(1, result.Value.Items[0].BatchCount);
    }
}
