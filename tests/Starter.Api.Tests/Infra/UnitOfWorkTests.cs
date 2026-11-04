using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Tests.Infra;

[Collection(nameof(ApiCollection))]
public sealed class UnitOfWorkTests(ApiFactory factory)
{
    private User NewUser(string email) => new()
    {
        Id = Guid.NewGuid(),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        Name = "Test User",
        Role = UserRole.User,
        CreatedAt = factory.Clock.GetUtcNow(),
    };

    private Task<bool> ExistsAsync(Guid id) =>
        factory.InDatabaseAsync(db => db.Set<User>().AsNoTracking().AnyAsync(user => user.Id == id));

    [Fact]
    public async Task Success_saves_and_commits()
    {
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var result = await unitOfWork.ExecuteInTransactionAsync<bool>(_ =>
        {
            db.Set<User>().Add(user);
            return Task.FromResult<Result<bool>>(true);
        }, CancellationToken.None);

        result.Success.Should().BeTrue();
        (await ExistsAsync(user.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task Failed_result_rolls_everything_back()
    {
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var result = await unitOfWork.ExecuteInTransactionAsync<bool>(async ct =>
        {
            db.Set<User>().Add(user);
            await db.SaveChangesAsync(ct);
            return ApplicationError.NotFound;
        }, CancellationToken.None);

        result.Error.Should().Be(ApplicationError.NotFound);
        (await ExistsAsync(user.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Exception_rolls_back_and_propagates()
    {
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var act = () => unitOfWork.ExecuteInTransactionAsync<bool>(async ct =>
        {
            db.Set<User>().Add(user);
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("boom");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        (await ExistsAsync(user.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task After_failing_the_same_scope_does_not_save_leftovers()
    {
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await unitOfWork.ExecuteInTransactionAsync<bool>(_ =>
        {
            db.Set<User>().Add(user);
            return Task.FromResult<Result<bool>>(ApplicationError.NotFound);
        }, CancellationToken.None);
        await db.SaveChangesAsync();

        (await ExistsAsync(user.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Concurrency_conflict_becomes_409_error()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var result = await unitOfWork.ExecuteInTransactionAsync<bool>(
            _ => throw new DbUpdateConcurrencyException("conflict"), CancellationToken.None);

        result.Error.Should().Be(CommonErrors.ConcurrentChange);
        CommonErrors.ConcurrentChange.Status.Should().Be(409);
    }

    [Fact]
    public async Task Rollback_failure_does_not_mask_original_exception_and_clears_tracker()
    {
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var act = () => unitOfWork.ExecuteInTransactionAsync<bool>(_ =>
        {
            db.Set<User>().Add(user);
            db.Database.GetDbConnection().Close();
            throw new InvalidOperationException("boom");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        db.ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_persists_pending_changes()
    {
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        db.Set<User>().Add(user);
        var result = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
        (await ExistsAsync(user.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_turns_a_concurrency_conflict_into_a_409_error_and_clears_the_tracker()
    {
        var user = NewUser($"{Guid.NewGuid()}@example.com");
        await factory.InDatabaseAsync(async db =>
        {
            db.Set<User>().Add(user);
            await db.SaveChangesAsync();
        });
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var tracked = await db.Set<User>().FirstAsync(row => row.Id == user.Id);
        await factory.InDatabaseAsync(other => other.Set<User>()
            .Where(row => row.Id == user.Id)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.ConcurrencyStamp, Guid.NewGuid().ToString())));

        tracked.Name = "Changed";
        var result = await unitOfWork.SaveChangesAsync(CancellationToken.None);

        result.Error.Should().Be(CommonErrors.ConcurrentChange);
        db.ChangeTracker.Entries().Should().BeEmpty();
    }
}
