using System.Net;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Auth;
using Starter.Api.Persistence;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Auth;

[Collection(nameof(ApiCollection))]
public sealed class RegisterTests(ApiFactory factory)
{
    private ApiClient NewClient() => new(factory.CreateClient());

    [Fact]
    public async Task Valid_registration_returns_201_and_tokens()
    {
        var response = await NewClient().RegisterAsync(Scenarios.NewEmail(), Scenarios.Password);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var tokens = await ApiClient.ReadAsync<TokenPair>(response);
        tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        tokens.AccessTokenExpiresAt.Should().BeAfter(factory.Clock.GetUtcNow());
        tokens.RefreshTokenExpiresAt.Should().BeAfter(tokens.AccessTokenExpiresAt);
    }

    [Fact]
    public async Task Duplicate_email_returns_409_even_with_different_casing()
    {
        var email = Scenarios.NewEmail();
        await Scenarios.RegisterAsync(factory, email);

        var same = await NewClient().RegisterAsync(email, Scenarios.Password);
        var upper = await NewClient().RegisterAsync(email.ToUpperInvariant(), Scenarios.Password);

        same.StatusCode.Should().Be(HttpStatusCode.Conflict);
        upper.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Two_simultaneous_registrations_of_the_same_email_only_one_wins()
    {
        var email = Scenarios.NewEmail();

        var responses = await Task.WhenAll(
            NewClient().RegisterAsync(email, Scenarios.Password),
            NewClient().RegisterAsync(email, Scenarios.Password));

        responses.Select(response => response.StatusCode).Should()
            .BeEquivalentTo([HttpStatusCode.Created, HttpStatusCode.Conflict]);
        var normalized = email.ToUpperInvariant();
        var users = await factory.InDatabaseAsync(db => db.Set<User>().CountAsync(user => user.NormalizedEmail == normalized));
        users.Should().Be(1);
        var refreshTokens = await factory.InDatabaseAsync(db => db.Set<RefreshToken>()
            .CountAsync(token => db.Set<User>().Any(user => user.Id == token.UserId && user.NormalizedEmail == normalized)));
        refreshTokens.Should().Be(1);
    }

    [Fact]
    public async Task Short_password_returns_400_on_the_password_field()
    {
        var response = await NewClient().RegisterAsync(Scenarios.NewEmail(), "short");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiClient.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("password", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_email_returns_400()
    {
        var response = await NewClient().RegisterAsync("not-an-email", Scenarios.Password);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ApiClient.ReadJsonAsync(response);
        body.GetProperty("errors").TryGetProperty("email", out _).Should().BeTrue();
    }
}
