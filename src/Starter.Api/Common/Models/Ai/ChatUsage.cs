namespace Starter.Api.Common;

public sealed record ChatUsage(int InputTokens, int CachedInputTokens, int OutputTokens)
{
    public static ChatUsage Zero { get; } = new(0, 0, 0);
}
