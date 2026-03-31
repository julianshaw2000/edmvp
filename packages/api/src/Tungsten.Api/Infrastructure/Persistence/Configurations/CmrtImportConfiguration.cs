using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class CmrtImportConfiguration : IEntityTypeConfiguration<CmrtImportEntity>
{
    public void Configure(EntityTypeBuilder<CmrtImportEntity> builder)
    {
        builder.ToTable("cmrt_imports");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(500);
        builder.Property(e => e.DeclarationCompany).IsRequired().HasMaxLength(300);
        builder.Property(e => e.ImportedAt).HasDefaultValueSql("now()");
        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Importer).WithMany().HasForeignKey(e => e.ImportedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
