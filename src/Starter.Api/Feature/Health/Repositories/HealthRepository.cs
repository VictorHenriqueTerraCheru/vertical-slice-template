using Starter.Api.Persistence;

namespace Starter.Api.Health;

public sealed class HealthRepository(AppDbContext db) : IHealthRepository
{
    public Task<bool> CanConnectAsync(CancellationToken ct) => db.Database.CanConnectAsync(ct);
}
