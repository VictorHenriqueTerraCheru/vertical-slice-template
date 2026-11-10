using Starter.Api.AiUsage;

namespace Starter.Api.Persistence;

public sealed class AiUsageEntry
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string Feature { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public AiCallStatus Status { get; set; }

    public int InputTokens { get; set; }

    public int CachedInputTokens { get; set; }

    public int OutputTokens { get; set; }

    public int LatencyMilliseconds { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
}
