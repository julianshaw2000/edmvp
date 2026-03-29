using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class FormSdPackageConfiguration : IEntityTypeConfiguration<FormSdPackageEntity>
{
    public void Configure(EntityTypeBuilder<FormSdPackageEntity> builder)
    {
        builder.ToTable("form_sd_packages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(p => p.Sha256Hash).IsRequired().HasMaxLength(64);
        builder.Property(p => p.RuleSetVersion).IsRequired().HasMaxLength(20);
        builder.Property(p => p.PlatformVersion).IsRequired().HasMaxLength(20);
        builder.Property(p => p.SourceJson).HasColumnType("jsonb");
        builder.Property(p => p.GeneratedAt).HasDefaultValueSql("now()");
        builder.HasIndex(p => new { p.TenantId, p.ReportingYear });
    }
}
