namespace Starter.Api.Health;

public sealed record HealthResponse(string Status)
{
    public static HealthResponse Healthy { get; } = new("healthy");
}
