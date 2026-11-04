using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Users;

public sealed class UserRepository(
    UserManager<User> users,
    AppDbContext db,
    ILogger<UserRepository> logger) : IUserRepository
{
    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Set<User>().FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<bool> IsLockedOutAsync(User user, CancellationToken cancellationToken) =>
        users.IsLockedOutAsync(user);

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
        if (await users.IsLockedOutAsync(user))
        {
            return PasswordCheckResult.LockedOut;
        }

        if (!await users.CheckPasswordAsync(user, password))
        {
            LogIfFailed(await users.AccessFailedAsync(user), "register the failed access");
            return PasswordCheckResult.Invalid;
        }

        LogIfFailed(await users.ResetAccessFailedCountAsync(user), "reset the failed access count");
        return PasswordCheckResult.Valid;
    }

    public Task<string> GeneratePasswordResetTokenAsync(User user, CancellationToken cancellationToken) =>
        users.GeneratePasswordResetTokenAsync(user);

    public async Task<Result<Empty>> ResetPasswordAsync(User user, string token, string newPassword, CancellationToken cancellationToken) =>
        PasswordOutcome(await users.ResetPasswordAsync(user, token, newPassword));

    public async Task<Result<Empty>> ChangePasswordAsync(User user, string currentPassword, string newPassword, CancellationToken cancellationToken) =>
        PasswordOutcome(await users.ChangePasswordAsync(user, currentPassword, newPassword));

    public async Task<Result<Empty>> UpdateAsync(User user, CancellationToken cancellationToken)
    {
        var result = await users.UpdateAsync(user);
        if (result.Succeeded)
        {
            return new Empty();
        }

        if (result.Errors.Any(error => error.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)))
        {
            return CommonErrors.ConcurrentChange;
        }

        logger.LogError("Identity rejected the user update: {ErrorCodes}", ErrorCodes(result));
        return UserErrors.IdentityFailure;
    }

    private Result<Empty> PasswordOutcome(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return new Empty();
        }

        if (HasError(result, nameof(IdentityErrorDescriber.PasswordMismatch)))
        {
            return UserErrors.WrongCurrentPassword;
        }

        if (HasError(result, nameof(IdentityErrorDescriber.InvalidToken)))
        {
            return UserErrors.InvalidPasswordResetToken;
        }

        if (HasError(result, nameof(IdentityErrorDescriber.ConcurrencyFailure)))
        {
            return CommonErrors.ConcurrentChange;
        }

        logger.LogError("Identity rejected the password change: {ErrorCodes}", ErrorCodes(result));
        return UserErrors.IdentityFailure;
    }

    private static bool HasError(IdentityResult result, string code) => result.Errors.Any(error => error.Code == code);

    private void LogIfFailed(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            logger.LogWarning("Identity could not {Operation}: {ErrorCodes}", operation, ErrorCodes(result));
        }
    }

    private static string ErrorCodes(IdentityResult result) => string.Join(", ", result.Errors.Select(error => error.Code));
}
