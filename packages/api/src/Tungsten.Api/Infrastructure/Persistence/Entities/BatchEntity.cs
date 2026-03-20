namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class BatchEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string BatchNumber { get; set; }
    public required string MineralType { get; set; }
    public required string OriginCountry { get; set; }
    public required string OriginMine { get; set; }
    public decimal WeightKg { get; set; }
    public required string Status { get; set; }
    public required string ComplianceStatus { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity Creator { get; set; } = null!;
    public ICollection<CustodyEventEntity> CustodyEvents { get; set; } = [];
    public ICollection<DocumentEntity> Documents { get; set; } = [];
}
