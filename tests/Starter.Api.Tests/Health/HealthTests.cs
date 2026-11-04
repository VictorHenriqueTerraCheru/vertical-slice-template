using System.Net;
using System.Net.Http.Json;

using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Health;

[Collection(nameof(ApiCollection))]
public sealed class HealthTests(ApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Health_responds_200_without_token()
    {
        var response = await _client.GetAsync("/api/v1/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body!["status"].Should().Be("healthy");
    }

    [Fact]
    public async Task Database_responds_200_when_database_is_up()
    {
        var response = await _client.GetAsync("/api/v1/health/database");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
