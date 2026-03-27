using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tungsten.Api.Common.Auth;

namespace Tungsten.Api.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.IdentityUserId).IsRequired().HasMaxLength(450);
        builder.Property(r => r.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => r.IdentityUserId);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");
    }
}
