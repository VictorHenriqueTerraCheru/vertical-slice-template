using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Starter.Api.Auth;
using Starter.Api.Common;
using Starter.Api.Persistence;
using Starter.Api.Tests.Infra;
using Starter.Api.Users;

namespace Starter.Api.Tests.Auth;

[Collection(nameof(ApiCollection))]
public sealed class RefreshTests(ApiFactory factory)
{
    private readonly EnvironmentConfiguration _configuration = factory.Services.GetRequiredService<EnvironmentConfiguration>();

    private ApiClient NewClient() => new(factory.CreateClient());

    [Fact]
    public async Task Refresh_returns_a_new_pair_and_invalidates_the_old_one()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var response = await NewClient().RefreshAsync(user.Tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var renewed = await ApiClient.ReadAsync<TokenPair>(response);
        renewed.RefreshToken.Should().NotBe(user.Tokens.RefreshToken);
        renewed.AccessToken.Should().NotBeNullOrWhiteSpace();
        (await NewClient().RefreshAsync(user.Tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await NewClient().RefreshAsync(renewed.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task The_same_refresh_in_parallel_lets_only_one_win()
    {
        var user = await Scenarios.RegisterAsync(factory);

        var responses = await Task.WhenAll(
            NewClient().RefreshAsync(user.Tokens.RefreshToken),
            NewClient().RefreshAsync(user.Tokens.RefreshToken));

        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized).Should().Be(1);

        var userId = (await ApiClient.ReadAsync<UserResponse>(await user.Client.GetMeAsync())).Id;
        var revokedFlags = await factory.InDatabaseAsync(db => db.Set<RefreshToken>()
            .Where(row => row.UserId == userId)
            .Select(row => row.RevokedAt != null)
            .ToListAsync());
        revokedFlags.Should().HaveCount(2);
        revokedFlags.Count(revoked => revoked).Should().Be(1);
        revokedFlags.Count(revoked => !revoked).Should().Be(1);
    }

    [Fact]
    public async Task Expired_refresh_returns_401()
    {
        var user = await Scenarios.RegisterAsync(factory);

        factory.Clock.Advance(TimeSpan.FromDays(_configuration.RefreshTokenDays + 1));

        (await NewClient().RefreshAsync(user.Tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unknown_refresh_returns_401()
    {
        var response = await NewClient().RefreshAsync("unknown-refresh-token");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Empty_refresh_returns_400()
    {
        var response = await NewClient().RefreshAsync(string.Empty);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_of_a_locked_account_returns_401_and_keeps_the_token_active()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var normalized = user.Email.ToUpperInvariant();
        await factory.InDatabaseAsync(db => db.Set<User>()
            .Where(row => row.NormalizedEmail == normalized)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.LockoutEnd, DateTimeOffset.MaxValue)));

        var response = await NewClient().RefreshAsync(user.Tokens.RefreshToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var tokenHash = factory.Services.GetRequiredService<ITokenService>().Hash(user.Tokens.RefreshToken);
        var revoked = await factory.InDatabaseAsync(db => db.Set<RefreshToken>()
            .Where(row => row.TokenHash == tokenHash)
            .Select(row => row.RevokedAt)
            .SingleAsync());
        revoked.Should().BeNull();
    }

    [Fact]
    public async Task Logout_revokes_every_refresh_of_the_account()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var secondLogin = await ApiClient.ReadAsync<TokenPair>(await NewClient().LoginAsync(user.Email, user.Password));

        var response = await user.Client.LogoutAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await NewClient().RefreshAsync(user.Tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await NewClient().RefreshAsync(secondLogin.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_does_not_touch_other_accounts()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var other = await Scenarios.RegisterAsync(factory);

        await user.Client.LogoutAsync();

        (await NewClient().RefreshAsync(other.Tokens.RefreshToken)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_without_token_returns_401()
    {
        var response = await NewClient().LogoutAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Database_stores_only_the_hash()
    {
        var user = await Scenarios.RegisterAsync(factory);
        var userId = (await ApiClient.ReadAsync<UserResponse>(await user.Client.GetMeAsync())).Id;

        var hashes = await factory.InDatabaseAsync(db => db.Set<RefreshToken>()
            .Where(row => row.UserId == userId)
            .Select(row => row.TokenHash)
            .ToListAsync());

        hashes.Should().ContainSingle();
        hashes[0].Should().NotBe(user.Tokens.RefreshToken);
        hashes[0].Should().Be(factory.Services.GetRequiredService<ITokenService>().Hash(user.Tokens.RefreshToken));
    }
}
