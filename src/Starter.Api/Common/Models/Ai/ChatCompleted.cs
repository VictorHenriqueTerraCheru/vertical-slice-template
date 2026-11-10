namespace Starter.Api.Common;

public sealed record ChatCompleted(
    ChatFinishReason FinishReason,
    IReadOnlyList<ChatToolCall> ToolCalls,
    ChatUsage Usage,
    string? ReplayState) : ChatStreamEvent;
