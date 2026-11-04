using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Starter.Api.Tests.Infra;

namespace Starter.Api.Tests.Architecture;

[Collection(nameof(ApiCollection))]
public sealed class EndpointSecurityTests(ApiFactory factory)
{
    private static readonly string[] PublicRoutes =
    [
        "/api/v1/auth/register",
        "/api/v1/auth/login",
        "/api/v1/auth/refresh",
        "/api/v1/auth/forgot-password",
        "/api/v1/auth/reset-password",
        "/api/v1/health",
        "/api/v1/health/database",
    ];

    private static readonly string[] PublicDocumentationPrefixes = ["/openapi", "/scalar"];

    private static bool IsPublic(string pattern)
    {
        var route = Normalize(pattern);
        return PublicRoutes.Contains(route)
            || PublicDocumentationPrefixes.Any(prefix => route.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static string Normalize(string pattern) => pattern.TrimEnd('/').Replace("?", string.Empty);

    [Fact]
    public void Every_route_requires_authentication_except_the_public_list()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();
        endpoints.Should().NotBeEmpty();

        var offenders = endpoints
            .Where(endpoint => !IsPublic(endpoint.RoutePattern.RawText!))
            .Where(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null
                || !endpoint.Metadata.OfType<IAuthorizeData>().Any())
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToList();

        offenders.Should().BeEmpty();
    }

    [Fact]
    public void Public_routes_that_exist_are_explicitly_anonymous_or_documentation()
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();

        var register = endpoints.Single(endpoint => Normalize(endpoint.RoutePattern.RawText!) == "/api/v1/auth/register");
        var login = endpoints.Single(endpoint => Normalize(endpoint.RoutePattern.RawText!) == "/api/v1/auth/login");

        register.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();
        login.Metadata.GetMetadata<IAllowAnonymous>().Should().NotBeNull();
    }
}
