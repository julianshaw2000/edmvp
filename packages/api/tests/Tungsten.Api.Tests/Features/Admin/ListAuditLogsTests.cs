using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Admin;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Admin;

public class ListAuditLogsTests
{
    [Fact]
    public async Task Handle_ReturnsPagedAuditLogs_FilteredByTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();

        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T1", SchemaPrefix = "t1", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Tenants.Add(new TenantEntity { Id = otherTenantId, Name = "T2", SchemaPrefix = "t2", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, IdentityUserId = "auth0|admin", Email = "a@a.com", DisplayName = "Admin", Role = "PLATFORM_ADMIN", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Action = "CreateBatch", EntityType = "Batch", Result = "Success", Timestamp = DateTime.UtcNow });
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = otherTenantId, UserId = userId, Action = "CreateBatch", EntityType = "Batch", Result = "Success", Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);
        currentUser.GetRoleAsync(Arg.Any<CancellationToken>()).Returns("TENANT_ADMIN");

        var handler = new ListAuditLogs.Handler(db, currentUser);
        var result = await handler.Handle(new ListAuditLogs.Query(1, 20, null, null, null, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
    }
}
