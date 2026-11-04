using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Starter.Api.Common;

namespace Starter.Api.Persistence;

public sealed class UnitOfWork(AppDbContext context, ILogger<UnitOfWork> logger) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    public async Task<Result<T>> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var committed = false;
        try
        {
            var result = await operation(cancellationToken);
            if (result.Success)
            {
                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                committed = true;
            }

            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CommonErrors.ConcurrentChange;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return CommonErrors.ConcurrentChange;
        }
        finally
        {
            if (!committed)
            {
                await DiscardAsync(transaction);
            }
        }
    }

    private async Task DiscardAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Transaction rollback failed.");
        }
        finally
        {
            context.ChangeTracker.Clear();
        }
    }
}
