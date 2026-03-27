using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.CustodyEvents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class GetCustodyEventTests
{
    [Fact]
    public async Task Handle_ExistingEvent_ReturnsEvent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|s", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 500, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);
        var evt = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.UtcNow, Location = "Bisie", ActorName = "Corp",
            Description = "Test", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);

        var handler = new GetCustodyEvent.Handler(db, currentUser);
        var result = await handler.Handle(new GetCustodyEvent.Query(evt.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.EventType.Should().Be("MINE_EXTRACTION");
    }

    [Fact]
    public async Task Handle_CrossTenantAccess_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantA = new TenantEntity { Id = Guid.NewGuid(), Name = "A", SchemaPrefix = "a", Status = "ACTIVE" };
        var tenantB = new TenantEntity { Id = Guid.NewGuid(), Name = "B", SchemaPrefix = "b", Status = "ACTIVE" };
        db.Tenants.AddRange(tenantA, tenantB);

        var userA = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|a", Email = "a@test.com",
            DisplayName = "A", Role = "SUPPLIER", TenantId = tenantA.Id, IsActive = true
        };
        var userB = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|b", Email = "b@test.com",
            DisplayName = "B", Role = "SUPPLIER", TenantId = tenantB.Id, IsActive = true
        };
        db.Users.AddRange(userA, userB);

        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantA.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 500, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = userA.Id
        };
        db.Batches.Add(batch);
        var evt = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenantA.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.UtcNow, Location = "Bisie", ActorName = "Corp",
            Description = "Test", Sha256Hash = new string('a', 64),
            CreatedBy = userA.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(evt);
        db.SaveChanges();

        // User B tries to access tenant A's event
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(userB.IdentityUserId);

        var handler = new GetCustodyEvent.Handler(db, currentUser);
        var result = await handler.Handle(new GetCustodyEvent.Query(evt.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
