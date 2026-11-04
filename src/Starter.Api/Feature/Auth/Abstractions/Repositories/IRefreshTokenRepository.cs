using Starter.Api.Persistence;

namespace Starter.Api.Auth;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);

    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<bool> RevokeIfActiveAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken);

    Task RevokeAllByUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken);
}
