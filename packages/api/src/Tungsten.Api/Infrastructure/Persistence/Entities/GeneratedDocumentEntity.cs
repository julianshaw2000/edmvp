namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class GeneratedDocumentEntity
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public required string DocumentType { get; set; }
    public required string StorageKey { get; set; }
    public Guid GeneratedBy { get; set; }
    public string? ShareToken { get; set; }
    public DateTime? ShareExpiresAt { get; set; }
    public DateTime GeneratedAt { get; set; }
    public BatchEntity Batch { get; set; } = null!;
    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity Generator { get; set; } = null!;
}
