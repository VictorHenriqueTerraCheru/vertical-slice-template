using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Starter.Api.Auth;
using Starter.Api.Common;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Auth;

[Collection(nameof(ApiCollection))]
public sealed class LoginTests(ApiFactory factory)
{
    private readonly EnvironmentConfiguration _configuration = factory.Services.GetRequiredService<EnvironmentConfiguration>();

    private ApiClient NewClient() => new(factory.CreateClient());

    private async Task LockAccountAsync(RegisteredUser user)
    {
        var client = NewClient();
        for (var attempt = 0; attempt < _configuration.LockoutMaxFailures; attempt++)
        {
            (await client.LoginAsync(user.Email, "wrong-password")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    [Fact]
    public async Task Correct_credentials_return_tokens()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var response = await NewClient().LoginAsync(user.Email, user.Password);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await ApiClient.ReadAsync<TokenPair>(response);
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBe(user.Tokens.RefreshToken);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_answer_the_same()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var wrongPassword = await NewClient().LoginAsync(user.Email, "wrong-password");
        var unknownEmail = await NewClient().LoginAsync(Scenarios.NewEmail(), "wrong-password");

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertSameProblemAsync(wrongPassword, unknownEmail);
    }

    [Fact]
    public async Task Five_failures_lock_the_account()
    {
        var user = await Scenarios.RegisterAsync(factory);
        await LockAccountAsync(user);

        var locked = await NewClient().LoginAsync(user.Email, user.Password);
        var wrongPassword = await NewClient().LoginAsync(user.Email, "wrong-password");

        locked.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertSameProblemAsync(locked, wrongPassword);
    }

    [Fact]
    public async Task Failures_below_the_limit_do_not_lock_and_success_resets_the_count()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var client = NewClient();

        for (var round = 0; round < 2; round++)
        {
            for (var attempt = 0; attempt < _configuration.LockoutMaxFailures - 1; attempt++)
            {
                await client.LoginAsync(user.Email, "wrong-password");
            }

            (await client.LoginAsync(user.Email, user.Password)).StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Lockout_expires_with_time()
    {
        var user = await Scenarios.RegisterAsync(factory);
        await LockAccountAsync(user);

        factory.Clock.Advance(TimeSpan.FromMinutes(_configuration.LockoutMinutes + 1));

        var response = await NewClient().LoginAsync(user.Email, user.Password);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Access_token_expires()
    {
        var user = await Scenarios.RegisterAsync(factory);
        (await user.Client.GetMeAsync()).StatusCode.Should().Be(HttpStatusCode.OK);

        factory.Clock.Advance(TimeSpan.FromMinutes(_configuration.AccessTokenMinutes + 10));

        (await user.Client.GetMeAsync()).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static async Task AssertSameProblemAsync(HttpResponseMessage first, HttpResponseMessage second)
    {
        var firstBody = await ApiClient.ReadJsonAsync(first);
        var secondBody = await ApiClient.ReadJsonAsync(second);
        firstBody.GetProperty("status").GetInt32().Should().Be(secondBody.GetProperty("status").GetInt32());
        firstBody.GetProperty("title").GetString().Should().Be(secondBody.GetProperty("title").GetString());
        firstBody.TryGetProperty("type", out var firstType).Should().Be(secondBody.TryGetProperty("type", out var secondType));
        firstType.ToString().Should().Be(secondType.ToString());
    }
}
