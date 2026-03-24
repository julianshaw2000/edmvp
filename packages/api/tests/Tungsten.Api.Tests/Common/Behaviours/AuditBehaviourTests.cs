using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Security.Claims;
using Tungsten.Api.Common;
using Tungsten.Api.Common.Audit;
using Tungsten.Api.Common.Auth;
using Tungsten.Api.Common.Behaviours;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Common.Behaviours;

public class AuditBehaviourTests
{
    public record TestAuditableCommand(string Name) : IRequest<Result<TestResponse>>, IAuditable
    {
        public string AuditAction => "TestAction";
        public string EntityType => "TestEntity";
    }

    public record TestResponse(Guid Id, string Name);

    public record TestNonAuditableCommand(string Name) : IRequest<Result<TestResponse>>;

    public record TestAuditableEventCommand(string Name, Guid BatchId) : IRequest<Result<TestResponse>>, IAuditable
    {
        public string AuditAction => "CreateCustodyEvent";
        public string EntityType => "CustodyEvent";
    }

    // Non-generic Result command (like UpdateUser)
    public record TestVoidCommand(string Name) : IRequest<Result>, IAuditable
    {
        public string AuditAction => "UpdateUser";
        public string EntityType => "User";
    }

    // Batch entity type command (BatchId = EntityId)
    public record TestBatchCommand(string Name) : IRequest<Result<TestResponse>>, IAuditable
    {
        public string AuditAction => "CreateBatch";
        public string EntityType => "Batch";
    }

    private static (AppDbContext db, AuditBehaviour<TRequest, TResponse> behaviour) CreateBehaviour<TRequest, TResponse>(
        string auth0Sub = "auth0|test") where TRequest : IRequest<TResponse>
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Test", SchemaPrefix = "test", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = userId, Auth0Sub = auth0Sub, Email = "t@t.com", DisplayName = "T", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        db.SaveChanges();

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, auth0Sub) };
        var identity = new ClaimsIdentity(claims, "Test");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
        httpContext.Request.Headers["User-Agent"] = "TestAgent";

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUser = new CurrentUserService(accessor, db);
        var logger = Substitute.For<ILogger<AuditBehaviour<TRequest, TResponse>>>();
        var webhookDispatch = Substitute.For<IWebhookDispatchService>();

        var behaviour = new AuditBehaviour<TRequest, TResponse>(db, currentUser, accessor, logger, webhookDispatch);
        return (db, behaviour);
    }

    [Fact]
    public async Task Handle_AuditableCommand_Success_WritesAuditLog()
    {
        var (db, behaviour) = CreateBehaviour<TestAuditableCommand, Result<TestResponse>>();
        var entityId = Guid.NewGuid();
        var response = Result<TestResponse>.Success(new TestResponse(entityId, "test"));

        var result = await behaviour.Handle(
            new TestAuditableCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("TestAction", log.Action);
        Assert.Equal("TestEntity", log.EntityType);
        Assert.Equal(entityId, log.EntityId);
        Assert.Equal("Success", log.Result);
        Assert.Null(log.FailureReason);
    }

    [Fact]
    public async Task Handle_AuditableCommand_Failure_WritesAuditLogWithReason()
    {
        var (db, behaviour) = CreateBehaviour<TestAuditableCommand, Result<TestResponse>>();
        var response = Result<TestResponse>.Failure("Not found");

        var result = await behaviour.Handle(
            new TestAuditableCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("Failure", log.Result);
        Assert.Equal("Not found", log.FailureReason);
        Assert.Null(log.EntityId);
    }

    [Fact]
    public async Task Handle_NonAuditableCommand_SkipsLogging()
    {
        var (db, behaviour) = CreateBehaviour<TestNonAuditableCommand, Result<TestResponse>>();
        var response = Result<TestResponse>.Success(new TestResponse(Guid.NewGuid(), "test"));

        await behaviour.Handle(
            new TestNonAuditableCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        var count = await db.AuditLogs.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Handle_AuditableCommand_ExtractsBatchId_FromCommandProperty()
    {
        var (db, behaviour) = CreateBehaviour<TestAuditableEventCommand, Result<TestResponse>>();
        var batchId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var response = Result<TestResponse>.Success(new TestResponse(entityId, "test"));

        await behaviour.Handle(
            new TestAuditableEventCommand("test", batchId),
            _ => Task.FromResult(response),
            CancellationToken.None);

        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(batchId, log.BatchId);
        Assert.Equal(entityId, log.EntityId);
    }

    [Fact]
    public async Task Handle_NoHttpContext_SkipsLogging()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var accessor = new HttpContextAccessor { HttpContext = null };
        var currentUser = Substitute.For<ICurrentUserService>();
        var logger = Substitute.For<ILogger<AuditBehaviour<TestAuditableCommand, Result<TestResponse>>>>();
        var webhookDispatch = Substitute.For<IWebhookDispatchService>();

        var behaviour = new AuditBehaviour<TestAuditableCommand, Result<TestResponse>>(db, currentUser, accessor, logger, webhookDispatch);
        var response = Result<TestResponse>.Success(new TestResponse(Guid.NewGuid(), "test"));

        await behaviour.Handle(
            new TestAuditableCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        var count = await db.AuditLogs.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Handle_NonGenericResult_Failure_WritesAuditLogWithReason()
    {
        var (db, behaviour) = CreateBehaviour<TestVoidCommand, Result>();
        var response = Result.Failure("User not found");

        var result = await behaviour.Handle(
            new TestVoidCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("Failure", log.Result);
        Assert.Equal("User not found", log.FailureReason);
        Assert.Equal("UpdateUser", log.Action);
    }

    [Fact]
    public async Task Handle_BatchEntityType_SetsBatchIdToEntityId()
    {
        var (db, behaviour) = CreateBehaviour<TestBatchCommand, Result<TestResponse>>();
        var entityId = Guid.NewGuid();
        var response = Result<TestResponse>.Success(new TestResponse(entityId, "batch"));

        await behaviour.Handle(
            new TestBatchCommand("test"),
            _ => Task.FromResult(response),
            CancellationToken.None);

        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal(entityId, log.EntityId);
        Assert.Equal(entityId, log.BatchId);
    }
}
