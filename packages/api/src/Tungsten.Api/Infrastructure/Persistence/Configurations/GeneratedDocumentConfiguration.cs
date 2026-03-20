using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class GeneratedDocumentConfiguration : IEntityTypeConfiguration<GeneratedDocumentEntity>
{
    public void Configure(EntityTypeBuilder<GeneratedDocumentEntity> builder)
    {
        builder.ToTable("generated_documents");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.DocumentType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(g => g.StorageKey)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(g => g.ShareToken)
            .HasMaxLength(200);

        builder.HasIndex(g => g.ShareToken)
            .IsUnique()
            .HasFilter("\"ShareToken\" IS NOT NULL");

        builder.Property(g => g.GeneratedAt)
            .HasDefaultValueSql("now()");

        builder.HasOne(g => g.Batch)
            .WithMany()
            .HasForeignKey(g => g.BatchId);

        builder.HasOne(g => g.Tenant)
            .WithMany()
            .HasForeignKey(g => g.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(g => g.Generator)
            .WithMany()
            .HasForeignKey(g => g.GeneratedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
