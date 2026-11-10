using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Starter.Api.AiUsage;

namespace Starter.Api.Persistence;

public sealed class AiUsageEntryConfiguration : IEntityTypeConfiguration<AiUsageEntry>
{
    public void Configure(EntityTypeBuilder<AiUsageEntry> builder)
    {
        builder.ToTable("ai_usage_entries");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.Feature).HasMaxLength(AiUsageConstants.FeatureMaxLength);
        builder.Property(row => row.Provider).HasMaxLength(AiUsageConstants.ProviderMaxLength);
        builder.Property(row => row.Model).HasMaxLength(AiUsageConstants.ModelMaxLength);
        builder.Property(row => row.Status).HasConversion<string>().HasMaxLength(AiUsageConstants.StatusMaxLength);
        builder.HasIndex(row => row.OccurredAt).HasDatabaseName("ix_ai_usage_entries_occurred_at");
        builder.HasIndex(row => new { row.UserId, row.OccurredAt }).HasDatabaseName("ix_ai_usage_entries_user_id_occurred_at");
        builder.HasOne<User>().WithMany().HasForeignKey(row => row.UserId).HasConstraintName("fk_ai_usage_entries_users_user_id").OnDelete(DeleteBehavior.SetNull);
    }
}
