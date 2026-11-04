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

        group.MapGet("/", GetMe)
            .WithSummary("Devolve os dados do usuário autenticado");

        group.MapPut("/", UpdateMe)
            .Validate<UpdateMeRequest>()
            .WithSummary("Atualiza os dados do usuário autenticado");
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> GetMe(
        ClaimsPrincipal user, IUserService service, CancellationToken ct)
    {
        var result = await service.GetMeAsync(user.GetUserId(), ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToNotFound(),
        };
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> UpdateMe(
        UpdateMeRequest request, ClaimsPrincipal user, IUserService service, CancellationToken ct)
    {
        var result = await service.UpdateMeAsync(user.GetUserId(), request, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToNotFound(),
        };
    }
}
