using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Text.Json;

using Starter.Api.Assistant;
using Starter.Api.Common;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Assistant;

public sealed record SseEvent(string Name, JsonElement Data);

public static class AssistantClient
{
    private static readonly JsonSerializerOptions Json = CreateJson();

    public static Task<HttpResponseMessage> SendAsync(HttpClient http, params AssistantMessageInput[] messages) =>
        http.PostAsJsonAsync("/api/v1/assistant/messages", new SendAssistantMessageRequest(messages), Json);

    public static Task<HttpResponseMessage> AskAsync(HttpClient http, string question) =>
        SendAsync(http, new AssistantMessageInput(ChatRole.User, question));

    public static async Task<IReadOnlyList<SseEvent>> ReadEventsAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/event-stream");
        var events = new List<SseEvent>();
        await using var body = await response.Content.ReadAsStreamAsync();
        await foreach (var item in SseParser.Create(body).EnumerateAsync())
        {
            events.Add(new SseEvent(item.EventType, JsonDocument.Parse(item.Data).RootElement.Clone()));
        }

        return events;
    }

    public static string Text(IEnumerable<SseEvent> events) =>
        string.Concat(events.Where(item => item.Name == "delta").Select(item => item.Data.GetProperty("text").GetString()));

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions();
        JsonConfig.Configure(options);
        return options;
    }
}
