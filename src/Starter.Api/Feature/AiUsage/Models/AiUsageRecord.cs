using Starter.Api.Common;

namespace Starter.Api.AiUsage;

public sealed record AiUsageRecord(Guid? UserId, string Feature, ChatUsage Usage, AiCallStatus Status, TimeSpan Latency);
