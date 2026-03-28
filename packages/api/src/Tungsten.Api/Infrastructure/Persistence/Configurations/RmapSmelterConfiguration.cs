using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class RmapSmelterConfiguration : IEntityTypeConfiguration<RmapSmelterEntity>
{
    public void Configure(EntityTypeBuilder<RmapSmelterEntity> builder)
    {
        builder.ToTable("rmap_smelters");

        builder.HasKey(r => r.SmelterId);

        builder.Property(r => r.SmelterId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.SmelterName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(r => r.Country)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(r => r.ConformanceStatus)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(r => r.LoadedAt)
            .HasDefaultValueSql("now()");

        builder.Property(r => r.MineralType)
            .HasMaxLength(50);

        builder.Property(r => r.FacilityLocation)
            .HasMaxLength(300);

        builder.Property(r => r.SourcingCountries)
            .HasColumnType("text[]");
    }
}
