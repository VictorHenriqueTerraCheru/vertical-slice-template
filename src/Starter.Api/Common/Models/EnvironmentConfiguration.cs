namespace Starter.Api.Common;

public sealed class EnvironmentConfiguration
{
    public EnvironmentConfiguration(IConfiguration configuration, string environment)
    {
        if (!EnvironmentNames.All.Contains(environment))
        {
            throw new InvalidOperationException(
                $"Unknown environment '{environment}'. Use one of: {string.Join(", ", EnvironmentNames.All)}.");
        }

        var missing = MissingRequiredKeys(configuration, environment);
        if (missing.Count > 0)
        {
            throw new InvalidOperationException($"Missing required environment variables: {string.Join(", ", missing)}.");
        }

        Environment = environment;
        DatabaseConnectionString = configuration[ConfigurationKeys.DatabaseConnection]!;
        JwtKey = ValidJwtKey(configuration[ConfigurationKeys.JwtKey]!, environment);
        JwtIssuer = configuration[ConfigurationKeys.JwtIssuer]!;
        JwtAudience = configuration[ConfigurationKeys.JwtAudience]!;
        AccessTokenMinutes = configuration.GetValue(ConfigurationKeys.AccessTokenMinutes, 15);
        RefreshTokenDays = configuration.GetValue(ConfigurationKeys.RefreshTokenDays, 30);
        LockoutMaxFailures = configuration.GetValue(ConfigurationKeys.LockoutMaxFailures, 5);
        LockoutMinutes = configuration.GetValue(ConfigurationKeys.LockoutMinutes, 15);
        RateLimitAuthPerMinute = configuration.GetValue(ConfigurationKeys.RateLimitAuthPerMinute, 10);
        CorsOrigins = Split(configuration[ConfigurationKeys.CorsOrigins]);
        SmtpHost = configuration[ConfigurationKeys.SmtpHost];
        SmtpPort = configuration.GetValue(ConfigurationKeys.SmtpPort, SmtpConstants.DefaultPort);
        SmtpUser = configuration[ConfigurationKeys.SmtpUser];
        SmtpPassword = configuration[ConfigurationKeys.SmtpPassword];
        SmtpSenderName = configuration[ConfigurationKeys.SmtpSenderName];
        SmtpSenderEmail = configuration[ConfigurationKeys.SmtpSenderEmail];
        SmtpSecurity = ParseSmtpSecurity(configuration[ConfigurationKeys.SmtpSecurity], SmtpPort, environment);
        EmailLogoUrl = OptionalUrl(configuration[ConfigurationKeys.EmailLogoUrl], ConfigurationKeys.EmailLogoUrl);
        PasswordResetUrl = configuration[ConfigurationKeys.PasswordResetUrl];
        InitialAdminEmail = configuration[ConfigurationKeys.InitialAdminEmail];
        InitialAdminPassword = configuration[ConfigurationKeys.InitialAdminPassword];
        if (string.IsNullOrWhiteSpace(InitialAdminEmail) != string.IsNullOrWhiteSpace(InitialAdminPassword))
        {
            throw new InvalidOperationException(
                $"{ConfigurationKeys.InitialAdminEmail} and {ConfigurationKeys.InitialAdminPassword} must be set together or both left empty.");
        }

        TrustedProxyNetworks = ParseNetworks(configuration[ConfigurationKeys.TrustedProxyNetworks]);
        AiChatProvider = ParseAiProvider(configuration[ConfigurationKeys.AiChatProvider]);
        AiChatModel = ValidModel(configuration[ConfigurationKeys.AiChatModel]);
        AiChatApiKey = configuration[ConfigurationKeys.AiChatApiKey];
        AiChatBaseUrl = SecureOutsideDevelopment(
            OptionalUrl(configuration[ConfigurationKeys.AiChatBaseUrl], ConfigurationKeys.AiChatBaseUrl) ?? new Uri(AiConstants.OpenAiBaseUrl),
            ConfigurationKeys.AiChatBaseUrl,
            environment);
        AiChatIncludeReasoning = configuration.GetValue(ConfigurationKeys.AiChatIncludeReasoning, true);
        AiChatReasoningEffort = string.IsNullOrWhiteSpace(configuration[ConfigurationKeys.AiChatReasoningEffort])
            ? null
            : configuration[ConfigurationKeys.AiChatReasoningEffort]!.Trim().ToLowerInvariant();
        RateLimitAiPerMinute = configuration.GetValue(ConfigurationKeys.RateLimitAiPerMinute, 10);
    }

    public string Environment { get; }

    public string DatabaseConnectionString { get; }

    public string JwtKey { get; }

    public string JwtIssuer { get; }

    public string JwtAudience { get; }

    public int AccessTokenMinutes { get; }

    public int RefreshTokenDays { get; }

    public int LockoutMaxFailures { get; }

    public int LockoutMinutes { get; }

    public int RateLimitAuthPerMinute { get; }

    public IReadOnlyList<string> CorsOrigins { get; }

    public string? SmtpHost { get; }

    public int SmtpPort { get; }

    public string? SmtpUser { get; }

    public string? SmtpPassword { get; }

    public string? SmtpSenderName { get; }

    public string? SmtpSenderEmail { get; }

    public SmtpSecurity SmtpSecurity { get; }

    public Uri? EmailLogoUrl { get; }

