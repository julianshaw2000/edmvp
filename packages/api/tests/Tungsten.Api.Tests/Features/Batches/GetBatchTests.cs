using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Batches;

public class GetBatchTests
{
    [Fact]
    public async Task Handle_ExistingBatch_ReturnsBatchWithEventCount()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|1", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);

        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        };
        db.Batches.Add(batch);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);

        var handler = new GetBatch.Handler(db, currentUser);
        var result = await handler.Handle(new GetBatch.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchNumber.Should().Be("B-001");
        result.Value.EventCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NonExistentBatch_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|1", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);

        var handler = new GetBatch.Handler(db, currentUser);
        var result = await handler.Handle(new GetBatch.Query(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
