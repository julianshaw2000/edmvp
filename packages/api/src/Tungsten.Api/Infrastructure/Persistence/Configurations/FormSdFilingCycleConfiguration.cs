using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class FormSdFilingCycleConfiguration : IEntityTypeConfiguration<FormSdFilingCycleEntity>
{
    public void Configure(EntityTypeBuilder<FormSdFilingCycleEntity> builder)
    {
        builder.ToTable("form_sd_filing_cycles");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Status).IsRequired().HasMaxLength(20);
        builder.HasIndex(c => new { c.TenantId, c.ReportingYear }).IsUnique();
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(c => c.UpdatedAt).HasDefaultValueSql("now()");
    }
}
