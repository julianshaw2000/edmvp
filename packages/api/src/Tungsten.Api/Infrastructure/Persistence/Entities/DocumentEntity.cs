namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class DocumentEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? CustodyEventId { get; set; }
    public Guid BatchId { get; set; }
    public required string FileName { get; set; }
    public required string StorageKey { get; set; }
    public long FileSizeBytes { get; set; }
    public required string ContentType { get; set; }
    public required string Sha256Hash { get; set; }
    public required string DocumentType { get; set; }
    public Guid UploadedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
    public CustodyEventEntity? CustodyEvent { get; set; }
    public BatchEntity Batch { get; set; } = null!;
    public UserEntity Uploader { get; set; } = null!;
}
