using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Batches;

public class CreateBatchTests
{
    private static (AppDbContext db, TenantEntity tenant, UserEntity user) SetupDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "Test", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|1", Email = "s@test.com",
            DisplayName = "Supplier", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);
        db.SaveChanges();

        return (db, tenant, user);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesBatch()
    {
        var (db, tenant, user) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new CreateBatch.Handler(db, currentUser);
        var command = new CreateBatch.Command(
            "BATCH-001", "tungsten", "CD", "Bisie Mine", 500.0m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.BatchNumber.Should().Be("BATCH-001");
        result.Value.Status.Should().Be("CREATED");
        result.Value.ComplianceStatus.Should().Be("PENDING");
    }

    [Fact]
    public async Task Handle_DuplicateBatchNumber_ReturnsFailure()
    {
        var (db, tenant, user) = SetupDb();
        db.Batches.Add(new BatchEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, BatchNumber = "BATCH-001",
            MineralType = "tungsten", OriginCountry = "CD", OriginMine = "Bisie",
            WeightKg = 100, Status = "CREATED", ComplianceStatus = "PENDING",
            CreatedBy = user.Id
        });
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new CreateBatch.Handler(db, currentUser);
        var command = new CreateBatch.Command(
            "BATCH-001", "tungsten", "CD", "Bisie Mine", 500.0m);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already exists");
    }
}
