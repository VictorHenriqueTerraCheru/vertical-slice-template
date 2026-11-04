using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;

using Starter.Api.Common;
using Starter.Api.Todos;

namespace Starter.Api.Modules;

public sealed class TodoModule : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/todos")
            .WithTags("Todos");

        group.MapPost("/", CreateAsync)
            .Validate<CreateTodoRequest>()
            .WithSummary("Cria uma tarefa para o usuário autenticado");

        group.MapGet("/", ListAsync)
            .Validate<ListTodosRequest>()
            .WithSummary("Lista as tarefas do usuário autenticado com paginação, ordenação e filtro de status");

        group.MapGet("/{id:guid}", GetAsync)
            .WithSummary("Devolve uma tarefa do usuário autenticado");

        group.MapPut("/{id:guid}", UpdateAsync)
            .Validate<UpdateTodoRequest>()
            .WithSummary("Atualiza título e descrição de uma tarefa")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/transitions", TransitionAsync)
            .Validate<TransitionTodoRequest>()
            .WithSummary("Muda o status de uma tarefa e registra o evento no histórico")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}/events", ListEventsAsync)
            .WithSummary("Lista o histórico de mudanças de status de uma tarefa em ordem de ocorrência");

        group.MapDelete("/{id:guid}", DeleteAsync)
            .WithSummary("Exclui uma tarefa")
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapGroup("/api/v1/admin/todos")
            .WithTags("Todos")
            .RequireAuthorization(AuthorizationPolicies.Administrator)
            .MapGet("/", ListAllAsync)
            .Validate<ListTodosRequest>()
            .WithSummary("Lista as tarefas de todos os usuários com paginação, ordenação e filtro de status");
    }

    private static async Task<Results<Created<TodoItemResponse>, ProblemHttpResult>> CreateAsync(
        CreateTodoRequest request, ClaimsPrincipal user, ITodoService service, CancellationToken ct)
    {
        var result = await service.CreateAsync(user.GetUserId(), request, ct);
        return result.Error switch
        {
            null => TypedResults.Created($"/api/v1/todos/{result.Value!.Id}", result.Value),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<Ok<PagedResponse<TodoItemResponse>>, ProblemHttpResult>> ListAsync(
        [AsParameters] ListTodosRequest request, ClaimsPrincipal user, ITodoService service, CancellationToken ct)
    {
        var result = await service.ListAsync(user.GetUserId(), request, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<Ok<TodoItemResponse>, NotFound>> GetAsync(
        Guid id, ClaimsPrincipal user, ITodoService service, CancellationToken ct)
    {
        var result = await service.GetAsync(user.GetUserId(), id, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToNotFound(),
        };
    }

    private static async Task<Results<Ok<TodoItemResponse>, NotFound, ProblemHttpResult>> UpdateAsync(
        Guid id, UpdateTodoRequest request, ClaimsPrincipal user, ITodoService service, CancellationToken ct)
    {
        var result = await service.UpdateAsync(user.GetUserId(), id, request, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            { Kind: ErrorType.NotFound } error => error.ToNotFound(),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<Ok<TodoItemResponse>, NotFound, ProblemHttpResult>> TransitionAsync(
        Guid id, TransitionTodoRequest request, ClaimsPrincipal user, ITodoService service, CancellationToken ct)
    {
        var result = await service.TransitionAsync(user.GetUserId(), id, request, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            { Kind: ErrorType.NotFound } error => error.ToNotFound(),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<Ok<TodoEventResponse[]>, NotFound>> ListEventsAsync(
        Guid id, ClaimsPrincipal user, ITodoService service, CancellationToken ct)
    {
        var result = await service.ListEventsAsync(user.GetUserId(), id, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToNotFound(),
        };
    }

    private static async Task<Results<Ok<PagedResponse<AdminTodoItemResponse>>, ProblemHttpResult>> ListAllAsync(
        [AsParameters] ListTodosRequest request, ITodoService service, CancellationToken ct)
    {
        var result = await service.ListAllAsync(request, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteAsync(
        Guid id, ClaimsPrincipal user, ITodoService service, CancellationToken ct)
    {
        var result = await service.DeleteAsync(user.GetUserId(), id, ct);
        return result.Error switch
        {
            null => TypedResults.NoContent(),
            { Kind: ErrorType.NotFound } error => error.ToNotFound(),
            var error => error.ToProblem(),
        };
    }
}
