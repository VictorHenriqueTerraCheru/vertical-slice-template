using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Tests.Infra;

[Collection(nameof(ApiCollection))]
public sealed class UnitOfWorkTests(ApiFactory factory)
{
    private static User NewUser(string email) => new()
    {
        Id = Guid.NewGuid(),
        UserName = email,
        NormalizedUserName = email.ToUpperInvariant(),
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        Name = "Test User",
        Role = UserRole.User,
        CreatedAt = DateTimeOffset.UtcNow,
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

        var result = await unitOfWork.ExecuteAsync<bool>(_ =>
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

        var result = await unitOfWork.ExecuteAsync<bool>(async ct =>
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

        var act = () => unitOfWork.ExecuteAsync<bool>(async ct =>
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

        await unitOfWork.ExecuteAsync<bool>(_ =>
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

        var result = await unitOfWork.ExecuteAsync<bool>(
            _ => throw new DbUpdateConcurrencyException("conflict"), CancellationToken.None);

        result.Error.Should().Be(CommonErrors.ConcurrentChange);
        CommonErrors.ConcurrentChange.Status.Should().Be(409);
    }
}
