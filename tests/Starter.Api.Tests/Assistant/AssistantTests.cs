using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

using Starter.Api.AiUsage;
using Starter.Api.Assistant;
using Starter.Api.Common;
using Starter.Api.Persistence;
using Starter.Api.Tests.Infra;
using Starter.Api.Todos;

namespace Starter.Api.Tests.Assistant;

[Collection(nameof(ApiCollection))]
public sealed class AssistantTests(ApiFactory factory)
{
    private static ChatCompleted Stop(int input = 10, int cached = 0, int output = 5) =>
        new(ChatFinishReason.Stop, [], new ChatUsage(input, cached, output), null);

    private static ChatCompleted Calls(params ChatToolCall[] calls) =>
        new(ChatFinishReason.ToolCalls, calls, new ChatUsage(20, 0, 3), "[]");

    private async Task<(RegisteredUser User, HttpClient Http)> NewUserAsync()
    {
        var user = await Scenarios.RegisterAsync(factory);
        return (user, Scenarios.ClientFor(factory, user));
    }

    private static async Task<TodoItemResponse> CreateTodoAsync(HttpClient http, string title)
    {
        var response = await http.PostAsJsonAsync("/api/v1/todos", new CreateTodoRequest(title, null));
        response.EnsureSuccessStatusCode();
        return (await ApiClient.ReadAsync<TodoItemResponse>(response))!;
    }

    private Task<List<AiUsageEntry>> UsageOfAsync(Guid userId) =>
        factory.InDatabaseAsync(db => db.Set<AiUsageEntry>().Where(entry => entry.UserId == userId).ToListAsync());

