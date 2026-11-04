using Starter.Api.Persistence;

namespace Starter.Api.Todos;

public interface ITodoEventRepository
{
    void Add(TodoEvent row);

    Task<TodoEventResponse[]> ListAsync(Guid todoId, CancellationToken cancellationToken);
}
