using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Admin;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Integration;

public class TenantIsolationTests
{
    [Fact]
    public async Task ListBatches_OnlyReturnsBatchesFromUserTenant()
    {
        // Arrange: two tenants, each with one batch; user1 belongs to tenant1
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        db.Tenants.Add(new TenantEntity { Id = tenant1Id, Name = "Tenant1", SchemaPrefix = "t1", Status = "ACTIVE" });
        db.Tenants.Add(new TenantEntity { Id = tenant2Id, Name = "Tenant2", SchemaPrefix = "t2", Status = "ACTIVE" });

        // user1 is a BUYER in tenant1 so it sees all tenant1 batches (not filtered by CreatedBy)
        db.Users.Add(new UserEntity
        {
            Id = user1Id, Auth0Sub = "auth0|u1", Email = "u1@t1.com",
            DisplayName = "User1", Role = "BUYER", TenantId = tenant1Id, IsActive = true
        });
        // user2 is in tenant2 (needed as CreatedBy for the tenant2 batch)
        db.Users.Add(new UserEntity
        {
            Id = user2Id, Auth0Sub = "auth0|u2", Email = "u2@t2.com",
            DisplayName = "User2", Role = "BUYER", TenantId = tenant2Id, IsActive = true
        });

        // One batch in tenant1
        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant1Id, BatchNumber = "T1-001",
            MineralType = "tungsten", OriginCountry = "RW", OriginMine = "Mine1",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user1Id
        });
        // One batch in tenant2 — must never appear for user1
        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant2Id, BatchNumber = "T2-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Mine2",
            WeightKg = 200, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user2Id
        });
        await db.SaveChangesAsync();

        // ListBatches.Handler resolves tenant via Auth0Sub -> DB user lookup
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns("auth0|u1");

        var handler = new ListBatches.Handler(db, currentUser);

        // Act
        var result = await handler.Handle(new ListBatches.Query(1, 20), CancellationToken.None);

        // Assert: only tenant1 batch returned, tenant2 batch is absent
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].BatchNumber.Should().Be("T1-001");
    }

    [Fact]
    public async Task ListAuditLogs_OnlyReturnsLogsFromUserTenant()
    {
        // Arrange: two tenants, each with one audit log; current user belongs to tenant1
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        db.Tenants.Add(new TenantEntity { Id = tenant1Id, Name = "Tenant1", SchemaPrefix = "t1", Status = "ACTIVE" });
        db.Tenants.Add(new TenantEntity { Id = tenant2Id, Name = "Tenant2", SchemaPrefix = "t2", Status = "ACTIVE" });

        // admin is in tenant1; otherUser is in tenant2
        db.Users.Add(new UserEntity
        {
            Id = adminId, Auth0Sub = "auth0|admin1", Email = "admin@t1.com",
            DisplayName = "Admin1", Role = "ADMIN", TenantId = tenant1Id, IsActive = true
        });
        db.Users.Add(new UserEntity
        {
            Id = otherUserId, Auth0Sub = "auth0|admin2", Email = "admin@t2.com",
            DisplayName = "Admin2", Role = "ADMIN", TenantId = tenant2Id, IsActive = true
        });

        // One log in tenant1
        db.AuditLogs.Add(new AuditLogEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant1Id, UserId = adminId,
            Action = "BATCH_CREATED", EntityType = "Batch", EntityId = Guid.NewGuid(),
            Result = "SUCCESS", Timestamp = DateTime.UtcNow
        });
        // One log in tenant2 — must never appear for tenant1's admin
        db.AuditLogs.Add(new AuditLogEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant2Id, UserId = otherUserId,
            Action = "BATCH_CREATED", EntityType = "Batch", EntityId = Guid.NewGuid(),
            Result = "SUCCESS", Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // ListAuditLogs.Handler resolves tenant via GetTenantIdAsync (direct mock)
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenant1Id);

        var handler = new ListAuditLogs.Handler(db, currentUser);

        // Act
        var result = await handler.Handle(
            new ListAuditLogs.Query(1, 20, null, null, null, null, null),
            CancellationToken.None);

        // Assert: only tenant1 log returned, tenant2 log is absent
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Action.Should().Be("BATCH_CREATED");
        result.Value.Items[0].UserId.Should().Be(adminId);
    }
}
