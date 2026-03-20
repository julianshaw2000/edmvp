using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class CustodyEventConfiguration : IEntityTypeConfiguration<CustodyEventEntity>
{
    public void Configure(EntityTypeBuilder<CustodyEventEntity> builder)
    {
        builder.ToTable("custody_events");

        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.BatchId, e.IdempotencyKey })
            .IsUnique();

        builder.Property(e => e.EventType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(e => e.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.GpsCoordinates)
            .HasMaxLength(100);

        builder.Property(e => e.ActorName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(e => e.SmelterId)
            .HasMaxLength(100);

        builder.Property(e => e.Metadata)
            .HasColumnType("jsonb");

        builder.Property(e => e.SchemaVersion)
            .HasDefaultValue(1);

        builder.Property(e => e.IsCorrection)
            .HasDefaultValue(false);

        builder.Property(e => e.Sha256Hash)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(e => e.PreviousEventHash)
            .HasMaxLength(64);

        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasOne(e => e.Batch)
            .WithMany(b => b.CustodyEvents)
            .HasForeignKey(e => e.BatchId);

        builder.HasOne(e => e.Tenant)
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Creator)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CorrectsEvent)
            .WithMany()
            .HasForeignKey(e => e.CorrectsEventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
