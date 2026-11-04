using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.Users;

public sealed class UserService(IUserRepository repository, TimeProvider clock) : IUserService
{
    public async Task<Result<UserResponse>> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await repository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ApplicationError.NotFound;
        }

        return UserResponse.From(user);
    }

    public async Task<Result<UserResponse>> UpdateMeAsync(Guid userId, UpdateMeRequest request, CancellationToken cancellationToken)
    {
        var user = await repository.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ApplicationError.NotFound;
        }

        user.Name = request.Name.Trim();
        await repository.UpdateAsync(user, cancellationToken);
        return UserResponse.From(user);
    }

    public Task<Result<User>> CreateAsync(string name, string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim();
        var user = new User
        {
            Id = Guid.CreateVersion7(clock.GetUtcNow()),
            Name = name.Trim(),
            Email = normalizedEmail,
            UserName = normalizedEmail,
            Role = UserRole.User,
            CreatedAt = clock.GetUtcNow(),
        };

        return repository.CreateAsync(user, password, cancellationToken);
    }

    public async Task<User?> VerifyCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await repository.FindByEmailAsync(email.Trim(), cancellationToken);
        if (user is null)
        {
            return null;
        }

        var check = await repository.CheckPasswordAsync(user, password, cancellationToken);
        return check == PasswordCheckResult.Valid ? user : null;
    }
}
