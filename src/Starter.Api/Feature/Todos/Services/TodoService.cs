using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Todos;

public sealed class TodoService(ITodoRepository repository, IUnitOfWork unitOfWork, TimeProvider clock) : ITodoService
{
    public async Task<Result<TodoItemResponse>> CreateAsync(Guid ownerId, CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var item = new TodoItem
        {
            Id = Guid.CreateVersion7(now),
            OwnerId = ownerId,
            Title = request.Title.Trim(),
            Description = NormalizeDescription(request.Description),
            Status = TodoStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };

        repository.Add(item);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TodoItemResponse.From(item);
    }

    public async Task<Result<TodoItemResponse>> GetAsync(Guid ownerId, Guid id, CancellationToken cancellationToken)
    {
        var response = await repository.FindResponseAsync(ownerId, id, cancellationToken);
        if (response is null)
        {
            return ApplicationError.NotFound;
        }

        return response;
    }

    public async Task<Result<PagedResponse<TodoItemResponse>>> ListAsync(Guid ownerId, ListTodosRequest request, CancellationToken cancellationToken) =>
        await repository.ListAsync(ownerId, request, cancellationToken);

    public Task<Result<TodoItemResponse>> UpdateAsync(Guid ownerId, Guid id, UpdateTodoRequest request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync<TodoItemResponse>(async token =>
        {
            var item = await repository.FindAsync(ownerId, id, token);
            if (item is null)
            {
                return ApplicationError.NotFound;
            }

            item.Title = request.Title.Trim();
            item.Description = NormalizeDescription(request.Description);
            item.UpdatedAt = clock.GetUtcNow();
            return TodoItemResponse.From(item);
        }, cancellationToken);

    public Task<Result<Empty>> DeleteAsync(Guid ownerId, Guid id, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync<Empty>(async token =>
        {
            var item = await repository.FindAsync(ownerId, id, token);
            if (item is null)
            {
                return ApplicationError.NotFound;
            }

            repository.Remove(item);
            return new Empty();
        }, cancellationToken);

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
