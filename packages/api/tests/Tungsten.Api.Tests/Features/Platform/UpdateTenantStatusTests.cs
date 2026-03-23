using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Platform;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Platform;

public class UpdateTenantStatusTests
{
    [Fact]
    public async Task Handle_SuspendsTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(Guid.NewGuid());

        var handler = new UpdateTenantStatus.Handler(db, currentUser);
        var result = await handler.Handle(
            new UpdateTenantStatus.Command(tenantId, "SUSPENDED"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("SUSPENDED", result.Value.Status);
    }

    [Fact]
    public async Task Handle_CannotSuspendOwnTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);

        var handler = new UpdateTenantStatus.Handler(db, currentUser);
        var result = await handler.Handle(
            new UpdateTenantStatus.Command(tenantId, "SUSPENDED"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("own tenant", result.Error!);
    }
}
