using System.Globalization;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Nodes;

namespace Starter.Api.Common;

public sealed class OpenAiChatModelClient(
    HttpClient http,
    EnvironmentConfiguration configuration,
    ILogger<OpenAiChatModelClient> logger) : IChatModelClient
{
    public async IAsyncEnumerable<ChatStreamEvent> StreamAsync(
        ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(AiConstants.ModelCallTimeout);
        await using var events = ReadAsync(request, timeout.Token).GetAsyncEnumerator(timeout.Token);

        while (true)
        {
            ChatStreamEvent next;
            try
            {
                if (!await events.MoveNextAsync())
                {
                    yield break;
                }

                next = events.Current;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning("Chat provider call failed with {ExceptionType}", exception.GetType().Name);
                next = new ChatFailed(
                    exception is OperationCanceledException ? AiConstants.FailureTimeout : AiConstants.FailureTransport,
                    ChatUsage.Zero);
            }

            yield return next;
            if (next is not ChatTextDelta)
            {
                yield break;
            }
        }
    }

    private async IAsyncEnumerable<ChatStreamEvent> ReadAsync(
        ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var message = new HttpRequestMessage(HttpMethod.Post, AiConstants.OpenAiResponsesPath)
        {
            Content = new StringContent(Payload(request).ToJsonString(), Encoding.UTF8, "application/json"),
        };
        using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            logger.LogWarning("Chat provider answered with status {StatusCode}", status);
            yield return new ChatFailed(AiConstants.FailureHttpPrefix + status.ToString(CultureInfo.InvariantCulture), ChatUsage.Zero);
            yield break;
        }

        await using var body = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var item in SseParser.Create(body).EnumerateAsync(cancellationToken))
        {
            var parsed = Parse(item.Data);
            if (parsed is null)
            {
                continue;
            }

            yield return parsed;
            if (parsed is not ChatTextDelta)
            {
                yield break;
            }
        }

        yield return new ChatFailed(AiConstants.FailureStreamEnded, ChatUsage.Zero);
    }

    private JsonObject Payload(ChatRequest request)
    {
        var payload = new JsonObject
        {
            ["model"] = configuration.AiChatModel,
            ["stream"] = true,
            ["store"] = false,
            ["instructions"] = request.Instructions,
            ["max_output_tokens"] = request.MaxOutputTokens,
            ["input"] = Input(request),
        };

        if (request.Tools.Count > 0)
        {
            payload["tools"] = new JsonArray([.. request.Tools.Select(Tool)]);
        }

        if (configuration.AiChatIncludeReasoning)
        {
            payload["include"] = new JsonArray("reasoning.encrypted_content");
        }

        if (configuration.AiChatReasoningEffort is not null)
        {
            payload["reasoning"] = new JsonObject { ["effort"] = configuration.AiChatReasoningEffort };
        }

        return payload;
    }

    private static JsonArray Input(ChatRequest request)
    {
        var input = new JsonArray();
        foreach (var message in request.Messages)
        {
            input.Add(new JsonObject
            {
                ["role"] = message.Role == ChatRole.User ? "user" : "assistant",
                ["content"] = message.Text,
            });
        }

        foreach (var round in request.ToolRounds)
        {
            foreach (var item in ReplayItems(round))
            {
                input.Add(item);
            }

            foreach (var output in round.Outputs)
            {
                input.Add(new JsonObject
                {
                    ["type"] = "function_call_output",
                    ["call_id"] = output.CallId,
                    ["output"] = output.Output,
                });
            }
        }

        return input;
    }

    private static IEnumerable<JsonNode> ReplayItems(ChatToolRound round)
    {
        if (round.ReplayState is not null && JsonNode.Parse(round.ReplayState) is JsonArray items)
        {
            return items
                .Where(item => (string?)item?["type"] != "reasoning" || item!["encrypted_content"] is not null)
                .Select(item => item!.DeepClone());
        }

        return round.Calls.Select(call => (JsonNode)new JsonObject
        {
            ["type"] = "function_call",
            ["call_id"] = call.CallId,
            ["name"] = call.Name,
            ["arguments"] = call.ArgumentsJson,
        });
    }

    private static JsonObject Tool(ChatToolDefinition tool) => new()
    {
        ["type"] = "function",
        ["name"] = tool.Name,
        ["description"] = tool.Description,
        ["parameters"] = JsonNode.Parse(tool.ParametersJsonSchema),
        ["strict"] = true,
    };

    private static ChatStreamEvent? Parse(string data)
    {
        if (!data.StartsWith('{'))
        {
            return null;
        }

        var root = JsonNode.Parse(data)!;
        return (string?)root["type"] switch
        {
            "response.output_text.delta" => new ChatTextDelta((string?)root["delta"] ?? string.Empty),
            "response.completed" => Completed(root["response"], truncated: false),
            "response.incomplete" => Incomplete(root["response"]),
            "response.failed" => new ChatFailed((string?)root["response"]?["error"]?["code"] ?? "failed", Usage(root["response"])),
            "error" => new ChatFailed((string?)root["code"] ?? "error", ChatUsage.Zero),
            _ => null,
        };
    }

    private static ChatStreamEvent Incomplete(JsonNode? response)
    {
        var reason = (string?)response?["incomplete_details"]?["reason"];
        return reason == "max_output_tokens"
            ? Completed(response, truncated: true)
            : new ChatFailed(AiConstants.FailureIncompletePrefix + (reason ?? "unknown"), Usage(response));
    }

    private static ChatCompleted Completed(JsonNode? response, bool truncated)
    {
        var output = response?["output"] as JsonArray ?? new JsonArray();
        var calls = output
            .Where(item => (string?)item?["type"] == "function_call")
            .Select(item => new ChatToolCall(
                (string?)item!["call_id"] ?? string.Empty,
                (string?)item["name"] ?? string.Empty,
                (string?)item["arguments"] ?? "{}"))
            .ToList();

        var reason = truncated ? ChatFinishReason.Truncated
            : calls.Count > 0 ? ChatFinishReason.ToolCalls
            : ChatFinishReason.Stop;

        return new ChatCompleted(reason, calls, Usage(response), calls.Count > 0 ? output.ToJsonString() : null);
    }

    private static ChatUsage Usage(JsonNode? response)
    {
        var usage = response?["usage"];
        if (usage is null)
        {
            return ChatUsage.Zero;
        }

        return new ChatUsage(
            (int?)usage["input_tokens"] ?? 0,
            (int?)usage["input_tokens_details"]?["cached_tokens"] ?? 0,
            (int?)usage["output_tokens"] ?? 0);
    }
}
