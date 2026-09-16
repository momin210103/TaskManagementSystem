using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RefreshTokens");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired();

        builder.Property(r => r.TokenHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(r => r.ExpiresAt)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UpdatedAt)
            .IsRequired();

        builder.Property(r => r.RevokedAt)
            .IsRequired(false);

        builder.Property(r => r.ReplacedByTokenId)
            .IsRequired(false);

        builder.Ignore(r => r.IsRevoked);
        builder.Ignore(r => r.IsExpired);
        builder.Ignore(r => r.IsActive);

        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.TokenHash);
    }
}

