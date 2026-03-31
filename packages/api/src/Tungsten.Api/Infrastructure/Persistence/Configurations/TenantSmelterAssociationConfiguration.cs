using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class TenantSmelterAssociationConfiguration : IEntityTypeConfiguration<TenantSmelterAssociationEntity>
{
    public void Configure(EntityTypeBuilder<TenantSmelterAssociationEntity> builder)
    {
        builder.ToTable("tenant_smelter_associations");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.SmelterId).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Source).IsRequired().HasMaxLength(30);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20);
        builder.Property(e => e.MetalType).IsRequired().HasMaxLength(50);
        builder.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(e => new { e.TenantId, e.SmelterId }).IsUnique();
        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Smelter).WithMany().HasForeignKey(e => e.SmelterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.CmrtImport).WithMany().HasForeignKey(e => e.CmrtImportId).OnDelete(DeleteBehavior.SetNull);
    }
}
