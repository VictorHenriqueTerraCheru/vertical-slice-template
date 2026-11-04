using System.Linq.Expressions;

using Starter.Api.Persistence;

namespace Starter.Api.Todos;

public sealed record TodoItemResponse(
    Guid Id,
    string Title,
    string? Description,
    TodoStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static Expression<Func<TodoItem, TodoItemResponse>> Projection { get; } = item =>
        new TodoItemResponse(item.Id, item.Title, item.Description, item.Status, item.CreatedAt, item.UpdatedAt);

    public static TodoItemResponse From(TodoItem item) =>
        new(item.Id, item.Title, item.Description, item.Status, item.CreatedAt, item.UpdatedAt);
}
