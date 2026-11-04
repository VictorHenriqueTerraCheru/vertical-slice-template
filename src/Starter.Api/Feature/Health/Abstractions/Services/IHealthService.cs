using Starter.Api.Common;

namespace Starter.Api.Health;

public interface IHealthService
{
    Task<Result<HealthResponse>> VerifyDatabaseAsync(CancellationToken ct);
}
