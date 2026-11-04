using Microsoft.AspNetCore.WebUtilities;

using Starter.Api.Common;
using Starter.Api.Persistence;
using Starter.Api.Users;

namespace Starter.Api.Auth;

public sealed class AuthService(
    IUserService users,
    IRefreshTokenRepository refreshTokens,
    ITokenService tokens,
    IUnitOfWork unitOfWork,
    IEmailSender emailSender,
    EnvironmentConfiguration configuration,
    ILogger<AuthService> logger,
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

    public async Task<Result<Empty>> LogoutAsync(Guid userId, CancellationToken cancellationToken)
    {
        await refreshTokens.RevokeAllByUserAsync(userId, clock.GetUtcNow(), cancellationToken);
        return new Empty();
    }

    public async Task<Result<Empty>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var grant = await users.GeneratePasswordResetTokenAsync(request.Email, cancellationToken);
        if (!grant.Success)
        {
            return new Empty();
        }

        try
        {
            await emailSender.SendAsync(
                new EmailMessage(grant.Value!.Email, AuthConstants.PasswordResetSubject, PasswordResetBody(grant.Value!)),
                cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Could not send the password reset email");
        }

        return new Empty();
    }

    public Task<Result<Empty>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync<Empty>(async token =>
        {
            var reset = await users.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, token);
            if (!reset.Success)
            {
                return reset.Error!;
            }

            await refreshTokens.RevokeAllByUserAsync(reset.Value!.Id, clock.GetUtcNow(), token);
            return new Empty();
        }, cancellationToken);

    public Task<Result<Empty>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken) =>
        unitOfWork.ExecuteInTransactionAsync<Empty>(async token =>
        {
            var changed = await users.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, token);
            if (!changed.Success)
            {
                return changed.Error!;
            }

            await refreshTokens.RevokeAllByUserAsync(userId, clock.GetUtcNow(), token);
            return new Empty();
        }, cancellationToken);

    private string PasswordResetBody(PasswordResetGrant grant)
    {
        var link = QueryHelpers.AddQueryString(
            configuration.PasswordResetUrl ?? AuthConstants.DefaultPasswordResetUrl,
            new Dictionary<string, string?> { ["email"] = grant.Email, ["token"] = grant.Token });

        return $"Recebemos um pedido para redefinir a senha da sua conta. Acesse o link abaixo para escolher uma nova senha:\n\n{link}\n\nSe você não fez esse pedido, ignore esta mensagem.";
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
