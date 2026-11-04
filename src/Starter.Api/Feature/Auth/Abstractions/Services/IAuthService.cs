using Starter.Api.Common;

namespace Starter.Api.Auth;

public interface IAuthService
{
    Task<Result<TokenPair>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result<TokenPair>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<Result<TokenPair>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userId, CancellationToken cancellationToken);
}
