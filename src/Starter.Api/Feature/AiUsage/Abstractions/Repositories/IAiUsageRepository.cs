using Starter.Api.Persistence;

namespace Starter.Api.AiUsage;

public interface IAiUsageRepository
{
    void Add(AiUsageEntry entry);

    void Discard(AiUsageEntry entry);

    Task<AiUsageByModel[]> SummarizeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken);
}
