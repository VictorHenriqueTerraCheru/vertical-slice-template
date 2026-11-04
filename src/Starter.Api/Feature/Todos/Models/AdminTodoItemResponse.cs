using System.Linq.Expressions;

using Starter.Api.Persistence;

namespace Starter.Api.Todos;

public sealed record AdminTodoItemResponse(
    Guid Id,
    Guid OwnerId,
    string Title,
    string? Description,
    TodoStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static Expression<Func<TodoItem, AdminTodoItemResponse>> Projection { get; } = item =>
        new AdminTodoItemResponse(item.Id, item.OwnerId, item.Title, item.Description, item.Status, item.CreatedAt, item.UpdatedAt);
}
