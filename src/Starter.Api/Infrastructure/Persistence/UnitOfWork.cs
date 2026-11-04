using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Starter.Api.Common;

namespace Starter.Api.Persistence;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task<Result<T>> ExecuteAsync<T>(Func<CancellationToken, Task<Result<T>>> operation, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        try
        {
            var result = await operation(ct);
            if (!result.Success)
            {
                await DiscardAsync(transaction);
                return result;
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
        catch (DbUpdateConcurrencyException)
        {
            await DiscardAsync(transaction);
            return CommonErrors.ConcurrentChange;
        }
        catch
        {
            await DiscardAsync(transaction);
            throw;
        }
    }

    private async Task DiscardAsync(IDbContextTransaction transaction)
    {
        await transaction.RollbackAsync(CancellationToken.None);
        context.ChangeTracker.Clear();
    }
}
