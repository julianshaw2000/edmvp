using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class BatchConfiguration : IEntityTypeConfiguration<BatchEntity>
{
    public void Configure(EntityTypeBuilder<BatchEntity> builder)
    {
        builder.ToTable("batches");

        builder.HasKey(b => b.Id);

        builder.HasIndex(b => new { b.TenantId, b.BatchNumber })
            .IsUnique();

        builder.Property(b => b.BatchNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(b => b.MineralType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(b => b.OriginCountry)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(b => b.OriginMine)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.WeightKg)
            .HasPrecision(18, 4);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.ComplianceStatus)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(b => b.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.Property(b => b.UpdatedAt)
            .HasDefaultValueSql("now()");

        builder.HasOne(b => b.Tenant)
            .WithMany(t => t.Batches)
            .HasForeignKey(b => b.TenantId);

        builder.HasOne(b => b.Creator)
            .WithMany()
            .HasForeignKey(b => b.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
