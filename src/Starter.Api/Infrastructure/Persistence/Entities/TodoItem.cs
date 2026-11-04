using Starter.Api.Todos;

namespace Starter.Api.Persistence;

public sealed class TodoItem
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public TodoStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public uint Version { get; set; }
}
