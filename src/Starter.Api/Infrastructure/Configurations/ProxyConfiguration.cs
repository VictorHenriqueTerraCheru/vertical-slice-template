using Microsoft.AspNetCore.HttpOverrides;

using Starter.Api.Common;

namespace Starter.Api.Infrastructure;

public static class ProxyConfiguration
{
    public static IServiceCollection AddProxy(this IServiceCollection services, EnvironmentConfiguration configuration) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var network in configuration.TrustedProxyNetworks)
            {
                options.KnownIPNetworks.Add(network);
            }
        });

    public static WebApplication UseProxy(this WebApplication app)
    {
        app.UseForwardedHeaders();
        return app;
    }
}
