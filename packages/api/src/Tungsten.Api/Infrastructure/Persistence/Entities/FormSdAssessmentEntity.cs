namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class FormSdAssessmentEntity
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public Guid TenantId { get; set; }
    public required string ApplicabilityStatus { get; set; }
    public required string RuleSetVersion { get; set; }
    public required string EngineVersion { get; set; }
    public string? Reasoning { get; set; }
    public Guid? SupersedesId { get; set; }
    public DateTime AssessedAt { get; set; }
}
