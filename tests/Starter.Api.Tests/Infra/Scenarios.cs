using System.Net;
using System.Net.Http.Headers;

using Microsoft.EntityFrameworkCore;

using Starter.Api.Auth;
using Starter.Api.Common;
using Starter.Api.Persistence;
using Starter.Api.Users;

namespace Starter.Api.Tests.Infra;

public static class Scenarios
{
    public static string Password { get; } = "correct-horse-battery";

    public static string NewPassword { get; } = "another-strong-password";

    public static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    public static async Task<RegisteredUser> RegisterAsync(ApiFactory factory, string? email = null)
    {
        var client = new ApiClient(factory.CreateClient());
        email ??= NewEmail();

        var response = await client.RegisterAsync(email, Password);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var tokens = await ApiClient.ReadAsync<TokenPair>(response);
        client.Authenticate(tokens.AccessToken);
        var me = await ApiClient.ReadAsync<UserResponse>(await client.GetMeAsync());
        return new RegisteredUser(email, Password, tokens, client, me.Id);
    }

    public static async Task<HttpClient> AdministratorClientAsync(ApiFactory factory)
    {
        var user = await RegisterAsync(factory);
        await factory.InDatabaseAsync(db => db.Set<User>()
            .Where(row => row.Id == user.UserId)
            .ExecuteUpdateAsync(set => set.SetProperty(row => row.Role, UserRole.Administrator)));
        var login = await user.Client.LoginAsync(user.Email, user.Password);
        login.EnsureSuccessStatusCode();
        var tokens = await ApiClient.ReadAsync<TokenPair>(login);
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return http;
    }

    public static HttpClient ClientFor(ApiFactory factory, RegisteredUser user)
    {
        var http = factory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Tokens.AccessToken);
        return http;
    }
}
