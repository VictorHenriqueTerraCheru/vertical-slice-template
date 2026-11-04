using Starter.Api.Common;

namespace Starter.Api.Auth;

public interface IAuthService
{
    Task<Result<TokenPair>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result<TokenPair>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Result<TokenPair>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);

    Task<Result<Empty>> LogoutAsync(Guid userId, CancellationToken cancellationToken);

    Task<Result<Empty>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);

    Task<Result<Empty>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);

    Task<Result<Empty>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken);
}
