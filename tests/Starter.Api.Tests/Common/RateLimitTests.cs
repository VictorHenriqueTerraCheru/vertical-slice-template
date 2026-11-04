using System.Net;

using Microsoft.AspNetCore.Hosting;

using Starter.Api.Common;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Common;

[Collection(nameof(ApiCollection))]
public sealed class RateLimitTests(ApiFactory factory)
{
    private const int Limit = 2;

    private ApiClient LimitedClient(out IDisposable owner)
    {
        var configured = factory.WithWebHostBuilder(builder =>
            builder.UseSetting(ConfigurationKeys.RateLimitAuthPerMinute, Limit.ToString()));
        owner = configured;
        return new ApiClient(configured.CreateClient());
    }

    private static async Task AssertLimitedAsync(Func<Task<HttpResponseMessage>> call)
    {
        for (var attempt = 0; attempt < Limit; attempt++)
        {
            (await call()).StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var blocked = await call();

        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        blocked.Headers.RetryAfter.Should().NotBeNull();
        blocked.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var problem = await ApiClient.ReadJsonAsync(blocked);
        problem.GetProperty("status").GetInt32().Should().Be(429);
    }

    [Fact]
    public async Task Login_above_the_limit_returns_429()
    {
        var client = LimitedClient(out var owner);
        using var _ = owner;

        await AssertLimitedAsync(() => client.LoginAsync(Scenarios.NewEmail(), "wrong-password"));
    }

    [Fact]
    public async Task Register_above_the_limit_returns_429()
    {
        var client = LimitedClient(out var owner);
        using var _ = owner;

        await AssertLimitedAsync(() => client.RegisterAsync(Scenarios.NewEmail(), Scenarios.Password));
    }

    [Fact]
    public async Task Forgot_password_above_the_limit_returns_429()
    {
        var client = LimitedClient(out var owner);
        using var _ = owner;

        await AssertLimitedAsync(() => client.ForgotPasswordAsync(Scenarios.NewEmail()));
    }
}
