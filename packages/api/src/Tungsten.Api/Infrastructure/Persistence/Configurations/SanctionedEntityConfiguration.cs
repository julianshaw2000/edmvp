using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class SanctionedEntityConfiguration : IEntityTypeConfiguration<SanctionedEntityEntity>
{
    public void Configure(EntityTypeBuilder<SanctionedEntityEntity> builder)
    {
        builder.ToTable("sanctioned_entities");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.EntityName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.EntityType)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.Source)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.LoadedAt)
            .HasDefaultValueSql("now()");

        builder.HasIndex(s => s.EntityName);
    }
}
