using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Starter.Api.Todos;

namespace Starter.Api.Persistence;

public sealed class TodoItemConfiguration : IEntityTypeConfiguration<TodoItem>
{
    public void Configure(EntityTypeBuilder<TodoItem> builder)
    {
        builder.ToTable("todo_items");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Title).HasMaxLength(TodosConstants.TitleMaxLength);
        builder.Property(item => item.Description).HasMaxLength(TodosConstants.DescriptionMaxLength);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(item => item.Version).IsRowVersion();
        builder.HasIndex(item => new { item.OwnerId, item.CreatedAt }).HasDatabaseName("ix_todo_items_owner_id_created_at");
        builder.HasOne<User>().WithMany().HasForeignKey(item => item.OwnerId).HasConstraintName("fk_todo_items_users_owner_id").OnDelete(DeleteBehavior.Cascade);
    }
}
