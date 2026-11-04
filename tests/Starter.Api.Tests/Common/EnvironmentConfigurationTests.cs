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

    [Fact]
    public void Production_rejects_the_example_key()
    {
        var values = Complete();
        values["JWT_KEY"] = "dev-only-change-me-use-at-least-32-characters";

        var act = () => new EnvironmentConfiguration(Build(values), EnvironmentNames.Production);

        act.Should().Throw<InvalidOperationException>().WithMessage("*JWT_KEY*");
    }
}
