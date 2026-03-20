using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.CustodyEvents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class CreateCustodyEventTests
{
    private static (AppDbContext db, TenantEntity tenant, UserEntity user, BatchEntity batch) SetupDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@test.com",
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
        db.SaveChanges();

        return (db, tenant, user, batch);
    }

    [Fact]
    public async Task Handle_ValidEvent_CreatesWithHashAndIdempotencyKey()
    {
        var (db, tenant, user, batch) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0",
            mineOperatorIdentity = "Mining Corp",
            mineralogicalCertificateRef = "CERT-001"
        });

        var publisher = Substitute.For<IPublisher>();
        var handler = new CreateCustodyEvent.Handler(db, currentUser, publisher);
        var command = new CreateCustodyEvent.Command(
            batch.Id, "MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            "Bisie Mine", null, "Mining Corp", null,
            "First extraction", metadata);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sha256Hash.Should().HaveLength(64);
        result.Value.PreviousEventHash.Should().BeNull();

        var saved = await db.CustodyEvents.FirstAsync();
        saved.IdempotencyKey.Should().NotBeNullOrEmpty();
        saved.Sha256Hash.Should().Be(result.Value.Sha256Hash);
    }

    [Fact]
    public async Task Handle_SecondEvent_LinksPreviousHash()
    {
        var (db, tenant, user, batch) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        // Create first event directly in DB
        var firstEvent = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "key1",
            EventDate = DateTime.UtcNow, Location = "Bisie", ActorName = "Corp",
            Description = "First", Sha256Hash = "aabbccdd" + new string('0', 56),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(firstEvent);
        db.SaveChanges();

        var metadata = JsonSerializer.SerializeToElement(new
        {
            facilityName = "Processor",
            processDescription = "Concentration",
            inputWeightKg = 500.0,
            outputWeightKg = 400.0,
            concentrationRatio = 1.25
        });

        var publisher = Substitute.For<IPublisher>();
        var handler = new CreateCustodyEvent.Handler(db, currentUser, publisher);
        var command = new CreateCustodyEvent.Command(
            batch.Id, "CONCENTRATION", "2026-01-16T10:00:00Z",
            "Facility", null, "Processor Inc", null,
            "Concentration step", metadata);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PreviousEventHash.Should().Be(firstEvent.Sha256Hash);
    }

    [Fact]
    public async Task Handle_DuplicateIdempotencyKey_ReturnsFailure()
    {
        var (db, tenant, user, batch) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0",
            mineOperatorIdentity = "Mining Corp",
            mineralogicalCertificateRef = "CERT-001"
        });

        var publisher = Substitute.For<IPublisher>();
        var handler = new CreateCustodyEvent.Handler(db, currentUser, publisher);
        var command = new CreateCustodyEvent.Command(
            batch.Id, "MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            "Bisie Mine", null, "Mining Corp", null,
            "First extraction", metadata);

        // First call succeeds
        var result1 = await handler.Handle(command, CancellationToken.None);
        result1.IsSuccess.Should().BeTrue();

        // Second call with same key fields fails
        var result2 = await handler.Handle(command, CancellationToken.None);
        result2.IsSuccess.Should().BeFalse();
        result2.Error.Should().Contain("Duplicate");
    }

    [Fact]
    public async Task Handle_InvalidMetadata_ReturnsFailure()
    {
        var (db, tenant, user, batch) = SetupDb();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var metadata = JsonSerializer.SerializeToElement(new { /* missing required fields */ });

        var publisher = Substitute.For<IPublisher>();
        var handler = new CreateCustodyEvent.Handler(db, currentUser, publisher);
        var command = new CreateCustodyEvent.Command(
            batch.Id, "MINE_EXTRACTION", "2026-01-15T10:00:00Z",
            "Bisie Mine", null, "Mining Corp", null,
            "First extraction", metadata);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("metadata");
    }
}
