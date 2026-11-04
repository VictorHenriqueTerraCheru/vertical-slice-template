using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;

using Starter.Api.Common;
using Starter.Api.Users;

namespace Starter.Api.Modules;

public sealed class UserModule : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/me")
            .WithTags("Users");

        group.MapGet("/", GetMeAsync)
            .WithSummary("Devolve os dados do usuário autenticado");

        group.MapPut("/", UpdateMeAsync)
            .Validate<UpdateMeRequest>()
            .WithSummary("Atualiza os dados do usuário autenticado")
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetMeAsync(
        ClaimsPrincipal user, IUserService service, CancellationToken ct)
    {
        var result = await service.GetMeAsync(user.GetUserId(), ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToNotFound(),
        };
    }

    private static async Task<Results<Ok<UserResponse>, NotFound, ProblemHttpResult>> UpdateMeAsync(
        UpdateMeRequest request, ClaimsPrincipal user, IUserService service, CancellationToken ct)
    {
        var result = await service.UpdateMeAsync(user.GetUserId(), request, ct);
        if (result.Success)
        {
            return TypedResults.Ok(result.Value!);
        }

        if (result.Error!.Kind == ErrorType.NotFound)
        {
            return result.Error.ToNotFound();
        }

        return result.Error.ToProblem();
    }
}
