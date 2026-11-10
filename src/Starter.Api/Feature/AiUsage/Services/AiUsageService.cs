using Starter.Api.Common;
using Starter.Api.Persistence;

namespace Starter.Api.AiUsage;

public sealed class AiUsageService(
    IAiUsageRepository repository,
    IUnitOfWork unitOfWork,
    EnvironmentConfiguration configuration,
    TimeProvider clock,
    ILogger<AiUsageService> logger) : IAiUsageService
{
    public async Task RecordAsync(AiUsageRecord record, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var entry = new AiUsageEntry
        {
            Id = Guid.CreateVersion7(now),
            UserId = record.UserId,
            Feature = record.Feature,
            Provider = configuration.AiChatProvider,
            Model = configuration.AiChatModel ?? string.Empty,
            Status = record.Status,
            InputTokens = record.Usage.InputTokens,
            CachedInputTokens = record.Usage.CachedInputTokens,
            OutputTokens = record.Usage.OutputTokens,
            LatencyMilliseconds = (int)Math.Min(int.MaxValue, record.Latency.TotalMilliseconds),
            OccurredAt = now,
        };

        repository.Add(entry);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            repository.Discard(entry);
            logger.LogWarning("Could not record AI usage: {ExceptionType}", exception.GetType().Name);
        }
    }

    public async Task<Result<AiUsageReport>> ReportAsync(AiUsageReportRequest request, CancellationToken cancellationToken)
    {
        var from = request.From!.Value.ToUniversalTime();
        var to = request.To!.Value.ToUniversalTime();
        var models = await repository.SummarizeAsync(from, to, cancellationToken);
        return new AiUsageReport(from, to, models);
    }
}
