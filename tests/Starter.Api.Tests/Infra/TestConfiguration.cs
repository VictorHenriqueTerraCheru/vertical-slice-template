using Microsoft.Extensions.Configuration;

using Starter.Api.Common;

namespace Starter.Api.Tests.Infra;

public static class TestConfiguration
{
    public static EnvironmentConfiguration Create(string environment, Dictionary<string, string?>? values = null)
    {
        var settings = new Dictionary<string, string?>
        {
            [ConfigurationKeys.DatabaseConnection] = "Host=localhost;Database=x",
            [ConfigurationKeys.JwtKey] = "a-test-key-with-more-than-32-characters",
            [ConfigurationKeys.JwtIssuer] = "issuer",
            [ConfigurationKeys.JwtAudience] = "audience",
        };
        foreach (var (key, value) in values ?? [])
        {
            settings[key] = value;
        }

        return new EnvironmentConfiguration(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), environment);
    }
}
