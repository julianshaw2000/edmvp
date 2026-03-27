using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Compliance.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Compliance;

public class ComplianceNotificationServiceTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(options);
    }

    // ─── scenario 1: FAIL status → creates notifications ─────────────────────

    [Fact]
    public async Task CreateNotifications_FailStatus_NotifiesSupplierBuyersAndAdmin()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FAIL",
            "Smelter non-conformant", CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().HaveCount(3); // supplier + buyer + admin
        notifications.Should().OnlyContain(n => n.Type == "COMPLIANCE_FLAG");
    }

    [Fact]
    public async Task CreateNotifications_FailStatus_NotificationContainsFailInTitle()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FAIL",
            "Smelter non-conformant", CancellationToken.None);

        var notification = await db.Notifications.FirstAsync();
        notification.Title.Should().Contain("FAIL");
    }

    [Fact]
    public async Task CreateNotifications_FailStatus_MessageMatchesDetail()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);
        const string detail = "Smelter non-conformant per RMAP";

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FAIL",
            detail, CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().OnlyContain(n => n.Message == detail);
    }

    [Fact]
    public async Task CreateNotifications_FailStatus_NotificationsAreUnread()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FAIL",
            "test", CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().OnlyContain(n => !n.IsRead);
    }

    // ─── scenario 2: FLAG status → creates notifications ─────────────────────

    [Fact]
    public async Task CreateNotifications_FlagStatus_CreatesNotifications()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FLAG",
            "High-risk origin country", CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().HaveCount(3); // supplier + buyer + admin
    }

    [Fact]
    public async Task CreateNotifications_FlagStatus_NotificationContainsFlagInTitle()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FLAG",
            "High-risk origin country", CancellationToken.None);

        var notification = await db.Notifications.FirstAsync();
        notification.Title.Should().Contain("FLAG");
    }

    // ─── scenario 3: PASS status → no notifications ───────────────────────────

    [Fact]
    public async Task CreateNotifications_PassStatus_NoNotifications()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "PASS",
            "All good", CancellationToken.None);

        var count = await db.Notifications.CountAsync();
        count.Should().Be(0);
    }

    // ─── scenario 4: INSUFFICIENT_DATA status → no notifications ─────────────

    [Fact]
    public async Task CreateNotifications_InsufficientDataStatus_NoNotifications()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "INSUFFICIENT_DATA",
            "Missing documents", CancellationToken.None);

        var count = await db.Notifications.CountAsync();
        count.Should().Be(0);
    }

    // ─── scenario 5: Multiple buyers → each gets a notification ──────────────

    [Fact]
    public async Task CreateNotifications_MultipleBuyers_EachGetsNotification()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "s1", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        var buyer1 = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "b1", Email = "b1@t.com",
            DisplayName = "B1", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        };
        var buyer2 = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "b2", Email = "b2@t.com",
            DisplayName = "B2", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        };
        var buyer3 = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "b3", Email = "b3@t.com",
            DisplayName = "B3", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(supplier, buyer1, buyer2, buyer3);
        await db.SaveChangesAsync();

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FAIL",
            "test", CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        // supplier + 3 buyers = 4
        notifications.Should().HaveCount(4);
        notifications.Select(n => n.UserId).Should().Contain(buyer1.Id);
        notifications.Select(n => n.UserId).Should().Contain(buyer2.Id);
        notifications.Select(n => n.UserId).Should().Contain(buyer3.Id);
    }

    // ─── scenario 6: Inactive users → not notified ───────────────────────────

    [Fact]
    public async Task CreateNotifications_InactiveUsers_NotNotified()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "s1", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        var inactiveBuyer = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "b1", Email = "b1@t.com",
            DisplayName = "B1", Role = "BUYER", TenantId = tenant.Id, IsActive = false
        };
        var inactiveAdmin = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "a1", Email = "a1@t.com",
            DisplayName = "A1", Role = "PLATFORM_ADMIN", TenantId = tenant.Id, IsActive = false
        };
        db.Users.AddRange(supplier, inactiveBuyer, inactiveAdmin);
        await db.SaveChangesAsync();

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FAIL",
            "test", CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        // Only the active supplier should be notified
        notifications.Should().HaveCount(1);
        notifications[0].UserId.Should().Be(supplier.Id);
    }

    [Fact]
    public async Task CreateNotifications_AllInactiveExceptBuyer_OnlyBuyerNotified()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var inactiveSupplier = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "s1", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = false
        };
        var activeBuyer = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "b1", Email = "b1@t.com",
            DisplayName = "B1", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(inactiveSupplier, activeBuyer);
        await db.SaveChangesAsync();

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, inactiveSupplier.Id, Guid.NewGuid(), "FAIL",
            "test", CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        // Inactive supplier is not notified; active buyer is
        notifications.Should().HaveCount(1);
        notifications[0].UserId.Should().Be(activeBuyer.Id);
    }

    // ─── referenceId is stored on the notification ────────────────────────────

    [Fact]
    public async Task CreateNotifications_FailStatus_ReferenceIdStored()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);
        var referenceId = Guid.NewGuid();

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, referenceId, "FAIL",
            "test", CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().OnlyContain(n => n.ReferenceId == referenceId);
    }

    // ─── tenantId is stored on the notification ───────────────────────────────

    [Fact]
    public async Task CreateNotifications_CorrectTenantIdStored()
    {
        var db = CreateDb();
        var (tenant, supplier, _, _) = await SeedTenantWithUsers(db);

        await ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, supplier.Id, Guid.NewGuid(), "FAIL",
            "test", CancellationToken.None);

        var notifications = await db.Notifications.ToListAsync();
        notifications.Should().OnlyContain(n => n.TenantId == tenant.Id);
    }

    // ─── no users in tenant → no notifications, no exception ─────────────────

    [Fact]
    public async Task CreateNotifications_NoUsersInTenant_NoNotifications()
    {
        var db = CreateDb();
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        var act = () => ComplianceNotificationService.CreateNotificationsAsync(
            db, tenant.Id, Guid.NewGuid(), Guid.NewGuid(), "FAIL",
            "test", CancellationToken.None);

        await act.Should().NotThrowAsync();
        var count = await db.Notifications.CountAsync();
        count.Should().Be(0);
    }

    // ─── helpers ─────────────────────────────────────────────────────────────

    private static async Task<(TenantEntity tenant, UserEntity supplier, UserEntity buyer, UserEntity admin)>
        SeedTenantWithUsers(AppDbContext db)
    {
        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);

        var supplier = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "s1", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        var buyer = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "b1", Email = "b@t.com",
            DisplayName = "B", Role = "BUYER", TenantId = tenant.Id, IsActive = true
        };
        var admin = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "a1", Email = "a@t.com",
            DisplayName = "A", Role = "PLATFORM_ADMIN", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(supplier, buyer, admin);
        await db.SaveChangesAsync();

        return (tenant, supplier, buyer, admin);
    }
}
