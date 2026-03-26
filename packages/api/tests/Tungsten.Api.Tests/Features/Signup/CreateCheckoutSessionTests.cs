using Microsoft.EntityFrameworkCore;
using Tungsten.Api.Features.Signup;
using Tungsten.Api.Infrastructure.Persistence;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Tests.Features.Signup;

public class CreateCheckoutSessionTests
{
    [Fact]
    public async Task Handle_DuplicateEmail_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(options);

        var tenantId = Guid.NewGuid();
        db.Tenants.Add(new TenantEntity { Id = tenantId, Name = "Existing", SchemaPrefix = "existing", Status = "ACTIVE", CreatedAt = DateTime.UtcNow });
        db.Users.Add(new UserEntity { Id = Guid.NewGuid(), EntraOid = "auth0|x", Email = "taken@acme.com", DisplayName = "X", Role = "SUPPLIER", TenantId = tenantId, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var handler = new CreateCheckoutSession.Handler(db, null!);
        var result = await handler.Handle(
            new CreateCheckoutSession.Command("Acme", "John", "taken@acme.com"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("already in use", result.Error!);
    }
}
