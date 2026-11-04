using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Users;

public sealed class UserRepository(
    UserManager<User> users,
    AppDbContext db,
    IOptions<IdentityOptions> identity,
    TimeProvider clock,
    ILogger<UserRepository> logger) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Set<User>().FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken) =>
        users.FindByEmailAsync(email);

    public async Task<Result<User>> CreateAsync(User user, string password, CancellationToken cancellationToken)
    {
        IdentityResult result;
        try
        {
            result = await users.CreateAsync(user, password);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return UserErrors.EmailAlreadyInUse;
        }

        if (result.Succeeded)
        {
            return user;
        }

        if (result.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName)))
        {
            return UserErrors.EmailAlreadyInUse;
        }

        logger.LogError("Identity rejected the user creation: {ErrorCodes}", string.Join(", ", result.Errors.Select(error => error.Code)));
        return UserErrors.IdentityFailure;
    }

    public async Task<PasswordCheckResult> CheckPasswordAsync(User user, string password, CancellationToken cancellationToken)
    {
        if (user.LockoutEnabled && user.LockoutEnd > clock.GetUtcNow())
        {
            return PasswordCheckResult.LockedOut;
        }

        if (!await users.CheckPasswordAsync(user, password))
        {
            await RecordFailureAsync(user.Id, cancellationToken);
            return PasswordCheckResult.Invalid;
        }

        await db.Set<User>()
            .Where(row => row.Id == user.Id && row.AccessFailedCount != 0)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.AccessFailedCount, 0), cancellationToken);
        return PasswordCheckResult.Valid;
    }

    public Task<string> GeneratePasswordResetTokenAsync(User user, CancellationToken cancellationToken) =>
        users.GeneratePasswordResetTokenAsync(user);

    public async Task<bool> ResetPasswordAsync(User user, string token, string newPassword, CancellationToken cancellationToken)
    {
        var result = await users.ResetPasswordAsync(user, token, newPassword);
        return result.Succeeded;
    }

    public async Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword, CancellationToken cancellationToken)
    {
        var result = await users.ChangePasswordAsync(user, currentPassword, newPassword);
        return result.Succeeded;
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            logger.LogError("Identity rejected the user update: {ErrorCodes}", string.Join(", ", result.Errors.Select(error => error.Code)));
            throw new InvalidOperationException("The user could not be updated.");
        }
    }

    private Task RecordFailureAsync(Guid userId, CancellationToken cancellationToken)
    {
        var lockout = identity.Value.Lockout;
        var maxFailures = lockout.MaxFailedAccessAttempts;
        var lockoutEnd = (DateTimeOffset?)clock.GetUtcNow().Add(lockout.DefaultLockoutTimeSpan);

        return db.Set<User>()
            .Where(row => row.Id == userId)
            .ExecuteUpdateAsync(set => set
                .SetProperty(row => row.AccessFailedCount, row => row.AccessFailedCount + 1 >= maxFailures ? 0 : row.AccessFailedCount + 1)
                .SetProperty(row => row.LockoutEnd, row => row.AccessFailedCount + 1 >= maxFailures ? lockoutEnd : row.LockoutEnd),
                cancellationToken);
    }
}
