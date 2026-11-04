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
        SmtpPort = configuration.GetValue(ConfigurationKeys.SmtpPort, 587);
        SmtpUser = configuration[ConfigurationKeys.SmtpUser];
        SmtpPassword = configuration[ConfigurationKeys.SmtpPassword];
        SmtpSenderName = configuration[ConfigurationKeys.SmtpSenderName];
        SmtpSenderEmail = configuration[ConfigurationKeys.SmtpSenderEmail];
        PasswordResetUrl = configuration[ConfigurationKeys.PasswordResetUrl];
        InitialAdminEmail = configuration[ConfigurationKeys.InitialAdminEmail];
        InitialAdminPassword = configuration[ConfigurationKeys.InitialAdminPassword];
        if (string.IsNullOrWhiteSpace(InitialAdminEmail) != string.IsNullOrWhiteSpace(InitialAdminPassword))
        {
            throw new InvalidOperationException(
                $"{ConfigurationKeys.InitialAdminEmail} and {ConfigurationKeys.InitialAdminPassword} must be set together or both left empty.");
        }

        TrustedProxyNetworks = ParseNetworks(configuration[ConfigurationKeys.TrustedProxyNetworks]);
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

    public string? PasswordResetUrl { get; }

    public string? InitialAdminEmail { get; }

    public string? InitialAdminPassword { get; }

    public IReadOnlyList<System.Net.IPNetwork> TrustedProxyNetworks { get; }

    public static IReadOnlyList<string> MissingRequiredKeys(IConfiguration configuration, string environment) =>
        RequiredKeys(environment).Where(key => string.IsNullOrWhiteSpace(configuration[key])).ToList();

    private static IEnumerable<string> RequiredKeys(string environment)
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
