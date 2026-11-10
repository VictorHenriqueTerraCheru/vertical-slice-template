using Starter.Api.Common;

namespace Starter.Api.Assistant;

public interface IAssistantService
{
    Result<AssistantReply> Start(Guid userId, SendAssistantMessageRequest request, CancellationToken cancellationToken);
}
