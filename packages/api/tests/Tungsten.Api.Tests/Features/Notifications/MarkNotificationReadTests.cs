using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Notifications;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Notifications;

public class MarkNotificationReadTests
{
    [Fact]
    public async Task Handle_ExistingNotification_MarksAsRead()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|s", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);

        var notification = new NotificationEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id,
            Type = "COMPLIANCE_FLAG", Title = "Test", Message = "test",
            IsRead = false, CreatedAt = DateTime.UtcNow
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(user.IdentityUserId);

        var handler = new MarkNotificationRead.Handler(db, currentUser);
        var result = await handler.Handle(new MarkNotificationRead.Command(notification.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var updated = await db.Notifications.FirstAsync(n => n.Id == notification.Id);
        updated.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OtherUsersNotification_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var userA = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|a", Email = "a@t.com",
            DisplayName = "A", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        var userB = new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "auth0|b", Email = "b@t.com",
            DisplayName = "B", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.AddRange(userA, userB);

        var notification = new NotificationEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = userA.Id,
            Type = "COMPLIANCE_FLAG", Title = "Test", Message = "test",
            IsRead = false, CreatedAt = DateTime.UtcNow
        };
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();

        // User B tries to mark user A's notification as read
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IdentityUserId.Returns(userB.IdentityUserId);

        var handler = new MarkNotificationRead.Handler(db, currentUser);
        var result = await handler.Handle(new MarkNotificationRead.Command(notification.Id), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }
}
