using Microsoft.EntityFrameworkCore;

using Starter.Api.Persistence;

namespace Starter.Api.Todos;

public sealed class TodoEventRepository(AppDbContext db) : ITodoEventRepository
{
    public void Add(TodoEvent row) => db.Set<TodoEvent>().Add(row);

    public async Task<TodoEventResponse[]> ListAsync(Guid todoId, CancellationToken cancellationToken) =>
        await db.Set<TodoEvent>()
            .AsNoTracking()
            .Where(row => row.TodoItemId == todoId)
            .OrderBy(row => row.OccurredAt)
            .ThenBy(row => row.Id)
            .Select(TodoEventResponse.Projection)
            .ToArrayAsync(cancellationToken);
}
