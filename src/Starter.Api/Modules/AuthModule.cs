using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Starter.Api.Auth;
using Starter.Api.Common;

namespace Starter.Api.Modules;

public sealed class AuthModule : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .AllowAnonymous()
            .Validate<RegisterRequest>()
            .WithSummary("Cria uma conta e devolve os tokens de acesso")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .Validate<LoginRequest>()
            .WithSummary("Autentica com e-mail e senha e devolve os tokens de acesso")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/refresh", RefreshAsync)
            .AllowAnonymous()
            .Validate<RefreshRequest>()
            .WithSummary("Troca um refresh token por um novo par de tokens e invalida o anterior")
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithSummary("Encerra todas as sessões da conta autenticada");
    }

    private static async Task<Results<Created<TokenPair>, ProblemHttpResult>> RegisterAsync(
        RegisterRequest request, IAuthService service, CancellationToken ct)
    {
        var result = await service.RegisterAsync(request, ct);
        return result.Error switch
        {
            null => TypedResults.Created("/api/v1/me", result.Value!),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<Ok<TokenPair>, ProblemHttpResult>> LoginAsync(
        LoginRequest request, IAuthService service, CancellationToken ct)
    {
        var result = await service.LoginAsync(request, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<Ok<TokenPair>, ProblemHttpResult>> RefreshAsync(
        RefreshRequest request, IAuthService service, CancellationToken ct)
    {
        var result = await service.RefreshAsync(request, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToProblem(),
        };
    }

    private static async Task<NoContent> LogoutAsync(
        ClaimsPrincipal user, IAuthService service, CancellationToken ct)
    {
        await service.LogoutAsync(user.GetUserId(), ct);
        return TypedResults.NoContent();
    }
}
