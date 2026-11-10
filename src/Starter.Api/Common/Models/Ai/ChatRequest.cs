namespace Starter.Api.Common;

public sealed record ChatRequest(
    string Instructions,
    IReadOnlyList<ChatMessage> Messages,
    IReadOnlyList<ChatToolDefinition> Tools,
    IReadOnlyList<ChatToolRound> ToolRounds,
    int MaxOutputTokens);
