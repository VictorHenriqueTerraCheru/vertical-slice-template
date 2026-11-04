using Starter.Api.Common;

namespace Starter.Api.Todos;

public interface ITodoService
{
    Task<Result<TodoItemResponse>> CreateAsync(Guid ownerId, CreateTodoRequest request, CancellationToken cancellationToken);

    Task<Result<TodoItemResponse>> GetAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResponse<TodoItemResponse>>> ListAsync(Guid ownerId, ListTodosRequest request, CancellationToken cancellationToken);

    Task<Result<TodoItemResponse>> UpdateAsync(Guid ownerId, Guid id, UpdateTodoRequest request, CancellationToken cancellationToken);

    Task<Result<TodoItemResponse>> TransitionAsync(Guid actorId, Guid id, TransitionTodoRequest request, CancellationToken cancellationToken);

    Task<Result<TodoEventResponse[]>> ListEventsAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);

    Task<Result<PagedResponse<AdminTodoItemResponse>>> ListAllAsync(ListTodosRequest request, CancellationToken cancellationToken);

    Task<Result<Empty>> DeleteAsync(Guid ownerId, Guid id, CancellationToken cancellationToken);
}
