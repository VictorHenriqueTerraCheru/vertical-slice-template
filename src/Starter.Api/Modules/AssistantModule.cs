using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;

using Starter.Api.Assistant;
using Starter.Api.Common;

namespace Starter.Api.Modules;

public sealed class AssistantModule : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/assistant")
            .WithTags("Assistant")
            .MapPost("/messages", Send)
            .Validate<SendAssistantMessageRequest>()
            .RequireRateLimiting(RateLimitPolicies.Ai)
            .WithSummary("Envia a conversa ao assistente de tarefas e devolve a resposta em streaming (SSE)")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static Results<ServerSentEventsResult<object>, ProblemHttpResult> Send(
        SendAssistantMessageRequest request, ClaimsPrincipal user, IAssistantService service, HttpContext http, CancellationToken ct)
    {
        var result = service.Start(user.GetUserId(), request, ct);
        if (result.Error is { } error)
        {
            return error.ToProblem();
        }

        http.Response.Headers[AssistantConstants.ProxyBufferingHeader] = "no";
        return TypedResults.ServerSentEvents(ItemsAsync(result.Value!.Events, ct));
    }

    private static async IAsyncEnumerable<SseItem<object>> ItemsAsync(
        IAsyncEnumerable<AssistantStreamEvent> events, [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var item in events.WithCancellation(ct))
        {
            yield return new SseItem<object>(item, EventName(item));
        }
    }

    private static string EventName(AssistantStreamEvent item) => item switch
    {
        AssistantDelta => AssistantConstants.EventDelta,
        AssistantDone => AssistantConstants.EventDone,
        _ => AssistantConstants.EventError,
    };
}
