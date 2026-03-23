using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class StripeWebhookTests
{
    [Fact]
    public async Task HandleCheckoutCompleted_CreatesTenantAndUser()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);
        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();

        var handler = new StripeWebhookHandler(db, logger);
        await handler.HandleCheckoutCompleted("cus_test123", "sub_test123", "Acme Mining", "John Smith", "john@acme.com");

        var tenant = await db.Tenants.FirstOrDefaultAsync();
        Assert.NotNull(tenant);
        Assert.Equal("Acme Mining", tenant.Name);
        Assert.Equal("TRIAL", tenant.Status);
        Assert.Equal("cus_test123", tenant.StripeCustomerId);
        Assert.Equal("sub_test123", tenant.StripeSubscriptionId);
        Assert.Equal("PRO", tenant.PlanName);
        Assert.NotNull(tenant.TrialEndsAt);

        var user = await db.Users.FirstOrDefaultAsync();
        Assert.NotNull(user);
        Assert.Equal("john@acme.com", user.Email);
        Assert.Equal("TENANT_ADMIN", user.Role);
        Assert.StartsWith("pending|", user.Auth0Sub);
    }

    [Fact]
    public async Task HandleCheckoutCompleted_DuplicateEmail_SkipsCreation()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Existing", SchemaPrefix = "existing", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), Auth0Sub = "auth0|x", Email = "john@acme.com", DisplayName = "X", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();
        var handler = new StripeWebhookHandler(db, logger);
        await handler.HandleCheckoutCompleted("cus_456", "sub_456", "Acme Mining", "John Smith", "john@acme.com");

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

        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();
        var handler = new StripeWebhookHandler(db, logger);
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

        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();
        var handler = new StripeWebhookHandler(db, logger);
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

        var logger = Substitute.For<ILogger<StripeWebhookHandler>>();
        var handler = new StripeWebhookHandler(db, logger);
        await handler.HandleSubscriptionDeleted("sub_test");

        var tenant = await db.Tenants.FirstAsync();
        Assert.Equal("CANCELLED", tenant.Status);
    }
}
