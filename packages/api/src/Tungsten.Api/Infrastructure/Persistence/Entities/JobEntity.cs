namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class JobEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string JobType { get; set; }
    public required string Status { get; set; }
    public Guid ReferenceId { get; set; }
    public string? ErrorDetail { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
}
