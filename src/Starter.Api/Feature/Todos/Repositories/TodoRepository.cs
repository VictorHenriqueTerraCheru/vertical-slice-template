using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Todos;

public sealed class TodoRepository(AppDbContext db) : ITodoRepository
{
    private static readonly IReadOnlyDictionary<string, Expression<Func<TodoItem, object>>> Sortable =
        new Dictionary<string, Expression<Func<TodoItem, object>>>
        {
            [TodosConstants.SortByCreatedAt] = item => item.CreatedAt,
            [TodosConstants.SortByTitle] = item => item.Title,
        };

    public void Add(TodoItem item) => db.Set<TodoItem>().Add(item);

    public void Remove(TodoItem item) => db.Set<TodoItem>().Remove(item);

    public Task<TodoItem?> FindAsync(Guid ownerId, Guid id, CancellationToken cancellationToken) =>
        db.Set<TodoItem>().FirstOrDefaultAsync(item => item.OwnerId == ownerId && item.Id == id, cancellationToken);

    public Task<TodoItemResponse?> FindResponseAsync(Guid ownerId, Guid id, CancellationToken cancellationToken) =>
        db.Set<TodoItem>()
            .AsNoTracking()
            .Where(item => item.OwnerId == ownerId && item.Id == id)
            .Select(TodoItemResponse.Projection)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExistsAsync(Guid ownerId, Guid id, CancellationToken cancellationToken) =>
        db.Set<TodoItem>().AnyAsync(item => item.OwnerId == ownerId && item.Id == id, cancellationToken);

    public Task<PagedResponse<TodoItemResponse>> ListAsync(Guid ownerId, ListTodosRequest request, CancellationToken cancellationToken) =>
        Filter(db.Set<TodoItem>().AsNoTracking().Where(item => item.OwnerId == ownerId), request)
            .ApplySort(request, Sortable, TodosConstants.SortByCreatedAt, SortDirection.Desc, item => item.Id)
            .ToPagedAsync(request, TodoItemResponse.Projection, cancellationToken);

    public Task<PagedResponse<AdminTodoItemResponse>> ListAllAsync(ListTodosRequest request, CancellationToken cancellationToken) =>
        Filter(db.Set<TodoItem>().AsNoTracking(), request)
            .ApplySort(request, Sortable, TodosConstants.SortByCreatedAt, SortDirection.Desc, item => item.Id)
            .ToPagedAsync(request, AdminTodoItemResponse.Projection, cancellationToken);

    private static IQueryable<TodoItem> Filter(IQueryable<TodoItem> query, ListTodosRequest request) =>
        request.Status?.Value is { } status ? query.Where(item => item.Status == status) : query;
}
