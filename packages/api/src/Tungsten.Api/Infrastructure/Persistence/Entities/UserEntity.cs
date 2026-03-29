namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class UserEntity
{
    public Guid Id { get; set; }
    public required string IdentityUserId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string Role { get; set; }
    public Guid TenantId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? StripeSessionId { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
}
