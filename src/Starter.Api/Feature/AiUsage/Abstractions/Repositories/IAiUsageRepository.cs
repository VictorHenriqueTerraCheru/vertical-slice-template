using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.AiUsage;

public interface IAiUsageRepository
{
    void Add(AiUsageEntry entry);

    void Discard(AiUsageEntry entry);

    Task<PagedResponse<AiUsageByModel>> SummarizeAsync(
        DateTimeOffset from, DateTimeOffset to, AiUsageReportRequest request, CancellationToken cancellationToken);
}
