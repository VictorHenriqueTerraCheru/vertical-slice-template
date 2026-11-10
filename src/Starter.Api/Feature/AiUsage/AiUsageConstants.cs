using Starter.Api.Common;

namespace Starter.Api.AiUsage;

public static class AiUsageConstants
{
    public const int FeatureMaxLength = 50;
    public const int ProviderMaxLength = 50;
    public const int ModelMaxLength = AiConstants.ModelMaxLength;
    public const int StatusMaxLength = 50;
    public const int MaxReportDays = 366;
    public const string SortByModel = "model";
    public const string SortByCalls = "calls";
    public const string SortByInputTokens = "inputTokens";
    public const string SortByOutputTokens = "outputTokens";
    public static IReadOnlyList<string> SortableFields { get; } = [SortByModel, SortByCalls, SortByInputTokens, SortByOutputTokens];
}
