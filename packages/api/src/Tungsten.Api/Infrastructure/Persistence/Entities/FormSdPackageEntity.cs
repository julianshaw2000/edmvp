namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class FormSdPackageEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int ReportingYear { get; set; }
    public required string StorageKey { get; set; }
    public required string Sha256Hash { get; set; }
    public required string RuleSetVersion { get; set; }
    public required string PlatformVersion { get; set; }
    public Guid GeneratedBy { get; set; }
    public string? SourceJson { get; set; }
    public DateTime GeneratedAt { get; set; }
}
