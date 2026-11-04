using Starter.Api.Common;
using Starter.Api.Persistence;
using Starter.Api.Users;

namespace Starter.Api.Auth;

public sealed class AuthService(
    IUserService users,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    TimeProvider clock) : IAuthService
{
    public Task<Result<TokenPair>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync<TokenPair>(async token =>
        {
            var created = await users.CreateAsync(request.Name, request.Email, request.Password, token);
            if (!created.Success)
            {
                return created.Error!;
            }

            return IssuePair(created.Value!);
        }, cancellationToken);

    public async Task<Result<TokenPair>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await users.VerifyCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials;
        }

        var pair = IssuePair(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return pair;
    }

    private TokenPair IssuePair(User user)
    {
        var access = tokens.CreateAccessToken(user);
        var refresh = tokens.CreateRefreshToken();

        refreshTokens.Add(new RefreshToken
        {
            Id = Guid.CreateVersion7(clock.GetUtcNow()),
            UserId = user.Id,
            TokenHash = tokens.Hash(refresh.Value),
            CreatedAt = clock.GetUtcNow(),
            ExpiresAt = refresh.ExpiresAt,
        });

        return new TokenPair(access.Value, access.ExpiresAt, refresh.Value, refresh.ExpiresAt);
    }
}
