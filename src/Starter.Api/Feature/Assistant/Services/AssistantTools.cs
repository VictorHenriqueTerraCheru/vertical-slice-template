using System.Text.Json;

using Starter.Api.Common;
using Starter.Api.Todos;

namespace Starter.Api.Assistant;

public sealed class AssistantTools(ITodoService todos, ILogger<AssistantTools> logger) : IAssistantTools
{
    private static readonly JsonSerializerOptions Json = CreateJson();

    public async Task<IReadOnlyList<ChatToolOutput>> RunAsync(
        Guid userId, IReadOnlyList<ChatToolCall> calls, CancellationToken cancellationToken)
    {
        var outputs = new List<ChatToolOutput>(calls.Count);
        foreach (var (index, call) in calls.Index())
        {
            var output = index < AssistantConstants.MaxToolCallsPerRound
                ? await RunGuardedAsync(userId, call, cancellationToken)
                : Error(AssistantConstants.ToolErrorTooManyCalls);
            outputs.Add(new ChatToolOutput(call.CallId, output));
        }

        return outputs;
    }

    private async Task<string> RunGuardedAsync(Guid userId, ChatToolCall call, CancellationToken cancellationToken)
    {
        try
        {
            return await RunAsync(userId, call, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning("Assistant tool {Tool} failed: {ExceptionType}", call.Name, exception.GetType().Name);
            return Error(AssistantConstants.ToolErrorFailed);
        }
    }

    private async Task<string> RunAsync(Guid userId, ChatToolCall call, CancellationToken cancellationToken)
    {
        if (!TryReadArguments(call.ArgumentsJson, out var arguments))
        {
            return Error(AssistantConstants.ToolErrorInvalidArguments);
        }

        return call.Name switch
        {
            AssistantConstants.ToolListTodos => await ListAsync(userId, arguments, cancellationToken),
            AssistantConstants.ToolGetTodo => await GetAsync(userId, arguments, cancellationToken),
            _ => Error(AssistantConstants.ToolErrorUnknownTool),
        };
    }

    private async Task<string> ListAsync(Guid userId, JsonElement arguments, CancellationToken cancellationToken)
    {
        QueryEnum<TodoStatus>? status = null;
        if (arguments.TryGetProperty("status", out var value) && value.ValueKind != JsonValueKind.Null)
        {
            if (value.ValueKind != JsonValueKind.String || !QueryEnum<TodoStatus>.TryParse(value.GetString(), null, out var parsed))
            {
                return Error(AssistantConstants.ToolErrorInvalidArguments);
            }

            status = parsed;
        }

        var page = await todos.ListAsync(
            userId, new ListTodosRequest(Status: status, PageSize: AssistantConstants.ToolListLimit), cancellationToken);
        if (!page.Success)
        {
            return Error(AssistantConstants.ToolErrorFailed);
        }

        return JsonSerializer.Serialize(new { total = page.Value!.TotalCount, items = page.Value.Items }, Json);
    }

    private async Task<string> GetAsync(Guid userId, JsonElement arguments, CancellationToken cancellationToken)
    {
        if (!arguments.TryGetProperty("id", out var value)
            || value.ValueKind != JsonValueKind.String
            || !Guid.TryParse(value.GetString(), out var id))
        {
            return Error(AssistantConstants.ToolErrorInvalidArguments);
        }

        var todo = await todos.GetAsync(userId, id, cancellationToken);
        return todo.Success ? JsonSerializer.Serialize(todo.Value, Json) : Error(AssistantConstants.ToolErrorNotFound);
    }

    private static bool TryReadArguments(string json, out JsonElement arguments)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            arguments = document.RootElement.Clone();
            return arguments.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            arguments = default;
            return false;
        }
    }

    private static string Error(string code) => JsonSerializer.Serialize(new { error = code }, Json);

    private static JsonSerializerOptions CreateJson()
    {
        var options = new JsonSerializerOptions();
        JsonConfig.Configure(options);
        return options;
    }
}
