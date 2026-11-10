namespace Starter.Api.Common;

public interface IChatModelClient
{
    IAsyncEnumerable<ChatStreamEvent> StreamAsync(ChatRequest request, CancellationToken cancellationToken);
}
