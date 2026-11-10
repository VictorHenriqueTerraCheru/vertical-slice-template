using Microsoft.Extensions.DependencyInjection;

using Starter.Api.Common;
using Starter.Api.Infrastructure;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Common;

public sealed class EmailSenderRegistrationTests
{
    private static readonly Dictionary<string, string?> Smtp = new()
    {
        ["SMTP_HOST"] = "smtp.example.com",
        ["SMTP_PORT"] = "587",
        ["SMTP_USER"] = "user",
        ["SMTP_PASSWORD"] = "password",
        ["SMTP_SENDER_NAME"] = "Starter",
        ["SMTP_SENDER_EMAIL"] = "no-reply@example.com",
        ["PASSWORD_RESET_URL"] = "https://app.example.com/reset",
        ["CORS_ORIGINS"] = "https://app.example.com",
    };

    private static Type RegisteredSender(string environment, bool withSmtp)
    {
        var configuration = TestConfiguration.Create(environment, withSmtp ? Smtp : null);
        var services = new ServiceCollection().AddEmailSender(configuration);
        return services.Single(descriptor => descriptor.ServiceType == typeof(IEmailSender)).ImplementationType!;
    }

    [Fact]
    public void Development_without_smtp_host_logs_the_email() =>
        RegisteredSender(EnvironmentNames.Development, withSmtp: false).Should().Be(typeof(LoggingEmailSender));

    [Fact]
    public void Development_with_smtp_host_sends_through_SMTP() =>
        RegisteredSender(EnvironmentNames.Development, withSmtp: true).Should().Be(typeof(SmtpEmailSender));

    [Fact]
    public void Testing_always_logs_the_email() =>
        RegisteredSender(EnvironmentNames.Testing, withSmtp: true).Should().Be(typeof(LoggingEmailSender));

    [Theory]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Staging_and_Production_send_through_SMTP(string environment) =>
        RegisteredSender(environment, withSmtp: true).Should().Be(typeof(SmtpEmailSender));
}
