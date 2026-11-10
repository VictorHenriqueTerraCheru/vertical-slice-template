namespace Starter.Api.Assistant;

public sealed record SendAssistantMessageRequest(IReadOnlyList<AssistantMessageInput> Messages);
