using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Common;

[Collection(nameof(ApiCollection))]
public sealed class OpenApiTests(ApiFactory factory)
{
    [Fact]
    public async Task Openapi_lists_the_auth_routes_and_the_bearer_scheme()
    {
        var document = await ApiClient.ReadJsonAsync(await factory.CreateClient().GetAsync("/openapi/v1.json"));

        var paths = document.GetProperty("paths");
        foreach (var route in new[] { "register", "login", "refresh", "logout", "forgot-password", "reset-password", "change-password" })
        {
            paths.TryGetProperty($"/api/v1/auth/{route}", out _).Should().BeTrue(route);
        }

        var bearer = document.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        bearer.GetProperty("scheme").GetString().Should().Be("bearer");
        bearer.GetProperty("type").GetString().Should().Be("http");
    }
}
