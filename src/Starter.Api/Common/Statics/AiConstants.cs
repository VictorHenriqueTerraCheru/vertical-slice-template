namespace Starter.Api.Common;

public static class AiConstants
{
    public const string ProviderOpenAi = "openai";
    public const string OpenAiBaseUrl = "https://api.openai.com/v1/";
    public const string OpenAiResponsesPath = "responses";
    public const string FailureTimeout = "timeout";
    public const string FailureTransport = "transport";
    public const string FailureStreamEnded = "stream_ended";
    public const string FailureHttpPrefix = "http_";
    public const string FailureIncompletePrefix = "incomplete_";
    public const int ModelMaxLength = 100;
    public static IReadOnlyList<string> SupportedProviders { get; } = [ProviderOpenAi];
    public static TimeSpan ModelCallTimeout { get; } = TimeSpan.FromSeconds(90);
}
