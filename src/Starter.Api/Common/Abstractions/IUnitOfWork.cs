namespace Starter.Api.Common;

public interface IUnitOfWork
{
    Task<Result<Empty>> SaveChangesAsync(CancellationToken cancellationToken);

    Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken);
}
