using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Common.Services;

public class PlanEnforcementServiceTests
{
    [Fact]
    public async Task CheckBatchLimit_NullLimit_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", MaxBatches = null, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new PlanEnforcementService(db);
        var result = await svc.CheckBatchLimitAsync(tenantId, CancellationToken.None);
        Assert.Null(result); // Unlimited
    }

    [Fact]
    public async Task CheckBatchLimit_UnderLimit_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", MaxBatches = 10, CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, EntraOid = "a", Email = "a@a.com", DisplayName = "A", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Batches.Add(new BatchEntity { Id = Guid.NewGuid(), TenantId = tenantId, BatchNumber = "B-001", MineralType = "Tungsten", OriginCountry = "RW", OriginMine = "M", WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING", CreatedBy = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new PlanEnforcementService(db);
        var result = await svc.CheckBatchLimitAsync(tenantId, CancellationToken.None);
        Assert.Null(result); // 1 of 10, under limit
    }

    [Fact]
    public async Task CheckBatchLimit_AtLimit_ReturnsError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", MaxBatches = 1, CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, EntraOid = "a", Email = "a@a.com", DisplayName = "A", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.Batches.Add(new BatchEntity { Id = Guid.NewGuid(), TenantId = tenantId, BatchNumber = "B-001", MineralType = "Tungsten", OriginCountry = "RW", OriginMine = "M", WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING", CreatedBy = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new PlanEnforcementService(db);
        var result = await svc.CheckBatchLimitAsync(tenantId, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Contains("Batch limit reached", result);
    }

    [Fact]
    public async Task CheckUserLimit_NullLimit_ReturnsNull()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", MaxUsers = null, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new PlanEnforcementService(db);
        var result = await svc.CheckUserLimitAsync(tenantId, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckUserLimit_AtLimit_ReturnsError()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", MaxUsers = 1, CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), EntraOid = "a", Email = "a@a.com", DisplayName = "A", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new PlanEnforcementService(db);
        var result = await svc.CheckUserLimitAsync(tenantId, CancellationToken.None);
        Assert.NotNull(result);
        Assert.Contains("User limit reached", result);
    }
}
