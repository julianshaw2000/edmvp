using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Notifications;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Notifications;

public class ListNotificationsTests
{
    [Fact]
    public async Task Handle_UserWithNotifications_ReturnsOrderedByDate()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenant = new TenantEntity { Id = Guid.NewGuid(), Name = "T", SchemaPrefix = "t", Status = "ACTIVE" };
        db.Tenants.Add(tenant);
        var user = new UserEntity
        {
            Id = Guid.NewGuid(), Auth0Sub = "auth0|s", Email = "s@t.com",
            DisplayName = "S", Role = "SUPPLIER", TenantId = tenant.Id, IsActive = true
        };
        db.Users.Add(user);

        db.Notifications.Add(new NotificationEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id,
            Type = "COMPLIANCE_FLAG", Title = "Old", Message = "old",
            IsRead = true, CreatedAt = DateTime.UtcNow.AddHours(-2)
        });
        db.Notifications.Add(new NotificationEntity
        {
            Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id,
            Type = "COMPLIANCE_FLAG", Title = "New", Message = "new",
            IsRead = false, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.Auth0Sub.Returns(user.Auth0Sub);

        var handler = new ListNotifications.Handler(db, currentUser);
        var result = await handler.Handle(new ListNotifications.Query(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Title.Should().Be("New"); // most recent first
        result.Value.UnreadCount.Should().Be(1);
    }
}
