using System.Text.Json;

namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class CustodyEventEntity
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public required string EventType { get; set; }
    public required string IdempotencyKey { get; set; }
    public DateTime EventDate { get; set; }
    public required string Location { get; set; }
    public string? GpsCoordinates { get; set; }
    public required string ActorName { get; set; }
    public string? SmelterId { get; set; }
    public required string Description { get; set; }
    public JsonElement? Metadata { get; set; }
    public int SchemaVersion { get; set; } = 1;
    public bool IsCorrection { get; set; }
    public Guid? CorrectsEventId { get; set; }
    public required string Sha256Hash { get; set; }
    public string? PreviousEventHash { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public BatchEntity Batch { get; set; } = null!;
    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity Creator { get; set; } = null!;
    public CustodyEventEntity? CorrectsEvent { get; set; }
    public ICollection<DocumentEntity> Documents { get; set; } = [];
    public ICollection<ComplianceCheckEntity> ComplianceChecks { get; set; } = [];
}
