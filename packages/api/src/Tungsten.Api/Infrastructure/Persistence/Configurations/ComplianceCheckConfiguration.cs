using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class ComplianceCheckConfiguration : IEntityTypeConfiguration<ComplianceCheckEntity>
{
    public void Configure(EntityTypeBuilder<ComplianceCheckEntity> builder)
    {
        builder.ToTable("compliance_checks");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Framework)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(c => c.Details)
            .HasColumnType("jsonb");

        builder.Property(c => c.CheckedAt)
            .HasDefaultValueSql("now()");

        builder.Property(c => c.RuleVersion)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("1.0.0-pilot");

        builder.HasOne(c => c.CustodyEvent)
            .WithMany(e => e.ComplianceChecks)
            .HasForeignKey(c => c.CustodyEventId);

        builder.HasOne(c => c.Batch)
            .WithMany()
            .HasForeignKey(c => c.BatchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
