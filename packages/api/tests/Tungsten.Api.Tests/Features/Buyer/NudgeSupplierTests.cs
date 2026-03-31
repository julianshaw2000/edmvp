using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Buyer;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Buyer;

public class NudgeSupplierTests
{
    private static (AppDbContext db, Guid tenantId, UserEntity supplier) CreateDbWithSupplier()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "TestCo", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "id|s1", Email = "supplier@test.com",
            DisplayName = "Test Supplier", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(supplier);
        db.SaveChanges();
        return (db, tenant.Id, supplier);
    }

    [Fact]
    public async Task Handle_SendsNudgeEmail()
    {
        var (db, tenantId, supplier) = CreateDbWithSupplier();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);
        currentUser.GetUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<NudgeSupplier.Handler>>();

        var handler = new NudgeSupplier.Handler(db, currentUser, emailService, logger);
        var result = await handler.Handle(new NudgeSupplier.Command(supplier.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await emailService.Received(1).SendAsync(
            supplier.Email,
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreatesNotification()
    {
        var (db, tenantId, supplier) = CreateDbWithSupplier();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);
        currentUser.GetUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<NudgeSupplier.Handler>>();

        var handler = new NudgeSupplier.Handler(db, currentUser, emailService, logger);
        await handler.Handle(new NudgeSupplier.Command(supplier.Id), CancellationToken.None);

        var notification = await db.Notifications.FirstOrDefaultAsync(n => n.UserId == supplier.Id);
        notification.Should().NotBeNull();
        notification!.Type.Should().Be("BUYER_NUDGE");
    }

    [Fact]
    public async Task Handle_UpdatesLastNudgedAt()
    {
        var (db, tenantId, supplier) = CreateDbWithSupplier();
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);
        currentUser.GetUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<NudgeSupplier.Handler>>();

        var handler = new NudgeSupplier.Handler(db, currentUser, emailService, logger);
        await handler.Handle(new NudgeSupplier.Command(supplier.Id), CancellationToken.None);

        var updated = await db.Users.FindAsync(supplier.Id);
        updated!.LastNudgedAt.Should().NotBeNull();
        updated.LastNudgedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_RateLimits_Within7Days()
    {
        var (db, tenantId, supplier) = CreateDbWithSupplier();
        supplier.LastNudgedAt = DateTime.UtcNow.AddDays(-3);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);
        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<NudgeSupplier.Handler>>();

        var handler = new NudgeSupplier.Handler(db, currentUser, emailService, logger);
        var result = await handler.Handle(new NudgeSupplier.Command(supplier.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Please wait 7 days");
        await emailService.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AllowsNudge_After7Days()
    {
        var (db, tenantId, supplier) = CreateDbWithSupplier();
        supplier.LastNudgedAt = DateTime.UtcNow.AddDays(-8);
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);
        currentUser.GetUserIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());
        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<NudgeSupplier.Handler>>();

        var handler = new NudgeSupplier.Handler(db, currentUser, emailService, logger);
        var result = await handler.Handle(new NudgeSupplier.Command(supplier.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_RejectsSupplierFromOtherTenant()
    {
        var (db, tenantId, supplier) = CreateDbWithSupplier();
        var otherTenantId = Guid.NewGuid();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(otherTenantId);
        var emailService = Substitute.For<IEmailService>();
        var logger = Substitute.For<ILogger<NudgeSupplier.Handler>>();

        var handler = new NudgeSupplier.Handler(db, currentUser, emailService, logger);
        var result = await handler.Handle(new NudgeSupplier.Command(supplier.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Supplier not found");
    }
}
