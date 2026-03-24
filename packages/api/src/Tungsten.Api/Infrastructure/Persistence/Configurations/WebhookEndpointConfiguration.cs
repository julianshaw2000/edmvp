using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class WebhookEndpointConfiguration : IEntityTypeConfiguration<WebhookEndpointEntity>
{
    public void Configure(EntityTypeBuilder<WebhookEndpointEntity> builder)
    {
        builder.ToTable("webhook_endpoints");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Url).HasMaxLength(500).IsRequired();
        builder.Property(e => e.Secret).HasMaxLength(100).IsRequired();
        builder.Property(e => e.Events).HasMaxLength(500).IsRequired();

        builder.HasOne(e => e.Tenant).WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(e => e.TenantId).HasDatabaseName("ix_webhook_endpoints_tenant");
    }
}
