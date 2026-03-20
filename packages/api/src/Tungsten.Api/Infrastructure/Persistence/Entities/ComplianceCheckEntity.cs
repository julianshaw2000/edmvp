using System.Text.Json;

namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class ComplianceCheckEntity
{
    public Guid Id { get; set; }
    public Guid CustodyEventId { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public required string Framework { get; set; }
    public required string Status { get; set; }
    public JsonElement? Details { get; set; }
    public DateTime CheckedAt { get; set; }
    public string RuleVersion { get; set; } = "1.0.0-pilot";
    public CustodyEventEntity CustodyEvent { get; set; } = null!;
    public BatchEntity Batch { get; set; } = null!;
}
