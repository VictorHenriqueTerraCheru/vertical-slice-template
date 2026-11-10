namespace Starter.Api.Assistant;

public sealed record AssistantDelta(string Text) : AssistantStreamEvent;
