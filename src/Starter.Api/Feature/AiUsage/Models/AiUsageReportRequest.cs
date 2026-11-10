namespace Starter.Api.AiUsage;

public sealed record AiUsageReportRequest(DateTimeOffset? From = null, DateTimeOffset? To = null);
