namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class TenantEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string SchemaPrefix { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<UserEntity> Users { get; set; } = [];
    public ICollection<BatchEntity> Batches { get; set; } = [];
}
