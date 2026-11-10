using Microsoft.AspNetCore.Http.HttpResults;

using Starter.Api.AiUsage;
using Starter.Api.Common;

namespace Starter.Api.Modules;

public sealed class AiUsageModule : IEndpointModule
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroup("/api/v1/admin/ai-usage")
            .WithTags("AiUsage")
            .RequireAuthorization(AuthorizationPolicies.Administrator)
            .MapGet("/", ReportAsync)
            .Validate<AiUsageReportRequest>()
            .WithSummary("Resume o gasto de IA por provedor e modelo em um período, com paginação e ordenação");
    }

    private static async Task<Results<Ok<PagedResponse<AiUsageByModel>>, ProblemHttpResult>> ReportAsync(
        [AsParameters] AiUsageReportRequest request, IAiUsageService service, CancellationToken ct)
    {
        var result = await service.ReportAsync(request, ct);
        return result.Error switch
        {
            null => TypedResults.Ok(result.Value!),
            var error => error.ToProblem(),
        };
    }
}
