namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class WebhookEndpointEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty; // HMAC signing secret
    public string Events { get; set; } = string.Empty; // Comma-separated: "batch.created,event.created,compliance.completed"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public TenantEntity Tenant { get; set; } = null!;
}
