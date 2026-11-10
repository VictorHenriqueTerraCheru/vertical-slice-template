using System.Linq.Expressions;

using Microsoft.EntityFrameworkCore;

using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.AiUsage;

public sealed class AiUsageRepository(AppDbContext db) : IAiUsageRepository
{
    private static readonly Dictionary<string, Expression<Func<AiUsageByModel, object>>> Sortable = new()
    {
        [AiUsageConstants.SortByModel] = row => row.Model,
        [AiUsageConstants.SortByCalls] = row => row.Calls,
        [AiUsageConstants.SortByInputTokens] = row => row.InputTokens,
        [AiUsageConstants.SortByOutputTokens] = row => row.OutputTokens,
    };

    public void Add(AiUsageEntry entry) => db.Set<AiUsageEntry>().Add(entry);

    public void Discard(AiUsageEntry entry) => db.Entry(entry).State = EntityState.Detached;

    public Task<PagedResponse<AiUsageByModel>> SummarizeAsync(
        DateTimeOffset from, DateTimeOffset to, AiUsageReportRequest request, CancellationToken cancellationToken) =>
        db.Set<AiUsageEntry>()
            .AsNoTracking()
            .Where(entry => entry.OccurredAt >= from && entry.OccurredAt < to)
            .GroupBy(entry => new { entry.Provider, entry.Model })
            .Select(group => new AiUsageByModel
            {
                Provider = group.Key.Provider,
                Model = group.Key.Model,
                Calls = group.Count(),
                FailedCalls = group.Count(entry => entry.Status == AiCallStatus.Failed),
                InputTokens = group.Sum(entry => (long)entry.InputTokens),
                CachedInputTokens = group.Sum(entry => (long)entry.CachedInputTokens),
                OutputTokens = group.Sum(entry => (long)entry.OutputTokens),
            })
            .ApplySort(request, Sortable, AiUsageConstants.SortByModel, SortDirection.Asc, row => row.Provider + "/" + row.Model)
            .ToPagedAsync(request, row => row, cancellationToken);
}
