using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Todos;

public interface ITodoRepository
{
    void Add(TodoItem item);

    void Remove(TodoItem item);

    Task<TodoItem?> FindAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);

    Task<TodoItemResponse?> FindResponseAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);

    Task<PagedResponse<AdminTodoItemResponse>> ListAllAsync(ListTodosRequest request, CancellationToken cancellationToken);

    Task<PagedResponse<TodoItemResponse>> ListAsync(Guid ownerId, ListTodosRequest request, CancellationToken cancellationToken);
}
