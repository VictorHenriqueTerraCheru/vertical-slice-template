using Starter.Api.Common;

namespace Starter.Api.AiUsage;

public interface IAiUsageService
{
    Task RecordAsync(AiUsageRecord record, CancellationToken cancellationToken);

    Task<Result<PagedResponse<AiUsageByModel>>> ReportAsync(AiUsageReportRequest request, CancellationToken cancellationToken);
}
