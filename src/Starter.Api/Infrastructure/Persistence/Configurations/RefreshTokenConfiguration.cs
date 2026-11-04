using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Starter.Api.Persistence;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.TokenHash).HasMaxLength(64);
        builder.HasIndex(token => token.TokenHash).HasDatabaseName("ix_refresh_tokens_token_hash").IsUnique();
        builder.HasIndex(token => token.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
        builder.HasOne<User>().WithMany().HasForeignKey(token => token.UserId).HasConstraintName("fk_refresh_tokens_users_user_id").OnDelete(DeleteBehavior.Cascade);
    }
}
