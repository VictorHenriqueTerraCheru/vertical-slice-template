namespace Starter.Api.AiUsage;

public sealed record AiUsageReport(DateTimeOffset From, DateTimeOffset To, IReadOnlyList<AiUsageByModel> Models);
