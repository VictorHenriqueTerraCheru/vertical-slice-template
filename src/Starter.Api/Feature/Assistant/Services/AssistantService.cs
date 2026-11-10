using System.Runtime.CompilerServices;

using Starter.Api.AiUsage;
using Starter.Api.Common;

namespace Starter.Api.Assistant;

public sealed class AssistantService(
    IChatModelClient chat,
    IAssistantTools tools,
    IAiUsageService usage,
    EnvironmentConfiguration configuration,
    TimeProvider clock) : IAssistantService
{
    public Result<AssistantReply> Start(Guid userId, SendAssistantMessageRequest request, CancellationToken cancellationToken)
    {
        if (!configuration.AiChatConfigured)
        {
            return AiErrors.NotConfigured;
        }

        var messages = request.Messages.Select(message => new ChatMessage(message.Role, message.Content.Trim())).ToList();
        return new AssistantReply(RunAsync(userId, messages, cancellationToken));
    }

    private async IAsyncEnumerable<AssistantStreamEvent> RunAsync(
        Guid userId, IReadOnlyList<ChatMessage> messages, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var rounds = new List<ChatToolRound>();
        for (var round = 0; ; round++)
        {
            var offerTools = round < AssistantConstants.MaxToolRounds;
            var request = new ChatRequest(
                AssistantConstants.Instructions,
                messages,
                offerTools ? AssistantConstants.Tools : [],
                rounds.ToArray(),
                AssistantConstants.MaxOutputTokens);

            var started = clock.GetTimestamp();
            var answered = false;
            ChatStreamEvent? outcome = null;
            var streamed = false;
            try
            {
                await foreach (var item in chat.StreamAsync(request, cancellationToken))
                {
                    if (item is ChatTextDelta delta)
                    {
                        answered = true;
                        yield return new AssistantDelta(delta.Text);
                    }
                    else
                    {
                        outcome = item;
                    }
                }

                streamed = true;
            }
            finally
            {
                if (!streamed)
                {
                    await RecordAsync(userId, ChatUsage.Zero, AiCallStatus.Canceled, clock.GetElapsedTime(started));
                }
            }

            var latency = clock.GetElapsedTime(started);
            if (outcome is ChatCompleted { FinishReason: ChatFinishReason.ToolCalls, ToolCalls.Count: > 0 } calling && offerTools)
            {
                await RecordAsync(userId, calling.Usage, AiCallStatus.Completed, latency);
                var outputs = await tools.RunAsync(userId, calling.ToolCalls, cancellationToken);
                rounds.Add(new ChatToolRound(calling.ReplayState, calling.ToolCalls, outputs));
                continue;
            }

            if (outcome is ChatCompleted completed && answered)
            {
                var truncated = completed.FinishReason == ChatFinishReason.Truncated;
                await RecordAsync(userId, completed.Usage, truncated ? AiCallStatus.Truncated : AiCallStatus.Completed, latency);
                yield return new AssistantDone(truncated);
                yield break;
            }

            await RecordAsync(userId, UsageOf(outcome), AiCallStatus.Failed, latency);
            yield return AssistantError.From(AiErrors.Unavailable);
            yield break;
        }
    }

    private Task RecordAsync(Guid userId, ChatUsage chatUsage, AiCallStatus status, TimeSpan latency) =>
        usage.RecordAsync(
            new AiUsageRecord(userId, AssistantConstants.UsageFeature, chatUsage, status, latency),
            CancellationToken.None);

    private static ChatUsage UsageOf(ChatStreamEvent? outcome) => outcome switch
    {
        ChatCompleted completed => completed.Usage,
        ChatFailed failed => failed.Usage,
        _ => ChatUsage.Zero,
    };
}
