using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Action).HasMaxLength(100).IsRequired();
        builder.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
        builder.Property(e => e.Result).HasMaxLength(20).IsRequired();
        builder.Property(e => e.FailureReason).HasMaxLength(2000);
        builder.Property(e => e.IpAddress).HasMaxLength(45);
        builder.Property(e => e.UserAgent).HasMaxLength(500);
        builder.Property(e => e.Payload).HasColumnType("jsonb");

        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.TenantId, e.Timestamp }).IsDescending(false, true).HasDatabaseName("ix_audit_logs_tenant_timestamp");
        builder.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId }).HasDatabaseName("ix_audit_logs_tenant_entity");
        builder.HasIndex(e => new { e.TenantId, e.BatchId, e.Timestamp }).HasDatabaseName("ix_audit_logs_tenant_batch");
    }
}
