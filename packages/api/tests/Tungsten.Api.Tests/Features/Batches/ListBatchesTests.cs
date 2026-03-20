using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Batches;

public class ListBatchesTests
{
    [Fact]
    public async Task Handle_SupplierRole_ReturnsOnlyOwnBatches()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var supplier1 = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s1", Email = "s1@test.com",
            DisplayName = "S1", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        var supplier2 = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s2", Email = "s2@test.com",
            DisplayName = "S2", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(supplier1, supplier2);

        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "M1",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = supplier1.Id
        });
        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-002",
            MineralType = "tungsten", OriginCountry = "RW", OriginMine = "M2",
            WeightKg = 200, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = supplier2.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(supplier1.Auth0Sub);

        var handler = new ListBatches.Handler(db, currentUser);
        var result = await handler.Handle(new ListBatches.Query(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].BatchNumber.Should().Be("B-001");
    }

    [Fact]
    public async Task Handle_BuyerRole_ReturnsAllTenantBatches()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@test.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        var buyer = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|b", Email = "b@test.com",
            DisplayName = "B", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(supplier, buyer);

        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "M1",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = supplier.Id
        });
        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "B-002",
            MineralType = "tungsten", OriginCountry = "RW", OriginMine = "M2",
            WeightKg = 200, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = supplier.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(buyer.Auth0Sub);

        var handler = new ListBatches.Handler(db, currentUser);
        var result = await handler.Handle(new ListBatches.Query(1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
    }
}
