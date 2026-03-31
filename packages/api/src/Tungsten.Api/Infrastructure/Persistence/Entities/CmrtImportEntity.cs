namespace Tungsten.Api.Infrastructure.Persistence.Entities;

public class CmrtImportEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string FileName { get; set; }
    public required string DeclarationCompany { get; set; }
    public int? ReportingYear { get; set; }
    public int RowsParsed { get; set; }
    public int RowsMatched { get; set; }
    public int RowsUnmatched { get; set; }
    public int Errors { get; set; }
    public Guid ImportedBy { get; set; }
    public DateTime ImportedAt { get; set; }
    public TenantEntity Tenant { get; set; } = null!;
    public UserEntity Importer { get; set; } = null!;
}
