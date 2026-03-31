namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class TenantSmelterAssociationEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string SmelterId { get; set; }
    public required string Source { get; set; }
    public Guid? CmrtImportId { get; set; }
    public required string Status { get; set; }
    public required string MetalType { get; set; }
    public DateTime CreatedAt { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
    public RmapSmelterEntity? Smelter { get; set; }
    public CmrtImportEntity? CmrtImport { get; set; }
}
