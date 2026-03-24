namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class ApiKeyEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty; // SHA-256 hash of the key
    public string KeyPrefix { get; set; } = string.Empty; // First 11 chars for identification
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity CreatedBy { get; set; } = null!;
}
