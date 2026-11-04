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
        var verified = await users.VerifyCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (!verified.Success)
        {
            return AuthErrors.InvalidCredentials;
        }

        var pair = IssuePair(verified.Value!);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return pair;
    }

    public Task<Result<TokenPair>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync<TokenPair>(async token =>
        {
            var now = clock.GetUtcNow();
            var stored = await refreshTokens.FindByHashAsync(tokens.Hash(request.RefreshToken), token);
            if (stored is null || stored.ExpiresAt <= now)
            {
                return AuthErrors.InvalidRefreshToken;
            }

            if (!await refreshTokens.RevokeIfActiveAsync(stored.Id, now, token))
            {
                return AuthErrors.InvalidRefreshToken;
            }

            var user = await users.FindUnlockedAsync(stored.UserId, token);
            if (!user.Success)
            {
                return AuthErrors.InvalidRefreshToken;
            }

            return IssuePair(user.Value!);
        }, cancellationToken);

    public Task LogoutAsync(Guid userId, CancellationToken cancellationToken) =>
        refreshTokens.RevokeAllByUserAsync(userId, clock.GetUtcNow(), cancellationToken);

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
