using Microsoft.EntityFrameworkCore;

using Starter.Api.Persistence;

namespace Starter.Api.Auth;

public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public void Add(RefreshToken token) => db.Set<RefreshToken>().Add(token);

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        db.Set<RefreshToken>().AsNoTracking().FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task<bool> RevokeIfActiveAsync(Guid id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var affected = await db.Set<RefreshToken>()
            .Where(token => token.Id == id && token.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(token => token.RevokedAt, now), cancellationToken);
        return affected == 1;
    }

    public Task RevokeAllByUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken) =>
        db.Set<RefreshToken>()
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(set => set.SetProperty(token => token.RevokedAt, now), cancellationToken);
}
