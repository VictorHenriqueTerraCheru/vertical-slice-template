using Starter.Api.Todos;

namespace Starter.Api.Persistence;

public sealed class TodoEvent
{
    public Guid Id { get; set; }

    public Guid TodoItemId { get; set; }

    public TodoStatus FromStatus { get; set; }

    public TodoStatus ToStatus { get; set; }

    public Guid ActorId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}
