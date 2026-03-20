using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<DocumentEntity>
{
    public void Configure(EntityTypeBuilder<DocumentEntity> builder)
    {
        builder.ToTable("documents");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.StorageKey)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(d => d.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Sha256Hash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(d => d.DocumentType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(d => d.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasOne(d => d.Tenant)
            .WithMany()
            .HasForeignKey(d => d.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.CustodyEvent)
            .WithMany(e => e.Documents)
            .HasForeignKey(d => d.CustodyEventId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.Batch)
            .WithMany(b => b.Documents)
            .HasForeignKey(d => d.BatchId);

        builder.HasOne(d => d.Uploader)
            .WithMany()
            .HasForeignKey(d => d.UploadedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
