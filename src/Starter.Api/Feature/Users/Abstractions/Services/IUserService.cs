using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Users;

public interface IUserService
{
    Task<Result<UserResponse>> GetMeAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<UserResponse>> UpdateMeAsync(Guid userId, UpdateMeRequest request, CancellationToken cancellationToken);

    Task<Result<User>> CreateAsync(string name, string email, string password, CancellationToken cancellationToken);

    Task<Result<User>> FindUnlockedAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<User>> VerifyCredentialsAsync(string email, string password, CancellationToken cancellationToken);

    Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken);

    Task<Result<User>> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken);

    Task<Result<Empty>> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken cancellationToken);
}