    [Fact]
    public async Task Streams_text_deltas_and_finishes_with_done()
    {
        var (_, http) = await NewUserAsync();
        factory.Chat.Script([new ChatTextDelta("Você tem "), new ChatTextDelta("2 tarefas."), Stop()]);

        var events = await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "Quantas tarefas eu tenho?"));

        events.Select(item => item.Name).Should().Equal("delta", "delta", "done");
        AssistantClient.Text(events).Should().Be("Você tem 2 tarefas.");
        events[^1].Data.GetProperty("truncated").GetBoolean().Should().BeFalse();
        var request = factory.Chat.Requests.Should().ContainSingle().Subject;
        request.Instructions.Should().Be(AssistantConstants.Instructions);
        request.Messages.Should().Equal(new ChatMessage(ChatRole.User, "Quantas tarefas eu tenho?"));
        request.Tools.Should().BeEquivalentTo(AssistantConstants.Tools);
    }

    [Fact]
    public async Task Conversation_history_is_sent_in_order()
    {
        var (_, http) = await NewUserAsync();
        factory.Chat.Script([new ChatTextDelta("Certo."), Stop()]);

        await AssistantClient.ReadEventsAsync(await AssistantClient.SendAsync(
            http,
            new AssistantMessageInput(ChatRole.User, "Oi"),
            new AssistantMessageInput(ChatRole.Assistant, "Olá!"),
            new AssistantMessageInput(ChatRole.User, "  E agora?  ")));

        factory.Chat.Requests.Single().Messages.Should().Equal(
            new ChatMessage(ChatRole.User, "Oi"),
            new ChatMessage(ChatRole.Assistant, "Olá!"),
            new ChatMessage(ChatRole.User, "E agora?"));
    }

    [Fact]
    public async Task Records_one_usage_entry_per_model_call()
    {
        var (user, http) = await NewUserAsync();
        factory.Chat.Script(
            [Calls(new ChatToolCall("c1", AssistantConstants.ToolListTodos, """{"status":null}"""))],
            [new ChatTextDelta("Nenhuma."), Stop(input: 30, cached: 12, output: 4)]);

        await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "Liste"));

        var usage = await UsageOfAsync(user.UserId);
        usage.Select(entry => entry.Status).Should().Equal(AiCallStatus.Completed, AiCallStatus.Completed);
        usage.Should().OnlyContain(entry => entry.Feature == "assistant" && entry.Provider == "openai" && entry.Model == "test-model");
        usage.Should().ContainSingle(entry => entry.InputTokens == 30 && entry.CachedInputTokens == 12 && entry.OutputTokens == 4);
        usage.Should().ContainSingle(entry => entry.InputTokens == 20 && entry.OutputTokens == 3);
    }

    [Fact]
    public async Task List_tool_reads_only_the_callers_todos()
    {
        var (_, http) = await NewUserAsync();
        var (_, otherHttp) = await NewUserAsync();
        await CreateTodoAsync(http, "Comprar pão");
        await CreateTodoAsync(otherHttp, "Tarefa de outra pessoa");
        factory.Chat.Script(
            [Calls(new ChatToolCall("c1", AssistantConstants.ToolListTodos, """{"status":"pending"}"""))],
            [new ChatTextDelta("Você precisa comprar pão."), Stop()]);

        var events = await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "O que falta?"));

        events[^1].Name.Should().Be("done");
        var round = factory.Chat.Requests[1].ToolRounds.Should().ContainSingle().Subject;
        round.ReplayState.Should().Be("[]");
        var output = round.Outputs.Should().ContainSingle().Subject;
        output.CallId.Should().Be("c1");
        var listed = JsonDocument.Parse(output.Output).RootElement;
        listed.GetProperty("total").GetInt32().Should().Be(1);
        listed.GetProperty("items")[0].GetProperty("title").GetString().Should().Be("Comprar pão");
        output.Output.Should().NotContain("outra pessoa");
    }

    [Fact]
    public async Task Get_tool_hides_todos_of_other_users_and_reports_bad_arguments()
    {
        var (_, http) = await NewUserAsync();
        var (_, otherHttp) = await NewUserAsync();
        var mine = await CreateTodoAsync(http, "Minha");
        var theirs = await CreateTodoAsync(otherHttp, "Dela");
        factory.Chat.Script(
            [Calls(
                new ChatToolCall("c1", AssistantConstants.ToolGetTodo, $$"""{"id":"{{mine.Id}}"}"""),
                new ChatToolCall("c2", AssistantConstants.ToolGetTodo, $$"""{"id":"{{theirs.Id}}"}"""),
                new ChatToolCall("c3", AssistantConstants.ToolGetTodo, """{"id":"not-a-guid"}"""),
                new ChatToolCall("c4", "delete_everything", "{}"),
                new ChatToolCall("c5", AssistantConstants.ToolListTodos, "not json"))],
            [new ChatTextDelta("ok"), Stop()]);

        await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "Detalhes"));

        var outputs = factory.Chat.Requests[1].ToolRounds.Single().Outputs;
        JsonDocument.Parse(outputs[0].Output).RootElement.GetProperty("title").GetString().Should().Be("Minha");
        outputs[1].Output.Should().Be("""{"error":"not_found"}""");
        outputs[2].Output.Should().Be("""{"error":"invalid_arguments"}""");
        outputs[3].Output.Should().Be("""{"error":"unknown_tool"}""");
        outputs[4].Output.Should().Be("""{"error":"invalid_arguments"}""");
    }

    [Fact]
    public async Task Calls_beyond_the_round_limit_get_an_error_output()
    {
        var (_, http) = await NewUserAsync();
        var calls = Enumerable.Range(0, AssistantConstants.MaxToolCallsPerRound + 1)
            .Select(index => new ChatToolCall($"c{index}", AssistantConstants.ToolListTodos, """{"status":null}"""))
            .ToArray();
        factory.Chat.Script([Calls(calls)], [new ChatTextDelta("ok"), Stop()]);

        await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "Liste"));

        var outputs = factory.Chat.Requests[1].ToolRounds.Single().Outputs;
        outputs.Should().HaveCount(calls.Length);
        outputs[^1].Output.Should().Be("""{"error":"too_many_calls"}""");
        outputs[0].Output.Should().Contain("\"total\"");
    }

    [Fact]
    public async Task Last_round_offers_no_tools()
    {
        var (_, http) = await NewUserAsync();
        var call = Calls(new ChatToolCall("c", AssistantConstants.ToolListTodos, """{"status":null}"""));
        var script = Enumerable.Repeat<ChatStreamEvent[]>([call], AssistantConstants.MaxToolRounds)
            .Append([new ChatTextDelta("Pronto."), Stop()])
            .ToArray();
        factory.Chat.Script(script);

        var events = await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "Liste sem parar"));

        events[^1].Name.Should().Be("done");
        var requests = factory.Chat.Requests;
        requests.Should().HaveCount(AssistantConstants.MaxToolRounds + 1);
        requests.SkipLast(1).Should().OnlyContain(request => request.Tools.Count > 0);
        requests[^1].Tools.Should().BeEmpty();
        requests[^1].ToolRounds.Should().HaveCount(AssistantConstants.MaxToolRounds);
        requests[0].ToolRounds.Should().BeEmpty();
    }

    [Fact]
    public async Task Provider_failure_ends_with_an_error_event_and_a_failed_usage_entry()
    {
        var (user, http) = await NewUserAsync();
        factory.Chat.Script([new ChatTextDelta("Comecei"), new ChatFailed("http_500", new ChatUsage(8, 0, 1))]);

        var events = await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "Oi"));

        events.Select(item => item.Name).Should().Equal("delta", "error");
        events[^1].Data.GetProperty("type").GetString().Should().Be("ai-unavailable");
        events[^1].Data.GetProperty("title").GetString().Should().Be(AiErrors.Unavailable.Title);
        var entry = (await UsageOfAsync(user.UserId)).Should().ContainSingle().Subject;
        entry.Status.Should().Be(AiCallStatus.Failed);
        entry.InputTokens.Should().Be(8);
    }

    [Fact]
    public async Task Completion_without_any_text_is_an_error()
    {
        var (_, http) = await NewUserAsync();
        factory.Chat.Script([Stop()]);

        var events = await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "Oi"));

        events.Select(item => item.Name).Should().Equal("error");
    }

    [Fact]
    public async Task Usage_recording_failure_does_not_break_the_answer()
    {
        var (user, http) = await NewUserAsync();
        await factory.InDatabaseAsync(db => db.Set<User>().Where(row => row.Id == user.UserId).ExecuteDeleteAsync());
        factory.Chat.Script(
            [Calls(new ChatToolCall("c1", AssistantConstants.ToolListTodos, """{"status":null}"""))],
            [new ChatTextDelta("Nenhuma."), Stop()]);

        var events = await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "Liste"));

        events.Select(item => item.Name).Should().Equal("delta", "done");
        (await UsageOfAsync(user.UserId)).Should().BeEmpty();
    }

    [Fact]
    public async Task Truncated_answer_is_flagged_on_done()
    {
        var (user, http) = await NewUserAsync();
        factory.Chat.Script([new ChatTextDelta("Resposta cortada"), new ChatCompleted(ChatFinishReason.Truncated, [], ChatUsage.Zero, null)]);

        var events = await AssistantClient.ReadEventsAsync(await AssistantClient.AskAsync(http, "Conte tudo"));

        events[^1].Data.GetProperty("truncated").GetBoolean().Should().BeTrue();
        (await UsageOfAsync(user.UserId)).Single().Status.Should().Be(AiCallStatus.Truncated);
    }

    [Theory]
    [InlineData("""{"messages":[]}""")]
    [InlineData("""{}""")]
    [InlineData("""{"messages":[{"role":"user","content":"   "}]}""")]
    [InlineData("""{"messages":[{"role":"user","content":"oi"},{"role":"assistant","content":"olá"}]}""")]
    [InlineData("""{"messages":[null]}""")]
    public async Task Invalid_conversation_returns_400_without_calling_the_model(string body)
    {
        var (_, http) = await NewUserAsync();
        factory.Chat.Script();

        var response = await http.PostAsync("/api/v1/assistant/messages", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        factory.Chat.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task Too_many_or_too_long_messages_return_400()
    {
        var (_, http) = await NewUserAsync();
        var many = Enumerable.Range(0, AssistantConstants.MaxMessages + 1)
            .Select(_ => new AssistantMessageInput(ChatRole.User, "oi"))
            .ToArray();

        (await AssistantClient.SendAsync(http, many)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await AssistantClient.AskAsync(http, new string('a', AssistantConstants.MessageMaxLength + 1)))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Without_an_api_key_returns_503()
    {
        var user = await Scenarios.RegisterAsync(factory);
        using var unconfigured = factory.WithWebHostBuilder(builder => builder.UseSetting(ConfigurationKeys.AiChatApiKey, string.Empty));
        var http = unconfigured.CreateClient();
        http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user.Tokens.AccessToken);

        var response = await AssistantClient.AskAsync(http, "Oi");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await ApiClient.ReadJsonAsync(response)).GetProperty("type").GetString().Should().Be("ai-not-configured");
    }
}
