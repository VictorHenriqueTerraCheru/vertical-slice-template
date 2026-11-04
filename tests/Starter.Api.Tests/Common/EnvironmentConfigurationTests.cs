using Microsoft.Extensions.Configuration;

using Starter.Api.Common;

namespace Starter.Api.Tests.Common;

public sealed class EnvironmentConfigurationTests
{
    private static readonly string[] SmtpKeys =
        ["SMTP_HOST", "SMTP_PORT", "SMTP_USER", "SMTP_PASSWORD", "SMTP_SENDER_NAME", "SMTP_SENDER_EMAIL", "PASSWORD_RESET_URL"];

    private static IConfiguration Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> Complete() => new()
    {
        ["DB_CONNECTION"] = "Host=localhost;Database=x",
        ["JWT_KEY"] = "a-production-key-with-more-than-32-characters",
        ["JWT_ISSUER"] = "issuer",
        ["JWT_AUDIENCE"] = "audience",
        ["SMTP_HOST"] = "smtp.example.com",
        ["SMTP_PORT"] = "587",
        ["SMTP_USER"] = "user",
        ["SMTP_PASSWORD"] = "password",
        ["SMTP_SENDER_NAME"] = "Starter",
        ["SMTP_SENDER_EMAIL"] = "no-reply@example.com",
        ["PASSWORD_RESET_URL"] = "https://app.example.com/reset",
        ["CORS_ORIGINS"] = "https://app.example.com",
    };

    [Fact]
    public void MissingRequiredKeys_lists_all_missing_keys()
    {
        var missing = EnvironmentConfiguration.MissingRequiredKeys(Build([]), EnvironmentNames.Development);

        missing.Should().BeEquivalentTo("DB_CONNECTION", "JWT_KEY", "JWT_ISSUER", "JWT_AUDIENCE");
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Staging_and_Production_require_smtp_and_password_reset_url(string environment)
    {
        var values = Complete();
        foreach (var key in SmtpKeys)
        {
            values.Remove(key);
        }

        var missing = EnvironmentConfiguration.MissingRequiredKeys(Build(values), environment);

        missing.Should().BeEquivalentTo(SmtpKeys);
    }

    [Fact]
    public void Production_requires_cors_origins()
    {
        var values = Complete();
        values["CORS_ORIGINS"] = "";

        var missing = EnvironmentConfiguration.MissingRequiredKeys(Build(values), EnvironmentNames.Production);

        missing.Should().BeEquivalentTo("CORS_ORIGINS");
    }

    [Fact]
    public void Jwt_key_shorter_than_32_characters_is_rejected()
    {
        var values = Complete();
        values["JWT_KEY"] = "too-short";

        var act = () => new EnvironmentConfiguration(Build(values), EnvironmentNames.Development);

        act.Should().Throw<InvalidOperationException>().WithMessage("*JWT_KEY*");
    }

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Staging_and_Production_reject_the_example_key(string environment)
    {
        var values = Complete();
        values["JWT_KEY"] = "dev-only-change-me-use-at-least-32-characters";

        var act = () => new EnvironmentConfiguration(Build(values), environment);

        act.Should().Throw<InvalidOperationException>().WithMessage("*JWT_KEY*");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void Development_and_Testing_accept_the_example_key(string environment)
    {
        var values = Complete();
        values["JWT_KEY"] = "dev-only-change-me-use-at-least-32-characters";

        var configuration = new EnvironmentConfiguration(Build(values), environment);

        configuration.JwtKey.Should().StartWith("dev-only-");
    }

    [Theory]
    [InlineData("production")]
    [InlineData("Homolog")]
    [InlineData("")]
    public void Unknown_environment_names_are_rejected(string environment)
    {
        var act = () => new EnvironmentConfiguration(Build(Complete()), environment);

        act.Should().Throw<InvalidOperationException>().WithMessage("*environment*");
    }

    [Fact]
    public void Trusted_proxy_networks_are_empty_by_default()
    {
        var configuration = new EnvironmentConfiguration(Build(Complete()), EnvironmentNames.Production);

        configuration.TrustedProxyNetworks.Should().BeEmpty();
    }

    [Fact]
    public void Trusted_proxy_networks_parse_a_comma_separated_list()
    {
        var values = Complete();
        values["TRUSTED_PROXY_NETWORKS"] = "10.0.0.0/8, 192.168.1.0/24";

        var configuration = new EnvironmentConfiguration(Build(values), EnvironmentNames.Production);

        configuration.TrustedProxyNetworks.Select(network => network.ToString())
            .Should().Equal("10.0.0.0/8", "192.168.1.0/24");
    }

    [Fact]
    public void Invalid_trusted_proxy_network_aborts_startup()
    {
        var values = Complete();
        values["TRUSTED_PROXY_NETWORKS"] = "10.0.0.0/8,not-a-cidr";

        var act = () => new EnvironmentConfiguration(Build(values), EnvironmentNames.Production);

        act.Should().Throw<InvalidOperationException>().WithMessage("*TRUSTED_PROXY_NETWORKS*");
    }
}
