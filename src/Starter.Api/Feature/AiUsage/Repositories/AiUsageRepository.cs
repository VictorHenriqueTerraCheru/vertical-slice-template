using Microsoft.EntityFrameworkCore;

using Starter.Api.Persistence;

namespace Starter.Api.AiUsage;

public sealed class AiUsageRepository(AppDbContext db) : IAiUsageRepository
{
    public void Add(AiUsageEntry entry) => db.Set<AiUsageEntry>().Add(entry);

    public void Discard(AiUsageEntry entry) => db.Entry(entry).State = EntityState.Detached;

    public Task<AiUsageByModel[]> SummarizeAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
        db.Set<AiUsageEntry>()
            .AsNoTracking()
            .Where(entry => entry.OccurredAt >= from && entry.OccurredAt < to)
            .GroupBy(entry => new { entry.Provider, entry.Model })
            .OrderBy(group => group.Key.Provider)
            .ThenBy(group => group.Key.Model)
            .Select(group => new AiUsageByModel(
                group.Key.Provider,
                group.Key.Model,
                group.Count(),
                group.Count(entry => entry.Status == AiCallStatus.Failed),
                group.Sum(entry => (long)entry.InputTokens),
                group.Sum(entry => (long)entry.CachedInputTokens),
                group.Sum(entry => (long)entry.OutputTokens)))
            .ToArrayAsync(cancellationToken);
}
