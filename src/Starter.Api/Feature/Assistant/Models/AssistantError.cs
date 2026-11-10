using Starter.Api.Common;

namespace Starter.Api.Assistant;

public sealed record AssistantError(string Type, string Title) : AssistantStreamEvent
{
    public static AssistantError From(ApplicationError error) => new(error.Type!, error.Title!);
}
