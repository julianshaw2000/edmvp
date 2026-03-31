using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Buyer;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Buyer;

public class GetSupplierEngagementTests
{
    private static (AppDbContext db, Guid tenantId) CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "TestTenant", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        db.SaveChanges();
        return (db, tenant.Id);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyWhenNoSuppliers()
    {
        var (db, tenantId) = CreateDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);

        var handler = new GetSupplierEngagement.Handler(db, currentUser);
        var result = await handler.Handle(new GetSupplierEngagement.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSuppliers.Should().Be(0);
        result.Value.Suppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ClassifiesActiveSupplier()
    {
        var (db, tenantId) = CreateDb();
        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "id|s1", Email = "s@test.com",
            DisplayName = "Active Supplier", Role = "SUPPLIER", TenantId = tenantId, IsActive = true
        };
        db.Users.Add(supplier);

        var batch = new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, BatchNumber = "B-001",
            MineralType = "tungsten", OriginCountry = "RW", OriginMine = "M1",
            WeightKg = 100, Status = "ACTIVE", ComplianceStatus = "COMPLIANT",
            CreatedBy = supplier.Id
        };
        db.Batches.Add(batch);

        db.CustodyEvents.Add(new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenantId,
            EventType = "MINE_EXTRACTION", IdempotencyKey = Guid.NewGuid().ToString(),
            EventDate = DateTime.UtcNow.AddDays(-10), Location = "Kigali",
            ActorName = "Test", Description = "Test", Sha256Hash = "abc123",
            CreatedBy = supplier.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);

        var handler = new GetSupplierEngagement.Handler(db, currentUser);
        var result = await handler.Handle(new GetSupplierEngagement.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSuppliers.Should().Be(1);
        result.Value.ActiveSuppliers.Should().Be(1);
        result.Value.Suppliers[0].Status.Should().Be("active");
    }

    [Fact]
    public async Task Handle_ClassifiesNewSupplier()
    {
        var (db, tenantId) = CreateDb();
        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "id|s2", Email = "s2@test.com",
            DisplayName = "New Supplier", Role = "SUPPLIER", TenantId = tenantId, IsActive = true
        };
        db.Users.Add(supplier);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);

        var handler = new GetSupplierEngagement.Handler(db, currentUser);
        var result = await handler.Handle(new GetSupplierEngagement.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSuppliers.Should().Be(1);
        result.Value.Suppliers[0].Status.Should().Be("new");
        result.Value.Suppliers[0].BatchCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ClassifiesFlaggedSupplier()
    {
        var (db, tenantId) = CreateDb();
        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "id|s3", Email = "s3@test.com",
            DisplayName = "Flagged Supplier", Role = "SUPPLIER", TenantId = tenantId, IsActive = true
        };
        db.Users.Add(supplier);

        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenantId, BatchNumber = "B-F01",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "M1",
            WeightKg = 50, Status = "ACTIVE", ComplianceStatus = "FLAGGED",
            CreatedBy = supplier.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);

        var handler = new GetSupplierEngagement.Handler(db, currentUser);
        var result = await handler.Handle(new GetSupplierEngagement.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.FlaggedSuppliers.Should().Be(1);
        result.Value.Suppliers[0].Status.Should().Be("flagged");
        result.Value.Suppliers[0].FlaggedBatchCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ExcludesOtherTenantSuppliers()
    {
        var (db, tenantId) = CreateDb();
        var otherTenant = new TenantEntity { Id = Guid.NewGuid(), Name = "Other", SchemaPrefix = "o", Status = "ACTIVE" };
        db.Tenants.Add(otherTenant);

        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "id|other", Email = "other@test.com",
            DisplayName = "Other Supplier", Role = "SUPPLIER", TenantId = otherTenant.Id, IsActive = true
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);

        var handler = new GetSupplierEngagement.Handler(db, currentUser);
        var result = await handler.Handle(new GetSupplierEngagement.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSuppliers.Should().Be(0);
    }
}
