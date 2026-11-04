using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Starter.Api.Common;
using Starter.Api.Infrastructure;

namespace Starter.Api.Tests.Common;

public sealed class EmailSenderRegistrationTests
{
    private static Type RegisteredSender(string environment)
    {
        var configuration = new EnvironmentConfiguration(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
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
            }).Build(),
            environment);

        var services = new ServiceCollection().AddEmailSender(configuration);
        return services.Single(descriptor => descriptor.ServiceType == typeof(IEmailSender)).ImplementationType!;
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void Development_and_Testing_log_the_email(string environment) =>
        RegisteredSender(environment).Should().Be(typeof(LoggingEmailSender));

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Staging_and_Production_send_through_SMTP(string environment) =>
        RegisteredSender(environment).Should().Be(typeof(SmtpEmailSender));
}
