namespace Starter.Api.AiUsage;

public sealed record AiUsageByModel(
    string Provider,
    string Model,
    int Calls,
    int FailedCalls,
    long InputTokens,
    long CachedInputTokens,
    long OutputTokens);
