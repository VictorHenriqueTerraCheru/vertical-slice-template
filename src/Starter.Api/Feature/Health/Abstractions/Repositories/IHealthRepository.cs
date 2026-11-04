namespace Starter.Api.Health;

public interface IHealthRepository
{
    Task<bool> CanConnectAsync(CancellationToken ct);
}
