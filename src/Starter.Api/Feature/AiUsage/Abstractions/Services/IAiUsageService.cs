using Starter.Api.Common;

namespace Starter.Api.AiUsage;

public interface IAiUsageService
{
    Task RecordAsync(AiUsageRecord record, CancellationToken cancellationToken);

    Task<Result<AiUsageReport>> ReportAsync(AiUsageReportRequest request, CancellationToken cancellationToken);
}
