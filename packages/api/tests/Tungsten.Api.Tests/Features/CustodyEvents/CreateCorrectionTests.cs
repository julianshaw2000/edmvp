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

public class CreateCorrectionTests
{
    [Fact]
    public async Task Handle_ValidCorrection_LinksToOriginalEvent()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), EntraOid = "auth0|s", Email = "s@test.com",
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

        var originalEvent = new CustodyEventEntity
        {
            Id = Guid.NewGuid(), BatchId = batch.Id, TenantId = tenant.Id,
            EventType = "MINE_EXTRACTION", IdempotencyKey = "key1",
            EventDate = DateTime.UtcNow, Location = "Bisie", ActorName = "Corp",
            Description = "Original", Sha256Hash = new string('a', 64),
            CreatedBy = user.Id, CreatedAt = DateTime.UtcNow
        };
        db.CustodyEvents.Add(originalEvent);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.EntraOid.Returns(user.EntraOid);

        var metadata = JsonSerializer.SerializeToElement(new
        {
            gpsCoordinates = "-1.5,29.0",
            mineOperatorIdentity = "Corrected Mining Corp",
            mineralogicalCertificateRef = "CERT-002"
        });

        var publisher = Substitute.For<IPublisher>();
        var handler = new CreateCorrection.Handler(db, currentUser, publisher);
        var command = new CreateCorrection.Command(
            originalEvent.Id, "2026-01-15T10:00:00Z",
            "Bisie Mine", null, "Corrected Mining Corp", null,
            "Corrected extraction details", metadata);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsCorrection.Should().BeTrue();
        result.Value.CorrectsEventId.Should().Be(originalEvent.Id);
    }
}
