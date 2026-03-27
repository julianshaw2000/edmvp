using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.CustodyEvents;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.CustodyEvents;

public class VerifyIntegrityTests
{
    [Fact]
    public async Task Handle_ValidChain_ReturnsIntact()
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

        // Build a valid chain manually
        var hash1 = HashService.ComputeEventHash("MINE_EXTRACTION",
            HashService.NormalizeDate("2026-01-15T10:00:00Z"),
            batch.Id, "Bisie", "Corp", null, "First", "{}", null);

        db.CustodyEvents.Add(new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.Parse("2026-01-15T10:00:00Z").ToUniversalTime(),
            Location = "Bisie", ActorName = "Corp", Description = "First",
            Sha256Hash = hash1, PreviousEventHash = null,
            CreatedBy = user.Id, CreatedAt = DateTime.Parse("2026-01-15T10:00:00Z").ToUniversalTime()
        });

        var hash2 = HashService.ComputeEventHash("CONCENTRATION",
            HashService.NormalizeDate("2026-01-16T10:00:00Z"),
            batch.Id, "Facility", "Processor", null, "Second", "{}", hash1);

        db.CustodyEvents.Add(new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "CONCENTRATION", IdempotencyKey = "k2",
            EventDate = DateTime.Parse("2026-01-16T10:00:00Z").ToUniversalTime(),
            Location = "Facility", ActorName = "Processor", Description = "Second",
            Sha256Hash = hash2, PreviousEventHash = hash1,
            CreatedBy = user.Id, CreatedAt = DateTime.Parse("2026-01-16T10:00:00Z").ToUniversalTime()
        });

        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);

        var handler = new VerifyIntegrity.Handler(db, currentUser);
        var result = await handler.Handle(new VerifyIntegrity.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeTrue();
        result.Value.EventCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_TamperedHash_ReturnsNotIntact()
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

        // Create event with a WRONG hash (simulating tampering)
        db.CustodyEvents.Add(new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "k1",
            EventDate = DateTime.Parse("2026-01-15T10:00:00Z").ToUniversalTime(),
            Location = "Bisie", ActorName = "Corp", Description = "First",
            Sha256Hash = "tampered_hash_" + new string('0', 50),  // 64 chars total
            PreviousEventHash = null,
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        });

        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);

        var handler = new VerifyIntegrity.Handler(db, currentUser);
        var result = await handler.Handle(new VerifyIntegrity.Query(batch.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsIntact.Should().BeFalse();
        result.Value.FirstTamperedEventId.Should().NotBeNull();
    }
}
