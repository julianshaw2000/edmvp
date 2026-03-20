using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Infrastructure.Persistence.Entities;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class RiskCountryConfiguration : IEntityTypeConfiguration<RiskCountryEntity>
{
    public void Configure(EntityTypeBuilder<RiskCountryEntity> builder)
    {
        builder.ToTable("risk_countries");

        builder.HasKey(r => r.CountryCode);

        builder.Property(r => r.CountryCode)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(r => r.CountryName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.RiskLevel)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(r => r.Source)
            .IsRequired()
            .HasMaxLength(200);
    }
}
