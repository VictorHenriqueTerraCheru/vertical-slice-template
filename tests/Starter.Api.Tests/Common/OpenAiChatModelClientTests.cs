using System.Net;
using System.Text;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Logging.Abstractions;

using Starter.Api.Common;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Common;

public sealed class OpenAiChatModelClientTests
{
    private const string Completed = """{"type":"response.completed","response":{"output":[{"type":"message","content":[]}],"usage":{"input_tokens":120,"input_tokens_details":{"cached_tokens":100},"output_tokens":7}}}""";

    private const string ToolCalls = """{"type":"response.completed","response":{"output":[{"type":"reasoning","id":"rs_1","encrypted_content":"opaque"},{"type":"function_call","id":"fc_1","call_id":"call_1","name":"search_items","arguments":"{\"status\":null}"}],"usage":{"input_tokens":50,"output_tokens":10}}}""";

    private static readonly ChatRequest Request = new(
        "be brief",
        [new ChatMessage(ChatRole.User, "hi"), new ChatMessage(ChatRole.Assistant, "hello"), new ChatMessage(ChatRole.User, "list")],
        [new ChatToolDefinition("search_items", "lists", """{"type":"object","properties":{},"required":[],"additionalProperties":false}""")],
        [],
        256);

    private sealed class StubHandler(Func<HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<JsonNode> Bodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!);
            return respond();
        }
    }

    private static HttpResponseMessage Sse(params string[] events) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(string.Concat(events.Select(data => $"event: x\ndata: {data}\n\n")), Encoding.UTF8, "text/event-stream"),
    };

    private static string Delta(string text) => new JsonObject { ["type"] = "response.output_text.delta", ["delta"] = text }.ToJsonString();

    private static OpenAiChatModelClient Client(StubHandler handler, bool includeReasoning = true, string? effort = null) => new(
        new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/v1/") },
        TestConfiguration.Create(EnvironmentNames.Testing, new()
        {
            [ConfigurationKeys.AiChatApiKey] = "key",
            [ConfigurationKeys.AiChatModel] = "model-x",
            [ConfigurationKeys.AiChatIncludeReasoning] = includeReasoning ? "true" : "false",
            [ConfigurationKeys.AiChatReasoningEffort] = effort,
        }),
        NullLogger<OpenAiChatModelClient>.Instance);

    private static async Task<List<ChatStreamEvent>> CollectAsync(OpenAiChatModelClient client, ChatRequest request)
    {
        var events = new List<ChatStreamEvent>();
        await foreach (var item in client.StreamAsync(request, CancellationToken.None))
        {
            events.Add(item);
        }

        return events;
    }

    [Fact]
    public async Task Text_deltas_then_completion_with_usage()
    {
        var handler = new StubHandler(() => Sse(Delta("Olá"), Delta(" mundo"), Completed));

        var events = await CollectAsync(Client(handler), Request);

        events.Should().HaveCount(3);
        events[0].Should().Be(new ChatTextDelta("Olá"));
        events[1].Should().Be(new ChatTextDelta(" mundo"));
        var completed = events[2].Should().BeOfType<ChatCompleted>().Subject;
        completed.FinishReason.Should().Be(ChatFinishReason.Stop);
        completed.ToolCalls.Should().BeEmpty();
        completed.Usage.Should().Be(new ChatUsage(120, 100, 7));
        completed.ReplayState.Should().BeNull();
    }

    [Fact]
    public async Task Payload_carries_model_instructions_messages_and_strict_tools_without_storage()
    {
        var handler = new StubHandler(() => Sse(Completed));

        await CollectAsync(Client(handler), Request);

        var body = handler.Bodies.Should().ContainSingle().Subject;
        ((string?)body["model"]).Should().Be("model-x");
        ((bool?)body["stream"]).Should().BeTrue();
        ((bool?)body["store"]).Should().BeFalse();
        ((string?)body["instructions"]).Should().Be("be brief");
        ((int?)body["max_output_tokens"]).Should().Be(256);
        body["input"]!.AsArray().Select(item => (string?)item!["role"]).Should().Equal("user", "assistant", "user");
        ((bool?)body["tools"]![0]!["strict"]).Should().BeTrue();
        ((string?)body["tools"]![0]!["name"]).Should().Be("search_items");
        ((string?)body["include"]![0]).Should().Be("reasoning.encrypted_content");
    }

    [Fact]
    public async Task Without_tools_and_reasoning_the_payload_omits_them()
    {
        var handler = new StubHandler(() => Sse(Completed));

        await CollectAsync(Client(handler, includeReasoning: false), Request with { Tools = [] });

        var body = handler.Bodies.Single().AsObject();
        body.ContainsKey("tools").Should().BeFalse();
        body.ContainsKey("include").Should().BeFalse();
        body.ContainsKey("reasoning").Should().BeFalse();
    }

    [Fact]
    public async Task Reasoning_effort_is_sent_when_configured()
    {
        var handler = new StubHandler(() => Sse(Completed));

        await CollectAsync(Client(handler, effort: " Low "), Request);

        ((string?)handler.Bodies.Single()["reasoning"]!["effort"]).Should().Be("low");
    }

    [Fact]
    public async Task Reasoning_items_without_encrypted_content_are_not_replayed()
    {
        var handler = new StubHandler(() => Sse(Completed));
        var replay = """[{"type":"reasoning","id":"rs_2","summary":[]},{"type":"function_call","id":"fc_2","call_id":"call_2","name":"search_items","arguments":"{}"}]""";
        var round = new ChatToolRound(replay, [new ChatToolCall("call_2", "search_items", "{}")], [new ChatToolOutput("call_2", "{}")]);

        await CollectAsync(Client(handler, includeReasoning: false), Request with { ToolRounds = [round] });

        handler.Bodies.Single()["input"]!.AsArray().Select(item => (string?)item!["type"])
            .Should().Equal(null, null, null, "function_call", "function_call_output");
    }

    [Fact]
    public async Task Function_calls_finish_with_tool_calls_and_are_replayed_with_their_outputs()
    {
        var handler = new StubHandler(() => Sse(ToolCalls));
        var client = Client(handler);

        var completed = (await CollectAsync(client, Request)).Should().ContainSingle().Subject.Should().BeOfType<ChatCompleted>().Subject;

        completed.FinishReason.Should().Be(ChatFinishReason.ToolCalls);
        completed.ToolCalls.Should().Equal(new ChatToolCall("call_1", "search_items", """{"status":null}"""));
        completed.Usage.Should().Be(new ChatUsage(50, 0, 10));

        var round = new ChatToolRound(completed.ReplayState, completed.ToolCalls, [new ChatToolOutput("call_1", """{"items":[]}""")]);
        await CollectAsync(client, Request with { ToolRounds = [round] });

        var input = handler.Bodies[1]["input"]!.AsArray();
        input.Select(item => (string?)item!["type"]).Should().Equal(null, null, null, "reasoning", "function_call", "function_call_output");
        ((string?)input[3]!["encrypted_content"]).Should().Be("opaque");
        ((string?)input[5]!["call_id"]).Should().Be("call_1");
        ((string?)input[5]!["output"]).Should().Be("""{"items":[]}""");
    }

    [Fact]
    public async Task Round_without_replay_state_is_rebuilt_from_the_calls()
    {
        var handler = new StubHandler(() => Sse(Completed));
        var round = new ChatToolRound(null, [new ChatToolCall("call_9", "get_item", "{}")], [new ChatToolOutput("call_9", "{}")]);

        await CollectAsync(Client(handler), Request with { ToolRounds = [round] });

        var replayed = handler.Bodies.Single()["input"]![3]!;
        ((string?)replayed["type"]).Should().Be("function_call");
        ((string?)replayed["call_id"]).Should().Be("call_9");
        ((string?)replayed["name"]).Should().Be("get_item");
    }

    [Fact]
    public async Task Incomplete_response_at_the_token_limit_is_truncated()
    {
        var handler = new StubHandler(() => Sse(Delta("parte"), """{"type":"response.incomplete","response":{"incomplete_details":{"reason":"max_output_tokens"},"output":[],"usage":{"input_tokens":1,"output_tokens":2}}}"""));

        var events = await CollectAsync(Client(handler), Request);

        events[^1].Should().BeOfType<ChatCompleted>().Which.FinishReason.Should().Be(ChatFinishReason.Truncated);
    }

    [Fact]
    public async Task Incomplete_response_for_another_reason_is_a_failure()
    {
        var handler = new StubHandler(() => Sse(Delta("parte"), """{"type":"response.incomplete","response":{"incomplete_details":{"reason":"content_filter"},"output":[],"usage":{"input_tokens":1,"output_tokens":2}}}"""));

        var events = await CollectAsync(Client(handler), Request);

        events[^1].Should().Be(new ChatFailed("incomplete_content_filter", new ChatUsage(1, 0, 2)));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "http_401")]
    [InlineData(HttpStatusCode.TooManyRequests, "http_429")]
    public async Task Http_error_becomes_a_failure_with_the_status(HttpStatusCode status, string reason)
    {
        var handler = new StubHandler(() => new HttpResponseMessage(status));

        var events = await CollectAsync(Client(handler), Request);

        events.Should().Equal(new ChatFailed(reason, ChatUsage.Zero));
    }

    [Fact]
    public async Task Failed_response_carries_the_error_code_and_usage()
    {
        var handler = new StubHandler(() => Sse("""{"type":"response.failed","response":{"error":{"code":"server_error"},"usage":{"input_tokens":3,"output_tokens":0}}}"""));

        var events = await CollectAsync(Client(handler), Request);

        events.Should().Equal(new ChatFailed("server_error", new ChatUsage(3, 0, 0)));
    }

    [Fact]
    public async Task Stream_that_ends_without_a_final_event_is_a_failure()
    {
        var handler = new StubHandler(() => Sse(Delta("meia")));

        var events = await CollectAsync(Client(handler), Request);

        events[^1].Should().Be(new ChatFailed("stream_ended", ChatUsage.Zero));
    }

    [Fact]
    public async Task Transport_exception_becomes_a_failure()
    {
        var handler = new StubHandler(() => throw new HttpRequestException("boom"));

        var events = await CollectAsync(Client(handler), Request);

        events.Should().Equal(new ChatFailed("transport", ChatUsage.Zero));
    }
}
