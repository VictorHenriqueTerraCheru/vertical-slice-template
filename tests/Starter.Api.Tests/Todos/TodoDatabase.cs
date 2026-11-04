using Microsoft.EntityFrameworkCore;

using Starter.Api.Persistence;
using Starter.Api.Tests.Infra;
using Starter.Api.Todos;

namespace Starter.Api.Tests.Todos;

public static class TodoDatabase
{
    public static Task SetStatusAsync(ApiFactory factory, Guid id, TodoStatus status) =>
        factory.InDatabaseAsync(db => db.Set<TodoItem>()
            .Where(item => item.Id == id)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.Status, status)));

    public static Task<TodoItem?> FindAsync(ApiFactory factory, Guid id) =>
        factory.InDatabaseAsync(db => db.Set<TodoItem>().AsNoTracking().FirstOrDefaultAsync(item => item.Id == id));
}
