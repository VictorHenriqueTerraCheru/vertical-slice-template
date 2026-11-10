using Microsoft.Extensions.Time.Testing;

using Starter.Api.AiUsage;
using Starter.Api.Assistant;
using Starter.Api.Common;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Assistant;

public sealed class AssistantCancellationTests
{
    private sealed class CapturingUsage : IAiUsageService
    {
        public List<AiUsageRecord> Records { get; } = [];

        public Task RecordAsync(AiUsageRecord record, CancellationToken cancellationToken)
        {
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task<Result<PagedResponse<AiUsageByModel>>> ReportAsync(AiUsageReportRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class NoTools : IAssistantTools
    {
        public Task<IReadOnlyList<ChatToolOutput>> RunAsync(Guid userId, IReadOnlyList<ChatToolCall> calls, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [Fact]
    public async Task Client_disconnect_records_the_interrupted_call_as_canceled()
    {
        var chat = new ScriptedChatModelClient();
        chat.Script([new ChatTextDelta("Começando"), new ScriptedHang()]);
        var usage = new CapturingUsage();
        var configuration = TestConfiguration.Create(EnvironmentNames.Testing, new()
        {
            [ConfigurationKeys.AiChatApiKey] = "key",
            [ConfigurationKeys.AiChatModel] = "model",
        });
        var service = new AssistantService(chat, new NoTools(), usage, configuration, new FakeTimeProvider());
        using var disconnect = new CancellationTokenSource();
        var userId = Guid.NewGuid();
        var reply = service.Start(userId, new SendAssistantMessageRequest([new AssistantMessageInput(ChatRole.User, "Oi")]), disconnect.Token);

        var received = new List<AssistantStreamEvent>();
        var act = async () =>
        {
            await foreach (var item in reply.Value!.Events)
            {
                received.Add(item);
                await disconnect.CancelAsync();
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
        received.Should().Equal(new AssistantDelta("Começando"));
        var record = usage.Records.Should().ContainSingle().Subject;
        record.Status.Should().Be(AiCallStatus.Canceled);
        record.UserId.Should().Be(userId);
        record.Usage.Should().Be(ChatUsage.Zero);
    }
}
