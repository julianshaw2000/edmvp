using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class JobConfiguration : IEntityTypeConfiguration<JobEntity>
{
    public void Configure(EntityTypeBuilder<JobEntity> builder)
    {
        builder.ToTable("jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.JobType)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(j => j.Status)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(j => j.CreatedAt)
            .HasDefaultValueSql("now()");

        builder.HasOne(j => j.Tenant)
            .WithMany()
            .HasForeignKey(j => j.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(j => j.Status);
    }
}
