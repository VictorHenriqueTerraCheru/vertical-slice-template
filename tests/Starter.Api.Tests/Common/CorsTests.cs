using Microsoft.AspNetCore.Hosting;

using Starter.Api.Common;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Common;

[Collection(nameof(ApiCollection))]
public sealed class CorsTests(ApiFactory factory)
{
    private const string AllowedOrigin = "https://app.example.com";

    private async Task<HttpResponseMessage> GetWithOriginAsync(string origin)
    {
        using var configured = factory.WithWebHostBuilder(builder => builder.UseSetting(ConfigurationKeys.CorsOrigins, AllowedOrigin));
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/health");
        request.Headers.Add("Origin", origin);
        return await configured.CreateClient().SendAsync(request);
    }

    [Fact]
    public async Task Allowed_origin_receives_the_cors_header()
    {
        var response = await GetWithOriginAsync(AllowedOrigin);

        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle().Which.Should().Be(AllowedOrigin);
    }

    [Fact]
    public async Task Disallowed_origin_does_not_receive_the_cors_header()
    {
        var response = await GetWithOriginAsync("https://evil.example.com");

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