    public string? PasswordResetUrl { get; }

    public string? InitialAdminEmail { get; }

    public string? InitialAdminPassword { get; }

    public IReadOnlyList<System.Net.IPNetwork> TrustedProxyNetworks { get; }

    public string AiChatProvider { get; }

    public string? AiChatModel { get; }

    public string? AiChatApiKey { get; }

    public Uri AiChatBaseUrl { get; }

    public bool AiChatIncludeReasoning { get; }

    public string? AiChatReasoningEffort { get; }

    public int RateLimitAiPerMinute { get; }

    public bool AiChatConfigured => !string.IsNullOrWhiteSpace(AiChatApiKey);

    public static IReadOnlyList<string> MissingRequiredKeys(IConfiguration configuration, string environment) =>
        RequiredKeys(configuration, environment).Where(key => string.IsNullOrWhiteSpace(configuration[key])).Distinct().ToList();

    private static IEnumerable<string> RequiredKeys(IConfiguration configuration, string environment)
    {
        yield return ConfigurationKeys.DatabaseConnection;
        yield return ConfigurationKeys.JwtKey;
        yield return ConfigurationKeys.JwtIssuer;
        yield return ConfigurationKeys.JwtAudience;

        if (environment is EnvironmentNames.Staging or EnvironmentNames.Production)
        {
            yield return ConfigurationKeys.SmtpHost;
            yield return ConfigurationKeys.SmtpPort;
            yield return ConfigurationKeys.SmtpUser;
            yield return ConfigurationKeys.SmtpPassword;
            yield return ConfigurationKeys.SmtpSenderName;
            yield return ConfigurationKeys.SmtpSenderEmail;
            yield return ConfigurationKeys.PasswordResetUrl;
        }

        if (environment == EnvironmentNames.Production)
        {
            yield return ConfigurationKeys.CorsOrigins;
        }

        if (!string.IsNullOrWhiteSpace(configuration[ConfigurationKeys.SmtpHost]))
        {
            yield return ConfigurationKeys.SmtpSenderEmail;
        }

        if (!string.IsNullOrWhiteSpace(configuration[ConfigurationKeys.AiChatApiKey]))
        {
            yield return ConfigurationKeys.AiChatModel;
        }
    }

    private static SmtpSecurity ParseSmtpSecurity(string? value, int port, string environment)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return port == SmtpConstants.ImplicitTlsPort ? SmtpSecurity.SslOnConnect : SmtpSecurity.StartTls;
        }

        if (!Enum.TryParse<SmtpSecurity>(value, ignoreCase: true, out var security) || !Enum.IsDefined(security))
        {
            throw new InvalidOperationException(
                $"{ConfigurationKeys.SmtpSecurity} must be one of: {string.Join(", ", Enum.GetNames<SmtpSecurity>())}.");
        }

        if (security == SmtpSecurity.None && environment != EnvironmentNames.Development)
        {
            throw new InvalidOperationException($"{ConfigurationKeys.SmtpSecurity}=None is accepted only in Development.");
        }

        return security;
    }

    private static string ParseAiProvider(string? value)
    {
        var provider = string.IsNullOrWhiteSpace(value) ? AiConstants.ProviderOpenAi : value.Trim().ToLowerInvariant();
        if (!AiConstants.SupportedProviders.Contains(provider))
        {
            throw new InvalidOperationException(
                $"{ConfigurationKeys.AiChatProvider} must be one of: {string.Join(", ", AiConstants.SupportedProviders)}.");
        }

        return provider;
    }

    private static string? ValidModel(string? value)
    {
        if (value is not null && value.Trim().Length > AiConstants.ModelMaxLength)
        {
            throw new InvalidOperationException($"{ConfigurationKeys.AiChatModel} must have at most {AiConstants.ModelMaxLength} characters.");
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Uri SecureOutsideDevelopment(Uri url, string key, string environment)
    {
        if (url.Scheme != Uri.UriSchemeHttps && environment != EnvironmentNames.Development)
        {
            throw new InvalidOperationException($"{key} must use https outside Development.");
        }

        return url;
    }

    private static Uri? OptionalUrl(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException($"{key} must be an absolute http or https URL.");
        }

        return url;
    }

    private static string ValidJwtKey(string key, string environment)
    {
        if (key.Length < SecurityConstants.JwtKeyMinimumLength)
        {
            throw new InvalidOperationException($"{ConfigurationKeys.JwtKey} must have at least {SecurityConstants.JwtKeyMinimumLength} characters.");
        }

        if (environment is not (EnvironmentNames.Development or EnvironmentNames.Testing) && key.StartsWith(SecurityConstants.ExampleJwtKeyPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{ConfigurationKeys.JwtKey} cannot be the example key outside Development and Testing.");
        }

        return key;
    }

    private static List<System.Net.IPNetwork> ParseNetworks(string? value)
    {
        var networks = new List<System.Net.IPNetwork>();
        foreach (var entry in Split(value))
        {
            if (!System.Net.IPNetwork.TryParse(entry, out var network))
            {
                throw new InvalidOperationException($"{ConfigurationKeys.TrustedProxyNetworks} contains an invalid CIDR: '{entry}'.");
            }

            networks.Add(network);
        }

        return networks;
    }

    private static string[] Split(string? value) =>
        value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];
}
