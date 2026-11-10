namespace Starter.Api.Assistant;

public sealed record AssistantReply(IAsyncEnumerable<AssistantStreamEvent> Events);
