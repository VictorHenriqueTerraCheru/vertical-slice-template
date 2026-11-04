using System.Net;
using Starter.Api.Auth;

namespace Starter.Api.Tests.Infra;

public static class Scenarios
{
    public static string Password { get; } = "correct-horse-battery";

    public static string NewEmail() => $"{Guid.NewGuid():N}@example.com";

    public static async Task<RegisteredUser> RegisterAsync(ApiFactory factory, string? email = null)
    {
        var client = new ApiClient(factory.CreateClient());
        email ??= NewEmail();

        var response = await client.RegisterAsync(email, Password);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var tokens = await ApiClient.ReadAsync<TokenPair>(response);
        client.Authenticate(tokens.AccessToken);
        return new RegisteredUser(email, Password, tokens, client);
    }
}
