using Starter.Api.Common;

namespace Starter.Api.Health;

public sealed class HealthService(IHealthRepository repository, ILogger<HealthService> logger) : IHealthService
{
    public async Task<Result<HealthResponse>> VerifyDatabaseAsync(CancellationToken ct)
    {
        if (await repository.CanConnectAsync(ct))
        {
            return HealthResponse.Healthy;
        }

        logger.LogWarning("Database health check failed: no connection.");
        return HealthErrors.DatabaseUnavailable;
    }
}
