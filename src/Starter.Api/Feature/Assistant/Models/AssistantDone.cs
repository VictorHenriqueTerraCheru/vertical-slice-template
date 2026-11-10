namespace Starter.Api.Assistant;

public sealed record AssistantDone(bool Truncated) : AssistantStreamEvent;
