namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class TenantEntity
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string SchemaPrefix { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? PlanName { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public int? MaxBatches { get; set; }
    public int? MaxUsers { get; set; }
    public string[]? Regulations { get; set; }
    public ICollection<UserEntity> Users { get; set; } = [];
    public ICollection<BatchEntity> Batches { get; set; } = [];
}
