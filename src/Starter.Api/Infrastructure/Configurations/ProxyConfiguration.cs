using Microsoft.AspNetCore.HttpOverrides;

namespace Starter.Api.Infrastructure;

public static class ProxyConfiguration
{
    private static readonly string[] TrustedNetworks = ["10.0.0.0/8", "172.16.0.0/12", "192.168.0.0/16"];

    public static IServiceCollection AddProxy(this IServiceCollection services) =>
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();
            foreach (var network in TrustedNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

    public static WebApplication UseProxy(this WebApplication app)
    {
        app.UseForwardedHeaders();
        return app;
    }
}
