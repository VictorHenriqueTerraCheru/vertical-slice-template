namespace Starter.Api.AiUsage;

public sealed record AiUsageByModel
{
    public required string Provider { get; init; }

    public required string Model { get; init; }

    public int Calls { get; init; }

    public int FailedCalls { get; init; }

    public long InputTokens { get; init; }

    public long CachedInputTokens { get; init; }

    public long OutputTokens { get; init; }
}
