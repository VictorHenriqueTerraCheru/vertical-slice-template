using Microsoft.AspNetCore.Http.HttpResults;
using Starter.Api.Common;
using Starter.Api.Health;

namespace Starter.Api.Modules;

public sealed class HealthModule : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/health")
            .AllowAnonymous()
            .WithTags("Health");

        group.MapGet("/", Application)
            .WithSummary("Verifica se a API está no ar");

        group.MapGet("/database", Database)
            .WithSummary("Verifica a conexão com o banco de dados")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);
    }

    private static Ok<HealthResponse> Application() => TypedResults.Ok(HealthResponse.Healthy);

    private static async Task<Results<Ok<HealthResponse>, ProblemHttpResult>> Database(IHealthService service, CancellationToken ct)
    {
        var result = await service.VerifyDatabaseAsync(ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToProblem(),
        };
    }
}
