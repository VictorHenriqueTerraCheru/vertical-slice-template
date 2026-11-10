using Starter.Api.Common;

namespace Starter.Api.Assistant;

public sealed record AssistantMessageInput(ChatRole Role, string Content);
