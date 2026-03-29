namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class FormSdFilingCycleEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public int ReportingYear { get; set; }
    public DateTime DueDate { get; set; }
    public required string Status { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
