using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Starter.Api.Persistence;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.Property(user => user.Name).HasMaxLength(200);
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(user => user.NormalizedUserName).HasDatabaseName("ix_users_normalized_user_name").IsUnique();
        builder.HasIndex(user => user.NormalizedEmail).HasDatabaseName("ix_users_normalized_email").IsUnique();
    }
}
