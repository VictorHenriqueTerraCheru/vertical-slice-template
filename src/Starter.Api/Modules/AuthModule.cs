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
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Validate<RegisterRequest>()
            .WithSummary("Cria uma conta e devolve os tokens de acesso")
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth)
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

        group.MapPost("/forgot-password", ForgotPasswordAsync)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitPolicies.Auth)
            .Validate<ForgotPasswordRequest>()
            .WithSummary("Envia o link de redefinição de senha quando a conta existe; a resposta é sempre a mesma");

        group.MapPost("/reset-password", ResetPasswordAsync)
            .AllowAnonymous()
            .Validate<ResetPasswordRequest>()
            .WithSummary("Redefine a senha com o token recebido por e-mail e encerra todas as sessões")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/change-password", ChangePasswordAsync)
            .Validate<ChangePasswordRequest>()
            .WithSummary("Troca a senha da conta autenticada e encerra todas as sessões")
            .ProducesProblem(StatusCodes.Status400BadRequest);
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

    private static async Task<Results<NoContent, ProblemHttpResult>> LogoutAsync(
        ClaimsPrincipal user, IAuthService service, CancellationToken ct)
    {
        var result = await service.LogoutAsync(user.GetUserId(), ct);
        return result.Error switch
        {
            null => TypedResults.NoContent(),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ForgotPasswordAsync(
        ForgotPasswordRequest request, IAuthService service, CancellationToken ct)
    {
        var result = await service.ForgotPasswordAsync(request, ct);
        return result.Error switch
        {
            null => TypedResults.NoContent(),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<NoContent, ProblemHttpResult>> ResetPasswordAsync(
        ResetPasswordRequest request, IAuthService service, CancellationToken ct)
    {
        var result = await service.ResetPasswordAsync(request, ct);
        return result.Error switch
        {
            null => TypedResults.NoContent(),
            var error => error.ToProblem(),
        };
    }

    private static async Task<Results<NoContent, ValidationProblem, ProblemHttpResult>> ChangePasswordAsync(
        ChangePasswordRequest request, ClaimsPrincipal user, IAuthService service, CancellationToken ct)
    {
        var result = await service.ChangePasswordAsync(user.GetUserId(), request, ct);
        if (result.Success)
        {
            return TypedResults.NoContent();
        }

        return result.Error!.Kind == ErrorType.Field ? result.Error.ToValidation() : result.Error.ToProblem();
    }
}
