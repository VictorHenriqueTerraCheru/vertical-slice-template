using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Starter.Api.Persistence;

public sealed class TodoEventConfiguration : IEntityTypeConfiguration<TodoEvent>
{
    public void Configure(EntityTypeBuilder<TodoEvent> builder)
    {
        builder.ToTable("todo_events");
        builder.HasKey(row => row.Id);
        builder.Property(row => row.FromStatus).HasConversion<string>().HasMaxLength(50);
        builder.Property(row => row.ToStatus).HasConversion<string>().HasMaxLength(50);
        builder.HasIndex(row => new { row.TodoItemId, row.OccurredAt }).HasDatabaseName("ix_todo_events_todo_item_id_occurred_at");
        builder.HasOne<TodoItem>().WithMany().HasForeignKey(row => row.TodoItemId).HasConstraintName("fk_todo_events_todo_items_todo_item_id").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(row => row.ActorId).HasConstraintName("fk_todo_events_users_actor_id").OnDelete(DeleteBehavior.Cascade);
    }
}
