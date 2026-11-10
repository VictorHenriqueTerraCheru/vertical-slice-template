namespace Starter.Api.Common;

public sealed record ChatFailed(string Reason, ChatUsage Usage) : ChatStreamEvent;
