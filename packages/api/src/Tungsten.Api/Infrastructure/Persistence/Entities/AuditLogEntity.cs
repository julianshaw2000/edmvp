using System.Text.Json;

namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class AuditLogEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? BatchId { get; set; }
    public JsonElement? Payload { get; set; }
    public string Result { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }

    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity User { get; set; } = null!;
}
