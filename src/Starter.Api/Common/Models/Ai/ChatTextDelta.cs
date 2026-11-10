namespace Starter.Api.Common;

public sealed record ChatTextDelta(string Text) : ChatStreamEvent;
