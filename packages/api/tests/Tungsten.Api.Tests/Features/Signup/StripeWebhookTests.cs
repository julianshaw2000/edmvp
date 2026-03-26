using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class StripeWebhookTests
{
    private static StripeWebhookHandler CreateHandler(AppDbContext db)
    {
        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();
        var emailService = Substitute.For<IEmailService>();
        var config = new ConfigurationBuilder().Build();
        return new StripeWebhookHandler(db, logger, emailService, config);
    }

    [Fact]
    public async Task HandleCheckoutCompleted_CreatesTenantAndUser()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var handler = CreateHandler(db);
        await handler.HandleCheckoutCompleted("cus_test123", "sub_test123", "Acme Mining", "John Smith", "john@acme.com", "PRO");

        var tenant = await db.Tenants.FirstOrDefaultAsync();
        Assert.NotNull(tenant);
        Assert.Equal("Acme Mining", tenant.Name);
        Assert.Equal("TRIAL", tenant.Status);
        Assert.Equal("cus_test123", tenant.StripeCustomerId);
        Assert.Equal("sub_test123", tenant.StripeSubscriptionId);
        Assert.Equal("PRO", tenant.PlanName);
        Assert.Null(tenant.MaxBatches);
        Assert.Null(tenant.MaxUsers);
        Assert.NotNull(tenant.TrialEndsAt);

        var user = await db.Users.FirstOrDefaultAsync();
        Assert.NotNull(user);
        Assert.Equal("john@acme.com", user.Email);
        Assert.Equal("TENANT_ADMIN", user.Role);
        Assert.StartsWith("pending|", user.EntraOid);
    }

    [Fact]
    public async Task HandleCheckoutCompleted_DuplicateEmail_SkipsCreation()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Existing", SchemaPrefix = "existing", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), EntraOid = "auth0|x", Email = "john@acme.com", DisplayName = "X", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        await handler.HandleCheckoutCompleted("cus_456", "sub_456", "Acme Mining", "John Smith", "john@acme.com", "PRO");

        Assert.Equal(1, await db.Tenants.CountAsync());
    }

    [Fact]
    public async Task HandleInvoicePaid_TransitionsTrialToActive()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        db.Tenants.Add(new TenantEntity { Id = Guid.NewGuid(), Name = "Acme", SchemaPrefix = "acme", Status = "TRIAL", StripeSubscriptionId = "sub_test", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        await handler.HandleInvoicePaid("sub_test");

        var tenant = await db.Tenants.FirstAsync();
        Assert.Equal("ACTIVE", tenant.Status);
    }

    [Fact]
    public async Task HandlePaymentFailed_SuspendsTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        db.Tenants.Add(new TenantEntity { Id = Guid.NewGuid(), Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", StripeSubscriptionId = "sub_test", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        await handler.HandlePaymentFailed("sub_test");

        var tenant = await db.Tenants.FirstAsync();
        Assert.Equal("SUSPENDED", tenant.Status);
    }

    [Fact]
    public async Task HandleSubscriptionDeleted_CancelsTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        db.Tenants.Add(new TenantEntity { Id = Guid.NewGuid(), Name = "Acme", SchemaPrefix = "acme", Status = "ACTIVE", StripeSubscriptionId = "sub_test", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db);
        await handler.HandleSubscriptionDeleted("sub_test");

        var tenant = await db.Tenants.FirstAsync();
        Assert.Equal("CANCELLED", tenant.Status);
    }

    [Fact]
    public async Task HandleCheckoutCompleted_StarterPlan_SetsLimits()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var handler = CreateHandler(db);
        await handler.HandleCheckoutCompleted("cus_s", "sub_s", "Small Co", "Jane", "jane@small.com", "STARTER");

        var tenant = await db.Tenants.FirstOrDefaultAsync();
        Assert.NotNull(tenant);
        Assert.Equal("STARTER", tenant!.PlanName);
        Assert.Equal(50, tenant.MaxBatches);
        Assert.Equal(5, tenant.MaxUsers);
    }
}
