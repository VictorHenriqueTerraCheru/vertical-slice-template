using Starter.Api.Common;

namespace Starter.Api.Assistant;

public interface IAssistantTools
{
    Task<IReadOnlyList<ChatToolOutput>> RunAsync(Guid userId, IReadOnlyList<ChatToolCall> calls, CancellationToken cancellationToken);
}
