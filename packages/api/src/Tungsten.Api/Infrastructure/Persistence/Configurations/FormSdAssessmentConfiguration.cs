using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class FormSdAssessmentConfiguration : IEntityTypeConfiguration<FormSdAssessmentEntity>
{
    public void Configure(EntityTypeBuilder<FormSdAssessmentEntity> builder)
    {
        builder.ToTable("form_sd_assessments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ApplicabilityStatus).IsRequired().HasMaxLength(20);
        builder.Property(a => a.RuleSetVersion).IsRequired().HasMaxLength(20);
        builder.Property(a => a.EngineVersion).IsRequired().HasMaxLength(20);
        builder.Property(a => a.Reasoning).HasColumnType("jsonb");
        builder.Property(a => a.AssessedAt).HasDefaultValueSql("now()");
        builder.HasIndex(a => new { a.BatchId, a.TenantId });
        builder.HasIndex(a => a.SupersedesId);
    }
}
