namespace Starter.Api.Common;

public sealed record ChatToolCall(string CallId, string Name, string ArgumentsJson);
