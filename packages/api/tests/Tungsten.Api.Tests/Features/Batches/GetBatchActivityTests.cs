using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Features.Batches;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Batches;

public class GetBatchActivityTests
{
    [Fact]
    public async Task Handle_ReturnsBatchAuditEntries_FilteredByBatchIdAndTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var otherBatchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "T", SchemaPrefix = "t", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, IdentityUserId = "auth0|u", Email = "u@u.com", DisplayName = "User", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Action = "CreateBatch", EntityType = "Batch", EntityId = batchId, BatchId = batchId, Result = "Success", Timestamp = DateTime.UtcNow.AddMinutes(-2) });
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Action = "CreateCustodyEvent", EntityType = "CustodyEvent", BatchId = batchId, Result = "Success", Timestamp = DateTime.UtcNow.AddMinutes(-1) });
        db.AuditLogs.Add(new AuditLogEntity { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Action = "CreateBatch", EntityType = "Batch", EntityId = otherBatchId, BatchId = otherBatchId, Result = "Success", Timestamp = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.GetTenantIdAsync(Arg.Any<CancellationToken>()).Returns(tenantId);

        var handler = new GetBatchActivity.Handler(db, currentUser);
        var result = await handler.Handle(new GetBatchActivity.Query(batchId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal("CreateBatch", result.Value[0].Action); // chronological order (oldest first)
    }
}
