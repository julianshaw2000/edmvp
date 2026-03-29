using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tungsten.Api.Common.Services;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class ResendSetupEmailTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Returns200_WhenEmailNotFound_DoesNotSendEmail()
    {
        var db = CreateDb();
        var emailService = Substitute.For<IEmailService>();
        var config = new ConfigurationBuilder().Build();

        var request = new ResendSetupEmail.Request("nobody@nowhere.com");
        var result = await ResendSetupEmail.Handle(request, db, emailService, config, NullLoggerFactory.Instance, CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok>(result);
        await emailService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendsSetupEmail_WhenPendingUserFound()
    {
        var db = CreateDb();
        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Acme Corp", SchemaPrefix = "acme", Status = "TRIAL", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity
        {
            Id = Guid.NewGuid(), IdentityUserId = "pending|abc", Email = "jane@acme.com",
            DisplayName = "Jane", Role = "TENANT_ADMIN", TenantId = tenantId,
            StripeSessionId = "cs_test_xyz", IsActive = true,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var emailService = Substitute.For<IEmailService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BaseUrl"] = "https://test.example.com" })
            .Build();

        var request = new ResendSetupEmail.Request("jane@acme.com");
        var result = await ResendSetupEmail.Handle(request, db, emailService, config, NullLoggerFactory.Instance, CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok>(result);
        await emailService.Received(1).SendAsync(
            "jane@acme.com",
            Arg.Any<string>(),
            Arg.Is<string>(h => h.Contains("cs_test_xyz")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
