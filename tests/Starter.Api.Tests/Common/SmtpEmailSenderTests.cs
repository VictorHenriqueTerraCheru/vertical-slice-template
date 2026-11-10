using System.Net.Http.Json;
using System.Text.Json;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

using Starter.Api.Auth;
using Starter.Api.Common;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Common;

public sealed class SmtpEmailSenderTests : IAsyncLifetime
{
    private const ushort SmtpPort = 1025;
    private const ushort HttpPort = 8025;

    private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit:v1.32")
        .WithPortBinding(SmtpPort, true)
        .WithPortBinding(HttpPort, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(HttpPort).ForPath("/livez")))
        .Build();

    public Task InitializeAsync() => _mailpit.StartAsync();

    public Task DisposeAsync() => _mailpit.DisposeAsync().AsTask();

    [Fact]
    public async Task Delivers_text_and_html_through_a_real_smtp_server()
    {
        var configuration = TestConfiguration.Create(EnvironmentNames.Development, new()
        {
            [ConfigurationKeys.SmtpHost] = _mailpit.Hostname,
            [ConfigurationKeys.SmtpPort] = _mailpit.GetMappedPublicPort(SmtpPort).ToString(System.Globalization.CultureInfo.InvariantCulture),
            [ConfigurationKeys.SmtpSecurity] = "None",
            [ConfigurationKeys.SmtpSenderName] = "Acme",
            [ConfigurationKeys.SmtpSenderEmail] = "no-reply@acme.test",
        });
        var message = PasswordResetEmail.Create("someone@acme.test", "https://app.acme.test/reset?token=abc", EmailBrand.From(configuration));

        await new SmtpEmailSender(configuration).SendAsync(message, CancellationToken.None);

        using var http = new HttpClient { BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(HttpPort)}/") };
        var list = await http.GetFromJsonAsync<JsonElement>("api/v1/messages");
        var summary = list.GetProperty("messages").EnumerateArray().Should().ContainSingle().Subject;
        summary.GetProperty("Subject").GetString().Should().Be(AuthConstants.PasswordResetSubject);
        summary.GetProperty("From").GetProperty("Address").GetString().Should().Be("no-reply@acme.test");
        summary.GetProperty("To")[0].GetProperty("Address").GetString().Should().Be("someone@acme.test");

        var detail = await http.GetFromJsonAsync<JsonElement>($"api/v1/message/{summary.GetProperty("ID").GetString()}");
        detail.GetProperty("Text").GetString().Should().Contain("https://app.acme.test/reset?token=abc");
        detail.GetProperty("HTML").GetString().Should().Contain("Escolher nova senha");
    }
}
