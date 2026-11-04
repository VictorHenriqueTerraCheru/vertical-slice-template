using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Users;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken);

    Task<Result<User>> CreateAsync(User user, string password, CancellationToken cancellationToken);

    Task<PasswordCheckResult> CheckPasswordAsync(User user, string password, CancellationToken cancellationToken);

    Task<string> GeneratePasswordResetTokenAsync(User user, CancellationToken cancellationToken);

    Task<bool> ResetPasswordAsync(User user, string token, string newPassword, CancellationToken cancellationToken);

    Task<bool> ChangePasswordAsync(User user, string currentPassword, string newPassword, CancellationToken cancellationToken);

    Task UpdateAsync(User user, CancellationToken cancellationToken);
}
