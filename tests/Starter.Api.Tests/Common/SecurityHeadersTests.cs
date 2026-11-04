using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Common;

[Collection(nameof(ApiCollection))]
public sealed class SecurityHeadersTests(ApiFactory factory)
{
    [Theory]
    [InlineData("/api/v1/health")]
    [InlineData("/api/v1/me")]
    [InlineData("/api/v1/does-not-exist")]
    public async Task Response_carries_the_security_headers(string path)
    {
        var response = await factory.CreateClient().GetAsync(path);

        response.Headers.GetValues("X-Content-Type-Options").Should().Equal("nosniff");
        response.Headers.GetValues("X-Frame-Options").Should().Equal("DENY");
        response.Headers.GetValues("Referrer-Policy").Should().Equal("no-referrer");
    }
}
