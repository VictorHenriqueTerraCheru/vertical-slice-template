using System.Linq.Expressions;

using Starter.Api.Persistence;

namespace Starter.Api.Todos;

public sealed record TodoEventResponse(
    Guid Id,
    TodoStatus FromStatus,
    TodoStatus ToStatus,
    Guid ActorId,
    DateTimeOffset OccurredAt)
{
    public static Expression<Func<TodoEvent, TodoEventResponse>> Projection { get; } = row =>
        new TodoEventResponse(row.Id, row.FromStatus, row.ToStatus, row.ActorId, row.OccurredAt);
}
