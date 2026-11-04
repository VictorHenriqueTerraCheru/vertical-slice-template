using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Starter.Api.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityUserContext<User, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<IdentityUserClaim<Guid>>(entity =>
        {
            entity.ToTable("user_claims");
            entity.HasOne<User>().WithMany().HasForeignKey(row => row.UserId).HasConstraintName("fk_user_claims_users_user_id");
        });
        builder.Entity<IdentityUserLogin<Guid>>(entity =>
        {
            entity.ToTable("user_logins");
            entity.HasOne<User>().WithMany().HasForeignKey(row => row.UserId).HasConstraintName("fk_user_logins_users_user_id");
        });
        builder.Entity<IdentityUserToken<Guid>>(entity =>
        {
            entity.ToTable("user_tokens");
            entity.HasOne<User>().WithMany().HasForeignKey(row => row.UserId).HasConstraintName("fk_user_tokens_users_user_id");
        });
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
