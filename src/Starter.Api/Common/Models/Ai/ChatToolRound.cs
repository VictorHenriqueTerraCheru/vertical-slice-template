namespace Starter.Api.Common;

public sealed record ChatToolRound(string? ReplayState, IReadOnlyList<ChatToolCall> Calls, IReadOnlyList<ChatToolOutput> Outputs);
