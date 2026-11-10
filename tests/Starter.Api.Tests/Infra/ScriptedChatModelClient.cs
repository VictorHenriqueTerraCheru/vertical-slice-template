using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Starter.Api.Common;

namespace Starter.Api.Tests.Infra;

public sealed class ScriptedChatModelClient : IChatModelClient
{
    private readonly ConcurrentQueue<ChatStreamEvent[]> _rounds = new();
    private readonly ConcurrentQueue<ChatRequest> _requests = new();

    public IReadOnlyList<ChatRequest> Requests => [.. _requests];

    public void Script(params ChatStreamEvent[][] rounds)
    {
        _rounds.Clear();
        _requests.Clear();
        foreach (var round in rounds)
        {
            _rounds.Enqueue(round);
        }
    }

    public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(
        ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _requests.Enqueue(request);
        var round = _rounds.TryDequeue(out var next) ? next : [new ChatFailed("unscripted", ChatUsage.Zero)];
        foreach (var item in round)
        {
            await Task.Yield();
            if (item is ScriptedHang)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                continue;
            }

            yield return item;
        }
    }
}
